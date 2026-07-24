# Error Handling

Consistent, predictable errors across the API. All failures use the response envelope's failure
shape (see [API_SPEC.md](API_SPEC.md)).

## Failure envelope

```json
{
  "success": false,
  "message": "Human-readable summary (localizable key resolvable client-side)",
  "error": {
    "code": "validation_error",
    "details": [ { "field": "amount", "message": "Must be greater than 0" } ]
  }
}
```

- `message` is a safe, user-facing summary. The client may map `error.code`/`field` to localized
  strings (EN/ID) — the server never leaks stack traces or SQL.
- `error.details` is present for validation errors; omitted otherwise.

## Error codes → HTTP status

| `error.code` | HTTP | When |
|---|---|---|
| `validation_error` | 400 | Request failed field validation |
| `unauthenticated` | 401 | Missing/invalid/expired token |
| `forbidden` | 403 | Authenticated but not allowed (rare in single-user) |
| `not_found` | 404 | Resource does not exist (or is soft-deleted) |
| `conflict` | 409 | State conflict (e.g. deactivate account still in use) |
| `unprocessable` | 422 | Semantically invalid (e.g. transfer to same account) |
| `rate_limited` | 429 | Too many requests |
| `internal_error` | 500 | Unhandled server fault (logged with a trace id) |

## Domain rule violations

Business-rule failures (see [BUSINESS_RULES.md](BUSINESS_RULES.md)) map to `422 unprocessable` (or
`409 conflict` for state clashes) with a specific `code`, e.g.:

| Rule | code | HTTP |
|---|---|---|
| Transfer source = destination | `transfer_same_account` | 422 |
| Amount ≤ 0 | `amount_not_positive` | 400 |
| Category flow mismatch | `category_flow_mismatch` | 422 |
| Deactivate account referenced by a transaction | `account_in_use` | 409 |
| Delete system category | `category_is_system` | 409 |
| Void an already-voided transaction | `already_voided` | 409 |

## Handling strategy

- A single ASP.NET Core exception-handling middleware maps exceptions → the failure envelope.
- Domain throws typed exceptions (`DomainRuleException` with a `code`); Application throws
  `ValidationException`; infrastructure faults become `internal_error` with a logged `traceId`.
- Every `internal_error` response includes `error.traceId` correlating to structured logs.
- Never return raw exception text or DB errors to the client.

## Client contract

- On `401`, the client clears the session and routes to login (after attempting one refresh).
- On `validation_error`, the client shows per-field messages using `error.details[].field`.
- On `internal_error`, the client shows a generic localized message and surfaces `traceId` for
  support/debugging.
