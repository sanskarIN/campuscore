# CampusCore

> A secure, accessible, production-oriented student management system built with ASP.NET Core, PostgreSQL, React, and TypeScript.

[![CI](https://github.com/sanskarIN/campuscore/actions/workflows/ci.yml/badge.svg)](https://github.com/sanskarIN/campuscore/actions/workflows/ci.yml)
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
- Announcements with attachment metadata
- Global search, filtering, pagination, bulk-import validation, and export endpoints
- Privacy-conscious dashboard metrics and audit logs
- Institution settings and configurable grading rules
- Responsive React/TypeScript web client with light/dark/system themes
- PWA/offline shell, accessible navigation, loading/empty/error states, and printable report styling
- PostgreSQL persistence with EF Core migrations and transaction-aware services
- Structured logging, security headers, rate limiting, secret-safe configuration, health checks, and OpenAPI

## Screenshots

Real release screenshots are captured from the running application and stored under `docs/screenshots/`. Until the first tagged release is deployed, see the UI implementation in `apps/web/src`.

## Supported platforms

CampusCore is delivered as a responsive **Web/PWA** application and is designed for current Chromium, Firefox, and Safari-based browsers on Windows, macOS, Linux, Android, iOS, and iPadOS.

## Technology

- .NET 9 / ASP.NET Core Web API
- Entity Framework Core + PostgreSQL
- ASP.NET Core Identity + JWT bearer authentication
- React 19 + TypeScript + Vite
- TanStack Query
- Vitest + Testing Library + Playwright
- xUnit + FluentAssertions
- GitHub Actions + CodeQL + Dependabot

## Quick start

```bash
cp .env.example .env
docker compose up -d postgres
dotnet restore CampusCore.sln
dotnet ef database update --project src/CampusCore.Infrastructure --startup-project src/CampusCore.Api
dotnet run --project src/CampusCore.Api
```

In a second terminal:

```bash
cd apps/web
npm ci
npm run dev
```

Open `http://localhost:5173`. API documentation is available in development at `http://localhost:5080/openapi/v1.json`.

## Development setup

See [`docs/setup.md`](docs/setup.md) and [`docs/development.md`](docs/development.md). Configuration is environment-driven; never commit real credentials. The local compose file starts PostgreSQL only, so the API and web client retain fast local hot reload.

## Testing and quality

```bash
dotnet format CampusCore.sln --verify-no-changes
dotnet test CampusCore.sln --configuration Release
cd apps/web && npm ci && npm run lint && npm run typecheck && npm run test:run && npm run build
```

For browser journeys:

```bash
cd apps/web
npx playwright install --with-deps chromium
npm run test:e2e
```

See [`docs/testing.md`](docs/testing.md) for database integration and accessibility checks.

## Build and release

```bash
dotnet publish src/CampusCore.Api/CampusCore.Api.csproj -c Release -o artifacts/api
cd apps/web && npm ci && npm run build
```

Tagged releases are built by `.github/workflows/release.yml`. See [`docs/release.md`](docs/release.md).

## Architecture

CampusCore is a modular monolith using Domain → Application → Infrastructure → API dependencies, with the React PWA as a separate client. Business rules stay outside controllers and EF Core mappings stay outside the domain. See [`docs/architecture.md`](docs/architecture.md) and [`docs/adr/`](docs/adr/).

## Security and privacy

- No production secret belongs in Git.
- Authentication secrets are supplied through environment variables or a secret store.
- PII is intentionally excluded from structured audit detail and application logs.
- File uploads are constrained by allow-list, size, and generated storage names.
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
