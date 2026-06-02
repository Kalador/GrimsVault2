using Vault.Core;
using Vault.Core.Internal;

namespace Vault.Features.Rawg;

public sealed class RawgCardBuilder
{
    /// <summary>
    /// Builds a match card for a RAWG search result.
    /// </summary>
    public PageContentItem BuildMatchCard(VaultItem item, RawgApiModels.RawgGame match)
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

        if (!string.IsNullOrWhiteSpace(match.Slug))
        {
            card.Elements.Add(new PageContentElement
            {
                Type = PageContentElementType.Button,
                Text = "View",
                ActionUrl = $"https://rawg.io/games/{match.Slug}",
                FormTarget = "_blank"
            });
        }

        card.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Button,
            Text = "Save",
            ActionUrl = "/rawg/apply"
        });

        card.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Checkbox,
            Name = "overwriteName",
            Label = "Use this title?"
        });

        card.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Hidden,
            Name = "id",
            Value = item.PageId
        });

        card.Elements.Add(new PageContentElement
        {
            Type = PageContentElementType.Hidden,
            Name = "rawgId",
            Value = match.Id.ToString()
        });

        return card;
    }

    /// <summary>
    /// Builds a card for a recently scraped item.
    /// </summary>
    public PageContentItem BuildRecentScrapeCard(VaultItem item)
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

        if (card.HasError)
        {
            card.Url = $"/rawg/search?id={item.PageId}&query={Uri.EscapeDataString(item.Name ?? "")}";
        }

        return card;
    }
}
