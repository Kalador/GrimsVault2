using System.Text.Json;
using System.Text.Json.Serialization;
using Vault.Core;
using Vault.Core.Internal;
using Vault.ViewModes;
using Vault.Features; // <-- to access VaultFeatureBase

namespace Vault.Features.Cex;

public class CexService(VaultService vault) : VaultFeatureBase, IVaultFeature
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

            model.Items.Add(BuildCardFromBox(id, item.Name ?? "(no name)", box));
        }

        return model;
    }

    // ------------------------------------------------------------
    // RETRY: REFRESH + BUILD CARD
    // ------------------------------------------------------------
    public async Task<PageContentItem> BuildSingleCardAsync(string id)
    {
        var box = await RefreshSingleAsync(id);
        return BuildCardFromBox(id, "(no name)", box);
    }

    // ------------------------------------------------------------
    // CARD MAPPING (UNCHANGED)
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

        if (localStores.Any())
            card.Group = "Priority Stock";
        else if (box.StoresInStock.Any())
            card.Group = "Other Stock";
        else
            card.Group = "Out of Stock";

        return card;
    }

    private async Task<CexBox?> RefreshSingleAsync(string id)
    {
        return await TryAsync(async () =>
        {
            // 1. DETAIL
            var detail = await GetJsonAsync<CexDetailResponse>(
                $"https://wss2.cex.uk.webuy.io/v3/boxes/{id}/detail"
            );

            var d = detail?.Response?.Data?.BoxDetails?.FirstOrDefault();
            if (d == null)
                return BuildErrorBox(id, "No detail data");

            // 2. Build the base box from DETAIL only
            var box = new CexBox
            {
                BoxId = id,
                BoxName = d.BoxName,
                ImageUrl = d.ImageUrls?.Large,
                SellPrice = d.SellPrice,
                CashPrice = d.CashPrice,
                ExchangePrice = d.ExchangePrice,
                StockOnline = d.EcomQuantityOnHand,
                StockInStore = 0,
                StoresInStock = new List<string>(),
                LastRefreshed = DateTime.UtcNow,
                HasError = false
            };

            // 3. If detail says out of stock → skip stock API entirely
            if (d.OutOfStock == 1)
            {
                UpdateCache(id, box);
                return box;
            }

            // 4. Otherwise → enrich with store stock
            var stock = await GetJsonAsync<CexStockResponse>(
                $"https://wss2.cex.uk.webuy.io/v3/boxes/{id}/stock"
            );

            var stockDetails = stock?.Response?.Data?.StockDetails
                               ?? new List<CexStockStore>();

            box.StockInStore = stockDetails.Sum(x => x.Quantity);
            box.StoresInStock = stockDetails
                .Where(x => x.Quantity > 0)
                .Select(x => $"{x.StoreName} ({x.Quantity})")
                .ToList();

            UpdateCache(id, box);
            return box;

        },
        () => BuildErrorBox(id, "Exception"));
    }


    // ------------------------------------------------------------
    // CACHE UPDATE (UNCHANGED)
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
    // ERROR BOX (UNCHANGED)
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
    // TICKER BUILDER (UNCHANGED)
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
            parts.Add($"<span class='status-badge' style='background-color:#d4edda; color:#155724;'>{store}</span>");

        var nonPriority = box.StoresInStock.Count - priority.Count;

        if (priority.Any())
        {
            if (nonPriority == 1)
                parts.Add("1 Other Store");
            else if (nonPriority > 1)
                parts.Add($"{nonPriority} Other Stores");
        }
        else
        {
            if (box.StoresInStock.Count == 1)
                parts.Add("1 Store");
            else if (box.StoresInStock.Count > 1)
                parts.Add($"{box.StoresInStock.Count} Stores");
        }

        return parts.Count == 0
            ? "<span class='text-muted'>Out of Stock</span>"
            : string.Join(", ", parts);
    }

    // ------------------------------------------------------------
    // CEX API MODELS (UNCHANGED)
    // ------------------------------------------------------------
    private class CexDetailResponse
    {
        [JsonPropertyName("response")]
        public CexDetailEnvelope? Response { get; set; }
    }

    private class CexDetailEnvelope
    {
        [JsonPropertyName("data")]
        public CexDetailDataWrapper? Data { get; set; }
    }

    private class CexDetailDataWrapper
    {
        [JsonPropertyName("boxDetails")]
        public List<CexDetailData>? BoxDetails { get; set; }
    }

    private class CexDetailData
    {
        [JsonPropertyName("boxId")]
        public string? BoxId { get; set; }

        [JsonPropertyName("boxName")]
        public string? BoxName { get; set; }

        [JsonPropertyName("sellPrice")]
        public decimal SellPrice { get; set; }

        [JsonPropertyName("cashPrice")]
        public decimal CashPrice { get; set; }

        [JsonPropertyName("exchangePrice")]
        public decimal ExchangePrice { get; set; }

        [JsonPropertyName("ecomQuantityOnHand")]
        public int EcomQuantityOnHand { get; set; }

        [JsonPropertyName("imageUrls")]
        public CexImageUrls? ImageUrls { get; set; }

        [JsonPropertyName("outOfStock")]
        public int OutOfStock { get; set; }

    }

    private class CexImageUrls
    {
        [JsonPropertyName("large")]
        public string? Large { get; set; }

        [JsonPropertyName("medium")]
        public string? Medium { get; set; }

        [JsonPropertyName("small")]
        public string? Small { get; set; }
    }

    private class CexStockResponse
    {
        [JsonPropertyName("response")]
        public CexStockEnvelope? Response { get; set; }
    }

    private class CexStockEnvelope
    {
        [JsonPropertyName("data")]
        public CexStockData? Data { get; set; }
    }

    private class CexStockData
    {
        [JsonPropertyName("stockDetails")]
        public List<CexStockStore> StockDetails { get; set; } = new();
    }

    private class CexStockStore
    {
        [JsonPropertyName("storeName")]
        public string StoreName { get; set; } = "";

        [JsonPropertyName("quantityOnHand")]
        public string QuantityOnHand { get; set; } = "0";

        public int Quantity => int.TryParse(QuantityOnHand, out var q) ? q : 0;
    }
}
