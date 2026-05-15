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

    [Fact]
    public void ParseInternal_ParsesLumenDigitalServicesPdfText()
    {
        var result = _parser.ParseInternal(
            LumenDigitalServicesText,
            "pdf-sample-01-valid-digital-services.pdf",
            1m,
            "PdfEmbeddedText");

        Assert.NotNull(result);
        Assert.Equal("LUMEN DIGITAL SERVICES CO, LTD", result.VendorName);
        Assert.Equal("LDS-2026-041", result.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 5, 6), result.IssueDate);
        Assert.Equal(new DateOnly(2026, 5, 13), result.DueDate);
        Assert.Equal("VND", result.Currency);
        Assert.Equal(2700000m, result.DeclaredSubtotal);
        Assert.Equal(270000m, result.DeclaredTaxAmount);
        Assert.Equal(2970000m, result.DeclaredTotalAmount);
        Assert.InRange(result.LineItems.Count, 1, 2);
        Assert.All(result.LineItems, item =>
        {
            Assert.InRange(item.TaxRate, 0m, 100m);
            Assert.NotEqual(11806.13m, item.TaxRate);
        });
        Assert.Collection(
            result.LineItems,
            item =>
            {
                Assert.Equal("Invoice data extraction configuration", item.Description);
                Assert.Equal(1m, item.Quantity);
                Assert.Equal(1800000m, item.UnitPrice);
                Assert.Equal(10m, item.TaxRate);
            },
            item =>
            {
                Assert.Equal("Monthly support package", item.Description);
                Assert.Equal(2m, item.Quantity);
                Assert.Equal(450000m, item.UnitPrice);
                Assert.Equal(10m, item.TaxRate);
            });
    }

    [Fact]
    public void ParseInternal_DoesNotParseTaxCodeAsTaxAmount()
    {
        var result = _parser.ParseInternal(
            """
            LUMEN DIGITAL SERVICES CO., LTD
            Tax code: 0318765432
            Invoice Number LDS-2026-041
            Subtotal 2,700,000 VND
            Total 2,970,000 VND
            """,
            "invoice.pdf",
            1m,
            "Test");

        Assert.NotNull(result);
        Assert.Equal(0m, result.DeclaredTaxAmount);
        Assert.All(result.LineItems, item => Assert.InRange(item.TaxRate, 0m, 100m));
    }

    [Fact]
    public void ParseInternal_ParsesVatLineWithPercentageAsMoneyAmount()
    {
        var result = _parser.ParseInternal(
            """
            LUMEN DIGITAL SERVICES CO., LTD
            Invoice Number LDS-2026-041
            Subtotal 2,700,000 VND
            VAT 10% 270,000 VND
            Total 2,970,000 VND
            """,
            "invoice.pdf",
            1m,
            "Test");

        Assert.NotNull(result);
        Assert.Equal(270000m, result.DeclaredTaxAmount);
        Assert.Equal(10m, result.LineItems.Single().TaxRate);
    }

    [Fact]
    public void ParseInternal_ParsesInvoiceNumberFromSameLineWithoutColon()
    {
        var result = _parser.ParseInternal(
            """
            LUMEN DIGITAL SERVICES CO., LTD
            Invoice Number LDS-2026-041 Invoice Date 06/05/2026
            Total 2,970,000 VND
            """,
            "invoice.pdf",
            1m,
            "Test");

        Assert.NotNull(result);
        Assert.Equal("LDS-2026-041", result.InvoiceNumber);
    }

    [Fact]
    public void ParseInternal_DoesNotAcceptCustomerDueDateLineAsInvoiceNumber()
    {
        var result = _parser.ParseInternal(
            """
            LUMEN DIGITAL SERVICES CO., LTD
            Invoice Number
            Customer Harbor Trading Joint Stock Company Due Date 13/05/2026
            Total 2,970,000 VND
            """,
            "invoice.pdf",
            1m,
            "Test");

        Assert.NotNull(result);
        Assert.StartsWith("UNREAD-", result.InvoiceNumber);
    }

    [Fact]
    public void ParseInternal_ResetsInferredTaxRateWhenItExceedsOneHundred()
    {
        var result = _parser.ParseInternal(
            """
            LUMEN DIGITAL SERVICES CO., LTD
            Invoice Number LDS-2026-041
            Subtotal 100.00
            VAT 150.00
            Total 200.00
            """,
            "invoice.pdf",
            1m,
            "Test");

        Assert.NotNull(result);
        Assert.Equal(150m, result.DeclaredTaxAmount);
        Assert.All(result.LineItems, item => Assert.Equal(0m, item.TaxRate));
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

    private const string LumenDigitalServicesText =
        """
        LUMEN DIGITAL SERVICES CO., LTD
        Tax code: 0318765432
        Address: 45 Vo Van Tan, Ward 6, District 3, Ho Chi Minh City
        Phone: 028-3812-4501 | Email: billing@lumendigital.vn
        COMMERCIAL INVOICE
        Invoice Number LDS-2026-041 Invoice Date 06/05/2026
        Customer Harbor Trading Joint Stock Company Due Date 13/05/2026
        Currency VND Payment Method Bank transfer
        Line Items
        No Description Qty Unit Price Amount
        1 Invoice data extraction configuration 1 1,800,000 VND 1,800,000 VND
        2 Monthly support package 2 450,000 VND 900,000 VND
        Subtotal 2,700,000 VND
        VAT 10% 270,000 VND
        Total 2,970,000 VND
        """;
}
