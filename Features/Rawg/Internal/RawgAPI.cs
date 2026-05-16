using System.Text.Json;
using System.Text.Json.Serialization;
using static Vault.Features.Rawg.RawgApiModels;

namespace Vault.Features.Rawg;

/// <summary>
/// DTOs representing RAWG API responses.
/// These classes mirror the JSON structure returned by api.rawg.io.
/// </summary>
public static class RawgApiModels
{
    /// <summary>
    /// Wrapper for RAWG search results.
    /// RAWG returns a "results" array for search queries.
    /// </summary>
    public sealed class RawgSearchResponse
    {
        [JsonPropertyName("results")]
        public List<RawgGame>? Results { get; set; }
    }

    /// <summary>
    /// Represents a single game returned by RAWG.
    /// Used for both search results and full game detail lookups.
    /// </summary>
    public class RawgGame
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("released")]
        public string? Released { get; set; }

        [JsonPropertyName("publishers")]
        public List<RawgEntity>? PublisherEntities { get; set; }

        [JsonPropertyName("developers")]
        public List<RawgEntity>? DeveloperEntities { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("description_raw")]
        public string? DescriptionRaw { get; set; }

        [JsonPropertyName("background_image")]
        public string? BackgroundImage { get; set; }

        [JsonPropertyName("short_screenshots")]
        public List<RawgScreenshot>? ShortScreenshots { get; set; }

        [JsonPropertyName("platforms")]
        public List<PlatformWrapper>? PlatformWrappers { get; set; }

        /// <summary>
        /// RAWG wraps platform info inside a "platform" object.
        /// </summary>
        public class PlatformWrapper
        {
            [JsonPropertyName("platform")]
            public Platform? Platform { get; set; }
        }

        /// <summary>
        /// Represents a single platform (e.g., "PC", "PlayStation 2").
        /// </summary>
        public class Platform
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        /// <summary>
        /// Represents a screenshot entry from RAWG.
        /// </summary>
        public class RawgScreenshot
        {
            [JsonPropertyName("image")]
            public string? ImageUrl { get; set; }
        }
    }

    /// <summary>
    /// RAWG uses the same structure for publishers, developers, etc.
    /// This class represents any simple "name" entity.
    /// </summary>
    public class RawgEntity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}

// ============================================================
// RAWG API CLIENT
// ============================================================

/// <summary>
/// Lightweight client for RAWG API endpoints.
/// Handles URL construction, HTTP calls, and JSON deserialization.
/// This class should remain transport-only: no business logic.
/// </summary>
public sealed class RawgApiClient(HttpClient http, IConfiguration config)
{
    /// <summary>
    /// RAWG API key pulled from configuration.
    /// </summary>
    private readonly string _rawgAPIKey = config["ApiKeys:RawgApiKey"]!;

    /// <summary>
    /// Shared JSON options for RAWG deserialization.
    /// RAWG uses lowercase snake_case; case-insensitive matching is required.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Performs a RAWG search query.
    /// Returns a list of RawgGame objects (unwrapped from the RAWG response).
    /// </summary>
    /// <param name="name">Game name to search for.</param>
    /// <param name="platformID">Optional RAWG platform ID for filtering.</param>
    public async Task<List<RawgGame>> SearchRawgAsync(string name, string? platformID)
    {
        if (string.IsNullOrWhiteSpace(name))
            return [];

        var query = Uri.EscapeDataString(name.Normalize());
        var url = $"https://api.rawg.io/api/games?search={query}&key={_rawgAPIKey}";

        if (!string.IsNullOrWhiteSpace(platformID))
            url += $"&platforms={platformID}";

        try
        {
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<RawgSearchResponse>(json, JsonOptions);

            return dto?.Results ?? [];
        }
        catch
        {
            // RAWG occasionally rate-limits or returns malformed data.
            // Fail gracefully and return an empty list.
            return [];
        }
    }

    /// <summary>
    /// Fetches a full RAWG game record by numeric ID.
    /// RAWG returns a single RawgGame object for this endpoint.
    /// </summary>
    public async Task<RawgGame?> GetGameByIdAsync(int rawgId)
    {
        var url = $"https://api.rawg.io/api/games/{rawgId}?key={_rawgAPIKey}";

        try
        {
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RawgGame>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fetches a full RAWG game record by slug.
    /// Useful when RAWG search returns a slug instead of an ID.
    /// </summary>
    public async Task<RawgGame?> GetGameBySlugAsync(string slug)
    {
        var url = $"https://api.rawg.io/api/games/{slug}?key={_rawgAPIKey}";

        try
        {
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RawgGame>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
