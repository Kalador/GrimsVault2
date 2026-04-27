using GrimsVault.Models;
using Microsoft.AspNetCore.Mvc;
using Vault.Core;

namespace Vault.Features.Cex;

/// <summary>
/// Service for the CEX feature. This service is responsible for handling all CEX-API related operations and interactions with the VaultService.
/// </summary>
/// <param name="vault"></param>
public class CexService(VaultService vault) : IVaultFeature
{
    private string cacheFile => "cex_cache.json";

    private class Meta : IVaultFeatureMetaData
    {
        public string Name => "Local CEX Stock Check";

        public string Route => "/cex";
    }

    public IVaultFeatureMetaData MetaData { get; } = new Meta();

    public string Status() => "CEX Feature OK";

    /// <summary>
    /// Returns a list of objects representing VaultItems that have a CEX API URL set.
    /// Result includes Cache Data
    /// </summary>
    public async Task<List<object>> GetItemsWithCacheAsync()
    {
        // 1. Load VaultItems with a CEX ID
        var items = await vault.GetItems(new QueryDefinition
        {
            Filters =
        {
            new QueryFilter
            {
                Property = "CEX Product Code",
                Operator = QueryOperator.NotEmpty
            }
        }
        });

        // 2. Load the CeX cache
        var cache = vault.Cache
            .ReadCache<Dictionary<string, CexBox>>(cacheFile)
            ?? new Dictionary<string, CexBox>();

        // 3. Merge VaultItems + Cache
        var result = new List<object>();

        foreach (var item in items)
        {
            var cexId = item.CexID;
            cache.TryGetValue(cexId ?? "", out var cached);

            result.Add(new
            {
                Name = item.Name ?? "(no name)",
                CexId = cexId,
                Cache = cached
            });
        }

        return result;
    }


}
