# OAI - OCR & AI Invoice Processing System

OAI is a graduation thesis project that implements a web-based invoice processing system using OCR and AI-assisted information extraction.

The system allows users to upload invoice images, extract text using OCR, parse invoice information into structured data, validate invoice consistency, manually review or correct extracted data, approve or reject invoices, and track all important changes through audit logs.

## Current version

- Current release: v1.3.0
- Focus: Phase 10 upload batch processing, background jobs, PDF processing, source file storage, secure source file APIs, and the source file viewer UI.

Highlights:

- Batch upload processing with Hangfire background jobs
- Upload batch status and detail tracking
- File type detection for Image, PDF, ZIP, and unsupported files
- Text-based PDF embedded text extraction
- Scanned PDF page rendering
- PDF page preview storage
- OCR for rendered PDF pages and merged raw text
- Secure source file download API
- Secure source file preview API
- Source file viewer in Invoice Detail
- Invoice source file list
- Improved source file metadata tracking through InvoiceSourceFiles

## Roadmap notes

- v1.5.0 / Phase 12A: T130 defines the audit anomaly detection dataset schema for invoice-level feature extraction. T131 adds a deterministic generator and committed CSV for 1000 normal audit anomaly samples. Anomaly generation and Logistic Regression model training are planned for later tasks.

## Thesis topic

**English:** Design and implementation of an invoice processing system using OCR and AI-assisted information extraction.

**Vietnamese:** Xây dựng hệ thống xử lý hóa đơn ứng dụng OCR và AI hỗ trợ trích xuất, kiểm tra và quản lý dữ liệu hóa đơn.

## Main goals

- Reduce manual invoice data entry.
- Convert invoice images into structured invoice data.
- Compare rule-based extraction with AI-assisted extraction.
- Keep humans in the review and approval loop.
- Provide auditability for invoice operations.
- Build a maintainable layered .NET web application suitable for a real business workflow.

## Tech stack

- .NET 10
- Blazor Web App with Interactive Server rendering
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server
- Tesseract OCR
- OpenAI API for LLM-based invoice parsing
- Bootstrap 5
- Iconify
- Blazor Server interactivity
- xUnit for unit tests

## Solution structure

```txt
OAI/
├── OAI.Domain
├── OAI.Application
├── OAI.Infrastructure
└── OAI.Web
```

### OAI.Domain

Contains core business models and rules.

Main concepts:

- `Vendor`
- `Invoice`
- `InvoiceLineItem`
- `ValidationIssue`
- `InvoiceExtractionResult`
- `AuditLogEntry`
- `Money` value object
- `InvoiceStatus`
- `ValidationSeverity`

Responsibilities:

- Invoice amount calculation.
- Invoice consistency validation.
- Invoice status transitions.
- Domain-level business rules.

### OAI.Application

Contains use cases, DTOs, and application abstractions.

Main use cases:

- Create invoice
- Update invoice
- Validate invoice
- Approve invoice
- Reject invoice
- Move invoice back to pending review
- Get invoice list/detail
- Compare OCR/rule-based extraction with OCR/AI extraction
- Get dashboard summary
- Get validation issues
- Get audit logs
- Get system settings
- Manage vendors

### OAI.Infrastructure

Contains technical implementations.

Main components:

- EF Core `OaiDbContext`
- SQL Server repositories
- ASP.NET Core Identity persistence
- File storage service
- Tesseract OCR service
- Rule-based invoice text parser
- OpenAI invoice text parser
- Hybrid invoice text parser
- Invoice extraction comparison service
- Audit trail interceptor
- Demo data seeder
- System health service

### OAI.Web

Contains the Blazor UI.

Main screens:

- Login / logout
- Public login layout
- Public not-found/access-denied pages
- Dashboard
- Upload invoice
- Invoice list
- Invoice detail
- Edit invoice
- OCR vs AI comparison
- Validation issues
- Vendor management
- Audit logs
- Settings
- Demo data tools in Development
- System health tools in Development
- Toast notifications
- Icon-based navigation/actions

