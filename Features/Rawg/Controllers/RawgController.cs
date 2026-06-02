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
            Message = "Loading recent scrapes...",
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
    public IActionResult Search(
        [FromQuery] string id,
        [FromQuery] string? query,
        [FromQuery] string? slug)
    {
        bool slugFlag = slug == "on" || slug == "true";

        return View("_Loader", new LoaderModel
        {
            Message = "Searching...",
            TargetUrl = Url.Action("Search", "Rawg", new { id, query, slug = slugFlag }),
            TargetAction = "LoadSearch"
        });
    }

    // =====================================================================
    //  SEARCH (POST → ROUTER)
    // =====================================================================

    [HttpPost("/rawg/search")]
    public async Task<IActionResult> SearchRouter(
        [FromHeader(Name = "X-Loader-Action")] string action,
        [FromQuery] string? id,
        [FromQuery] string? query,
        [FromQuery] bool slug)
    {
        return action switch
        {
            "LoadSearch" => await LoadSearch(id, query, slug),
            _ => BadRequest()
        };
    }

    private async Task<IActionResult> LoadSearch(string? id, string? query, bool? slug)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Missing id");

        var model = await _rawg.BuildSearchModelAsync(id, query, slug);
        return await ViewModeResult.BuildAsync(this, _viewMode, model);
    }

    // =====================================================================
    //  APPLY MATCH (GET → LOADER)
    // =====================================================================

    [HttpGet("/rawg/apply")]
    public IActionResult ApplyLoader(
        [FromQuery] string id,
        [FromQuery] int rawgId,
        [FromQuery] string? overwriteName)
    {
        return View("_Loader", new LoaderModel
        {
            Message = "Applying...",
            TargetUrl = Url.Action("ApplyRouter", "Rawg", new { id, rawgId, overwriteName }),
            TargetAction = "ApplyMatch"
        });
    }

    // =====================================================================
    //  APPLY MATCH (POST → ROUTER)
    // =====================================================================

    [HttpPost("/rawg/apply")]
    public async Task<IActionResult> ApplyRouter(
        [FromHeader(Name = "X-Loader-Action")] string action,
        [FromQuery] string id,
        [FromQuery] int rawgId,
        [FromQuery] string? overwriteName)
    {
        if (action != "ApplyMatch")
            return BadRequest();

        bool overwrite = overwriteName == "on" || overwriteName == "true";

        // Apply the RAWG match
        await _rawg.ApplyMatchAsync(id, rawgId, overwrite);

        //reload
        return await ViewModeResult.BuildAsync(this, _viewMode, await _rawg.GetRecentScrapesAsync());


    }


    // =====================================================================
    //  SCRAPE (GET → LOADER)
    // =====================================================================

    [HttpPost("/rawg/scrape")]
    public IActionResult Scrape()
    {
        return View("_Loader", new LoaderModel
        {
            Message = "Scraping...",
            TargetUrl = "/rawg",
            TargetAction = "RunScrape"
        });
    }
}
