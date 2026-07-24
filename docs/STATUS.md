# Status

> Snapshot of where Tameru is and what to do next. Update this whenever a milestone moves.

## Phase

**Foundation.** M0 (solution + BuildingBlocks scaffold) complete; M1 (Identity) next. See the
milestone plan in [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

## Done

- ✅ Founding decisions locked (ADR-0001…ADR-0006): single-user, single-entry cashflow, MVP scope,
  dark-first green-accent UI, modular monolith, derived balances.
- ✅ Source workbook analyzed; 35 sheets mapped to modules/tables
  ([DATA_MODEL_FROM_EXCEL.md](DATA_MODEL_FROM_EXCEL.md)).
- ✅ Documentation set authored (`CLAUDE.md`, `README.md`, `docs/`).
- ✅ Milestone build plan authored ([IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md)).

## Next (milestones)

- ✅ **M0** — Solution + BuildingBlocks scaffold, `Tameru.Api` host, `/health`, architecture test,
  docker-compose. `dotnet build`/`test` green.
- **M1 (active)** — Identity (single-user auth).
- **M2** — Accounts.
- **M3** — Ledger (the core: Income/Expense/Transfer + derived balances).
- **M4** — Budgeting (Categories, Budget, Master Plan).
- **M5** — Reporting.
- **M6** — Frontend scaffold + shell + login.
- **M7** — Frontend MVP screens.

## Deferred (post-MVP)

Goals & Projects (Life Plan, Wedding Plan), Debts (Liabilities), Loan/Property Simulator,
Investments (RDN Ajaib, ASII, Trans. Hist.), Work & Payroll helpers. See [ROADMAP.md](ROADMAP.md).

## Conventions reminder

Every task ends by updating [../CHANGELOG.md](../CHANGELOG.md) (`CHG-*`) and any doc the change
touches. Sole author: Yovan Alvianto — no AI attribution.
