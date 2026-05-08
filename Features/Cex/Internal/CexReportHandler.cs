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
        Func<CexBox, Task<bool>> hasLocalStock,
        Func<CexBox, Task<string>> buildStockSummary
    )
    {
        var boxes = new List<CexBox>();

        foreach (var box in cache.Values)
        {
            if (await hasLocalStock(box))
                boxes.Add(box);
        }

        boxes = boxes
            .OrderByDescending(b => b.LastRefreshed)
            .ToList();

        if (!boxes.Any())
            return "No local stock";

        var emailItems = new List<CexEmailItem>();

        foreach (var box in boxes)
        {
            emailItems.Add(MapToEmailItem(box, await buildStockSummary(box)));
        }

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
        Func<CexBox, Task<List<string>>> getLocalStoresForBox,
        Func<CexBox, Task<string>> buildStockSummary
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

            var localStores = await getLocalStoresForBox(box);
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

        var emailItems = new List<CexEmailItem>();

        foreach (var box in changedBoxes
            .OrderByDescending(b => b.LastRefreshed))
        {
            emailItems.Add(MapToEmailItem(box, await buildStockSummary(box)));
        }

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
    private CexEmailItem MapToEmailItem(CexBox box, string stockSummaryHtml)
    {
        return new CexEmailItem
        {
            BoxName = box.BoxName ?? "(no name)",
            SellPriceFormatted = box.SellPrice.FormatPrice(),
            StockSummaryHtml = stockSummaryHtml,
            LastScanAge = box.LastRefreshed.FormatAge(),
            CexUrl = CexHelpers.BuildCexUrl(box.BoxId),
            ImageUrl = box.ImageUrl
        };
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

        var previousStores = box.LastReportedLocalStores ?? [];

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
