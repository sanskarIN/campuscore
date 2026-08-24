# Threat Model

## Assets

Student/staff identity data, guardian contacts, attendance, marks, reports, credentials, uploaded documents, audit history, institution configuration, backups, JWT signing material, Android signing material, and release artifacts.

## Trust boundaries

- Browser/PWA ↔ API.
- Capacitor Android webview (`https://localhost`) ↔ API.
- API ↔ PostgreSQL.
- API ↔ file storage.
- Operator ↔ deployment/signing secrets.
- CI ↔ repository/release artifacts.
- Browser extension ↔ browser sync storage.
- Browser extension ↔ configured CampusCore Web/PWA URL through navigation only.

## Primary abuse cases and mitigations

- **Credential theft:** strong password policy, short-lived JWTs, no credential logging, production TLS, no credential/token storage in the browser extension.
- **Broken authorization:** policy/role checks server-side; UI visibility is never an authorization control and Android uses the same API authorization boundary.
- **Mass enumeration:** pagination, validation, rate limiting, scoped queries, and privacy-conscious analytics.
- **Injection:** EF Core parameterization and explicit request validation.
- **Malicious uploads:** size/type allow-list, generated names, non-executable storage, authenticated access.
- **CSRF/CORS confusion:** bearer tokens with strict configured CORS; production CORS entries are HTTPS-only origins and wildcard credential policies are not enabled.
- **Native-origin CORS bypass:** Capacitor's `https://localhost` is explicitly configured only as an allowed origin; it does not bypass JWT/role authorization.
- **XSS:** React escaping plus CSP/security headers; never render unsanitized HTML. Native packaging does not make WebView content trusted.
- **Service-worker private-data retention:** `/api/*` is excluded from service-worker caching and the service worker is not registered inside the native shell.
- **Unsafe Android API targeting:** Android packaging requires an explicit API origin; public cleartext HTTP is rejected, with a narrow emulator/local-host development override only.
- **Android signing-key disclosure:** signing keystores/passwords stay outside Git and are supplied through approved local/CI secret handling.
- **Tampered Android source generation:** CI regenerates the native project from committed Capacitor/web source and assembles a verification APK; release signing must use the same reviewed tag.
- **Extension privilege creep:** Manifest validation currently rejects host permissions and content scripts and requires storage-only permission.
- **Extension URL abuse:** configured destinations must be absolute HTTPS URLs except localhost development; credentials/query/fragment values are rejected.
- **Extension credential capture:** the extension does not embed CampusCore login forms, read page content, or directly call authenticated CampusCore APIs.
- **Secret exposure:** `.env` files and generated sensitive output are ignored; committed examples contain placeholders only; production validation rejects known unsafe placeholders.
- **Audit tampering:** append-only service path and restricted read access; production deployments should use database-level retention/backup controls.
- **Backup disclosure:** operator-encrypted backup storage and least privilege.
- **Release artifact substitution:** tagged release workflows build artifacts from the tag, generate checksums, validate the extension, and gate release creation on reproducible Android assembly.

## Native and extension change review

Treat these as security-sensitive architectural changes requiring explicit review:

- adding a Capacitor plugin that exposes sensitive device capability or persistent storage;
- enabling arbitrary cleartext Android network traffic;
- adding Android deep links/app links that can trigger authenticated actions;
- changing native WebView/navigation policy;
- adding extension `host_permissions`, content scripts, history/tabs read access, or direct API access;
- storing CampusCore tokens/records in extension storage;
- changing extension sync-storage schema to contain user/student data;
- publishing a signed Android/extension release from source other than the reviewed tag.

## Residual risks

A self-hosting operator can misconfigure infrastructure, TLS/CORS, exports, device management, browser-extension distribution, signing keys, backups, or role assignments. A compromised client device/browser profile can expose an active authenticated session. CampusCore documents and validates secure application defaults where practical but cannot enforce every external deployment, endpoint-security, browser-sync, or organizational control.
