# Decisions (ADRs)

Architectural Decision Records for Tameru. Newest at the top. Use
[adr/0000-template.md](adr/0000-template.md) for new records. Once **Accepted**, an ADR is immutable;
change direction with a new ADR that supersedes it.

---

## ADR-0007 — Single-user auth: JWT access + rotating refresh, PBKDF2 hashing, snake_case columns
**Status:** Accepted · 2026-07-24 (implemented in M1)

**Context.** The owner needs to log in and stay logged in without a heavy identity stack. We also need
a database naming convention that matches the documented snake_case schema.

**Decision.**
- **Passwords** are hashed with ASP.NET Core's `PasswordHasher` (PBKDF2) behind an `IPasswordHasher`
  port. Raw passwords are never stored or logged.
- **Access tokens** are short-lived HMAC-SHA256 JWTs (claims: `sub`, `email`, `name`, `locale`).
  Default inbound claim mapping is disabled so `sub` stays `sub`.
- **Refresh tokens** are opaque random values; only a SHA-256 hash is stored. Refreshing **rotates**:
  the presented token is revoked and a new pair issued. Reuse of a rotated token is rejected.
- **Column naming** uses `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention`) so EF columns are
  snake_case, matching [DATABASE.md](DATABASE.md). Each module context keeps its EF migrations-history
  table in its own schema.
- No roles/RBAC (ADR-0001). Every non-`/auth` endpoint just requires a valid owner token.

**Consequences.** Simple, secure-by-default auth. Access-token revocation is time-based (short expiry)
rather than a server-side denylist — acceptable for a single-user app. Rotating refresh tokens give
basic replay protection.

---

## ADR-0006 — Derived balances (ledger is the single source of truth)
**Status:** Accepted · 2026-07-24

**Context.** The workbook stores monthly balances per account, which drift from the transactions when
formulas break. We want one authoritative representation.

**Decision.** Account balances, net worth, budget actuals, and all report figures are **computed from
the transaction ledger**. No table stores a balance treated as truth. Reporting may cache read models
that are fully rebuildable from ledger events.

**Consequences.** No reconciliation bugs; editing a transaction is always safe. Requires efficient
aggregate queries and indexes; heavy dashboards may use cached projections.

---

## ADR-0005 — Modular monolith + Clean Architecture
**Status:** Accepted · 2026-07-24

**Context.** Single-user app, but we want testable domain logic and room to grow.

**Decision.** One deployable ASP.NET Core app composed of domain modules (Identity, Accounts, Ledger,
Budgeting, Reporting), each in Clean-Architecture layers, communicating via contracts/events, with
module boundaries enforced by architecture tests. Mirrors AccounTrack, minus microservice ambitions.

**Consequences.** Clear structure and isolation without distributed-system overhead. Slight ceremony
(four projects per module) accepted for testability and growth.

---

## ADR-0004 — UI: dark-first fintech, single solid green accent, no gradients
**Status:** Accepted · 2026-07-24

**Context.** The UI must feel fresh and distinct from AccounTrack's dense teal ERP look, and must not
read as "AI-generic." The product owner supplied a reference: a dark fintech mobile app with a solid
green accent.

**Decision.** Dark-first theme; a single **solid green** accent (`#35D07A` family) used sparingly for
primary actions and active nav; a separate semantic palette (green/red/amber) for finance meaning;
rounded cards; tabular figures; Lucide icons; ECharts with **solid** series fills. **No gradients**,
no emoji, no glassmorphism. Full spec in
[frontend/DESIGN_LANGUAGE.md](frontend/DESIGN_LANGUAGE.md).

**Consequences.** Distinct identity; gradient-free rule keeps it from looking templated. A light
theme is deferred.

---

## ADR-0003 — MVP scope
**Status:** Accepted · 2026-07-24

**Context.** The workbook has 35 sheets spanning core money, planning, investments, and payroll.

**Decision.** MVP delivers three groups: (1) Core ledger + Accounts, (2) Budget + Master Plan, (3)
Dashboards & Overview. Planning/Debts, Investments, and Work/Payroll are later phases (see
[ROADMAP.md](ROADMAP.md)).

**Consequences.** Fastest path to replacing daily workbook use; satellite sheets wait.

---

## ADR-0002 — Single-entry cashflow (no double-entry)
**Status:** Accepted · 2026-07-24

**Context.** AccounTrack uses strict double-entry. The workbook — and personal finance generally —
uses simple cashflow: money in, out, or transferred.

**Decision.** Tameru's core is **single-entry**. A `Transaction` is Income, Expense, or Transfer;
balances derive from these. No journals, no debits/credits, no chart of accounts.

**Consequences.** Matches the owner's mental model and the source data; far less machinery. Trade-off:
no formal accounting reports (P&L/Balance Sheet) — acceptable for a personal tool.

---

## ADR-0001 — Single-user (no multi-tenancy / multi-company / RBAC)
**Status:** Accepted · 2026-07-24

**Context.** AccounTrack is multi-tenant, multi-company, with RBAC and segregation of duties. Tameru
serves one person.

**Decision.** Single owner account. Drop `TenantId`/`CompanyId`, roles, approvals, and SoD. Keep the
Clean-Architecture structure, audit fields, soft delete, and engineering discipline. A side-business
("Shirt Lab") is modeled as ordinary accounts/categories, not a separate company.

**Consequences.** Dramatically simpler auth and data model; no tenant query filters. If multi-user is
ever needed, it becomes a future ADR (non-trivial migration).

---

_Earlier template:_ [adr/0000-template.md](adr/0000-template.md).
