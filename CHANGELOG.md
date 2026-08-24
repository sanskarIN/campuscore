# Changelog

All notable changes are documented here. The project follows Keep a Changelog conventions and intends to use Semantic Versioning.

## [Unreleased]

### Added
- Initial production-oriented CampusCore architecture, backend, PWA client, tests, CI, security, and documentation baseline.
- Deterministic browser E2E/accessibility coverage, bundle budgets, migration integrity checks, deployment smoke testing, and backup/restore drills.
- Production deployment hardening, non-root web container execution, migration SQL artifacts, and production configuration validation.
- Capacitor 8 Android packaging configuration using the shared React/Vite application.
- Native runtime detection, Android safe-area behavior, explicit native API targeting, and Android-specific environment validation.
- Android CI workflow that regenerates the native project and assembles a debug APK.
- Production validation and Compose configuration for browser/Capacitor CORS origins.
- Chromium Manifest V3 CampusCore Companion preparation with storage-only permission, configurable CampusCore URL, route shortcuts, settings UI, policy validation, and packaging CI.

### Changed
- Android production builds now require an explicit HTTPS API origin; a constrained cleartext override exists only for local/emulator development.
- PWA service-worker registration is disabled inside native Capacitor shells.
- Public setup/build documentation now uses the actual `src/CampusCore.Web` paths and repository test/toolchain commands.

### Security
- Production CORS origins are validated as HTTPS origins without credentials, application paths, queries, or fragments.
- Browser companion validation rejects host permissions and content scripts to preserve its least-privilege navigation-only boundary.
