using Microsoft.AspNetCore.Mvc;
using Vault.Core;
namespace Vault.Features.Cex;

[Route("cex")]
public class CexController(CexService cex) : Controller
{
    private readonly CexService _cex = cex;

    [HttpGet("")]
    public IActionResult Index()
    {
        return View("_Loader", new LoaderModel
        {
            Message = "Loading Cached CEX Stock...",
            TargetUrl = Url.Action("", "cex"),
            TargetAction = "Stock"
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
            "stock" => Ok("Groovy!"),
            _ => NotFound()
        };
    }

}
