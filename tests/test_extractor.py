"""Integration tests for the end-to-end InvoiceExtractor pipeline."""

from __future__ import annotations

from datetime import date
from decimal import Decimal

import pytest

from invoice_extractor import InvoiceExtractor
from invoice_extractor.llm import StubLLMBackend
from invoice_extractor.ocr import StubOCRBackend
from invoice_extractor.models import ExtractedInvoiceResult


SAMPLE_OCR_TEXT = """\
ACME Corp
123 Main St, Springfield, USA

INVOICE
Invoice #: INV-2024-042
Date: 2024-03-01
Due Date: 2024-03-31

Bill To:
Jane Doe
456 Oak Ave, Test City

Description          Qty   Unit Price   Total
Widget A              2      50.00      100.00
Service B             1     200.00      200.00

Subtotal:  300.00
Tax (10%):  30.00
TOTAL:     330.00

Currency: USD
Notes: Net 30 days
"""

SAMPLE_INVOICE_JSON = {
    "invoice_number": "INV-2024-042",
    "invoice_date": "2024-03-01",
    "due_date": "2024-03-31",
    "vendor_name": "ACME Corp",
    "vendor_address": "123 Main St, Springfield, USA",
    "customer_name": "Jane Doe",
    "customer_address": "456 Oak Ave, Test City",
    "line_items": [
        {"description": "Widget A", "quantity": 2, "unit_price": 50.00, "total": 100.00},
        {"description": "Service B", "quantity": 1, "unit_price": 200.00, "total": 200.00},
    ],
    "subtotal": 300.00,
    "tax_rate": 0.10,
    "tax_amount": 30.00,
    "total": 330.00,
    "currency": "USD",
    "notes": "Net 30 days",
}


class TestInvoiceExtractorIntegration:
    def _make_extractor(self, ocr_text=None, invoice_json=None):
        ocr_backend = StubOCRBackend(text=ocr_text or SAMPLE_OCR_TEXT)
        llm_backend = StubLLMBackend(response_json=invoice_json or SAMPLE_INVOICE_JSON)
        return InvoiceExtractor(ocr_backend=ocr_backend, llm_backend=llm_backend)

    def test_extract_returns_result(self):
        extractor = self._make_extractor()
        result = extractor.extract(b"\x00")  # dummy bytes; OCR is stubbed
        assert isinstance(result, ExtractedInvoiceResult)

    def test_extract_invoice_fields(self):
        extractor = self._make_extractor()
        result = extractor.extract(b"\x00")
        invoice = result.invoice
        assert invoice.invoice_number == "INV-2024-042"
        assert invoice.invoice_date == date(2024, 3, 1)
        assert invoice.vendor_name == "ACME Corp"
        assert invoice.total == Decimal("330.00")
        assert invoice.currency == "USD"

    def test_extract_line_items(self):
        extractor = self._make_extractor()
        result = extractor.extract(b"\x00")
        items = result.invoice.line_items
        assert len(items) == 2
        assert items[0].description == "Widget A"
        assert items[1].total == Decimal("200.00")

    def test_raw_ocr_text_preserved(self):
        extractor = self._make_extractor()
        result = extractor.extract(b"\x00")
        assert "ACME Corp" in result.raw_ocr_text

    def test_consistent_invoice_passes_check(self):
        extractor = self._make_extractor()
        result = extractor.extract(b"\x00")
        assert result.consistency_report.is_consistent is True
        assert result.consistency_report.issues == []

    def test_inconsistent_invoice_detected(self):
        bad_json = {**SAMPLE_INVOICE_JSON, "total": 999.00}  # wrong total
        extractor = self._make_extractor(invoice_json=bad_json)
        result = extractor.extract(b"\x00")
        assert result.consistency_report.is_consistent is False
        fields = [i.field for i in result.consistency_report.issues]
        assert "total" in fields

    def test_extract_from_text_skips_ocr(self):
        llm_backend = StubLLMBackend(response_json=SAMPLE_INVOICE_JSON)
        extractor = InvoiceExtractor(ocr_backend=StubOCRBackend(), llm_backend=llm_backend)
        result = extractor.extract_from_text(SAMPLE_OCR_TEXT)
        assert result.invoice.invoice_number == "INV-2024-042"
        assert result.raw_ocr_text == SAMPLE_OCR_TEXT

    def test_empty_ocr_text_produces_warning(self):
        llm_backend = StubLLMBackend(response_json={})
        extractor = InvoiceExtractor(ocr_backend=StubOCRBackend(), llm_backend=llm_backend)
        result = extractor.extract_from_text("   ")
        assert len(result.extraction_warnings) > 0
        assert any("empty" in w.lower() for w in result.extraction_warnings)

    def test_missing_required_fields_flagged(self):
        incomplete_json = {
            "invoice_number": None,
            "invoice_date": None,
            "vendor_name": None,
            "total": None,
            "line_items": [],
        }
        llm_backend = StubLLMBackend(response_json=incomplete_json)
        extractor = InvoiceExtractor(ocr_backend=StubOCRBackend(), llm_backend=llm_backend)
        result = extractor.extract_from_text("Some invoice text")
        assert result.consistency_report.is_consistent is False
        fields = [i.field for i in result.consistency_report.issues]
        assert "invoice_number" in fields
        assert "vendor_name" in fields
        assert "total" in fields
