using System.ComponentModel.DataAnnotations;

namespace OAI.Web.Components.Pages.Invoices.Models;

public sealed class InvoiceLineItemEditFormModel
{
    public Guid? InvoiceLineItemId { get; set; }
    
    public int LineNo { get; set; }

    [Required(ErrorMessage = "Mô tả là bắt buộc.")]
    public string Description { get; set; } = string.Empty;

    [Range(0.0001, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm.")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100, ErrorMessage = "Thuế suất phải nằm trong khoảng 0 - 100.")]
    public decimal TaxRate { get; set; }
}