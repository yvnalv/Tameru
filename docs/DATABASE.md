# Database

PostgreSQL (Npgsql) via EF Core. This document defines conventions and the MVP schema. Domain-to-app
mapping from the source workbook is in [DATA_MODEL_FROM_EXCEL.md](DATA_MODEL_FROM_EXCEL.md).

## Conventions

- **Engine:** PostgreSQL 16+.
- **Primary keys:** `uuid` (GUID) for all business entities. No `INT IDENTITY`.
- **Money:** `numeric(19,2)`. Every monetary column pairs with a `currency_code char(3)` (default
  `IDR`) at the row or table level.
- **Dates:** `date` for calendar dates (transaction date); `timestamptz` for audit timestamps
  (UTC). Year/month/day are derived in queries, not stored.
- **Enums:** stored as `smallint` (mapped from C# enums) or `text` with a check constraint —
  default to `text` for readability of a personal DB; document allowed values.
- **Naming:** tables plural `snake_case` with a module prefix (e.g. `ledger_transactions`,
  `accounts`, `accounts_groups`, `budgeting_categories`). C# entities `PascalCase` singular.
- **Soft delete:** `is_deleted boolean not null default false` + a global query filter; deletes set
  `is_deleted`, `deleted_at`, `deleted_by`.
- **Schemas / ownership:** each module owns its tables (prefix or Postgres schema:
  `accounts`, `ledger`, `budgeting`, `identity`, `reporting`). Modules never join across ownership;
  they use contracts (see [ARCHITECTURE.md](ARCHITECTURE.md)).

## Standard audit fields (every table)

| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` | PK |
| `created_at` | `timestamptz` | UTC |
| `created_by` | `uuid` | user id |
| `updated_at` | `timestamptz` | UTC |
| `updated_by` | `uuid` | user id |
| `deleted_at` | `timestamptz null` | soft delete |
| `deleted_by` | `uuid null` | soft delete |
| `is_deleted` | `boolean` | default false |

## MVP schema

### identity.users
| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `email` | text | unique |
| `password_hash` | text | hashed (see SECURITY.md) |
| `display_name` | text | |
| `locale` | text | `en` / `id`, default `en` |
| + audit | | |

### identity.refresh_tokens
`id, user_id, token_hash, expires_at, revoked_at null, + audit`.

### accounts.account_groups
| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `name` | text | e.g. Saving, Investment, Family |
| `sort_order` | int | |
| + audit | | |

### accounts.accounts
| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `name` | text | e.g. "BCA", "Cash (yvnalv)" |
| `group_id` | uuid null | → account_groups |
| `type` | text | Cash / Bank / EWallet / Investment / Blocked |
| `currency_code` | char(3) | default IDR |
| `opening_balance` | numeric(19,2) | default 0 |
| `is_active` | boolean | default true |
| `sort_order` | int | |
| + audit | | |

> **Balance is not stored.** Current/monthly balance = `opening_balance` + ledger sums (see below).

### budgeting.categories (self-referencing, three levels)
| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `name` | text | |
| `level` | text | Budget / Category / Sub |
| `parent_id` | uuid null | → categories.id (Category→Budget, Sub→Category) |
| `flow` | text | Income / Expense / Transfer / Any |
| `is_system` | boolean | seeded (Income/Transfer/Adjustment) — cannot delete |
| `is_active` | boolean | default true |
| `sort_order` | int | |
| + audit | | |

### ledger.transactions
| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `type` | text | Income / Expense / Transfer |
| `date` | date | transaction date |
| `title` | text | the "Transaction" name |
| `amount` | numeric(19,2) | > 0 (sign implied by type) |
| `currency_code` | char(3) | default IDR |
| `account_id` | uuid | source (Expense/Transfer) or destination (Income) |
| `to_account_id` | uuid null | destination for Transfer |
| `budget_category_id` | uuid null | → categories (level Budget) |
| `category_id` | uuid null | → categories (level Category) |
| `sub_category_id` | uuid null | → categories (level Sub) |
| `status` | text | Cleared / Uncleared |
| `description` | text null | |
| + audit | | |

Constraints:
- `amount > 0`.
- Transfer: `to_account_id is not null and to_account_id <> account_id`.
- Income/Expense: `to_account_id is null`.
- Category flow must match `type` (an Expense uses Expense/Any categories).

### budgeting.budget_periods
`id, year int, month int, note text null, + audit`; unique `(year, month)`.

### budgeting.budget_lines
`id, budget_period_id uuid, category_id uuid, plan_amount numeric(19,2), + audit`;
unique `(budget_period_id, category_id)`. *Actual* and *Leftover* are computed, not stored.

### budgeting.master_plan_sections
`id, name text (Investment/Needs/Wants), target_percent numeric(5,2), sort_order, + audit`.

### budgeting.master_plan_items
`id, section_id uuid, name text, price numeric(19,2), frequency int, sort_order, + audit`;
`total_budget` (= price × frequency) is computed.

## Derived balance (canonical formula)

For an account `A` up to date `d`:

```
balance(A, d) = A.opening_balance
              + Σ amount WHERE type=Income   AND account_id   = A AND date ≤ d AND not deleted
              − Σ amount WHERE type=Expense  AND account_id   = A AND date ≤ d AND not deleted
              − Σ amount WHERE type=Transfer AND account_id   = A AND date ≤ d AND not deleted
              + Σ amount WHERE type=Transfer AND to_account_id= A AND date ≤ d AND not deleted
```

Net worth = Σ balance over active accounts. Budget actual(category, period) = Σ expense amount for
that category within the period. These are the only "truth" computations; Reporting may cache them as
read models rebuildable from Ledger events.

## Indexing (MVP)

- `ledger.transactions`: `(account_id, date)`, `(to_account_id, date)`, `(category_id, date)`,
  `(date)`, partial `where is_deleted = false`.
- `budgeting.categories`: `(level)`, `(parent_id)`.
- `budgeting.budget_lines`: unique `(budget_period_id, category_id)`.
- `accounts.accounts`: `(group_id)`, `(is_active)`.

## Naming convention

Columns are snake_case via `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention`), applied on
every module's `DbContext` options (and its design-time factory) so entity property `DisplayName`
maps to column `display_name`, etc. (ADR-0007).

## Migrations

- Per-module EF Core migrations (each module's Infrastructure owns its migration history table,
  `__ef_migrations_history`, in the module's own schema).
- Applied automatically in Development (`Database__AutoMigrate=true`); explicit in Production.
- Seed (idempotent): the owner user, default account groups, the Budget→Category→Sub starter
  taxonomy, and the three Master Plan sections with 40/50/10 targets.

## Data types summary

| Concept | Postgres | C# |
|---|---|---|
| Id | uuid | Guid |
| Money | numeric(19,2) | decimal |
| Currency | char(3) | string / CurrencyCode VO |
| Calendar date | date | DateOnly |
| Timestamp | timestamptz | DateTimeOffset |
| Enum | text (+check) | enum |
| Flag | boolean | bool |
| Percent | numeric(5,2) | decimal |
