using static Vault.Features.Cex.CexApiModels;

namespace Vault.Features.Cex;

/// <summary>
/// Class represents a single "Box" item from the CEX API. Each Box represents a single purchasable item
/// </summary>
public class CexBox
{
    public string? BoxId { get; set; }
    public string? BoxName { get; set; }
    public decimal CashPrice { get; set; }
    public decimal ExchangePrice { get; set; }
    public decimal SellPrice { get; set; }
    public int StockOnline { get; set; }
    public int StockInStore { get; set; }
    public string? ImageUrl { get; set; }

    public bool HasError { get; set; } = false;

    // Unified timestamp for rolling refresh
    public DateTime LastRefreshed { get; set; } = DateTime.MinValue;

    public List<string> StoresInStock { get; set; } = new();

    // Last price that was included in a report
    public decimal? LastReportedPrice { get; set; }

    // Last set of local stores that were included in a report
    public List<string> LastReportedLocalStores { get; set; } = new();

    // When this item was last included in a report
    public DateTime? LastReportedAt { get; set; }
}

internal static class CexBoxMapper
{
    public static CexBox FromDetail(string id, CexDetailData d)
    {
        return new CexBox
        {
            BoxId = id,
            BoxName = d.BoxName,
            ImageUrl = d.ImageUrls?.Large,
            SellPrice = d.SellPrice,
            CashPrice = d.CashPrice,
            ExchangePrice = d.ExchangePrice,
            StockOnline = d.EcomQuantityOnHand,

            // Only meaningful non-default assignment
            LastRefreshed = DateTime.UtcNow
        };
    }

    public static void ApplyStock(CexBox box, IEnumerable<CexStockStore> stockDetails)
    {
        box.StockInStore = stockDetails.Any(x => x.Quantity > 0) ? 1 : 0;

        box.StoresInStock = stockDetails
            .Where(x => x.Quantity > 0)
            .Select(x => x.StoreName)
            .ToList();
    }
}
