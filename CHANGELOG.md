# Changelog

All notable changes are documented here. The project follows Keep a Changelog conventions and intends to use Semantic Versioning.

## [Unreleased]

### Planned
- Complete the v0.2.0 release-candidate verification cycle and create the tag only after required GitHub Actions checks are green.
- Add a reviewed npm lockfile and move clean/release Node installs from `npm install` to `npm ci`.
- Add signed Android App Bundle configuration and store-ready metadata outside source-controlled secrets.
- Finish browser companion store branding, icons, screenshots, privacy metadata, and publication review.

## [0.2.0] - Release candidate

### Added
- Production-oriented CampusCore modular architecture, ASP.NET Core API, PostgreSQL persistence, Identity/JWT authentication, audit logging, migrations, and validated file storage.
- Student, guardian, enrollment, attendance, leave, marks, grade-scale, report-card, timetable, staff, announcement, search, import/export, institution-settings, and user-administration workflows.
- Responsive React/TypeScript Web/PWA client with dashboard, search, student, academic, operations, staff, announcements, academic-catalog, settings/audit, and About surfaces.
- Light/dark/system theming, PWA/offline shell behavior, printable reports, loading/empty/error states, and keyboard-accessible navigation.
- Deterministic browser E2E coverage for authentication, authorization, primary routes, offline behavior, keyboard journeys, and axe-powered WCAG A/AA smoke checks.
- Disposable real-stack Chromium release smoke covering first-run administrator bootstrap, student/guardian persistence, academic catalog setup, enrollment, attendance, marks, report-card rendering, announcements, settings/audit, sign-out, and protected-route enforcement against PostgreSQL/API/Web containers.
- Strict TypeScript and ESLint coverage for both mocked and real-stack Playwright suites.
- Database-backed `/readyz` readiness alongside process-only `/healthz` liveness.
- Cross-platform PostgreSQL + attachment backup, checksum verification, guarded restore tooling, and automated disaster-recovery round-trip CI.
- Migration integrity/idempotence CI plus generated idempotent migration SQL artifacts.
- Production deployment smoke testing, web bundle budgets, CodeQL/dependency automation, and release artifact/checksum workflow.
- Production Web artifact safety verification that rejects known local/development deployment markers before packaging.
- Production configuration validation that rejects known local/development secrets and wildcard production host filtering.
- Capacitor 8 Android packaging using the shared React/Vite client, native runtime detection, Android safe-area behavior, lifecycle/back handling, explicit native API targeting, and Android debug-APK CI verification.
- Chromium Manifest V3 CampusCore Companion preparation with storage-only permission, configurable CampusCore URL, route shortcuts, options UI, policy validation, and packaging CI.
- Repository-wide `VERSION` source for v0.2.0 plus automated version-consistency validation across .NET, Web/PWA, Compose, environment examples, and browser companion metadata.

### Changed
- API and Web containers run with non-root application users where supported by their runtime images.
- Docker Compose local defaults bind database/API/Web ports to loopback and use Development mode unless explicitly overridden.
- Web/PWA, Android CI, Docker Compose, environment samples, extension metadata, and .NET assemblies are aligned to version `0.2.0`.
- The API root endpoint reports its compiled assembly version instead of a hard-coded application version.
- Tagged releases now require the Git tag to match the repository version and include `VERSION` plus reviewable migration SQL alongside API/Web/companion archives and SHA-256 checksums.
- Tagged Web/PWA archives are built with same-origin API routing and verified for release-safe deployment markers before packaging.
- Android production builds require an explicit HTTPS API origin; constrained cleartext usage remains limited to local/emulator development scenarios.
- PWA service-worker registration is disabled inside native Capacitor shells.
- Setup/build/release/testing documentation uses the actual `src/CampusCore.Web` and `src/CampusCore.Extension` paths and the repository-local EF tool manifest.

### Security
- Production CORS origins are validated as HTTPS origins without credentials, application paths, queries, or fragments.
- Production deployment smoke uses explicit HTTPS CORS origins so the smoke environment exercises the same fail-closed validation as a real Production deployment.
- Browser companion validation rejects host permissions and content scripts to preserve its least-privilege navigation-only boundary and verifies package/manifest version alignment.
- Production startup rejects known development/local database and signing-key placeholders and requires explicit `AllowedHosts`.
- Attachment restore operations preserve non-root runtime ownership after controlled recovery operations.
- Backup outputs and Playwright reports are excluded from Git and documented as generated/sensitive operational data where applicable.

### Known release-candidate limitations
- No npm lockfile is committed yet; Node workflows intentionally use `npm install` until a reviewed lockfile can be generated and committed.
- The Android workflow assembles an unsigned/debug verification APK; Play-distributable signed AAB generation requires deployment-owner signing credentials and is not claimed as complete.
- The browser companion is technically packaged and policy-validated but has not completed Chrome/Edge store publication requirements.
- The v0.2.0 Git tag and public GitHub Release must not be created until the final release-candidate checks are confirmed green.
