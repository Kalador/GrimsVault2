using Vault.Core;
using Vault.Core.Internal;
using Vault.ViewModes;

namespace Vault.Features.Cex;

/// <summary>
/// Service for the CEX feature. This service is responsible for handling all CEX-API related operations and interactions with the VaultService.
/// </summary>
public class CexService(VaultService vault) : IVaultFeature
{
    private string cacheFile => "cex_cache.json";
    private static readonly HttpClient http = new();

    private class Meta : IVaultFeatureMetaData
    {
        public string Name => "Local CEX Stock Check";
        public string Route => "/cex";
    }

    public IVaultFeatureMetaData MetaData { get; } = new Meta();
    public string Status() => "CEX Feature OK";
    public VaultViewMode DefaultViewMode => ColumnsViewMode.Instance;

    private readonly string[] priorityStores = new[] { "Leeds" };

    // ------------------------------------------------------------
    // MAIN PAGE LOAD (CACHE ONLY)
    // ------------------------------------------------------------
    public async Task<PageContentModel> GetItemsWithCacheAsync()
    {
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

        var cache = vault.Cache
            .ReadCache<Dictionary<string, CexBox>>(cacheFile)
            ?? new Dictionary<string, CexBox>();

        var model = new PageContentModel();

        foreach (var item in items)
        {
            var id = item.CexID ?? "";
            cache.TryGetValue(id, out var box);

            var card = BuildCardFromBox(id, item.Name ?? "(no name)", box);
            model.Items.Add(card);
        }

        return model;
    }

    // ------------------------------------------------------------
    // RETRY: REFRESH + BUILD CARD
    // ------------------------------------------------------------
    public async Task<PageContentItem> BuildSingleCardAsync(string id)
    {
        var box = await RefreshSingleAsync(id);

        // We do NOT have the VaultItem.Name here, so fallback to "(no name)"
        return BuildCardFromBox(id, "(no name)", box);
    }

    // ------------------------------------------------------------
    // CARD MAPPING (USED BY BOTH MAIN LOAD + RETRY)
    // ------------------------------------------------------------
    private PageContentItem BuildCardFromBox(string id, string fallbackName, CexBox? box)
    {
        var card = new PageContentItem
        {
            Title = "🎮 " + (box?.BoxName ?? fallbackName),
            ImageUrl = box?.ImageUrl,
            Subtitle = box?.SellPrice.FormatPrice(),
            Ticker = BuildTicker(box),
            SecondFooter = "🕒 " + box?.LastRefreshed.FormatAge(),

            Url = box != null && !box.HasError
                ? $"https://uk.webuy.com/product-detail?id={box.BoxId}"
                : $"/cex/retry?id={id}",

            HasError = box?.HasError ?? true,

            Meta =
            {
                ["id"]     = id,
                ["stores"] = box?.StoresInStock ?? []
            }
        };

        // Grouping logic
        if (box == null || box.HasError)
        {
            card.Group = "Out of Stock";
            return card;
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

        return card;
    }

    // ------------------------------------------------------------
    // SINGLE ITEM REFRESH (USED BY RETRY ENDPOINT)
    // ------------------------------------------------------------
    private async Task<CexBox?> RefreshSingleAsync(string id)
    {
        try
        {
            var detailUrl = $"https://wss2.cex.uk.webuy.io/v3/boxes/{id}/detail";
            var detail = await http.GetFromJsonAsync<CexDetailResponse>(detailUrl);

            if (detail?.Data == null)
                return BuildErrorBox(id, "No detail data");

            var stockUrl = $"https://wss2.cex.uk.webuy.io/v3/boxes/{id}/availability";
            var stock = await http.GetFromJsonAsync<CexStockResponse>(stockUrl);

            if (stock?.Data == null)
                return BuildErrorBox(id, "No stock data");

            var box = new CexBox
            {
                BoxId = id,
                BoxName = detail.Data.BoxName,
                ImageUrl = detail.Data.ImageUrls?.FirstOrDefault(),
                SellPrice = detail.Data.SellPrice,
                CashPrice = detail.Data.CashPrice,
                ExchangePrice = detail.Data.ExchangePrice,
                StockOnline = stock.Data.OnlineStock,
                StockInStore = stock.Data.Stores.Sum(s => s.Quantity),
                StoresInStock = stock.Data.Stores
                    .Select(s => $"{s.StoreName} ({s.Quantity})")
                    .ToList(),
                LastRefreshed = DateTime.UtcNow,
                HasError = false
            };

            UpdateCache(id, box);
            return box;
        }
        catch
        {
            return BuildErrorBox(id, "Exception");
        }
    }

    // ------------------------------------------------------------
    // CACHE UPDATE
    // ------------------------------------------------------------
    private void UpdateCache(string id, CexBox box)
    {
        var cache = vault.Cache
            .ReadCache<Dictionary<string, CexBox>>(cacheFile)
            ?? new Dictionary<string, CexBox>();

        cache[id] = box;
        vault.Cache.WriteCache(cacheFile, cache);
    }

    // ------------------------------------------------------------
    // ERROR BOX BUILDER
    // ------------------------------------------------------------
    private static CexBox BuildErrorBox(string id, string reason)
    {
        return new CexBox
        {
            BoxId = id,
            BoxName = "Error",
            ImageUrl = null,
            SellPrice = 0,
            CashPrice = 0,
            ExchangePrice = 0,
            StockOnline = 0,
            StockInStore = 0,
            StoresInStock = new(),
            LastRefreshed = DateTime.UtcNow,
            HasError = true
        };
    }

    // ------------------------------------------------------------
    // TICKER BUILDER
    // ------------------------------------------------------------
    private string BuildTicker(CexBox? box)
    {
        if (box == null || box.HasError)
            return "<span class='text-muted'>Out of Stock</span>";

        var parts = new List<string>();

        if (box.StockOnline > 0)
            parts.Add("Online");

        var priority = box.StoresInStock
            .Where(store =>
                priorityStores.Any(p =>
                    store.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var store in priority)
        {
            parts.Add(
                $"<span class='status-badge' style='background-color:#d4edda; color:#155724;'>{store}</span>"
            );
        }

        var nonPriorityCount = box.StoresInStock.Count - priority.Count;

        if (priority.Count > 0)
        {
            if (nonPriorityCount == 1)
                parts.Add("1 Other Store");
            else if (nonPriorityCount > 1)
                parts.Add($"{nonPriorityCount} Other Stores");
        }
        else
        {
            if (box.StoresInStock.Count == 1)
                parts.Add("1 Store");
            else if (box.StoresInStock.Count > 1)
                parts.Add($"{box.StoresInStock.Count} Stores");
        }

        if (parts.Count == 0)
            return "<span class='text-muted'>Out of Stock</span>";

        return string.Join(", ", parts);
    }

    // ------------------------------------------------------------
    // API RESPONSE MODELS (SELF-CONTAINED)
    // ------------------------------------------------------------
    private class CexDetailResponse
    {
        public CexDetailData? Data { get; set; }
    }

    private class CexDetailData
    {
        public string? BoxName { get; set; }
        public List<string>? ImageUrls { get; set; }
        public decimal SellPrice { get; set; }
        public decimal CashPrice { get; set; }
        public decimal ExchangePrice { get; set; }
    }

    private class CexStockResponse
    {
        public CexStockData? Data { get; set; }
    }

    private class CexStockData
    {
        public int OnlineStock { get; set; }
        public List<CexStoreEntry> Stores { get; set; } = new();
    }

    private class CexStoreEntry
    {
        public string StoreName { get; set; } = "";
        public int Quantity { get; set; }
    }
}
