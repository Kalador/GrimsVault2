using Vault.Core;

namespace Vault.Features.Cex;

/// <summary>
/// Service for the CEX feature. This service is responsible for handling all CEX-API related operations and interactions with the VaultService.
/// </summary>
/// <param name="vault"></param>
public class CexService(VaultService vault) : IVaultFeature
{
    private class Meta : IVaultFeatureMetaData
    {
        public string Name => "Local CEX Stock Check";

        public string Route => "/cex";
    }

    public IVaultFeatureMetaData MetaData { get; } = new Meta();

    public string Status() => "CEX Feature OK";


}
