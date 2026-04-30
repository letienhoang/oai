# Task History - OAI

## Phase 1: Domain Foundation
- Designed core invoice domain.
- Created main entities: Vendor, Invoice, InvoiceLineItem, ValidationIssue, InvoiceExtractionResult.
- Created value object: Money.
- Created enums: InvoiceStatus, ValidationSeverity.
- Discussed sealed classes and value object design.
- Designed entity relationships and domain skeleton.

## Phase 2: Application Layer
- T7: Created DTOs for input/output.
- T8: Created repository and service interfaces.
- T9: Built CreateInvoiceUseCase.
- T10: Built upload and invoice processing use case/service flow.
- T11: Built consistency validation use case.
- T12: Built invoice detail and list use cases.
- Added use-case-oriented Application structure.
- Added Application dependency injection registration.

## Phase 3: Infrastructure Layer
- T13: Created DbContext and EF Core mappings.
- T14: Created initial migration and database schema.
- T15: Implemented repositories: InvoiceRepository, VendorRepository, UnitOfWork.
- Fixed EF Include query type issue by filtering before Include.
- T16: Integrated invoice file storage.
- Used IHostEnvironment instead of IWebHostEnvironment for Infrastructure.
- Fixed FileStorageOptions binding issue.
- T17: Created OCR service wrapper with Tesseract.
- Installed Tesseract 5.2.0.
- T18: Integrated OCR wrapper into invoice extraction flow.
- T19: Added logging and audit trail setup.
- Added AuditTrailInterceptor and audit-related infrastructure.
- Later moved audit entities to Domain for cleaner architecture.

## Phase 4: Web Layer with Blazor
- T20: Created basic layout and navigation.
- Switched UI to Bootstrap 5.3.3.
- Fixed App.razor head/title and Routes NotFoundPage usage.
- Created sidebar navigation, topbar, dashboard skeleton, NotFound, Error pages.
- Added responsive/collapsible/resizable sidebar.
- T21: Created invoice upload screen.
- T22: Created invoice list screen.
- T23: Created invoice detail screen.
- T24: Created validation issues screen.
- T25: Created edit extracted data screen.
- Fixed line item update issue by using InvoiceLineItemId instead of replacing entire collection.
- T26: Created dashboard statistics screen.

## Phase 5: OCR and AI Integration
- T27: Integrated OCR into real processing flow and tested with sample invoice image.
- Added tessdata setup and OCR configuration.
- Stored RawText and StructuredJson in InvoiceExtractionResults.
- T28: Refactored OCR parsing into IInvoiceTextParser.
- Created RuleBasedInvoiceTextParser.
- Improved parser for vendor name, invoice number, dates, subtotal, VAT, total, and line items.
- T29: Integrated OpenAI parser and HybridInvoiceTextParser.
- Added fallback from OpenAI parser to RuleBased parser.
- Converted parser interface to async.
- T30: Created OCR vs OCR+AI comparison feature.
- T31: Refined OpenAI prompt and structured JSON schema output.
- Logged OpenAI 429 insufficient_quota clearly.

## Phase 6A: Complete Business Workflow and UI
- T32: Created approve invoice function.
- Added domain method Invoice.Approve().
- Added ApproveInvoiceUseCase.
- Added approve button to invoice detail page.
- T33: Created reject invoice and move invoice back to PendingReview functions.
- Added RejectInvoiceUseCase.
- Added MoveInvoiceToPendingReviewUseCase.
- Added visibility-based buttons instead of disabled-only buttons.
- T34: Completed audit logs screen.
- Added audit log DTOs, repository, use case, and UI.
- Refactored AuditLogEntry and AuditActionType into Domain/Audit.
- T35: Completed read-only system settings screen.
- Added SystemSettingsService and GetSystemSettingsUseCase.

## Current Phase
Phase 6B: Testing, experiment, and thesis report.

## Remaining Tasks
- T36. Write tests for Domain rules.
- T37. Write tests for Application use cases.
- T38. Test upload → OCR → parse → validate flow.
- T39. Prepare evaluation dataset.
- T40. Summarize experiment results.
- T41. Write thesis report.
- T42. Prepare demo checklist.
