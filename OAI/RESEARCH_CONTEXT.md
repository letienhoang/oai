# Research Context - OAI Graduation Thesis

## 1. Thesis Topic

The current graduation thesis focuses on building an invoice processing system that combines OCR and AI to extract, validate, and manage invoice data.

The system is named **OAI**.

The main idea is:

> Build a web-based invoice processing system that allows users to upload invoice images, extract text using OCR, parse invoice information into structured data, validate consistency, allow human correction, and compare rule-based extraction with AI-assisted extraction.

## 2. Motivation

Invoice processing is a common business task in many companies. Manual data entry from invoices is repetitive, time-consuming, and error-prone. OCR can help convert invoice images into text, but OCR output is often noisy and difficult to convert directly into structured invoice data.

Traditional rule-based extraction can work with simple invoice layouts, but it is fragile when:
- the invoice layout changes;
- labels and values are split across different lines;
- OCR introduces noise;
- invoice formats vary across vendors;
- Vietnamese and English labels appear together.

AI/LLM-based extraction can improve flexibility by understanding context from OCR text and producing structured JSON output. However, AI output should still be validated and reviewed by users before being trusted.

Therefore, the thesis system is designed with a **human-in-the-loop workflow**:

```txt
Upload invoice
→ OCR
→ Rule-based / AI extraction
→ Validation
→ User review and correction
→ Approval or rejection
→ Audit tracking
```

## 3. Original Research Direction

The initial thesis idea was inspired by research papers related to OCR-based invoice or document information extraction.

The first implementation direction was to build a basic OCR-based invoice extraction system similar to traditional document processing approaches:

```txt
Invoice image
→ OCR text
→ rule-based extraction
→ structured invoice fields
```

After discussion, the system was expanded to include AI-assisted extraction:

```txt
Invoice image
→ OCR text
→ LLM parser
→ structured JSON
→ validation and human review
```

This allows the thesis to compare:

```txt
OCR + Rule-based parser
vs
OCR + AI parser
```

## 4. Related Research Area

### 4.1 Optical Character Recognition

OCR is used to convert invoice images into raw text.

In this project:
- Tesseract OCR is used as the OCR engine.
- The OCR wrapper supports image files first.
- PDF invoice support is planned for a later stage.
- Languages tested include English and Vietnamese through `eng+vie`.

OCR limitations observed during testing:
- vendor name can be misread because of logo or layout noise;
- invoice number may be split from its label;
- total amount may appear on a different line from the `Total:` label;
- table layout may be flattened into plain text;
- OCR may introduce unexpected characters such as `Ạ`.

### 4.2 Information Extraction from Semi-Structured Documents

Invoices are semi-structured documents. They contain common fields such as:
- vendor name;
- invoice number;
- issue date;
- due date;
- subtotal;
- tax amount;
- total amount;
- line items.

However, the layout varies across invoices. Therefore, extracting information requires more than simple OCR.

The system currently supports two extraction strategies:

```txt
Rule-based extraction:
- Regex
- label matching
- fallback heuristics

AI-based extraction:
- OCR raw text
- prompt + JSON schema
- structured JSON output
```

### 4.3 Human-in-the-Loop Document Processing

The system does not automatically trust OCR or AI results. Instead, extracted data is stored as processed data and then reviewed by the user.

The invoice lifecycle is:

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

This design is important because OCR and AI can still make mistakes.

## 5. Scientific Paper Usage

The thesis direction was influenced by scientific papers about invoice information extraction and OCR-based document processing.

The paper-based idea was adapted into a practical system:

```txt
From paper:
OCR-based invoice information extraction

Adapted thesis topic:
Building an invoice processing system using OCR and AI for structured extraction, validation, and human review
```

The implementation does not simply reproduce one paper. Instead, it uses the paper as a foundation and expands the system into a complete software application with:
- layered architecture;
- OCR integration;
- AI parser;
- validation rules;
- editable extracted data;
- approval workflow;
- audit logs;
- dashboard;
- settings screen.

## 6. Final Thesis Direction

The final thesis direction is not just “OCR invoice reading”. It is broader:

> Design and implement an invoice processing system using OCR and AI-assisted information extraction, with validation, human review, approval workflow, and auditability.

A suitable Vietnamese thesis title could be:

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

## 7. Current System Architecture

The implementation uses a layered monolith architecture.

Projects:

```txt
OAI.Domain
OAI.Application
OAI.Infrastructure
OAI.Web
```

### OAI.Domain

Contains core business entities, value objects, enums, and domain rules.

Important entities:
- Vendor
- Invoice
- InvoiceLineItem
- ValidationIssue
- InvoiceExtractionResult
- AuditLogEntry

Important value object:
- Money

Important enums:
- InvoiceStatus
- ValidationSeverity
- AuditActionType

