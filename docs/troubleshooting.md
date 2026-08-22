# CampusCore Troubleshooting

Use this guide to diagnose common local-development and deployment problems without exposing credentials or private student data.

## Start with versions

From the repository root:

```bash
dotnet --info
node --version
npm --version
docker --version
docker compose version
```

The repository `global.json` and `src/CampusCore.Web/package.json` define the expected .NET and Node.js ranges. Prefer those versions before investigating framework-specific errors.

## .NET restore fails

Symptoms:

- package source cannot be reached;
- NU1101/NU1301-style restore failures;
- build fails because `project.assets.json` is missing.

Checks:

```bash
dotnet nuget list source
dotnet restore CampusCore.sln --force-evaluate
```

Verify network/proxy configuration and that the public NuGet source is available. Do not add private credentials to repository files to work around a restore problem.

## Node/npm install fails

Use the lockfile-driven install:

```bash
npm --prefix src/CampusCore.Web ci
```

If the lockfile and `package.json` disagree, do not use `--force` as a permanent fix. Regenerate the lockfile intentionally with the supported Node/npm version, review dependency changes, and commit the result with the package manifest change.

## API stops at startup with a JWT error

CampusCore refuses to start when `Jwt:Key` is absent or shorter than 32 UTF-8 bytes.

For local development, use the development-only configuration already provided by the repository or an environment override. For production, inject a strong secret through the deployment secret store.

Never commit the real production signing key.

## API cannot connect to PostgreSQL

Typical symptoms include connection-refused, DNS, authentication, or migration errors during startup.

Check:

```bash
docker compose ps
docker compose logs postgres
docker compose logs api
```

Confirm that the configured connection string points to the correct host for the execution environment. Inside Docker Compose, the database host is the service name rather than `localhost`.

If running the API directly on the host, use the host-exposed PostgreSQL port documented in the setup guide.

## Database migration fails

First identify the current migration state:

```bash
dotnet ef migrations list \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api
```

Then inspect an idempotent script before making manual changes:

```bash
dotnet ef migrations script --idempotent \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api
```

Do not delete migration history rows or edit a released migration merely to make a local database appear healthy. For disposable local data, recreating the local database is usually safer. For persistent data, back up first and diagnose the exact failed migration.

## Port is already in use

Check the ports in `docker-compose.yml` and the local launch configuration. Stop the conflicting process or change the local port mapping rather than changing public API route contracts.

Examples:

```bash
docker compose ps
# Linux/macOS
lsof -i :8080
# Windows PowerShell
Get-NetTCPConnection -LocalPort 8080
```

## Browser shows CORS errors

Development CORS only allows configured origins. Confirm the browser origin exactly matches an entry in `Cors:Origins`, including scheme and port.

Do not solve CORS problems in production by enabling arbitrary origins together with credentials.

## Web app loads but API calls fail

Check `VITE_API_BASE_URL` in the Web/PWA environment configuration. The value must be reachable from the browser, not only from another container.

Use the browser network panel to distinguish:

- DNS/connectivity failure;
- CORS rejection;
- 401 unauthenticated response;
- 403 authorization response;
- 404 route mismatch;
- 409 safe conflict response;
- 429 rate limit;
- 5xx server error.

Do not paste authorization headers, JWTs, or student response payloads into public bug reports.

## Sign-in returns 401

Verify the account is enabled and the credentials are correct. CampusCore intentionally avoids detailed authentication errors that would disclose account existence.

If using a fresh local database, review the setup documentation for the fictional development seed process. Change any development bootstrap password before using a persistent shared environment.

## A protected page is visible but an action returns 403

The Web/PWA may render navigation useful to several roles while the API remains authoritative. A 403 generally means the authenticated account lacks the server-required role.

Do not weaken the API authorization policy just to make the UI action succeed. Align the UI permission hint with the server policy instead.

## Attachment upload fails

Check:

- permitted extension/content type;
- configured size limit;
- available storage capacity;
- persistence/volume permissions;
- whether the request is authenticated and authorized.

Never rename a prohibited executable to an allowed extension as a workaround. Upload validation is a security boundary.

## Service worker or PWA appears stale

A previously installed service worker can keep an older shell while a new deployment is available.

In browser developer tools:

1. inspect the active service worker;
2. verify the deployed asset URLs and cache version;
3. reload with the network available;
4. unregister the local service worker only when diagnosing development behavior.

Production update behavior should remain user-safe; do not instruct users to clear all browser data as the normal update mechanism.

## Docker health check fails

Inspect the individual service logs and health status:

```bash
docker compose ps
docker inspect --format='{{json .State.Health}}' <container>
docker compose logs --tail=200 api web postgres
```

A healthy container process is not enough if the application cannot reach required dependencies. Fix readiness dependencies rather than replacing the health check with a command that always succeeds.

## CI fails but local checks pass

Compare:

- exact SDK/Node versions;
- clean restore vs cached local dependencies;
- case-sensitive file paths;
- generated files that are present locally but untracked;
- environment variables;
- line endings and formatter output.

Reproduce CI from a clean checkout when possible. Never disable a quality gate solely because it found a failure that does not reproduce immediately.

## Reporting a problem safely

Include:

- operating system and architecture;
- .NET, Node, npm, Docker versions;
- the command that failed;
- the smallest relevant sanitized error excerpt;
- whether the problem occurs from a clean checkout;
- whether Docker or host-native setup is used.

Remove or replace:

- passwords and secrets;
- JWTs and cookies;
- connection strings containing credentials;
- real student, guardian, staff, or institution data;
- private hostnames/IP addresses when they are sensitive.

For suspected security vulnerabilities, follow `SECURITY.md` instead of opening a public issue.
