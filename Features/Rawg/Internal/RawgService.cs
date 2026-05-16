using Vault.Core;
using Vault.Core.Internal;

namespace Vault.Features.Rawg;

/// <summary>
/// RAWG feature service.
/// Handles searching RAWG, applying RAWG metadata to VaultItems,
/// and producing generic page content for ViewModes.
/// </summary>
public sealed class RawgService(IConfiguration config, VaultService vault) : IVaultFeature
{
    // ------------------------------------------------------------
    // Internal API client (self-contained for drop-in architecture)
    // ------------------------------------------------------------
    private readonly RawgApiClient _api = new(new HttpClient(), config);

    // ------------------------------------------------------------
    // Feature metadata
    // ------------------------------------------------------------
    private sealed class Meta : IVaultFeatureMetaData
    {
        public string Name => "RAWG Search and Scrape";
        public string Route => "/rawg";
    }

    public IVaultFeatureMetaData MetaData { get; } = new Meta();
    public string Status() => "RAWG Feature OK";
    public VaultViewMode DefaultViewMode => ColumnsViewMode.Instance;

    // ------------------------------------------------------------
    // Public API – Page content (Recent Scrapes)
    // ------------------------------------------------------------

    /// <summary>
    /// Builds a page model showing the most recently scraped Vault items.
    /// The result is a generic <see cref="PageContentModel"/> that can be
    /// rendered by any ViewMode (e.g., Columns).
    /// </summary>
    public async Task<PageContentModel> GetRecentScrapesAsync(int pageSize = 30)
    {
        var items = await vault.GetItems(new QueryDefinition
        {
            Filters =
            {
                new QueryFilter
                {
                    Property = "Scraped",
                    Operator = QueryOperator.NotEmpty
                }
            },
            Sorts =
            {
                new QuerySort
                {
                    Property = "Scraped",
                    Direction = QuerySortDirection.Desc
                }
            }
        }, pageSize: pageSize);

        var model = new PageContentModel();

        foreach (var item in items)
            model.Items.Add(BuildRecentScrapeCard(item));

        return model;
    }

    // ------------------------------------------------------------
    // Public API – RAWG search / match
    // ------------------------------------------------------------

    /// <summary>
    /// Searches RAWG for possible matches for a given VaultItem.
    /// Returns a clean list of RawgGame objects for the controller/UI.
    /// </summary>
    public async Task<List<RawgApiModels.RawgGame>> SearchAsync(VaultItem item)
    {
        return await _api.SearchRawgAsync(item.Name ?? string.Empty, item.PlatformId);
    }

    /// <summary>
    /// Fetches a RAWG game by its numeric ID.
    /// Used when loading an existing match.
    /// </summary>
    public async Task<RawgApiModels.RawgGame?> SearchByIdAsync(int rawgId)
    {
        return await _api.GetGameByIdAsync(rawgId);
    }

    /// <summary>
    /// Fetches a RAWG game by its slug.
    /// Useful when name search fails but a slug is known.
    /// </summary>
    public async Task<RawgApiModels.RawgGame?> SearchBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return await _api.GetGameBySlugAsync(slug);
    }

    /// <summary>
    /// Applies a RAWG match to a VaultItem.
    /// Fetches full RAWG details, maps fields, and returns the updated item.
    /// </summary>
    public async Task<VaultItem?> ApplyMatchAsync(VaultItem item, int rawgId, bool overwriteName = false)
    {
        var full = await _api.GetGameByIdAsync(rawgId);
        if (full == null)
            return null;

        return ApplyRawgMatch(item, full, overwriteName);
    }

    // ------------------------------------------------------------
    // Card builders (PageContentItem)
    // ------------------------------------------------------------

    /// <summary>
    /// Builds a card representing a recently scraped Vault item.
    /// The card is grouped under "Recently Scraped" and contains
    /// basic display information and metadata for ViewModes.
    /// </summary>
    private static PageContentItem BuildRecentScrapeCard(VaultItem item) => new()
    {
        Group = item.PlatformName ?? "Unknown Platform",

        Secondary = string.Join(" / ",
        new[] { item.Developer, item.Publisher }
            .Where(s => !string.IsNullOrWhiteSpace(s))
        ),

        Primary = item.Name ?? "(no name)",
        ImageUrl = item.Media?.FirstOrDefault(),
        SecondFooter = item.Scraped?.ToString("yyyy-MM-dd HH:mm"),

        Url = $"/rawg/detail?id={item.PageId}",

        HasError = false,

        Meta =
            {
                ["id"]       = item.PageId,
                ["rawgId"]   = item.RawgId ?? "",
                ["platform"] = item.PlatformName ?? ""
            }
    };

    // ------------------------------------------------------------
    // Mapping logic – RAWG → VaultItem
    // ------------------------------------------------------------

    /// <summary>
    /// Maps RAWG fields onto a VaultItem.
    /// This is the only part of the old service that survives into V2.
    /// </summary>
    public VaultItem ApplyRawgMatch(VaultItem item, RawgApiModels.RawgGame match, bool overwriteName)
    {
        item.RawgId = match.Id.ToString();

        if (match.Released != null)
            item.ReleaseDate = match.Released.ToDate();

        item.Publisher = match.PublisherEntities?.Count > 0
            ? string.Join(", ", match.PublisherEntities.Select(p => p.Name))
            : null;

        item.Developer = match.DeveloperEntities?.Count > 0
            ? string.Join(", ", match.DeveloperEntities.Select(d => d.Name))
            : null;

        if (!string.IsNullOrWhiteSpace(match.DescriptionRaw))
            item.Description = match.DescriptionRaw.Sanitize();

        var media = new List<string>();

        if (!string.IsNullOrWhiteSpace(match.BackgroundImage))
            media.Add(match.BackgroundImage);

        if (match.ShortScreenshots?.Count > 0)
        {
            media.AddRange(
                match.ShortScreenshots
                    .Select(s => s.ImageUrl)
                    .Where(url => !string.IsNullOrWhiteSpace(url))!
            );
        }

        item.Media = media.Count > 0
            ? [.. media.Distinct()]
            : null;

        if (overwriteName && !string.IsNullOrWhiteSpace(match.Name))
            item.Name = match.Name;

        item.ScrapeStatus = "Scraped";
        item.Scraped = DateTime.UtcNow;

        return item;
    }
}