Invoice statuses:

```txt
Draft
PendingReview
Approved
Rejected
Exported
```

### OAI.Application

Contains DTOs, use cases, service abstractions, and application-level workflows.

Important use cases:
- CreateInvoiceUseCase
- UpdateInvoiceUseCase
- ValidateInvoiceUseCase
- GetInvoiceListUseCase
- GetInvoiceDetailUseCase
- GetValidationIssueListUseCase
- ApproveInvoiceUseCase
- RejectInvoiceUseCase
- MoveInvoiceToPendingReviewUseCase
- CompareInvoiceExtractionUseCase
- GetDashboardSummaryUseCase
- GetAuditLogListUseCase
- GetSystemSettingsUseCase

### OAI.Infrastructure

Contains EF Core persistence, repositories, OCR implementation, file storage, AI parser, audit interceptor, and system settings implementation.

Important infrastructure services:
- FileStorageService
- TesseractOcrService
- InvoiceExtractionService
- RuleBasedInvoiceTextParser
- OpenAiInvoiceTextParser
- HybridInvoiceTextParser
- InvoiceExtractionComparisonService
- SystemSettingsService
- AuditTrailInterceptor

### OAI.Web

Blazor Web App UI.

Important screens:
- Dashboard
- Upload invoice
- Invoice list
- Invoice detail
- Edit invoice
- Validation issues
- OCR vs OCR+AI comparison
- Audit logs
- Settings

## 8. OCR and AI Design

The current extraction pipeline is:

```txt
Uploaded image
→ FileStorageService
→ TesseractOcrService
→ OCR RawText
→ HybridInvoiceTextParser
   ├── OpenAiInvoiceTextParser if enabled and available
   └── RuleBasedInvoiceTextParser fallback
→ ExtractedInvoiceDto
→ CreateInvoiceUseCase
→ Database
```

### Rule-Based Parser

The rule-based parser handles:
- normalized OCR lines;
- invoice number pattern matching;
- vendor name detection;
- date extraction by label and fallback by index;
- amount extraction by label;
- VAT/tax extraction;
- line item extraction through regex;
- tax rate inference.

### AI Parser

The AI parser uses:
- OCR raw text;
- OpenAI API;
- prompt instructions;
- JSON schema / structured output;
- mapped `ParsedInvoiceLlmResult`;
- fallback to rule-based parser when AI fails.

Common AI failure currently handled:
- missing API key;
- disabled LLM;
- insufficient quota;
- invalid JSON;
- failed extraction.

## 9. Important Observed Test Case

Sample invoice content:

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

Actual OCR result showed layout issues:

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

This test case revealed several important parser issues:
- vendor name was initially detected as OCR noise `Ạ`;
- invoice number was initially parsed incorrectly;
- due date was initially confused with issue date;
- total amount was initially confused with subtotal;
- VAT label with parentheses required better parsing.

The parser was improved to correctly extract:
- VendorName: `ACME SOFTWARE COMPANY`
- InvoiceNumber: `INV-2026-001`
- IssueDate: `2026-04-27`
- DueDate: `2026-04-30`
- Subtotal: `1500000`
- TaxAmount: `150000`
- TotalAmount: `1650000`
- LineItems:
  - Consulting service
  - OCR setup

## 10. Evaluation Plan

The evaluation phase should compare:

```txt
OCR + Rule-based parser
vs
OCR + AI parser
```

Possible evaluation metrics:
- field-level accuracy;
- invoice-level accuracy;
- extraction success rate;
- number of fields requiring manual correction;
- comparison of structured JSON output.

Suggested fields for evaluation:
- VendorName
- InvoiceNumber
- IssueDate
- DueDate
- Subtotal
- TaxAmount
- TotalAmount
- LineItems

Suggested dataset:
- 10–20 invoice images;
- multiple layouts;
- English and Vietnamese samples;
- clean and noisy images;
- expected ground truth for each invoice.

## 11. Thesis Contribution

The system contributes:

1. A practical invoice processing workflow using OCR and AI.
2. A layered .NET implementation suitable for a real business application.
3. A hybrid extraction mechanism combining rule-based and AI-based parsing.
4. A validation mechanism to detect inconsistent invoice totals.
5. A human-in-the-loop workflow for correcting and approving extracted data.
6. An audit trail for traceability.
7. A comparison feature to evaluate OCR-only versus OCR+AI extraction.

## 12. Next Phase

The next phase is Phase 6B:

```txt
Testing, experiment, and thesis report
```

Planned tasks:

```txt
T36. Write tests for Domain rules
T37. Write tests for Application use cases
T38. Test upload → OCR → parse → validate flow
T39. Prepare evaluation dataset
T40. Summarize experiment results
T41. Write thesis report
T42. Prepare demo checklist
```
