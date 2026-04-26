using Microsoft.AspNetCore.Components;

namespace OAI.Web.Components.Pages.Invoices;

public partial class InvoiceDetail : ComponentBase
{
    [Parameter]
    public Guid InvoiceId { get; set; }
}