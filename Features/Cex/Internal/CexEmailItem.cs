namespace Vault.Features.Cex;

/// <summary>
/// A lightweight DTO used exclusively for generating CeX email reports.
/// This is NOT a domain model and NOT a UI model.
/// It contains only the formatted fields needed by the EmailBuilder.
/// </summary>
public class CexEmailItem
{
    public string BoxName { get; set; } = "";
    public string SellPriceFormatted { get; set; } = "";
    public string StockSummaryHtml { get; set; } = "";
    public string LastScanAge { get; set; } = "";
    public string CexUrl { get; set; } = "";
    public string? ImageUrl { get; set; }
}
