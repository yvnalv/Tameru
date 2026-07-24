# Modules

Module catalog for Tameru. Each module is a Clean-Architecture slice (Domain / Application /
Infrastructure / Api) that owns its own tables and communicates with others only through contracts or
integration events. See [ARCHITECTURE.md](ARCHITECTURE.md).

## MVP modules

### Identity
- **Purpose:** single-user authentication. One owner account; email + password; JWT access + refresh.
- **Owns:** `Users`, `RefreshTokens`.
- **Depends on:** BuildingBlocks only.
- **Notes:** no roles, no RBAC, no tenancy. Provides `ICurrentUser` to the rest of the app.

### Accounts
- **Purpose:** money containers and their grouping; expose derived balances.
- **Owns:** `accounts.accounts`, `accounts.account_groups`.
- **Provides (contract):** `IAccountDirectory` (exists/active, currency) for other modules.
- **Consumes (contract):** `ILedgerAccountQuery` (net movement per account, has-transactions) to
  derive current balances (`opening + movement`) and guard deactivation. A `NoOp` implementation is
  used until the Ledger module (M3) provides the real one.
- **Source sheets:** `Account`.
- **Status:** ✅ implemented (M2) — CRUD, deactivate guard (BR-021), group roll-ups, seeded groups.

### Ledger
- **Purpose:** the cashflow core. Income / Expense / Transfer transactions.
- **Owns:** `Transactions`.
- **Provides (contract):** `ILedgerAccountQuery` (net movement per account, has-transactions — for
  Accounts), and `ICategorySpendQuery` (expense totals per category per month — for Budgeting).
  Publishes `TransactionPosted`, `TransactionUpdated`, `TransactionVoided` (planned).
- **Consumes (contract):** Accounts's `IAccountDirectory` (account exists/active) and Budgeting's
  `ICategoryDirectory` (category exists/active/flow, BR-005/006). A permissive no-op category
  directory is used if Budgeting is absent.
- **Rules:** transfer must have distinct `AccountId` and `ToAccountId`; amount > 0; voided rather than
  hard-deleted (soft-delete). See [BUSINESS_RULES.md](BUSINESS_RULES.md).
- **Source sheets:** `Income`, `Expenses`, `Account Transfer`, `Expenses (Sample)`.
- **Status:** ✅ implemented (M3, extended in M4) — Income/Expense/Transfer CRUD, clear/unclear, void,
  filtered list; `BalanceCalculator` + real `ILedgerAccountQuery` make Accounts balances live;
  category-flow validation and the Budget-actual spend query added in M4.

### Budgeting
- **Purpose:** categories, monthly budgets, and the allocation Master Plan.
- **Owns:** `budgeting.categories` (self-referencing Budget→Category→Sub), `budget_periods`,
  `budget_lines`, `master_plan_sections`, `master_plan_items`.
- **Provides (contract):** `ICategoryDirectory` (validate a category's existence/active/flow for
  Ledger, BR-005/006).
- **Consumes (contract):** Ledger's `ICategorySpendQuery` to compute Budget *Actual* and *Leftover*
  (BR-062).
- **Source sheets:** `Budget`, `Master Plan`, `Category List`.
- **Status:** ✅ implemented (M4) — Category tree CRUD + system/child guards, monthly Budget
  (Plan stored; Actual/Leftover derived from the ledger), Master Plan (Investment/Needs/Wants 40/50/10
  + items with `Price × Frequency`), seeded taxonomy and sections.

### Reporting
- **Purpose:** dashboards and analytics — read-only projections.
- **Owns:** report read models / caches (rebuildable from Ledger events); owns no authoritative data.
- **Consumes:** Ledger + Accounts + Budgeting contracts, and Ledger integration events.
- **Outputs:** net worth, monthly cashflow, category breakdown (daily/monthly), yearly overview.
- **Source sheets:** `Overview (*)`, `Summary`, `Category Tracker (Daily)`, `Category Tracker (Monthly)`.

## Post-MVP modules (planned)

| Module | Purpose | Source sheets | Phase |
|---|---|---|---|
| **Goals** | Long-term goals + one-off project budgets | `Life Plan`, `Wedding Plan` | 2 |
| **Debts** | Liabilities: amount, paid, leftover, status | `Liabilities` | 2 |
| **Simulator** | Property / loan installment simulation | `Simulasi` | 2 |
| **Investments** | Holdings + trades with ID fees (Broker/Levy/PPN/PPh) | `RDN Ajaib`, `ASII`, `Trans. Hist.` | 3 |
| **Work** | Salary, leave, bonus, overtime, THR list | `Salary Slip`, `Leave Calc.`, `Bonuse Calc.`, `Overtime *`, `List of Expenses` | 4 |

## Dependency summary

```
Identity        → (none)
Accounts        → Ledger (contract)
Ledger          → Accounts (contract), Budgeting (contract)
Budgeting       → Ledger (contract)
Reporting       → Accounts, Ledger, Budgeting (contracts + events)
```

Cyclic contract references (Accounts↔Ledger, Ledger↔Budgeting) are resolved by keeping the shared
contracts in `Modules.Contracts` (interfaces only, no implementations), so no module project
references another module project directly.
