using Microsoft.AspNetCore.Components;

namespace OAI.Web.Components.Shared;

public partial class Icon : ComponentBase
{
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public int Size { get; set; } = 16;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string CssClass { get; set; } = string.Empty;

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private bool IsMeaningful => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(AriaLabel);

    private string? AriaHidden => IsMeaningful ? null : "true";

    private string? Role => IsMeaningful ? "img" : null;

    private string? AccessibleLabel => string.IsNullOrWhiteSpace(AriaLabel) ? Title : AriaLabel;

    private IReadOnlyDictionary<string, object>? CapturedAttributes => AdditionalAttributes?
        .Where(attribute => !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
        .ToDictionary();

    private string IconClass
    {
        get
        {
            var classes = new List<string> { "oai-icon" };

            if (AdditionalAttributes?.TryGetValue("class", out var htmlClass) == true)
            {
                var classValue = htmlClass?.ToString();
                if (!string.IsNullOrWhiteSpace(classValue))
                {
                    classes.Add(classValue);
                }
            }

            if (!string.IsNullOrWhiteSpace(CssClass))
            {
                classes.Add(CssClass);
            }

            return string.Join(" ", classes);
        }
    }
}
