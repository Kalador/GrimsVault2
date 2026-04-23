using Microsoft.AspNetCore.Mvc;

namespace Vault.Features.Cex;

[Route("cex")]
public class CexController : Controller
{
    private readonly CexService _cex;

    public CexController(CexService cex)
    {
        _cex = cex;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewBag.Status = _cex.Status();
        return View();
    }
}
