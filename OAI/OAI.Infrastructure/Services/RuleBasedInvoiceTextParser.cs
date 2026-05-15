using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;

namespace OAI.Infrastructure.Services;

public sealed class RuleBasedInvoiceTextParser : IInvoiceTextParser
{
    private static readonly string[] InvoiceNumberLabels =
    [
        "invoice number",
        "invoice no",
        "invoice #",
        "inv no",
        "inv #",
        "document number",
        "bill number",
        "reference",
        "reference number",
        "số hóa đơn",
        "mã hóa đơn"
    ];

    private static readonly string[] InvoiceDateLabels =
    [
        "date",
        "invoice date",
        "issue date",
        "issued date",
        "billing date",
        "document date",
        "ngày",
        "ngày hóa đơn",
        "ngày lập"
    ];

    private static readonly string[] DueDateLabels =
    [
        "due date",
        "payment due",
        "payment due date",
        "pay by",
        "due",
        "hạn thanh toán",
        "ngày đến hạn"
    ];

    private static readonly string[] SubtotalLabels =
    [
        "sub total",
        "subtotal",
        "net total",
        "net amount",
        "amount before tax",
        "total before tax",
        "pre-tax total",
        "taxable amount",
        "before tax",
        "tạm tính",
        "cộng tiền hàng",
        "tiền hàng",
        "giá trị trước thuế"
    ];

    private static readonly string[] TaxLabels =
    [
        "tax total",
        "sales tax",
        "vat total",
        "gst",
        "gst amount",
        "taxes",
        "vat",
        "tax amount",
        "vat amount",
        "thuế",
        "thuế gtgt",
        "tiền thuế",
        "tiền thuế gtgt"
    ];

    private static readonly string[] TotalLabels =
    [
        "invoice total",
        "total due",
        "balance due",
        "amount payable",
        "payment total",
        "total payable",
        "total incl. vat",
        "total including tax",
        "total after tax",
        "final total",
        "payable amount",
        "total",
        "grand total",
        "total amount",
        "amount due",
        "thành tiền",
        "số tiền thanh toán",
        "tổng thanh toán",
        "tổng giá trị thanh toán",
        "tổng cộng",
        "tổng tiền",
        "thanh toán"
    ];

    private static readonly string[] KnownAmountLabels =
        [.. SubtotalLabels, .. TaxLabels, .. TotalLabels];

    private static readonly string[] MetadataLabels =
    [
        "tax code",
        "tax no",
        "tax number",
        "tax id",
        "vat code",
        "vat number",
        "mst",
        "mã số thuế",
        "customer tax id",
        "customer tax code",
        "phone",
        "tel",
        "email",
        "address",
        "invoice number",
        "invoice no",
        "date",
        "invoice date",
        "due date",
        "customer",
        "currency",
        "payment"
    ];

    private static readonly string[] FollowingInvoiceNumberLabels =
    [
        "invoice date",
        "issue date",
        "issued date",
        "date",
        "due date",
        "customer",
        "currency",
        "payment",
        "payment method",
        "address",
        "phone",
        "email"
    ];

    private static readonly string[] InvoiceNumberRejectLabels =
    [
        "customer",
        "due date",
        "invoice date",
        "payment",
        "currency",
        "address",
        "phone",
        "email"
    ];

    private static readonly string[] StrictInvoiceCodePatterns =
    [
        @"(?i)\b(?<value>[A-Z]{2,10}-\d{4}-\d{2,6})\b",
        @"(?i)\b(?<value>INV[-\/]?\d{4}[-\/]?\d{3,})\b",
        @"(?i)\b(?<value>INV[-\/]?[A-Z0-9]+[-\/]?[A-Z0-9]+)\b",
        @"(?i)\b(?<value>[A-Z]{2,}[-\/]\d{4}[-\/]\d{2,})\b",
        @"(?i)\b(?<value>HD[-\/]?\d{4}[-\/]?\d{2,})\b"
    ];

