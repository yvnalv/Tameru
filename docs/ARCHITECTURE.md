# Architecture

Tameru is a **modular monolith** built with **Clean Architecture**. It reuses AccounTrack's structure
and discipline, simplified for a single-user personal-finance app (no multi-tenancy, no double-entry,
no inventory, no approval workflow).

## Goals

- One deployable unit, clear internal module boundaries.
- Domain logic isolated and testable, independent of frameworks and the database.
- The transaction ledger is the single source of truth; everything else derives from it.
- Easy to grow (add modules) without a rewrite.

## Solution layout

```
src/
├── Bootstrapper/
│   └── Tameru.Api                     Composition root: DI, auth, middleware, controllers host,
│                                      Swagger, migration/seed on startup (dev).
├── BuildingBlocks/
│   ├── Tameru.SharedKernel            Entity base, value objects (Money, CurrencyCode), Result,
│   │                                  audit fields, domain-event base, guard clauses.
│   ├── Tameru.Application.Abstractions Ports: IClock, ICurrentUser, IUnitOfWork, mediator markers.
│   ├── Tameru.Infrastructure.Common   EF Core base DbContext behaviors (audit, soft-delete filter),
│   │                                  JWT, password hashing, outbox (if needed).
│   └── Tameru.Web.Common              Response envelope, exception→ProblemDetails mapping, filters.
├── Modules/
│   ├── Identity/
│   ├── Accounts/
│   ├── Ledger/
│   ├── Budgeting/
│   └── Reporting/
├── Modules.Contracts/                 Public cross-module contracts + integration-event definitions.
tests/
├── Tameru.Accounts.UnitTests
├── Tameru.Ledger.UnitTests
├── Tameru.Budgeting.UnitTests
├── Tameru.Reporting.UnitTests
├── Tameru.Identity.UnitTests
├── Tameru.IntegrationTests
└── Tameru.ArchitectureTests
frontend/                              Vue 3 app (sibling to src/).
```

## Per-module layers (Clean Architecture)

Each module is four projects, e.g. for `Ledger`:

```
Modules/Ledger/
├── Tameru.Ledger.Domain           Entities (Transaction), value objects, domain rules, events.
├── Tameru.Ledger.Application      Use cases (commands/queries), validators, DTOs, contracts.
├── Tameru.Ledger.Infrastructure   EF Core DbContext, entity configs, repositories, migrations.
└── Tameru.Ledger.Api              Minimal API/controllers, request/response models, DI wiring.
```

Dependency direction (inward only):

```
Api ──▶ Application ──▶ Domain
Infrastructure ──▶ Application ──▶ Domain
```

- **Domain** depends on nothing outward (pure C#).
- **Application** depends on Domain and abstractions only.
- **Infrastructure** implements Application ports; depends on Domain + Application.
- **Api** wires everything and exposes HTTP.

## Module boundaries

- A module MUST NOT reference another module's Domain/Infrastructure/tables directly.
- Cross-module interaction goes through:
  - **Application-service contracts** in `Modules.Contracts` (synchronous, in-process), or
  - **Integration events** published via an in-process mediator (asynchronous within the process).
- Each module owns its own EF Core schema / table prefix, keeping data ownership explicit.
- Boundaries are enforced by `Tameru.ArchitectureTests` (NetArchTest) in CI.

### Example cross-module flows

- **Reporting** needs balances and spending: it reads **Ledger** and **Accounts** through their
  query contracts, or maintains its own read models updated from `TransactionPosted` /
  `TransactionVoided` integration events. It never queries Ledger tables directly.
- **Budgeting** computes *Actual* by asking **Ledger** for the sum of expenses per category per
  period through a contract — not by joining Ledger tables.

## Request pipeline

```
HTTP → Api endpoint → Application handler (validation → domain → persistence via UnitOfWork)
     → domain events → integration events (mediator) → response envelope
```

- Validation: FluentValidation (or DataAnnotations) at the Application boundary.
- Cross-cutting: logging, exception mapping, and audit-field stamping via pipeline behaviors and the
  base DbContext.
- Consistency: a single use case commits in one transaction (`IUnitOfWork`). Effects that can be
  eventually consistent (e.g. refreshing a report read model) run off integration events.

## Source of truth

- **Ledger** owns `Transactions`. Account balances, budget actuals, and all report figures are
  **computed** from transactions (optionally cached as read models). No table stores a balance that
  is treated as authoritative.

## Data & persistence

- PostgreSQL via Npgsql + EF Core. GUID keys. Soft-delete global query filter. Audit fields stamped
  automatically. Migrations are per-module (each module's Infrastructure owns its migrations). See
  [DATABASE.md](DATABASE.md).

## Configuration & secrets

- `appsettings.json` + environment overrides + environment variables. No secrets in source. See
  [DEPLOYMENT.md](DEPLOYMENT.md) and [SECURITY.md](SECURITY.md).

## Why this shape (see DECISIONS.md)

- **Modular monolith** — one user, one deployable; modules give structure without distributed-system
  cost (ADR-0005).
- **Single-entry** — matches the workbook and personal mental model (ADR-0002).
- **Derived balances** — one source of truth eliminates reconciliation bugs (ADR-0006).
