# Production deployment

This guide describes the production contract for the repository-provided Docker Compose deployment. The default `docker-compose.yml` is intentionally safe for local development: it uses `Development` unless overridden and binds published ports to loopback.

## Production architecture

A recommended single-host topology is:

```text
Internet
   |
TLS reverse proxy / load balancer
   |
127.0.0.1:8081  CampusCore Web (nginx)
   |
Docker network
   +--> CampusCore API :8080
   +--> PostgreSQL :5432
```

Only the TLS edge should be publicly reachable. Do not publish PostgreSQL or the API directly to an untrusted network. The Web container proxies `/api/`, `/healthz`, and `/readyz` to the API.

## Required production configuration

CampusCore refuses to start in `Production` when known development placeholders are still present. Configure production values through a secret manager, protected environment file, orchestrator secret, or equivalent mechanism.

At minimum set:

```text
ASPNETCORE_ENVIRONMENT=Production
POSTGRES_DB=campuscore
POSTGRES_USER=<dedicated database user>
POSTGRES_PASSWORD=<strong unique database password>
CAMPUSCORE_JWT_ISSUER=CampusCore
CAMPUSCORE_JWT_AUDIENCE=CampusCore.Web
CAMPUSCORE_JWT_KEY=<random signing secret of at least 32 characters>
CAMPUSCORE_ALLOWED_HOSTS=campus.example.edu
CAMPUSCORE_WEB_BIND=127.0.0.1
CAMPUSCORE_WEB_PORT=8081
```

For a brand-new database, also set a strong one-time bootstrap key:

```text
CAMPUSCORE_BOOTSTRAP_KEY=<random one-time bootstrap secret>
```

After the first administrator account has been created, remove `CAMPUSCORE_BOOTSTRAP_KEY` from the production environment and recreate/restart the API container. The bootstrap endpoint is then fail-closed.

Never use the values containing `development-only`, `local-only`, `change-before-production`, or `replace-with` in production. The API rejects these markers when `ASPNETCORE_ENVIRONMENT=Production`.

## Host filtering

`CAMPUSCORE_ALLOWED_HOSTS` must be an explicit semicolon-separated ASP.NET Core host list in production, for example:

```text
CAMPUSCORE_ALLOWED_HOSTS=campus.example.edu;www.campus.example.edu
```

Wildcard `*` is intentionally rejected in production.

## Network exposure

The base Compose file binds all published ports to `127.0.0.1` by default. Keep these defaults on a single-host deployment:

- PostgreSQL: `127.0.0.1:5432`
- API: `127.0.0.1:5080`
- Web: `127.0.0.1:8081`

If an external reverse proxy runs on the same host, point it to `http://127.0.0.1:8081`.

If a reverse proxy runs in Docker, prefer attaching it to the Compose network and address the `web` service directly instead of exposing database/API ports. Do not change `CAMPUSCORE_DB_BIND` or `CAMPUSCORE_API_BIND` to `0.0.0.0` unless a trusted network design explicitly requires it and firewall policy is in place.

## TLS

Terminate TLS at a maintained reverse proxy or load balancer and redirect public HTTP to HTTPS there. Use a trusted certificate and modern TLS configuration. The repository Nginx container is an application/static-file proxy; it is not configured as the public certificate endpoint.

At the public edge, preserve or add these response protections as appropriate:

- `Strict-Transport-Security` after HTTPS is verified for the domain;
- `X-Content-Type-Options: nosniff`;
- `Referrer-Policy: no-referrer`;
- frame-embedding protection;
- the application Content Security Policy.

Do not enable HSTS on a domain until HTTPS works reliably for every intended subdomain covered by the chosen policy.

## First deployment

1. Prepare the production host with Docker Engine and Docker Compose v2.
2. Copy only the tagged/reviewed CampusCore source or release deployment bundle to the host.
3. Configure production secrets outside source control.
4. Validate interpolation before starting services:

```bash
docker compose config --quiet
```

5. Build and start:

```bash
docker compose up -d --build --wait --wait-timeout 240
```

6. Confirm service state:

```bash
docker compose ps
curl --fail http://127.0.0.1:5080/healthz
curl --fail http://127.0.0.1:5080/readyz
curl --fail http://127.0.0.1:8081/readyz
```

7. Access the public HTTPS URL through the TLS edge and complete first-run administrator setup using the bootstrap key.
8. Remove the bootstrap key and recreate the API:

```bash
docker compose up -d --force-recreate api
```

