using Microsoft.AspNetCore.Mvc;
using Vault.Core;

namespace Vault.Features.Rawg;

/// <summary>
/// RAWG controller (Vault 2.0).
/// 
/// Responsibilities:
/// - Provide the /rawg entry point
/// - Use the Loader pattern (GET → loader, POST → action)
/// - Delegate all business logic to RawgService
/// - Pass PageContentModel to the active ViewMode
/// 
/// This controller contains *no* UI logic and *no* feature logic.
/// </summary>
public sealed class RawgController : Controller
{
    private readonly RawgService _rawg;

    /// <summary>
    /// The ViewMode used to render RAWG pages.
    /// This comes from the feature, not the controller.
    /// </summary>
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
            Message = "Loading RAWG data...",
            TargetUrl = Url.Action("IndexPost", "Rawg"),
            TargetAction = "LoadRecentScrapes"
        });
    }

    // =====================================================================
    //  POST /rawg  → Action router
    // =====================================================================

    /// <summary>
    /// Loader POST endpoint.
    /// Dispatches based on X-Loader-Action header.
    /// </summary>
    [HttpPost("/rawg")]
    public async Task<IActionResult> IndexPost(
        [FromHeader(Name = "X-Loader-Action")] string action)
    {
        return action switch
        {
            "LoadRecentScrapes" => await LoadRecentScrapes(),
            _ => BadRequest($"Unknown loader action: {action}")
        };
    }

    // =====================================================================
    //  ACTION: Recent Scrapes
    // =====================================================================

    /// <summary>
    /// Loads the 30 most recently scraped VaultItems,
    /// builds a PageContentModel via RawgService,
    /// and renders it using the active ViewMode.
    /// </summary>
    private async Task<IActionResult> LoadRecentScrapes()
    {
        var model = await _rawg.GetRecentScrapesAsync();

        return await ViewModeResult.BuildAsync(
            controller: this,
            viewMode: _viewMode,
            model: model
        );
    }

    // =====================================================================
    //  FUTURE ACTIONS (detail, match, retry, etc.)
    // =====================================================================

    // These will be added later:
    //
    // private async Task<IActionResult> LoadDetail()
    // private async Task<IActionResult> ApplyMatch()
    // private async Task<IActionResult> RetryScrape()
    //
    // The controller remains thin — all logic stays in RawgService.
}
