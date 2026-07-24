# Tameru Changelog

This file is Tameru's immutable historical record. A task is not complete until this file has been
updated. Newest entries at the top. See `CLAUDE.md` → **CHANGELOG Rules** for the full procedure.

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
