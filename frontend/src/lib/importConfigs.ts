// Per-entity import configs. Each captures the loaded reference data (accounts, categories) so it can
// resolve names → ids, then delegates creation to the existing typed API modules.
import type { Account, AccountGroup, Category, MasterPlanSection } from '@/types/api';
import { createAccount } from '@/lib/accounts';
import { createTransaction } from '@/lib/transactions';
import { createCategory, flowAccepts } from '@/lib/categories';
import { createMasterPlanItem } from '@/lib/budgeting';
import { displayName } from '@/lib/seededNames';
import { ApiClientError } from '@/lib/api';
import { type ImportConfig, parseAmount } from '@/lib/import';

const ACCOUNT_TYPES = ['Cash', 'Bank', 'EWallet', 'Investment', 'Blocked'];
const TX_TYPES = ['Income', 'Expense', 'Transfer'];
const STATUSES = ['Cleared', 'Uncleared'];
const CATEGORY_LEVELS = ['Budget', 'Category', 'Sub'];
const CATEGORY_FLOWS = ['Any', 'Income', 'Expense', 'Transfer'];
const PARENT_LEVEL: Record<string, string> = { Category: 'Budget', Sub: 'Category' };

const ci = (a: string, b: string) => a.toLowerCase() === b.toLowerCase();
const canon = (list: string[], v: string) => list.find((x) => ci(x, v));

function findAccount(accounts: Account[], name: string): Account | undefined {
  return accounts.find((a) => ci(a.name, name));
}
function findCategory(categories: Category[], name: string, level: string, locale: string): Category | undefined {
  return categories.find(
    (c) => c.level === level && (ci(c.name, name) || ci(displayName(c.name, locale), name)),
  );
}

// --- Accounts ---------------------------------------------------------------

export function accountsImportConfig(groups: AccountGroup[]): ImportConfig {
  return {
    templateName: 'tameru-accounts-template',
    columns: ['name', 'type', 'openingBalance', 'currency', 'group'],
    sample: ['BCA', 'Bank', '15000000', 'IDR', ''],
    summary: (r) => r.name || '—',
    validate: (r) => {
      if (!r.name) return 'name is required';
      if (r.type && !canon(ACCOUNT_TYPES, r.type)) return `invalid type '${r.type}'`;
      if (r.openingbalance && parseAmount(r.openingbalance) === null) return 'invalid openingBalance';
      if (r.group && !groups.some((g) => ci(g.name, r.group))) return `unknown group '${r.group}'`;
      return null;
    },
    importRecord: async (r) => {
      const group = r.group ? groups.find((g) => ci(g.name, r.group)) : undefined;
      await createAccount({
        name: r.name,
        type: canon(ACCOUNT_TYPES, r.type) ?? 'Bank',
        openingBalance: parseAmount(r.openingbalance) ?? 0,
        groupId: group?.id ?? null,
        currencyCode: r.currency || 'IDR',
        sortOrder: 0,
      });
    },
  };
}

// --- Transactions -----------------------------------------------------------

