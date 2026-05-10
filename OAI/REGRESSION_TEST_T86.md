# T86 Regression Test: Main Workflows

## Date

2026-05-10

## Branch

master

## Environment

- OS: Windows 10.0.26200, win-x64
- .NET SDK: 10.0.300-preview.0.26177.108
- Solution root: `D:\learn\Citd\KhoaLuan\source\oai\OAI`
- Web app: `OAI.Web`
- ASP.NET Core environment: Development
- Launch profile: `http`
- App URL: `http://localhost:5242`
- Database: local SQL Server via `Server=.;Database=OaiDB;Trusted_Connection=True;TrustServerCertificate=True`
- Browser: Codex in-app browser
- Seed users used:
  - `admin@oai.local`
  - `viewer@oai.local`

## Build Result

Command:

```powershell
dotnet build
```

Result: Passed

- Projects restored successfully.
- Build completed with `0 Warning(s)` and `0 Error(s)`.

## Test Result

Command:

```powershell
dotnet test
```

Result: Passed

- `OAI.Domain.Tests`: 24 passed, 0 failed, 0 skipped.
- `OAI.Application.Tests`: 22 passed, 0 failed, 0 skipped.
- Total: 46 passed, 0 failed, 0 skipped.

## Manual Workflow Checklist

- [x] App starts successfully on `http://localhost:5242` with the Development profile.
- [x] Login page renders with `PublicLayout`, language selector, and ApplicationInfo footer.
- [x] Admin login succeeds with `admin@oai.local`.
- [x] Dashboard renders after login with sidebar navigation, header controls, footer, and summary widgets.
- [x] Invoice list opens from navigation and renders invoice rows, search/filter controls, icon-only detail actions, and localized labels.
- [x] Invoice detail opens from the invoice list.
- [x] Invoice detail renders polished header/actions, invoice overview, line items, validation issues, extraction history tabs, and footer.
- [x] ConfirmDialog async flow opens from invoice detail reject action.
- [x] ConfirmDialog cancel closes the dialog without changing invoice status.
- [x] Icon-only action buttons expose accessible labels/titles in invoice list and invoice detail.
- [x] Bootstrap tooltip targets are present on icon-only actions through `data-bs-toggle="tooltip"` and title/aria-label values.
- [x] Toast container renders a status toast after running the Development demo-data seed action.
- [x] Demo-data seed action reports existing DEMO data was skipped; no new demo invoices were created during this check.
- [x] Logout works from the authenticated user menu.
- [x] NotFound page renders with `PublicLayout` at `/not-found`.
- [x] Language selector switches public pages from English to Vietnamese.
- [x] Viewer login succeeds with `viewer@oai.local`.
- [x] Viewer access to `/settings` redirects to `/access-denied?returnUrl=%2Fsettings`.
- [x] AccessDenied page renders with `PublicLayout` and localized Vietnamese text.
- [x] Browser console error log is empty during the manual pass.

## Issues Found

None.

Notes:

- The app log emitted `Failed to determine the https port for redirect` while using the `http` launch profile. This did not block the app or tested workflows.
- The demo-data seed toast was tested in an existing database where DEMO invoices already existed, so the seed result was skipped as expected.

## Follow-up Tasks

None required for T86.

## Documentation Follow-up

- T87 updated `README.md` and added `RELEASE_NOTES_v1.1.0.md` to document the v1.1.0 UI/UX and workflow stabilization release.
