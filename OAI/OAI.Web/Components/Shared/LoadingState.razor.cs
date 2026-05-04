using Microsoft.AspNetCore.Components;

namespace OAI.Web.Components.Shared;

public partial class LoadingState : ComponentBase
{
    [Parameter]
    public string? Message { get; set; }

    [Parameter]
    public bool InCard { get; set; } = true;

    [Parameter]
    public string CssClass { get; set; } = string.Empty;
}
