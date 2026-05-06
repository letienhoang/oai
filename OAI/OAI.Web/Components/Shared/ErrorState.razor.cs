using Microsoft.AspNetCore.Components;

namespace OAI.Web.Components.Shared;

public partial class ErrorState : ComponentBase
{
    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? Actions { get; set; }

    [Parameter]
    public bool InCard { get; set; } = true;

    [Parameter]
    public string CssClass { get; set; } = string.Empty;
    
    private RenderFragment AlertBody => builder =>
    {
        if (!string.IsNullOrWhiteSpace(Title))
        {
            builder.OpenElement(0, "h2");
            builder.AddAttribute(1, "class", "h5 fw-bold");
            builder.AddContent(2, Title);
            builder.CloseElement();
        }
        
        builder.OpenElement(3, "div");
        builder.AddContent(4, Message);
        builder.CloseElement();
        
        if (Actions is not null)
        {
            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "class", "mt-3");
            builder.AddContent(7, Actions);
            builder.CloseElement();
        }
    };
}