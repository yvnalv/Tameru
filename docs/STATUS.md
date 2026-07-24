# Status

> Snapshot of where Tameru is and what to do next. Update this whenever a milestone moves.

## Phase

**Architecture & design.** Documentation baseline complete; no application code yet.

## Done

- ✅ Founding decisions locked (ADR-0001…ADR-0004): single-user, single-entry cashflow, MVP scope,
  dark-first green-accent UI.
- ✅ Source workbook analyzed; 35 sheets mapped to modules/tables
  ([DATA_MODEL_FROM_EXCEL.md](DATA_MODEL_FROM_EXCEL.md)).
- ✅ Documentation set authored (`CLAUDE.md`, `README.md`, `docs/`).

## Next (in order)

1. **Backend scaffold** — solution, `BuildingBlocks`, module skeletons, EF Core `DbContext`,
   first migration, seed data (accounts, category taxonomy, master-plan sections).
2. **Accounts module** — CRUD + derived balances + tests.
3. **Ledger module** — Income / Expense / Transfer transactions + balance derivation + tests.
4. **Budgeting module** — Categories, Budget periods (Plan/Actual/Leftover), Master Plan + tests.
5. **Reporting module** — Overview, Summary, Category trackers (read models) + tests.
6. **Frontend scaffold** — Vite + Vue 3 + design tokens + app shell (dark, green accent) + login.
7. **Frontend MVP screens** — Dashboard, Transactions, Accounts, Budget, Master Plan, Categories.

## Deferred (post-MVP)

Goals & Projects (Life Plan, Wedding Plan), Debts (Liabilities), Loan/Property Simulator,
Investments (RDN Ajaib, ASII, Trans. Hist.), Work & Payroll helpers. See [ROADMAP.md](ROADMAP.md).

## Conventions reminder

Every task ends by updating [../CHANGELOG.md](../CHANGELOG.md) (`CHG-*`) and any doc the change
touches. Sole author: Yovan Alvianto — no AI attribution.
