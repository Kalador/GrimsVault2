using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Vault.Data.Notion
{
    /// <summary>
    /// Low-level HTTP client for communicating with the Notion API.
    /// Handles headers, HTTP verbs, and JSON transport only.
    /// </summary>
    public class NotionClient(HttpClient http, IConfiguration config)
    {
        private readonly string _version = config["Notion:NotionVersion"]!;
        private readonly string _token = config["ApiKeys:NotionToken"]!;
        private readonly string _baseUrl = config["Notion:BaseUrl"]!;
        private readonly int _defaultPageSize = int.Parse(config["Notion:PageSize"]!);
        private readonly HttpClient _http = http;

        private static int ClampPageSize(int size)
        {
            if (size < 1) return 1;
            if (size > 100) return 100;
            return size;
        }

        private void ApplyHeaders()
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            _http.DefaultRequestHeaders.Add("Notion-Version", _version);
        }

        /// <summary>
        /// Sends a POST request to a Notion endpoint with a JSON payload.
        /// Supports optional page size.
        /// Returns the raw JSON document.
        /// </summary>
        public async Task<JsonDocument> PostAsync(string url, object payload, int? pageSize = null)
        {
            ApplyHeaders();

            // Determine final page size
            int finalPageSize = ClampPageSize(pageSize ?? _defaultPageSize);

            // Merge page_size into the payload (flat JSON)
            var dict = new Dictionary<string, object?>
            {
                ["page_size"] = finalPageSize
            };

            // Copy original payload properties into the dictionary
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload)))
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    dict[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                }
            }

            var json = JsonSerializer.Serialize(dict);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(_baseUrl + url, content);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(body);
        }

        public async Task<JsonDocument> GetAsync(string url)
        {
            ApplyHeaders();

            var response = await _http.GetAsync(_baseUrl + url);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(body);
        }

        public async Task<JsonDocument> PatchAsync(string url, object payload)
        {
            ApplyHeaders();

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PatchAsync(_baseUrl + url, content);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(body);
        }
    }
}
