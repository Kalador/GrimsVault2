using Vault.Core;

namespace Vault.Features.Cex;

public class CexReportHandler
{
    // ------------------------------------------------------------
    // FULL REPORT
    // ------------------------------------------------------------
    public async Task<string> SendFullReportAsync(
        VaultService vault,
        Dictionary<string, CexBox> cache,
        Func<CexBox, bool> hasLocalStock
    )
    {
        var boxes = cache.Values
            .Where(hasLocalStock)
            .OrderByDescending(b => b.LastRefreshed)
            .ToList();

        if (!boxes.Any())
            return "No local stock";

        var emailItems = boxes
            .Select(MapToEmailItem)
            .ToList();

        var html = CexEmailBuilder.BuildDailyCexHtmlEmail(emailItems, "CeX stock report");
        var subject = CexEmailBuilder.BuildDailyCexHtmlSubject() + " (FULL)";

        await vault.Email.SendHtmlEmailAsync(subject, html);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        return
            $"CeX full report at {timestamp} UTC\n" +
            $"Items: {boxes.Count}";
    }

    // ------------------------------------------------------------
    // UPDATED REPORT (CHANGES ONLY)
    // ------------------------------------------------------------
    public async Task<string> SendUpdatedReportAsync(
        VaultService vault,
        Dictionary<string, CexBox> cache,
        Func<CexBox, List<string>> getLocalStoresForBox
    )
    {
        int processed = 0;
        int included = 0;
        var lines = new List<string>();
        var changedBoxes = new List<CexBox>();

        foreach (var kvp in cache)
        {
            var id = kvp.Key;
            var box = kvp.Value;

            processed++;

            var localStores = getLocalStoresForBox(box);
            if (!localStores.Any())
            {
                lines.Add($"{id}: SKIP (no local stock)");
                continue;
            }

            if (ShouldReportChange(box, localStores))
            {
                included++;
                lines.Add($"{id}: CHANGED");

                UpdateReportSnapshot(box, localStores);
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

        var html = CexEmailBuilder.BuildDailyCexHtmlEmail(emailItems, "");
        var subject = CexEmailBuilder.BuildDailyCexHtmlSubject();

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
    // EMAIL MAPPING
    // ------------------------------------------------------------
    private CexEmailItem MapToEmailItem(CexBox box)
    {
        return new CexEmailItem
        {
            BoxName = box.BoxName ?? "(no name)",
            SellPriceFormatted = box.SellPrice.FormatPrice(),
            StockSummaryHtml = BuildStockSummaryHtml(box),
            LastScanAge = box.LastRefreshed.FormatAge(),
            CexUrl = CexHelpers.BuildCexUrl(box.BoxId),
            ImageUrl = box.ImageUrl
        };
    }

    private static string BuildStockSummaryHtml(CexBox box)
    {
        if (box.HasError)
            return "<span class='text-muted'>Error loading stock</span>";

        var hasOnlineStock = box.StockOnline > 0;
        var hasStoreStock = box.StoresInStock?.Any() == true;

        if (!hasOnlineStock && !hasStoreStock)
            return "<span class='text-muted'>No stock</span>";

        var parts = new List<string>();

        if (box.StockOnline > 0)
            parts.Add($"Online: {box.StockOnline}");

        if (box.StoresInStock?.Any() == true)
            parts.Add(string.Join(", ", box.StoresInStock));

        return string.Join(" • ", parts);
    }

    // ------------------------------------------------------------
    // CHANGE DETECTION
    // ------------------------------------------------------------
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

    private void UpdateReportSnapshot(CexBox box, List<string> localStores)
    {
        box.LastReportedPrice = box.SellPrice;
        box.LastReportedLocalStores = localStores;
        box.LastReportedAt = DateTime.UtcNow;
    }
}