using Microsoft.AspNetCore.Mvc;
namespace Vault.Core;

public class VaultController(VaultService vault, IEnumerable<IVaultFeature> features) : Controller
{
    private readonly VaultService _vault = vault;
    private readonly IEnumerable<IVaultFeature> _features = features;

    /// <summary>
    /// Returns a simple list of registered Features
    /// </summary>
    public IActionResult Index()
    {
        ViewBag.Status = _vault.Status();
        return View(_features.Select(f => f.MetaData));
    }

    /// <summary>
    /// Test Notion Connectivity
    /// </summary>
    [HttpGet("notion")]
    public IActionResult Notion()
    {
        return View("_Loader", new LoaderModel
        {
            Message = "Requesting Items from Notion",
            TargetUrl = Url.Action("notion"),
            TargetAction = "Test"

        });
    }

    /// <summary>
    /// Endpoint to query Notion for items.
    /// </summary>
    [LoaderAction]
    [HttpPost("notion")]
    public async Task<IActionResult> NotionPost([FromHeader(Name = "X-Loader-Action")] string action)
    {
        // Pull items using default page size (your client handles this)
        var items = await _vault.GetItems(10);

        // Extract just the names
        var names = items
            .Select(i => i.Name ?? "(no name)")
            .ToList();

        return Ok(names);
    }
}
