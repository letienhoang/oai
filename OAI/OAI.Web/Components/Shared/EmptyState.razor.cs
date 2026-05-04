using Microsoft.AspNetCore.Components;

namespace OAI.Web.Components.Shared;

public partial class EmptyState : ComponentBase
{
    [Parameter]
    public string Icon { get; set; } = "ℹ️";

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Message { get; set; }

    [Parameter]
    public RenderFragment? Actions { get; set; }

    [Parameter]
    public bool InCard { get; set; } = true;

    [Parameter]
    public string CssClass { get; set; } = string.Empty;
    
    private RenderFragment StateContent => builder =>
    {
        if (!string.IsNullOrWhiteSpace(Icon))
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "fs-1 mb-3");
            builder.AddContent(2, Icon);
            builder.CloseElement();
        }

        if (!string.IsNullOrWhiteSpace(Title))
        {
            builder.OpenElement(3, "h2");
            builder.AddAttribute(4, "class", "h5 fw-bold");
            builder.AddContent(5, Title);
            builder.CloseElement();
        }

        if (!string.IsNullOrWhiteSpace(Message))
        {
            builder.OpenElement(6, "p");
            builder.AddAttribute(7, "class", $"text-secondary {(Actions is null ? "mb-0" : "mb-3")}");
            builder.AddContent(8, Message);
            builder.CloseElement();
        }

        if (Actions is not null)
        {
            builder.OpenElement(9, "div");
            builder.AddContent(10, Actions);
            builder.CloseElement();
        }
    };
}
