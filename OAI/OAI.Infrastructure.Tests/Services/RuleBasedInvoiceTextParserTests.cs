using OAI.Infrastructure.Services;

namespace OAI.Infrastructure.Tests.Services;

public sealed class RuleBasedInvoiceTextParserTests
{
    private readonly RuleBasedInvoiceTextParser _parser = new();

    [Theory]
    [InlineData("Invoice Total: 1,234.56", 1234.56)]
    [InlineData("Total Due: 2,345.67", 2345.67)]
    [InlineData("Balance Due: 3,456.78", 3456.78)]
    public void ParseInternal_ParsesTotalFromExpandedTotalLabels(
        string totalLine,
        decimal expectedTotal)
    {
        var result = _parser.ParseInternal(
            BuildInvoiceText(totalLine),
            "invoice.pdf",
            1m,
            "Test");

        Assert.NotNull(result);
        Assert.Equal(expectedTotal, result.DeclaredTotalAmount);
    }

    [Fact]
    public void ParseInternal_ParsesTotalWhenLabelAndAmountAreOnSeparateLines()
    {
        var result = _parser.ParseInternal(
            BuildInvoiceText(
                """
                Total Due
                1,234.56
                """),
            "invoice.pdf",
            1m,
            "Test");

        Assert.NotNull(result);
        Assert.Equal(1234.56m, result.DeclaredTotalAmount);
    }

    [Fact]
    public void ParseInternal_ParsesTotalWhenCurrencyLineIsBetweenLabelAndAmount()
    {
        var result = _parser.ParseInternal(
            BuildInvoiceText(
                """
                Amount Payable
                USD
                1,234.56
                """),
            "invoice.pdf",
            1m,
            "Test");

        Assert.NotNull(result);
        Assert.Equal(1234.56m, result.DeclaredTotalAmount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void ParseInternal_PrefersLastAmountForTotalLine()
    {
        var result = _parser.ParseInternal(
            BuildInvoiceText("Subtotal 100.00 Tax 10.00 Total 110.00"),
            "invoice.pdf",
            1m,
            "Test");

        Assert.NotNull(result);
        Assert.Equal(110.00m, result.DeclaredTotalAmount);
    }

    [Fact]
    public void ParseInternal_ParsesPdfLikeDigitalServicesInvoiceText()
    {
        var text =
            """
            --- Page 1 ---
            DIGITAL SERVICES INVOICE
            Acme Digital Services Ltd
            Invoice Number
            INV-2026-001
            Invoice Date
            2026-05-01
            Payment Due Date
            2026-05-15
            Bill To
            Example Customer
            Description Quantity Unit Price Amount
            Platform subscription 1 100.00 100.00
            Support services 1 50.00 50.00
            Subtotal Tax Total
            150.00 15.00 165.00
            Balance Due
            USD
            165.00
            """;

        var result = _parser.ParseInternal(
            text,
            "pdf-sample-01-valid-digital-services.pdf",
            1m,
            "PdfEmbeddedText");

        Assert.NotNull(result);
        Assert.Equal("INV-2026-001", result.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 5, 1), result.IssueDate);
        Assert.Equal(new DateOnly(2026, 5, 15), result.DueDate);
        Assert.Equal(165.00m, result.DeclaredTotalAmount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void ParseInternal_KeepsOcrStyleInvoiceParsingWorking()
    {
        var result = _parser.ParseInternal(
            """
            Contoso Software Ltd
            Invoice No: INV-2026-101
            Invoice Date: 01/05/2026
            Due Date: 15/05/2026
            Description Quantity Unit Price Amount
            SaaS subscription 1 100.00 100.00
            VAT: 10.00
            Grand Total: 110.00
            """,
            "ocr-sample.png",
            0.85m,
            "Tesseract");

        Assert.NotNull(result);
        Assert.Equal("INV-2026-101", result.InvoiceNumber);
        Assert.Equal(110.00m, result.DeclaredTotalAmount);
        Assert.Equal(10.00m, result.DeclaredTaxAmount);
        Assert.Equal(100.00m, result.DeclaredSubtotal);
    }

    private static string BuildInvoiceText(string totalText)
        =>
            $"""
            Acme Digital Services Ltd
            Invoice Number: INV-2026-001
            Invoice Date: 2026-05-01
            Due Date: 2026-05-15
            {totalText}
            """;
}
