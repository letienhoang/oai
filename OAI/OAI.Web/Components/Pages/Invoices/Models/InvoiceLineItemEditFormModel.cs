namespace OAI.Web.Components.Pages.Invoices.Models;

public sealed class InvoiceLineItemEditFormModel
{
    public Guid? InvoiceLineItemId { get; set; }

    public int LineNo { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TaxRate { get; set; }
}
