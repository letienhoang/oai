# Research Context - OAI Graduation Thesis

## 1. Thesis Topic

The graduation thesis focuses on building an invoice processing system that combines OCR and AI-assisted information extraction with validation, human review, approval workflow, and auditability.

System name: **OAI**.

Core idea:

```txt
Invoice image
→ OCR text extraction
→ Rule-based and/or AI-assisted structured extraction
→ Consistency validation
→ Human review and correction
→ Approval/rejection workflow
→ Audit logging and reporting support
```

Suggested Vietnamese thesis title:

```txt
Xây dựng hệ thống xử lý hóa đơn ứng dụng OCR và AI hỗ trợ trích xuất, kiểm tra và quản lý dữ liệu hóa đơn
```

Alternative titles:

```txt
1. Xây dựng hệ thống trích xuất và kiểm tra dữ liệu hóa đơn bằng OCR kết hợp AI
2. Nghiên cứu và xây dựng hệ thống xử lý hóa đơn thông minh sử dụng OCR và mô hình ngôn ngữ lớn
3. Xây dựng hệ thống quản lý và xác thực dữ liệu hóa đơn từ ảnh chụp sử dụng OCR và AI
4. Ứng dụng OCR và AI trong tự động hóa quy trình nhập liệu và kiểm tra hóa đơn
5. Xây dựng hệ thống xử lý hóa đơn bán tự động với cơ chế human-in-the-loop
```

## 2. Motivation

Invoice processing is a common business workflow. Manual invoice data entry is repetitive, time-consuming, and error-prone.

OCR helps convert invoice images into text, but OCR output is often noisy and not directly suitable for database storage. Common problems include:

- invoice layout variation;
- text recognition errors;
- table flattening;
- labels and values split across lines;
- noisy logos or headers being interpreted as text;
- mixed Vietnamese and English labels;
- inconsistent date and amount formats.

Rule-based extraction can work with simple layouts but is fragile when invoice formats vary. AI/LLM-based extraction is more flexible because it can infer structure from noisy OCR text, but AI results still require validation and human review.

Therefore, the thesis system uses a **human-in-the-loop invoice processing workflow** rather than fully automatic posting.

## 3. Research Direction

The project began as an OCR-based invoice extraction system inspired by research on document information extraction and invoice processing.

Initial direction:

```txt
Invoice image
→ OCR text
→ Rule-based extraction
→ Structured invoice fields
```

Expanded direction:

```txt
Invoice image
→ OCR text
→ AI-assisted parser
→ Structured JSON
→ Validation
→ Human review
```

The final system supports comparison between:

```txt
OCR + Rule-based parser
vs
OCR + AI-assisted parser
```

This makes the thesis not only a software implementation but also a small practical evaluation of extraction approaches for semi-structured invoices.

## 4. Related Research Areas

### 4.1 Optical Character Recognition

OCR is used to convert invoice images into raw text.

Current implementation:

- Tesseract OCR engine.
- Language setting: `eng+vie`.
- Image invoice support remains available.
- PDF invoice support has been added in Phase 10 with embedded text extraction, scanned page rendering, page preview storage, and OCR for rendered pages.

Observed OCR limitations:

- vendor name can be misread due to logos/noise;
- invoice number can be separated from its label;
- dates may be detected but associated with the wrong fields;
- table layout is often flattened into plain lines;
- total amount can be confused with subtotal;
- OCR may emit unexpected characters such as `Ạ`.

### 4.2 Semi-Structured Document Information Extraction

Invoices are semi-structured documents. They share common concepts but differ in layout.

Important extracted fields:

- vendor name;
- invoice number;
- issue date;
- due date;
- subtotal;
- tax amount;
- total amount;
- currency;
- line items.

Supported extraction approaches:

```txt
Rule-based extraction:
- regex
- label matching
- date/amount fallback heuristics
- line-item pattern detection

AI-assisted extraction:
- OCR raw text
- prompt instructions
- structured JSON output
- mapping into application DTOs
```

### 4.3 Large Language Models for Information Extraction

The AI parser uses an LLM to transform OCR text into structured invoice data.

Benefits:

- more flexible with varied layouts;
- can infer semantic relationships between labels and values;
- can produce JSON-like structured output;
- can improve extraction when OCR text order is imperfect.

Risks and limitations:

- API quota/cost;
- invalid JSON output;
- hallucinated fields;
- inconsistent line item extraction;
- need for fallback when AI fails;
- need for validation before user approval.

Therefore, the project uses a hybrid design:

```txt
Try AI parser if enabled and available
→ fallback to rule-based parser if AI fails or is disabled
```

### 4.4 Human-in-the-Loop Document Processing

The system does not fully trust OCR or AI output. Extracted data must be reviewed before approval.

Workflow:

```txt
Upload/OCR/AI
→ PendingReview
→ Edit if needed
→ Validate again
→ Approved
```

Alternative flow:

```txt
PendingReview / Approved
→ Rejected
→ PendingReview
```

