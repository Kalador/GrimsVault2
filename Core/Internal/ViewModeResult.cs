using Microsoft.AspNetCore.Mvc;

namespace Vault.Core;

/// <summary>
/// Renders a ViewMode and returns a standard JSON payload containing:
/// - html:    the rendered Razor output
/// - scripts: the ViewMode's JavaScript files
/// - styles:  the ViewMode's CSS files
///
/// All feature controllers use this to return ViewMode content
/// without duplicating rendering or JSON construction.
/// </summary>
public static class ViewModeResult
{
    /// <summary>
    /// Renders the ViewMode's Razor view to a string and packages it
    /// with the ViewMode's declared assets.
    /// </summary>
    public static async Task<IActionResult> BuildAsync(
        Controller controller,
        VaultViewMode viewMode,
        object model)
    {
        var html = await ViewRenderer.RenderAsync(
            controller,
            viewMode.ViewPath,
            model
        );

        return controller.Json(new
        {
            html,
            scripts = viewMode.Scripts,
            styles = viewMode.Styles
        });
    }
}
