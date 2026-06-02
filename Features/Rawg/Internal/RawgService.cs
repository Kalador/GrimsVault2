using Vault.Core;
using Vault.Core.Internal;
using Vault.ViewModes;

namespace Vault.Features.Rawg;

public sealed class RawgService(IConfiguration config, VaultService vault) : IVaultFeature
{
    private readonly RawgApiClient _api = new(new HttpClient(), config);
    private readonly RawgCardBuilder _cards = new();

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
        }, pageSize);

        var model = new PageContentModel
        {
            Meta = { ["Title"] = $"Recently Scraped: {items.Count} Games" }
        };

        model.PageButtons.Add(new PageContentButton
        {
            Text = "Scrape More",
            ActionUrl = "/rawg/scrape"
        });

        foreach (var item in items)
            model.Items.Add(_cards.BuildRecentScrapeCard(item));

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

        var updated = new List<VaultItem>();

        foreach (var item in itemsToScrape)
        {
            var matches = await _api.SearchRawgAsync(item.Name ?? string.Empty, item.PlatformId);

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
                continue;
            }

            var full = await _api.GetGameByIdAsync(match.Id);

            if (full == null)
            {
                item.ScrapeStatus = "Error";
                item.Scraped = DateTime.UtcNow;
                updated.Add(item);
                continue;
            }

            ApplyRawgMatch(item, full, overwriteName: false);
            updated.Add(item);
        }

        await vault.UpdateItems(updated);

        var model = new PageContentModel
        {
            Meta = { ["Title"] = $"Recently Scraped: {updated.Count} Games" }
        };

        model.PageButtons.Add(new PageContentButton
        {
            Text = "Scrape More",
            ActionUrl = "/rawg/scrape"
        });

        foreach (var item in updated)
            model.Items.Add(_cards.BuildRecentScrapeCard(item));

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

        return await BuildSearchModelAsync(item, query, slug);
    }

    public async Task<PageContentModel> BuildSearchModelAsync(VaultItem item, string? query, bool? slug)
    {
        query ??= item.Name ?? "";

        var subtitle = $"Platform: {item.PlatformName}<br>";

        if (slug == true)
            subtitle += $"Searching by slug: {query}";
        else if (!string.IsNullOrWhiteSpace(query) && query != item.Name)
            subtitle += $"Searching with alternate title: {query}";
        else
            subtitle += $"Searching RAWG for: {query}";

        var model = new PageContentModel
        {
            Meta =
            {
                ["Title"] = $"RAWG Search: {item.Name}",
                ["Subtitle"] = subtitle,
                ["Mode"] = "Search",
                ["PageId"] = item.PageId,
                ["Platform"] = item.PlatformName ?? "",
                ["Slug"] = slug?.ToString() ?? "false"
            }
        };

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

        searchCard.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Hidden,
            Name = "id",
            Value = item.PageId
        });

        model.Items.Add(searchCard);

        if (slug == true)
        {
            var result = await _api.GetGameBySlugAsync(query);

            if (result != null)
                model.Items.Add(_cards.BuildMatchCard(item, result));

            return model;
        }

        var results = await _api.SearchRawgAsync(query, item.PlatformId);

        foreach (var r in results)
            model.Items.Add(_cards.BuildMatchCard(item, r));

        return model;
    }

    // =====================================================================
    //  APPLY MATCH (simple apply, no reload)
    // =====================================================================

    public async Task ApplyMatchAsync(string pageId, int rawgId, bool overwriteName)
    {
        var item = await vault.GetItem(pageId);
        if (item == null)
            return;

        var full = await _api.GetGameByIdAsync(rawgId);
        if (full == null)
            return;

        ApplyRawgMatch(item, full, overwriteName);

        await vault.UpdateItem(item);
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
