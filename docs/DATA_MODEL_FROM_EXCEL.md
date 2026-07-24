# Data Model — from the Excel workbook

This is Tameru's Rosetta stone: how the source workbook `Financial Projection (Indonesia)` maps to
the application's modules, menus, and tables. The workbook has **35 sheets**. This document records
what each sheet is, its columns, and where it goes in Tameru.

> Money in the workbook is IDR with occasional fractional values (bank interest). Dates appear both
> as a single `Date` and as split `Y / M / D` columns — in Tameru we store one `Date` and derive
> year/month/day. "Status" is `Cleared` / `Uncleared` (a few typos like "Uncleard" exist in the
> source and are normalized on import).

## Sheet → module map (all 35)

| # | Sheet | Meaning | Tameru target | Phase |
|---|---|---|---|---|
| 1 | Overtime 2019 | Daily overtime timesheet (per-day grid, working days/holidays) | Work → Overtime | 4 |
| 2 | List of Expenses | Annual THR / per-person yearly amounts (yvnalv, Wife, Ibu, …) | Work → Annual (THR) | 4 |
| 3 | Master Plan | Allocation planner: Investment / Needs / Wants, Item·Price·Freq·Total | Budgeting → Master Plan | 1 |
| 4 | Account | Accounts with monthly balances + account group tags | Accounts | 1 |
| 5 | Budget | Monthly budget per category: Plan / Act / Leftover | Budgeting → Budget | 1 |
| 6 | Account Transfer | Transfer transactions (From/To account) | Ledger (Transfer) | 1 |
| 7 | Income | Income transactions | Ledger (Income) | 1 |
| 8 | Expenses | Expense transactions (Budget→Category→Sub) | Ledger (Expense) | 1 |
| 9–18 | Overview (2019…2026), (Sims), (2020 wo Bonus) | Per-year overview: income/expense by category × month | Reporting → Overview | 1 |
| 19 | Summary | Daily income/expense roll-up (Plan/Act) | Reporting → Summary | 1 |
| 20 | Category Tracker (Daily) | Pivot: amount by category × day | Reporting → Trackers | 1 |
| 21 | Category Tracker (Monthly) | Pivot: amount by category × month | Reporting → Trackers | 1 |
| 22 | Life Plan | Long-term goals: transaction, amount, years, saving/month, term | Goals | 2 |
| 23 | Overtime 2022 | Overtime timesheet | Work → Overtime | 4 |
| 24 | Overtime 2024 | Overtime timesheet | Work → Overtime | 4 |
| 25 | RDN Ajaib | Stock trades (buy/sell, fees, profit, cash balance) — Ajaib broker | Investments | 3 |
| 26 | ASII | Per-day price grid for a holding (Astra) | Investments | 3 |
| 27 | Liabilities | Debts: amount, paid, leftover, plan, method, status | Debts | 2 |
| 28 | Salary Slip | Monthly payslip breakdown (earnings/deductions) | Work → Payslip | 4 |
| 29 | Leave Calc. | Leave/severance calc (upah, sisa cuti) | Work → Leave | 4 |
| 30 | Expenses (Sample) | Sample/template of the Expenses ledger | Ledger (import template) | 1 |
| 31 | Trans. Hist. | Trade history with fee constants (Broker/Levy/PPN/PPh) | Investments | 3 |
| 32 | Wedding Plan | One-off project budget: expense list for an event | Goals → Projects | 2 |
| 33 | Simulasi | Property/loan purchase simulation | Simulator | 2 |
| 34 | Sheet1 | Scratch/working data | (not migrated) | — |
| 35 | Bonuse Calc. | Annual bonus calculation (achievement vs BP/LY ratios) | Work → Bonus | 4 |

## The core transaction ledgers (MVP)

The three ledgers share the same shape and become one `Transactions` table discriminated by `Type`.

### Income (sheet 7)
Columns: `No, Date, Y, M, D, Transaction, Amount, Account, Category, Status, Description`.
- `Account` = destination account. `Category` = e.g. Income / Interests / Adjustment / Dividen.
- Amounts may be negative (e.g. interest reversal).

### Expenses (sheet 8)
Columns: `No, Date, Y, M, D, Transaction, Amount, Account, Budget, Category, Sub, Status, Description`.
- `Account` = source account. Three-level classification: `Budget` (Needs/Want/Investment) →
  `Category` (Wife, Internet, Personal, …) → `Sub` (Household, Internet, Transportation, …).

### Account Transfer (sheet 6)
Columns: `No, Date, Y, M, D, Transaction, Amount, From, To, Status, Description`.
- Moves `Amount` from `From` account to `To` account. Also carries a small FX side-table
  (SGD/JPY/USD rates) — reserved for future multi-currency, not in MVP.

### Mapping to `Transactions`

