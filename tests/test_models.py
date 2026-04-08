"""Tests for the invoice data models."""

from __future__ import annotations

from decimal import Decimal
from datetime import date

import pytest

from invoice_extractor.models import (
    ConsistencyIssue,
    ConsistencyReport,
    ExtractedInvoiceResult,
    Invoice,
    LineItem,
)


class TestLineItem:
    def test_basic_construction(self):
        item = LineItem(description="Widget", quantity="2", unit_price="5.00", total="10.00")
        assert item.description == "Widget"
        assert item.quantity == Decimal("2")
        assert item.unit_price == Decimal("5.00")
        assert item.total == Decimal("10.00")

    def test_numeric_inputs_accepted(self):
        item = LineItem(description="Gadget", quantity=3, unit_price=9.99, total=29.97)
        assert item.quantity == Decimal("3")
        assert item.unit_price == Decimal("9.99")
        assert item.total == Decimal("29.97")

    def test_none_coerced_to_zero(self):
        item = LineItem(description="Test", quantity=None, unit_price=None, total=None)
        assert item.quantity == Decimal("0")
        assert item.unit_price == Decimal("0")
        assert item.total == Decimal("0")


class TestInvoice:
    def test_empty_invoice(self):
        inv = Invoice()
        assert inv.invoice_number is None
        assert inv.line_items == []
        assert inv.total is None

    def test_full_invoice(self):
        inv = Invoice(
            invoice_number="INV-001",
            invoice_date=date(2024, 1, 15),
            vendor_name="ACME Corp",
            line_items=[
                LineItem(description="Service A", quantity=1, unit_price=100, total=100),
            ],
            subtotal="100.00",
            tax_rate="0.10",
            tax_amount="10.00",
            total="110.00",
            currency="USD",
        )
        assert inv.invoice_number == "INV-001"
        assert inv.subtotal == Decimal("100.00")
        assert inv.tax_rate == Decimal("0.10")
        assert inv.tax_amount == Decimal("10.00")
        assert inv.total == Decimal("110.00")

    def test_optional_decimals_empty_string(self):
        inv = Invoice(subtotal="", tax_amount="", total="")
        assert inv.subtotal is None
        assert inv.tax_amount is None
        assert inv.total is None


class TestConsistencyReport:
    def test_consistent_report(self):
        report = ConsistencyReport(is_consistent=True, checks_performed=["check_a"])
        assert report.is_consistent is True
        assert report.issues == []

    def test_report_with_issues(self):
        issue = ConsistencyIssue(
            field="total",
            expected="110.00",
            actual="100.00",
            message="Total mismatch",
        )
        report = ConsistencyReport(
            is_consistent=False,
            issues=[issue],
            checks_performed=["grand_total"],
        )
        assert report.is_consistent is False
        assert len(report.issues) == 1
        assert report.issues[0].field == "total"


class TestExtractedInvoiceResult:
    def test_construction(self):
        inv = Invoice(invoice_number="INV-1", total="100.00")
        report = ConsistencyReport(is_consistent=True, checks_performed=[])
        result = ExtractedInvoiceResult(
            invoice=inv,
            raw_ocr_text="Invoice #INV-1 Total: 100.00",
            consistency_report=report,
        )
        assert result.invoice.invoice_number == "INV-1"
        assert result.extraction_warnings == []
