using Vault.Core;
using Vault.Core.Internal;

namespace Vault.Features.Cex;

public class CexService(VaultService vault) : VaultFeatureBase, IVaultFeature
{
    private readonly CexApiClient _api = new();

    // NEW FIELD (allowed under H2)
    private readonly CexReportHandler _reportHandler = new();

    private static string CacheFile => "cex_cache.json";

    private class Meta : IVaultFeatureMetaData
    {
        public string Name => "Local CEX Stock Check";
        public string Route => "/cex";
    }

    public IVaultFeatureMetaData MetaData { get; } = new Meta();
    public string Status() => "CEX Feature OK";
    public VaultViewMode DefaultViewMode => ColumnsViewMode.Instance;

    //TODO: Pull Priority Stores from DataHandler config instead of hardcoding here
    private readonly string[] priorityStores = ["Leeds"];

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
            .ReadCache<Dictionary<string, CexBox>>(CacheFile)
            ?? [];

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
    // CARD MAPPING
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
                ? CexHelpers.BuildCexUrl(box.BoxId)
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

        var localStores = GetLocalStoresForBox(box);

        if (localStores.Count != 0)
            card.Group = "Priority Stock";
        else if (box.StoresInStock.Count != 0)
            card.Group = "Other Stock";
        else
            card.Group = "Out of Stock";

