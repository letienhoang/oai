# oai — Invoice Extraction with OCR + LLM + Consistency Checking

A Python library that extracts structured data from invoice images by combining
**Optical Character Recognition (OCR)** and a **Large Language Model (LLM)**,
then validates the result with an automated **consistency checker**.

## Architecture

```
Invoice image
      │
      ▼
 ┌──────────┐   raw text   ┌──────────┐   Invoice JSON   ┌───────────────────┐
 │   OCR    │ ──────────►  │   LLM    │ ──────────────►  │ ConsistencyChecker│
 │ Extractor│              │ Extractor│                  └─────────┬─────────┘
 └──────────┘              └──────────┘                           │
                                                        ConsistencyReport +
                                                        ExtractedInvoiceResult
```

### Components

| Module | Description |
|---|---|
| `ocr.py` | Extracts raw text from invoice images. Default backend: **Tesseract**. Swap in any `BaseOCRBackend`. |
| `llm.py` | Parses OCR text into a structured `Invoice` model via an LLM prompt. Default backend: **OpenAI GPT-4o-mini**. |
| `consistency.py` | Runs 6 checks (line-item totals, subtotal, tax, grand total, required fields, positive amounts). |
| `models.py` | Pydantic v2 models: `Invoice`, `LineItem`, `ConsistencyReport`, `ExtractedInvoiceResult`. |
| `extractor.py` | `InvoiceExtractor` — orchestrates the full pipeline in a single `.extract()` call. |

## Quick Start

```python
from invoice_extractor import InvoiceExtractor

extractor = InvoiceExtractor()          # uses Tesseract OCR + OpenAI LLM
result = extractor.extract("invoice.png")

print(result.invoice.invoice_number)    # e.g. "INV-2024-001"
print(result.invoice.total)             # e.g. Decimal("330.00")
print(result.consistency_report.is_consistent)   # True / False
for issue in result.consistency_report.issues:
    print(issue.message)
```

If you already have the OCR text:

```python
result = extractor.extract_from_text(ocr_text)
```

## Installation

```bash
pip install -e ".[dev]"          # core + development tools
pip install -e ".[ocr]"          # adds pytesseract + Pillow for local OCR
```

Set your OpenAI key before running:

```bash
export OPENAI_API_KEY="sk-..."
```

## Consistency Checks

The `ConsistencyChecker` performs the following validations automatically:

1. **`check_line_item_totals`** — each line item: `total ≈ quantity × unit_price`
2. **`check_subtotal`** — `subtotal ≈ Σ line_item.total`
3. **`check_tax_amount`** — `tax_amount ≈ subtotal × tax_rate`
4. **`check_grand_total`** — `total ≈ subtotal + tax_amount`
5. **`check_required_fields`** — `invoice_number`, `invoice_date`, `vendor_name`, `total` must be present
6. **`check_positive_amounts`** — `subtotal`, `tax_amount`, `total` must be ≥ 0

A tolerance of ±0.02 is applied to all monetary comparisons to accommodate rounding.

## Custom Backends

```python
from invoice_extractor import InvoiceExtractor
from invoice_extractor.ocr import StubOCRBackend
from invoice_extractor.llm import StubLLMBackend

# Use stubs for testing
extractor = InvoiceExtractor(
    ocr_backend=StubOCRBackend(text="..."),
    llm_backend=StubLLMBackend(response_json={...}),
)
```

## Running Tests

```bash
pytest
# or with coverage
pytest --cov=invoice_extractor
```

