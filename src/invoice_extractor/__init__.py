"""
Invoice Extraction System combining OCR and LLM with consistency checking.

This package provides tools to extract structured data from invoice images
by combining Optical Character Recognition (OCR) and Large Language Models (LLM),
then validating the extracted data for internal consistency.
"""

from .extractor import InvoiceExtractor
from .models import Invoice, LineItem, ExtractedInvoiceResult, ConsistencyReport
from .consistency import ConsistencyChecker
from .ocr import OCRExtractor
from .llm import LLMExtractor

__all__ = [
    "InvoiceExtractor",
    "Invoice",
    "LineItem",
    "ExtractedInvoiceResult",
    "ConsistencyReport",
    "ConsistencyChecker",
    "OCRExtractor",
    "LLMExtractor",
]
