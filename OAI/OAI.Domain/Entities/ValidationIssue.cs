using OAI.Domain.Common;
using OAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OAI.Domain.Entities;

public sealed class ValidationIssue : Entity
{
    public Guid InvoiceId { get; private set; }
    public string FieldName { get; private set; }
    public string RuleCode { get; private set; }
    public string Message { get; private set; }
    public ValidationSeverity Severity { get; private set; }
    public DateTimeOffset DetectedAt { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private ValidationIssue()
    {
        FieldName = string.Empty;
        RuleCode = string.Empty;
        Message = string.Empty;
    }

    public ValidationIssue(
        Guid invoiceId,
        string fieldName,
        string ruleCode,
        string message,
        ValidationSeverity severity)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId cannot be empty.", nameof(invoiceId));

        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("FieldName is required.", nameof(fieldName));

        if (string.IsNullOrWhiteSpace(ruleCode))
            throw new ArgumentException("RuleCode is required.", nameof(ruleCode));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        InvoiceId = invoiceId;
        FieldName = fieldName.Trim();
        RuleCode = ruleCode.Trim();
        Message = message.Trim();
        Severity = severity;
        DetectedAt = DateTimeOffset.UtcNow;
    }

    public void Resolve()
    {
        if (IsResolved) return;

        IsResolved = true;
        ResolvedAt = DateTimeOffset.UtcNow;
        Touch();
    }
}
