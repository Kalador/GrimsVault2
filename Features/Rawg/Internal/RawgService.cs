using System.Collections.Concurrent;
using Vault.Core;
using Vault.Core.Internal;
using Vault.ViewModes;

namespace Vault.Features.Rawg;

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

    // =====================================================================
    //  RECENT SCRAPES
    // =====================================================================

    public async Task<PageContentModel> GetRecentScrapesAsync(int pageSize = 32)
    {
        var items = await vault.GetItems(new QueryDefinition
        {
            Filters =
            {
                new QueryFilter { Property = "Scraped", Operator = QueryOperator.NotEmpty }
            },
            Sorts =
            {
                new QuerySort { Property = "Scraped", Direction = QuerySortDirection.Desc }
            }
        }, pageSize: pageSize);

        var model = new PageContentModel();

        foreach (var item in items)
            model.Items.Add(BuildRecentScrapeCard(item));

        return model;
    }

    // =====================================================================
    //  SCRAPE PASS
    // =====================================================================

    public async Task<PageContentModel> RunScrapeAsync()
    {
        var itemsToScrape = await vault.GetItems(new QueryDefinition
        {
            Filters =
            {
                new QueryFilter { Property = "Category", Operator = QueryOperator.Equals, Value = "Games" },
                new QueryFilter { Property = "RAWG ID", Operator = QueryOperator.Empty },
                new QueryFilter { Property = "Name", Operator = QueryOperator.NotEmpty },
                new QueryFilter { Property = "Platform ID", Operator = QueryOperator.NotEmpty }
            }
        }, 24);

        var updated = new ConcurrentBag<VaultItem>();
        var semaphore = new SemaphoreSlim(8);

        var tasks = itemsToScrape.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                var matches = await SearchAsync(item);

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

                var full = await SearchByIdAsync(match.Id);

                if (full == null)
                {
                    item.ScrapeStatus = "Error";
                    item.Scraped = DateTime.UtcNow;
                    updated.Add(item);
                    return;
                }

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

        var model = new PageContentModel();

        foreach (var item in updated)
            model.Items.Add(BuildRecentScrapeCard(item));

        return model;
    }

    // =====================================================================
    //  RAWG SEARCH HELPERS
    // =====================================================================

    public async Task<List<RawgApiModels.RawgGame>> SearchAsync(VaultItem item)
    {
        return await _api.SearchRawgAsync(item.Name ?? string.Empty, item.PlatformId);
    }

    public async Task<RawgApiModels.RawgGame?> SearchByIdAsync(int rawgId)
    {
        return await _api.GetGameByIdAsync(rawgId);
    }

    public async Task<RawgApiModels.RawgGame?> SearchBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return await _api.GetGameBySlugAsync(slug);
    }

    public async Task<VaultItem?> ApplyMatchAsync(VaultItem item, int rawgId, bool overwriteName = false)
    {
        var full = await _api.GetGameByIdAsync(rawgId);
        if (full == null)
            return null;

        return ApplyRawgMatch(item, full, overwriteName);
    }

    // =====================================================================
    //  SEARCH PAGE (Unified)
    // =====================================================================

    public async Task<PageContentModel> BuildSearchModelAsync(string pageId, string? query, bool? slug)
    {
        var item = await vault.GetItem(pageId);

        if (item == null)
        {
            return new PageContentModel
            {
                Meta = { ["Title"] = "Item Not Found" }
            };
        }

        // Determine final query
        query ??= item.Name ?? "";

        var model = new PageContentModel
        {
            Meta =
            {
                ["Title"] = $"RAWG Search: {item.Name}",
                ["Subtitle"] = $"Platform: {item.PlatformName}",
                ["Mode"] = "Search",
                ["PageId"] = item.PageId,
                ["Platform"] = item.PlatformName ?? "",
                ["Slug"] = slug?.ToString() ?? "false"
            }
        };

        // =====================================================================
        //  SEARCH CARD (Elements-based)
        // =====================================================================

        var searchCard = new PageContentItem
        {
            Group = "",
            Primary = "Search RAWG Manually",
            FormAction = "/rawg/search",
            FormMethod = "get"
        };

        searchCard.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Input,
            Name = "query",
            Value = query,
            Placeholder = "Enter alternate title..."
        });

        searchCard.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Checkbox,
            Name = "slug",
            Label = "Slug?",
            Checked = slug == true
        });

        searchCard.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Button,
            Text = "Search"
        });

        // Hidden ID field
        searchCard.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Hidden,
            Name = "id",
            Value = item.PageId
        });

        model.Items.Add(searchCard);

        // =====================================================================
        //  RAWG SEARCH LOGIC
        // =====================================================================

        if (slug == true)
        {
            var result = await _api.GetGameBySlugAsync(query);

            if (result != null)
                model.Items.Add(BuildMatchCard(item, result));

            return model;
        }

        var results = await _api.SearchRawgAsync(query, item.PlatformId);

        foreach (var r in results)
            model.Items.Add(BuildMatchCard(item, r));

        return model;
    }

    // =====================================================================
    //  APPLY MATCH → Return to Search Page
    // =====================================================================

    public async Task<PageContentModel> ApplyMatchAndBuildSearchModelAsync(
        string pageId,
        int rawgId,
        bool overwriteName)
    {
        var item = await vault.GetItem(pageId);
        if (item == null)
            return new PageContentModel
            {
                Meta = { ["Title"] = "Item Not Found" }
            };

        var updated = await ApplyMatchAsync(item, rawgId, overwriteName);
        if (updated != null)
            await vault.UpdateItem(updated);

        return await BuildSearchModelAsync(pageId, updated?.Name ?? "", slug: null);
    }

    // =====================================================================
    //  MATCH CARD (Elements-based)
    // =====================================================================

    private PageContentItem BuildMatchCard(VaultItem item, RawgApiModels.RawgGame match)
    {
        var year = match.Released;

        var platforms = match.PlatformWrappers is { Count: > 0 }
            ? string.Join(" | ",
                match.PlatformWrappers
                    .Select(p => p.Platform?.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))!)
            : null;

        var thumbnails = match.ShortScreenshots?
            .Select(s => s.ImageUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Take(5)
            .ToList();

        var card = new PageContentItem
        {
            Group = platforms ?? "",
            Primary = $"🎮 {match.Name ?? "(no name)"}",
            Ticker = year is not null ? $"📅 {year}" : null,
            SecondFooter = match.DescriptionRaw,
            ImageUrl = match.BackgroundImage
        };

        if (thumbnails?.Count > 0)
            card.Meta["thumbnails"] = string.Join(";", thumbnails);

        // Save button
        card.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Button,
            Text = "Save",
            ActionUrl = $"/rawg/apply?id={item.PageId}&rawgId={match.Id}"
        });

        // View button
        if (!string.IsNullOrWhiteSpace(match.Slug))
        {
            card.Elements.Add(new PageContentElement
            {
                Type = PageContentElementType.Button,
                Text = "View",
                ActionUrl = $"https://rawg.io/games/{match.Slug}"
            });
        }

        return card;
    }

    // =====================================================================
    //  RECENT SCRAPE CARD (NO RETRY BUTTON)
    // =====================================================================

    private static PageContentItem BuildRecentScrapeCard(VaultItem item)
    {
        var card = new PageContentItem
        {
            Group = item.PlatformName ?? "Unknown Platform",

            Secondary = string.Join(" ",
                new[]
                {
                    item.Developer is { Length: > 0 } d ? $"🛠️ {d}" : null,
                    item.Publisher is { Length: > 0 } p ? $"🏢 {p}" : null
                }.Where(s => s != null)
            ),

            Primary = $"🎮 {item.Name ?? "(no name)"}",
            ImageUrl = item.Media?.FirstOrDefault(),

            Ticker = item.ReleaseDate is not null ? $"📅 {item.ReleaseDate:yyyy}" : null,

            SecondFooter = item.Description,

            HasError = string.Equals(item.ScrapeStatus, "Error", StringComparison.OrdinalIgnoreCase)
        };

        // If error → whole card links to search page
        if (card.HasError)
        {
            card.Url = $"/rawg/search?id={item.PageId}&query={Uri.EscapeDataString(item.Name ?? "")}";
        }

        return card;
    }

    // =====================================================================
    //  RAWG → VAULT MAPPING
    // =====================================================================

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
