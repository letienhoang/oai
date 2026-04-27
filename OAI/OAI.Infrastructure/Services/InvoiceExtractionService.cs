using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;

namespace OAI.Infrastructure.Services;

public sealed class InvoiceExtractionService : IInvoiceExtractionService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IOcrService _ocrService;
    private readonly ILogger<InvoiceExtractionService> _logger;

    public InvoiceExtractionService(
        IFileStorageService fileStorageService,
        IOcrService ocrService,
        ILogger<InvoiceExtractionService> logger)
    {
        _fileStorageService = fileStorageService;
        _ocrService = ocrService;
        _logger = logger;
    }

    public async Task<ExtractedInvoiceDto?> ExtractFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        _logger.LogInformation("Start extracting invoice from file path {FilePath}", filePath);
        var stream = await _fileStorageService.OpenReadAsync(filePath, cancellationToken);
        if (stream is null)
        {
            _logger.LogWarning("Cannot extract invoice because file was not found at {FilePath}", filePath);
            return null;
        }

        using (stream)
        {
            var fileName = Path.GetFileName(filePath);

            var ocrResult = await _ocrService.ExtractTextAsync(stream, fileName, cancellationToken);
            if (!ocrResult.IsSuccess || string.IsNullOrWhiteSpace(ocrResult.Text))
            {
                _logger.LogWarning(
                    "OCR failed or returned empty text for file {FileName}. Error: {ErrorMessage}",
                    fileName,
                    ocrResult.ErrorMessage);

                return null;
            }

            var extracted = ExtractFromTextInternal(
                ocrResult.Text,
                fileName,
                ocrResult.Confidence,
                engineName: "Tesseract");

            if (extracted is null)
            {
                _logger.LogWarning("Cannot parse invoice data from OCR text for file {FileName}", fileName);
                return null;
            }

            _logger.LogInformation(
                "Invoice extraction succeeded for file {FileName}. InvoiceNumber: {InvoiceNumber}, Confidence: {Confidence}",
                fileName,
                extracted.InvoiceNumber,
                extracted.ConfidenceScore);

            return extracted;
        }
    }

    public Task<ExtractedInvoiceDto?> ExtractFromTextAsync(
        string rawText,
        CancellationToken cancellationToken = default)
    {
        var result = ExtractFromTextInternal(rawText, "raw-text", 1.0f, "RawText");
        return Task.FromResult(result);
    }

    private static ExtractedInvoiceDto? ExtractFromTextInternal(
        string rawText,
        string sourceFileName,
        float confidence,
        string engineName)
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
            ExtractDateByLabel(lines, "invoice date", "issue date")
            ?? ExtractDateByIndex(rawText, 0)
            ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var dueDate =
            ExtractDateByLabel(lines, "due date", "payment due", "pay by")
            ?? ExtractDateByIndex(rawText, 1);
        if (dueDate == issueDate)
        {
            dueDate = ExtractDateByIndex(rawText, 1) ?? dueDate;
        }

        var subtotal =
            ExtractAmountByLabel(lines, "subtotal", "net amount", "amount before tax", "before tax");

        var taxAmount =
            ExtractAmountByLabel(lines, "vat", "tax", "tax amount", "vat amount") ?? 0m;

        var totalAmount =
            ExtractAmountByLabel(lines, "total", "grand total", "total amount", "amount due");

        if (totalAmount is null && subtotal is not null)
        {
            totalAmount = subtotal.Value + taxAmount;
        }

        if (totalAmount is null)
            return null;
        
        var totalValue = totalAmount.Value;

        var subtotalValue = subtotal ?? Math.Max(totalValue - taxAmount, 0m);

        var lineItems = ExtractLineItems(lines);

        var inferredTaxRate = subtotalValue > 0 && taxAmount > 0
            ? Math.Round(taxAmount / subtotalValue * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

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

        return new ExtractedInvoiceDto
        {
            VendorName = vendorName,
            InvoiceNumber = invoiceNumber,
            IssueDate = issueDate,
            DueDate = dueDate,
            Currency = currency,
            DeclaredSubtotal = subtotalValue,
            DeclaredTaxAmount = taxAmount,
            DeclaredTotalAmount = totalAmount.Value,
            ConfidenceScore = Math.Clamp((decimal)confidence, 0m, 1m),
            EngineName = engineName,
            RawText = rawText,
            LineItems = lineItems
        };
    }

    private static List<string> NormalizeLines(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static string? ExtractInvoiceNumber(IReadOnlyList<string> lines, string rawText)
    {
        var normalizedText = string.Join("\n", lines);

        // Ưu tiên bắt mã invoice rõ ràng kiểu INV-2026-001
        var invoiceCodePatterns = new[]
        {
            @"(?i)\b(?<value>INV[-\/]?\d{4}[-\/]?\d{3,})\b",
            @"(?i)\b(?<value>INV[-\/]?[A-Z0-9]+[-\/]?[A-Z0-9]+)\b",
            @"(?i)\b(?<value>[A-Z]{2,}[-\/]\d{4}[-\/]\d{2,})\b"
        };

        var invoiceCode = TryRegexGroup(normalizedText, invoiceCodePatterns, "value");
        if (IsLikelyInvoiceNumber(invoiceCode))
            return invoiceCode;

        invoiceCode = TryRegexGroup(rawText, invoiceCodePatterns, "value");
        if (IsLikelyInvoiceNumber(invoiceCode))
            return invoiceCode;

        // Then try tracking it by the Invoice Number label
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (!LineStartsWithAnyLabel(line, "invoice number", "invoice no", "inv no"))
                continue;

            var valuePart = GetValueAfterColon(line);

            if (IsLikelyInvoiceNumber(valuePart))
                return valuePart;

            // OCR can separate the label and value into different lines
            for (var j = i + 1; j < Math.Min(i + 8, lines.Count); j++)
            {
                var candidate = lines[j].Trim();

                if (LineStartsWithAnyLabel(candidate, "invoice date", "due date", "date"))
                    continue;

                var code = TryRegexGroup(candidate, invoiceCodePatterns, "value");

                if (IsLikelyInvoiceNumber(code))
                    return code;

                if (IsLikelyInvoiceNumber(candidate))
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
            "subtotal"
        };

        if (invalidValues.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (normalized.Length < 4)
            return false;

        return normalized.Any(char.IsDigit);
    }

    private static string GenerateUnreadInvoiceNumber()
    {
        return $"UNREAD-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    }

    private static string? ExtractVendorName(IReadOnlyList<string> lines)
    {
        var candidates = lines
            .Take(12)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !IsNoiseLine(x))
            .Where(x => IsLikelyVendorLine(x))
            .ToList();

        var companyCandidate = candidates
            .Where(x => x.Contains("company", StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("co.,", StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("ltd", StringComparison.OrdinalIgnoreCase) ||
                        x.Contains("corp", StringComparison.OrdinalIgnoreCase))
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
                if (LineStartsWithAnyLabel(lines[j], "invoice date", "due date", "date", "subtotal", "total", "vat"))
                    continue;

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

    private static decimal? ExtractAmountByLabel(IReadOnlyList<string> lines, params string[] labels)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (!LineStartsWithAnyLabel(line, labels))
                continue;

            var valuePart = GetValueAfterColon(line);

            if (TryFindAmount(valuePart, out var amountFromSameLine))
                return amountFromSameLine;

            for (var j = i + 1; j < Math.Min(i + 4, lines.Count); j++)
            {
                if (LineStartsWithAnyLabel(lines[j], "subtotal", "vat", "tax", "total", "grand total"))
                    continue;

                if (TryFindAmount(lines[j], out var amountFromNextLine))
                    return amountFromNextLine;
            }
        }

        return null;
    }

    private static bool TryFindAmount(string? text, out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var matches = Regex.Matches(text, @"(?<value>\d{1,3}(?:[,.]\d{3})*(?:[,.]\d{1,2})?|\d+)");

        foreach (Match match in matches)
        {
            var value = match.Groups["value"].Value;

            if (TryParseAmount(value, out amount))
                return true;
        }

        return false;
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

    private static string NormalizeLabel(string value)
    {
        var beforeColon = value.Split(':')[0];

        beforeColon = Regex.Replace(beforeColon, @"\([^)]*\)", string.Empty);
        beforeColon = Regex.Replace(beforeColon, @"[^a-zA-Z0-9 ]", " ");
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

    private static List<InvoiceLineItemRequestDto> ExtractLineItems(IReadOnlyList<string> lines)
    {
        var items = new List<InvoiceLineItemRequestDto>();
        var lineNo = 1;

        var lineItemRegex = new Regex(
            @"^(?<desc>.+?)\s+(?<qty>\d+(?:[.,]\d+)?)\s+(?<unit>[\d.,]+)\s+(?<amount>[\d.,]+)$",
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

            items.Add(new InvoiceLineItemRequestDto
            {
                LineNo = lineNo++,
                Description = desc,
                Quantity = qty,
                UnitPrice = unitPrice,
                TaxRate = 0
            });
        }

        return items;
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
            "description quantity unit price amount"
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
            "currency"
        };

        return startsWithNoiseWords.Any(x => lower.StartsWith(x));
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

        if (DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

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

        // Process the format as 1,234.56 or 1.234,56
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