This is important because invoice data is financially sensitive and OCR/AI mistakes can affect accounting records.

### 4.5 Auditability and Traceability

Auditability is a major practical requirement for business systems. The project records data changes through an EF Core audit interceptor.

Audit log information includes:

- entity name;
- entity id;
- action type;
- old values;
- new values;
- user id/user name;
- timestamp;
- source.

This supports traceability of invoice edits, approval decisions, vendor changes, and demo operations.

## 5. Final System Scope

The project is now broader than OCR-only extraction. It is a complete invoice processing workflow system with:

- upload, batch processing, and file storage;
- OCR extraction;
- PDF embedded text extraction and scanned PDF page OCR;
- AI-assisted parsing;
- rule-based fallback parsing;
- structured invoice persistence;
- validation rules;
- human correction;
- approval/rejection workflow;
- vendor management;
- dashboard and filtering;
- audit logs;
- secure source file preview/download;
- invoice source file viewer;
- authentication and authorization;
- localization;
- demo and system health tools.

## 6. Current Implementation Architecture

The implementation uses a layered monolith architecture:

```txt
OAI.Domain
OAI.Application
OAI.Infrastructure
OAI.Web
```

### OAI.Domain

Contains core entities, value objects, and domain rules.

Important entities:

- `Vendor`
- `Invoice`
- `InvoiceLineItem`
- `ValidationIssue`
- `InvoiceExtractionResult`
- `AuditLogEntry`

Important value object:

- `Money`

Important enums:

- `InvoiceStatus`
- validation severity/action-related enums where applicable.

### OAI.Application

Contains use cases, DTOs, filters, abstractions, and application orchestration.

Important areas:

- invoice creation/update/detail/list;
- invoice approval/rejection/state transitions;
- validation issue queries;
- extraction comparison;
- dashboard summary;
- audit log list/detail support;
- vendor list/upsert/options;
- filter DTOs for invoice list, dashboard, audit logs, vendors;
- current user abstraction for audit context.

### OAI.Infrastructure

Contains technical implementations:

- EF Core persistence;
- SQL Server repositories;
- Identity stores;
- file storage;
- Tesseract OCR service;
- rule-based parser;
- OpenAI/LLM parser;
- hybrid parser;
- audit interceptor;
- demo data seed/reset;
- system health service.

### OAI.Web

Blazor Web App UI:

- localized EN/VI interface;
- authentication UI;
- authorization-based pages/actions;
- dashboard;
- invoice upload/list/detail/edit/compare;
- validation issues;
- vendor management;
- audit logs;
- settings;
- development demo tools;
- system health panel.

## 7. OCR and AI Extraction Pipeline

Current extraction pipeline:

```txt
Uploaded image
→ FileStorageService
→ TesseractOcrService
→ OCR raw text
→ HybridInvoiceTextParser
   ├── OpenAiInvoiceTextParser if enabled and successful
   └── RuleBasedInvoiceTextParser fallback
→ ExtractedInvoiceDto / parsed result
→ CreateInvoiceUseCase
→ Validation
→ Database
```

### Rule-Based Parser Responsibilities

The rule-based parser handles:

- normalized OCR lines;
- invoice number detection;
- vendor name detection;
- date extraction;
- amount extraction;
- VAT/tax detection;
- line item parsing;
- fallback heuristics.

### AI Parser Responsibilities

The AI parser handles:

- prompt-based extraction from OCR text;
- structured JSON output;
- mapping model output into typed DTOs;
- fallback when disabled, quota-limited, or failed.

### Extraction History

Each invoice can store extraction history with:

- engine name;
- confidence score;
- extracted timestamp;
- success flag;
- attempt number;
- raw OCR text;
- structured JSON.

The Invoice Detail page now includes an extraction history tab and detail dialog.

## 8. Validation and Human Review

The system validates invoice data after extraction and after manual edit.

Examples of validation concerns:

- subtotal/tax/total consistency;
- required fields;
- date consistency;
- line item total consistency;
- unresolved validation errors blocking approval.

Users can edit extracted invoice data and rerun validation before approval.

Important UX decisions:

- important actions use confirmation dialogs;
- action buttons are shown/hidden by permission;
- action handlers also check authorization;
- validation and action messages are localized.

## 9. Authentication, Authorization, and Roles

The system uses ASP.NET Core Identity.

Roles:

```txt
Administrator
Accountant
Auditor
Viewer
```

Authorization design:

- roles are mapped to permission claims;
- pages use authorization policies;
- important actions check authorization before running use cases;
- unauthorized users see access denied or do not see unavailable actions.

Default development users exist for testing each role.

## 10. Localization

The system uses `.resx` resources.

Default language:

```txt
English
```

Supported additional language:

```txt
Vietnamese
```

UI pattern:

```razor
@L["Key"]
```

Language can be changed from the TopBar.

Localization covers:

- page titles;
- menu labels;
- buttons;
- validation messages;
- action/system messages;
- role-protected pages;
- development tools.

## 11. Important Observed OCR Test Case

