# Status

> Snapshot of where Tameru is and what to do next. Update this whenever a milestone moves.

## Phase

**Core money.** M0–M4 complete (scaffold, Identity, Accounts, Ledger, Budgeting); M5 (Reporting)
next. The ledger is live end-to-end: transactions drive real account balances **and** budget actuals.
See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

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
- ✅ **M1** — Identity: `users` + `refresh_tokens`, PBKDF2 hashing, JWT access + rotating refresh,
  `/auth/login|refresh|logout|me`, owner seed, EF migration (snake_case), 21 unit tests. Verified
  end-to-end against the dev Postgres.
- ✅ **M2** — Accounts: `account_groups` + `accounts`, derived balances (`opening + net movement`
  via the `ILedgerAccountQuery` seam; NoOp until M3), CRUD + deactivate guard, group roll-ups,
  `/accounts` + `/account-groups`, seeded groups, EF migration, 13 unit + 2 architecture tests.
  Introduced `Modules.Contracts` (`IAccountDirectory`, `ILedgerAccountQuery`). Verified end-to-end.
- ✅ **M3** — Ledger: `ledger.transactions` (Income/Expense/Transfer), money rules (amount > 0,
  transfer distinct accounts, void = soft-delete), clear/unclear, filtered list; `BalanceCalculator`
  + real `ILedgerAccountQuery` (replaces the Accounts no-op) so balances derive live from the ledger;
  account validation via `IAccountDirectory`; `/api/v1/transactions`; 25 unit + 2 architecture tests.
  Verified end-to-end (income + transfer + expense → correct balances; void recomputes; in-use guard).
- ✅ **M4** — Budgeting: `categories` (Budget→Category→Sub tree) with system/child guards; monthly
  `budget_periods`/`budget_lines` (Plan stored, Actual/Leftover derived from the ledger via
  `ICategorySpendQuery`); Master Plan `sections`/`items` (40/50/10, `Price × Frequency`); seeded
  taxonomy + sections; `/categories`, `/budget-periods`, `/master-plan`. Also closed BR-005/006 —
  Ledger now validates category existence/flow via Budgeting's `ICategoryDirectory`. 18 unit + 2
  architecture tests. Verified end-to-end (budget actual = ledger spend; flow mismatch → 422).
- **M5 (active)** — Reporting.
- **M6** — Frontend scaffold + shell + login.
- **M7** — Frontend MVP screens.

## Deferred (post-MVP)

Goals & Projects (Life Plan, Wedding Plan), Debts (Liabilities), Loan/Property Simulator,
Investments (RDN Ajaib, ASII, Trans. Hist.), Work & Payroll helpers. See [ROADMAP.md](ROADMAP.md).

## Conventions reminder

Every task ends by updating [../CHANGELOG.md](../CHANGELOG.md) (`CHG-*`) and any doc the change
touches. Sole author: Yovan Alvianto — no AI attribution.