9. Confirm sign-in still works and bootstrap is no longer available without a configured key.
10. Create and verify the first production backup. See `docs/backup-restore.md`.

## Health probes

CampusCore exposes two intentionally different probes:

- `/healthz`: process liveness only. It does not query PostgreSQL.
- `/readyz`: readiness including PostgreSQL connectivity.

Use liveness to decide whether a process is stuck and readiness to decide whether it can receive application traffic. The Compose API health check uses `/readyz`; the image-level health check uses `/healthz`.

## Database migrations

The API applies committed EF Core migrations during startup before serving normal workloads. Before each production upgrade:

1. verify a recent backup;
2. review migration changes in the release;
3. schedule maintenance for migrations that can lock or rewrite large tables;
4. deploy one migration-running API instance at a time unless the migration has been proven safe under concurrent startup;
5. wait for `/readyz` before routing traffic.

CI verifies that a clean database receives every committed migration and that restarting the API does not create additional migration-history entries.

When the repository gains more than one historical migration, release testing should also exercise a copy of the previous release database through the full upgrade chain.

## Attachment storage

The Compose deployment persists uploads in `campuscore-uploads`. The API image seeds `/data/uploads` with ownership for its non-root `campuscore` user.

Do not replace the named volume with ephemeral container storage. If using a host bind mount, create the directory with permissions that allow the container's `campuscore` user to write while preventing unrelated host users from reading student documents.

## Backups and disaster recovery

Use the supported backup tools rather than copying a live PostgreSQL data directory:

```bash
./scripts/backup.sh /secure/campuscore-backups
./scripts/verify-backup.sh /secure/campuscore-backups/campuscore-<timestamp>
```

PowerShell equivalents are provided in `scripts/backup.ps1`, `scripts/verify-backup.ps1`, and `scripts/restore.ps1`.

Back up both PostgreSQL and attachment storage. Keep at least one encrypted/off-host copy and run periodic restore drills. Full procedures and retention guidance are in `docs/backup-restore.md`.

## Upgrade procedure

For a routine version upgrade:

1. read `CHANGELOG.md` and release notes;
2. create and verify a backup;
3. fetch the intended immutable tag/release artifact;
4. review configuration additions or removals;
5. build/pull the new application version;
6. deploy the API and wait for `/readyz`;
7. deploy/recreate the Web service;
8. exercise sign-in, dashboard, student search, a representative mutation, attachment download, report card, and audit log;
9. keep the pre-upgrade backup until the release is considered stable.

Example for the repository Compose stack:

```bash
docker compose build --pull api web
docker compose up -d api --wait --wait-timeout 240
docker compose up -d web
curl --fail http://127.0.0.1:8081/readyz
```

## Rollback

Application rollback and data rollback are separate decisions.

If a new binary is faulty but database compatibility is preserved, redeploy the previous immutable application version and verify health.

If a migration changed data/schema incompatibly, do not assume an older binary is safe. Prefer a reviewed forward fix. Use `scripts/restore.sh`/`restore.ps1` only when restoring the entire selected recovery point is the intended data-loss decision.

## Logging and monitoring

At minimum monitor:

- API process/container restarts;
- `/readyz` failures and PostgreSQL availability;
- HTTP 5xx rates;
- authentication failures and account lockouts;
- storage capacity for PostgreSQL, Docker volumes, and backup destinations;
- backup/restore-drill success;
- latency for search, dashboard, report-card, and attachment operations.

Centralized logs should have access controls and retention appropriate for student data. Do not log JWTs, passwords, bootstrap keys, uploaded document contents, or unrestricted request bodies.

## Production security checklist

Before opening access:

- production startup validation passes;
- JWT key is random, unique, and stored outside Git;
- bootstrap key is removed after first administrator creation;
- `AllowedHosts` is explicit;
- PostgreSQL/API are not publicly exposed;
- TLS is enforced at the public edge;
- backups are encrypted and restore-tested;
- administrator accounts follow least privilege;
- dependency/CodeQL checks are reviewed;
- browser E2E and automated accessibility checks pass;
- the tagged release artifacts and checksums match the deployment source.

## Scaling beyond one host

The repository Compose file is designed primarily for a single-host deployment. Before horizontal scaling, externalize PostgreSQL and attachment storage, define a shared data-protection/session strategy where required, ensure migrations have single-writer coordination, configure trusted proxy/network boundaries, and move secrets into the target orchestrator's secret system.
