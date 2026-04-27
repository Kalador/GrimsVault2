using GrimsVault.Models;
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
        var result = await cexService.GetItemsWithCacheAsync();
        return Ok(result);
    }


}