Sample intended invoice:

```txt
ACME SOFTWARE COMPANY

Invoice Number: INV-2026-001
Invoice Date: 27/04/2026
Due Date: 30/04/2026

Consulting service 1 1000000 1000000
OCR setup 1 500000 500000

Subtotal: 1500000
VAT: 150000
Total: 1650000
```

Observed OCR output contained layout/noise problems:

```txt
Ạ

ACME

SOFTWARE COMPANY.

Invoice Number:
Invoice Date:

Due Date:

ACME SOFTWARE COMPANY

INV-2026-001

27/04/2026
30/04/2026

Description Quantity Unit Price (VND) Amount (VND)
Consulting service 1 1,000,000 1,000,000
OCR setup 1 500,000 500,000

Subtotal: 1,500,000
VAT (10%): 150,000
Total:

1,650,000
```

This revealed important extraction issues:

- vendor name initially detected as OCR noise;
- invoice number separated from label;
- issue date and due date could be confused;
- total amount split from label;
- VAT label with parentheses required improved parsing;
- table line items needed regex/fallback handling.

The parser was improved to extract:

- `VendorName = ACME SOFTWARE COMPANY`
- `InvoiceNumber = INV-2026-001`
- `IssueDate = 2026-04-27`
- `DueDate = 2026-04-30`
- `Subtotal = 1500000`
- `TaxAmount = 150000`
- `TotalAmount = 1650000`
- line items: Consulting service, OCR setup.

## 12. Evaluation Plan

The thesis evaluation should compare:

```txt
OCR + Rule-based parser
vs
OCR + AI-assisted parser
```

Recommended metrics:

- field-level accuracy;
- invoice-level accuracy;
- extraction success rate;
- number of fields requiring manual correction;
- line item extraction accuracy;
- comparison of structured JSON quality;
- qualitative analysis of failure cases.

Suggested fields for evaluation:

- VendorName
- InvoiceNumber
- IssueDate
- DueDate
- Subtotal
- TaxAmount
- TotalAmount
- Currency
- LineItems

Suggested dataset:

- 10-20 invoice images;
- multiple layouts;
- English and Vietnamese invoices;
- clean and noisy images;
- expected ground truth for each invoice.

Suggested experiment table:

```txt
Invoice sample
Field
Ground truth
Rule-based result
AI-assisted result
Correct/incorrect
Notes
```

## 13. Thesis Contributions

The system contributes:

1. A practical OCR + AI invoice processing workflow.
2. A layered .NET implementation suitable for business application development.
3. A hybrid extraction mechanism combining rule-based and AI-assisted parsing.
4. A validation mechanism to detect inconsistent invoice data.
5. A human-in-the-loop workflow for review, correction, and approval.
6. Role-based access control for business actors.
7. Audit logging for traceability.
8. A comparison feature for OCR-only/rule-based versus OCR+AI extraction.
9. A localized UI suitable for Vietnamese/English demonstration.
10. Demo data and system health tools to support thesis presentation.

## 14. Current System State

The project has completed major implementation phases through Phase 10. v1.3.0 is prepared after Phase 10 completion.

Phase 12A / v1.5.0 research preparation has started with T130, which defines the invoice-level audit anomaly dataset schema. Later tasks will generate the synthetic normal/anomaly dataset, normalize features, train a lightweight Logistic Regression model, export model weights, and wire C# inference.

Completed areas include:

```txt
- Domain and application workflow
- OCR and AI parser integration
- Invoice upload/list/detail/edit/approval workflow
- Validation and comparison features
- Dashboard and filtering
- Localization
- Authentication and authorization
- Vendor management
- Audit logs and detail dialog
- Invoice detail tabs and extraction history detail
- Demo data seed/reset tools
- System health/status panel
- EF money precision configuration
- Upload batch processing with Hangfire background jobs
- Image/PDF/ZIP file type detection
- PDF embedded text extraction and scanned page rendering/OCR
- Source file storage, preview/download APIs, and Invoice Detail source file viewer
```

The old `NEXT_TASKS.md` and `TASK_HISTORY.md` files were removed. The current state should now be inferred from:

```txt
AI_PROJECT_CONTEXT.md
RESEARCH_CONTEXT.md
Git commit history
Current source code on develop
```

## 15. Recommended Remaining Work

### Thesis and evaluation work

```txt
1. Prepare evaluation dataset.
2. Run OCR + rule-based extraction tests.
3. Run OCR + AI extraction tests.
4. Compare field-level accuracy.
5. Document observed OCR/AI limitations.
6. Write implementation chapter.
7. Write evaluation chapter.
8. Prepare screenshots and demo script.
```

### Technical polish work

```txt
1. Verify clean database migration from scratch.
2. Run build/test regularly.
3. Review localization completeness.
4. Add field-level audit change view.
5. Improve OCR/AI comparison screen layout.
6. Persist list/dashboard/audit filters in query string.
7. Start Phase 11 roadmap work.
8. Add Docker and CI/CD if time allows.
```
