"""
Pydantic data models for structured invoice representation.
"""

from __future__ import annotations

from datetime import date
from decimal import Decimal
from typing import List, Optional

from pydantic import BaseModel, Field, field_validator


class LineItem(BaseModel):
    """A single line item on an invoice."""

    description: str = Field(..., description="Description of the product or service")
    quantity: Decimal = Field(..., description="Quantity of units")
    unit_price: Decimal = Field(..., description="Price per unit")
    total: Decimal = Field(..., description="Total for this line item (quantity × unit_price)")

    @field_validator("quantity", "unit_price", "total", mode="before")
    @classmethod
    def coerce_to_decimal(cls, v: object) -> Decimal:
        if v is None:
            return Decimal("0")
        return Decimal(str(v))


class Invoice(BaseModel):
    """Structured representation of an invoice."""

    invoice_number: Optional[str] = Field(None, description="Unique invoice identifier")
    invoice_date: Optional[date] = Field(None, description="Date the invoice was issued")
    due_date: Optional[date] = Field(None, description="Payment due date")

    vendor_name: Optional[str] = Field(None, description="Name of the vendor/seller")
    vendor_address: Optional[str] = Field(None, description="Vendor's address")

    customer_name: Optional[str] = Field(None, description="Name of the customer/buyer")
    customer_address: Optional[str] = Field(None, description="Customer's address")

    line_items: List[LineItem] = Field(default_factory=list, description="List of line items")

    subtotal: Optional[Decimal] = Field(None, description="Sum of all line item totals before tax")
    tax_rate: Optional[Decimal] = Field(None, description="Tax rate as a decimal (e.g. 0.10 for 10%)")
    tax_amount: Optional[Decimal] = Field(None, description="Total tax amount")
    total: Optional[Decimal] = Field(None, description="Grand total including tax")

    currency: Optional[str] = Field(None, description="Currency code (e.g. USD, VND)")

    notes: Optional[str] = Field(None, description="Additional notes or payment terms")

    @field_validator("subtotal", "tax_rate", "tax_amount", "total", mode="before")
    @classmethod
    def coerce_optional_decimal(cls, v: object) -> Optional[Decimal]:
        if v is None or v == "":
            return None
        return Decimal(str(v))


class ConsistencyIssue(BaseModel):
    """A single consistency issue found during validation."""

    field: str = Field(..., description="The field or check that failed")
    expected: Optional[str] = Field(None, description="The expected value")
    actual: Optional[str] = Field(None, description="The actual value found")
    message: str = Field(..., description="Human-readable description of the issue")


class ConsistencyReport(BaseModel):
    """Report of consistency checks performed on an extracted invoice."""

    is_consistent: bool = Field(..., description="True if no consistency issues were found")
    issues: List[ConsistencyIssue] = Field(
        default_factory=list, description="List of consistency issues found"
    )
    checks_performed: List[str] = Field(
        default_factory=list, description="Names of checks that were performed"
    )


class ExtractedInvoiceResult(BaseModel):
    """Complete result of invoice extraction including the invoice and consistency report."""

    invoice: Invoice = Field(..., description="The extracted invoice data")
    raw_ocr_text: str = Field(..., description="Raw text output from OCR")
    consistency_report: ConsistencyReport = Field(
        ..., description="Report of consistency checks"
    )
    extraction_warnings: List[str] = Field(
        default_factory=list,
        description="Non-fatal warnings encountered during extraction",
    )
