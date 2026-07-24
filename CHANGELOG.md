# Tameru Changelog

This file is Tameru's immutable historical record. A task is not complete until this file has been
updated. Newest entries at the top. See `CLAUDE.md` → **CHANGELOG Rules** for the full procedure.

## [2026-07-24 16:22:00 UTC]

CHG-0010 — Fix: login email field vanished in production (vue-i18n '@' escaping)

- The login email placeholder messages (`you@example.com` / `anda@contoh.com`) contained a literal
  `@`, which vue-i18n parses as its linked-message syntax (`@:key`). In the stricter production i18n
  runtime this broke the email field's render, so only the password field showed. Escaped the `@` as
  `{'@'}` in both locales.
- Added a `LoginView` mount test asserting both the email and password fields render and that the
  placeholder resolves to `you@example.com` (regression guard). Frontend suite: 15 tests green.
- Rebuilt the Docker `web` image with the fix.

---

## [2026-07-24 16:12:09 UTC]

CHG-0009 — M6: Frontend scaffold + shell + login

- New `frontend/` project: Vite + Vue 3 (`<script setup>`) + TypeScript (strict) + Tailwind, Pinia,
  Vue Router, vue-i18n, axios, Lucide. Structure per docs/frontend/FRONTEND_ARCHITECTURE.md.
- Design system: `assets/styles/tokens.css` mirrors the LOCKED design language (dark canvas, single
  solid green `#35D07A`, semantic finance colors, category spectrum, radii) and Tailwind maps
  utilities to those CSS variables — no hardcoded hex, no gradients. Placeholder brand mark + lockup
  SVGs (accumulating bars).
- lib: `format.ts` (id-ID money/number, negatives in parentheses, signed money), `api.ts` (axios at
  `/api/v1`, bearer attach, `{success,data}` unwrap, one-shot 401→refresh rotation, typed
  `ApiClientError`), `session.ts` (token/user storage), and typed API modules `auth.ts`, `reports.ts`.
- State/i18n: Pinia `auth` (login/logout/refresh, persisted session), `ui` (locale + density toggle),
  `theme` (dark v1, ready for a light flip); structurally-identical EN + ID dictionaries; live
  language switching; enum labels (account type, transaction type/status) translated via maps.
- Routing: Vue Router with a tested auth guard (unauthenticated → `/login?redirect=`; authenticated
  away from `/login`).
- UI: responsive `AppShell` — desktop sidebar + top bar (language + sign-out), mobile rounded
  bottom-nav pill; UI kit `AppButton`, `AppCard`, `BalanceCard`, `StatTile`, `StatusChip`,
  `TransactionRow`, `SpendBar`, `Money`, `AvatarChip`, `AppInput`, `FormField`.
- Views: `LoginView` (owner sign-in, localized error mapping) and a live `DashboardView` that renders
  net worth + this month's cashflow from the M5 reports (loading / error+retry / empty states).
- Tooling: `vue-tsc` typecheck + `vite build` green; 14 Vitest tests (formatters + auth guard).
- Docker: added the `web` service (frontend `Dockerfile` multi-stage build → Nginx serving the SPA
  with history fallback and a same-origin `/api` proxy to the API) and `frontend/nginx.conf` +
  `.dockerignore`. `docker-compose.yml` now runs web → api → db; app at `:8091`.
- Verified end-to-end on Docker: SPA served (200), history deep-link fallback, owner login through the
  Nginx→API proxy succeeds, dashboard reads live figures.
- Docs: STATUS, IMPLEMENTATION_PLAN, `frontend/README.md`.

---

## [2026-07-24 15:53:15 UTC]

CHG-0008 — Local Docker stack (API + Postgres) for hands-on testing

- Added `docker-compose.yml`: builds the API image and runs it against a Postgres 16 container. The
  API reaches the DB over the internal compose network, so the database port is not published to the
  host (avoids clashing with a local Postgres on 5433). API is published on `${API_PORT:-8090}` and
  auto-migrates + seeds the owner on startup. The `web` (Nginx + SPA) service is deferred until the
  frontend lands (M6+).
- Fixed `Dockerfile.api`: it copied `Tameru.sln`, but the repo uses `Tameru.slnx` — the image build
  would have failed. Now copies `Tameru.slnx`.
- Added `.dockerignore` (excludes `bin`/`obj`, `.vs`, `tests`, `frontend/node_modules`, `.git`, …) so
  the image builds cleanly and reproducibly from a minimal context.
