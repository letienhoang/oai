"""
OCR component for extracting text from invoice images.

Supports multiple OCR backends:
- pytesseract (local, requires Tesseract installation)
- A stub/mock backend for testing without dependencies
"""

from __future__ import annotations

import abc
import io
import logging
from pathlib import Path
from typing import Optional, Union

logger = logging.getLogger(__name__)


class BaseOCRBackend(abc.ABC):
    """Abstract base class for OCR backends."""

    @abc.abstractmethod
    def extract_text(self, image_source: Union[str, Path, bytes]) -> str:
        """Extract text from an image.

        Args:
            image_source: Path to the image file, or raw image bytes.

        Returns:
            Extracted text as a string.
        """


class TesseractBackend(BaseOCRBackend):
    """OCR backend using pytesseract (Tesseract OCR engine)."""

    def __init__(self, lang: str = "eng", config: str = "--psm 6") -> None:
        """
        Args:
            lang: Tesseract language code(s), e.g. "eng" or "eng+vie".
            config: Additional Tesseract configuration string.
        """
        try:
            import pytesseract
            from PIL import Image  # noqa: F401
        except ImportError as exc:
            raise ImportError(
                "pytesseract and Pillow are required for TesseractBackend. "
                "Install them with: pip install pytesseract Pillow"
            ) from exc

        self._pytesseract = pytesseract
        self._lang = lang
        self._config = config

    def extract_text(self, image_source: Union[str, Path, bytes]) -> str:
        """Extract text from an image using Tesseract OCR.

        Args:
            image_source: Path to the image file, or raw image bytes.

        Returns:
            Extracted text as a string.
        """
        from PIL import Image

        if isinstance(image_source, bytes):
            image = Image.open(io.BytesIO(image_source))
        else:
            image = Image.open(str(image_source))

        text: str = self._pytesseract.image_to_string(
            image, lang=self._lang, config=self._config
        )
        logger.debug("Tesseract extracted %d characters", len(text))
        return text


class StubOCRBackend(BaseOCRBackend):
    """Stub OCR backend for testing. Returns pre-configured text."""

    def __init__(self, text: str = "") -> None:
        self._text = text

    def extract_text(self, image_source: Union[str, Path, bytes]) -> str:  # noqa: ARG002
        return self._text


class OCRExtractor:
    """High-level OCR extractor that preprocesses images and extracts text."""

    def __init__(self, backend: Optional[BaseOCRBackend] = None) -> None:
        """
        Args:
            backend: OCR backend to use. When *None* (the default) a
                     :class:`TesseractBackend` is created lazily on the first
                     call to :meth:`extract`, so that importing the package
                     does not fail when pytesseract is not installed.
        """
        self._backend: Optional[BaseOCRBackend] = backend

    def _get_backend(self) -> BaseOCRBackend:
        """Return the backend, creating TesseractBackend lazily if needed."""
        if self._backend is None:
            self._backend = TesseractBackend()
        return self._backend

    def extract(self, image_source: Union[str, Path, bytes]) -> str:
        """Extract text from an invoice image.

        Args:
            image_source: Path to the image file (JPEG, PNG, TIFF, PDF page, etc.),
                          or raw image bytes.

        Returns:
            Extracted text as a string.

        Raises:
            ValueError: If the image_source is invalid.
            RuntimeError: If OCR extraction fails.
        """
        if isinstance(image_source, (str, Path)):
            path = Path(image_source)
            if not path.exists():
                raise ValueError(f"Image file not found: {path}")
            logger.info("Extracting text from image: %s", path)
        else:
            logger.info("Extracting text from image bytes (%d bytes)", len(image_source))

        try:
            text = self._get_backend().extract_text(image_source)
        except Exception as exc:
            raise RuntimeError(f"OCR extraction failed: {exc}") from exc

        cleaned = self._clean_text(text)
        logger.debug("Cleaned OCR text has %d characters", len(cleaned))
        return cleaned

    @staticmethod
    def _clean_text(text: str) -> str:
        """Remove excessive whitespace while preserving meaningful line breaks."""
        lines = text.splitlines()
        # Strip leading/trailing whitespace from each line
        lines = [line.strip() for line in lines]
        # Remove runs of blank lines (more than 2 consecutive)
        cleaned_lines = []
        blank_count = 0
        for line in lines:
            if line == "":
                blank_count += 1
                if blank_count <= 2:
                    cleaned_lines.append(line)
            else:
                blank_count = 0
                cleaned_lines.append(line)
        return "\n".join(cleaned_lines).strip()
