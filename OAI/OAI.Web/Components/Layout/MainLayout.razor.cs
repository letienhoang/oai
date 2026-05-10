namespace OAI.Web.Components.Layout;

public partial class MainLayout
{
    private OAI.Web.Options.ApplicationInfoOptions ApplicationInfo => ApplicationInfoOptions.Value;

    private bool IsSidebarCollapsed { get; set; }

    private SidebarSize CurrentSidebarSize { get; set; } = SidebarSize.Normal;

    private string AppShellClass => IsSidebarCollapsed
        ? "app-shell app-shell-collapsed d-flex min-vh-100 bg-light"
        : "app-shell d-flex min-vh-100 bg-light";

    private string SidebarStyle => CurrentSidebarSize switch
    {
        SidebarSize.Small => "--sidebar-width: 220px;",
        SidebarSize.Normal => "--sidebar-width: 280px;",
        SidebarSize.Large => "--sidebar-width: 340px;",
        _ => "--sidebar-width: 280px;"
    };

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ChangeSidebarSize(SidebarSize size)
    {
        CurrentSidebarSize = size;
    }

    private enum SidebarSize
    {
        Small,
        Normal,
        Large
    }
}
