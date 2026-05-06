using Microsoft.AspNetCore.Components;

namespace OAI.Web.Components.Layout;

public partial class TopBar
{
    [Parameter]
    public EventCallback OnToggleSidebar { get; set; }
}