using GrimsVault.Models;
using Vault.Core;
using Vault.Core.Internal;
using Vault.ViewModes;

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

    public VaultViewMode DefaultViewMode => ColumnsViewMode.Instance;

    /// <summary>
    /// Returns a list of objects representing VaultItems that have a CEX API URL set.
    /// Result includes Cache Data
    /// </summary>
    public async Task<PageContentModel> GetItemsWithCacheAsync()
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

        // 3. Load priority stores (same as old system)
        //TODO: This is a bit of a hack, but it works for now.
        var priorityStores = new[] { "Leeds" };
        //var priorityStores = await vault.GetCexStoresAsync(priorityOnly: true);

        // 4. Build the PageContentModel
        var model = new PageContentModel();

        var priorityGroup = new PageContentGroup { Title = "Priority Stock" };
        var otherGroup = new PageContentGroup { Title = "Other Stock" };
        var outGroup = new PageContentGroup { Title = "Out of Stock" };

        foreach (var item in items)
        {
            var cexId = item.CexID ?? "";
            cache.TryGetValue(cexId, out var box);

            // Build card
            var card = new PageContentItem
            {
                Title = box?.BoxName ?? item.Name ?? "(no name)",
                ImageUrl = box?.ImageUrl,
                Subtitle = box != null ? $"£{box.SellPrice:0.00}" : null,
                Footer = box != null ? Helpers.FormatAge(box.LastRefreshed) : null,
                Url = box != null ? $"https://uk.webuy.com/product-detail?id={box.BoxId}" : null,
                Meta =
            {
                ["id"] = cexId,
                ["error"] = box?.HasError ?? true,
                ["stores"] = box?.StoresInStock ?? [],
                ["raw"] = box
            }
            };

            // Determine grouping (faithful to old logic)
            if (box == null || box.HasError)
            {
                outGroup.Items.Add(card);
                continue;
            }

            //var localStores = box.StoresInStock
            //    .Where(store =>
            //        priorityStores.Any(p =>
            //            store.Contains(p.Name, StringComparison.OrdinalIgnoreCase)))
            //    .ToList();

            //ToDo: This is a bit of a hack
            var localStores = box.StoresInStock
    .Where(store =>
        priorityStores.Any(p =>
            store.Contains(p, StringComparison.OrdinalIgnoreCase)))
    .ToList();


            bool hasPriority = localStores.Any();
            bool hasAnyStock = box.StoresInStock.Any();

            if (hasPriority)
                priorityGroup.Items.Add(card);
            else if (hasAnyStock)
                otherGroup.Items.Add(card);
            else
                outGroup.Items.Add(card);
        }

        model.Groups.Add(priorityGroup);
        model.Groups.Add(otherGroup);
        model.Groups.Add(outGroup);

        return model;
    }




}
