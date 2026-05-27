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
    //  GET /rawg  → Loader
    // =====================================================================

    [HttpGet("/rawg")]
    public IActionResult Index()
    {
        return View("_Loader", new LoaderModel
        {
            Message = "Loading recently scraped metadata...",
            TargetUrl = Url.Action("IndexPost", "Rawg"),
            TargetAction = "LoadRecentScrapes"
        });
    }

    // =====================================================================
    //  POST /rawg → Loader router
    // =====================================================================

    [HttpPost("/rawg")]
    public async Task<IActionResult> IndexPost(
        [FromHeader(Name = "X-Loader-Action")] string action)
    {
        return action switch
        {
            "LoadRecentScrapes" => await LoadRecentScrapes(),
            "RunScrape" => await RunScrape(),
            _ => BadRequest($"Unknown loader action: {action}")
        };
    }

    // =====================================================================
    //  ACTION: Recent Scrapes
    // =====================================================================

    private async Task<IActionResult> LoadRecentScrapes()
    {
        var model = await _rawg.GetRecentScrapesAsync();
        model.Meta["Title"] = $"Recently Scraped: {model.Items.Count} Games";

        model.PageButtons.Add(new PageContentButton
        {
            Text = "Scrape More",
            ActionUrl = "/rawg/scrape"
        });

        return await ViewModeResult.BuildAsync(this, _viewMode, model);
    }

    // =====================================================================
    //  ACTION: Begin Scrape
    // =====================================================================

    [HttpPost("/rawg/scrape")]
    public IActionResult Scrape()
    {
        return View("_Loader", new LoaderModel
        {
            Message = "Scraping. Please wait...",
            TargetUrl = Url.Action("IndexPost", "Rawg"),
            TargetAction = "RunScrape"
        });
    }

    // =====================================================================
    //  ACTION: Run Scrape
    // =====================================================================

    private async Task<IActionResult> RunScrape()
    {
        var model = await _rawg.RunScrapeAsync();
        model.Meta["Title"] = $"Scrape Results: {model.Items.Count} Updated";

        model.PageButtons.Add(new PageContentButton
        {
            Text = "Scrape More",
            ActionUrl = "/rawg/scrape"
        });

        return await ViewModeResult.BuildAsync(this, _viewMode, model);
    }

    // =====================================================================
    //  ACTION: Search (GET)
    // =====================================================================

    [HttpGet("/rawg/search")]
    public IActionResult Search([FromQuery] string id, [FromQuery] string? query)
    {
        return View("_Loader", new LoaderModel
        {
            Message = "Searching RAWG...",
            TargetUrl = Url.Action("SearchPost", "Rawg", new { id, query }),
            TargetAction = "LoadSearch"
        });
    }


    // =====================================================================
    //  ACTION: Search (POST)
    // =====================================================================

    [HttpPost("/rawg/search")]
    public async Task<IActionResult> SearchPost(
        [FromQuery] string id,
        [FromQuery] string? query)
    {
        var model = await _rawg.BuildSearchModelAsync(id, query);
        return await ViewModeResult.BuildAsync(this, _viewMode, model);
    }


    // =====================================================================
    //  ACTION: Apply Match
    // =====================================================================

    [HttpPost("/rawg/apply")]
    public async Task<IActionResult> Apply(
        [FromForm(Name = "id")] string pageId,
        [FromForm(Name = "rawgId")] int rawgId,
        [FromForm(Name = "overwriteName")] bool overwriteName = false)
    {
        var model = await _rawg.ApplyMatchAndBuildSearchModelAsync(pageId, rawgId, overwriteName);
        return await ViewModeResult.BuildAsync(this, _viewMode, model);
    }
}
