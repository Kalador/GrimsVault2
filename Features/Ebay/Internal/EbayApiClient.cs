using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;

namespace Vault.Features.Ebay;

// ============================================================================
//  API CLIENT
//  Performs authenticated requests to the eBay Browse API.
// ============================================================================
public sealed class EbayApiClient
{
    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EbayApiClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _clientId = config["ApiKeys:EbayClientId"]!;
        _clientSecret = config["ApiKeys:EbayClientSecret"]!;
    }

    /// <summary>
    /// Searches eBay for listings matching the supplied query.
    /// </summary>
    public async Task<List<EbayApiModels.ItemSummary>> SearchAsync(string query)
    {
        var encoded = Uri.EscapeDataString(query);
        var url = $"https://api.ebay.com/buy/browse/v1/item_summary/search?q={encoded}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-EBAY-C-MARKETPLACE-ID", "EBAY_GB");
        req.Headers.Authorization = await EbayOAuth.GetAuthHeaderAsync(_http, _clientId, _clientSecret);

        var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode)
            return [];

        var json = await res.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<EbayApiModels.ItemSummaryResponse>(json, JsonOptions);

        return dto?.Items ?? [];
    }

    /// <summary>
    /// Retrieves full item detail, including auction bid price when present.
    /// </summary>
    public async Task<EbayApiModels.ItemDetail?> GetItemAsync(string itemId)
    {
        var url = $"https://api.ebay.com/buy/browse/v1/item/{itemId}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-EBAY-C-MARKETPLACE-ID", "EBAY_GB");
        req.Headers.Authorization = await EbayOAuth.GetAuthHeaderAsync(_http, _clientId, _clientSecret);

        var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode)
            return null;

        var json = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EbayApiModels.ItemDetail>(json, JsonOptions);
    }
}


// ============================================================================
//  OAUTH HELPER
//  Handles client‑credentials authentication and token caching.
// ============================================================================
public static class EbayOAuth
{
    private static string? _cachedToken;
    private static DateTime _tokenExpiryUtc;

    /// <summary>
    /// Returns a valid Bearer token for authenticated eBay API requests.
    /// </summary>
    public static async Task<AuthenticationHeaderValue> GetAuthHeaderAsync(
        HttpClient http,
        string clientId,
        string clientSecret)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiryUtc)
            return new AuthenticationHeaderValue("Bearer", _cachedToken);

        var authBytes = Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}");
        var authHeader = Convert.ToBase64String(authBytes);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.ebay.com/identity/v1/oauth2/token"
        );

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = "https://api.ebay.com/oauth/api_scope"
        });

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<TokenResponse>(json)!;

        _cachedToken = dto.access_token;
        _tokenExpiryUtc = DateTime.UtcNow.AddSeconds(dto.expires_in - 60);

        return new AuthenticationHeaderValue("Bearer", _cachedToken);
    }

    private sealed class TokenResponse
    {
        public string access_token { get; set; } = "";
        public int expires_in { get; set; }
    }
}


// ============================================================================
//  DTOs
//  Represent the JSON returned by the eBay Browse API.
// ============================================================================
public static class EbayApiModels
{
    // ------------------------------------------------------------------------
    // SEARCH RESPONSE
    // ------------------------------------------------------------------------
    public sealed class ItemSummaryResponse
    {
        [JsonPropertyName("itemSummaries")]
        public List<ItemSummary>? Items { get; set; }
    }

    public sealed class ItemSummary
    {
        [JsonPropertyName("itemId")]
        public string? ItemId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("image")]
        public ImageInfo? Image { get; set; }

        [JsonPropertyName("price")]
        public PriceInfo? Price { get; set; }

        [JsonPropertyName("itemWebUrl")]
        public string? Url { get; set; }

        [JsonPropertyName("condition")]
        public string? Condition { get; set; }

        [JsonPropertyName("buyingOptions")]
        public List<string>? BuyingOptions { get; set; }

        [JsonPropertyName("itemEndDate")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("itemLocation")]
        public LocationInfo? Location { get; set; }

        public sealed class ImageInfo
        {
            [JsonPropertyName("imageUrl")]
            public string? Url { get; set; }
        }

        public sealed class PriceInfo
        {
            [JsonPropertyName("value")]
            public string? Value { get; set; }

            [JsonPropertyName("currency")]
            public string? Currency { get; set; }
        }

        public sealed class LocationInfo
        {
            [JsonPropertyName("country")]
            public string? Country { get; set; }
        }
    }

    // ------------------------------------------------------------------------
    // FULL ITEM DETAIL (AUCTION BID PRICE)
    // ------------------------------------------------------------------------
    public sealed class ItemDetail
    {
        [JsonPropertyName("itemId")]
        public string? ItemId { get; set; }

        [JsonPropertyName("currentBidPrice")]
        public PriceInfo? CurrentBid { get; set; }

        public sealed class PriceInfo
        {
            [JsonPropertyName("value")]
            public string? Value { get; set; }

            [JsonPropertyName("currency")]
            public string? Currency { get; set; }
        }
    }
}
