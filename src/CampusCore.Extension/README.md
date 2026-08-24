# CampusCore Companion

CampusCore Companion is the browser-extension preparation target for CampusCore. The current package is a Chromium Manifest V3 companion that opens trusted CampusCore routes without reading page content or authentication data.

## Current scope

- Dashboard, Students, Global Search, and Announcements shortcuts.
- Configurable CampusCore web origin stored with browser sync storage.
- HTTPS required for deployed instances; localhost HTTP is allowed only for development.
- No content scripts.
- No host permissions.
- No browsing-history access.
- No password or CampusCore token storage.
- Manifest and URL-policy validation in CI.

## Local validation

From this directory:

```bash
npm run check
```

The package has no third-party runtime or development dependencies.

## Load unpacked in Chrome or Edge

1. Open the browser extensions management page.
2. Enable Developer mode.
3. Choose **Load unpacked**.
4. Select `src/CampusCore.Extension`.
5. Open **Extension settings** and configure the HTTPS CampusCore web origin.

The repository CI also creates a `campuscore-companion.zip` artifact from the validated files.

## Security model

The extension is intentionally a navigation companion rather than an API client. Authentication remains inside the normal CampusCore web application. Future extension features must preserve least privilege: add a browser permission only when a specific reviewed feature cannot work without it.

Made by the Sanskar.
