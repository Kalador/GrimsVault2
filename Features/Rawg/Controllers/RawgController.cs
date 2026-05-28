using Microsoft.AspNetCore.Mvc;
using Vault.Core;
using Vault.Core.Internal;

namespace Vault.Features.Rawg;

public sealed class RawgController : Controller
{
    private readonly RawgService _rawg;
    private readonly VaultViewMode _viewMode;

    public RawgController(RawgService rawg)
    {
        _rawg = rawg;
        _viewMode = rawg.DefaultViewMode;
    }

    // =====================================================================
    //  LOADER ENTRY
    // =====================================================================

    [HttpGet("/rawg")]
    public IActionResult Index()
    {
        return View("_Loader", new LoaderModel
        {
            Message = "Loading...",
            TargetUrl = "/rawg",
            TargetAction = "LoadRecent"
        });
    }

    // =====================================================================
    //  LOADER ROUTER
    // =====================================================================

    [HttpPost("/rawg")]
    public async Task<IActionResult> IndexPost(
        [FromHeader(Name = "X-Loader-Action")] string action)
    {
        return action switch
        {
            "LoadRecent" => await LoadRecent(),
            "RunScrape" => await RunScrape(),
            _ => BadRequest()
        };
    }

    private async Task<IActionResult> LoadRecent()
    {
        var model = await _rawg.GetRecentScrapesAsync();
        return await ViewModeResult.BuildAsync(this, _viewMode, model);
    }

    private async Task<IActionResult> RunScrape()
    {
        var model = await _rawg.RunScrapeAsync();
        return await ViewModeResult.BuildAsync(this, _viewMode, model);
    }

    // =====================================================================
    //  SEARCH (GET → LOADER)
    // =====================================================================

    [HttpGet("/rawg/search")]
    public IActionResult Search([FromQuery] string id, [FromQuery] string? query)
    {
        // IMPORTANT:
        // The loader ONLY forwards the URL.
        // So id/query MUST be in TargetUrl.
        return View("_Loader", new LoaderModel
        {
            Message = "Searching...",
            TargetUrl = $"/rawg/search?id={id}&query={query}",
            TargetAction = "LoadSearch"
        });
    }

    // =====================================================================
    //  SEARCH (POST → ROUTER)
    // =====================================================================

    [HttpPost("/rawg/search")]
    public async Task<IActionResult> SearchRouter(
        [FromHeader(Name = "X-Loader-Action")] string action,
        string? id,
        string? query)
    {
        return action switch
        {
            "LoadSearch" => await LoadSearch(id, query),
            _ => BadRequest()
        };
    }

    // =====================================================================
    //  SEARCH (LOGIC)
    // =====================================================================

    private async Task<IActionResult> LoadSearch(string id, string? query)
    {
        var model = await _rawg.BuildSearchModelAsync(id, query);
        return await ViewModeResult.BuildAsync(this, _viewMode, model);
    }

    // =====================================================================
    //  APPLY MATCH
    // =====================================================================

    [HttpPost("/rawg/apply")]
    public async Task<IActionResult> Apply(
        [FromForm] string id,
        [FromForm] int rawgId,
        [FromForm] bool overwriteName)
    {
        var model = await _rawg.ApplyMatchAndBuildSearchModelAsync(id, rawgId, overwriteName);
        return await ViewModeResult.BuildAsync(this, _viewMode, model);
    }
}