        return card;
    }

    // ------------------------------------------------------------
    // SINGLE ITEM REFRESH
    // ------------------------------------------------------------
    private async Task<CexBox?> RefreshSingleAsync(string id)
    {
        return await TryAsync(async () =>
        {
            var detail = await _api.GetDetailAsync(id);

            var d = detail?.Response?.Data?.BoxDetails?.FirstOrDefault();
            if (d == null)
                return BuildErrorBox(id);

            var box = CexBoxMapper.FromDetail(id, d);
            if (d.OutOfStock == 1)
            {
                UpdateCache(id, box);
                return box;
            }

            var stock = await _api.GetStockAsync(id);

            var stockDetails = stock?.Response?.Data?.StockDetails
                               ?? [];

            CexBoxMapper.ApplyStock(box, stockDetails);

            UpdateCache(id, box);
            return box;

        },
        () => BuildErrorBox(id));
    }

    // ------------------------------------------------------------
    // CACHE UPDATE
    // ------------------------------------------------------------
    private void UpdateCache(string id, CexBox box)
    {
        var cache = vault.Cache
            .ReadCache<Dictionary<string, CexBox>>(CacheFile)
            ?? [];

        cache[id] = box;
        vault.Cache.WriteCache(CacheFile, cache);
    }

    // ------------------------------------------------------------
    // ERROR BOX
    // ------------------------------------------------------------
    private static CexBox BuildErrorBox(string id)
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
            StoresInStock = [],
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

        var stores = box.StoresInStock ?? [];

        bool hasOnline = box.StockOnline > 0;

        // Split stores into priority + other
        var priority = new List<string>();
        int otherCount = 0;

        foreach (var store in stores)
        {
            if (priorityStores.Any(p =>
                store.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                priority.Add(store);
            }
            else
            {
                otherCount++;
            }
        }

        // Build output parts
        var parts = new List<string>();

        if (hasOnline)
            parts.Add("Online");

        // Priority stores with badges
        foreach (var store in priority)
        {
            parts.Add(
                $"<span class='status-badge' style='background-color:#d4edda; color:#155724;'>{store}</span>"
            );
        }

        // Summary of other stores
        if (otherCount == 1)
            parts.Add("and 1 other store");
        else if (otherCount > 1)
            parts.Add($"and {otherCount} other stores");

        // If nothing at all
        if (!hasOnline && priority.Count == 0 && otherCount == 0)
            return "<span class='text-muted'>Out of Stock</span>";

        return string.Join(", ", parts);
    }

    // ------------------------------------------------------------
    // CACHE HELPERS FOR CRON
    // ------------------------------------------------------------
    private Dictionary<string, CexBox> ReadCache()
    {
        return vault.Cache
            .ReadCache<Dictionary<string, CexBox>>(CacheFile)
            ?? [];
    }

    private IEnumerable<string> GetMissing(IEnumerable<string> ids)
    {
        var cache = ReadCache();
        return ids.Where(id => !cache.ContainsKey(id));
    }

    private IEnumerable<CexBox> GetOldest(int count)
    {
        var cache = ReadCache();

        return cache.Values
            .OrderBy(b => b.LastRefreshed)
            .Take(count);
    }

    // ------------------------------------------------------------
    // REFRESH SUMMARY MODEL
    // ------------------------------------------------------------
    public record RefreshSummary(
        int Processed,
        int Success,
        int Failed,
        List<string> Lines
    )
    {
        public string ToPlainText()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            return
                $"CeX refresh at {timestamp} UTC\n" +
                $"Processed: {Processed}\n" +
                $"Success: {Success}\n" +
                $"Failed: {Failed}\n" +
                $"Items:\n" +
                string.Join("\n", Lines);
        }
    }

    // ------------------------------------------------------------
    // ATOMIC REFRESH WRAPPER
    // ------------------------------------------------------------
    private async Task<(bool ok, CexBox? box)> RefreshAtomicAsync(string id)
    {
        var box = await RefreshSingleAsync(id);
        return (box != null && !box.HasError, box);
    }

    // ------------------------------------------------------------
    // ROLLING REFRESH (CRON)
    // ------------------------------------------------------------
    public async Task<RefreshSummary> RefreshRollingAsync(int count = 6)
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

        var ids = items
            .Select(i => i.CexID!)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        var missing = GetMissing(ids).Take(count).ToList();
        var refreshList = new List<string>(missing);

        if (refreshList.Count < count)
        {
            var needed = count - refreshList.Count;
            var oldest = GetOldest(needed)
                .Where(b => !refreshList.Contains(b.BoxId!))
                .Select(b => b.BoxId!)
                .ToList();

            refreshList.AddRange(oldest);
        }

        int success = 0;
        int fail = 0;
        var lines = new List<string>();

        foreach (var id in refreshList)
        {
            var (ok, box) = await RefreshAtomicAsync(id);

            if (ok)
            {
                success++;
                lines.Add($"{id}: OK");
            }
            else
            {
                fail++;
                lines.Add($"{id}: FAIL");
            }
        }

        return new RefreshSummary(
            Processed: refreshList.Count,
            Success: success,
            Failed: fail,
            Lines: lines
        );
    }

    // ------------------------------------------------------------
    // EMAIL REPORT – FULL (DELEGATED TO HANDLER)
    // ------------------------------------------------------------
    public async Task<string> SendFullReportAsync()
    {
        return await _reportHandler.SendFullReportAsync(
            vault,
            ReadCache(),
            HasLocalStock
        );
    }

    // ------------------------------------------------------------
    // EMAIL REPORT – UPDATED (DELEGATED TO HANDLER)
    // ------------------------------------------------------------
    public async Task<string> SendUpdatedReportAsync()
    {
        return await _reportHandler.SendUpdatedReportAsync(
            vault,
            ReadCache(),
            GetLocalStoresForBox
        );
    }

    // ------------------------------------------------------------
    // LOCAL STORE FILTERING (UNIQUE TO CexService)
    // ------------------------------------------------------------
    private bool HasLocalStock(CexBox box)
    {
        return GetLocalStoresForBox(box).Count != 0;
    }

    private List<string> GetLocalStoresForBox(CexBox box)
    {
        var stores = box.StoresInStock ?? [];

        return [.. stores
            .Where(store =>
                priorityStores.Any(p =>
                    store.Contains(p, StringComparison.OrdinalIgnoreCase)))];
    }
}
