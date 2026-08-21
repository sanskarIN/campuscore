# Threat Model

## Assets

Student/staff identity data, guardian contacts, attendance, marks, reports, credentials, uploaded documents, audit history, institution configuration, and backups.

## Trust boundaries

Browser ↔ API; API ↔ PostgreSQL; API ↔ file storage; operator ↔ deployment secrets; CI ↔ repository/release artifacts.

## Primary abuse cases and mitigations

- **Credential theft:** strong password policy, short-lived JWTs, no credential logging, production TLS.
- **Broken authorization:** policy/role checks server-side; UI visibility is never an authorization control.
- **Mass enumeration:** pagination, validation, rate limiting, scoped queries, and privacy-conscious analytics.
- **Injection:** EF Core parameterization and explicit request validation.
- **Malicious uploads:** size/type allow-list, generated names, non-executable storage, authenticated access.
- **CSRF/CORS confusion:** bearer tokens with strict configured CORS; do not enable wildcard credentials.
- **XSS:** React escaping plus CSP/security headers; never render unsanitized HTML.
- **Secret exposure:** `.env` ignored, secret scanning, placeholder-only examples.
- **Audit tampering:** append-only service path and restricted read access; production deployments should use database-level retention/backup controls.
- **Backup disclosure:** operator-encrypted backup storage and least privilege.

## Residual risks

A self-hosting operator can misconfigure infrastructure, export data insecurely, or grant excessive roles. CampusCore documents secure defaults but cannot enforce every external deployment control.
