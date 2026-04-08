"""Tests for the LLM extraction component."""

from __future__ import annotations

import json

import pytest

from invoice_extractor.llm import LLMExtractor, StubLLMBackend
from invoice_extractor.models import Invoice


SAMPLE_INVOICE_JSON = {
    "invoice_number": "INV-2024-001",
    "invoice_date": "2024-01-15",
    "due_date": "2024-02-14",
    "vendor_name": "ACME Corp",
    "vendor_address": "123 Main St, Springfield",
    "customer_name": "Jane Doe",
    "customer_address": "456 Oak Ave, Hanoi",
    "line_items": [
        {"description": "Widget A", "quantity": 2, "unit_price": 50.00, "total": 100.00},
        {"description": "Service B", "quantity": 1, "unit_price": 200.00, "total": 200.00},
    ],
    "subtotal": 300.00,
    "tax_rate": 0.10,
    "tax_amount": 30.00,
    "total": 330.00,
    "currency": "USD",
    "notes": "Net 30",
}


class TestStubLLMBackend:
    def test_returns_json_response(self):
        backend = StubLLMBackend(response_json=SAMPLE_INVOICE_JSON)
        response = backend.complete("system", "user text")
        parsed = json.loads(response)
        assert parsed["invoice_number"] == "INV-2024-001"


class TestLLMExtractor:
    def _make_extractor(self, response_json=None):
        backend = StubLLMBackend(response_json=response_json or SAMPLE_INVOICE_JSON)
        return LLMExtractor(backend=backend)

    def test_extract_returns_invoice(self):
        extractor = self._make_extractor()
        invoice = extractor.extract("some OCR text")
        assert isinstance(invoice, Invoice)
        assert invoice.invoice_number == "INV-2024-001"

    def test_extract_line_items(self):
        extractor = self._make_extractor()
        invoice = extractor.extract("some OCR text")
        assert len(invoice.line_items) == 2
        assert invoice.line_items[0].description == "Widget A"

    def test_extract_amounts(self):
        from decimal import Decimal
        extractor = self._make_extractor()
        invoice = extractor.extract("some OCR text")
        assert invoice.subtotal == Decimal("300.00")
        assert invoice.total == Decimal("330.00")

    def test_extract_empty_text_returns_empty_invoice(self):
        extractor = self._make_extractor()
        invoice = extractor.extract("   ")
        assert isinstance(invoice, Invoice)
        assert invoice.invoice_number is None

    def test_parse_json_with_markdown_fences(self):
        fenced = "```json\n" + json.dumps(SAMPLE_INVOICE_JSON) + "\n```"
        backend = StubLLMBackend()
        backend._response_json = {}
        extractor = LLMExtractor(backend=backend)
        result = extractor._parse_json(fenced)
        assert result["invoice_number"] == "INV-2024-001"

    def test_parse_json_with_plain_fences(self):
        fenced = "```\n" + json.dumps(SAMPLE_INVOICE_JSON) + "\n```"
        extractor = LLMExtractor(backend=StubLLMBackend())
        result = extractor._parse_json(fenced)
        assert result["vendor_name"] == "ACME Corp"

    def test_parse_json_raises_on_invalid_json(self):
        extractor = LLMExtractor(backend=StubLLMBackend())
        with pytest.raises(ValueError, match="invalid JSON"):
            extractor._parse_json("this is not json at all")

    def test_parse_json_raises_on_non_dict(self):
        extractor = LLMExtractor(backend=StubLLMBackend())
        with pytest.raises(ValueError, match="Expected a JSON object"):
            extractor._parse_json("[1, 2, 3]")

    def test_raises_runtime_error_on_backend_failure(self):
        class FailingBackend(StubLLMBackend):
            def complete(self, system, user):
                raise ConnectionError("API unreachable")

        extractor = LLMExtractor(backend=FailingBackend())
        with pytest.raises(RuntimeError, match="LLM call failed"):
            extractor.extract("some text")

    def test_raises_value_error_on_unparseable_invoice(self):
        # Return valid JSON that doesn't match Invoice schema (bad date)
        bad_data = {**SAMPLE_INVOICE_JSON, "invoice_date": "not-a-date"}
        extractor = self._make_extractor(response_json=bad_data)
        with pytest.raises(ValueError, match="could not be validated"):
            extractor.extract("some text")
