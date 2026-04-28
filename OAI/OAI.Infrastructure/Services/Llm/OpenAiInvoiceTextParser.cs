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
    
    private const string SystemPrompt = 
        """
        You are an invoice extraction engine.

        Your task is to extract structured invoice data from OCR text.

        Important rules:
        - Return data that matches the provided JSON schema.
        - Do not invent values that are not supported by the OCR text.
        - Ignore OCR noise tokens such as single random characters, broken logo text, or isolated punctuation.
        - The vendorName is usually the seller/company name near the top of the document.
        - Preserve invoiceNumber exactly when possible, including prefixes such as INV-, HD-, VAT-, etc.
        - Dates must be normalized to yyyy-MM-dd.
        - Amounts must be numbers, not strings.
        - If total appears on one line and the amount appears on the next line, treat the next numeric amount as the total.
        - VAT, tax, and GTGT mean tax amount.
        - If line item tax rate is not explicit but VAT is 10% of subtotal, use 10 as the taxRate.
        - If a nullable field is not found, return null.
        - If a required string field is not found, return an empty string.
        """;
    
    private static string BuildUserPrompt(string rawText)
    {
        return $$"""
                 Extract invoice data from the OCR text below.

                 OCR text:
                 {{rawText}}
                 """;
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
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(BuildUserPrompt(trimmedText))
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "invoice_extraction",
                    jsonSchema: BinaryData.FromString(InvoiceExtractionJsonSchema.Schema),
                    jsonSchemaIsStrict: true)
            };

            var completion = await client.CompleteChatAsync(
                messages,
                options,
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
        catch (Exception ex) when (ex.Message.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "OpenAI API quota is insufficient. Please check billing and quota.");
            return null;
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

        var currency = string.IsNullOrWhiteSpace(parsed.Currency)
            ? "VND"
            : parsed.Currency.Trim().ToUpperInvariant();

        var subtotal = parsed.DeclaredSubtotal;
        var tax = parsed.DeclaredTaxAmount;
        var total = parsed.DeclaredTotalAmount;

        if (total <= 0)
            return null;

        if (subtotal <= 0 && total > 0)
            subtotal = Math.Max(total - tax, 0m);

        if (tax <= 0 && subtotal > 0 && total > subtotal)
            tax = total - subtotal;

        var inferredTaxRate = subtotal > 0 && tax > 0
            ? Math.Round(tax / subtotal * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var lineItems = parsed.LineItems
            .OrderBy(x => x.LineNo)
            .Select((x, index) => new InvoiceLineItemRequestDto
            {
                LineNo = x.LineNo <= 0 ? index + 1 : x.LineNo,
                Description = string.IsNullOrWhiteSpace(x.Description)
                    ? "Extracted item"
                    : x.Description.Trim(),
                Quantity = x.Quantity <= 0 ? 1 : x.Quantity,
                UnitPrice = x.UnitPrice < 0 ? 0 : x.UnitPrice,
                TaxRate = x.TaxRate > 0 ? x.TaxRate : inferredTaxRate
            })
            .ToList();

        if (lineItems.Count == 0)
        {
            lineItems.Add(new InvoiceLineItemRequestDto
            {
                LineNo = 1,
                Description = "Auto extracted item",
                Quantity = 1,
                UnitPrice = subtotal > 0 ? subtotal : total,
                TaxRate = inferredTaxRate
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
            InvoiceNumber = NormalizeInvoiceNumber(parsed.InvoiceNumber),
            IssueDate = issueDate,
            DueDate = dueDate,
            Currency = currency,
            DeclaredSubtotal = subtotal,
            DeclaredTaxAmount = tax,
            DeclaredTotalAmount = total,
            ConfidenceScore = Math.Clamp(confidenceScore, 0m, 1m),
            EngineName = engineName,
            RawText = rawText,
            LineItems = lineItems
        };
    }
    
    private static string NormalizeInvoiceNumber(string value)
    {
        return value.Trim();
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