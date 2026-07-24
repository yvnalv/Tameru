# CLAUDE.md

# Tameru — Master Project Instructions

## Purpose

This file is the single source of truth for AI coding assistants, the author, and any future
contributor working on Tameru.

All design, implementation, architectural, and product decisions must align with this document. If
any implementation conflicts with this document, this document takes precedence unless explicitly
superseded by an approved decision recorded in [docs/DECISIONS.md](docs/DECISIONS.md).

---

# Project Overview

## Project Name

Tameru (溜める — "to save up / accumulate").

## Project Type

Personal finance manager (single-user).

## Origin

Tameru is the application successor to a long-lived Excel workbook,
`Financial Projection (Indonesia)`, which tracked income, expenses, transfers, account balances,
budgets, and long-term plans across many years. The workbook's 35 sheets are the functional
reference for the product; the mapping from sheet → module/table is documented in
[docs/DATA_MODEL_FROM_EXCEL.md](docs/DATA_MODEL_FROM_EXCEL.md).

## Engineering Reference

Tameru reuses the **architecture, tech stack, and engineering discipline** of AccountTrack (modular
monolith, Clean Architecture, PostgreSQL, Vue 3 + TS + Tailwind, documented decisions, changelog,
tests). It deliberately **drops** what a personal app doesn't need: multi-tenancy, multi-company,
double-entry accounting, inventory, and approval workflows.

## Target User

A single individual managing personal (and small side-business) finances. One account owner; no
multi-tenant, no per-company RBAC, no team collaboration.

## Long-Term Vision

Grow from a faithful digital replacement of the workbook into a calm, insightful money companion:

* Core cashflow ledger (income / expenses / transfers)
* Accounts & balances
* Budgeting & allocation planning (Master Plan)
* Dashboards, overview & category analytics
* Goals & projects, debts, loan simulation
* Investments (holdings + trade history)
* Work/payroll helpers (salary, leave, bonus, overtime)

---

# Core Principles

1. Correctness of money over convenience.
2. The ledger is the source of truth; balances and reports are derived, never stored as truth.
3. Never physically delete financial data — soft delete + full audit history.
4. Maintainability over premature optimization.
5. Clarity and calm in the UI; data first.
6. Architecture should allow growth without a rewrite.
7. Documentation is part of the product.
8. Every major decision is documented.
9. Bilingual by default (English + Bahasa Indonesia); no hardcoded UI text.
10. Simple where personal, rigorous where financial.

---

# Technology Stack

## Backend

* .NET 8
* ASP.NET Core Web API
* Entity Framework Core
* PostgreSQL (Npgsql)

## Frontend

* Vue 3 (Composition API, `<script setup>`)
* TypeScript (strict)
* Pinia
* Vue Router
* Tailwind CSS
* vue-i18n (English + Bahasa Indonesia)
* Apache ECharts (via vue-echarts)
* Lucide icons

## Infrastructure

* Docker & Docker Compose
* Nginx
* GitHub & GitHub Actions

## Future (not MVP)

* Redis (caching), background jobs, mobile client (the UI reference is mobile-first).

---

# Architecture

## Style

* **Modular Monolith** + **Clean Architecture**.
* Do NOT use microservices.
* Modules are organized by domain and kept loosely coupled so they *could* be extracted later, but
  extraction is not a goal for a personal app.

## Clean Architecture layers (per module)

```
Modules/
├── Identity/        (single-user auth)
├── Accounts/        (accounts, account groups, balances)
├── Ledger/          (income / expense / transfer transactions)
├── Budgeting/       (categories, budget periods, master plan)
└── Reporting/       (overview, summary, category trackers — read models)
```

Each module contains:

* **Domain** — entities, value objects, business rules. Depends on nothing outward.
* **Application** — use cases, contracts, validation.
* **Infrastructure** — EF Core persistence, integrations.
* **API** — endpoints and contracts.

Rules:

* Domain must not depend on Infrastructure.
* A module MUST NOT read or write another module's tables directly. Cross-module needs go through
  application-service contracts or in-process integration events.
* Reports are computed from the ledger, never from a stored balance treated as truth.
* Module boundaries are enforced in CI by an architecture-fitness test.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and [docs/MODULES.md](docs/MODULES.md).

---

# Data Model & Money Rules

## Single-entry cashflow (ADR-0002)

* A **Transaction** has a `Type` of **Income**, **Expense**, or **Transfer**.
  * Income increases an account balance.
  * Expense decreases an account balance.
  * Transfer moves an amount from one account to another (decreases `AccountId`, increases
    `ToAccountId`).
* **Account balance = OpeningBalance + Σ(income) − Σ(expense) ± transfers**, computed from the
  ledger. Never store a running balance as the source of truth.
* There is **no double-entry journal**. This is a deliberate decision (ADR-0002).

