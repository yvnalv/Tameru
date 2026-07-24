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
- **Owns:** `Accounts`, `AccountGroups`.
- **Provides (contract):** `IAccountBalanceQuery` (current + monthly balance for an account, computed
  from Ledger), account existence/active checks.
- **Depends on:** Ledger (via contract) for balance computation; BuildingBlocks.
- **Source sheets:** `Account`.

### Ledger
- **Purpose:** the cashflow core. Income / Expense / Transfer transactions.
- **Owns:** `Transactions`.
- **Provides (contract):** `ITransactionQuery` (sums per account / per category / per period),
  `IBalanceProjection`. Publishes `TransactionPosted`, `TransactionUpdated`, `TransactionVoided`.
- **Consumes:** Accounts (validate account exists/active), Budgeting (validate category exists).
- **Rules:** transfer must have distinct `AccountId` and `ToAccountId`; amount > 0; a posted
  transaction is editable while Draft/Uncleared, and voided rather than hard-deleted. See
  [BUSINESS_RULES.md](BUSINESS_RULES.md).
- **Source sheets:** `Income`, `Expenses`, `Account Transfer`, `Expenses (Sample)`.

### Budgeting
- **Purpose:** categories, monthly budgets, and the allocation Master Plan.
- **Owns:** `Categories` (self-referencing Budget→Category→Sub), `BudgetPeriods`, `BudgetLines`,
  `MasterPlanSections`, `MasterPlanItems`, `AllocationSettings` (40/50/10 targets).
- **Provides (contract):** `ICategoryQuery` (validate/lookup categories for Ledger).
- **Consumes:** Ledger (via `ITransactionQuery`) to compute Budget *Actual* and *Leftover*.
- **Source sheets:** `Budget`, `Master Plan`, `Category List`.

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
