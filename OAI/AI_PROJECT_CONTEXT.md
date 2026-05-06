# OAI Project Context

## 1. Project Summary

**OAI** is a graduation thesis project: an invoice processing system that uses OCR and AI-assisted extraction to convert invoice images into structured invoice data, validate the data, support human review/correction, and keep an audit trail.

The current implementation is a layered .NET monolith with a Blazor Web App UI.

Main thesis direction:

```txt
Upload invoice image
→ Store file
→ OCR with Tesseract
→ Parse invoice text using AI-first / rule-based fallback
→ Validate invoice consistency
→ Human review and correction
→ Approve / reject / move back to review
→ Audit traceability
```

Vietnamese thesis title currently suitable for the project:

```txt
Xây dựng hệ thống xử lý hóa đơn ứng dụng OCR và AI hỗ trợ trích xuất, kiểm tra và quản lý dữ liệu hóa đơn
```

## 2. Tech Stack

- .NET 10 preview
- ASP.NET Core / Blazor Web App with Interactive Server rendering
- EF Core 10
- SQL Server
- ASP.NET Core Identity
- Tesseract OCR
- OpenAI-compatible LLM extraction service
- Bootstrap 5.3.3
- `.resx` localization with English default and Vietnamese support

## 3. Solution Architecture

Projects:

```txt
OAI.Domain
OAI.Application
OAI.Infrastructure
OAI.Web
```

### OAI.Domain

Contains business model, domain behavior, value objects, enums, and domain exceptions.

Important domain objects:

- `Vendor`
- `Invoice`
- `InvoiceLineItem`
- `ValidationIssue`
- `InvoiceExtractionResult`
- `AuditLogEntry`
- `Money`
- `InvoiceStatus`

Invoice statuses:

```txt
Draft
PendingReview
Approved
Rejected
Exported
```

### OAI.Application

Contains use cases, DTOs, abstractions, message codes, filter DTOs, and orchestration contracts.

Important use cases include:

- Create invoice
- Update invoice
- Validate invoice
- Get invoice list/detail
- Approve invoice
- Reject invoice
- Move invoice to pending review
- Compare OCR vs OCR+AI extraction
- Get dashboard summary
- Get validation issues
- Get audit logs
- Vendor list/options/upsert
- System settings

### OAI.Infrastructure

Contains EF Core persistence, repository implementations, file storage, OCR service, AI parser, audit interceptor, identity seed, demo data seed/reset, and system health services.

Important services:

- `OaiDbContext`
- `AuditTrailInterceptor`
- `FileStorageService`
- `TesseractOcrService`
- `RuleBasedInvoiceTextParser`
- `OpenAiInvoiceTextParser`
- `HybridInvoiceTextParser`
- `InvoiceExtractionComparisonService`
- `IdentityDataSeeder`
- `DemoDataSeeder`
- `SystemHealthService`

### OAI.Web

Blazor Web App UI with localized pages, authentication, role/permission-based authorization, dashboard, invoice workflow screens, vendor management, audit tools, and dev tools.

## 4. Current Feature Snapshot

### Invoice Processing

Completed:

- Invoice upload page
- File storage integration
- OCR wrapper using Tesseract
- Rule-based invoice text parser
- AI/LLM parser
- Hybrid parser: AI-first with rule-based fallback
- Invoice create flow from uploaded image
- Invoice list with filters
- Invoice detail page with tabs
- Edit extracted invoice data
- Revalidation after edit
- Approve invoice
- Reject invoice
- Move invoice back to PendingReview
- OCR vs OCR+AI comparison screen
- Extraction history detail dialog with raw OCR text and structured JSON

### Validation and Workflow

Completed:

- Validation issue list screen
- Validation messages localized through message codes
- Action-level authorization checks for upload/edit/approve/reject/move
- Confirmation dialogs for important actions
- Loading, empty, and error states across screens

### Dashboard and Filtering

Completed:

- Dashboard summary
- Dashboard date range filter
- Invoice list keyword/status/vendor/date filters
- Audit log filters: keyword, entity, action, source, user, date range
- Repository-level filtered queries for invoice list, dashboard, validation issues, vendors, and audit logs

### Vendor Management

Completed:

- Vendor management page at `/vendors`
- Vendor list/search/pagination
- Create/update vendor
- Vendor permissions
- Vendor dropdown in invoice edit screen
- Quick-create vendor dialog from invoice edit/upload flow

### Authentication and Authorization

Completed:

- ASP.NET Core Identity foundation
- Identity user model
- Default roles:
  - `Administrator`
  - `Accountant`
  - `Auditor`
  - `Viewer`
- Default demo users for each role
- Permission claims and authorization policies
- Page-level and action-level authorization
- Access denied page
- TopBar auth menu

Important default development users:

```txt
admin@oai.local       / Admin@123456       / Administrator
accountant@oai.local  / Accountant@123456  / Accountant
auditor@oai.local     / Auditor@123456     / Auditor
viewer@oai.local      / Viewer@123456      / Viewer
```

### Localization

Completed:

- `.resx` localization foundation
- English default resources
- Vietnamese resources
- Language switcher in TopBar
- Localized UI labels, actions, validation/action/system messages
- Localized domain/application message codes where needed

Use localization pattern:

```razor
@L["Key"]
```

Avoid:

```razor
@L.Key
```

### Audit Logs

Completed:

- EF Core audit interceptor
- Audit logs include entity/action/old values/new values/source/time
- User information added to audit logs through `ICurrentUserContext`
- Audit log list page
- Audit log filters
- Audit log detail dialog with pretty-printed JSON