## Main features

### Invoice processing workflow

```txt
Upload invoice image
→ Save file
→ Run OCR with Tesseract
→ Parse OCR text
   ├── OpenAI parser if enabled and available
   └── Rule-based parser fallback
→ Create invoice record
→ Validate consistency
→ PendingReview
→ Human review/edit
→ Approve or reject
→ Audit tracking
```

### OCR and AI extraction

The system supports a hybrid extraction pipeline:

- Tesseract converts invoice image files into raw text.
- OpenAI parser attempts to convert raw OCR text into structured invoice data.
- Rule-based parser is used as fallback when AI is disabled, unavailable, or fails.
- Extraction results are stored with raw text, structured JSON, engine name, confidence score, attempt number, and status.

### Human-in-the-loop review

The system does not fully trust OCR or AI output. Extracted data is stored for review, then users with the correct permissions can edit normalized invoice data and re-run consistency validation.

### Invoice lifecycle

```txt
PendingReview → Approved
PendingReview → Rejected
Approved → PendingReview
Approved → Rejected
```

Exported invoices are protected from invalid state transitions according to domain rules.

### Validation

The system validates invoice consistency by comparing declared amounts with calculated amounts from line items.

Examples:

- Missing line items
- Subtotal mismatch
- Tax amount mismatch
- Total amount mismatch
- Unresolved error-level issues blocking approval

### Authentication and authorization

The system uses ASP.NET Core Identity with role-based and permission-based authorization.

Default roles:

- `Administrator`
- `Accountant`
- `Auditor`
- `Viewer`

Permission examples:

- `dashboard.view`
- `invoices.view`
- `invoices.upload`
- `invoices.edit`
- `invoices.approve`
- `invoices.reject`
- `vendors.manage`
- `audit_logs.view`
- `settings.view`

### Localization

The UI supports English and Vietnamese using `.resx` resource files.

- Default language: English
- Supported languages: English, Vietnamese
- Language switcher is available in both authenticated and public layouts.
- English and Vietnamese resources are maintained for the current release.

### v1.3.0 highlights

- Upload batch processing with Hangfire background jobs.
- Batch status API and batch detail UI.
- File type detection for image, PDF, ZIP, and unsupported uploads.
- Embedded text extraction for text-based PDFs.
- Scanned PDF page rendering and page preview storage.
- OCR support for rendered PDF pages with merged raw text processing.
- Secure source file download and preview APIs.
- Source file viewer and source file list in Invoice Detail.
- Shared file storage configuration across Web/API/Worker.
- InvoiceSourceFiles metadata tracking.

### Filtering and paging

The system supports filters for operational screens.

Invoice list filters:

- Keyword
- Status
- Vendor
- Issue date from/to
- Paging

Dashboard filters:

- Issue date from/to

Audit log filters:

- Keyword
- Entity
- Action type
- Source
- User name
- Occurred date from/to
- Paging

### Audit logs

Important data changes are captured through an EF Core SaveChanges interceptor.

Audit logs include:

- Entity name
- Entity ID
- Action type
- Old values
- New values
- User ID
- User name
- Source
- Occurred time

Audit detail is displayed through a dialog to keep the audit log list readable.

### Demo and development tools

In Development environment, the system exposes tools for:

- Seeding demo data
- Resetting demo data
- Checking system health

System health checks include:

- Database connectivity
- Pending migrations
- Vendor count
- Invoice count
- Demo invoice count
- Audit log count
- Identity user count
- Identity role count
- File storage configuration
- OCR configuration
- LLM provider/model configuration

## Prerequisites

- .NET 10 SDK
- SQL Server
- Tesseract OCR language data files in `OAI/OAI.Web/tessdata`
- Optional: OpenAI API key for AI-assisted extraction

## Configuration

Development settings are stored in:

```txt
OAI/OAI.Web/appsettings.Development.json
```

