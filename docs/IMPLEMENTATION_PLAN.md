# Implementation Plan

The ordered, buildable breakdown that turns the docs into working software. Each milestone is a
self-contained, reviewable slice that ends with green tests and a `CHANGELOG.md` entry (`CHG-*`).
See [ROADMAP.md](ROADMAP.md) for the product phasing and [ARCHITECTURE.md](ARCHITECTURE.md) for the
structure this fills in.

## Conventions per milestone

- Own branch off `main` (`feat/…`); PR when green.
- Backend: xUnit unit tests mandatory for money-critical logic; NetArchTest boundary checks.
- Docs touched are updated in the **same** change; `CHANGELOG.md` gets the next `CHG-*` last.
- Sole author Yovan Alvianto; no AI attribution.

## M0 — Solution & BuildingBlocks scaffold (`CHG-0002`)

Compilable skeleton, no features.

- `Tameru.sln`, `Directory.Build.props` (net8.0, nullable on, warnings-as-errors), `.editorconfig`.
- `BuildingBlocks/`:
  - `Tameru.SharedKernel` — `Entity`, `AuditableEntity`, `Money`, `CurrencyCode`, `Result`/`Error`,
    `ValueObject`, `IClock`, `DomainRuleException(code)`, domain-event markers.
  - `Tameru.Application.Abstractions` — `ICurrentUser`, `IUnitOfWork`, mediator markers.
  - `Tameru.Infrastructure.Common` — `BaseDbContext` (audit stamping + soft-delete filter),
    `SystemClock`.
  - `Tameru.Web.Common` — `ApiResponse` envelope, exception→envelope middleware.
- `Bootstrapper/Tameru.Api` — minimal host, Swagger, `/health`, config binding.
- `tests/Tameru.ArchitectureTests` — first fitness rule (Domain depends on nothing outward).
- Infra: `docker-compose.dev.yml` (Postgres), `Dockerfile.api`, `.env.example`.
- **DoD:** `dotnet build` + `dotnet test` green; API boots; `/health` returns 200.

## M1 — Identity (`CHG-0003`)

- `identity.users`, `identity.refresh_tokens`; password hashing; JWT access + rotating refresh.
- Endpoints: `/auth/login`, `/refresh`, `/logout`, `GET/PATCH /auth/me`. Seed owner user.
- **Tests:** login success/failure, refresh rotation, hash verify; integration login→me.

## M2 — Accounts (`CHG-0004`)

- `accounts.account_groups`, `accounts.accounts`; CRUD + deactivate guard (BR-021); seed groups.
- `IAccountBalanceQuery` (opening balance until Ledger lands).
- **Tests:** CRUD, deactivate-in-use guard, group rollups.

## M3 — Ledger — the core (`CHG-0005`)

- `ledger.transactions` (Income/Expense/Transfer) + constraints (BR-001…009).
- `Transaction` factories, `BalanceCalculator`, `ITransactionQuery`, `IBalanceProjection`;
  publish `TransactionPosted/Updated/Voided`. Endpoints incl. clear/unclear/void/import.
- Wire Accounts balances to real ledger sums.
- **Tests (highest priority):** balance derivation (opening/income/expense/transfer/void/date-cutoff),
  transfer integrity, amount > 0, category-flow match, net worth; integration transfer→balances.

## M4 — Budgeting (`CHG-0006`)

- `categories` (self-ref Budget→Category→Sub), `budget_periods`, `budget_lines`,
  `master_plan_sections`, `master_plan_items`. `ICategoryQuery`; Actual/Leftover via `ITransactionQuery`;
  Master Plan totals + 40/50/10 targets; seed taxonomy + sections.
- **Tests:** tree rules, system-category delete guard, actual/leftover, master-plan totals.

## M5 — Reporting (`CHG-0007`)

- Read models (rebuildable from Ledger events): net worth, monthly cashflow, category tracker
  (daily/monthly), yearly overview. Endpoints `/reports/*`.
- **Tests:** net worth over active accounts, cashflow aggregation, tracker pivots.

## M6 — Frontend scaffold + shell + login (`CHG-0008`)

- Vite + Vue 3 + TS + Tailwind; `tokens.css` (dark, `#35D07A`, no gradients); `format.ts` (id-ID);
  axios envelope client; Pinia auth/theme/ui; vue-i18n `en`+`id`.
- `AppShell` (desktop sidebar + topbar; mobile bottom-nav pill), Login, Dashboard placeholder,
  UI kit (Button/Card/BalanceCard/StatTile/StatusChip/TransactionRow/SpendBar/Money).
- **Tests (Vitest):** `formatMoney`, auth guard.

## M7 — Frontend MVP screens (`CHG-0009…`)

- Dashboard → Transactions (all 3 types) → Accounts → Budget → Master Plan → Categories.
- Every screen fully EN + ID; wired to typed `lib/*` API modules.

## M8 — Import, i18n audit & polish (`CHG-00xx`)

- Spreadsheet/CSV import (accounts/categories/ledgers) validated against derived balances vs the
  workbook; i18n completeness check; density toggle; empty/error/loading states; CSV/PDF export.

## Cross-module contracts (introduced in M2)

`src/Modules.Contracts` holds interface-only cross-module contracts so no module references another
module's projects directly:
- `IAccountDirectory` — provided by Accounts (exists/active, currency) for Ledger to validate.
- `ILedgerAccountQuery` — provided by Ledger (net movement per account, has-transactions) for
  Accounts to derive balances and guard deactivation. A `NoOp` default is used until M3; Ledger
  replaces it via DI.

## Current position

See [STATUS.md](STATUS.md). Completed: **M0, M1, M2, M3, M4**. Active milestone: **M5 — Reporting**.