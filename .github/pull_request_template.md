## What changed

Describe the focused change and why it belongs in CampusCore.

## Verification

- [ ] `dotnet format CampusCore.sln --verify-no-changes`
- [ ] `dotnet build CampusCore.sln --configuration Release -warnaserror`
- [ ] `dotnet test CampusCore.sln --configuration Release`
- [ ] `cd src/CampusCore.Web && npm run check` (when web code changes)
- [ ] `docker compose config --quiet` (when deployment files change)

## Quality review

- [ ] No secrets, credentials, real student/staff data, or private endpoints were added.
- [ ] Authorization and audit behavior were reviewed for sensitive changes.
- [ ] Loading, empty, success, and error states were considered where relevant.
- [ ] Keyboard, focus, labels, contrast, and reduced-motion behavior were considered for UI changes.
- [ ] Database changes include migrations and migration notes where applicable.
- [ ] Documentation and `what_changed.md` were updated when behavior or setup changed.

## Screenshots / notes

Add sanitized screenshots for UI changes, or explain why they are not applicable.
