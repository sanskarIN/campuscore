# Testing CampusCore

CampusCore uses layered verification so failures are caught as close to their source as possible. The repository should never rely on a single end-to-end suite as its only safety net.

## Quality gates

Run these checks before opening or merging a pull request:

```bash
node scripts/verify-version.mjs

dotnet tool restore
dotnet restore CampusCore.sln
dotnet format CampusCore.sln --verify-no-changes
dotnet build CampusCore.sln --configuration Release --no-restore
dotnet test CampusCore.sln --configuration Release --no-build

cd src/CampusCore.Web
npm install
npm run check
```

To verify the same-origin production Web artifact shape used by releases:

```bash
cd src/CampusCore.Web
version="$(cat ../../VERSION)"
VITE_API_BASE_URL=/ VITE_APP_VERSION="$version" npm run build
npm run verify:release
```

For Android-impacting web/native changes:

```bash
cd src/CampusCore.Web
VITE_API_BASE_URL=https://api.example.test npm run build:android
```

For browser-companion changes:

```bash
cd src/CampusCore.Extension
npm run check
```

When Docker is available, also validate the deployment definition:

```bash
docker compose config --quiet
```

The GitHub Actions workflows are the authoritative clean-environment checks because they restore dependencies and regenerate deployment/native artifacts from committed source.

## Test pyramid

### Domain unit tests

Location: `tests/CampusCore.Domain.Tests`

Domain tests cover business invariants that do not need a database or HTTP host. They should be deterministic, fast, and independent of wall-clock timing wherever possible.

Required coverage includes:

- student identity and admission-data validation;
- academic-year date boundaries;
- attendance and mark value constraints;
- grade-scale range validation;
- enrollment and timetable invariants as those rules evolve.

Every production bug caused by a domain invariant should receive a regression test when the behavior can be reproduced at this layer.

### Application tests

Application-service tests should verify use cases such as student creation, guardian changes, enrollment, attendance updates, mark recording, report-card calculation, search pagination, imports, and audit emission.

Prefer an isolated relational test database over mocking Entity Framework query behavior. PostgreSQL-backed integration tests are required for PostgreSQL-specific constraints or SQL semantics.

### API tests

Location: `tests/CampusCore.Api.Tests`

API-focused tests cover validation/security helpers and should expand toward real ASP.NET Core routing, authentication, authorization, validation, safe problem responses, rate-limit behavior where practical, and security headers.

Integration tests must not depend on production credentials. Use a disposable PostgreSQL database or a CI service/container seeded only with fictional data.

Critical API scenarios include:

- anonymous requests cannot reach protected student or administration routes;
- invalid sign-in attempts never disclose whether an account exists;
- role-protected routes reject insufficient privileges;
- duplicate admission numbers return a safe conflict response;
- invalid attachment metadata or oversized uploads are rejected;
- search and bulk operations enforce configured limits;
- health checks report readiness without exposing secrets;
- production configuration rejects unsafe secrets, hosts, and CORS origins.

### Web unit and component tests

Location: `src/CampusCore.Web/src/**/*.test.{ts,tsx}`

Vitest is the browser-client test runner. New UI behavior should be factored so parsing, formatting, permission decisions, runtime boundaries, reducers, query construction, and other deterministic logic can be tested without network access.

Behavior-oriented coverage should include:

- accessible names and labels where components are rendered in tests;
- loading, empty, error, and success states;
- role-based decisions;
- expired-session behavior;
- service-worker privacy boundaries;
- native/web runtime selection;
- URL and API error handling.

### Browser end-to-end and accessibility tests

CampusCore now has two intentionally different Playwright suites.

#### Deterministic mocked suite

Location: `src/CampusCore.Web/e2e`

This suite controls API routes so UI/auth/authorization/accessibility regressions are deterministic and fast. It covers authentication routing, role boundaries, primary module rendering, offline shell behavior, keyboard navigation, and axe-powered WCAG A/AA smoke checks.

Run locally after installing Chromium:

```bash
cd src/CampusCore.Web
npx playwright install --with-deps chromium
npm run test:e2e
```

#### Real full-stack release smoke

Location: `src/CampusCore.Web/fullstack`

Configuration: `src/CampusCore.Web/playwright.fullstack.config.ts`

Workflow: `.github/workflows/fullstack-e2e.yml`

The workflow starts a completely disposable Docker Compose stack with a fresh PostgreSQL volume, runs Chromium against the real Nginx/Web/API stack, and destroys all volumes afterward.

The current real-stack journey exercises:

1. first-run administrator bootstrap through the real authentication UI;
2. student creation through the Web UI;
3. primary guardian persistence through the authenticated API where no guardian-creation UI exists yet;
4. academic year, class, section, and subject setup through authenticated API setup calls;
5. student enrollment through the real Operations UI;
6. attendance and mark recording through the real Academics UI;
7. report-card generation/rendering through the real Operations UI;
8. announcement publication through the real Web UI;
9. institution settings persistence and audit-event verification;
10. sign-out and restoration of the protected-route boundary.

Run against an already-running disposable stack:

