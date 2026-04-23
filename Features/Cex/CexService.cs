using Vault.Core;

namespace Vault.Features.Cex;

public class CexService : IVaultFeature
{
    private readonly VaultService _vault;
    
    public CexService(VaultService vault)
    {
        _vault = vault;
    }

    public string Status() => "CEX Feature OK";
}

