# API Specification

REST API conventions and the MVP resource catalog. Errors are defined in
[ERROR_HANDLING.md](ERROR_HANDLING.md).

## Conventions

- **Base route:** `/api/v1`.
- **Auth:** Bearer JWT on every route except `/auth/*`. Single owner user.
- **Format:** JSON. Money as string or number in minor-unit-safe decimal (server sends `numeric`
  serialized as number; the client formats `id-ID`). Dates as `YYYY-MM-DD`. Timestamps ISO-8601 UTC.
- **Naming:** resource paths are kebab-case plural (`/accounts`, `/transactions`, `/budget-periods`,
  `/master-plan/items`).
- **Pagination:** `?page=1&pageSize=50`; response includes `page, pageSize, total`.
- **Filtering (transactions):** `?type=&accountId=&categoryId=&status=&from=&to=&q=`.
- **Sorting:** `?sort=date:desc`.

## Response envelope

```json
// success (single)
{ "success": true, "data": { } }

// success (list)
{ "success": true, "data": { "items": [], "page": 1, "pageSize": 50, "total": 0 } }

// failure
{ "success": false, "message": "Human readable", "error": { "code": "validation_error",
  "details": [ { "field": "amount", "message": "Must be greater than 0" } ] } }
```

## Auth

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/auth/login` | email + password → access + refresh tokens |
| POST | `/api/v1/auth/refresh` | rotate refresh token |
| POST | `/api/v1/auth/logout` | revoke refresh token |
| GET | `/api/v1/auth/me` | current user + locale |
| PATCH | `/api/v1/auth/me` | update display name / locale |

## Accounts

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/accounts` | list accounts (with current balance) |
| GET | `/api/v1/accounts/{id}` | account detail + monthly balances |
| POST | `/api/v1/accounts` | create |
| PUT | `/api/v1/accounts/{id}` | update |
| POST | `/api/v1/accounts/{id}/deactivate` | soft-delete (blocked if referenced & active) |
| GET | `/api/v1/account-groups` | list groups |
| POST | `/api/v1/account-groups` | create |
| PUT | `/api/v1/account-groups/{id}` | update |

## Categories

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/categories?level=&flow=&parentId=` | list (tree or filtered) |
| POST | `/api/v1/categories` | create (Budget / Category / Sub) |
| PUT | `/api/v1/categories/{id}` | update |
| POST | `/api/v1/categories/{id}/deactivate` | soft-delete (blocked if in use / system) |

## Transactions (Ledger)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/transactions` | list (filters above) |
| GET | `/api/v1/transactions/{id}` | detail |
| POST | `/api/v1/transactions` | create Income / Expense / Transfer |
| PUT | `/api/v1/transactions/{id}` | update |
| POST | `/api/v1/transactions/{id}/clear` | set status Cleared |
| POST | `/api/v1/transactions/{id}/unclear` | set status Uncleared |
| POST | `/api/v1/transactions/{id}/void` | soft-delete (never hard delete) |
| POST | `/api/v1/transactions/import` | dry-run/commit spreadsheet import |

Create body (example — transfer):
```json
{ "type": "Transfer", "date": "2026-06-25", "title": "Transfer to BSI",
  "amount": 7300000, "accountId": "…octo", "toAccountId": "…bsi",
  "status": "Cleared", "description": null }
```

## Budget

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/budget-periods?year=` | list periods |
| GET | `/api/v1/budget-periods/{year}/{month}` | period with lines (Plan/Actual/Leftover) |
| POST | `/api/v1/budget-periods` | create a month |
| PUT | `/api/v1/budget-periods/{id}/lines` | upsert plan amounts per category |

## Master Plan

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/master-plan` | sections + items + totals + target split |
| POST | `/api/v1/master-plan/items` | add item |
| PUT | `/api/v1/master-plan/items/{id}` | update item (price/frequency) |
| DELETE | `/api/v1/master-plan/items/{id}` | soft-delete item |
| PUT | `/api/v1/master-plan/sections/{id}` | update target percent |

## Reporting

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/reports/net-worth` | total + per-account balances |
| GET | `/api/v1/reports/cashflow?year=&month=` | income vs expense, trend |
| GET | `/api/v1/reports/overview?year=` | yearly matrix (category × month) |
| GET | `/api/v1/reports/category-tracker?granularity=daily|monthly&from=&to=` | pivot |

## Versioning

`v1` is stable within MVP. Breaking changes bump the path (`/api/v2`). Additive changes stay in `v1`.
