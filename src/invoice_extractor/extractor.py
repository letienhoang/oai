"""
Main invoice extraction pipeline.

Combines OCR, LLM extraction, and consistency checking into a single
easy-to-use InvoiceExtractor class.
"""

from __future__ import annotations

import logging
from pathlib import Path
from typing import List, Optional, Union

from .consistency import ConsistencyChecker
from .llm import LLMExtractor, BaseLLMBackend
from .models import ExtractedInvoiceResult
from .ocr import OCRExtractor, BaseOCRBackend

logger = logging.getLogger(__name__)


class InvoiceExtractor:
    """End-to-end invoice extraction pipeline.

    Workflow:
    1. **OCR** — extract raw text from the invoice image.
    2. **LLM** — parse the raw text into a structured Invoice model.
    3. **Consistency check** — validate the extracted data for internal consistency.

    Example usage::

        from invoice_extractor import InvoiceExtractor

        extractor = InvoiceExtractor()
        result = extractor.extract("invoice.png")
        print(result.invoice.total)
        print(result.consistency_report.is_consistent)
    """

    def __init__(
        self,
        ocr_backend: Optional[BaseOCRBackend] = None,
        llm_backend: Optional[BaseLLMBackend] = None,
        consistency_checker: Optional[ConsistencyChecker] = None,
    ) -> None:
        """
        Args:
            ocr_backend: OCR backend to use (defaults to TesseractBackend).
            llm_backend: LLM backend to use (defaults to OpenAIBackend).
            consistency_checker: Checker instance (defaults to ConsistencyChecker()).
        """
        self._ocr = OCRExtractor(backend=ocr_backend)
        self._llm = LLMExtractor(backend=llm_backend)
        self._checker = consistency_checker or ConsistencyChecker()

    def extract(
        self, image_source: Union[str, Path, bytes]
    ) -> ExtractedInvoiceResult:
        """Extract structured invoice data from an image.

        Args:
            image_source: Path to the invoice image file, or raw image bytes.

        Returns:
            An ExtractedInvoiceResult containing the invoice, raw OCR text,
            consistency report, and any extraction warnings.

        Raises:
            ValueError: If the image path is invalid or the LLM output cannot
                        be parsed into an Invoice.
            RuntimeError: If OCR or LLM calls fail unexpectedly.
        """
        warnings: List[str] = []

        # Step 1: OCR
        logger.info("Step 1/3: OCR extraction")
        raw_ocr_text = self._ocr.extract(image_source)
        if not raw_ocr_text.strip():
            warnings.append("OCR produced no text; invoice fields will likely be empty.")

        # Step 2: LLM structured extraction
        logger.info("Step 2/3: LLM structured extraction")
        invoice = self._llm.extract(raw_ocr_text)

        # Step 3: Consistency check
        logger.info("Step 3/3: Consistency check")
        report = self._checker.check(invoice)

        return ExtractedInvoiceResult(
            invoice=invoice,
            raw_ocr_text=raw_ocr_text,
            consistency_report=report,
            extraction_warnings=warnings,
        )

    def extract_from_text(self, ocr_text: str) -> ExtractedInvoiceResult:
        """Extract structured invoice data from pre-extracted OCR text.

        Useful when you already have the OCR text and want to skip the OCR step.

        Args:
            ocr_text: Raw text extracted from an invoice.

        Returns:
            An ExtractedInvoiceResult (raw_ocr_text will contain the input text).
        """
        warnings: List[str] = []
        if not ocr_text.strip():
            warnings.append("Provided OCR text is empty; invoice fields will likely be empty.")

        logger.info("Extracting from pre-extracted OCR text")
        invoice = self._llm.extract(ocr_text)
        report = self._checker.check(invoice)

        return ExtractedInvoiceResult(
            invoice=invoice,
            raw_ocr_text=ocr_text,
            consistency_report=report,
            extraction_warnings=warnings,
        )
