# Android development and packaging

CampusCore uses Capacitor to package the existing React/Vite application as an Android application while keeping the browser/PWA codebase authoritative. The generated `src/CampusCore.Web/android/` project is intentionally not committed; it is recreated from the pinned Capacitor configuration and verified in CI.

## Prerequisites

Install the same major toolchain used by CI:

- Node.js 24 or another version satisfying `>=22.12.0`.
- Java 21.
- Android Studio with a current Android SDK, platform tools, and build tools.
- An Android emulator or physical Android device with USB debugging enabled for local testing.

From `src/CampusCore.Web`, install the pinned JavaScript dependencies:

```bash
npm install
```

## Configure the API target

Copy the tracked template:

```bash
cp .env.android.example .env.android
```

For a deployed environment, set an HTTPS API origin:

```env
VITE_API_BASE_URL=https://api.example.edu
VITE_APP_VERSION=0.1.0
```

Android packaging fails before Vite runs when the API target is missing, malformed, contains a path/query/fragment, or uses public cleartext HTTP.

### Android emulator and a host API

The Android emulator reaches the development machine through `10.0.2.2`, not through the emulator's own `localhost`:

```env
VITE_API_BASE_URL=http://10.0.2.2:5080
VITE_APP_VERSION=0.1.0-dev
CAMPUSCORE_ANDROID_ALLOW_HTTP=1
```

The HTTP override is deliberately limited to `localhost`, `127.0.0.1`, or `10.0.2.2`. Do not use it for a deployed build.

## API CORS requirement

Capacitor serves bundled Android web assets from the local secure origin `https://localhost`. When the Android app calls a separately hosted CampusCore API, that API must allow `https://localhost` as a CORS origin.

Docker Compose already maps:

```env
CAMPUSCORE_CORS_ORIGIN_1=https://localhost
```

For production deployments that do not use the repository Compose file, configure the equivalent ASP.NET Core setting:

```text
Cors__Origins__0=https://localhost
```

Additional web origins can be added with the following array indexes. Production startup validation rejects HTTP CORS origins and entries containing paths, queries, fragments, or credentials.

## Generate the native project

The first local generation is:

```bash
npm run android:init
```

That command validates the Android environment, builds the Vite assets in `android` mode, and runs `cap add android`.

After web-code or configuration changes, synchronize the existing generated project:

```bash
npm run android:sync
```

Open it in Android Studio with:

```bash
npm run android:open
```

Inspect Capacitor environment health with:

```bash
npm run android:doctor
```

## Runtime behavior

The shared web application detects whether it is running in a native Capacitor shell. On Android it:

- marks the document with the native runtime so Android-specific safe-area styling can apply;
- respects display cutouts and bottom/side safe-area insets;
- avoids registering the PWA service worker inside the native shell;
- requires an explicit API URL rather than silently falling back to device-local `localhost`;
- keeps normal text selection disabled for shell UI while preserving selection/editing in inputs, textareas, selects, and editable regions.

The browser/PWA behavior remains unchanged outside a native runtime.

## Debug APK verification

`.github/workflows/android.yml` regenerates the Android project on GitHub Actions whenever relevant web/native files change. It then runs a Capacitor sync and Gradle `assembleDebug`. The resulting `app-debug.apk` is uploaded as the `campuscore-android-debug` workflow artifact for short-lived verification.

This validates that the committed web/configuration source can produce an Android project without relying on a developer's locally generated Gradle tree.

## Release signing

Do not commit signing keystores, passwords, service-account credentials, or Play Console credentials.

For a production release:

1. Generate/supply the organization-controlled Android signing key outside Git.
2. Configure Gradle signing through local secret properties or CI secrets.
3. Build an Android App Bundle (`.aab`) for Play distribution.
4. Verify the release build points only at the intended HTTPS API.
5. Run the release build on supported physical devices and emulators before upload.
6. Store the signing key and recovery material in an organization-controlled secret-management process.

The repository intentionally stops short of committing a real signing identity because signing credentials are deployment-specific secrets.

## Troubleshooting

### Android app cannot reach an API running on the computer

Use `http://10.0.2.2:5080` for the standard Android emulator, enable `CAMPUSCORE_ANDROID_ALLOW_HTTP=1` only for that local build, and ensure the API is reachable from the host firewall/network configuration.

### Browser reports a CORS error in the native app

Confirm the API has `https://localhost` in `Cors:Origins`. If production startup rejects a CORS value, ensure it is a plain HTTPS origin without a route or trailing application path.

### `cap add android` reports the platform already exists

Use `npm run android:sync` instead. `android:init` is only for creating a fresh generated Android project.

### Native project becomes inconsistent

Because the Android tree is generated, it can be deleted and rebuilt from the committed source:

```bash
rm -rf android
npm run android:init
```

Do not delete a native project containing uncommitted manual Android changes unless those changes are no longer needed or have been migrated into reproducible Capacitor configuration/plugin code.