export function transactionsImportConfig(
  accounts: Account[],
  categories: Category[],
  locale: string,
): ImportConfig {
  return {
    templateName: 'tameru-transactions-template',
    columns: ['date', 'type', 'title', 'amount', 'account', 'toAccount', 'budget', 'category', 'status', 'description'],
    sample: ['2026-07-15', 'Expense', 'Groceries', '250000', 'BCA', '', 'Needs', 'Food', 'Cleared', ''],
    summary: (r) => r.title || '—',
    validate: (r) => {
      if (!r.date || Number.isNaN(Date.parse(r.date))) return 'invalid or missing date (use YYYY-MM-DD)';
      const type = canon(TX_TYPES, r.type);
      if (!type) return `invalid type '${r.type}' (Income/Expense/Transfer)`;
      if (!r.title) return 'title is required';
      const amount = parseAmount(r.amount);
      if (amount === null || amount <= 0) return 'amount must be a positive number';
      if (!findAccount(accounts, r.account)) return `unknown account '${r.account}'`;
      if (type === 'Transfer') {
        if (!r.toaccount) return 'toAccount is required for a Transfer';
        if (!findAccount(accounts, r.toaccount)) return `unknown toAccount '${r.toaccount}'`;
      } else {
        if (r.budget && !findCategory(categories, r.budget, 'Budget', locale)) return `unknown budget '${r.budget}'`;
        if (r.category && !findCategory(categories, r.category, 'Category', locale)) return `unknown category '${r.category}'`;
      }
      if (r.status && !canon(STATUSES, r.status)) return `invalid status '${r.status}'`;
      return null;
    },
    importRecord: async (r) => {
      const type = canon(TX_TYPES, r.type) ?? 'Expense';
      const account = findAccount(accounts, r.account)!;
      const base = {
        type,
        date: r.date,
        title: r.title,
        amount: parseAmount(r.amount) ?? 0,
        accountId: account.id,
        status: canon(STATUSES, r.status) ?? 'Uncleared',
        description: r.description || null,
      };
      if (type === 'Transfer') {
        await createTransaction({ ...base, toAccountId: findAccount(accounts, r.toaccount)!.id });
      } else {
        const budget = r.budget ? findCategory(categories, r.budget, 'Budget', locale) : undefined;
        const category = r.category ? findCategory(categories, r.category, 'Category', locale) : undefined;
        // Surface an obvious flow mismatch before the round-trip (server still enforces it).
        if (budget && !flowAccepts(budget.flow, type)) {
          throw new ApiClientError('category_flow_mismatch', 'category_flow_mismatch');
        }
        await createTransaction({
          ...base,
          budgetCategoryId: budget?.id ?? null,
          categoryId: category?.id ?? null,
        });
      }
    },
  };
}

// --- Categories -------------------------------------------------------------
// Parents must already exist (import Budgets first, or add categories under the seeded budgets).

export function categoriesImportConfig(categories: Category[], locale: string): ImportConfig {
  return {
    templateName: 'tameru-categories-template',
    columns: ['name', 'level', 'parent', 'flow'],
    sample: ['Groceries', 'Category', 'Needs', 'Expense'],
    summary: (r) => r.name || '—',
    validate: (r) => {
      if (!r.name) return 'name is required';
      const level = canon(CATEGORY_LEVELS, r.level);
      if (!level) return `invalid level '${r.level}' (Budget/Category/Sub)`;
      if (r.flow && !canon(CATEGORY_FLOWS, r.flow)) return `invalid flow '${r.flow}'`;
      if (level !== 'Budget') {
        if (!r.parent) return `parent is required for a ${level}`;
        if (!findCategory(categories, r.parent, PARENT_LEVEL[level], locale)) {
          return `unknown parent '${r.parent}' (must be an existing ${PARENT_LEVEL[level]})`;
        }
      }
      return null;
    },
    importRecord: async (r) => {
      const level = canon(CATEGORY_LEVELS, r.level) ?? 'Category';
      const parent = level === 'Budget' ? undefined : findCategory(categories, r.parent, PARENT_LEVEL[level], locale);
      await createCategory({
        name: r.name,
        level,
        parentId: parent?.id ?? null,
        flow: canon(CATEGORY_FLOWS, r.flow) ?? 'Any',
        sortOrder: 0,
      });
    },
  };
}

// --- Master Plan items ------------------------------------------------------

export function masterPlanImportConfig(sections: MasterPlanSection[], locale: string): ImportConfig {
  const findSection = (name: string) =>
    sections.find((s) => ci(s.name, name) || ci(displayName(s.name, locale), name));
  return {
    templateName: 'tameru-master-plan-template',
    columns: ['section', 'name', 'price', 'frequency'],
    sample: ['Investment', 'Mutual funds', '2000000', '12'],
    summary: (r) => r.name || '—',
    validate: (r) => {
      if (!findSection(r.section)) return `unknown section '${r.section}' (Investment/Needs/Wants)`;
      if (!r.name) return 'name is required';
      if (parseAmount(r.price) === null) return 'invalid price';
      const freq = Number(r.frequency);
      if (!Number.isInteger(freq) || freq < 1) return 'frequency must be a whole number ≥ 1';
      return null;
    },
    importRecord: async (r) => {
      await createMasterPlanItem({
        sectionId: findSection(r.section)!.id,
        name: r.name,
        price: parseAmount(r.price) ?? 0,
        frequency: Number(r.frequency),
        sortOrder: 0,
      });
    },
  };
}