- Verified: `docker compose up -d --build` → `/health` 200; owner login; the M5 report endpoints
  respond (net worth, cashflow with a 12-point trend); seed taxonomy present.

---

## [2026-07-24 15:45:54 UTC]

CHG-0007 — M5: Reporting (dashboard analytics, compute-on-read)

- Added the Reporting module (Application / Infrastructure / Api). It owns **no data**: no tables, no
  `DbContext`, no migration — every figure is computed on read from other modules' contracts, so
  reports can never drift from the ledger, the single source of truth (ADR-0006). A materialized
  cache is deliberately deferred until data volume would justify it.
  - Application: `ReportingService` with four read use cases returning `Result`s —
    net worth (sum of derived balances over **active** accounts, BR-023, + per-account breakdown),
    monthly cashflow (income vs expense for a month + the 12-month trend), yearly overview (category ×
    month spend matrix), and a category-tracker pivot (daily/monthly over a date range). Query-param
    validation (`ReportingErrors`): month 1..12, known granularity, `from ≤ to`.
  - Infrastructure: `AddReportingModule` (registers the service; no persistence).
  - Api: `/api/v1/reports/{net-worth,cashflow,overview,category-tracker}`, owner-authorized.
- Cross-module contracts: added `IAccountBalanceDirectory` (provided by Accounts — per-account derived
  balances, reusing `AccountService` so the `opening + net movement` formula stays in one place) and
  `ILedgerReportingQuery` (provided by Ledger — monthly income/expense totals and expense totals per
  level-2 category per period bucket). Reporting consumes both; it references no other module directly.