### Dev Tools and Demo Support

Completed:

- Demo data seed service and development-only UI
- Demo data reset/cleanup tool
- Development-only page: `/dev-tools/demo-data`
- System health/status panel
- Development-only page: `/dev-tools/system-health`
- Demo data includes vendors, invoices, line items, validation issues, and extraction history

The demo tools are only for Development and Administrator users.

## 5. Important Routes

```txt
/                              Dashboard
/login                         Login
/access-denied                 Access denied
/invoices/upload               Upload invoice
/invoices                      Invoice list
/invoices/{InvoiceId}          Invoice detail
/invoices/{InvoiceId}/edit     Edit invoice
/invoices/{InvoiceId}/compare  OCR vs OCR+AI comparison
/invoices/validation           Validation issues
/vendors                       Vendor management
/audit-logs                    Audit logs
/settings                      Read-only system settings
/dev-tools/demo-data           Development demo seed/reset tools
/dev-tools/system-health       Development system health panel
```

## 6. Runtime Configuration

Example `appsettings.Development.json` sections:

```json
{
  "FileStorage": {
    "BasePath": "",
    "RootPath": "storage",
    "InvoiceFolder": "invoices",
    "MaxFileSizeBytes": 20971520
  },
  "Ocr": {
    "BasePath": "",
    "TessDataPath": "tessdata",
    "Languages": "eng+vie"
  },
  "Llm": {
    "Enabled": true,
    "Provider": "OpenAI",
    "Model": "gpt-4.1-mini",
    "MaxInputCharacters": 12000
  },
  "IdentitySeed": {
    "Users": [
      {
        "Email": "admin@oai.local",
        "Password": "Admin@123456",
        "DisplayName": "OAI Administrator",
        "Role": "Administrator",
        "IsActive": true
      }
    ]
  },
  "DemoDataSeed": {
    "Enabled": true,
    "ResetBeforeSeed": false,
    "InvoiceNumberPrefix": "DEMO"
  }
}
```

API keys must not be committed to `appsettings.json`. Use user secrets or environment variables:

```bash
dotnet user-secrets set "Llm:ApiKey" "sk-..." --project OAI.Web
```

## 7. Database and EF Core Notes

- Main DbContext: `OaiDbContext`
- Identity is integrated into the same DbContext using ASP.NET Core Identity tables.
- Audit logs are created through `AuditTrailInterceptor`.
- Decimal precision for `Money.Amount` should be explicitly configured with EF Core precision, currently expected as `decimal(18,2)` for money fields.
- Date/time values should be stored in UTC when applicable.
- UI should convert date/time values to user/browser timezone through `UserTimeZoneService` where supported.

Common EF commands:

```bash
dotnet ef migrations add <MigrationName> --project OAI.Infrastructure --startup-project OAI.Web --context OaiDbContext

dotnet ef database update --project OAI.Infrastructure --startup-project OAI.Web --context OaiDbContext
```

## 8. Development and Demo Workflow

Recommended local demo flow:

```txt
1. Run the web app in Development.
2. Login as admin@oai.local.
3. Open /dev-tools/system-health and verify status.
4. Open /dev-tools/demo-data and seed demo data.
5. Demo Dashboard, Invoice List filters, Invoice Detail tabs, Extraction History, Audit Logs, Vendor Management.
6. Reset demo data when needed.
```

Important: `MapDemoDataEndpoints()` only maps endpoints. It does not automatically seed data on startup. Use `/dev-tools/demo-data` or the development-only endpoint to trigger seed/reset.

## 9. Current Project Status

The project has moved beyond the old Phase 6A/6B context. The current state is:

```txt
Phase 7A: Localization and authentication foundation - completed
Phase 7B: Authorization, roles, users, and audit user info - completed
Phase 7C: Filtering and data scope - completed
Phase 7D: UX/detail improvements and vendor management - completed
Phase 7E: Demo/dev tooling and system health - completed
```

Recently completed task range includes approximately T43-T67:

- Localization with `.resx`
- Language switcher
- Localized validation/action/system messages
- ASP.NET Core Identity
- Role/permission policies
- Default users/roles
- Action-level authorization
- Audit user information
- Filter DTOs and repository query filters
- Invoice list/dashboard/audit filters
- Vendor dropdown and vendor management
- Quick-create vendor dialog
- Audit detail dialog
- Confirmation dialogs
- Invoice detail tabs
- Extraction history detail
- Demo data seed/reset tools
- System health/status panel
- EF decimal precision fix for `Money.Amount`

`NEXT_TASKS.md` and `TASK_HISTORY.md` were removed because this file now acts as the compact current-state context.

## 10. Recommended Next Focus

The next practical focus should be finalization for thesis/demo quality:

```txt
1. Run full build and test suite.
2. Verify EF migrations are clean and database update works from scratch.
3. Prepare demo checklist and screenshots.
4. Prepare evaluation dataset for OCR-only vs OCR+AI.
5. Summarize extraction accuracy and observed limitations.
6. Write thesis implementation chapter based on current architecture.
7. Write thesis experiment/evaluation chapter.
8. Polish UI text and verify EN/VI localization completeness.
9. Review security boundaries before any production-like deployment.
```

Potential technical follow-up tasks:

```txt
- Add field-level audit change view instead of raw JSON only.
- Improve OCR/AI comparison screen layout.
- Persist filters in query string.
- Add user management page for administrators.
- Add export/report features.
- Add PDF invoice support.
- Add Docker deployment and GitHub Actions CI.
```
