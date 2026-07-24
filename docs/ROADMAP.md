# Roadmap

Phased delivery. Each phase ends with tests and a CHANGELOG entry. Phases are ordered by value for a
personal finance workflow, not by workbook order.

## Phase 0 — Foundation (current)

- Documentation baseline (this set).
- Backend scaffold: solution, BuildingBlocks, module skeletons, EF Core, first migration, seed.
- Frontend scaffold: Vite + Vue 3 + TS + Tailwind, design tokens, app shell, login.
- Single-user auth (email + password, JWT + refresh).

## Phase 1 — MVP (Core money) 🎯

Maps to workbook: `Income`, `Expenses`, `Account Transfer`, `Account`, `Budget`, `Master Plan`,
`Category List`, `Overview (*)`, `Summary`, `Category Tracker (*)`.

- **Accounts** — accounts, account groups, derived balances (current + monthly).
- **Ledger** — Income / Expense / Transfer transactions; Cleared/Uncleared; description; import.
- **Categories** — Budget → Category → Sub taxonomy management.
- **Budget** — monthly Plan / Actual (derived) / Leftover.
- **Master Plan** — Investment / Needs / Wants items and 40/50/10 target split.
- **Dashboard / Reports** — net worth, monthly cashflow, category breakdown, yearly overview.
- **i18n** — EN + ID complete for all MVP screens.

## Phase 2 — Planning & Debts

Maps to workbook: `Life Plan`, `Wedding Plan`, `Liabilities`, `Simulasi`.

- **Goals & Projects** — long-term goals (target amount, timeline, saving/month) and one-off project
  budgets (e.g. a wedding) with their own expense list.
- **Debts** — liabilities with amount, paid, leftover, payment method, status.
- **Loan / Property Simulator** — property purchase and installment simulation.

## Phase 3 — Investments

Maps to workbook: `RDN Ajaib`, `ASII`, `Trans. Hist.`.

- **Portfolio** — holdings per instrument, lots, average cost.
- **Trades** — buy/sell with Indonesian fees (Broker Fee, Levy, PPN, PPh); realized/unrealized P/L,
  cash balance.

## Phase 4 — Work & Payroll helpers

Maps to workbook: `Salary Slip`, `Leave Calc.`, `Bonuse Calc.`, `Overtime *`, `List of Expenses`.

- **Salary & payslips**, **leave balance calc**, **bonus calc**, **overtime timesheets**, annual
  **THR / per-person expense list**.

## Phase 5 — Polish & intelligence

- Spreadsheet import/export round-trip, PDF report export.
- Recurring transactions, reminders.
- Light theme, PWA/mobile packaging (UI reference is mobile-first).
- Simple insights (trends, anomalies) — no heavy ML.

## Out of scope (for now)

Multi-user, bank/open-banking sync, double-entry accounting, invoicing, inventory, tax filing.
