# Tameru Changelog

This file is Tameru's immutable historical record. A task is not complete until this file has been
updated. Newest entries at the top. See `CLAUDE.md` → **CHANGELOG Rules** for the full procedure.

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