    private static readonly Regex AmountRegex = new(
        @"(?<![\w])(?:[$€₫]\s*)?(?<value>\d{1,3}(?:[,.]\d{3})+(?:[,.]\d{1,2})?|\d+(?:[,.]\d{1,2})?)(?:\s*(?:USD|EUR|VND|VNĐ|đ|₫))?(?![\w])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ILogger<RuleBasedInvoiceTextParser> _logger;

    public RuleBasedInvoiceTextParser()
        : this(NullLogger<RuleBasedInvoiceTextParser>.Instance)
    {
    }

    public RuleBasedInvoiceTextParser(ILogger<RuleBasedInvoiceTextParser> logger)
    {
        _logger = logger;
    }

    public Task<ExtractedInvoiceDto?> ParseAsync(
        string rawText,
        string sourceFileName,
        decimal confidenceScore,
        string ocrEngineName,
        CancellationToken cancellationToken = default)
    {
        var result = ParseInternal(rawText, sourceFileName, confidenceScore, ocrEngineName);
        return Task.FromResult(result);
    }
    
    public ExtractedInvoiceDto? ParseInternal(
        string rawText,
        string sourceFileName,
        decimal confidenceScore,
        string ocrEngineName)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return null;

        var lines = NormalizeLines(rawText);
        if (lines.Count == 0)
            return null;

        var invoiceNumber = ExtractInvoiceNumber(lines, rawText);
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            invoiceNumber = GenerateUnreadInvoiceNumber();

        var currency = DetectCurrency(rawText);

        var vendorName = ExtractVendorName(lines);
        if (string.IsNullOrWhiteSpace(vendorName))
            vendorName = "Unknown Vendor";

        var issueDate =
            ExtractDateByLabel(lines, InvoiceDateLabels)
            ?? ExtractDateByIndex(rawText, 0)
            ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var dueDate =
            ExtractDateByLabel(lines, DueDateLabels)
            ?? ExtractDateByIndex(rawText, 1);

        if (dueDate == issueDate)
        {
            dueDate = ExtractDateByIndex(rawText, 1) ?? dueDate;
        }

        var subtotal =
            ExtractAmountByLabel(
                lines,
                AmountSelection.First,
                allowContainedLabel: false,
                SubtotalLabels);

        var taxAmount =
            ExtractAmountByLabel(
                lines,
                AmountSelection.Last,
                allowContainedLabel: false,
                TaxLabels) ?? 0m;

        var totalAmount =
            ExtractAmountByLabel(
                lines,
                AmountSelection.Last,
                allowContainedLabel: true,
                TotalLabels);

        if (totalAmount is null && subtotal is not null)
        {
            totalAmount = subtotal.Value + taxAmount;
            _logger.LogInformation(
                "Inferred invoice total from subtotal and tax. SourceFileName: {SourceFileName}, Subtotal: {Subtotal}, TaxAmount: {TaxAmount}, TotalAmount: {TotalAmount}",
                sourceFileName,
                subtotal.Value,
                taxAmount,
                totalAmount.Value);
        }

        if (totalAmount is null)
        {
            totalAmount = InferTotalFromLargestAmount(rawText);
            if (totalAmount is not null)
            {
                _logger.LogInformation(
                    "Inferred invoice total from largest amount candidate. SourceFileName: {SourceFileName}, TotalAmount: {TotalAmount}",
                    sourceFileName,
                    totalAmount.Value);
            }
        }

        if (totalAmount is null)
            return null;

        var totalValue = totalAmount.Value;
        if (taxAmount < 0)
        {
            _logger.LogWarning(
                "Parsed invoice tax amount was negative and has been reset. SourceFileName: {SourceFileName}, TaxAmount: {TaxAmount}, TotalAmount: {TotalAmount}",
                sourceFileName,
                taxAmount,
                totalValue);
            taxAmount = 0m;
        }

        if (taxAmount > totalValue)
        {
            _logger.LogWarning(
                "Parsed invoice tax amount exceeded total and has been reset. SourceFileName: {SourceFileName}, TaxAmount: {TaxAmount}, TotalAmount: {TotalAmount}",
                sourceFileName,
                taxAmount,
                totalValue);
            taxAmount = 0m;
        }

        var subtotalValue = subtotal ?? Math.Max(totalValue - taxAmount, 0m);
        if (subtotalValue < 0)
        {
            _logger.LogWarning(
                "Parsed invoice subtotal was negative and has been reset. SourceFileName: {SourceFileName}, Subtotal: {Subtotal}, TotalAmount: {TotalAmount}",
                sourceFileName,
                subtotalValue,
                totalValue);
            subtotalValue = 0m;
        }

        if (subtotalValue > totalValue && taxAmount > 0)
        {
            _logger.LogWarning(
                "Parsed invoice subtotal exceeded total while tax was present. SourceFileName: {SourceFileName}, Subtotal: {Subtotal}, TaxAmount: {TaxAmount}, TotalAmount: {TotalAmount}",
                sourceFileName,
                subtotalValue,
                taxAmount,
                totalValue);
        }

        var lineItems = ExtractLineItems(lines);

        var inferredTaxRate = subtotalValue > 0 && taxAmount > 0
            ? Math.Round(taxAmount / subtotalValue * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        if (inferredTaxRate is < 0m or > 100m)
        {
            _logger.LogWarning(
                "Inferred invoice tax rate was outside the allowed range and has been reset. SourceFileName: {SourceFileName}, Subtotal: {Subtotal}, TaxAmount: {TaxAmount}, TotalAmount: {TotalAmount}, InferredTaxRate: {InferredTaxRate}",
                sourceFileName,
                subtotalValue,
                taxAmount,
                totalValue,
                inferredTaxRate);
            inferredTaxRate = 0m;
        }

        if (lineItems.Count == 0)
        {
            lineItems.Add(new InvoiceLineItemRequestDto
            {
                LineNo = 1,
                Description = "Auto extracted item",
                Quantity = 1,
                UnitPrice = subtotalValue,
                TaxRate = inferredTaxRate
            });
        }
        else if (inferredTaxRate > 0)
        {
            lineItems = lineItems
                .Select(x => new InvoiceLineItemRequestDto
                {
                    LineNo = x.LineNo,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    TaxRate = inferredTaxRate
                })
                .ToList();
        }

        _logger.LogInformation(
            "Invoice parsed successfully using rule-based parser. SourceFileName: {SourceFileName}, InvoiceNumber: {InvoiceNumber}, Subtotal: {Subtotal}, TaxAmount: {TaxAmount}, TotalAmount: {TotalAmount}, InferredTaxRate: {InferredTaxRate}, LineItemCount: {LineItemCount}",
            sourceFileName,
            invoiceNumber,
            subtotalValue,
            taxAmount,
            totalValue,
            inferredTaxRate,
            lineItems.Count);

        return new ExtractedInvoiceDto
        {
            VendorName = vendorName,
            InvoiceNumber = invoiceNumber,
            IssueDate = issueDate,
            DueDate = dueDate,
            Currency = currency,
            DeclaredSubtotal = subtotalValue,
            DeclaredTaxAmount = taxAmount,
            DeclaredTotalAmount = totalValue,
            ConfidenceScore = Math.Clamp(confidenceScore, 0m, 1m),
            EngineName = $"{ocrEngineName}+RuleBased",
            RawText = rawText,
            LineItems = lineItems
        };
    }

    private static List<string> NormalizeLines(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Regex.Replace(x.Trim(), @"\s+", " "))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static string? ExtractInvoiceNumber(IReadOnlyList<string> lines, string rawText)
    {
        var normalizedText = string.Join("\n", lines);

        var invoiceCode = TryRegexGroup(normalizedText, StrictInvoiceCodePatterns, "value");
        if (IsLikelyInvoiceNumber(invoiceCode))
            return invoiceCode;

        invoiceCode = TryRegexGroup(rawText, StrictInvoiceCodePatterns, "value");
        if (IsLikelyInvoiceNumber(invoiceCode))
            return invoiceCode;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (!LineStartsWithAnyLabel(
                    line,
                    InvoiceNumberLabels))
            {
                continue;
            }

            var valuePart = GetValueAfterColon(line);

            var sameLineSegment = valuePart ?? RemoveLeadingLabel(line, InvoiceNumberLabels);
            sameLineSegment = StopAtFollowingLabel(sameLineSegment, FollowingInvoiceNumberLabels);

            var sameLineCode = TryRegexGroup(sameLineSegment, StrictInvoiceCodePatterns, "value");
            if (IsLikelyInvoiceNumber(sameLineCode))
                return sameLineCode;

            if (IsCompactInvoiceNumberToken(sameLineSegment))
                return sameLineSegment.Trim();

            for (var j = i + 1; j < Math.Min(i + 8, lines.Count); j++)
            {
                var candidate = lines[j].Trim();

                if (LineStartsWithAnyLabel(candidate, "invoice date", "due date", "date", "ngày"))
                    continue;

                var code = TryRegexGroup(candidate, StrictInvoiceCodePatterns, "value");

                if (IsLikelyInvoiceNumber(code))
                    return code;

                if (IsCompactInvoiceNumberToken(candidate))
                    return candidate;
            }
        }

        return null;
    }

