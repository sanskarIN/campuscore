# Testing CampusCore

CampusCore uses layered verification so failures are caught as close to their source as possible. The repository should never rely on a single end-to-end suite as its only safety net.

## Quality gates

Run these checks before opening or merging a pull request:

```bash
dotnet restore CampusCore.sln
dotnet format CampusCore.sln --verify-no-changes
dotnet build CampusCore.sln --configuration Release --no-restore
dotnet test CampusCore.sln --configuration Release --no-build

cd src/CampusCore.Web
npm install
npm run check
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

Location: `src/CampusCore.Web/e2e`

Playwright covers high-value browser journeys. `@axe-core/playwright` adds automated accessibility checks, but automation does not replace keyboard, zoom, contrast, screen-reader, and responsive manual review.

Run locally after installing Chromium:

```bash
cd src/CampusCore.Web
npx playwright install --with-deps chromium
npm run test:e2e
```

The current E2E suite uses controlled route mocks for deterministic shell/module coverage. Release-candidate validation should additionally exercise a disposable full stack for business-critical journeys.

Minimum full-stack release-candidate journeys:

1. administrator signs in and reaches the dashboard;
2. administrator creates a student and primary guardian;
3. administrator enrolls the student into a section;
4. authorized staff records attendance and marks;
5. authorized user views a report card;
6. administrator publishes an announcement;
7. administrator changes institution settings and verifies audit history;
8. user signs out and protected routes become inaccessible.

## Android verification

`.github/workflows/android.yml` performs a stronger native reproducibility check than `build:android` alone:

1. install pinned web/Capacitor dependencies;
2. validate and build Android-mode web assets;
3. generate the Android platform from `capacitor.config.ts`;
4. synchronize Capacitor assets/plugins;
5. run Gradle `assembleDebug`;
6. upload the resulting debug APK.

For release candidates, CI assembly is not enough. Install the candidate on representative physical devices/emulators and verify:

- first launch and relaunch;
- authentication and sign-out;
- API connectivity over the production-like HTTPS path;
- display cutouts/safe areas;
- portrait layouts across small and large Android screens;
- keyboard/form behavior;
- offline/error recovery;
- system back/navigation behavior;
- external-link handling;
- upgrade behavior from the previous supported app version.

Signed release/AAB validation remains a separate release gate because signing credentials are intentionally not committed.

## Browser companion verification

`src/CampusCore.Extension/validate.mjs` enforces the current least-privilege contract. CI fails when the package gains host permissions/content scripts, references missing manifest files, leaves Manifest V3, or weakens URL rules without corresponding validator changes.

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

The CI migration job also compares committed migration count with `__EFMigrationsHistory` and verifies startup migration idempotence.

Never edit a previously released migration to change production history. Add a new migration instead.

## Backup/restore verification

The recovery CI job creates a database/upload marker, produces a backup, mutates live state, restores the backup, and checks that the pre-backup state returns. This is a regression guard for the repository scripts, not a substitute for operator rehearsals with production-equivalent volume and storage.

Read `docs/backup-restore.md` before a production restore.

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

## Flaky tests

A flaky test is a defect. Do not repeatedly re-run a failing test until it passes and then merge.

When a test flakes:

1. record the failure and environment;
2. determine whether time, concurrency, external I/O, ordering, or shared mutable state is involved;
3. make the test deterministic or fix the underlying race;
4. add regression coverage for the discovered condition;
5. remove any temporary quarantine before release.

## Coverage philosophy

CampusCore does not optimize for a single percentage. Coverage must be risk-based. Authentication, authorization, academic calculations, student mutations, imports, uploads, audit behavior, migrations, mobile runtime boundaries, and permission boundaries deserve deeper coverage than passive presentation code.

A release candidate is not ready when important behavior remains untested merely because an aggregate line-coverage number is high.
