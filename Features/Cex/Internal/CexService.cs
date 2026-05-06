using System.Text.Json.Serialization;
using Vault.Core;
using Vault.Core.Internal;
using static Vault.Features.Cex.CexApiModels;

namespace Vault.Features.Cex;

public class CexService(VaultService vault) : VaultFeatureBase, IVaultFeature
{
    private static string CacheFile => "cex_cache.json";

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

    // ------------------------------------------------------------
    // SINGLE ITEM REFRESH
    // ------------------------------------------------------------
    private async Task<CexBox?> RefreshSingleAsync(string id)
    {
        return await TryAsync(async () =>
        {
            var detail = await GetJsonAsync<CexDetailResponse>(
                $"https://wss2.cex.uk.webuy.io/v3/boxes/{id}/detail"
            );

            var d = detail?.Response?.Data?.BoxDetails?.FirstOrDefault();
            if (d == null)
                return BuildErrorBox(id, "No detail data");

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

            if (d.OutOfStock == 1)
            {
                UpdateCache(id, box);
                return box;
            }

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
    // CACHE UPDATE
    // ------------------------------------------------------------
    private void UpdateCache(string id, CexBox box)
    {
        var cache = vault.Cache
            .ReadCache<Dictionary<string, CexBox>>(CacheFile)
            ?? new Dictionary<string, CexBox>();

        cache[id] = box;
        vault.Cache.WriteCache(CacheFile, cache);
    }

    // ------------------------------------------------------------
    // ERROR BOX
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
    // CACHE HELPERS FOR CRON
    // ------------------------------------------------------------
    private Dictionary<string, CexBox> ReadCache()
    {
        return vault.Cache
            .ReadCache<Dictionary<string, CexBox>>(CacheFile)
            ?? new Dictionary<string, CexBox>();
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
    // EMAIL REPORT – FULL (ALL LOCAL STOCK)
    // ------------------------------------------------------------
    public async Task<string> SendFullReportAsync()
    {
        var cache = ReadCache();

        var boxes = cache.Values
            .Where(HasLocalStock)
            .OrderByDescending(b => b.LastRefreshed)
            .ToList();

        if (!boxes.Any())
            return "No local stock";

        var emailItems = boxes
            .Select(MapToEmailItem)
            .ToList();

        var html = EmailBuilder.BuildDailyCexHtmlEmail(emailItems, "CeX stock report");
        var subject = EmailBuilder.BuildDailyCexHtmlSubject() + " (FULL)";

        await vault.Email.SendHtmlEmailAsync(subject, html);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        return
            $"CeX full report at {timestamp} UTC\n" +
            $"Items: {boxes.Count}";
    }

    // ------------------------------------------------------------
    // EMAIL REPORT – UPDATED (CHANGES ONLY)
    // ------------------------------------------------------------
    public async Task<string> SendUpdatedReportAsync()
    {
        var cache = ReadCache();

        int processed = 0;
        int included = 0;
        var lines = new List<string>();
        var changedBoxes = new List<CexBox>();

        foreach (var kvp in cache)
        {
            var id = kvp.Key;
            var box = kvp.Value;

            processed++;

            var localStores = GetLocalStoresForBox(box);
            if (!localStores.Any())
            {
                lines.Add($"{id}: SKIP (no local stock)");
                continue;
            }

            if (ShouldReportChange(box, localStores))
            {
                included++;
                lines.Add($"{id}: CHANGED");

                UpdateReportSnapshot(id, box, localStores);
                changedBoxes.Add(box);
            }
            else
            {
                lines.Add($"{id}: NO CHANGE");
            }
        }

        if (!changedBoxes.Any())
            return "No changes";

        var emailItems = changedBoxes
            .OrderByDescending(b => b.LastRefreshed)
            .Select(MapToEmailItem)
            .ToList();

        var html = EmailBuilder.BuildDailyCexHtmlEmail(emailItems, "");
        var subject = EmailBuilder.BuildDailyCexHtmlSubject();

        await vault.Email.SendHtmlEmailAsync(subject, html);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        return
            $"CeX updated report at {timestamp} UTC\n" +
            $"Processed: {processed}\n" +
            $"Included: {included}\n" +
            $"Items:\n" +
            string.Join("\n", lines);
    }

    // ------------------------------------------------------------
    // EMAIL REPORT HELPERS
    // ------------------------------------------------------------
    private IEnumerable<CexEmailItem> BuildEmailItemsFromCache()
    {
        var cache = ReadCache();

        return cache.Values
            .Where(HasLocalStock)
            .OrderByDescending(b => b.LastRefreshed)
            .Select(MapToEmailItem);
    }

    private CexEmailItem MapToEmailItem(CexBox box)
    {
        return new CexEmailItem
        {
            BoxName = box.BoxName ?? "(no name)",
            SellPriceFormatted = FormatPrice(box.SellPrice),
            StockSummaryHtml = BuildStockSummaryHtml(box),
            LastScanAge = FormatAge(box.LastRefreshed),
            CexUrl = BuildCexUrl(box.BoxId),
            ImageUrl = box.ImageUrl
        };
    }

    private static string FormatPrice(decimal price)
    {
        return price % 1 == 0
            ? price.ToString("0")
            : price.ToString("0.00");
    }

    private static string FormatAge(DateTime timestamp)
    {
        if (timestamp == DateTime.MinValue)
            return "never";

        var diff = DateTime.UtcNow - timestamp.ToUniversalTime();

        if (diff.TotalSeconds < 60)
            return "just now";

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} mins";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} hrs";

        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} days";

        return timestamp.ToLocalTime().ToString("dd/MM/yy");
    }

    private string BuildStockSummaryHtml(CexBox box)
    {
        if (box.HasError)
            return "<span class='text-muted'>Error loading stock</span>";

        var localStores = GetLocalStoresForBox(box);

        if (box.StockOnline == 0 && !localStores.Any())
            return "<span class='text-muted'>No stock</span>";

        var parts = new List<string>();

        if (box.StockOnline > 0)
            parts.Add($"Online: {box.StockOnline}");

        if (localStores.Any())
            parts.Add(string.Join(", ", localStores));

        return string.Join(" • ", parts);
    }

    private static string BuildCexUrl(string? boxId)
    {
        if (string.IsNullOrWhiteSpace(boxId))
            return "https://uk.webuy.com";

        return $"https://uk.webuy.com/product-detail?id={boxId}";
    }

    private bool HasLocalStock(CexBox box)
    {
        return GetLocalStoresForBox(box).Any();
    }

    private List<string> GetLocalStoresForBox(CexBox box)
    {
        var stores = box.StoresInStock ?? new List<string>();

        return stores
            .Where(store =>
                priorityStores.Any(p =>
                    store.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private bool ShouldReportChange(CexBox box, List<string> currentLocalStores)
    {
        var firstTime = box.LastReportedAt == null;

        var priceChanged =
            box.LastReportedPrice == null ||
            box.LastReportedPrice.Value != box.SellPrice;

        var previousStores = box.LastReportedLocalStores ?? new List<string>();

        var storesChanged = !NormalizeStores(previousStores)
            .SequenceEqual(NormalizeStores(currentLocalStores));

        var previouslyHadStock = previousStores.Any();
        var currentlyHasStock = currentLocalStores.Any();

        var regainedStock = !previouslyHadStock && currentlyHasStock;
        var lostStock = previouslyHadStock && !currentlyHasStock;

        return firstTime
               || priceChanged
               || storesChanged
               || regainedStock
               || lostStock;
    }

    private static IEnumerable<string> NormalizeStores(IEnumerable<string> stores)
    {
        return stores
            .Select(s => s.Trim().ToLowerInvariant())
            .OrderBy(s => s);
    }

    private void UpdateReportSnapshot(string id, CexBox box, List<string> localStores)
    {
        box.LastReportedPrice = box.SellPrice;
        box.LastReportedLocalStores = localStores;
        box.LastReportedAt = DateTime.UtcNow;

        UpdateCache(id, box);
    }
}
