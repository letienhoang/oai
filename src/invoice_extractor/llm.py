"""
LLM component for extracting structured invoice data from OCR text.

Uses the OpenAI Chat Completions API (or a compatible interface) to parse
unstructured OCR text into a validated Invoice Pydantic model.
"""

from __future__ import annotations

import abc
import json
import logging
from typing import Any, Dict, Optional

from .models import Invoice

logger = logging.getLogger(__name__)

# System prompt instructing the LLM on extraction behaviour
_SYSTEM_PROMPT = """\
You are an expert invoice parser. Given the raw text extracted from an invoice via OCR,
extract all relevant fields and return them as a JSON object matching this schema:

{
  "invoice_number": "<string or null>",
  "invoice_date": "<YYYY-MM-DD or null>",
  "due_date": "<YYYY-MM-DD or null>",
  "vendor_name": "<string or null>",
  "vendor_address": "<string or null>",
  "customer_name": "<string or null>",
  "customer_address": "<string or null>",
  "line_items": [
    {
      "description": "<string>",
      "quantity": <number>,
      "unit_price": <number>,
      "total": <number>
    }
  ],
  "subtotal": <number or null>,
  "tax_rate": <decimal fraction e.g. 0.10 for 10%, or null>,
  "tax_amount": <number or null>,
  "total": <number or null>,
  "currency": "<ISO currency code or null>",
  "notes": "<string or null>"
}

Rules:
- All monetary values must be plain numbers (no currency symbols).
- If a field is not present in the text, return null.
- For line_item totals, use the value stated in the invoice; do not compute it.
- Return ONLY the JSON object — no markdown, no explanation.
"""


class BaseLLMBackend(abc.ABC):
    """Abstract base class for LLM backends."""

    @abc.abstractmethod
    def complete(self, system_prompt: str, user_message: str) -> str:
        """Send a chat completion request and return the assistant's reply.

        Args:
            system_prompt: Instruction for the model.
            user_message: The user content (OCR text in our case).

        Returns:
            The model's text response.
        """


class OpenAIBackend(BaseLLMBackend):
    """LLM backend powered by the OpenAI Chat Completions API."""

    def __init__(
        self,
        api_key: Optional[str] = None,
        model: str = "gpt-4o-mini",
        temperature: float = 0.0,
        max_tokens: int = 2048,
    ) -> None:
        """
        Args:
            api_key: OpenAI API key. Falls back to the OPENAI_API_KEY env variable.
            model: Model name to use (e.g. "gpt-4o", "gpt-4o-mini").
            temperature: Sampling temperature. Use 0 for deterministic output.
            max_tokens: Maximum tokens in the completion.
        """
        try:
            from openai import OpenAI
        except ImportError as exc:
            raise ImportError(
                "openai package is required for OpenAIBackend. "
                "Install it with: pip install openai"
            ) from exc

        self._client = OpenAI(api_key=api_key)
        self._model = model
        self._temperature = temperature
        self._max_tokens = max_tokens

    def complete(self, system_prompt: str, user_message: str) -> str:
        response = self._client.chat.completions.create(
            model=self._model,
            temperature=self._temperature,
            max_tokens=self._max_tokens,
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_message},
            ],
        )
        content: str = response.choices[0].message.content or ""
        logger.debug("LLM response length: %d characters", len(content))
        return content


class StubLLMBackend(BaseLLMBackend):
    """Stub LLM backend for testing. Returns pre-configured JSON responses."""

    def __init__(self, response_json: Optional[Dict[str, Any]] = None) -> None:
        self._response_json: Dict[str, Any] = response_json or {}

    def complete(self, system_prompt: str, user_message: str) -> str:  # noqa: ARG002
        return json.dumps(self._response_json)


class LLMExtractor:
    """Extracts a structured Invoice from raw OCR text using an LLM."""

    def __init__(
        self,
        backend: Optional[BaseLLMBackend] = None,
        system_prompt: Optional[str] = None,
    ) -> None:
        """
        Args:
            backend: LLM backend to use. Defaults to OpenAIBackend.
            system_prompt: Override the default system prompt.
        """
        if backend is None:
            backend = OpenAIBackend()
        self._backend = backend
        self._system_prompt = system_prompt or _SYSTEM_PROMPT

    def extract(self, ocr_text: str) -> Invoice:
        """Parse OCR text into a structured Invoice model.

        Args:
            ocr_text: Raw text extracted from an invoice via OCR.

        Returns:
            A validated Invoice instance.

        Raises:
            ValueError: If the LLM response cannot be parsed into an Invoice.
            RuntimeError: If the LLM call itself fails.
        """
        if not ocr_text.strip():
            logger.warning("OCR text is empty; returning empty Invoice")
            return Invoice()

        logger.info("Sending OCR text to LLM for structured extraction")
        try:
            raw_response = self._backend.complete(self._system_prompt, ocr_text)
        except Exception as exc:
            raise RuntimeError(f"LLM call failed: {exc}") from exc

        invoice_data = self._parse_json(raw_response)
        try:
            invoice = Invoice(**invoice_data)
        except Exception as exc:
            raise ValueError(
                f"LLM response could not be validated as an Invoice: {exc}\n"
                f"Raw response: {raw_response}"
            ) from exc

        logger.info("Successfully extracted Invoice with %d line item(s)", len(invoice.line_items))
        return invoice

    @staticmethod
    def _parse_json(raw: str) -> Dict[str, Any]:
        """Extract a JSON object from the LLM response string.

        Handles cases where the model wraps the JSON in markdown code fences.
        """
        text = raw.strip()

        # Strip markdown code fences if present
        if text.startswith("```"):
            lines = text.splitlines()
            # Remove opening fence (```json or ```)
            lines = lines[1:]
            # Remove closing fence
            if lines and lines[-1].strip() == "```":
                lines = lines[:-1]
            text = "\n".join(lines).strip()

        try:
            result = json.loads(text)
        except json.JSONDecodeError as exc:
            raise ValueError(
                f"LLM returned invalid JSON: {exc}\nRaw response: {raw}"
            ) from exc

        if not isinstance(result, dict):
            raise ValueError(
                f"Expected a JSON object from LLM, got {type(result).__name__}"
            )
        return result
