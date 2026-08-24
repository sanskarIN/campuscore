# CampusCore Release Guide

This document defines the release process for CampusCore. A release is a reproducible engineering event, not just a Git tag.

## Versioning

CampusCore follows Semantic Versioning once public stable releases begin:

- `MAJOR`: incompatible API, configuration, or migration expectations;
- `MINOR`: backward-compatible features;
- `PATCH`: backward-compatible fixes and security hardening.

The repository root `VERSION` file is the release version source of truth. Component metadata must match it. Pre-release tags may use identifiers such as `v0.3.0-rc.1` only when every release surface can represent that version safely.

For the current release candidate, `VERSION` is `0.2.0` and the intended tag is `v0.2.0`.

## Release prerequisites

Before creating a release candidate or tag:

- `main` contains only reviewed, intended changes;
- `CHANGELOG.md`, `ROADMAP.md`, `docs/releases/`, and `what_changed.md` describe the real repository state;
- no real credentials, user data, signing keys, or private endpoints are present;
- the API, Web/PWA, Android source/configuration, browser companion, tests, Docker definitions, and documentation all match the intended version;
- every database change is represented by an EF Core migration;
- known blocker or critical defects are resolved;
- dependency and static-analysis findings have been reviewed;
- CI, CodeQL, migration, deployment-smoke, recovery, Android, extension, performance, and version-consistency checks are green for the release commit.

## Version consistency

Run the repository version gate before any release work:

```bash
node scripts/verify-version.mjs
```

For a candidate tag:

```bash
node scripts/verify-version.mjs --tag v0.2.0
```

The check validates the root version against:

- .NET `CampusCoreVersion`, assembly version, and file version;
- Web/PWA package and runtime fallback;
- Compose build default and `.env.example`;
- browser companion package and Manifest V3 version.

The tagged release workflow runs the same check and refuses a mismatched tag.

## Clean verification

Run from a clean checkout with no generated build directories.

```bash
node scripts/verify-version.mjs

dotnet tool restore
dotnet restore CampusCore.sln
dotnet format CampusCore.sln --verify-no-changes
dotnet build CampusCore.sln --configuration Release --no-restore
dotnet test CampusCore.sln --configuration Release --no-build

npm --prefix src/CampusCore.Web install
npm --prefix src/CampusCore.Web run check

VITE_API_BASE_URL=https://api.example.test npm --prefix src/CampusCore.Web run build:android
npm --prefix src/CampusCore.Extension run check

docker compose config --quiet
docker compose build --pull
```

The repository currently has no committed npm lockfile, so automation intentionally uses `npm install`. Move clean/release automation to `npm ci` only after a reviewed lockfile is generated and committed.

Bring up the disposable stack and verify health/readiness:

```bash
docker compose up -d --build --wait
curl --fail http://127.0.0.1:5080/healthz
curl --fail http://127.0.0.1:5080/readyz
curl --fail http://127.0.0.1:8081/readyz
curl --fail http://127.0.0.1:8081/
docker compose down -v
```

Use platform-appropriate HTTP tooling when `curl` is unavailable.

## Database migration review

Restore the repository-local EF tool first:

```bash
dotnet tool restore
```

Generate an idempotent migration script:

```bash
mkdir -p artifacts
dotnet ef migrations script --idempotent \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api \
  --output artifacts/migrations.sql
```

Review the script for destructive operations, long locks, table rewrites, index creation cost, unexpected default values, and data-loss risk. Production backups and rollback procedures must exist before applying a destructive or irreversible migration.

The migration workflow independently produces a reviewable SQL artifact. The release workflow also publishes `migrations.sql` beside the application archives.

As migration history grows, add previous-release database upgrade tests; clean and idempotent migration checks are not a substitute for real upgrade-chain verification.

## Security review

At minimum:

```bash
dotnet list CampusCore.sln package --vulnerable --include-transitive
npm --prefix src/CampusCore.Web audit --audit-level=high
```

Also confirm GitHub CodeQL and repository secret/dependency checks are successful when available.

Review release configuration for:

- a production-only JWT signing key supplied by a secret store;
- explicit allowed hosts;
- explicit HTTPS CORS origins, including the trusted Capacitor origin when Android uses a separately hosted API;
- HTTPS termination and secure proxy headers;
- least-privilege PostgreSQL credentials;
- attachment-storage persistence, quotas, and backup policy;
- log destinations and retention;
- production error responses that contain no stack traces or secrets;
- no Android signing keystore/password committed to Git;
- no browser-extension host permissions/content scripts unless a reviewed feature explicitly requires them.

## Backup and recovery gate

Before tagging a production-intended release:

1. create a backup using `scripts/backup.sh` or `scripts/backup.ps1`;
2. verify the backup checksums and archive structure;
3. confirm the automated recovery round-trip is green;
4. for production upgrades, keep a pre-upgrade backup until the release is considered stable.

See `docs/backup-restore.md`.

## Release artifacts

`.github/workflows/release.yml` produces artifacts from the tagged source only after its build/validation steps succeed.

