# Next Tasks - Phase 6B

## T36. Write tests for Domain rules

Goal: verify that the core business rules are correct and stable.

Suggested tests:
- Money value object
  - cannot create negative money if your domain forbids it;
  - same currency arithmetic works;
  - different currency arithmetic should be blocked if implemented.
- InvoiceLineItem calculations
  - NetAmount = Quantity × UnitPrice
  - TaxAmount = NetAmount × TaxRate / 100
  - GrossAmount = NetAmount + TaxAmount
- Invoice consistency validation
  - valid invoice has no Error validation issue;
  - wrong subtotal creates Error issue;
  - wrong tax amount creates Error issue;
  - wrong total creates Error issue.
- Invoice status transitions
  - PendingReview can be approved if no unresolved Error issue exists;
  - cannot approve if unresolved Error issue exists;
  - Approved can move back to PendingReview;
  - PendingReview or Approved can be rejected;
  - Exported cannot be moved back or rejected if that rule is implemented.

## T37. Write tests for Application use cases

Goal: verify application workflows with fake or in-memory repositories.

Suggested tests:
- CreateInvoiceUseCase
  - creates invoice with vendor, lines, declared totals;
  - rejects duplicate invoice number;
  - creates validation issues if totals are inconsistent.
- UpdateInvoiceUseCase
  - updates header and line items;
  - keeps existing line item IDs when editing;
  - moves invoice back to PendingReview after edit;
  - recalculates validation issues.
- ValidateInvoiceUseCase
  - returns correct validation summary.
- ApproveInvoiceUseCase
  - approves invoice without unresolved Error;
  - rejects approval when unresolved Error exists.
- RejectInvoiceUseCase
  - changes status to Rejected.
- MoveInvoiceToPendingReviewUseCase
  - changes status to PendingReview.
- CompareInvoiceExtractionUseCase
  - loads latest OCR raw text;
  - returns comparison between rule-based and AI parser.

## T38. Test upload → OCR → parse → validate flow

Goal: manually or automatically test the complete real flow.

Test cases:
- Valid image invoice.
- Duplicate invoice number.
- OCR returns empty text.
- Missing invoice number.
- Missing total amount.
- VAT invoice.
- Invoice where Total label and amount are split into different lines.
- Vietnamese invoice if possible.
- Image with OCR noise.

Expected flow:

```txt
Upload file
→ FileStorageService saves file
→ TesseractOcrService extracts RawText
→ HybridInvoiceTextParser parses structured data
→ CreateInvoiceUseCase creates invoice
→ ValidateConsistency creates issues if needed
→ User reviews, edits, approves/rejects
```

## T39. Prepare evaluation dataset

Goal: prepare a small ground-truth dataset for the experiment chapter.

Suggested dataset size:
- 10–20 invoice images.

Dataset categories:
- simple English invoice;
- simple Vietnamese invoice;
- invoice with VAT;
- invoice with multiple line items;
- invoice with noisy OCR;
- invoice with labels and values split across lines;
- invoice with different table layout.

For each invoice, prepare expected ground truth:
- VendorName
- InvoiceNumber
- IssueDate
- DueDate
- Currency
- Subtotal
- TaxAmount
- TotalAmount
- LineItems

## T40. Summarize experiment results

Goal: compare OCR + Rule-based parser vs OCR + AI parser.

Suggested metrics:
- Field-level accuracy
- Invoice-level accuracy
- Extraction success rate
- Number of fields requiring manual correction
- Parser failure rate

Suggested result table:

```txt
Field                 Rule-based     OCR+AI
VendorName            70%            90%
InvoiceNumber         80%            95%
IssueDate             85%            95%
DueDate               75%            90%
Subtotal              90%            95%
TaxAmount             85%            95%
TotalAmount           90%            95%
LineItems             60%            85%
```

## T41. Write thesis report

Suggested structure:

```txt
Chapter 1: Introduction
Chapter 2: Background and related work
Chapter 3: System analysis and design
Chapter 4: Implementation
Chapter 5: Experiment and evaluation
Chapter 6: Conclusion and future work
```

Important points to explain:
- why invoice processing is useful;
- why OCR alone is not enough;
- why AI/LLM helps with semi-structured text;
- why human-in-the-loop is needed;
- architecture of the system;
- validation and auditability;
- comparison of rule-based and AI-assisted extraction.

## T42. Prepare demo checklist

Recommended demo flow:
1. Open Dashboard.
2. Upload sample invoice image.
3. Show invoice list.
4. Open invoice detail.
5. Show RawText and StructuredJson through extraction history if available.
6. Open OCR vs OCR+AI comparison.
7. Edit extracted data.
8. Show validation issues.
9. Approve invoice.
10. Reject or move invoice back to PendingReview.
11. Open audit logs.
12. Open settings screen.

Demo data:
- Use one simple invoice that works well.
- Use one invoice with OCR noise to show why AI/human review matters.
- Prepare database cleanup script for repeated demos.
