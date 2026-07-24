# Tameru

**A personal finance manager** — a modern, dark-first money tracker for a single owner, built from a
long-lived Indonesian financial-projection workbook and engineered with the same architecture and
discipline as [AccounTrack](../AccounTrack).

> Status: **architecture & design phase.** No application code yet — the documentation below defines
> the system before implementation begins.

_Tameru_ (溜める — "to save up / accumulate") tracks day-to-day money the way the source workbook
does: three transaction ledgers (**Income**, **Expenses**, **Transfers**) move amounts between
**Accounts**; **Budget** and **Master Plan** set intent; **Dashboards** show where the money went.

## What it is

Tameru is a **single-user** personal-finance app with a **single-entry cashflow** core: every
transaction moves money in, out, or between accounts, and all balances, budgets, and reports are
derived from that one ledger (the ledger is the source of truth). It is Indonesia-first (IDR,
`id-ID` formatting) and bilingual (English + Bahasa Indonesia).

## Tech stack

- **Backend:** .NET 8, ASP.NET Core Web API, EF Core, PostgreSQL (Npgsql)
- **Frontend:** Vue 3, TypeScript, Pinia, Vue Router, Tailwind CSS
- **Architecture:** Modular Monolith + Clean Architecture (module-per-domain)
- **Infra:** Docker, Docker Compose, Nginx, GitHub Actions

## Core invariants

Single-entry cashflow · the transaction ledger is the source of truth · balances/budgets/reports are
**derived**, never stored as truth · soft delete + full audit history · money always carries an
explicit currency code (IDR functional) · every user-facing string exists in English **and** Bahasa
Indonesia.

## Documentation

`CLAUDE.md` is the single source of truth. Detailed design lives in [`docs/`](docs/) — start with the
[docs index](docs/README.md).

| Area | Doc |
|---|---|
| Status & roadmap | [STATUS](docs/STATUS.md) · [PRD](docs/PRD.md) · [ROADMAP](docs/ROADMAP.md) |
| Architecture | [ARCHITECTURE](docs/ARCHITECTURE.md) · [MODULES](docs/MODULES.md) |
| Data & API | [DATABASE](docs/DATABASE.md) · [DATA_MODEL_FROM_EXCEL](docs/DATA_MODEL_FROM_EXCEL.md) · [API_SPEC](docs/API_SPEC.md) · [ERROR_HANDLING](docs/ERROR_HANDLING.md) |
| Rules & decisions | [BUSINESS_RULES](docs/BUSINESS_RULES.md) · [DECISIONS](docs/DECISIONS.md) · [GLOSSARY](docs/GLOSSARY.md) |
| Frontend | [frontend/README](docs/frontend/README.md) · [DESIGN_LANGUAGE](docs/frontend/DESIGN_LANGUAGE.md) · [FRONTEND_ARCHITECTURE](docs/frontend/FRONTEND_ARCHITECTURE.md) · [BRAND](docs/frontend/BRAND.md) |
| Engineering | [SECURITY](docs/SECURITY.md) · [CODING_STANDARDS](docs/CODING_STANDARDS.md) · [TESTING](docs/TESTING.md) · [DEPLOYMENT](docs/DEPLOYMENT.md) · [CONTRIBUTING](docs/CONTRIBUTING.md) |
| History | [CHANGELOG](CHANGELOG.md) — notable changes (`CHG-*`, reverse-chronological) |

## Author

Tameru is built and maintained by **Yovan Alvianto** (sole author).
