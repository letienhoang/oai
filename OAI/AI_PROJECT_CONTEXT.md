# OAI Project Context

## Project
OAI - Invoice Processing System for graduation thesis.

## Tech Stack
- .NET 10 preview
- Blazor Web App
- EF Core 10
- SQL Server
- Tesseract OCR
- OpenAI API for LLM extraction
- Bootstrap 5.3.3

## Architecture
- OAI.Domain
- OAI.Application
- OAI.Infrastructure
- OAI.Web

## Main Features Completed
- Invoice domain model
- Vendor, Invoice, InvoiceLineItem, ValidationIssue, InvoiceExtractionResult
- File upload
- File storage
- OCR wrapper with Tesseract
- Rule-based invoice text parser
- OpenAI invoice text parser
- Hybrid parser: OpenAI first, RuleBased fallback
- Upload invoice flow
- Invoice list
- Invoice detail
- Edit extracted invoice data
- Validation issue screen
- Dashboard
- OCR vs OCR+AI comparison
- Approve invoice
- Reject invoice
- Move invoice back to PendingReview
- Audit logs screen
- Read-only settings screen
- Collapsible/resizable sidebar layout

## Invoice Lifecycle

```txt
Upload/OCR/AI → PendingReview → Edit if needed → Validate → Approved
```

Alternative flow:

```txt
PendingReview / Approved → Rejected → PendingReview
```

## Current Status
Finished Phase 6A: Complete business workflow and UI.

Next phase: Phase 6B - Testing, experiment, and thesis report.

## Important Notes
- Store datetime values as UTC in database.
- Convert datetime to browser/user timezone in Blazor UI through UserTimeZoneService.
- Keep API keys out of appsettings.json. Use user secrets or environment variables.
- OpenAI API quota/billing is separate from ChatGPT Plus.
- If OpenAI fails or quota is insufficient, the system falls back to rule-based parsing.

## Important Runtime Configuration

Example appsettings.Development.json:

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
  }
}
```

API key should be configured using user secrets:

```bash
dotnet user-secrets set "Llm:ApiKey" "sk-..." --project OAI.Web
```

## Completed Screens
- `/` Dashboard
- `/invoices/upload` Upload invoice
- `/invoices` Invoice list
- `/invoices/{InvoiceId}` Invoice detail
- `/invoices/{InvoiceId}/edit` Edit invoice
- `/invoices/{InvoiceId}/compare` OCR vs OCR+AI comparison
- `/invoices/validation` Validation issues
- `/audit-logs` Audit logs
- `/settings` Read-only system settings

## Current Next Tasks
- T36. Write tests for Domain rules
- T37. Write tests for Application use cases
- T38. Test upload → OCR → parse → validate flow
- T39. Prepare evaluation dataset
- T40. Summarize experiment results
- T41. Write thesis report
- T42. Prepare demo checklist
