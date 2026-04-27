using System.ComponentModel.DataAnnotations;

namespace OAI.Web.Components.Pages.Invoices.Models;

public sealed class InvoiceEditFormModel
{
    public Guid InvoiceId { get; set; }

    [Required(ErrorMessage = "VendorId là bắt buộc.")]
    public string VendorIdText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số hóa đơn là bắt buộc.")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày phát hành là bắt buộc.")]
    public DateOnly IssueDate { get; set; }

    public DateOnly? DueDate { get; set; }

    [Required(ErrorMessage = "Currency là bắt buộc.")]
    public string Currency { get; set; } = "VND";

    public decimal DeclaredSubtotal { get; set; }
    public decimal DeclaredTaxAmount { get; set; }
    public decimal DeclaredTotalAmount { get; set; }

    public List<InvoiceLineItemEditFormModel> LineItems { get; set; } = new();
}