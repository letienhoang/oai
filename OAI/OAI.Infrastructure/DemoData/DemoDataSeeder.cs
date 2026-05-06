using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Domain.ValueObjects;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.DemoData;

public sealed class DemoDataSeeder
{
    private static readonly string[] DemoVendorNames =
    [
        "Demo ACME Software Company",
        "Demo Contoso Supplies",
        "Demo Fabrikam Consulting",
        "Demo Northwind Services"
    ];

    private readonly OaiDbContext _dbContext;
    private readonly DemoDataSeedOptions _options;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        OaiDbContext dbContext,
        IOptions<DemoDataSeedOptions> options,
        ILogger<DemoDataSeeder> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DemoDataSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new DemoDataSeedResult
            {
                Skipped = true,
                Message = "Demo data seeding is disabled."
            };
        }

        var prefix = GetInvoiceNumberPrefix();

        var hasDemoInvoices = await _dbContext.Invoices
            .AnyAsync(x => x.InvoiceNumber.StartsWith(prefix), cancellationToken);

        if (hasDemoInvoices && !_options.ResetBeforeSeed)
        {
            return new DemoDataSeedResult
            {
                Skipped = true,
                Message = $"Demo invoices with prefix '{prefix}' already exist."
            };
        }

        if (_options.ResetBeforeSeed)
        {
            await ResetDemoDataAsync(prefix, cancellationToken);
        }

        var vendors = CreateDemoVendors();
        _dbContext.Vendors.AddRange(vendors);

        var invoices = CreateDemoInvoices(vendors, prefix);
        _dbContext.Invoices.AddRange(invoices);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var validationIssuesCreated = invoices.Sum(x => x.ValidationIssues.Count);
        var extractionResultsCreated = invoices.Sum(x => x.ExtractionResults.Count);

        _logger.LogInformation(
            "Seeded demo data: {VendorCount} vendors, {InvoiceCount} invoices, {IssueCount} validation issues, {ExtractionCount} extraction results.",
            vendors.Count,
            invoices.Count,
            validationIssuesCreated,
            extractionResultsCreated);

        return new DemoDataSeedResult
        {
            VendorsCreated = vendors.Count,
            InvoicesCreated = invoices.Count,
            ValidationIssuesCreated = validationIssuesCreated,
            ExtractionResultsCreated = extractionResultsCreated,
            Message = "Demo data seeded successfully."
        };
    }

    public async Task<DemoDataResetResult> ResetAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new DemoDataResetResult
            {
                Skipped = true,
                Message = "Demo data reset is disabled."
            };
        }

        var prefix = GetInvoiceNumberPrefix();

        return await ResetDemoDataAsync(prefix, cancellationToken);
    }

    private string GetInvoiceNumberPrefix()
    {
        return string.IsNullOrWhiteSpace(_options.InvoiceNumberPrefix)
            ? "DEMO"
            : _options.InvoiceNumberPrefix.Trim();
    }

    private async Task<DemoDataResetResult> ResetDemoDataAsync(string prefix, CancellationToken cancellationToken)
    {
        var demoInvoices = await _dbContext.Invoices
            .Where(x => x.InvoiceNumber.StartsWith(prefix))
            .Include(x => x.LineItems)
            .Include(x => x.ValidationIssues)
            .Include(x => x.ExtractionResults)
            .ToListAsync(cancellationToken);

        var invoicesDeleted = demoInvoices.Count;
        var lineItemsDeleted = demoInvoices.Sum(x => x.LineItems.Count);
        var validationIssuesDeleted = demoInvoices.Sum(x => x.ValidationIssues.Count);
        var extractionResultsDeleted = demoInvoices.Sum(x => x.ExtractionResults.Count);

        if (demoInvoices.Count > 0)
        {
            _dbContext.Invoices.RemoveRange(demoInvoices);
        }

        var demoVendors = await _dbContext.Vendors
            .Where(x => DemoVendorNames.Contains(x.Name))
            .ToListAsync(cancellationToken);

        var vendorsDeleted = 0;
        var keptDemoVendors = false;

        if (demoVendors.Count > 0)
        {
            var demoVendorIds = demoVendors.Select(x => x.Id).ToList();
            var hasNonDemoInvoicesForDemoVendors = await _dbContext.Invoices
                .AnyAsync(
                    x => demoVendorIds.Contains(x.VendorId)
                        && !x.InvoiceNumber.StartsWith(prefix),
                    cancellationToken);

            if (hasNonDemoInvoicesForDemoVendors)
            {
                keptDemoVendors = true;
                _logger.LogWarning(
                    "Demo vendors were not deleted because at least one non-demo invoice references a demo vendor.");
            }
            else
            {
                vendorsDeleted = demoVendors.Count;
                _dbContext.Vendors.RemoveRange(demoVendors);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var message = keptDemoVendors
            ? "Demo data reset completed. Demo vendors were kept because non-demo invoices reference them."
            : "Demo data reset completed successfully.";

        _logger.LogInformation(
            "Reset demo data: {VendorCount} vendors, {InvoiceCount} invoices, {LineItemCount} line items, {IssueCount} validation issues, {ExtractionCount} extraction results.",
            vendorsDeleted,
            invoicesDeleted,
            lineItemsDeleted,
            validationIssuesDeleted,
            extractionResultsDeleted);

        return new DemoDataResetResult
        {
            VendorsDeleted = vendorsDeleted,
            InvoicesDeleted = invoicesDeleted,
            LineItemsDeleted = lineItemsDeleted,
            ValidationIssuesDeleted = validationIssuesDeleted,
            ExtractionResultsDeleted = extractionResultsDeleted,
            Message = message
        };
    }

    private static List<Vendor> CreateDemoVendors()
    {
        return
        [
            new Vendor(
                "Demo ACME Software Company",
                taxNumber: "DEMO-TAX-ACME",
                address: "12 Nguyen Hue Street, District 1, Ho Chi Minh City",
                email: "billing@demo-acme.local"),
            new Vendor(
                "Demo Contoso Supplies",
                taxNumber: "DEMO-TAX-CONTOSO",
                address: "88 Tran Hung Dao Street, District 5, Ho Chi Minh City",
                email: "invoice@demo-contoso.local"),
            new Vendor(
                "Demo Fabrikam Consulting",
                taxNumber: "DEMO-TAX-FABRIKAM",
                address: "25 Ly Thuong Kiet Street, Hoan Kiem, Hanoi",
                email: "finance@demo-fabrikam.local"),
            new Vendor(
                "Demo Northwind Services",
                taxNumber: "DEMO-TAX-NORTHWIND",
                address: "9 Bach Dang Street, Hai Chau, Da Nang",
                email: "accounts@demo-northwind.local")
        ];
    }

    private static List<Invoice> CreateDemoInvoices(IReadOnlyList<Vendor> vendors, string prefix)
    {
        var invoice1 = CreateInvoice(
            vendors[0],
            $"{prefix}-2026-001",
            new DateOnly(2026, 1, 12),
            new DateOnly(2026, 2, 11),
            "acme-demo-2026-001.pdf",
            [
                new DemoLineItem(1, "Cloud OCR subscription", 1m, 3500000m, 10m),
                new DemoLineItem(2, "Document processing add-on", 2m, 850000m, 10m)
            ],
            0.92m);
        invoice1.AddValidationIssue(new ValidationIssue(
            invoice1.Id,
            fieldName: nameof(Invoice.DeclaredTaxAmount),
            ruleCode: "DEMO-TAX-MISMATCH",
            message: "Extracted tax amount requires accountant review against the invoice image.",
            severity: ValidationSeverity.Warning));

        var invoice2 = CreateInvoice(
            vendors[1],
            $"{prefix}-2026-002",
            new DateOnly(2026, 1, 18),
            new DateOnly(2026, 2, 17),
            "contoso-demo-2026-002.pdf",
            [
                new DemoLineItem(1, "Office printer paper A4", 12m, 95000m, 8m),
                new DemoLineItem(2, "Toner cartridge set", 3m, 720000m, 8m)
            ],
            0.96m);
        invoice2.Approve();

        var invoice3 = CreateInvoice(
            vendors[2],
            $"{prefix}-2026-003",
            new DateOnly(2026, 2, 2),
            new DateOnly(2026, 3, 4),
            "fabrikam-demo-2026-003.pdf",
            [
                new DemoLineItem(1, "ERP integration consulting", 16m, 1250000m, 10m),
                new DemoLineItem(2, "Data migration workshop", 1m, 5000000m, 10m)
            ],
            0.71m);
        invoice3.AddValidationIssue(new ValidationIssue(
            invoice3.Id,
            fieldName: nameof(Invoice.InvoiceNumber),
            ruleCode: "DEMO-LOW-CONFIDENCE",
            message: "Invoice number confidence is below the configured review threshold.",
            severity: ValidationSeverity.Error));
        invoice3.AddValidationIssue(new ValidationIssue(
            invoice3.Id,
            fieldName: nameof(Invoice.DueDate),
            ruleCode: "DEMO-DATE-CONFLICT",
            message: "Due date appears inconsistent between header and payment terms.",
            severity: ValidationSeverity.Warning));
        invoice3.Reject();

        var invoice4 = CreateInvoice(
            vendors[3],
            $"{prefix}-2026-004",
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 3, 12),
            "northwind-demo-2026-004.pdf",
            [
                new DemoLineItem(1, "Managed server monitoring", 1m, 4200000m, 10m),
                new DemoLineItem(2, "Monthly support retainer", 1m, 2800000m, 10m)
            ],
            0.88m);

        return [invoice1, invoice2, invoice3, invoice4];
    }

    private static Invoice CreateInvoice(
        Vendor vendor,
        string invoiceNumber,
        DateOnly issueDate,
        DateOnly dueDate,
        string sourceFileName,
        IReadOnlyList<DemoLineItem> lineItems,
        decimal confidenceScore)
    {
        const string currency = "VND";
        var subtotal = lineItems.Sum(x => x.Quantity * x.UnitPrice);
        var taxAmount = lineItems.Sum(x => x.Quantity * x.UnitPrice * x.TaxRate / 100m);
        var totalAmount = subtotal + taxAmount;

        var invoice = new Invoice(
            vendor.Id,
            invoiceNumber,
            issueDate,
            currency,
            new Money(subtotal, currency),
            new Money(taxAmount, currency),
            new Money(totalAmount, currency),
            dueDate,
            sourceFileName,
            $"demo/{sourceFileName}");

        foreach (var item in lineItems)
        {
            invoice.AddLineItem(new InvoiceLineItem(
                invoice.Id,
                item.LineNo,
                item.Description,
                item.Quantity,
                new Money(item.UnitPrice, currency),
                item.TaxRate));
        }

        invoice.AddExtractionResult(new InvoiceExtractionResult(
            invoice.Id,
            engineName: "Demo Hybrid Extractor",
            confidenceScore,
            attemptNo: 1,
            isSuccessful: true,
            rawText: BuildRawText(vendor, invoiceNumber, issueDate, dueDate, lineItems, totalAmount),
            structuredJson: BuildStructuredJson(vendor, invoiceNumber, issueDate, dueDate, currency, subtotal, taxAmount, totalAmount, lineItems)));

        return invoice;
    }

    private static string BuildRawText(
        Vendor vendor,
        string invoiceNumber,
        DateOnly issueDate,
        DateOnly dueDate,
        IReadOnlyList<DemoLineItem> lineItems,
        decimal totalAmount)
    {
        var itemText = string.Join(
            Environment.NewLine,
            lineItems.Select(x => $"{x.LineNo}. {x.Description} | Qty {x.Quantity} | Unit {x.UnitPrice:N0} | VAT {x.TaxRate:N0}%"));

        return $"""
            Vendor: {vendor.Name}
            Tax number: {vendor.TaxNumber}
            Invoice number: {invoiceNumber}
            Issue date: {issueDate:yyyy-MM-dd}
            Due date: {dueDate:yyyy-MM-dd}
            Items:
            {itemText}
            Grand total: {totalAmount:N0} VND
            """;
    }

    private static string BuildStructuredJson(
        Vendor vendor,
        string invoiceNumber,
        DateOnly issueDate,
        DateOnly dueDate,
        string currency,
        decimal subtotal,
        decimal taxAmount,
        decimal totalAmount,
        IReadOnlyList<DemoLineItem> lineItems)
    {
        var payload = new
        {
            vendor = new
            {
                name = vendor.Name,
                taxNumber = vendor.TaxNumber,
                address = vendor.Address,
                email = vendor.Email
            },
            invoiceNumber,
            issueDate = issueDate.ToString("yyyy-MM-dd"),
            dueDate = dueDate.ToString("yyyy-MM-dd"),
            currency,
            declaredSubtotal = subtotal,
            declaredTaxAmount = taxAmount,
            declaredTotalAmount = totalAmount,
            lineItems = lineItems.Select(x => new
            {
                x.LineNo,
                x.Description,
                x.Quantity,
                x.UnitPrice,
                x.TaxRate
            })
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private sealed record DemoLineItem(
        int LineNo,
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        decimal TaxRate);
}
