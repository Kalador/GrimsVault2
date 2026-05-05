using Microsoft.AspNetCore.Mvc;
using Vault.Core;

namespace Vault.Features.Cex;

[Route("cex")]
public class CexController : Controller
{
    private readonly CexService _cexService;
    private readonly VaultViewMode _viewMode;

    public CexController(CexService cexService)
    {
        _cexService = cexService;

        // Default for now — but this is where theme switching will plug in later
        _viewMode = cexService.DefaultViewMode;
    }

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

    private async Task<IActionResult> LoadCEXItems()
    {
        var model = await _cexService.GetItemsWithCacheAsync();

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

        return await ViewModeResult.BuildAsync(
            this,
            _viewMode,
            model
        );
    }

    [HttpPost("retry")]
    public async Task<IActionResult> Retry([FromQuery] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Missing id");

        var card = await _cexService.BuildSingleCardAsync(id);

        var html = await ViewRenderer.RenderAsync(
            this,
            _viewMode.CardPartialPath,
            card
        );

        return Content(html, "text/html");
    }
}
