using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vault.Features.Cex
{
    // ============================================================
    // API MODELS
    // ============================================================
    public static class CexApiModels
    {
        // ---------------- DETAIL ----------------
        public class CexDetailResponse
        {
            [JsonPropertyName("response")]
            public CexDetailEnvelope? Response { get; set; }
        }

        public class CexDetailEnvelope
        {
            [JsonPropertyName("data")]
            public CexDetailDataWrapper? Data { get; set; }
        }

        public class CexDetailDataWrapper
        {
            [JsonPropertyName("boxDetails")]
            public List<CexDetailData>? BoxDetails { get; set; }
        }

        public class CexDetailData
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

        public class CexImageUrls
        {
            [JsonPropertyName("large")]
            public string? Large { get; set; }

            [JsonPropertyName("medium")]
            public string? Medium { get; set; }

            [JsonPropertyName("small")]
            public string? Small { get; set; }
        }

        // ---------------- STOCK ----------------
        public class CexStockResponse
        {
            [JsonPropertyName("response")]
            public CexStockEnvelope? Response { get; set; }
        }

        public class CexStockEnvelope
        {
            [JsonPropertyName("data")]
            public CexStockData? Data { get; set; }
        }

        public class CexStockData
        {
            [JsonPropertyName("stockDetails")]
            public List<CexStockStore> StockDetails { get; set; } = new();
        }

        public class CexStockStore
        {
            [JsonPropertyName("storeName")]
            public string StoreName { get; set; } = "";

            [JsonPropertyName("quantityOnHand")]
            public string QuantityOnHand { get; set; } = "0";

            public int Quantity => int.TryParse(QuantityOnHand, out var q) ? q : 0;
        }
    }

    // ============================================================
    // API CLIENT
    // ============================================================
    public sealed class CexApiClient
    {
        private static readonly HttpClient Http = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public Task<CexApiModels.CexDetailResponse?> GetDetailAsync(string boxId, CancellationToken ct = default)
        {
            var url = $"https://wss2.cex.uk.webuy.io/v3/boxes/{boxId}/detail";
            return Http.GetFromJsonAsync<CexApiModels.CexDetailResponse>(url, JsonOptions, ct);
        }

        public Task<CexApiModels.CexStockResponse?> GetStockAsync(string boxId, CancellationToken ct = default)
        {
            var url = $"https://wss2.cex.uk.webuy.io/v3/boxes/{boxId}/stock";
            return Http.GetFromJsonAsync<CexApiModels.CexStockResponse>(url, JsonOptions, ct);
        }
    }
}
