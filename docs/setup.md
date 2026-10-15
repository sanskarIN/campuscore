# CampusCore Setup

## Prerequisites

For local web/API development install:

- Git
- .NET SDK 9.x compatible with `global.json`
- Node.js 24.x
- npm 10+ (bundled with current Node releases)
- Docker Desktop or Docker Engine + Compose plugin
- PostgreSQL client tools are optional but useful

For native Android work also install Java 21 and Android Studio with a current Android SDK/platform/build-tools installation. See `docs/android.md` before generating the native project.

The default development ports are:

- Web/PWA: `http://localhost:5173`
- API: `http://localhost:5080`
- PostgreSQL: `localhost:5432`
- Full Compose Web/PWA: `http://localhost:8081`

## Clone and configure

```bash
git clone https://github.com/sanskarIN/campuscore.git
cd campuscore
cp .env.example .env
```

The committed environment files contain only local examples. Never copy production credentials into source-controlled files.

## Local hot-reload setup

### 1. Start PostgreSQL

```bash
docker compose up -d postgres
docker compose ps
```

The default local database values are intentionally development-only and may be overridden through `.env`.

### 2. Configure API secrets

Use environment variables or .NET user-secrets. The important settings are:

```text
ConnectionStrings__Database
Jwt__Issuer
Jwt__Audience
Jwt__Key
BootstrapAdmin__Key
Cors__Origins__0
Cors__Origins__1
Storage__RootPath
```

For a user-secrets workflow:

```bash
dotnet user-secrets init --project src/CampusCore.Api
dotnet user-secrets set "Jwt:Key" "<generate-a-long-random-development-key>" --project src/CampusCore.Api
dotnet user-secrets set "BootstrapAdmin:Key" "<generate-a-separate-random-development-key>" --project src/CampusCore.Api
```

Do not reuse the Compose development defaults for a deployed environment. Production startup rejects wildcard `AllowedHosts`, known development secret placeholders, and unsafe configured CORS origins.

### 3. Restore and migrate

```bash
dotnet tool restore
dotnet restore CampusCore.sln
dotnet ef database update \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api
```

The repository tool manifest pins the compatible `dotnet-ef` command.

### 4. Start the API

```bash
dotnet run --project src/CampusCore.Api
```

Check:

```text
http://localhost:5080/healthz
http://localhost:5080/readyz
http://localhost:5080/openapi/v1.json   (development)
```

### 5. Start the Web/PWA

In another terminal:

```bash
cd src/CampusCore.Web
cp .env.example .env.local
npm install
npm run dev
```

Open `http://localhost:5173`.

`VITE_API_BASE_URL` defaults to `http://localhost:5080` for normal browser-local development.

## First administrator

The login screen includes **First-run setup**. This calls the guarded `/api/auth/bootstrap` endpoint and works only while no user exists.

Enter:

- administrator display name;
- administrator email;
- a password satisfying the server Identity policy;
- the bootstrap key configured on the API.

After the first account exists, the bootstrap endpoint rejects subsequent attempts. Remove or rotate the bootstrap secret after initial provisioning.

## Full Compose stack

To build and run PostgreSQL, API, and Web/PWA together:

```bash
docker compose up --build -d --wait
```

Then open:

```text
http://localhost:8081
```

The containerized web app proxies `/api/*` to the API, so browser requests remain same-origin. Compose also supplies local development CORS entries for the Vite development origin and the Capacitor native origin.

For anything beyond a local machine, override at minimum:

```bash
POSTGRES_PASSWORD='<strong-random-value>' \
CAMPUSCORE_JWT_KEY='<long-random-signing-key>' \
CAMPUSCORE_BOOTSTRAP_KEY='<separate-random-bootstrap-key>' \
CAMPUSCORE_ALLOWED_HOSTS='campus.example.edu' \
CAMPUSCORE_CORS_ORIGIN_0='https://localhost' \
docker compose up --build -d --wait
```

Only keep `https://localhost` when a Capacitor Android client will call this API. Add each additional public web origin explicitly. Prefer an external secret manager/orchestrator rather than shell history for production credentials.

## Android setup

From `src/CampusCore.Web`:

```bash
cp .env.android.example .env.android
```

Set `VITE_API_BASE_URL` to the intended HTTPS API origin, then:

```bash
npm install
npm run android:init
npm run android:open
```

For the standard emulator talking to an API on the development computer, use the documented `10.0.2.2` development configuration in `docs/android.md`.

After a generated native project exists, use `npm run android:sync` rather than `android:init`.

## Browser companion setup

No third-party install is required for the extension package itself. Validate it with:

```bash
cd src/CampusCore.Extension
npm run check
```

For unpacked browser loading and configuration, see `src/CampusCore.Extension/README.md`.

## Verify the checkout

Backend:

```bash
dotnet format CampusCore.sln --verify-no-changes
dotnet build CampusCore.sln --configuration Release -warnaserror
dotnet test CampusCore.sln --configuration Release
```

Web/PWA:

```bash
cd src/CampusCore.Web
npm run check
```

Android source/configuration:

```bash
cd src/CampusCore.Web
VITE_API_BASE_URL=https://api.example.test npm run build:android
```

Browser companion:

```bash
cd src/CampusCore.Extension
npm run check
```

Compose:

```bash
docker compose config --quiet
```

## Reset local data

This destroys local development data:

```bash
docker compose down -v
```

Then start PostgreSQL and apply migrations again.

Do not use this command against a production deployment or any environment whose data must be retained.

## Windows notes

- Run commands from PowerShell or Windows Terminal.
- Docker Desktop must be running before Compose commands.
- Use the Windows equivalent of `cp` (`Copy-Item`) when needed.
- Android Studio can invoke the generated Gradle wrapper directly after `npm run android:open`.
- If script execution policy blocks local tooling, prefer invoking the executable directly rather than lowering machine-wide security policy.

## macOS/Linux notes

- Ensure the Docker daemon is running and your user can access it.
- Do not run the application as root merely to work around file permissions.
- When using a remote PostgreSQL instance, require encrypted connections according to the provider's guidance.

## Next steps

Read:

- `docs/development.md` for coding workflow;
- `docs/testing.md` for quality gates;
- `docs/android.md` for native Android packaging;
- `docs/architecture.md` for design boundaries;
- `docs/troubleshooting.md` for common failures;
- `docs/release.md` before deploying or tagging a release.
