"""
Consistency checker for extracted invoice data.

Validates that the numerical and logical relationships within an invoice
are internally consistent (e.g. line item totals, subtotal, tax, grand total).
"""

from __future__ import annotations

import logging
from decimal import Decimal, ROUND_HALF_UP
from typing import List

from .models import ConsistencyIssue, ConsistencyReport, Invoice, LineItem

logger = logging.getLogger(__name__)

# Tolerance for floating-point / rounding differences (2 decimal places)
_TOLERANCE = Decimal("0.02")


def _fmt(value: Decimal) -> str:
    return str(value.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP))


class ConsistencyChecker:
    """Runs a suite of consistency checks on an extracted Invoice.

    Checks performed:
    1. ``line_item_totals``   — each line item's total ≈ quantity × unit_price
    2. ``subtotal``           — subtotal ≈ sum of all line item totals
    3. ``tax_amount``         — if tax_rate and subtotal are present, tax_amount ≈ subtotal × tax_rate
    4. ``grand_total``        — total ≈ subtotal + tax_amount (or subtotal if no tax)
    5. ``required_fields``    — invoice_number, invoice_date, vendor_name, total are present
    6. ``positive_amounts``   — subtotal, tax_amount and total must be ≥ 0
    """

    def check(self, invoice: Invoice) -> ConsistencyReport:
        """Run all consistency checks and return a report.

        Args:
            invoice: The Invoice to validate.

        Returns:
            A ConsistencyReport listing any issues found.
        """
        issues: List[ConsistencyIssue] = []
        checks_performed: List[str] = []

        for method_name in [
            "_check_line_item_totals",
            "_check_subtotal",
            "_check_tax_amount",
            "_check_grand_total",
            "_check_required_fields",
            "_check_positive_amounts",
        ]:
            check_name = method_name.lstrip("_")
            checks_performed.append(check_name)
            method = getattr(self, method_name)
            new_issues = method(invoice)
            issues.extend(new_issues)

        report = ConsistencyReport(
            is_consistent=len(issues) == 0,
            issues=issues,
            checks_performed=checks_performed,
        )
        if report.is_consistent:
            logger.info("Invoice passed all %d consistency checks", len(checks_performed))
        else:
            logger.warning(
                "Invoice failed %d consistency check(s): %s",
                len(issues),
                [i.field for i in issues],
            )
        return report

    # ------------------------------------------------------------------
    # Individual check methods
    # ------------------------------------------------------------------

    def _check_line_item_totals(self, invoice: Invoice) -> List[ConsistencyIssue]:
        """Verify each line item's total ≈ quantity × unit_price."""
        issues: List[ConsistencyIssue] = []
        for idx, item in enumerate(invoice.line_items):
            expected = (item.quantity * item.unit_price).quantize(
                Decimal("0.01"), rounding=ROUND_HALF_UP
            )
            actual = item.total.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)
            if abs(expected - actual) > _TOLERANCE:
                issues.append(
                    ConsistencyIssue(
                        field=f"line_items[{idx}].total",
                        expected=_fmt(expected),
                        actual=_fmt(actual),
                        message=(
                            f"Line item {idx + 1} ('{item.description}'): "
                            f"total {_fmt(actual)} ≠ quantity {_fmt(item.quantity)} "
                            f"× unit_price {_fmt(item.unit_price)} = {_fmt(expected)}"
                        ),
                    )
                )
        return issues

    def _check_subtotal(self, invoice: Invoice) -> List[ConsistencyIssue]:
        """Verify subtotal ≈ sum of line item totals."""
        if not invoice.line_items:
            return []
        if invoice.subtotal is None:
            return []

        expected = sum(
            item.total for item in invoice.line_items
        ).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)
        actual = invoice.subtotal.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)

        if abs(expected - actual) > _TOLERANCE:
            return [
                ConsistencyIssue(
                    field="subtotal",
                    expected=_fmt(expected),
                    actual=_fmt(actual),
                    message=(
                        f"Subtotal {_fmt(actual)} ≠ sum of line item totals {_fmt(expected)}"
                    ),
                )
            ]
        return []

    def _check_tax_amount(self, invoice: Invoice) -> List[ConsistencyIssue]:
        """Verify tax_amount ≈ subtotal × tax_rate (when both are provided)."""
        if invoice.tax_rate is None or invoice.subtotal is None:
            return []
        if invoice.tax_amount is None:
            return []

        expected = (invoice.subtotal * invoice.tax_rate).quantize(
            Decimal("0.01"), rounding=ROUND_HALF_UP
        )
        actual = invoice.tax_amount.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)

        if abs(expected - actual) > _TOLERANCE:
            return [
                ConsistencyIssue(
                    field="tax_amount",
                    expected=_fmt(expected),
                    actual=_fmt(actual),
                    message=(
                        f"Tax amount {_fmt(actual)} ≠ subtotal {_fmt(invoice.subtotal)} "
                        f"× tax rate {invoice.tax_rate} = {_fmt(expected)}"
                    ),
                )
            ]
        return []

    def _check_grand_total(self, invoice: Invoice) -> List[ConsistencyIssue]:
        """Verify total ≈ subtotal + tax_amount (or subtotal when no tax)."""
        if invoice.total is None or invoice.subtotal is None:
            return []

        tax = invoice.tax_amount or Decimal("0")
        expected = (invoice.subtotal + tax).quantize(
            Decimal("0.01"), rounding=ROUND_HALF_UP
        )
        actual = invoice.total.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)

        if abs(expected - actual) > _TOLERANCE:
            return [
                ConsistencyIssue(
                    field="total",
                    expected=_fmt(expected),
                    actual=_fmt(actual),
                    message=(
                        f"Total {_fmt(actual)} ≠ subtotal {_fmt(invoice.subtotal)} "
                        f"+ tax {_fmt(tax)} = {_fmt(expected)}"
                    ),
                )
            ]
        return []

    @staticmethod
    def _check_required_fields(invoice: Invoice) -> List[ConsistencyIssue]:
        """Check that key fields are populated."""
        issues: List[ConsistencyIssue] = []
        required = {
            "invoice_number": invoice.invoice_number,
            "invoice_date": invoice.invoice_date,
            "vendor_name": invoice.vendor_name,
            "total": invoice.total,
        }
        for field, value in required.items():
            if value is None:
                issues.append(
                    ConsistencyIssue(
                        field=field,
                        expected="non-null value",
                        actual="null",
                        message=f"Required field '{field}' is missing from the invoice",
                    )
                )
        return issues

    @staticmethod
    def _check_positive_amounts(invoice: Invoice) -> List[ConsistencyIssue]:
        """Ensure monetary totals are not negative."""
        issues: List[ConsistencyIssue] = []
        checks = {
            "subtotal": invoice.subtotal,
            "tax_amount": invoice.tax_amount,
            "total": invoice.total,
        }
        for field, value in checks.items():
            if value is not None and value < Decimal("0"):
                issues.append(
                    ConsistencyIssue(
                        field=field,
                        expected=">= 0",
                        actual=_fmt(value),
                        message=f"Field '{field}' has a negative value: {_fmt(value)}",
                    )
                )
        return issues
