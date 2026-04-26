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
                ocrResult.Confidence);

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
        var result = ExtractFromTextInternal(rawText, "raw-text", 1.0f);
        return Task.FromResult(result);
    }

    private static ExtractedInvoiceDto? ExtractFromTextInternal(
        string rawText,
        string sourceFileName,
        float confidence)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return null;

        var lines = NormalizeLines(rawText);
        if (lines.Count == 0)
            return null;

        var invoiceNumber = ExtractInvoiceNumber(lines, rawText);
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return null;

        var currency = DetectCurrency(rawText);
        var vendorName = ExtractVendorName(lines);

        if (string.IsNullOrWhiteSpace(vendorName))
            vendorName = lines.FirstOrDefault(x => !IsNoiseLine(x)) ?? "Unknown Vendor";

        var issueDate =
            ExtractDate(rawText, "invoice date", "issue date", "date")
            ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var dueDate =
            ExtractDate(rawText, "due date", "payment due", "pay by");

        var totalAmount =
            ExtractAmount(rawText, "grand total", "total amount", "amount due", "total");

        if (totalAmount is null)
            return null;

        var taxAmount =
            ExtractAmount(rawText, "tax amount", "vat amount", "vat", "tax") ?? 0m;

        var subtotal =
            ExtractAmount(rawText, "subtotal", "net amount", "amount before tax", "before tax")
            ?? Math.Max(totalAmount.Value - taxAmount, 0m);

        if (subtotal <= 0 && totalAmount.Value > 0)
            subtotal = totalAmount.Value;

        if (taxAmount <= 0 && totalAmount.Value > subtotal)
            taxAmount = totalAmount.Value - subtotal;

        var lineItems = ExtractLineItems(lines);

        if (lineItems.Count == 0)
        {
            lineItems.Add(new InvoiceLineItemRequestDto
            {
                LineNo = 1,
                Description = "Auto extracted item",
                Quantity = 1,
                UnitPrice = subtotal,
                TaxRate = 0
            });
        }

        return new ExtractedInvoiceDto
        {
            VendorName = vendorName,
            InvoiceNumber = invoiceNumber,
            IssueDate = issueDate,
            DueDate = dueDate,
            Currency = currency,
            DeclaredSubtotal = subtotal,
            DeclaredTaxAmount = taxAmount,
            DeclaredTotalAmount = totalAmount.Value,
            ConfidenceScore = Math.Clamp((decimal)confidence, 0m, 1m),
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
        var patterns = new[]
        {
            @"(?i)\binvoice\s*(no\.?|number|#)\s*[:\-]?\s*(?<value>[A-Z0-9\-\/]+)",
            @"(?i)\binv\s*(no\.?|number|#)\s*[:\-]?\s*(?<value>[A-Z0-9\-\/]+)",
            @"(?i)\bno\.?\s*[:\-]?\s*(?<value>[A-Z0-9\-\/]+)"
        };

        var result = TryRegexGroup(rawText, patterns, "value");
        if (!string.IsNullOrWhiteSpace(result))
            return result;

        foreach (var line in lines)
        {
            result = TryRegexGroup(line, patterns, "value");
            if (!string.IsNullOrWhiteSpace(result))
                return result;
        }

        return null;
    }

    private static string? ExtractVendorName(IReadOnlyList<string> lines)
    {
        foreach (var line in lines.Take(8))
        {
            if (IsNoiseLine(line))
                continue;

            if (line.Any(char.IsLetter))
                return line.Trim();
        }

        return null;
    }

    private static DateOnly? ExtractDate(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            var pattern = $@"(?i){Regex.Escape(keyword)}\s*[:\-]?\s*(?<value>\d{{1,4}}[\/\.\-]\d{{1,2}}[\/\.\-]\d{{2,4}})";
            var result = TryRegexGroup(text, new[] { pattern }, "value");
            if (TryParseDate(result, out var date))
                return date;
        }

        // Fallback: tìm bất kỳ ngày nào trong text
        var genericMatch = Regex.Match(
            text,
            @"(?<value>\d{1,4}[\/\.\-]\d{1,2}[\/\.\-]\d{2,4})",
            RegexOptions.IgnoreCase);

        if (genericMatch.Success && TryParseDate(genericMatch.Groups["value"].Value, out var genericDate))
            return genericDate;

        return null;
    }

    private static decimal? ExtractAmount(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            var pattern = $@"(?i){Regex.Escape(keyword)}\s*[:\-]?\s*(?<value>[\d.,]+)";
            var result = TryRegexGroup(text, new[] { pattern }, "value");
            if (TryParseAmount(result, out var amount))
                return amount;
        }

        return null;
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
        var lower = line.ToLowerInvariant();

        var noiseWords = new[]
        {
            "invoice",
            "bill to",
            "ship to",
            "date",
            "subtotal",
            "tax",
            "vat",
            "total",
            "grand total",
            "amount due",
            "payment",
            "due date",
            "currency"
        };

        return noiseWords.Any(x => lower.Contains(x));
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

        // Xử lý format kiểu 1,234.56 hoặc 1.234,56
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