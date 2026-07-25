// Per-entity import configs. Each captures the loaded reference data (accounts, categories) so it can
// resolve names → ids, then delegates creation to the existing typed API modules.
import type { Account, AccountGroup, Category } from '@/types/api';
import { createAccount } from '@/lib/accounts';
import { createTransaction } from '@/lib/transactions';
import { flowAccepts } from '@/lib/categories';
import { displayName } from '@/lib/seededNames';
import { ApiClientError } from '@/lib/api';
import { type ImportConfig, parseAmount } from '@/lib/import';

const ACCOUNT_TYPES = ['Cash', 'Bank', 'EWallet', 'Investment', 'Blocked'];
const TX_TYPES = ['Income', 'Expense', 'Transfer'];
const STATUSES = ['Cleared', 'Uncleared'];

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
