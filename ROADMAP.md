# Roadmap

CampusCore is currently preparing **v0.2.0**. A root `VERSION` file is the release version source of truth; the public tag is created only after release-candidate verification succeeds.

## Completed foundations included in v0.2.0

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
- [x] Browser E2E coverage and enforceable bundle budgets.

### Operations and security hardening
- [x] Separate liveness (`/healthz`) and database-backed readiness (`/readyz`).
- [x] Production configuration validation and safe local Compose defaults.
- [x] Non-root API/Web runtime containers.
- [x] Cross-platform backup, verification, and guarded restore scripts.
- [x] Automated backup/restore drill and deployment smoke testing.
- [x] Clean/idempotent migration CI and generated migration SQL artifacts.
- [x] Deployment, backup/restore, testing, release, security, privacy, and development documentation.

### Android and browser companion
- [x] Capacitor 8 Android packaging from the shared Web/PWA client.
- [x] Native runtime detection, safe-area behavior, explicit Android API targeting, and lifecycle/back handling.
- [x] Reproducible Android project regeneration and debug APK verification in CI.
- [x] Chromium Manifest V3 CampusCore Companion with storage-only permission.
- [x] Companion route shortcuts, settings UI, URL policy validation, and packaging CI.

### v0.2.0 release preparation
- [x] Repository-wide `VERSION` source set to `0.2.0`.
- [x] .NET, Web/PWA, Compose, environment sample, and extension versions aligned.
- [x] Automated version-consistency workflow.
- [x] Release workflow rejects mismatched tags and components.
- [x] Release workflow packages migration SQL, version manifest, checksums, API, Web/PWA, and companion artifacts.
- [x] v0.2.0 release-candidate changelog and release notes prepared.

## Remaining blockers before tag v0.2.0

- [ ] Confirm the complete required GitHub Actions set is green for the final candidate commit.
- [ ] Fix any failures surfaced by the expanded E2E, accessibility, migration, recovery, deployment, Android, extension, performance, or version gates.
- [ ] Generate and review npm lockfiles when registry access is available, then move Node clean/release installs to `npm ci`.
- [ ] Capture representative release screenshots from a verified deployed candidate if screenshots are part of the v0.2.0 release requirement.
- [ ] Perform a final secrets/permissions/configuration review before creating the tag.

## v0.3.0 — Distribution readiness

- [ ] Add deployment-owner Android release-signing integration using protected secrets.
- [ ] Produce and verify a signed Android App Bundle (AAB) from tagged source.
- [ ] Add Android versionCode/versionName automation suitable for store upgrades.
- [ ] Add representative physical-device/emulator smoke coverage for signed candidates.
- [ ] Add final browser companion icons/branding assets and store-listing metadata.
- [ ] Complete Chrome Web Store / Edge Add-ons privacy, permission, screenshot, and publication review.
- [ ] Add release provenance/attestation improvements for downloadable artifacts and future container images.

## v0.4.0 — Deployment and scale hardening

- [ ] Exercise upgrades from a real previous-version database as the migration history grows.
- [ ] Add production-oriented external PostgreSQL/object-storage deployment examples.
- [ ] Add observability guidance/integration for metrics, traces, structured logs, and alerting.
- [ ] Add capacity/load baselines for search, reporting, attachment, and bulk workflows.
- [ ] Review horizontal-scaling requirements and single-writer migration coordination.

## 1.0.0 — Stable production release

- [ ] Complete all blocker-level security, privacy, accessibility, recovery, and upgrade audits.
- [ ] Provide stable deployment/upgrade support expectations and compatibility policy.
- [ ] Publish fully verified Web/PWA release artifacts and supported native/store packages.
- [ ] Confirm release documentation, screenshots, checksums/provenance, support channels, and rollback procedures.

See `what_changed.md` for the current exact repository state, recent commits, verification evidence, and the next executable tasks.
