using Microsoft.AspNetCore.Mvc;
using Vault.Core;

namespace Vault.Features.Cex;

[Route("cex")]
public class CexController(CexService cexService) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View("_Loader", new LoaderModel
        {
            Message = "Loading CEX Data...",
            TargetUrl = Url.Action("", "cex"),
            TargetAction = "LoadStock"
        });
    }

    /// <summary>
    /// Action returns the current cached CEX Stock, displaying all details.
    /// </summary>
    [HttpPost("")]
    [LoaderAction]
    public async Task<IActionResult> NotioIndexPost([FromHeader(Name = "X-Loader-Action")] string action)
    {
        return action.ToLower() switch
        {
            "loadstock" => await LoadCEXItems(),
            _ => NotFound()
        };
    }

    private async Task<IActionResult> LoadCEXItems()
    {
        var model = await cexService.GetItemsWithCacheAsync();

        // Define the desired column order (v1 behaviour)
        var groupOrder = new Dictionary<string, int>
        {
            ["Priority Stock"] = 0,
            ["Other Stock"] = 1,
            ["Out of Stock"] = 2
        };

        // Reorder the items BEFORE the ViewMode sees them
        model.Items = [.. model.Items
            .OrderBy(i => groupOrder.TryGetValue(i.Group, out var order) ? order : 99)
            .ThenBy(i => i.HasError ? 1 : 0)   // errors always last within the group
            .ThenBy(i => i.Title)];

        

        return View(cexService.DefaultViewMode.PartialView, model);
    }

}