| Ledger field | Column source |
|---|---|
| `Type` | Income / Expense / Transfer (by sheet) |
| `Date` | `Date` (Y/M/D derived) |
| `Title` | `Transaction` |
| `Amount` | `Amount` (numeric(19,2)) |
| `AccountId` | `Account` (Income=destination, Expense=source, Transfer=From) |
| `ToAccountId` | `To` (transfers only) |
| `BudgetCategoryId` | `Budget` (expenses) |
| `CategoryId` | `Category` |
| `SubCategoryId` | `Sub` (expenses) |
| `Status` | `Status` → Cleared / Uncleared |
| `Description` | `Description` |

## Accounts (sheet 4)

- Row per account: `Id, Account Name, Jan…Dec` monthly balances, plus a side column of **account
  group** tags with roll-up totals: *Saving, Investment, Family, Subscription, Transportation, Eats,
  blu Account, Shirt Lab, Body Care, Internet, Personal*.
- Example accounts: Cash (yvnalv), Cash (Shirt Lab), BSI (Auto-debet), BSI (Blokir), BCA,
  BCA (Shirt Lab), BCA (RDN Saham), blubyBCA, BTN, Octo+, Gopay.
- Tameru: `Accounts` (name, group, type, opening balance, currency, active, order). Monthly balances
  are **derived** from the ledger, not stored.

## Categories (Category List, Budget)

- The **Category List** column (present on the ledger sheets) is the master list used for data
  validation dropdowns.
- **Budget** sheet defines top envelopes and the Plan/Actual/Leftover mechanics.
- Tameru models one self-referencing `Categories` table:
  - Level `Budget`: Investment, Needs, Wants, Income (and Adjustment/Interests/Transfer as system).
  - Level `Category`: Wife, Internet, Food, Transportation, Personal, Education, Body Care, …
  - Level `Sub`: Household, Fuel, Breakfast, Internet, Entertainment, …

## Budget (sheet 5)

- Columns: `Id, Category, Account Name, Balance(Plan / Act / Leftover / Budget Review), Jan…Dec`.
- Tameru: `BudgetPeriods` (year, month) + `BudgetLines` (categoryId, planAmount). *Actual* and
  *Leftover* are derived from the ledger per category per period.

## Master Plan (sheet 3)

- Three sections **Investment / Needs / Wants**, each with sub-blocks (Saving, Gold, Food,
  Transportation, Personal, Debt, …). Each item: `Item, Price, Freq., Total Budget (= Price × Freq)`.
- Header ratios seen in `Overview`: **Investment 40% / Needs 50% / Wants 10%**.
- Tameru: `MasterPlanSections` (Investment/Needs/Wants + target %) → `MasterPlanItems`
  (name, price, frequency, total = price × frequency).

## Reporting sheets (Overview / Summary / Trackers)

- `Overview (YYYY)`: matrix of income & expense lines (Basic Salary, Food Allowances, …) by month,
  with the 40/50/10 split header. → derived Reporting read model.
- `Summary`: per-day income and expense, Plan vs Act. → derived.
- `Category Tracker (Daily/Monthly)`: pivot of amount by category. → derived.

## Satellite sheets (later phases) — field notes

- **Liabilities** (27): `No, Date, Transaction, Amount, Paid, Leftover, Plan, Payment Method, Status,
  Description`; totals for Total Liabilities / Leftover.
- **Life Plan** (22): `No, Date, Transaction, Amount, Year, Saving/month, Plan(Short/Long term),
  Payment Method, Status, Description`.
- **Wedding Plan** (32): `No, Date, Transaction, Amount, Account, Budget, Category, Sub, Status,
  Description` — a scoped expense list (a "project").
- **Simulasi** (33): property purchase inputs (installments, land/building area, price/m², AJB cost,
  remaining loan) → a calculator.
- **RDN Ajaib / Trans. Hist.** (25/31): buy & sell blocks with fee constants — Broker Fee 0.08%,
  Levy 0.04%, PPN 0.03% (buy) / + PPh 0.1% (sell) — plus `Shares, Price, Lot, Sub Total, + Fees,
  Adj., Profit, Cash Balance`.
- **ASII** (26): per-day price grid for one instrument (month × day).
- **Salary Slip** (28): earnings (Gaji Pokok, tunjangan, bonus) vs deductions (JHT/JP, iuran, potong).
- **Leave Calc.** (29): `Upah, Sisa Cuti` → severance/leave value.
- **Bonuse Calc.** (35): achievement vs Business Plan / Last Year ratios × porsi → bonus index.
- **Overtime 20xx** (1/23/24): per-day hour grid with working-day/holiday classification.
- **List of Expenses** (2): annual THR per person (yvnalv, Wife, Ibu, Yona, Yohan, Mama, Saving) by
  year.

## Import strategy

MVP ships a per-entity spreadsheet/CSV import (accounts, categories, the three ledgers) so historical
workbook data can be loaded and verified against derived balances (a validated dry-run → commit
flow). Normalization handled on import: status typos, split Y/M/D → single date, blank sub-levels.