Important configuration sections:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=OaiDB;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "FileStorage": {
    "RootPath": "storage",
    "InvoiceFolder": "invoices",
    "MaxFileSizeBytes": 20971520
  },
  "Ocr": {
    "TessDataPath": "tessdata",
    "Languages": "eng+vie"
  },
  "Llm": {
    "Enabled": true,
    "Provider": "OpenAI",
    "Model": "gpt-4.1-mini",
    "MaxInputCharacters": 12000
  }
}
```

Do not commit real API keys to source control. Use user secrets or environment variables.

Example:

```powershell
cd OAI
dotnet user-secrets set "Llm:ApiKey" "sk-..." --project OAI.Web/OAI.Web.csproj
```

To disable AI parsing and use rule-based extraction only:

```json
"Llm": {
  "Enabled": false
}
```

## Database migrations

Run these commands from the repository root:

```powershell
cd OAI
dotnet ef database update --project OAI.Infrastructure/OAI.Infrastructure.csproj --startup-project OAI.Web/OAI.Web.csproj
```

To create a new migration:

```powershell
cd OAI
dotnet ef migrations add MigrationName --project OAI.Infrastructure/OAI.Infrastructure.csproj --startup-project OAI.Web/OAI.Web.csproj --context OaiDbContext
```

## Run the application

From the repository root:

```powershell
cd OAI
dotnet run --project OAI.Web/OAI.Web.csproj
```

Then open the local URL printed by the application.

## Mobile/PWA Capture

- `/mobile/capture` supports camera-friendly invoice capture.
- `/mobile/uploads/{batchId}` shows the mobile processing result.
- `manifest.webmanifest` allows install-like behavior on supported mobile browsers.
- Offline processing is not enabled yet because OAI uses authenticated server/API flows.

## Default development accounts

The Development configuration can seed these users:

| Role | Email | Password |
| --- | --- | --- |
| Administrator | `admin@oai.local` | `Admin@123456` |
| Accountant | `accountant@oai.local` | `Accountant@123456` |
| Auditor | `auditor@oai.local` | `Auditor@123456` |
| Viewer | `viewer@oai.local` | `Viewer@123456` |

These accounts are for local development and demonstration only. Change or remove them before production use.

## Run tests

Run all tests:

```powershell
cd OAI
dotnet test
```

Run a specific test project:

```powershell
cd OAI
dotnet test OAI.Domain.Tests/OAI.Domain.Tests.csproj
dotnet test OAI.Application.Tests/OAI.Application.Tests.csproj
```

## Demo checklist

A recommended demo flow:

1. Verify public Login page does not expose internal navigation.
2. Log in as Administrator or Accountant.
3. Open Dashboard and show date range filters.
4. Upload a sample invoice image or PDF and confirm batch status feedback appears.
5. Verify toast notifications.
6. Open Invoice List and use filters.
7. Open Invoice Detail.
8. Show Overview, Line Items, Validation, and Extraction History tabs.
9. Verify icon tooltips.
10. Open source file preview and extraction result detail to show source content, raw text, and structured JSON.
11. Edit invoice data and revalidate.
12. Approve or reject the invoice.
13. Open Audit Logs and view audit detail dialog.
14. Verify English/Vietnamese switching on public and authenticated pages.
15. Open system settings or health tools in Development.

## Current thesis contribution

This project demonstrates:

1. A practical invoice processing workflow using OCR and AI.
2. A layered .NET implementation with separation of concerns.
3. Hybrid extraction using rule-based and AI-assisted parsing.
4. Validation logic for invoice consistency.
5. Human-in-the-loop correction and approval.
6. Authentication and role/permission-based authorization.
7. Audit logging for traceability.
8. Localization using `.resx`.
9. Filtering, paging, demo data, and system health support.

## Notes

- The project supports image and PDF invoice uploads, with ZIP upload packages processed as batches.
- OpenAI API billing is separate from ChatGPT Plus.
- If OpenAI fails or quota is insufficient, the system can fall back to rule-based parsing.
- Keep secrets out of `appsettings.json`.
