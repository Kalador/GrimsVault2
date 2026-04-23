using Microsoft.AspNetCore.Mvc;
using Vault.Core;

namespace Vault.Controllers;

public class VaultController : Controller
{
    private readonly VaultService _vault;
    private readonly IEnumerable<IVaultFeatureMetaData> _features;

    public VaultController(VaultService vault, IEnumerable<IVaultFeatureMetaData> features)
    {
        _vault = vault;
        _features = features;
    }

    public IActionResult Index()
    {
        ViewBag.Status = _vault.Status();
        return View(_features);
    }
}
