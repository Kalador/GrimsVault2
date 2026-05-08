using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vault.Features.Cex
{
    // ============================================================
    // API MODELS
    // ============================================================

    /// <summary>
    /// Strongly typed models for CeX API responses.
    /// Mirrors the JSON structure returned by the v3 endpoints.
    /// </summary>
    public static class CexApiModels
    {
        // ---------------- DETAIL ----------------

        /// <summary>
        /// Root wrapper for the detail endpoint.
        /// </summary>
        public class CexDetailResponse
        {
            [JsonPropertyName("response")]
            public CexDetailEnvelope? Response { get; set; }
        }

        /// <summary>
        /// Envelope containing the detail data section.
        /// </summary>
        public class CexDetailEnvelope
        {
            [JsonPropertyName("data")]
            public CexDetailDataWrapper? Data { get; set; }
        }

        /// <summary>
        /// Wrapper containing the list of box detail entries.
        /// </summary>
        public class CexDetailDataWrapper
        {
            [JsonPropertyName("boxDetails")]
            public List<CexDetailData>? BoxDetails { get; set; }
        }

        /// <summary>
        /// Detail information for a single CeX box.
        /// </summary>
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

        /// <summary>
        /// Image URL variants for a box.
        /// </summary>
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

        /// <summary>
        /// Root wrapper for the stock endpoint.
        /// </summary>
        public class CexStockResponse
        {
            [JsonPropertyName("response")]
            public CexStockEnvelope? Response { get; set; }
        }

        /// <summary>
        /// Envelope containing the stock data section.
        /// </summary>
        public class CexStockEnvelope
        {
            [JsonPropertyName("data")]
            public CexStockData? Data { get; set; }
        }

        /// <summary>
        /// Wrapper containing store-level stock entries.
        /// </summary>
        public class CexStockData
        {
            [JsonPropertyName("stockDetails")]
            public List<CexStockStore> StockDetails { get; set; } = [];
        }

        /// <summary>
        /// Stock information for a single store.
        /// Quantity is provided as a string and parsed into an integer.
        /// </summary>
        public class CexStockStore
        {
            [JsonPropertyName("storeName")]
            public string StoreName { get; set; } = string.Empty;

            [JsonPropertyName("quantityOnHand")]
            public string QuantityOnHand { get; set; } = "0";

            public int Quantity => int.TryParse(QuantityOnHand, out var q) ? q : 0;
        }
    }

    // ============================================================
    // API CLIENT
    // ============================================================

    /// <summary>
    /// Lightweight client for CeX v3 API endpoints.
    /// Handles JSON deserialization and URL construction.
    /// </summary>
    public sealed class CexApiClient
    {
        private static readonly HttpClient Http = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Fetches detail information for a given box ID.
        /// </summary>
        public Task<CexApiModels.CexDetailResponse?> GetDetailAsync(string boxId, CancellationToken ct = default)
        {
            var url = $"https://wss2.cex.uk.webuy.io/v3/boxes/{boxId}/detail";
            return Http.GetFromJsonAsync<CexApiModels.CexDetailResponse>(url, JsonOptions, ct);
        }

        /// <summary>
        /// Fetches store-level stock information for a given box ID.
        /// </summary>
        public Task<CexApiModels.CexStockResponse?> GetStockAsync(string boxId, CancellationToken ct = default)
        {
            var url = $"https://wss2.cex.uk.webuy.io/v3/boxes/{boxId}/stock";
            return Http.GetFromJsonAsync<CexApiModels.CexStockResponse>(url, JsonOptions, ct);
        }
    }
}
