# Release Notes - v1.1.0

## Release type

Minor UI/UX and workflow stabilization release.

## Summary

v1.1.0 stabilizes the post-v1.0.0 user experience across public pages, authenticated workflows, notifications, localization, and main invoice review flows. The release focuses on making the application feel more polished and predictable without changing the core OCR, AI extraction, validation, approval, and audit concepts.

## Completed task list T67-T87

- T67: UI/UX stabilization work started after v1.0.0.
- T68: Public and authenticated layout behavior reviewed.
- T69: Login page polish and public-page behavior improved.
- T70: NotFound and AccessDenied public-page flow improved.
- T71: Application metadata configuration added.
- T72: Footer metadata/version display added.
- T73: Toast notification infrastructure added.
- T74: Common alert feedback migrated toward toast notifications.
- T75: Icon rendering support added through Iconify.
- T76: Sidebar and navigation actions updated to icon-based UI.
- T77: Table and common action buttons updated to icon-based UI.
- T78: Tooltip support initialized for icon actions.
- T79: Invoice Detail header polish completed.
- T80: Invoice Detail Overview tab improved.
- T81: Localization resources updated.
- T82: Public layout language switching verified.
- T83: Authenticated layout language switching verified.
- T84: Main workflow UX pass completed.
- T85: Workflow feedback and action polish completed.
- T86: Main workflows regression-tested.
- T87: README and release notes updated for v1.1.0.

## Added

- `PublicLayout` separation for public Login, NotFound, and AccessDenied pages.
- `ApplicationInfo` configuration for application name, version, and metadata.
- Footer display for application metadata/version.
- Bootstrap toast service and toast container.
- Iconify rendering support for consistent icon-based controls.
- Bootstrap tooltip initialization for icon-only actions.
- v1.1.0 release notes.

## Changed

- Login, NotFound, and AccessDenied no longer render inside the internal authenticated layout.
- Common workflow feedback moved from blocking alerts toward toast notifications.
- Sidebar, navigation, table, and action buttons were converted to icon-oriented controls.
- Invoice Detail header and Overview tab were polished for better scanning and workflow clarity.
- Demo checklist now covers public layout behavior, toasts, icon tooltips, and bilingual switching.

## Fixed

- ConfirmDialog async callback behavior was fixed.
- Public pages no longer expose internal navigation.
- Main invoice review workflows were stabilized after the v1.0.0 baseline.

## Localization

- English and Vietnamese resources were updated for v1.1.0.
- Language switching is available in both authenticated and public layouts.
- Public Login, NotFound, and AccessDenied pages were included in the localization pass.

## Regression testing

- Main workflows were regression-tested in T86.
- Build and automated tests passed during the T86 regression pass.
- Manual checks covered login, dashboard, invoice list/detail, confirm dialog behavior, icon tooltips, toast feedback, logout, public NotFound, public AccessDenied, and English/Vietnamese switching.

## Known notes / follow-up candidates

- PDF invoice support remains a future enhancement.
- Production deployments should replace demo accounts and keep secrets out of committed configuration.
- Additional accessibility review can further validate keyboard behavior and screen reader announcements for icon-only actions and toast notifications.
- More end-to-end browser automation can be added around upload, approval, rejection, and audit-log flows.
