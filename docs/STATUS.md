# Status

> Snapshot of where Tameru is and what to do next. Update this whenever a milestone moves.

## Phase

**MVP feature-complete.** M0–M5 (backend) and M6–M7 (frontend) complete. A real Vue client signs in
and drives the whole workbook menu — Dashboard, Transactions, Accounts, Budget, Master Plan,
Categories — against the live API. M8 (import, i18n audit & polish) is next.
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
- ✅ **M5** — Reporting: read-only dashboard analytics computed on read from the Accounts + Ledger
  contracts (no tables, no cache — the ledger is the source of truth). Net worth (active accounts,
  BR-023), monthly cashflow + 12-month trend, yearly category × month overview, and a daily/monthly
  category-tracker pivot; `/api/v1/reports/{net-worth,cashflow,overview,category-tracker}`. Added the
  `IAccountBalanceDirectory` (Accounts) and `ILedgerReportingQuery` (Ledger) contracts. 10 unit + 2
  architecture tests. Verified end-to-end (net worth 7,250,000; Jul cashflow net 2,500,000; overview
  and tracker pivots correct; validation/auth paths return the right envelopes).
- ✅ **M6** — Frontend scaffold + shell + login: Vite + Vue 3 + TS + Tailwind, design tokens
  (dark, `#35D07A`, no gradients), id-ID formatters, axios envelope client with 401→refresh, Pinia
  (auth/theme/ui), vue-i18n EN+ID; responsive `AppShell` (desktop sidebar + topbar, mobile bottom-nav
  pill), Login, and a live Dashboard (net worth + monthly cashflow from the M5 reports); UI kit
  (Button/Card/BalanceCard/StatTile/StatusChip/TransactionRow/SpendBar/Money/…). 14 Vitest tests
  (formatters + auth guard); `vue-tsc` typecheck + build green. Added a Docker `web` service (Nginx +
  SPA) — full app runs on Docker at `:8091`, Nginx proxying `/api` to the API. Verified end-to-end.
- ✅ **M7** — Frontend MVP screens complete: Dashboard, **Accounts**, **Transactions**,
  **Categories** (Budget→Category→Sub tree + CRUD), **Budget** (month picker; Plan editable,
  Actual/Leftover derived from the ledger), and **Master Plan** (Investment/Needs/Wants 40/50/10;
  items `Price × Frequency`). Typed `lib/*` per module, `AppSelect`/`AppModal`, bilingual EN/ID,
  seeded-name localization. `vue-tsc` + build + 15 Vitest tests green. Verified end-to-end on Docker
  with seeded data (budget Actual derives live from the ledger).
- **M8 (active)** — Analytics & polish. Done: a **Reports** screen (M5 overview heatmap +
  category-tracker pivot); an **i18n EN/ID parity test**; a **density toggle**; **CSV export** of
  transactions; and **CSV import** (client-side upload → preview/validate → per-row create via the
  existing endpoints, with a result report) for **accounts** and **transactions** (name→id resolution,
  downloadable templates). Remaining (optional): PDF export, categories import.
- **M9 (active)** — UI/UX hardening (agreed multi-phase pass). Done: **P1** short month labels
  (`Jan/Feb/Mar`), tooltip'd `IconButton` on every icon-only action, a **collapsible sidebar** (72px
  icon-only rail; toggle now fixed in the top bar so it never moves), scrollable tables; **P2
  responsive/mobile** pass (headers wrap, mobile-friendly transaction rows, stacked totals, fluid
  card numbers — targets iPhone 8 / Android ~375px); **P3** content area fills the screen width (no
  max-width cap); **P4** Reports merged into one card with a `Yearly | Monthly | Daily` toggle (last
  5 yrs / 12 months / days-of-month with prev-next; monthly also has year prev/next); **P5** Budget
  now shows per-category **progress bars** (green fill to Plan, red overspend beyond, % + leftover);
  **P6** CSV **import on every relevant screen** — added Categories and Master Plan importers
  alongside Accounts/Transactions. Next: richer **ECharts** dashboard, then cross-cutting polish
  (toasts, skeletons, self-hosted Inter).

## Deferred (post-MVP)

Goals & Projects (Life Plan, Wedding Plan), Debts (Liabilities), Loan/Property Simulator,
Investments (RDN Ajaib, ASII, Trans. Hist.), Work & Payroll helpers. See [ROADMAP.md](ROADMAP.md).

## Conventions reminder

Every task ends by updating [../CHANGELOG.md](../CHANGELOG.md) (`CHG-*`) and any doc the change
touches. Sole author: Yovan Alvianto — no AI attribution.
