using Microsoft.AspNetCore.Mvc;
using Vault.Core;
using Vault.Core.Internal;

namespace Vault.Features.Rawg;

/// <summary>
/// RAWG controller (Vault 2.0).
/// Provides the /rawg entry point, loader routing, and delegates
/// all feature logic to <see cref="RawgService"/>.
/// </summary>
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

    /// <summary>
    /// Entry point for the RAWG feature.
    /// Returns the unified loader which will POST back to LoadRecentScrapes.
    /// </summary>
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
    //  POST /rawg  → Action router
    // =====================================================================

    /// <summary>
    /// Loader POST endpoint. Dispatches based on X-Loader-Action header.
    /// </summary>
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

    /// <summary>
    /// Loads the most recently scraped items and renders them using
    /// the active ViewMode.
    /// </summary>
    private async Task<IActionResult> LoadRecentScrapes()
    {
        var model = await _rawg.GetRecentScrapesAsync();
        model.Meta["Title"] = $"Recently Scraped: {model.Items.Count} Games";

        model.PageButtons.Add(new PageContentButton
        {
            Text = "Scrape More",
            ActionUrl = "/rawg/scrape"
        });

        return await ViewModeResult.BuildAsync(
            controller: this,
            viewMode: _viewMode,
            model: model
        );
    }

    // =====================================================================
    //  ACTION: Begin Scrape (Loader)
    // =====================================================================

    /// <summary>
    /// Begins a new RAWG scrape operation by showing the loader.
    /// The loader will POST back with X-Loader-Action = "RunScrape".
    /// </summary>
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

    /// <summary>
    /// Executes a RAWG scrape pass via <see cref="RawgService"/>.
    /// Returns a page showing only the items scraped in this run.
    /// </summary>
    private async Task<IActionResult> RunScrape()
    {
        var model = await _rawg.RunScrapeAsync();
        model.Meta["Title"] = $"Scrape Results: {model.Items.Count} Updated";

        model.PageButtons.Add(new PageContentButton
        {
            Text = "Scrape More",
            ActionUrl = "/rawg/scrape"
        });

        return await ViewModeResult.BuildAsync(
            controller: this,
            viewMode: _viewMode,
            model: model
        );
    }

    // =====================================================================
    //  FUTURE ACTIONS (detail, match, retry, etc.)
    // =====================================================================

    // private async Task<IActionResult> LoadDetail()
    // private async Task<IActionResult> ApplyMatch()
    // private async Task<IActionResult> RetryScrape()
}