```bash
cd src/CampusCore.Web
CAMPUSCORE_E2E_BASE_URL=http://127.0.0.1:8081 \
CAMPUSCORE_E2E_BOOTSTRAP_KEY=campuscore-fullstack-bootstrap-key-2026 \
npm run test:e2e:fullstack
```

Never point this destructive first-run suite at a shared, staging, or production database. It assumes an empty database and creates fictional records.

Both `e2e` and `fullstack` are part of the strict TypeScript project graph through `tsconfig.e2e.json`, and both are linted by the normal Web quality gate.

Automated accessibility does not replace keyboard, zoom, contrast, screen-reader, responsive, and touch-device manual review.

## Android verification

`.github/workflows/android.yml` performs a stronger native reproducibility check than `build:android` alone:

1. load the application version from the root `VERSION` file;
2. install pinned web/Capacitor dependencies;
3. validate and build Android-mode web assets;
4. generate the Android platform from `capacitor.config.ts`;
5. synchronize Capacitor assets/plugins;
6. run Gradle `assembleDebug`;
7. upload the resulting debug APK.

For release candidates, CI assembly is not enough. Install the candidate on representative physical devices/emulators and verify:

- first launch and relaunch;
- authentication and sign-out;
- API connectivity over the production-like HTTPS path;
- display cutouts/safe areas;
- portrait layouts across small and large Android screens;
- keyboard/form behavior;
- offline/error recovery;
- system back/navigation and lifecycle behavior;
- external-link handling;
- upgrade behavior from the previous supported app version.

Signed release/AAB validation remains a separate release gate because signing credentials are intentionally not committed.

## Browser companion verification

`src/CampusCore.Extension/validate.mjs` enforces the current least-privilege contract. CI fails when the package gains host permissions/content scripts, references missing manifest files, leaves Manifest V3, weakens URL rules without corresponding validator changes, or lets package/manifest versions drift.

Before store publication, also test unpacked packages in supported Chrome and Edge versions and review:

- shortcut navigation;
- settings persistence;
- invalid URL feedback;
- keyboard focus/order;
- light and dark OS appearance;
- install/update behavior;
- requested permissions shown by the browser;
- store privacy disclosures against actual behavior.

## Database and migration verification

A release candidate must prove both clean installation and upgrade safety.

Restore the repository-local EF tool first:

```bash
dotnet tool restore
```

For a clean database:

```bash
dotnet ef database update \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api
```

For migration review, generate an idempotent SQL script and inspect it before deployment:

```bash
dotnet ef migrations script --idempotent \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api \
  --output artifacts/migrations.sql
```

The CI migration job compares committed migration count with `__EFMigrationsHistory`, verifies startup migration idempotence, and the migration-script workflow uploads reviewable SQL.

As soon as multiple released database versions exist, add explicit previous-release database upgrade tests. Clean-database verification alone cannot prove upgrade compatibility.

Never edit a previously released migration to change production history. Add a new migration instead.

## Backup/restore verification

The recovery CI job creates a database/upload marker, produces a backup, mutates live state, restores the backup, and checks that the pre-backup state returns. This is a regression guard for the repository scripts, not a substitute for operator rehearsals with production-equivalent volume and storage.

Read `docs/backup-restore.md` before a production restore.

## Deployment and release-asset verification

The production deployment smoke workflow starts CampusCore with `ASPNETCORE_ENVIRONMENT=Production`, explicit non-placeholder secrets/hosts, and HTTPS-only CORS test origins. It verifies API/Web liveness/readiness and non-root application users.

The Web release-asset verifier fails if non-source-map release assets contain known local/development deployment markers such as the local API origin.

The version-consistency workflow validates the root `VERSION` value across .NET, Web, Compose, environment examples, and the browser companion.

## Security checks

CI should run maintained dependency and static-analysis checks. Locally, also review:

```bash
dotnet list CampusCore.sln package --vulnerable --include-transitive
npm --prefix src/CampusCore.Web audit --audit-level=high
```

Treat audit output as evidence that needs review, not as an automatic guarantee. Confirm exploitability and affected runtime paths before accepting or suppressing findings.

## Test data rules

- Use fictional people, institutions, email addresses, and identifiers only.
- Do not copy production databases into local or CI test environments.
- Keep fixtures deterministic and minimal.
- Do not commit JWT signing keys, passwords, API tokens, signing keystores, or real attachment data.
- Randomized/property tests must print a reproducible seed on failure.
- Full-stack first-run E2E must run only against a disposable empty database.

## Flaky tests

A flaky test is a defect. Do not repeatedly re-run a failing test until it passes and then merge.

When a test flakes:

1. record the failure and environment;
2. determine whether time, concurrency, external I/O, ordering, or shared mutable state is involved;
3. make the test deterministic or fix the underlying race;
4. add regression coverage for the discovered condition;
5. remove any temporary quarantine before release.

## Coverage philosophy

CampusCore does not optimize for a single percentage. Coverage must be risk-based. Authentication, authorization, academic calculations, student mutations, imports, uploads, audit behavior, migrations, mobile runtime boundaries, recovery behavior, and permission boundaries deserve deeper coverage than passive presentation code.

A release candidate is not ready when important behavior remains untested merely because an aggregate line-coverage number is high.