For `v0.2.0`, expected public GitHub Release files are:

- `campuscore-api-v0.2.0.tar.gz`;
- `campuscore-web-v0.2.0.tar.gz`;
- `campuscore-companion-v0.2.0.zip`;
- `migrations.sql`;
- `VERSION`;
- `SHA256SUMS.txt`.

The checksum file covers all downloadable application archives plus `migrations.sql` and `VERSION`.

The release workflow also regenerates the Android project and assembles a debug APK as a short-lived **workflow verification artifact**. That APK is not attached to the public GitHub Release and must not be presented as a signed production Android build.

A Play-distributable Android App Bundle requires deployment-owner signing configuration and credentials. Those credentials are intentionally outside Git.

Container registries may be added later, but images must use immutable version tags in addition to any moving convenience tag.

## Android release gate

Before Play distribution:

1. confirm the tagged source passed reproducible Android project generation/debug assembly;
2. configure the real production HTTPS API target;
3. configure the trusted Capacitor origin in production CORS when required;
4. supply release signing through an approved secret mechanism;
5. automate an increasing Android `versionCode` and aligned `versionName`;
6. build the signed release/AAB from the same tagged source;
7. install/test the signed candidate on representative devices;
8. verify upgrade behavior from the previous supported Android version;
9. archive signing/recovery material according to the organization's key-management policy.

Read `docs/android.md` for the native generation/runtime contract.

## Browser companion release gate

The tagged release includes a validated companion ZIP, but store publication is a separate decision. Before Chrome Web Store or Edge Add-ons publication:

- add final approved store icons/branding assets;
- verify manifest/store version alignment;
- test the packaged ZIP in each target browser;
- review all requested permissions against actual functionality;
- prepare accurate privacy/data-use disclosures;
- verify support/privacy URLs and screenshots required by the store;
- confirm no development URL is presented as a production default in published onboarding material.

The current companion is navigation-only and storage-only by design.

## Tagging

Only tag a commit after the release-candidate checks pass.

For the prepared v0.2.0 candidate:

```bash
node scripts/verify-version.mjs --tag v0.2.0
git tag -s v0.2.0 -m "CampusCore v0.2.0"
git push origin v0.2.0
```

Do not create the tag while required checks are pending or failing.

If signed tags are not available in the release environment, use the repository's approved signing policy and record the exception.

## Smoke-test checklist

After deployment, verify:

- API liveness and readiness endpoints respond successfully;
- Web/PWA shell loads over HTTPS;
- sign-in works with an authorized fictional test account;
- unauthorized users cannot access protected routes;
- dashboard data loads;
- student search and student detail views work;
- one reversible student or academic mutation succeeds and is audited;
- announcement and attachment paths behave as expected;
- report-card rendering works;
- no browser console errors or unexpected server errors appear;
- security headers are present at the public edge.

When Android is part of the release, also verify the same authenticated journeys in the signed native candidate and check safe areas, keyboard behavior, system navigation, lifecycle/back behavior, and offline/error handling.

When the browser companion is published, verify all shortcuts use the configured CampusCore URL and that the browser permission prompt matches the documented storage-only boundary.

Use fictional release-test records and remove them afterward if the environment is persistent.

## Rollback strategy

Application rollback and database rollback are different concerns.

For application failures:

1. stop further rollout;
2. restore the last known-good immutable application/container artifact;
3. verify health and primary workflows;
4. preserve logs and correlation identifiers for investigation.

For database failures, prefer forward-fix migrations. Only use a database restore or explicit rollback when the migration and operational runbook have been reviewed for data safety.

Never assume application rollback automatically makes a migrated database compatible with an older binary.

For Android, the store may not permit downgrading an installed app version. Treat a bad mobile release as a forward-fix/hotfix exercise and preserve backward API compatibility accordingly.

For the extension, use the browser store's staged rollout/rollback controls when available and avoid sync-storage schema changes that make older extension builds unusable.

## Release notes

Release notes should include:

- user-visible features and fixes;
- security-relevant changes without exploit-enabling detail;
- migration/configuration changes;
- Android/API compatibility notes when native packaging changed;
- extension permission/data-use changes when applicable;
- upgrade and rollback notes;
- known limitations;
- contributor acknowledgements when appropriate;
- support and responsible-disclosure links.

The prepared v0.2.0 notes live in `docs/releases/v0.2.0.md`.

## Post-release

After a successful release:

- change the v0.2.0 changelog marker from release-candidate status to the actual release date;
- create a fresh `[Unreleased]` section for post-v0.2.0 work;
- move completed roadmap items as needed;
- update `what_changed.md` with the tag, commit, verification evidence, and next milestone;
- verify GitHub release artifacts are downloadable and checksums match;
- verify the published `VERSION` and `migrations.sql` correspond to the tag;
- verify any signed Android artifact came from the tagged source and approved signing process;
- verify published extension metadata/permissions match the tagged package;
- monitor error rate, latency, database health, storage growth, and authentication failures;
- create issues for non-blocking follow-up work discovered during release.
