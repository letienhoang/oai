using OAI.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OAI.Domain.Entities;

public sealed class InvoiceExtractionResult : Entity
{
    public Guid InvoiceId { get; private set; }
    public string EngineName { get; private set; }
    public decimal ConfidenceScore { get; private set; } // 0.0 -> 1.0
    public DateTimeOffset ExtractedAt { get; private set; }
    public bool IsSuccessful { get; private set; }
    public int AttemptNo { get; private set; }
    public string? RawText { get; private set; }
    public string? StructuredJson { get; private set; }

    private InvoiceExtractionResult()
    {
        EngineName = string.Empty;
    }

    public InvoiceExtractionResult(
        Guid invoiceId,
        string engineName,
        decimal confidenceScore,
        int attemptNo,
        bool isSuccessful,
        string? rawText = null,
        string? structuredJson = null)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId cannot be empty.", nameof(invoiceId));

        if (string.IsNullOrWhiteSpace(engineName))
            throw new ArgumentException("EngineName is required.", nameof(engineName));

        if (confidenceScore < 0m || confidenceScore > 1m)
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence score must be between 0 and 1.");

        if (attemptNo <= 0)
            throw new ArgumentOutOfRangeException(nameof(attemptNo), "Attempt number must be greater than zero.");

        InvoiceId = invoiceId;
        EngineName = engineName.Trim();
        ConfidenceScore = confidenceScore;
        AttemptNo = attemptNo;
        IsSuccessful = isSuccessful;
        ExtractedAt = DateTimeOffset.UtcNow;
        RawText = rawText;
        StructuredJson = structuredJson;
    }
}
