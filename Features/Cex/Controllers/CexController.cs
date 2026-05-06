using Microsoft.AspNetCore.Mvc;
using Vault.Core;

namespace Vault.Features.Cex;

[Route("cex")]
public class CexController(CexService cexService) : Controller
{
    private readonly VaultViewMode _viewMode = cexService.DefaultViewMode;

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

    /// <summary>
    /// Triggers automatic refresh of the CEX stock data, limited to 6 items per run.
    /// </summary>
    [HttpGet("update")]
    public async Task<IActionResult> Update()
    {
        var summary = await cexService.RefreshRollingAsync(6);
        return Content(summary.ToPlainText());
    }

    // ------------------------------------------------------------
    // NEW: REPORT ENDPOINTS (CRON)
    // ------------------------------------------------------------

    /// <summary>
    /// Sends a FULL CeX stock report (all local stock).
    /// </summary>
    [HttpGet("report/full")]
    public async Task<IActionResult> ReportFull()
    {
        var summary = await cexService.SendFullReportAsync();
        return Content(summary);
    }

    /// <summary>
    /// Sends an INCREMENTAL CeX stock report (changes only).
    /// </summary>
    [HttpGet("report/incremental")]
    public async Task<IActionResult> ReportIncremental()
    {
        var summary = await cexService.SendUpdatedReportAsync();
        return Content(summary);
    }

    // ------------------------------------------------------------
    // INTERNAL: LOAD UI ITEMS
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

        return await ViewModeResult.BuildAsync(
            this,
            _viewMode,
            model
        );
    }

    // ------------------------------------------------------------
    // RETRY SINGLE CARD
    // ------------------------------------------------------------
    [HttpPost("retry")]
    public async Task<IActionResult> Retry([FromQuery] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Missing id");

        var card = await cexService.BuildSingleCardAsync(id);

        var html = await ViewRenderer.RenderAsync(
            this,
            _viewMode.CardPartialPath,
            card
        );

        return Content(html, "text/html");
    }
}