    private static bool IsLikelyInvoiceNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim();

        var invalidValues = new[]
        {
            "invoice",
            "number",
            "date",
            "due",
            "total",
            "subtotal",
            "tax",
            "vat"
        };

        if (invalidValues.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (normalized.Length < 4)
            return false;

        if (normalized.Length > 50)
            return false;

        if (Regex.Matches(normalized, @"\b[\p{L}\p{N}]+\b").Count > 6)
            return false;

        if (InvoiceNumberRejectLabels.Any(label =>
                normalized.Contains(label, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (Regex.IsMatch(normalized, @"\d{1,4}[\/\.\-]\d{1,2}[\/\.\-]\d{2,4}"))
            return false;

        return normalized.Any(char.IsDigit);
    }

    private static bool IsCompactInvoiceNumberToken(string? value)
    {
        if (!IsLikelyInvoiceNumber(value))
            return false;

        var normalized = value!.Trim();
        if (normalized.Contains(' '))
            return false;

        return Regex.IsMatch(
            normalized,
            @"^[A-Z0-9][A-Z0-9\-\/#_.]*\d[A-Z0-9\-\/#_.]*$",
            RegexOptions.IgnoreCase);
    }

    private static string RemoveLeadingLabel(string line, params string[] labels)
    {
        var normalizedLine = NormalizeLabel(line);
        var matchingLabel = labels
            .Select(label => new
            {
                Original = label,
                Normalized = NormalizeLabel(label)
            })
            .Where(label => normalizedLine.StartsWith(label.Normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(label => label.Normalized.Length)
            .FirstOrDefault();

        if (matchingLabel is null)
            return line;

        var pattern = @"^\s*" + Regex.Escape(matchingLabel.Original).Replace(@"\ ", @"\s+") + @"\b[:#]?\s*";
        return Regex.Replace(line, pattern, string.Empty, RegexOptions.IgnoreCase).Trim();
    }

    private static string StopAtFollowingLabel(string value, params string[] labels)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var earliestIndex = value.Length;

        foreach (var label in labels)
        {
            var pattern = $@"\b{Regex.Escape(label).Replace(@"\ ", @"\s+")}\b";
            var match = Regex.Match(value, pattern, RegexOptions.IgnoreCase);

            if (match.Success && match.Index > 0 && match.Index < earliestIndex)
                earliestIndex = match.Index;
        }

        return value[..earliestIndex].Trim();
    }

    private static string GenerateUnreadInvoiceNumber()
    {
        return $"UNREAD-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    }

    private static string? ExtractVendorName(IReadOnlyList<string> lines)
    {
        var candidates = lines
            .Take(15)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !IsNoiseLine(x))
            .Where(IsLikelyVendorLine)
            .ToList();

        var companyCandidate = candidates
            .Where(x =>
                x.Contains("company", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("software", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("co.,", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("ltd", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("corp", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("công ty", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("tnhh", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("cổ phần", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Length)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(companyCandidate))
            return CleanVendorName(companyCandidate);

        return candidates
            .OrderByDescending(x => x.Length)
            .Select(CleanVendorName)
            .FirstOrDefault();
    }

    private static bool IsLikelyVendorLine(string line)
    {
        if (line.Length < 4)
            return false;

        if (!line.Any(char.IsLetter))
            return false;

        if (line.Count(char.IsLetter) < 3)
            return false;

        if (Regex.IsMatch(line, @"^\W+$"))
            return false;

        if (Regex.IsMatch(line, @"^[A-ZÀ-ỸẠ]{1,2}$", RegexOptions.IgnoreCase))
            return false;

        if (line.Contains(':'))
            return false;

        if (Regex.IsMatch(line, @"\d{1,4}[\/\.\-]\d{1,2}[\/\.\-]\d{2,4}"))
            return false;

        if (TryFindAmount(line, out _))
            return false;

        return true;
    }

    private static string CleanVendorName(string value)
    {
        return value
            .Replace(".", string.Empty)
            .Trim();
    }

    private static DateOnly? ExtractDateByLabel(IReadOnlyList<string> lines, params string[] labels)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (!LineStartsWithAnyLabel(line, labels))
                continue;

            var valuePart = GetValueAfterColon(line);

            if (TryParseDate(valuePart, out var dateFromSameLine))
                return dateFromSameLine;

            for (var j = i + 1; j < Math.Min(i + 6, lines.Count); j++)
            {
                if (LineStartsWithAnyLabel(
                        lines[j],
                        "invoice date",
                        "due date",
                        "date",
                        "subtotal",
                        "total",
                        "vat",
                        "tax",
                        "ngày",
                        "tổng",
                        "thuế"))
                {
                    continue;
                }

                if (TryParseDate(lines[j], out var dateFromNextLine))
                    return dateFromNextLine;
            }
        }

        return null;
    }

    private static DateOnly? ExtractDateByIndex(string text, int index)
    {
        var matches = Regex.Matches(
            text,
            @"(?<value>\d{1,4}[\/\.\-]\d{1,2}[\/\.\-]\d{2,4})",
            RegexOptions.IgnoreCase);

        if (matches.Count <= index)
            return null;

        var value = matches[index].Groups["value"].Value;

        return TryParseDate(value, out var date) ? date : null;
    }

    private enum AmountSelection
    {
        First,
        Last
    }

    private static decimal? ExtractAmountByLabel(
        IReadOnlyList<string> lines,
        AmountSelection selection,
        bool allowContainedLabel,
        params string[] labels)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (IsIdentifierOrMetadataLine(line))
                continue;

            if (!LineMatchesAnyLabel(line, allowContainedLabel, labels))
                continue;

            var valuePart = GetValueAfterColon(line);

            if (TryFindAmount(valuePart ?? line, selection, out var amountFromSameLine))
                return amountFromSameLine;

            for (var j = i + 1; j < Math.Min(i + 9, lines.Count); j++)
            {
                if (IsCurrencyOnlyLine(lines[j])
                    || IsLikelyLabelOnlyLine(lines[j])
                    || IsIdentifierOrMetadataLine(lines[j]))
                {
                    continue;
                }

                if (TryFindAmount(lines[j], selection, out var amountFromNextLine))
                    return amountFromNextLine;
            }
        }

        return null;
    }

    private static List<InvoiceLineItemRequestDto> ExtractLineItems(IReadOnlyList<string> lines)
    {
        var items = new List<InvoiceLineItemRequestDto>();
        var lineNo = 1;

        var lineItemRegex = new Regex(
            @"^(?:(?<lineNo>\d+)\s+)?(?<desc>.+?)\s+(?<qty>\d+(?:[.,]\d+)?)\s+(?<unit>\d{1,3}(?:[,.]\d{3})+(?:[,.]\d{1,2})?|\d+(?:[,.]\d{1,2})?)(?:\s*(?:USD|EUR|VND|VNĐ|đ|₫))?\s+(?<amount>\d{1,3}(?:[,.]\d{3})+(?:[,.]\d{1,2})?|\d+(?:[,.]\d{1,2})?)(?:\s*(?:USD|EUR|VND|VNĐ|đ|₫))?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (var line in lines)
        {
            if (IsNoiseLine(line))
                continue;

            var match = lineItemRegex.Match(line);
            if (!match.Success)
                continue;

            var desc = match.Groups["desc"].Value.Trim();
            if (string.IsNullOrWhiteSpace(desc))
                continue;

            if (!TryParseAmount(match.Groups["qty"].Value, out var qty))
                continue;

            if (!TryParseAmount(match.Groups["unit"].Value, out var unitPrice))
                continue;

            var parsedLineNo = lineNo;
            if (int.TryParse(match.Groups["lineNo"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var explicitLineNo)
                && explicitLineNo > 0)
            {
                parsedLineNo = explicitLineNo;
            }

            items.Add(new InvoiceLineItemRequestDto
            {
                LineNo = parsedLineNo,
                Description = desc,
                Quantity = qty,
                UnitPrice = unitPrice,
                TaxRate = 0
            });

            lineNo = Math.Max(lineNo + 1, parsedLineNo + 1);
        }

        return items;
    }

    private static bool TryFindAmount(string? text, out decimal amount)
        => TryFindFirstAmount(text, out amount);

    private static bool TryFindAmount(string? text, AmountSelection selection, out decimal amount)
    {
        return selection == AmountSelection.Last
            ? TryFindLastAmount(text, out amount)
            : TryFindFirstAmount(text, out amount);
    }

    private static bool TryFindFirstAmount(string? text, out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var matches = AmountRegex.Matches(text);

        foreach (Match match in matches)
        {
            if (IsPercentageMatch(text, match))
                continue;

            var value = match.Groups["value"].Value;

            if (TryParseAmount(value, out amount))
                return true;
        }

        return false;
    }

    private static bool TryFindLastAmount(string? text, out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var matches = AmountRegex.Matches(text);

        for (var i = matches.Count - 1; i >= 0; i--)
        {
            if (IsPercentageMatch(text, matches[i]))
                continue;

            var value = matches[i].Groups["value"].Value;

            if (TryParseAmount(value, out amount))
                return true;
        }

        return false;
    }

    private static bool IsPercentageMatch(string text, Match match)
    {
        var nextIndex = match.Index + match.Length;
        while (nextIndex < text.Length && char.IsWhiteSpace(text[nextIndex]))
        {
            nextIndex++;
        }

        return nextIndex < text.Length && text[nextIndex] == '%';
    }

    private static decimal? InferTotalFromLargestAmount(string rawText)
    {
        var candidates = new List<decimal>();

        foreach (Match match in AmountRegex.Matches(rawText))
        {
            if (IsLikelyDateOrIdentifierAmount(rawText, match))
                continue;

            var line = GetContainingLine(rawText, match.Index);
            if (IsIdentifierOrMetadataLine(line) || IsPercentageMatch(line, MatchInLine(match, rawText, line)))
                continue;

            if (!TryParseAmount(match.Groups["value"].Value, out var amount))
                continue;

            if (amount <= 0)
                continue;

            candidates.Add(amount);
        }

        return candidates.Count == 0 ? null : candidates.Max();
    }

    private static Match MatchInLine(Match match, string rawText, string line)
    {
        var lineStart = rawText.LastIndexOf('\n', Math.Max(0, match.Index));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        var lineMatchIndex = Math.Max(0, match.Index - lineStart);

        return AmountRegex.Match(line, lineMatchIndex);
    }

    private static string GetContainingLine(string text, int index)
    {
        var start = text.LastIndexOf('\n', Math.Max(0, index));
        start = start < 0 ? 0 : start + 1;

        var end = text.IndexOf('\n', index);
        if (end < 0)
            end = text.Length;

        return text[start..end];
    }

    private static bool IsLikelyDateOrIdentifierAmount(string rawText, Match match)
    {
        var value = match.Groups["value"].Value;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue)
            && intValue is >= 1900 and <= 2100)
        {
            return true;
        }

        var start = Math.Max(0, match.Index - 8);
        var length = Math.Min(rawText.Length - start, match.Length + 16);
        var context = rawText.Substring(start, length);

        if (Regex.IsMatch(context, @"\d{1,4}[\/\.\-]\d{1,2}[\/\.\-]\d{1,4}"))
            return true;

        var digitsOnly = Regex.Replace(value, @"\D", string.Empty);
        if (digitsOnly.Length >= 7 && !value.Contains('.') && !value.Contains(','))
            return true;

        return false;
    }

    private static bool TryParseAmount(string? value, out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value
            .Trim()
            .Replace("₫", string.Empty)
            .Replace("$", string.Empty)
            .Replace("€", string.Empty)
            .Replace(" ", string.Empty);

        var hasComma = normalized.Contains(',');
        var hasDot = normalized.Contains('.');

        if (hasComma && hasDot)
        {
            if (normalized.LastIndexOf(',') > normalized.LastIndexOf('.'))
            {
                normalized = normalized.Replace(".", string.Empty).Replace(",", ".");
            }
            else
            {
                normalized = normalized.Replace(",", string.Empty);
            }
        }
        else if (hasComma && !hasDot)
        {
            normalized = normalized.Replace(",", string.Empty);
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out amount);
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var formats = new[]
        {
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "yyyy-MM-dd",
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

    private static string DetectCurrency(string text)
    {
        var upper = text.ToUpperInvariant();

        if (upper.Contains("USD") || text.Contains('$'))
            return "USD";

        if (upper.Contains("EUR") || text.Contains('€'))
            return "EUR";

        if (upper.Contains("VND") || upper.Contains("VNĐ") || text.Contains('₫'))
            return "VND";

        return "VND";
    }

    private static bool IsNoiseLine(string line)
    {
        var lower = line.ToLowerInvariant().Trim();

        if (string.IsNullOrWhiteSpace(lower))
            return true;

        var exactNoiseWords = new[]
        {
            "invoice",
            "bill to",
            "ship to",
            "description quantity unit price amount",
            "description quantity unit price (vnd) amount (vnd)"
        };

        if (exactNoiseWords.Any(x => lower == x))
            return true;

        var startsWithNoiseWords = new[]
        {
            "invoice number",
            "invoice date",
            "due date",
            "subtotal",
            "tax",
            "vat",
            "total",
            "grand total",
            "amount due",
            "payment",
            "currency",
            "số hóa đơn",
            "ngày hóa đơn",
            "ngày lập",
            "hạn thanh toán",
            "thuế",
            "tổng tiền",
            "tổng cộng"
        };

        return startsWithNoiseWords.Any(x => lower.StartsWith(x));
    }

    private static bool LineStartsWithAnyLabel(string line, params string[] labels)
    {
        var normalizedLine = NormalizeLabel(line);

        return labels.Any(label =>
        {
            var normalizedLabel = NormalizeLabel(label);
            return normalizedLine.StartsWith(normalizedLabel, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool LineMatchesAnyLabel(
        string line,
        bool allowContainedLabel,
        params string[] labels)
    {
        if (LineStartsWithAnyLabel(line, labels))
            return true;

        if (!allowContainedLabel)
            return false;

        var normalizedLine = NormalizeLabel(line);

        return labels.Any(label =>
        {
            var normalizedLabel = NormalizeLabel(label);
            if (string.IsNullOrWhiteSpace(normalizedLabel))
                return false;

            return Regex.IsMatch(
                normalizedLine,
                $@"(^| ){Regex.Escape(normalizedLabel)}( |$)",
                RegexOptions.IgnoreCase);
        });
    }

    private static bool IsCurrencyOnlyLine(string line)
    {
        var normalized = line.Trim();

        return Regex.IsMatch(
            normalized,
            @"^(USD|EUR|VND|VNĐ|đ|\$|€|₫)$",
            RegexOptions.IgnoreCase);
    }

    private static bool IsIdentifierOrMetadataLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var normalizedLine = NormalizeLabel(line);

        if (MetadataLabels.Any(label =>
                LabelStartsWithWholePhrase(normalizedLine, NormalizeLabel(label))))
        {
            return true;
        }

        if (Regex.IsMatch(line, @"\b[A-Z]{2,10}-\d{4}-\d{2,6}\b", RegexOptions.IgnoreCase))
            return true;

        if (Regex.IsMatch(line, @"\d{1,4}[\/\.\-]\d{1,2}[\/\.\-]\d{2,4}"))
            return true;

        if (Regex.IsMatch(line, @"\b\d{2,4}[-.\s]\d{3,5}[-.\s]\d{3,5}\b"))
            return true;

        return false;
    }

    private static bool LabelStartsWithWholePhrase(string normalizedLine, string normalizedLabel)
    {
        if (string.IsNullOrWhiteSpace(normalizedLabel))
            return false;

        return normalizedLine == normalizedLabel
            || normalizedLine.StartsWith(normalizedLabel + " ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLikelyLabelOnlyLine(string line)
    {
        if (TryFindAmount(line, out _))
            return false;

        if (!line.Any(char.IsLetter))
            return false;

        var normalizedLine = NormalizeLabel(line);
        if (string.IsNullOrWhiteSpace(normalizedLine))
            return true;

        return KnownAmountLabels.Any(label =>
        {
            var normalizedLabel = NormalizeLabel(label);
            return normalizedLine == normalizedLabel
                || normalizedLine.StartsWith(normalizedLabel + " ", StringComparison.OrdinalIgnoreCase)
                || normalizedLine.EndsWith(" " + normalizedLabel, StringComparison.OrdinalIgnoreCase)
                || normalizedLine.Contains(" " + normalizedLabel + " ", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string NormalizeLabel(string value)
    {
        var beforeColon = value.Split(':')[0];

        beforeColon = Regex.Replace(beforeColon, @"\([^)]*\)", string.Empty);
        beforeColon = Regex.Replace(beforeColon, @"[^a-zA-ZÀ-ỹ0-9 ]", " ");
        beforeColon = Regex.Replace(beforeColon, @"\s+", " ");

        return beforeColon.Trim().ToLowerInvariant();
    }

    private static string? GetValueAfterColon(string line)
    {
        var colonIndex = line.IndexOf(':');

        if (colonIndex < 0 || colonIndex >= line.Length - 1)
            return null;

        return line[(colonIndex + 1)..].Trim();
    }

    private static string? TryRegexGroup(string input, IEnumerable<string> patterns, string groupName)
    {
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var value = match.Groups[groupName].Value.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }
}
