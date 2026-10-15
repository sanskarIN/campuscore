# Accessibility Standard

CampusCore is intended for administrators, teachers, and staff who may use keyboards, touch screens, screen readers, magnification, high-contrast settings, reduced-motion settings, or combinations of assistive technology. Accessibility is a product requirement and a release gate, not a final visual polish task.

## Target

The Web/PWA should follow WCAG 2.2 AA-oriented practices for all primary workflows. This document is an engineering checklist rather than a legal conformance claim.

## Semantic structure

- Use landmarks such as `header`, `nav`, `main`, `aside`, and `footer` according to page structure.
- Keep one descriptive page-level `h1`; maintain logical heading order beneath it.
- Use native buttons for actions and native links for navigation.
- Use tables only for tabular data and provide meaningful column headers.
- Use lists for list semantics instead of visually repeated generic containers when possible.
- Keep DOM order aligned with reading and keyboard order.

## Keyboard behavior

Every interactive feature must be usable without a pointer.

- Preserve visible focus indicators.
- Never use positive `tabindex` values to repair a poor DOM order.
- Do not create keyboard traps in dialogs, menus, drawers, or custom widgets.
- Escape should close dismissible overlays when that matches user expectations.
- After dialogs close, return focus to the invoking control when it still exists.
- Skip links should let keyboard users move directly to main content.
- Global shortcuts must not conflict with text input and should be documented when added.

## Forms

- Every control has a programmatically associated label.
- Required state is conveyed in text/semantics, not only by color or an asterisk.
- Validation errors identify the field and explain how to correct the value.
- On failed submission, focus or an error summary should make the failure discoverable.
- Preserve valid user-entered data after validation failures.
- Give examples or format hints before users make predictable formatting mistakes.
- Do not disable paste in password or verification fields.

## Status and feedback

Loading, offline, success, warning, and error states must be perceivable without relying only on color.

Use polite live regions for non-urgent asynchronous status. Use assertive announcements sparingly for errors that require immediate attention.

Avoid announcing rapidly changing decorative progress values that create screen-reader noise.

## Color and contrast

- Text and interactive controls must maintain sufficient contrast in light and dark themes.
- Focus rings must remain visible against adjacent backgrounds.
- Charts and status badges need labels, patterns, icons, or text in addition to color.
- Disabled controls must remain identifiable without becoming unreadable.
- Do not encode attendance, marks, or risk states using red/green alone.

## Text and zoom

- Layouts should remain usable at browser zoom levels up to at least 200%.
- Avoid fixed heights for text containers that clip localized or enlarged content.
- Use responsive reflow instead of forcing horizontal scrolling for the entire page.
- Data tables may scroll horizontally within their own region when necessary; preserve row/column context.
- Do not use images of text for core information.

## Motion

Respect `prefers-reduced-motion`.

- Remove non-essential animation and parallax when reduced motion is requested.
- Never flash content at unsafe frequencies.
- Loading skeletons should not create distracting indefinite motion.
- Functional state changes must remain clear when animations are removed.

## Touch and pointer input

- Primary touch targets should be comfortably sized and separated.
- Do not require hover to discover essential actions or information.
- Drag interactions need a non-drag alternative when introduced.
- Avoid precision-only gestures for common workflows.

## Dialogs and overlays

When modal behavior is necessary:

- provide an accessible name;
- move focus into the dialog intentionally;
- keep focus inside while modal;
- provide an obvious close/cancel control;
- restore focus after dismissal;
- prevent background content from being exposed as interactive to assistive technology.

Prefer simple inline workflows over unnecessary modal dialogs.

## Navigation and routing

Single-page navigation should update the document title and make the new page context discoverable. Route changes must not silently leave keyboard focus in a removed or unrelated control.

Authenticated session expiry should present an understandable recovery path rather than repeatedly redirecting without explanation.

## Tables and dense administration screens

CampusCore includes data-heavy workflows. For each table:

- provide an accessible name or nearby heading;
- use semantic header cells;
- make sort direction programmatically available;
- label row actions with the record context, for example `Edit Ada Sharma` rather than repeated `Edit` when practical;
- preserve keyboard access to filters, pagination, and row actions;
- expose empty and filtered-zero-result states in text;
- avoid forcing users to interpret truncated identifiers when full values are necessary.

## Charts and analytics

Dashboard visualizations require equivalent textual information.

- Give each chart a descriptive heading/label.
- Summarize the key value or trend in text.
- Provide a table or list for data that users may need to inspect precisely.
- Avoid exposing very small privacy-sensitive cohorts in either visual or text alternatives.

## PWA and offline states

Offline status should be clear but non-disruptive. Users must understand whether they are viewing cached shell content, current server data, or a failed network operation.

CampusCore does not claim offline mutation synchronization unless the feature is explicitly implemented and tested.

## Manual release review

For each release candidate, test at minimum:

1. complete sign-in and sign-out using keyboard only;
2. navigate every primary sidebar/top-level destination with keyboard only;
3. create/edit a student using keyboard only;
4. use search, filters, pagination, and at least one table row action;
5. exercise one error state and one empty state;
6. zoom to 200% on a desktop-sized viewport;
7. test a narrow mobile viewport;
8. test light and dark themes;
9. enable reduced motion;
10. run one screen-reader smoke test on the primary administrator journey.

Recommended combinations include NVDA + Firefox/Chrome on Windows and VoiceOver + Safari on Apple platforms when available.

## Automated checks

Automated accessibility tooling is useful for catching missing labels, invalid ARIA, contrast issues, and structural errors, but it cannot prove usability.

Component and end-to-end tests should assert accessible roles/names for important controls. Add an automated accessibility scanner when the browser test harness is introduced, then retain manual keyboard and screen-reader review.

## Accessibility defects

Treat blockers in authentication, navigation, form completion, destructive-action recovery, report access, or core student/academic workflows as release blockers.

When fixing an accessibility regression, add automated coverage where the behavior can be expressed reliably and record any manual verification steps in the pull request.