- Bootstrapper: `AddReportingModule` + `MapReportingEndpoints` wired into the host.
- Tests: 10 Reporting unit tests (net-worth active-only, cashflow month + trend, overview/tracker
  pivots ordered by total, granularity/month/date-range validation) and 2 architecture-boundary rules
  (Reporting.Application depends on no Infrastructure/Web and on no other module's internals). Full
  suite green (99 tests).
- Verified end-to-end on Postgres: seeded owner + taxonomy; created an account (opening 5,000,000), a
  3,000,000 income and 500,000 (Jul) + 250,000 (Jun) expenses under Food → net worth 7,250,000; Jul
  cashflow net 2,500,000 with a correct 12-month trend; overview and monthly/daily tracker pivots
  aggregate correctly; invalid month / granularity / inverted range → 400, unauthenticated → 401. An
  Income tagged with an Expense-flow budget envelope is still rejected (`category_flow_mismatch`).
- Docs: MODULES (Reporting implemented; Accounts/Ledger provide the new contracts), ERROR_HANDLING
  (reporting param validation), STATUS, IMPLEMENTATION_PLAN.

---

## [2026-07-24 11:30:00 UTC]

CHG-0006 — M4: Budgeting (Categories, Budget, Master Plan)

- Added the Budgeting module (Domain / Application / Infrastructure / Api):
  - Domain: `Category` (self-referencing Budget→Category→Sub, flow, system flag, BR-040/041/005),
    `BudgetPeriod`/`BudgetLine` (BR-060/061), `MasterPlanSection`/`MasterPlanItem`
    (`TotalBudget = Price × Frequency`, target %, BR-080/081).
  - Application: `CategoryService` (tree CRUD, system + child guards), `BudgetService` (periods +
    lines; Actual/Leftover derived from the ledger via `ICategorySpendQuery`, BR-062),
    `MasterPlanService` (sections/items + roll-ups); `BudgetingErrors`.
  - Infrastructure: `BudgetingDbContext` (schema `budgeting`, snake_case) + configs, repositories,
    `CategoryDirectory` (provides `ICategoryDirectory`), starter-taxonomy + 40/50/10 section seeder,
    DI, design-time factory.
  - Api: `/api/v1/categories`, `/api/v1/budget-periods`, `/api/v1/master-plan`.
- Cross-module contracts: added `ICategoryDirectory` (provided by Budgeting) and `ICategorySpendQuery`
  (provided by Ledger).
- **Ledger extended:** implements `ICategorySpendQuery` (monthly expense totals per category) and now
  validates a transaction's category (exists / active / flow) via `ICategoryDirectory` — closing
  BR-005/006. A permissive no-op directory is used when Budgeting is absent; the bootstrapper
  registers Budgeting last so its real directory wins.
- Bootstrapper: `AddBudgetingModule`, plus Budgeting migrate + seed on startup and endpoint mapping.
- EF migration `Budgeting_Initial`; applied to the dev Postgres; taxonomy + sections seeded.
- Tests: 18 Budgeting unit tests (domain rules; category/budget/master-plan services incl.
  actual-from-ledger) and 2 architecture-boundary rules; updated Ledger tests for the new dependency.
  Full suite green (87 tests).
- Verified end-to-end on Docker: seeded categories + 40/50/10 sections; an Expense with an Income-flow
  category is rejected (`category_flow_mismatch`, 422); a budget's Actual (500,000) derives from a real
  ledger expense against Plan (770,000) → Leftover 270,000.
- Docs: MODULES (Budgeting + Ledger), ERROR_HANDLING (category codes), STATUS, IMPLEMENTATION_PLAN.

---

## [2026-07-24 11:00:00 UTC]

CHG-0005 — M3: Ledger (Income / Expense / Transfer — the cashflow core)

- Added the Ledger module (Domain / Application / Infrastructure / Api):
  - Domain: `Transaction` (single-entry Income/Expense/Transfer) with money rules — amount > 0
    (BR-001), transfer has a distinct target account (BR-002/003), title required; clear/unclear;
    void = soft-delete (BR-007). Pure `BalanceCalculator` (net movement / balance, `asOf` cutoff).
  - Application: `LedgerService` (create/update/clear/unclear/void/list with filters) returning
    `Result`s; validates referenced accounts via the Accounts `IAccountDirectory` contract;
    `LedgerErrors`.
  - Infrastructure: `LedgerDbContext` (schema `ledger`, snake_case), `Transaction` config + indexes,
    repository, and the real `LedgerAccountQuery` implementing `ILedgerAccountQuery` in SQL —
    replacing the Accounts no-op so balances derive live from the ledger (ADR-0006). DI + design-time
    factory.
  - Api: `/api/v1/transactions` (list/filter, get, create, update, clear, unclear, void).
- Bootstrapper: `AddLedgerModule` registered after Accounts (its `ILedgerAccountQuery` overrides the
  no-op); Ledger added to startup migrate and endpoint mapping.
- Fixed the `Result → HTTP` mapper: `account_in_use` (and `category_is_system`, `already_voided`)
  now map to 409 Conflict per ERROR_HANDLING.md.
- EF migration `Ledger_Initial`; applied to the dev Postgres.
- Tests: 25 Ledger unit tests (balance derivation priority + transaction rules + service) and 2
  Ledger architecture-boundary rules. Full suite green (67 tests).
- Verified end-to-end on Docker: income + transfer + expense produce correct account balances
  (A = 8,700,000; B = 7,150,000); void recomputes (B → 7,300,000); deactivating an in-use account is
  blocked (409); type/account/amount validation paths return the right envelopes.
- Docs: MODULES (Ledger status + contracts), ERROR_HANDLING (account_not_found), STATUS,
  IMPLEMENTATION_PLAN updated.

---

## [2026-07-24 10:30:00 UTC]

CHG-0004 — M2: Accounts (accounts, groups, derived balances)

- Added the Accounts module (Domain / Application / Infrastructure / Api):
  - Domain: `Account` (type, opening balance, currency, active, sort order) and `AccountGroup`;
    `BalanceWith(netMovement) = opening + movement` (ADR-0006, BR-022).
  - Application: `AccountService` (list/get/create/update, deactivate with in-use guard BR-021,
    group roll-ups) returning `Result`s; repository + unit-of-work ports; `AccountErrors`.
  - Infrastructure: `AccountsDbContext` (schema `accounts`, snake_case), EF configs, repositories,
    `AccountDirectory` (provides `IAccountDirectory`), `NoOpLedgerAccountQuery` (default
    `ILedgerAccountQuery` until Ledger ships), groups seeder, DI, design-time factory.
  - Api: `/api/v1/accounts` (list/get/create/update/deactivate) and `/api/v1/account-groups`
    (list/create/update), owner-authorized.
- Introduced `src/Modules.Contracts` for interface-only cross-module contracts:
  `IAccountDirectory` (provided by Accounts) and `ILedgerAccountQuery` (provided by Ledger later;
  NoOp for now), so no module references another module's projects directly.
- Bootstrapper: registered the module, mapped endpoints, added Accounts to startup migrate + seed.
- EF migration `Accounts_Initial`; applied to the dev Postgres; default account groups seeded.
- Tests: 13 Accounts unit tests (domain + service incl. balance derivation, deactivate guard,
  group roll-up) and 2 Accounts architecture-boundary rules. Full suite green (40 tests).
- Verified end-to-end on Docker: login → create/list accounts (balance = opening via NoOp ledger)
  → invalid type 400 → deactivate 200 → unauth 401.
- Docs: MODULES, STATUS, IMPLEMENTATION_PLAN updated.

---

## [2026-07-24 10:00:00 UTC]

CHG-0003 — M1: Identity (single-user auth)

- Added the Identity module (Domain / Application / Infrastructure / Api) for the single owner:
  - Domain: `User` (normalized email, locale en/id) and `RefreshToken` (hash-only, rotation-aware).
  - Application: `AuthService` (login, refresh-rotation, logout, get/update profile) returning
    `Result`s; ports `IPasswordHasher`, `ITokenService`, repositories, module unit of work.
  - Infrastructure: `IdentityDbContext` (schema `identity`), EF configs, repositories,
    PBKDF2 `PasswordHasherAdapter`, HMAC-SHA256 `JwtTokenService`, owner seeder, DI, design-time
    factory; snake_case columns via `EFCore.NamingConventions` (ADR-0007).
  - Api: `/api/v1/auth/login|refresh|logout|me` (minimal APIs) using the response envelope; a
    reusable `Result → HTTP` mapper in Web.Common.
- Bootstrapper: JWT bearer authentication, `HttpCurrentUser` (claims → audit), module registration,
  Swagger bearer button, and startup auto-migrate + seed.
- Initial EF migration `Identity_Initial`; applied to the dev Postgres. Verified end-to-end:
  login → `/me` → refresh rotation, plus 401/invalid-credentials paths.
- Tests: 21 Identity unit tests (domain + AuthService incl. rotation/reuse) and 2 new Identity
  architecture-boundary rules. Full suite green (25 tests).
- Dev infra: `docker-compose.dev.yml` now maps Postgres to host port 5433 (5432 was reserved);
  `appsettings.Development.json` wired for the dev DB, JWT, and owner seed.
- Docs: ADR-0007; DATABASE (naming convention), DEPLOYMENT (local run + dev creds), STATUS updated.

---

## [2026-07-24 09:30:00 UTC]

CHG-0002 — M0: solution & BuildingBlocks scaffold

- Created the .NET 8 solution (`Tameru.sln`), `Directory.Build.props` (net8.0, nullable on,
  warnings-as-errors) and `.editorconfig`.
- BuildingBlocks:
  - `Tameru.SharedKernel` — `Entity`/`AuditableEntity`, `ValueObject`, `Money` (IDR, numeric(19,2)),
    `Result`/`Error`, `PagedResult`, `IClock`/`SystemClock`, `DomainRuleException`, domain-event and
    audit/soft-delete markers.
  - `Tameru.Application.Abstractions` — `ICurrentUser`, `IUnitOfWork`, in-process messaging markers.
  - `Tameru.Infrastructure.Common` — `BaseDbContext` (audit stamping + physical-delete→soft-delete +
    soft-delete global query filter) with EF Core + Npgsql.
  - `Tameru.Web.Common` — `ApiResponse` envelope and `ExceptionHandlingMiddleware`
    (DomainRuleException→422, unhandled→500 with traceId; no leaks).
- `Bootstrapper/Tameru.Api` — minimal host with Swagger, CORS, DI for `IClock`/`ICurrentUser`,
  exception middleware, and a `/health` endpoint returning the standard envelope (verified 200).
- `tests/Tameru.ArchitectureTests` — NetArchTest fitness rules (SharedKernel and
  Application.Abstractions have no outward/infrastructure dependencies). `dotnet build` and
  `dotnet test` green (2 passed, 0 warnings).
- Infra: `docker-compose.dev.yml` (Postgres, loopback-only), `Dockerfile.api` (multi-stage),
  `.env.example`.
- Added [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md); updated docs index and STATUS.

---

## [2026-07-24 00:00:00 UTC]

CHG-0001 — Project documentation & design baseline

- Established Tameru as a single-user, single-entry personal finance manager derived from the
  `Financial Projection (Indonesia)` workbook, on AccounTrack's architecture and engineering
  discipline.
- Authored the initial documentation set: `CLAUDE.md`, `README.md`, and `docs/` (product,
  architecture, data model, API, rules, decisions, security, engineering, and frontend design).
- Locked the four founding decisions (single-user, single-entry cashflow, MVP scope, dark-first
  green-accent UI) as ADR-0001…ADR-0004 in [docs/DECISIONS.md](docs/DECISIONS.md).
- No application code yet — see [docs/STATUS.md](docs/STATUS.md) for what's next.
