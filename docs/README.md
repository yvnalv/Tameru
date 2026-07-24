# Tameru Documentation Index

`CLAUDE.md` (repository root) is the single source of truth. These documents elaborate it.

## Reading order for new contributors
1. [`../CLAUDE.md`](../CLAUDE.md) — master instructions (read fully).
2. [STATUS.md](STATUS.md) — where we are and what's next.
3. [GLOSSARY.md](GLOSSARY.md) — shared vocabulary.
4. [ARCHITECTURE.md](ARCHITECTURE.md) — how the system is built.
5. [DATA_MODEL_FROM_EXCEL.md](DATA_MODEL_FROM_EXCEL.md) — how the source workbook maps to the app.
6. [DECISIONS.md](DECISIONS.md) — why it's built this way (ADRs).

## Catalog

### Product
- [STATUS.md](STATUS.md) — **start here**: milestones, where we are, what's next.
- [PRD.md](PRD.md) — product requirements and MVP scope.
- [ROADMAP.md](ROADMAP.md) — phased delivery plan.

### Architecture
- [ARCHITECTURE.md](ARCHITECTURE.md) — modular monolith, clean architecture, boundaries, pipeline.
- [MODULES.md](MODULES.md) — module catalog (purpose, deps, MVP scope).

### Data & API
- [DATABASE.md](DATABASE.md) — conventions, schema, indexing, migrations.
- [DATA_MODEL_FROM_EXCEL.md](DATA_MODEL_FROM_EXCEL.md) — the 35-sheet workbook → module/table map.
- [API_SPEC.md](API_SPEC.md) — REST conventions, response envelope, resource catalog.
- [ERROR_HANDLING.md](ERROR_HANDLING.md) — error model, codes, status mapping.

### Rules & Decisions
- [BUSINESS_RULES.md](BUSINESS_RULES.md) — catalog of business rules (`BR-*` ids).
- [DECISIONS.md](DECISIONS.md) — Architectural Decision Records (`ADR-*`).
- [GLOSSARY.md](GLOSSARY.md) — ubiquitous language.
- [adr/0000-template.md](adr/0000-template.md) — ADR template.
- [../CHANGELOG.md](../CHANGELOG.md) — immutable change history (`CHG-*`).

### Frontend
- [frontend/README.md](frontend/README.md) — frontend docs index.
- [frontend/DESIGN_LANGUAGE.md](frontend/DESIGN_LANGUAGE.md) — colors, type, spacing, components.
- [frontend/FRONTEND_ARCHITECTURE.md](frontend/FRONTEND_ARCHITECTURE.md) — stack, structure, theming.
- [frontend/BRAND.md](frontend/BRAND.md) — placeholder identity.

### Engineering
- [SECURITY.md](SECURITY.md) — auth, secrets, data protection.
- [CODING_STANDARDS.md](CODING_STANDARDS.md) — code conventions and quality rules.
- [TESTING.md](TESTING.md) — testing strategy and priorities.
- [DEPLOYMENT.md](DEPLOYMENT.md) — environments, CI/CD, migrations, secrets.
- [CONTRIBUTING.md](CONTRIBUTING.md) — workflow and authorship rules.

## Maintenance rule
Documentation is part of the product. A change to schema, architecture, API, or business rules
updates the matching doc in the **same change** (`CLAUDE.md` → Documentation Rules).
