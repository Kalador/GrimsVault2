using Vault.Core;
using Vault.Core.Internal;

namespace Vault.Features.Cex;

public class CexService(VaultService vault) : VaultFeatureBase, IVaultFeature
{
    private readonly CexApiClient _api = new();
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

    //Build the priority store list from the vault
    private List<VaultStore> _priorityStores = [];
    private bool _storesLoaded = false;

    // ------------------------------------------------------------
    // STORE NAME NORMALISATION
    // ------------------------------------------------------------
    private static string CleanStoreName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        var idx = raw.IndexOf(" (", StringComparison.Ordinal);
        return idx > 0 ? raw[..idx] : raw;
    }

    // ------------------------------------------------------------
    // STORE LOADER (PRIORITY STORES FROM NOTION)
    // ------------------------------------------------------------
    private async Task EnsureStoresLoadedAsync()
    {
        if (_storesLoaded)
            return;

        var stores = await vault.GetStores(new QueryDefinition
        {
            Filters =
            {
                new QueryFilter
                {
                    Property = "Priority",
                    Operator = QueryOperator.Equals,
                    Value = true
                }
            }
        });

        _priorityStores = stores;
        _storesLoaded = true;
    }

    // ------------------------------------------------------------
    // MAIN PAGE LOAD (CACHE ONLY)
    // ------------------------------------------------------------
    public async Task<PageContentModel> GetItemsWithCacheAsync()
    {
        await EnsureStoresLoadedAsync();

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

            model.Items.Add(await BuildCardFromBoxAsync(id, item.Name ?? "(no name)", box));
        }

        return model;
    }

    // ------------------------------------------------------------
    // RETRY: REFRESH + BUILD CARD
    // ------------------------------------------------------------
    public async Task<PageContentItem> BuildSingleCardAsync(string id)
    {
        await EnsureStoresLoadedAsync();

        var box = await RefreshSingleAsync(id);
        return await BuildCardFromBoxAsync(id, "(no name)", box);
    }

    // ------------------------------------------------------------
    // CARD MAPPING (ASYNC)
    // ------------------------------------------------------------
    private async Task<PageContentItem> BuildCardFromBoxAsync(string id, string fallbackName, CexBox? box)
    {
        var card = new PageContentItem
        {
            Title = "🎮 " + (box?.BoxName ?? fallbackName),
            ImageUrl = box?.ImageUrl,
            Subtitle = box?.SellPrice.FormatPrice(),
            Ticker = await BuildTickerAsync(box),
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

        var localStores = await GetLocalStoresForBoxAsync(box);

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
            StoresInStock = [],
            LastRefreshed = DateTime.UtcNow,
            HasError = true
        };
    }

    // ------------------------------------------------------------
    // TICKER BUILDER (ASYNC)
    // ------------------------------------------------------------
    private async Task<string> BuildTickerAsync(CexBox? box)
    {
        if (box == null || box.HasError)
            return "<span class='text-muted'>Out of Stock</span>";

        await EnsureStoresLoadedAsync();

        var stores = box.StoresInStock ?? [];

        var priority = new List<string>();
        int otherCount = 0;

        foreach (var store in stores)
        {
            var clean = CleanStoreName(store);

            if (_priorityStores.Any(ps =>
                clean.Equals(ps.Name, StringComparison.OrdinalIgnoreCase)))
            {
                priority.Add(clean);
            }
            else
            {
                otherCount++;
            }
        }

        var parts = new List<string>();

        if (box.IsAvailableOnline)
            parts.Add("Online");

        foreach (var store in priority)
        {
            parts.Add(
                $"<span class='status-badge' style='background-color:#d4edda; color:#155724;'>{store}</span>"
            );
        }

        if (otherCount == 1)
            parts.Add("and 1 other store");
        else if (otherCount > 1)
            parts.Add($"and {otherCount} other stores");

        if (!box.IsAvailableOnline && priority.Count == 0 && otherCount == 0)
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
        await EnsureStoresLoadedAsync();

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
        await EnsureStoresLoadedAsync();

        return await _reportHandler.SendFullReportAsync(
            vault,
            ReadCache(),
            hasLocalStock: HasLocalStockAsync,
            buildStockSummary: BuildTickerAsync
        );
    }

    // ------------------------------------------------------------
    // EMAIL REPORT – UPDATED (DELEGATED TO HANDLER)
    // ------------------------------------------------------------
    public async Task<string> SendUpdatedReportAsync()
    {
        await EnsureStoresLoadedAsync();

        return await _reportHandler.SendUpdatedReportAsync(
            vault,
            ReadCache(),
            getLocalStoresForBox: GetLocalStoresForBoxAsync,
            buildStockSummary: BuildTickerAsync
        );
    }

    // ------------------------------------------------------------
    // LOCAL STORE FILTERING (UNIQUE TO CexService)
    // ------------------------------------------------------------
    private async Task<bool> HasLocalStockAsync(CexBox box)
    {
        var local = await GetLocalStoresForBoxAsync(box);
        return local.Count != 0;
    }

    private async Task<List<string>> GetLocalStoresForBoxAsync(CexBox box)
    {
        await EnsureStoresLoadedAsync();

        var stores = box.StoresInStock ?? [];

        return stores
            .Select(CleanStoreName)
            .Where(clean =>
                _priorityStores.Any(ps =>
                    clean.Equals(ps.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
