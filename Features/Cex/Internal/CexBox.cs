using Vault.Core;
using static Vault.Features.Cex.CexApiModels;

namespace Vault.Features.Cex;

/// <summary>
/// Domain model representing a single CeX "Box" item.
/// Tracks where the item is available, not how many are in stock.
/// </summary>
public class CexBox
{
    // Identity
    public string? BoxId { get; set; }
    public string? BoxName { get; set; }

    // Pricing
    public decimal CashPrice { get; set; }
    public decimal ExchangePrice { get; set; }
    public decimal SellPrice { get; set; }

    // Stock availability (location-based only)
    public bool IsAvailableOnline { get; set; }
    public List<string> StoresInStock { get; set; } = [];

    // Media
    public string? ImageUrl { get; set; }

    // Error state
    public bool HasError { get; set; } = false;

    // Refresh tracking
    public DateTime LastRefreshed { get; set; } = DateTime.MinValue;

    // Reporting metadata
    public decimal? LastReportedPrice { get; set; }
    public List<string> LastReportedLocalStores { get; set; } = [];
    public DateTime? LastReportedAt { get; set; }
}

/// <summary>
/// Maps API detail/stock responses into domain models.
/// </summary>
internal static class CexBoxMapper
{
    /// <summary>
    /// Creates a CexBox from a detail response.
    /// </summary>
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

            // Online availability is binary
            IsAvailableOnline = d.EcomQuantityOnHand > 0,

            LastRefreshed = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies store-level availability to an existing box.
    /// </summary>
    public static void ApplyStock(CexBox box, IEnumerable<CexStockStore> stockDetails)
    {
        box.StoresInStock =
        [
            .. stockDetails
                .Where(x => x.Quantity > 0)
                .Select(x => x.StoreName)
        ];
    }
}


