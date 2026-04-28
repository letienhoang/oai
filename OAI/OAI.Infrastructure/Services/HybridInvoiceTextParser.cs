using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;
using OAI.Infrastructure.Services.Llm;

namespace OAI.Infrastructure.Services;

public sealed class HybridInvoiceTextParser : IInvoiceTextParser
{
    private readonly OpenAiInvoiceTextParser _llmParser;
    private readonly RuleBasedInvoiceTextParser _ruleBasedParser;
    private readonly ILogger<HybridInvoiceTextParser> _logger;

    public HybridInvoiceTextParser(
        OpenAiInvoiceTextParser llmParser,
        RuleBasedInvoiceTextParser ruleBasedParser,
        ILogger<HybridInvoiceTextParser> logger)
    {
        _llmParser = llmParser;
        _ruleBasedParser = ruleBasedParser;
        _logger = logger;
    }

    public async Task<ExtractedInvoiceDto?> ParseAsync(
        string rawText,
        string sourceFileName,
        decimal confidenceScore,
        string ocrEngineName,
        CancellationToken cancellationToken = default)
    {
        var llmResult = await _llmParser.ParseAsync(
            rawText,
            sourceFileName,
            confidenceScore,
            ocrEngineName,
            cancellationToken);

        if (llmResult is not null)
        {
            _logger.LogInformation(
                "Invoice parsed successfully using LLM parser. InvoiceNumber: {InvoiceNumber}",
                llmResult.InvoiceNumber);

            return llmResult;
        }

        _logger.LogWarning("LLM parser failed or disabled. Falling back to rule-based parser.");

        var ruleBasedResult = await _ruleBasedParser.ParseAsync(
            rawText,
            sourceFileName,
            confidenceScore,
            ocrEngineName,
            cancellationToken);

        if (ruleBasedResult is not null)
        {
            _logger.LogInformation(
                "Invoice parsed successfully using rule-based parser. InvoiceNumber: {InvoiceNumber}",
                ruleBasedResult.InvoiceNumber);
        }

        return ruleBasedResult;
    }
}