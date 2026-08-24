# Privacy

CampusCore is self-hosted software. The deploying institution controls the database, accounts, retention, backups, deployment endpoints, and legal basis for processing student and staff records.

CampusCore does not require telemetry to function. Application logs are designed to avoid passwords, tokens, authorization headers, document bodies, and unnecessary PII. Audit events record actor, action, entity type, entity identifier, timestamp, and safe metadata rather than full sensitive records.

## Web/PWA and Android client storage

The shared React client follows the same data-minimization boundary in a browser/PWA and inside the Capacitor Android shell:

- access tokens use session-scoped web storage rather than persistent `localStorage`;
- authenticated API responses remain in application memory rather than being written to persistent browser storage;
- the service worker caches only public application-shell resources and excludes `/api/*`;
- service-worker registration is disabled inside the native Capacitor runtime;
- theme preference is non-sensitive and may be stored locally;
- Android packaging requires an explicit API endpoint and does not add telemetry by itself.

The deploying institution remains responsible for device-management policy, session expectations, Android backup policy, screen-capture policy where required by local rules, and secure distribution of signed mobile builds.

## Browser companion

The current CampusCore Companion extension is deliberately navigation-only. It stores the configured CampusCore web URL using browser sync storage so the setting can follow the user's browser profile when browser sync is enabled.

It does **not** currently request or store:

- CampusCore passwords;
- CampusCore access tokens;
- student/staff records;
- browsing history;
- page contents;
- host permissions.

Its only requested browser permission is `storage`. Institutions that publish or distribute the extension should accurately describe browser-sync behavior in their privacy/store disclosures. Any future host permission, content script, direct API access, or storage of CampusCore data requires a new privacy/threat review before implementation or release.

## Operator responsibilities

Operators should configure TLS, explicit trusted CORS origins, least-privilege database credentials, retention, backups, access roles, applicable consent/notice obligations, and secure signing/distribution practices before production use. Demo/test seed data must remain fictional only.

No production Android signing key, database credential, JWT signing key, bootstrap secret, or extension-store credential belongs in Git.
