"""Tests for the OCR extraction component."""

from __future__ import annotations

from pathlib import Path

import pytest

from invoice_extractor.ocr import OCRExtractor, StubOCRBackend, TesseractBackend


class TestStubOCRBackend:
    def test_returns_configured_text(self):
        backend = StubOCRBackend(text="Hello Invoice")
        assert backend.extract_text("any_path.png") == "Hello Invoice"

    def test_returns_empty_by_default(self):
        backend = StubOCRBackend()
        assert backend.extract_text(b"bytes") == ""


class TestOCRExtractor:
    def test_extract_with_stub_backend(self):
        backend = StubOCRBackend(text="  Invoice\n  Total: 100  \n\n\n\nEnd  ")
        extractor = OCRExtractor(backend=backend)
        result = extractor.extract(b"\x00")  # bytes input skips file-existence check
        # Should be cleaned (stripped lines, collapsed blank lines)
        assert "Invoice" in result
        assert "Total: 100" in result
        assert not result.startswith(" ")

    def test_extract_bytes_with_stub_backend(self):
        backend = StubOCRBackend(text="Invoice bytes test")
        extractor = OCRExtractor(backend=backend)
        result = extractor.extract(b"\x00\x01\x02")
        assert result == "Invoice bytes test"

    def test_raises_on_missing_file(self):
        extractor = OCRExtractor(backend=StubOCRBackend())
        with pytest.raises(ValueError, match="not found"):
            extractor.extract("/nonexistent/path/invoice.png")

    def test_clean_text_strips_whitespace(self):
        raw = "  line one  \n  line two  \n\n\n\n\nline three"
        cleaned = OCRExtractor._clean_text(raw)
        lines = cleaned.splitlines()
        for line in lines:
            if line:
                assert line == line.strip()

    def test_clean_text_collapses_blank_lines(self):
        raw = "line1\n\n\n\n\nline2"
        cleaned = OCRExtractor._clean_text(raw)
        # Should not have more than 2 consecutive blank lines
        blank_run = 0
        for line in cleaned.splitlines():
            if line == "":
                blank_run += 1
                assert blank_run <= 2
            else:
                blank_run = 0

    def test_raises_runtime_error_on_backend_failure(self):
        class FailingBackend(StubOCRBackend):
            def extract_text(self, source):
                raise OSError("disk read error")

        extractor = OCRExtractor(backend=FailingBackend())
        with pytest.raises(RuntimeError, match="OCR extraction failed"):
            extractor.extract(b"\x00")
