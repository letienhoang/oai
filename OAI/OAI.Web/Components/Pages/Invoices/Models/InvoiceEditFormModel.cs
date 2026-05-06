namespace OAI.Web.Components.Pages.Invoices.Models;

public sealed class InvoiceEditFormModel
{
    public Guid InvoiceId { get; set; }

    public string VendorIdText { get; set; } = string.Empty;

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateOnly IssueDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string Currency { get; set; } = "VND";

    public decimal DeclaredSubtotal { get; set; }
    public decimal DeclaredTaxAmount { get; set; }
    public decimal DeclaredTotalAmount { get; set; }

    public List<InvoiceLineItemEditFormModel> LineItems { get; set; } = new();
}
