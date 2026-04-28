using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;
using OAI.Infrastructure.Options;
using OpenAI.Chat;

namespace OAI.Infrastructure.Services.Llm;

public sealed class OpenAiInvoiceTextParser : IInvoiceTextParser
{
    private readonly LlmOptions _options;
    private readonly ILogger<OpenAiInvoiceTextParser> _logger;

    public OpenAiInvoiceTextParser(
        IOptions<LlmOptions> options,
        ILogger<OpenAiInvoiceTextParser> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExtractedInvoiceDto?> ParseAsync(
        string rawText,
        string sourceFileName,
        decimal confidenceScore,
        string ocrEngineName,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("LLM parser is enabled but API key is missing.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(rawText))
            return null;

        try
        {
            var trimmedText = rawText.Length > _options.MaxInputCharacters
                ? rawText[.._options.MaxInputCharacters]
                : rawText;

            var client = new ChatClient(
                model: _options.Model,
                apiKey: _options.ApiKey);

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(
                    """
                    You extract invoice data from OCR text.

                    Return only valid JSON.
                    Do not wrap the response in markdown.
                    Do not explain.

                    Rules:
                    - Use null if a field is not found.
                    - Dates must be ISO format yyyy-MM-dd.
                    - Currency should be VND, USD, or EUR.
                    - Amounts must be numbers, not strings.
                    - Preserve invoice number exactly if possible, e.g. INV-2026-001.
                    - VendorName should be the seller/company name, not a random OCR noise token.
                    - LineItems must include description, quantity, unitPrice, and taxRate.
                    """),
                new UserChatMessage(
                    $$"""
                    Extract this invoice OCR text into the JSON shape below.

                    JSON shape:
                    {
                      "vendorName": "",
                      "vendorTaxNumber": null,
                      "vendorAddress": null,
                      "vendorEmail": null,
                      "invoiceNumber": "",
                      "issueDate": "yyyy-MM-dd",
                      "dueDate": "yyyy-MM-dd or null",
                      "currency": "VND",
                      "declaredSubtotal": 0,
                      "declaredTaxAmount": 0,
                      "declaredTotalAmount": 0,
                      "lineItems": [
                        {
                          "lineNo": 1,
                          "description": "",
                          "quantity": 1,
                          "unitPrice": 0,
                          "taxRate": 0
                        }
                      ]
                    }

                    OCR text:
                    {{trimmedText}}
                    """)
            };

            var completion = await client.CompleteChatAsync(
                messages,
                cancellationToken: cancellationToken);

            var json = completion.Value.Content
                .Where(x => x.Kind == ChatMessageContentPartKind.Text)
                .Select(x => x.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("LLM parser returned empty content.");
                return null;
            }

            var parsed = JsonSerializer.Deserialize<ParsedInvoiceLlmResult>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (parsed is null)
            {
                _logger.LogWarning("LLM parser returned invalid JSON.");
                return null;
            }

            return MapToExtractedInvoiceDto(
                parsed,
                rawText,
                confidenceScore,
                engineName: $"{ocrEngineName}+OpenAI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM invoice parsing failed.");
            return null;
        }
    }

    private static ExtractedInvoiceDto? MapToExtractedInvoiceDto(
        ParsedInvoiceLlmResult parsed,
        string rawText,
        decimal confidenceScore,
        string engineName)
    {
        if (string.IsNullOrWhiteSpace(parsed.InvoiceNumber))
            return null;

        if (!TryParseDate(parsed.IssueDate, out var issueDate))
            return null;

        DateOnly? dueDate = null;
        if (!string.IsNullOrWhiteSpace(parsed.DueDate) &&
            TryParseDate(parsed.DueDate, out var parsedDueDate))
        {
            dueDate = parsedDueDate;
        }

        if (parsed.DeclaredTotalAmount <= 0)
            return null;

        var currency = string.IsNullOrWhiteSpace(parsed.Currency)
            ? "VND"
            : parsed.Currency.Trim().ToUpperInvariant();

        var lineItems = parsed.LineItems
            .OrderBy(x => x.LineNo)
            .Select(x => new InvoiceLineItemRequestDto
            {
                LineNo = x.LineNo <= 0 ? 1 : x.LineNo,
                Description = string.IsNullOrWhiteSpace(x.Description)
                    ? "Extracted item"
                    : x.Description.Trim(),
                Quantity = x.Quantity <= 0 ? 1 : x.Quantity,
                UnitPrice = x.UnitPrice < 0 ? 0 : x.UnitPrice,
                TaxRate = x.TaxRate < 0 ? 0 : x.TaxRate
            })
            .ToList();

        if (lineItems.Count == 0)
        {
            lineItems.Add(new InvoiceLineItemRequestDto
            {
                LineNo = 1,
                Description = "Auto extracted item",
                Quantity = 1,
                UnitPrice = parsed.DeclaredSubtotal > 0
                    ? parsed.DeclaredSubtotal
                    : parsed.DeclaredTotalAmount,
                TaxRate = 0
            });
        }

        return new ExtractedInvoiceDto
        {
            VendorName = string.IsNullOrWhiteSpace(parsed.VendorName)
                ? "Unknown Vendor"
                : parsed.VendorName.Trim(),
            VendorTaxNumber = parsed.VendorTaxNumber,
            VendorAddress = parsed.VendorAddress,
            VendorEmail = parsed.VendorEmail,
            InvoiceNumber = parsed.InvoiceNumber.Trim(),
            IssueDate = issueDate,
            DueDate = dueDate,
            Currency = currency,
            DeclaredSubtotal = parsed.DeclaredSubtotal,
            DeclaredTaxAmount = parsed.DeclaredTaxAmount,
            DeclaredTotalAmount = parsed.DeclaredTotalAmount,
            ConfidenceScore = Math.Clamp(confidenceScore, 0m, 1m),
            EngineName = engineName,
            RawText = rawText,
            LineItems = lineItems
        };
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var formats = new[]
        {
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "yyyy/MM/dd"
        };

        if (DateTime.TryParseExact(
                value.Trim(),
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        if (DateTime.TryParse(
                value.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        return false;
    }
}