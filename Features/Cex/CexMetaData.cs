using Vault.Core;

namespace Vault.Features.Cex;

public class CexMetaData : IVaultFeatureMetaData
{
    public string Name => "Local CEX Stock Check";
    public string Route => "/cex";
}

