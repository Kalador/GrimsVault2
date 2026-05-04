namespace Vault.Features.Cex
{
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

        public bool HasError { get; set; }

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
}
