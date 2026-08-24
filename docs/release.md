# CampusCore Release Guide

This document defines the release process for CampusCore. A release is a reproducible engineering event, not just a Git tag.

## Versioning

CampusCore follows Semantic Versioning once public stable releases begin:

- `MAJOR`: incompatible API, configuration, or migration expectations;
- `MINOR`: backward-compatible features;
- `PATCH`: backward-compatible fixes and security hardening.

Pre-release tags may use identifiers such as `v0.2.0-rc.1`.

## Release prerequisites

Before creating a release candidate:

- `main` contains only reviewed, intended changes;
- `CHANGELOG.md`, `ROADMAP.md`, and `what_changed.md` describe the real repository state;
- no real credentials, user data, signing keys, or private endpoints are present;
- the API, Web/PWA, Android source/configuration, browser companion, tests, Docker definitions, and documentation all match the intended version;
- every database change is represented by an EF Core migration;
- known blocker or critical defects are resolved;
- dependency and static-analysis findings have been reviewed;
- CI, CodeQL, migration, deployment-smoke, recovery, Android, and extension checks are green for the release commit.

## Clean verification

Run from a clean checkout with no generated build directories.

```bash
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

The repository currently has no committed npm lockfile, so automation uses `npm install`. Move clean/release automation to `npm ci` only after a reviewed lockfile is committed.

Bring up the disposable stack and verify health:

```bash
docker compose up -d --build --wait
curl --fail http://127.0.0.1:5080/healthz
curl --fail http://127.0.0.1:8081/
docker compose down -v
```

Use platform-appropriate HTTP tooling when `curl` is unavailable.

## Database migration review

Generate an idempotent migration script:

```bash
mkdir -p artifacts
dotnet ef migrations script --idempotent \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api \
  --output artifacts/migrations.sql
```

Review the script for destructive operations, long locks, table rewrites, index creation cost, unexpected default values, and data-loss risk. Production backups and rollback procedures must exist before applying a destructive or irreversible migration.

The committed migration workflow and release artifacts do not remove the need to review upgrade behavior from the previous supported production schema.

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
- explicit HTTPS CORS origins, including `https://localhost` when Android will use a separately hosted API;
- HTTPS termination and secure proxy headers;
- least-privilege PostgreSQL credentials;
- attachment-storage persistence, quotas, and backup policy;
- log destinations and retention;
- production error responses that contain no stack traces or secrets;
- no Android signing keystore/password committed to Git;
- no browser-extension host permissions/content scripts unless a reviewed feature explicitly requires them.

## Release artifacts

`.github/workflows/release.yml` produces artifacts from the tagged source only after its build/validation steps succeed.

Published GitHub Release files currently include:

- published ASP.NET Core API archive;
- built Web/PWA static-assets archive;
- validated Manifest V3 CampusCore Companion ZIP;
- SHA-256 checksum file covering the downloadable API/Web/extension archives.

The release workflow also regenerates the Android project and assembles a debug APK as a short-lived **workflow verification artifact**. That APK is not attached to the public GitHub Release and must not be presented as a signed production Android build.

A Play-distributable Android App Bundle requires the deployment owner's signing configuration and credentials. Those credentials are intentionally outside Git.

Container registries may be added later, but images must use immutable version tags in addition to any moving convenience tag.

## Android release gate

Before Play distribution:

1. confirm the tagged source passed the reproducible Android assembly gate;
2. configure the real production HTTPS API target;
3. configure `https://localhost` CORS on that API when the native shell calls it cross-origin;
4. supply release signing through an approved secret mechanism;
5. build the signed release/AAB from the same tagged source;
6. install/test the signed candidate on representative devices;
7. verify upgrade behavior from the previous supported Android version;
8. archive signing/recovery material according to the organization's key-management policy.

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

```bash
git tag -s v0.2.0 -m "CampusCore v0.2.0"
git push origin v0.2.0
```

If signed tags are not available in the release environment, use the repository's approved signing policy and record the exception.

## Smoke-test checklist

After deployment, verify:

- API readiness endpoint responds successfully;
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

When Android is part of the release, also verify the same authenticated journeys in the signed native candidate and check safe areas, keyboard behavior, system navigation, and offline/error handling.

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

For the extension, use the browser store's staged rollout/rollback controls when available and avoid schema changes in sync storage that make older extension builds unusable.

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

## Post-release

After a successful release:

- update `CHANGELOG.md` from `Unreleased` to the released version/date;
- move completed roadmap items;
- update `what_changed.md` with the tag, commit, verification evidence, and next milestone;
- verify GitHub release artifacts are downloadable and checksums match;
- verify any signed Android artifact came from the tagged source and approved signing process;
- verify published extension metadata/permissions match the tagged package;
- monitor error rate, latency, database health, storage growth, and authentication failures;
- create issues for non-blocking follow-up work discovered during release.
