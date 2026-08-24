# CampusCore

> A secure, accessible, production-oriented student management system built with ASP.NET Core, PostgreSQL, React, TypeScript, and Capacitor.

[![CI](https://github.com/sanskarIN/campuscore/actions/workflows/ci.yml/badge.svg)](https://github.com/sanskarIN/campuscore/actions/workflows/ci.yml)
[![Android](https://github.com/sanskarIN/campuscore/actions/workflows/android.yml/badge.svg)](https://github.com/sanskarIN/campuscore/actions/workflows/android.yml)
[![Browser extension](https://github.com/sanskarIN/campuscore/actions/workflows/extension.yml/badge.svg)](https://github.com/sanskarIN/campuscore/actions/workflows/extension.yml)
[![CodeQL](https://github.com/sanskarIN/campuscore/actions/workflows/codeql.yml/badge.svg)](https://github.com/sanskarIN/campuscore/actions/workflows/codeql.yml)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-sanskarIN-FFDD00?logo=buy-me-a-coffee&logoColor=000000)](https://buymeacoffee.com/sanskarIN)

**Made by the Sanskar**

## Why CampusCore

CampusCore centralizes student records, guardians, enrollment, staff, attendance, leave, marks, grades, report cards, timetables, announcements, reporting, audit history, institution settings, and bulk workflows without turning a school-management project into an unmaintainable monolith.

## Current feature set

- Student and guardian profiles with class/section enrollment
- Academic years, classes, sections, subjects, and subject assignment
- Staff directory and role-based authorization foundation
- Attendance, leave, marks, grades, report-card data, and timetables
- Announcements with validated attachment handling
- Global search, filtering, pagination, transactional bulk-import validation, and export endpoints
- Privacy-conscious dashboard metrics and audit logs
- Institution settings and configurable grading rules
- Responsive React/TypeScript web client with light/dark/system themes
- PWA/offline shell, accessible navigation, loading/empty/error states, and printable report styling
- Capacitor Android packaging with native-runtime safe areas and dedicated APK build verification
- Manifest V3 CampusCore Companion preparation for Chromium browsers with storage-only permission
- PostgreSQL persistence with EF Core migrations and transaction-aware services
- Structured logging, security headers, rate limiting, production configuration validation, health checks, and OpenAPI
- Backup/restore scripts, migration integrity checks, deployment smoke tests, accessibility E2E tests, and bundle budgets

## Supported platforms

- **Web/PWA:** current Chromium, Firefox, and Safari-based browsers on Windows, macOS, Linux, Android, iOS, and iPadOS.
- **Android native shell:** generated from the shared web client with Capacitor; see [`docs/android.md`](docs/android.md).
- **Browser companion preparation:** Chromium Manifest V3 package for Chrome/Edge-compatible browsers; see [`src/CampusCore.Extension/README.md`](src/CampusCore.Extension/README.md).

## Technology

- .NET 9 / ASP.NET Core Web API
- Entity Framework Core + PostgreSQL
- ASP.NET Core Identity + JWT bearer authentication
- React 19 + TypeScript + Vite + React Router
- Capacitor 8 Android runtime
- Vitest + Playwright + axe-core browser accessibility checks
- MSTest + coverlet for .NET tests
- GitHub Actions + CodeQL + Dependabot

## Quick start

Start PostgreSQL and the API locally:

```bash
cp .env.example .env
docker compose up -d postgres
dotnet tool restore
dotnet restore CampusCore.sln
dotnet ef database update --project src/CampusCore.Infrastructure --startup-project src/CampusCore.Api
dotnet run --project src/CampusCore.Api
```

In a second terminal:

```bash
cd src/CampusCore.Web
npm install
npm run dev
```

Open `http://localhost:5173`. API documentation is available in development at `http://localhost:5080/openapi/v1.json`.

For a containerized local stack instead, configure `.env` and run:

```bash
docker compose up -d --build --wait
```

The containerized web app is exposed on the configured `CAMPUSCORE_WEB_BIND` / `CAMPUSCORE_WEB_PORT` values (defaults: `127.0.0.1:8081`).

## Android development

From `src/CampusCore.Web`, copy `.env.android.example` to `.env.android`, configure the API target, and generate the native project:

```bash
npm install
npm run android:init
npm run android:open
```

After web changes, use `npm run android:sync`. The Android CI workflow regenerates the native project and assembles a debug APK from committed source. See [`docs/android.md`](docs/android.md) for emulator networking, CORS, release signing, and troubleshooting.

## Browser extension preparation

The extension source is in `src/CampusCore.Extension`. It intentionally has no content scripts or host permissions and does not store CampusCore credentials/tokens.

```bash
cd src/CampusCore.Extension
npm run check
```

See [`src/CampusCore.Extension/README.md`](src/CampusCore.Extension/README.md) for loading and packaging instructions.

## Development setup

See [`docs/setup.md`](docs/setup.md) and [`docs/development.md`](docs/development.md). Configuration is environment-driven; never commit real credentials.

## Testing and quality

Backend:

```bash
dotnet format CampusCore.sln --verify-no-changes
dotnet test CampusCore.sln --configuration Release
```

Web client:

```bash
cd src/CampusCore.Web
npm install
npm run check
```

Browser journeys:

```bash
cd src/CampusCore.Web
npx playwright install --with-deps chromium
npm run test:e2e
```

See [`docs/testing.md`](docs/testing.md) for database integration, browser, and accessibility checks.

## Build and release

```bash
dotnet publish src/CampusCore.Api/CampusCore.Api.csproj -c Release -o artifacts/api
cd src/CampusCore.Web
npm install
npm run build
```

Tagged releases are built by `.github/workflows/release.yml`. See [`docs/release.md`](docs/release.md) and [`docs/deployment.md`](docs/deployment.md).

## Architecture

CampusCore is a modular monolith using Domain → Application → Infrastructure → API dependencies, with the React PWA as a separate client. The Android package reuses that client through Capacitor, while the browser companion remains a minimal navigation surface. Business rules stay outside HTTP endpoint composition and EF Core mappings stay outside the domain. See [`docs/architecture.md`](docs/architecture.md) and [`docs/adr/`](docs/adr/).

## Security and privacy

- No production secret belongs in Git.
- Authentication and signing secrets are supplied through environment variables, CI secrets, or a secret store.
- PII is intentionally excluded from structured audit detail and application logs.
- File uploads are constrained by allow-list, size, and generated storage names.
- Android production API targets and configured production CORS origins are HTTPS-only.
- The browser companion currently requests only `storage` permission and does not inspect page content.
- See [`SECURITY.md`](SECURITY.md), [`PRIVACY.md`](PRIVACY.md), and [`THREAT_MODEL.md`](THREAT_MODEL.md).

## Contributing

Read [`CONTRIBUTING.md`](CONTRIBUTING.md), follow the code of conduct, add tests with behavior changes, and use focused Conventional Commits where practical.

## License

MIT — see [`LICENSE`](LICENSE).

## Contact and support

- Business: [sanskarin@outlook.in](mailto:sanskarin@outlook.in)
- Business: [sanskarin.business@gmail.com](mailto:sanskarin.business@gmail.com)
- Support: [supportramsandesh@gmail.com](mailto:supportramsandesh@gmail.com)
- GitHub: <https://github.com/sanskarIN>
- Buy Me a Coffee: <https://buymeacoffee.com/sanskarIN>

**Made by the Sanskar**
