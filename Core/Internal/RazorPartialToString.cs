using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Vault.Core;

/// <summary>
/// Renders a Razor view (full or partial) into a string.
/// Used when returning ViewMode HTML inside a JSON payload.
/// </summary>
public static class RazorPartialToString
{
    public static async Task<string> RenderAsync(
        Controller controller,
        string viewPath,   // this is a file path, not a view name
        object model)
        {
        controller.ViewData.Model = model;

        using var writer = new StringWriter();

        var engine = controller.HttpContext.RequestServices
            .GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;

        // Load the view directly from the absolute virtual path
        var result = engine!.GetView(null, viewPath, false);

        if (!result.Success)
            throw new FileNotFoundException($"View '{viewPath}' not found.");

        var context = new ViewContext(
            controller.ControllerContext,
            result.View,
            controller.ViewData,
            controller.TempData,
            writer,
            new HtmlHelperOptions()
        );

        await result.View.RenderAsync(context);
        return writer.ToString();
    }
}
