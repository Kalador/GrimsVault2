using Microsoft.AspNetCore.Mvc;
using Vault.Core;

namespace Vault.Features.Cex;

[Route("cex")]
public class CexController(CexService cexService) : Controller
{
    // ------------------------------------------------------------
    // Initial GET request.
    // Loads the generic loader page, which will POST back to this
    // controller to fetch the actual ViewMode content.
    // ------------------------------------------------------------
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

    // ------------------------------------------------------------
    // Loader POST entry point.
    // Dispatches to the correct loader action based on the header.
    // ------------------------------------------------------------
    [HttpPost("")]
    [LoaderAction]
    public async Task<IActionResult> NotioIndexPost(
        [FromHeader(Name = "X-Loader-Action")] string action)
    {
        return action.ToLower() switch
        {
            "loadstock" => await LoadCEXItems(),
            _ => NotFound()
        };
    }

    // ------------------------------------------------------------
    // Main loader action.
    // ------------------------------------------------------------
    private async Task<IActionResult> LoadCEXItems()
    {
        var model = await cexService.GetItemsWithCacheAsync();

        var groupOrder = new Dictionary<string, int>
        {
            ["Priority Stock"] = 0,
            ["Other Stock"] = 1,
            ["Out of Stock"] = 2
        };

        model.Items = [.. model.Items
            .OrderBy(i => groupOrder.TryGetValue(i.Group, out var order) ? order : 99)
            .ThenBy(i => i.HasError ? 1 : 0)
            .ThenBy(i => i.Title)];

        // Render the ViewMode and return HTML + assets
        return await ViewModeResult.BuildAsync(
            this,
            cexService.DefaultViewMode,
            model


        );
    }

    // ------------------------------------------------------------
    // Retry endpoint.
    // Returns a single card partial with updated API Data
    // ------------------------------------------------------------
    [HttpPost("retry")]
    public async Task<IActionResult> Retry([FromQuery] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Missing id");

        var card = await cexService.BuildSingleCardAsync(id);

        return PartialView("_VaultCard", card);
    }
}
