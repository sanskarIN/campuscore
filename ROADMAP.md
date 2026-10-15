# Roadmap

CampusCore **v1.0.0** was released on October 15, 2026 as the first stable self-hosted distribution: https://github.com/sanskarIN/campuscore/releases/tag/v1.0.0. The root `VERSION` remains authoritative; its tagged release passed all required checks.

## Completed foundations included in v1.0.0

### Foundation and core data
- [x] Modular Domain → Application → Infrastructure → API architecture.
- [x] PostgreSQL persistence and EF Core migrations.
- [x] ASP.NET Core Identity/JWT authentication and role-based administration.
- [x] Student, guardian, enrollment, staff, institution-settings, and audit foundations.

### Academic and communication operations
- [x] Attendance and leave workflows.
- [x] Marks, grading, report-card generation, and timetable workflows.
- [x] Announcements and validated attachment storage/download.
- [x] Search, paging, CSV reporting, transactional bulk import, and administrative endpoints.

### Web/PWA product
- [x] Responsive React/TypeScript product shell and primary module screens.
- [x] Light/dark/system themes and printable report styling.
- [x] PWA/offline shell with authenticated API data excluded from service-worker caching.
- [x] Keyboard-focused navigation and automated WCAG A/AA browser smoke checks.
- [x] Deterministic mocked browser E2E coverage and enforceable bundle budgets.
- [x] Disposable real-stack browser release smoke covering authentication, student/guardian persistence, academic operations, report-card rendering, announcements, settings/audit, and sign-out.
- [x] Strict TypeScript/ESLint validation for mocked and real-stack browser suites.

### Operations and security hardening
- [x] Separate liveness (`/healthz`) and database-backed readiness (`/readyz`).
- [x] Production configuration validation and safe local Compose defaults.
- [x] Non-root API/Web runtime containers.
- [x] Cross-platform backup, verification, and guarded restore scripts.
- [x] Automated backup/restore drill and deployment smoke testing.
- [x] Clean/idempotent migration CI and generated migration SQL artifacts.
- [x] Production Web artifact verification for same-origin API routing and rejection of local/development deployment markers.
- [x] Deployment, backup/restore, testing, release, security, privacy, and development documentation.

### Android and browser companion
- [x] Capacitor 8 Android packaging from the shared Web/PWA client.
- [x] Native runtime detection, safe-area behavior, explicit Android API targeting, and lifecycle/back handling.
- [x] Reproducible Android project regeneration and debug APK verification in CI.
- [x] Chromium Manifest V3 CampusCore Companion with storage-only permission.
- [x] Companion route shortcuts, settings UI, URL policy validation, version-alignment validation, and packaging CI.

### v1.0.0 release preparation
- [x] Release version alignment gate; `VERSION` is promoted to `1.0.0` for the stable candidate.
- [x] .NET, Web/PWA, Compose, environment sample, and extension versions aligned.
- [x] Automated version-consistency workflow including .NET assembly/file versions and browser-manifest syntax.
- [x] Release workflow rejects mismatched tags and components.
- [x] Release workflow packages migration SQL, version manifest, checksums, API, Web/PWA, and companion artifacts.
- [x] Tagged Web/PWA archive uses same-origin API routing and passes a release-asset safety check before packaging.
- [x] v1.0.0 release-candidate changelog, testing documentation, and release notes prepared.

## v1.0.0 — Stable self-hosted distribution

- [x] Prepare cross-platform API server binary packages (Windows x64, Linux x64, macOS arm64) plus Web/PWA and browser companion archives.
- [x] Add integrity hashes, migration SQL and documented executable/server installation boundaries.
- [x] Include a clearly marked Android debug **test-only** APK in release assets.
- [x] Gate automated tagging and GitHub Release creation on successful same-commit quality and security workflows.
- [x] Confirm all seven required workflows passed on the author-requested rebuilt v1.0.0 revision `aa506acd` (2026-10-15).
- [x] Confirm replaced annotated tag `v1.0.0` resolves to `aa506acd`, the republished stable GitHub Release has ten new nonempty assets and SHA-256 digests (2026-10-15).

- [x] Remove the retired public support mailbox and publish the corrected Buy Me a Coffee destination in rebuilt Web/PWA and Android test assets; release packaging verified both strings.

## After v1.0.0 — Future distribution enhancements

- [ ] Configure Android AAB production signing via a protected owner-managed keystore and real HTTPS API endpoint.
- [ ] Verify signed Android updates on representative physical devices/emulators before Play Store distribution.
- [ ] Complete companion Chromium/Edge store branding, assets, listing disclosures and approval.
- [ ] Review and commit npm lockfiles and upgrade CI to reproducible `npm ci`.
- [ ] Add signed provenance/attestations for release artifacts and immutable container images.
- [ ] Expand backup/restore rehearsal across prior-version migrations and deployment models.
- [ ] Improve external PostgreSQL, object storage, monitoring, scale, and performance references.
- [ ] Add operating-system/runtime variants based on validated user demand.

See `what_changed.md` for the latest verification results, current release blockers and next engineering steps.
