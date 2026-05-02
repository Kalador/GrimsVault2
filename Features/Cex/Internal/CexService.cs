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

        // 3. Priority stores (hack for now)
        var priorityStores = new[] { "Leeds" };

        // 4. Build flat PageContentModel
        var model = new PageContentModel();

        foreach (var item in items)
        {
            var cexId = item.CexID ?? "";
            cache.TryGetValue(cexId, out var box);

            // Build card
            var card = new PageContentItem
            {
                Title = "🎮 " + (box?.BoxName ?? item.Name ?? "(no name)"),
                ImageUrl = box?.ImageUrl,
                Subtitle = box?.SellPrice.FormatPrice(),
                
                // Correct ticker: raw store names (v1 behaviour)
                Ticker = BuildTicker(box!, priorityStores),


                // Timestamp footer
                SecondFooter = "🕒 " + box?.LastRefreshed.FormatAge(),


                Url = box != null ? $"https://uk.webuy.com/product-detail?id={box.BoxId}" : null,

                // New first‑class error property
                HasError = box?.HasError ?? true,

                // Clean Meta (no raw, no error)
                Meta =
            {
                ["id"]     = cexId,
                ["stores"] = box?.StoresInStock ?? []
            }
            };

            // Determine grouping
            if (box == null || box.HasError)
            {
                card.Group = "Out of Stock";
                model.Items.Add(card);
                continue;
            }

            var localStores = box.StoresInStock
                .Where(store =>
                    priorityStores.Any(p =>
                        store.Contains(p, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            bool hasPriority = localStores.Count != 0;
            bool hasAnyStock = box.StoresInStock.Count != 0;

            if (hasPriority)
                card.Group = "Priority Stock";
            else if (hasAnyStock)
                card.Group = "Other Stock";
            else
                card.Group = "Out of Stock";

            model.Items.Add(card);
        }

        return model;
    }

    private static string BuildTicker(CexBox box, string[] priorityStores)
    {
        if (box == null || box.HasError)
            return "<span class='text-muted'>Out of Stock</span>";

        var parts = new List<string>();

        // 1. Online stock
        if (box.StockOnline > 0)
            parts.Add("Online");

        // 2. Priority stores (badge style)
        var priority = box.StoresInStock
            .Where(store =>
                priorityStores.Any(p =>
                    store.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var store in priority)
        {
            parts.Add(
                $"<span class='status-badge' " +
                $"style='background-color:#d4edda; color:#155724;'>" +
                $"{store}</span>"
            );
        }

        // 3. Non‑priority stores
        var nonPriorityCount = box.StoresInStock.Count - priority.Count;

        if (priority.Count > 0)
        {
            // PRIORITY STOCK COLUMN
            if (nonPriorityCount == 1)
                parts.Add("1 Other Store");
            else if (nonPriorityCount > 1)
                parts.Add($"{nonPriorityCount} Other Stores");
        }
        else
        {
            // OTHER STOCK COLUMN
            if (box.StoresInStock.Count == 1)
                parts.Add("1 Store");
            else if (box.StoresInStock.Count > 1)
                parts.Add($"{box.StoresInStock.Count} Stores");
        }

        // 4. Out of stock fallback
        if (parts.Count == 0)
            return "<span class='text-muted'>Out of Stock</span>";

        return string.Join(", ", parts);
    }








}