## Categories (three levels)

Mirrors the workbook's `Budget → Category → Sub` taxonomy:

* **Budget** (top): Investment, Needs, Wants, Income, … (the "envelope").
* **Category**: e.g. Wife, Internet, Food, Transportation.
* **Sub**: e.g. Household, Fuel, Breakfast.

Modeled as a single self-referencing `Category` table with a `Level` and `ParentId`.

## Money

* Stored as `numeric(19,2)`. (Some source rows — e.g. bank interest — carry fractional values;
  precision is preserved.)
* Every monetary value carries an explicit **CurrencyCode**. Functional currency is **IDR**;
  multi-currency (the workbook's SGD/JPY/USD columns) is a reserved, future capability.
* Displayed `id-ID`: `Rp 1.234.567`; negatives in parentheses `(1.234,56)` with tabular figures.

## Primary keys & audit fields

* Business entity PKs are **GUID** (not `INT IDENTITY`).
* Every table carries: `CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt, DeletedBy, IsDeleted`.
* Master data (accounts, categories) supports **edit** and **deactivate (soft-delete)** — never
  physical delete. A record referenced by a transaction cannot be deactivated while in use.

See [docs/DATABASE.md](docs/DATABASE.md) and [docs/DATA_MODEL_FROM_EXCEL.md](docs/DATA_MODEL_FROM_EXCEL.md).

---

# Modules & Menu

The application menu mirrors the workbook. MVP menu:

* **Dashboard** — net worth, month cashflow, overview, category trackers (from `Overview`,
  `Summary`, `Category Tracker`).
* **Transactions** — Income · Expenses · Transfers (from `Income`, `Expenses`, `Account Transfer`).
* **Accounts** — accounts & balances (from `Account`).
* **Budget** — monthly Plan / Actual / Leftover (from `Budget`).
* **Master Plan** — Investment / Needs / Wants allocation planner (from `Master Plan`).
* **Categories** — the Budget→Category→Sub taxonomy (from `Category List`).

Later phases: **Goals & Projects** (Life Plan, Wedding Plan), **Debts** (Liabilities), **Simulator**
(property/loan `Simulasi`), **Investments** (RDN Ajaib, ASII, Trans. Hist.), **Work & Payroll**
(Salary Slip, Leave/Bonus/Overtime, THR list). See [docs/ROADMAP.md](docs/ROADMAP.md).

---

# Authentication & Security

* Single account owner. Phase 1: email + password login, JWT access token + refresh token.
* No RBAC/roles, no segregation of duties, no multi-tenant isolation (single user).
* Secrets, connection strings, and keys are never hardcoded — configuration only.
* See [docs/SECURITY.md](docs/SECURITY.md).

---

# Internationalization

Documentation language: **English**. Application default language: **English**. Secondary language:
**Bahasa Indonesia**.

* **Every word shown to the user must exist in both English and Bahasa Indonesia** — menus, titles,
  tabs, labels, buttons, table headers, placeholders, tooltips, empty states, toasts, validation and
  error messages, and status/enum labels. Permitted exceptions: proper nouns, currency/number
  formats, codes, and universal loan-words (e.g. "PPN", "Transfer", "PDF", "Email", "Total").
* **No hardcoded UI text.** All strings go through the i18n layer (`t('…')`). The two locale
  dictionaries (`frontend/src/i18n/locales/en.ts` and `id.ts`) must stay structurally identical.
* **Backend enum values rendered in the UI must be translated** by an i18n map keyed on the enum
  value (transaction type, status, category level), never shown raw.
* **Seeded reference data shown in the UI has a localized display mapping**, overriding a name only
  while it is still at its seeded default (a user rename is always shown verbatim).
* Language switching is live; localized text reacts to a locale change.
* A user-facing string change is not complete until both locales are updated.

---

# API Standards

* Base route: `/api/v1`. Examples: `/api/v1/accounts`, `/api/v1/transactions`, `/api/v1/budgets`.
* Response envelope:

```json
// success
{ "success": true, "data": {} }
// failure
{ "success": false, "message": "Validation failed", "error": { "code": "validation_error" } }
```

* Naming: C# `PascalCase` (e.g. `Transaction`), SQL tables plural (`Transactions`), API routes
  kebab-case (`master-plan`). See [docs/API_SPEC.md](docs/API_SPEC.md) and
  [docs/ERROR_HANDLING.md](docs/ERROR_HANDLING.md).

---

# Frontend / Design

* **Dark-first fintech aesthetic** with a **single solid green accent** (from the product's UI
  reference). **No gradients**, no emoji, no glassmorphism.
* Rounded cards, tabular figures on all numbers, `id-ID` money formatting, Lucide icons, ECharts with
  solid series fills.
* Full spec: [docs/frontend/DESIGN_LANGUAGE.md](docs/frontend/DESIGN_LANGUAGE.md),
  [docs/frontend/FRONTEND_ARCHITECTURE.md](docs/frontend/FRONTEND_ARCHITECTURE.md),
  [docs/frontend/BRAND.md](docs/frontend/BRAND.md).

---

# Testing Strategy

Required:

* **Unit tests** — highest priority on money-critical logic: balance derivation, transfer integrity,
  budget actual-vs-plan, master-plan totals, category rules.
* **Integration tests** — API + EF Core against PostgreSQL.
* **Architecture tests** — module-boundary fitness (a module must not depend on another module's
  internals).
* **Frontend unit tests** — formatters and stores (Vitest).

See [docs/TESTING.md](docs/TESTING.md).

---

# Code Quality Rules

Avoid: God classes, massive services, circular dependencies, shared mutable state, business logic in
static helpers.

Prefer: SOLID, dependency injection, Clean Architecture, small focused services, domain-driven
naming. See [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md).

---

# Deployment

Environments: Local · Development · Production. Configuration via `appsettings.json` +
environment-specific overrides and environment variables. Never hardcode secrets, connection
strings, or API keys. See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

---

# Documentation Structure

```
/
├── CLAUDE.md
├── README.md
├── CHANGELOG.md
└── docs/
    ├── README.md            (docs index)
    ├── STATUS.md            (where we are / what's next)
    ├── PRD.md
    ├── ROADMAP.md
    ├── ARCHITECTURE.md
    ├── MODULES.md
    ├── DATABASE.md
    ├── DATA_MODEL_FROM_EXCEL.md
    ├── API_SPEC.md
    ├── ERROR_HANDLING.md
    ├── BUSINESS_RULES.md
    ├── DECISIONS.md
    ├── GLOSSARY.md
    ├── SECURITY.md
    ├── CODING_STANDARDS.md
    ├── TESTING.md
    ├── DEPLOYMENT.md
    ├── CONTRIBUTING.md
    ├── adr/
    │   └── 0000-template.md
    └── frontend/
        ├── README.md
        ├── DESIGN_LANGUAGE.md
        ├── FRONTEND_ARCHITECTURE.md
        └── BRAND.md
```

## Documentation Rules

Documentation is part of the product. Whenever a major change occurs, update in the **same change**:
Architecture, Database design, API spec, Module docs, Business rules, and Decisions. Documentation
must stay synchronized with implementation.

### CHANGELOG Rules

`CHANGELOG.md` (repository root) is Tameru's immutable historical record. A task is not complete
until `CHANGELOG.md` has been updated.

**Ordering**

* Reverse chronological — newest entries at the top, directly below the `# Tameru Changelog`
  heading; never append to the bottom.
* Historical entries are immutable: never edit, reorder, renumber, or delete them.

**Entry structure**

```
# Tameru Changelog

## [YYYY-MM-DD HH:mm:ss UTC]

CHG-0002 — Short title

- Detail bullets describing what changed and why.

---

## [YYYY-MM-DD HH:mm:ss UTC]

CHG-0001 — Short title
...
```

**Change ids** — `CHG-NNNN`, sequential, zero-padded to four digits; never reused or renumbered.
**Timestamps** — always UTC, `YYYY-MM-DD HH:mm:ss UTC`. **Rollbacks** — recorded as a new entry, not
by editing the original.

**Procedure (each update):** read the top-most id → increment → insert the new entry at the top →
UTC timestamp → preserve all history exactly.

---

# Authorship & Attribution

**Tameru has a single author: Yovan Alvianto.** Do NOT add "Claude", "Anthropic", or any AI
co-author, `Co-Authored-By` trailer, or "Generated with" line to commits, pull requests, CHANGELOG
entries, or documentation. Author name in git and docs is Yovan Alvianto only.

---

# Non-Negotiable Rules

1. Use .NET 8, PostgreSQL, Vue 3, TypeScript, Tailwind CSS.
2. Modular Monolith + Clean Architecture.
3. Single-user; no multi-tenant, no multi-company, no RBAC roles.
4. Single-entry cashflow; **no double-entry accounting**.
5. The transaction ledger is the source of truth; balances/budgets/reports are derived.
6. Never physically delete financial data; maintain complete audit history.
7. Business entity PKs are GUIDs; every table has the standard audit fields.
8. Money is stored with an explicit currency code; IDR is the functional currency.
9. English is the default UI language; Bahasa Indonesia must be fully supported; no hardcoded UI text.
10. Documentation is written in English and kept synchronized with implementation.
11. Every major decision is recorded in DECISIONS.md; every major business rule in BUSINESS_RULES.md.
12. The UI is dark-first, single solid green accent, **no gradients**.
13. Never hardcode secrets, connection strings, or API keys.
14. Sole author is Yovan Alvianto; no AI attribution anywhere.
