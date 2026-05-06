using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Invoices.Dtos.ExtractionComparison;
using OAI.Infrastructure.Services.Llm;

namespace OAI.Infrastructure.Services;

public sealed class InvoiceExtractionComparisonService : IInvoiceExtractionComparisonService
{
    private readonly RuleBasedInvoiceTextParser _ruleBasedParser;
    private readonly OpenAiInvoiceTextParser _openAiParser;
    private readonly ILogger<InvoiceExtractionComparisonService> _logger;

    public InvoiceExtractionComparisonService(
        RuleBasedInvoiceTextParser ruleBasedParser,
        OpenAiInvoiceTextParser openAiParser,
        ILogger<InvoiceExtractionComparisonService> logger)
    {
        _ruleBasedParser = ruleBasedParser;
        _openAiParser = openAiParser;
        _logger = logger;
    }

    public async Task<InvoiceExtractionComparisonDto> CompareAsync(
        Guid invoiceId,
        string invoiceNumber,
        string rawText,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Start comparing rule-based and AI extraction. InvoiceId: {InvoiceId}",
            invoiceId);

        var ruleBasedExtracted = await _ruleBasedParser.ParseAsync(
            rawText,
            "comparison-raw-text",
            1.0m,
            "Tesseract",
            cancellationToken);

        var aiExtracted = await _openAiParser.ParseAsync(
            rawText,
            "comparison-raw-text",
            1.0m,
            "Tesseract",
            cancellationToken);

        var ruleBasedResult = MapResult(
            ruleBasedExtracted,
            "Tesseract+RuleBased",
            "Rule-based parser could not extract invoice data.");

        var aiResult = MapResult(
            aiExtracted,
            "Tesseract+OpenAI",
            "AI parser is disabled, missing API key, or could not extract invoice data.");

        return new InvoiceExtractionComparisonDto
        {
            InvoiceId = invoiceId,
            InvoiceNumber = invoiceNumber,
            RawText = rawText,
            RuleBasedResult = ruleBasedResult,
            AiResult = aiResult,
            FieldComparisons = BuildFieldComparisons(ruleBasedResult, aiResult)
        };
    }

    private static ParserExtractionResultDto MapResult(
        ExtractedInvoiceDto? extracted,
        string engineName,
        string errorMessage)
    {
        if (extracted is null)
        {
            return new ParserExtractionResultDto
            {
                IsAvailable = false,
                EngineName = engineName,
                ErrorMessage = errorMessage
            };
        }

        return new ParserExtractionResultDto
        {
            IsAvailable = true,
            EngineName = extracted.EngineName,
            VendorName = extracted.VendorName,
            InvoiceNumber = extracted.InvoiceNumber,
            IssueDate = extracted.IssueDate,
            DueDate = extracted.DueDate,
            Currency = extracted.Currency,
            DeclaredSubtotal = extracted.DeclaredSubtotal,
            DeclaredTaxAmount = extracted.DeclaredTaxAmount,
            DeclaredTotalAmount = extracted.DeclaredTotalAmount,
            LineItemCount = extracted.LineItems.Count
        };
    }

    private static List<FieldComparisonDto> BuildFieldComparisons(
        ParserExtractionResultDto ruleBased,
        ParserExtractionResultDto ai)
    {
        return new List<FieldComparisonDto>
        {
            Compare("VendorName", ruleBased.VendorName, ai.VendorName),
            Compare("InvoiceNumber", ruleBased.InvoiceNumber, ai.InvoiceNumber),
            Compare("IssueDate", FormatDate(ruleBased.IssueDate), FormatDate(ai.IssueDate)),
            Compare("DueDate", FormatDate(ruleBased.DueDate), FormatDate(ai.DueDate)),
            Compare("Currency", ruleBased.Currency, ai.Currency),
            Compare("DeclaredSubtotal", FormatDecimal(ruleBased.DeclaredSubtotal), FormatDecimal(ai.DeclaredSubtotal)),
            Compare("DeclaredTaxAmount", FormatDecimal(ruleBased.DeclaredTaxAmount), FormatDecimal(ai.DeclaredTaxAmount)),
            Compare("DeclaredTotalAmount", FormatDecimal(ruleBased.DeclaredTotalAmount), FormatDecimal(ai.DeclaredTotalAmount)),
            Compare("LineItemCount", ruleBased.LineItemCount.ToString(), ai.LineItemCount.ToString())
        };
    }

    private static FieldComparisonDto Compare(
        string fieldName,
        string ruleBasedValue,
        string aiValue)
    {
        return new FieldComparisonDto
        {
            FieldName = fieldName,
            RuleBasedValue = ruleBasedValue,
            AiValue = aiValue,
            IsSame = string.Equals(
                NormalizeValue(ruleBasedValue),
                NormalizeValue(aiValue),
                StringComparison.OrdinalIgnoreCase)
        };
    }

    private static string NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static string FormatDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd") ?? string.Empty;
    }

    private static string FormatDecimal(decimal? value)
    {
        return value?.ToString("0.##") ?? string.Empty;
    }
}