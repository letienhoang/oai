"""Tests for the consistency checker."""

from __future__ import annotations

from decimal import Decimal
from datetime import date

import pytest

from invoice_extractor.consistency import ConsistencyChecker
from invoice_extractor.models import Invoice, LineItem


def make_invoice(**kwargs) -> Invoice:
    """Helper to build a valid invoice for testing."""
    defaults = dict(
        invoice_number="INV-001",
        invoice_date=date(2024, 1, 15),
        vendor_name="ACME Corp",
        line_items=[
            LineItem(description="Widget A", quantity=2, unit_price="50.00", total="100.00"),
            LineItem(description="Service B", quantity=1, unit_price="200.00", total="200.00"),
        ],
        subtotal="300.00",
        tax_rate="0.10",
        tax_amount="30.00",
        total="330.00",
        currency="USD",
    )
    defaults.update(kwargs)
    return Invoice(**defaults)


class TestConsistencyChecker:
    def setup_method(self):
        self.checker = ConsistencyChecker()

    # ------------------------------------------------------------------
    # Happy path
    # ------------------------------------------------------------------

    def test_consistent_invoice_passes(self):
        invoice = make_invoice()
        report = self.checker.check(invoice)
        assert report.is_consistent is True
        assert report.issues == []

    def test_all_checks_are_performed(self):
        invoice = make_invoice()
        report = self.checker.check(invoice)
        expected_checks = {
            "check_line_item_totals",
            "check_subtotal",
            "check_tax_amount",
            "check_grand_total",
            "check_required_fields",
            "check_positive_amounts",
        }
        assert set(report.checks_performed) == expected_checks

    # ------------------------------------------------------------------
    # Line item total checks
    # ------------------------------------------------------------------

    def test_line_item_total_mismatch(self):
        invoice = make_invoice(
            line_items=[
                LineItem(description="Widget", quantity=2, unit_price="50.00", total="99.00"),  # wrong
            ],
            subtotal="99.00",
            tax_amount="9.90",
            total="108.90",
        )
        report = self.checker.check(invoice)
        assert report.is_consistent is False
        fields = [i.field for i in report.issues]
        assert "line_items[0].total" in fields

    def test_line_item_total_within_tolerance(self):
        # $0.01 rounding difference should be OK
        invoice = make_invoice(
            line_items=[
                LineItem(description="Item", quantity=3, unit_price="33.33", total="99.99"),
                LineItem(description="Bonus", quantity=1, unit_price="200.01", total="200.01"),
            ],
            subtotal="300.00",
            tax_amount="30.00",
            total="330.00",
        )
        report = self.checker.check(invoice)
        line_issues = [i for i in report.issues if "line_items" in i.field]
        assert line_issues == []

    # ------------------------------------------------------------------
    # Subtotal checks
    # ------------------------------------------------------------------

    def test_subtotal_mismatch(self):
        invoice = make_invoice(subtotal="250.00")  # should be 300.00
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "subtotal" in fields

    def test_subtotal_check_skipped_when_no_line_items(self):
        invoice = Invoice(
            invoice_number="INV-001",
            invoice_date=date(2024, 1, 1),
            vendor_name="Vendor",
            subtotal="500.00",
            total="500.00",
        )
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "subtotal" not in fields

    # ------------------------------------------------------------------
    # Tax amount checks
    # ------------------------------------------------------------------

    def test_tax_amount_mismatch(self):
        invoice = make_invoice(tax_amount="50.00")  # should be 30.00 (10% of 300)
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "tax_amount" in fields

    def test_tax_amount_check_skipped_when_rate_absent(self):
        invoice = make_invoice(tax_rate=None, tax_amount="0.00")
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "tax_amount" not in fields

    def test_tax_amount_check_skipped_when_amount_absent(self):
        invoice = make_invoice(tax_amount=None, total="300.00")
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "tax_amount" not in fields

    # ------------------------------------------------------------------
    # Grand total checks
    # ------------------------------------------------------------------

    def test_grand_total_mismatch(self):
        invoice = make_invoice(total="400.00")  # should be 330.00
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "total" in fields

    def test_grand_total_no_tax(self):
        invoice = Invoice(
            invoice_number="INV-001",
            invoice_date=date(2024, 1, 1),
            vendor_name="Vendor",
            subtotal="100.00",
            total="100.00",
        )
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "total" not in fields

    # ------------------------------------------------------------------
    # Required fields
    # ------------------------------------------------------------------

    def test_missing_invoice_number(self):
        invoice = make_invoice(invoice_number=None)
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "invoice_number" in fields

    def test_missing_invoice_date(self):
        invoice = make_invoice(invoice_date=None)
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "invoice_date" in fields

    def test_missing_vendor_name(self):
        invoice = make_invoice(vendor_name=None)
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "vendor_name" in fields

    def test_missing_total(self):
        invoice = make_invoice(total=None)
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "total" in fields

    # ------------------------------------------------------------------
    # Positive amounts
    # ------------------------------------------------------------------

    def test_negative_total_flagged(self):
        invoice = make_invoice(subtotal="-300.00", tax_amount="-30.00", total="-330.00")
        report = self.checker.check(invoice)
        fields = [i.field for i in report.issues]
        assert "total" in fields
        assert "subtotal" in fields
        assert "tax_amount" in fields

    def test_zero_total_not_flagged(self):
        invoice = Invoice(
            invoice_number="INV-001",
            invoice_date=date(2024, 1, 1),
            vendor_name="Vendor",
            subtotal="0.00",
            total="0.00",
        )
        report = self.checker.check(invoice)
        # Zero is OK
        amount_issues = [
            i for i in report.issues if i.field in ("subtotal", "total", "tax_amount")
        ]
        assert amount_issues == []
