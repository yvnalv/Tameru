# Security

Tameru is a single-user personal app; security is deliberately simple but not lax. It protects one
owner's financial data.

## Authentication

- **Email + password** login. Passwords hashed with a modern KDF (**Argon2id** or ASP.NET Core
  `PasswordHasher` / bcrypt) — never stored or logged in plaintext.
- **JWT access token** (short-lived, ~15 min) + **refresh token** (rotating, hashed at rest, stored
  in `identity.refresh_tokens`). Refresh rotation revokes the prior token.
- No third-party OAuth in MVP (single user); may be added later as a convenience.

## Authorization

- One owner account. No roles/RBAC. Every non-`/auth` endpoint simply requires a valid token for the
  owner. `ICurrentUser` supplies the authenticated user id for audit stamping.

## Secrets & configuration

- **Never hardcode** secrets, connection strings, JWT signing keys, or API keys. Use
  `appsettings.*.json` (non-secret defaults) + environment variables / user-secrets (dev) for
  secrets. `.env` is git-ignored.
- JWT signing key is a strong random secret provided via configuration; rotate on compromise.

## Data protection

- Financial data is never physically deleted (soft delete + audit), so accidental loss is
  recoverable.
- Backups: the database is the system of record; document a periodic `pg_dump` backup in
  [DEPLOYMENT.md](DEPLOYMENT.md). The source workbook is retained locally, not committed.
- The database is **never publicly exposed**; access is loopback/tunnel only in production.

## Transport & headers

- HTTPS everywhere (Nginx terminates TLS). HSTS in production.
- CORS restricted to the app origin. Standard secure headers (X-Content-Type-Options,
  Referrer-Policy, a conservative CSP for the SPA).

## Input & output safety

- All input validated at the Application boundary; parameterized EF Core queries (no string SQL).
- Errors never leak stack traces, SQL, or secrets (see [ERROR_HANDLING.md](ERROR_HANDLING.md)).
- Rate-limit `/auth/login` to slow brute force.

## Auditing

- Every mutation records `CreatedBy/At` or `UpdatedBy/At`; removals record `DeletedBy/At`. Because
  there is one user, the audit trail is primarily a change history and undo/forensic aid.

## Dependencies

- Keep runtime and npm/NuGet dependencies patched; enable Dependabot/`dotnet list package
  --vulnerable` checks in CI.
