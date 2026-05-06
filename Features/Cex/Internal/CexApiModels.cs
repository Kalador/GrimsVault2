using System.Text.Json.Serialization;

namespace Vault.Features.Cex;

/// <summary>
/// DTOs for the CeX public API (detail + stock).
/// Pure data models, no logic.
/// </summary>
internal static class CexApiModels
{
    // ------------------------------------------------------------
    // DETAIL RESPONSE
    // ------------------------------------------------------------
    internal class CexDetailResponse
    {
        [JsonPropertyName("response")]
        public CexDetailEnvelope? Response { get; set; }
    }

    internal class CexDetailEnvelope
    {
        [JsonPropertyName("data")]
        public CexDetailDataWrapper? Data { get; set; }
    }

    internal class CexDetailDataWrapper
    {
        [JsonPropertyName("boxDetails")]
        public List<CexDetailData>? BoxDetails { get; set; }
    }

    internal class CexDetailData
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

    internal class CexImageUrls
    {
        [JsonPropertyName("large")]
        public string? Large { get; set; }

        [JsonPropertyName("medium")]
        public string? Medium { get; set; }

        [JsonPropertyName("small")]
        public string? Small { get; set; }
    }

    // ------------------------------------------------------------
    // STOCK RESPONSE
    // ------------------------------------------------------------
    internal class CexStockResponse
    {
        [JsonPropertyName("response")]
        public CexStockEnvelope? Response { get; set; }
    }

    internal class CexStockEnvelope
    {
        [JsonPropertyName("data")]
        public CexStockData? Data { get; set; }
    }

    internal class CexStockData
    {
        [JsonPropertyName("stockDetails")]
        public List<CexStockStore> StockDetails { get; set; } = new();
    }

    internal class CexStockStore
    {
        [JsonPropertyName("storeName")]
        public string StoreName { get; set; } = "";

        [JsonPropertyName("quantityOnHand")]
        public string QuantityOnHand { get; set; } = "0";

        public int Quantity => int.TryParse(QuantityOnHand, out var q) ? q : 0;
    }
}
