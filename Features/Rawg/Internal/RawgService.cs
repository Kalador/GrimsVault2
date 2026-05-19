using System.Collections.Concurrent;
using Vault.Core;
using Vault.Core.Internal;
using Vault.ViewModes;

namespace Vault.Features.Rawg;

/// <summary>
/// RAWG feature service. Handles RAWG search, scrape passes,
/// applying RAWG metadata to VaultItems, and producing
/// ViewMode-friendly page content.
/// </summary>
public sealed class RawgService(IConfiguration config, VaultService vault) : IVaultFeature
{
    private readonly RawgApiClient _api = new(new HttpClient(), config);

    private sealed class Meta : IVaultFeatureMetaData
    {
        public string Name => "RAWG Search and Scrape";
        public string Route => "/rawg";
    }

    public IVaultFeatureMetaData MetaData { get; } = new Meta();
    public string Status() => "RAWG Feature OK";
    public VaultViewMode DefaultViewMode => CompactListViewMode.Instance;

    // ------------------------------------------------------------
    // Recent Scrapes
    // ------------------------------------------------------------

    /// <summary>
    /// Returns a page model containing the most recently scraped items,
    /// sorted by scrape timestamp (descending).
    /// </summary>
    public async Task<PageContentModel> GetRecentScrapesAsync(int pageSize = 24)
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

    /// <summary>
    /// Runs a scrape pass against all eligible Vault items.
    /// Eligibility matches the original RAWG 1.0 behaviour:
    /// Games with no RAWG ID, and with valid Name and PlatformId.
    ///
    /// This placeholder version does not call RAWG. It applies a
    /// temporary merge step and returns the updated items as a
    /// <see cref="PageContentModel"/> for display.
    /// </summary>
    public async Task<PageContentModel> RunScrapeAsync()
    {
        // Select items eligible for scraping
        var itemsToScrape = await vault.GetItems(new QueryDefinition
        {
            Filters =
        {
            new QueryFilter
            {
                Property = "Category",
                Operator = QueryOperator.Equals,
                Value = "Games"
            },
            new QueryFilter
            {
                Property = "RAWG ID",
                Operator = QueryOperator.Empty
            },
            new QueryFilter
            {
                Property = "Name",
                Operator = QueryOperator.NotEmpty
            },
            new QueryFilter
            {
                Property = "Platform ID",
                Operator = QueryOperator.NotEmpty
            }
        }
        }, 24);

        var updated = new ConcurrentBag<VaultItem>();
        var semaphore = new SemaphoreSlim(8);

        var tasks = itemsToScrape.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                // Search RAWG for possible matches
                var matches = await SearchAsync(item);

                // Exact name match
                var match = matches.FirstOrDefault(r =>
                    string.Equals(
                        r.Name?.Normalize(),
                        item.Name?.Normalize(),
                        StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    item.ScrapeStatus = "Error";
                    item.Scraped = DateTime.UtcNow;
                    updated.Add(item);
                    return;
                }

                // Fetch full RAWG details
                var full = await SearchByIdAsync(match.Id);

                if (full == null)
                {
                    item.ScrapeStatus = "Error";
                    item.Scraped = DateTime.UtcNow;
                    updated.Add(item);
                    return;
                }

                // Apply metadata
                ApplyRawgMatch(item, full, overwriteName: false);

                updated.Add(item);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        await vault.UpdateItems(updated);

        // Build page model for display
        var model = new PageContentModel();

        foreach (var item in updated)
            model.Items.Add(BuildRecentScrapeCard(item));

        return model;
    }



    /// <summary>
    /// Temporary merge step used during early development.
    /// Replaces RAWG integration. Mutates the item in-place.
    /// </summary>
    private static VaultItem PlaceholderMerge(VaultItem item)
    {
        item.Name = "WORKS";
        return item;
    }

    
    /// <summary>
    /// Searches RAWG for possible matches for a given VaultItem.
    /// </summary>
    public async Task<List<RawgApiModels.RawgGame>> SearchAsync(VaultItem item)
    {
        return await _api.SearchRawgAsync(item.Name ?? string.Empty, item.PlatformId);
    }

    /// <summary>
    /// Fetches a RAWG game by its numeric ID.
    /// </summary>
    public async Task<RawgApiModels.RawgGame?> SearchByIdAsync(int rawgId)
    {
        return await _api.GetGameByIdAsync(rawgId);
    }

    /// <summary>
    /// Fetches a RAWG game by its slug.
    /// </summary>
    public async Task<RawgApiModels.RawgGame?> SearchBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return await _api.GetGameBySlugAsync(slug);
    }

    /// <summary>
    /// Applies a RAWG match to a VaultItem by fetching full details
    /// and mapping them onto the item.
    /// </summary>
    public async Task<VaultItem?> ApplyMatchAsync(VaultItem item, int rawgId, bool overwriteName = false)
    {
        var full = await _api.GetGameByIdAsync(rawgId);
        if (full == null)
            return null;

        return ApplyRawgMatch(item, full, overwriteName);
    }

    // ------------------------------------------------------------
    // Card Builder
    // ------------------------------------------------------------

    /// <summary>
    /// Builds a card representing a recently scraped Vault item.
    /// </summary>
    private static PageContentItem BuildRecentScrapeCard(VaultItem item) => new()
    {
        Group = item.PlatformName ?? "Unknown Platform",

        Secondary = string.Join(" / ",
            new[]
            {
                item.Developer is { Length: > 0 } d ? $"🛠️ {d}" : null,
                item.Publisher is { Length: > 0 } p ? $"🏢 {p}" : null
            }.Where(s => s != null)
        ),

        Primary = $"🎮 {item.Name ?? "(no name)"}",
        ImageUrl = item.Media?.FirstOrDefault(),

        SecondFooter =
            item.ReleaseDate is not null
                ? $"📅 {item.ReleaseDate:yyyy}"
                : item.Scraped is not null
                    ? $"📅 {item.Scraped:yyyy-MM-dd HH:mm}"
                    : null,

        Ticker = item.Description,
        Url = $"/rawg/detail?id={item.PageId}",

        HasError = string.Equals(item.ScrapeStatus, "Error", StringComparison.OrdinalIgnoreCase),

        Meta =
        {
            ["id"]     = item.PageId,
            ["rawgId"] = item.RawgId ?? "",
            ["status"] = item.ScrapeStatus ?? ""
        }
    };


    /// <summary>
    /// Maps RAWG fields onto a VaultItem.
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

        item.Media = media.Count > 0 ? [.. media.Distinct()] : null;

        if (overwriteName && !string.IsNullOrWhiteSpace(match.Name))
            item.Name = match.Name;

        item.ScrapeStatus = "Scraped";
        item.Scraped = DateTime.UtcNow;

        return item;
    }
}
