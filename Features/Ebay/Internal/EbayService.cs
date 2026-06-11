using Microsoft.Extensions.Configuration;
using Vault.Core;
using Vault.Core.Internal;
using Vault.ViewModes;

namespace Vault.Features.Ebay;

/// <summary>
/// Provides eBay integration for Vault. 
/// Fetches items flagged for eBay scanning, queries eBay for each,
/// and shapes results into a PageContentModel for rendering.
/// </summary>
public sealed class EbayService(IConfiguration config, VaultService vault)
    : VaultFeatureBase, IVaultFeature
{
    private readonly EbayApiClient _api = new(new HttpClient(), config);
    private readonly VaultService _vault = vault;

    private sealed class Meta : IVaultFeatureMetaData
    {
        public string Name => "eBay Scanner";
        public string Route => "/ebay";
    }

    public IVaultFeatureMetaData MetaData { get; } = new Meta();

    public string Status() => "eBay Feature OK";

    public VaultViewMode DefaultViewMode => CollapsingListsViewMode.Instance;

    // ------------------------------------------------------------------------
    // MAIN FEATURE ENTRY
    // ------------------------------------------------------------------------

    public async Task<PageContentModel> LoadItemsAsync()
    {
        var model = new PageContentModel();

        var query = new QueryDefinition
        {
            Filters =
            {
                new QueryFilter
                {
                    Property = "Search eBay",
                    Operator = QueryOperator.Equals,
                    Value = true
                }
            }
        };

        var items = await _vault.GetItems(query);

        foreach (var game in items)
        {
            var searchQuery = $"{game.Name} {game.PlatformName}".Trim();
            var summaries = await _api.SearchAsync(searchQuery);

            if (summaries == null || summaries.Count == 0)
                continue;

            var listings = summaries
                .Select(MapSummary)
                .ToList();

            var excludedIds = game.ExcludedEbayIds ?? new List<string>();

            var included = listings
                .Where(l => l.ItemId != null && !excludedIds.Contains(l.ItemId))
                .ToList();

            var excluded = listings
                .Where(l => l.ItemId != null && excludedIds.Contains(l.ItemId))
                .ToList();

            var ordered = included.Concat(excluded).ToList();

            // Group-level action card
            model.Items.Add(new PageContentItem
            {
                Group = game.Name ?? "Unknown Item",
                Elements =
                {
                    new PageContentElement
                    {
                        Type = PageContentElementType.Button,
                        Text = "Exclude Selected",
                        ActionUrl = "/ebay/exclude",
                        FormTarget = "_self"
                    },
                    new PageContentElement
                    {
                        Type = PageContentElementType.Hidden,
                        Name = "gameId",
                        Value = game.PageId
                    }
                },
                FormMethod = "post",
                FormAction = "/ebay/exclude"
            });

            // Listing cards
            foreach (var listing in ordered)
            {
                var isExcluded = listing.ItemId != null && excludedIds.Contains(listing.ItemId);

                var card = new PageContentItem
                {
                    Group = game.Name ?? "Unknown Item",
                    Primary = listing.Title,
                    Secondary = FormatPrice(listing),
                    Ticker = FormatEndTime(listing),
                    SecondFooter = FormatFooter(listing),
                    ImageUrl = listing.ImageUrl,
                    Url = listing.ListingUrl,
                    HasError = isExcluded,
                    Meta =
                    {
                        ["itemId"] = listing.ItemId ?? "",
                        ["gameId"] = game.PageId
                    },
                    Elements =
                    {
                        new PageContentElement
                        {
                            Type = PageContentElementType.Checkbox,
                            Name = $"exclude_{listing.ItemId}",
                            Label = "Exclude",
                            Checked = isExcluded
                        },
                        new PageContentElement
                        {
                            Type = PageContentElementType.Hidden,
                            Name = "itemId",
                            Value = listing.ItemId ?? ""
                        },
                        new PageContentElement
                        {
                            Type = PageContentElementType.Hidden,
                            Name = "gameId",
                            Value = game.PageId
                        }
                    },
                    FormMethod = "post",
                    FormAction = "/ebay/exclude"
                };

                model.Items.Add(card);
            }
        }

        return model;
    }

    // ------------------------------------------------------------------------
    // MAPPING
    // ------------------------------------------------------------------------

    private static EbayListing MapSummary(EbayApiModels.ItemSummary s)
    {
        var isAuction = s.BuyingOptions?.Any(o =>
            o.Equals("AUCTION", StringComparison.OrdinalIgnoreCase)) == true;

        return new EbayListing
        {
            ItemId = s.ItemId,
            Title = s.Title,
            ImageUrl = s.Image?.Url,
            ListingUrl = s.Url,
            Condition = s.Condition,
            ListingType = isAuction ? "AUCTION" : "FIXED_PRICE",
            EndTime = s.EndTime,
            SellerLocation = s.Location?.Country,
            Price = decimal.TryParse(s.Price?.Value, out var p) ? p : 0,
            Currency = s.Price?.Currency,
            CurrentBid = null,
            CurrentBidCurrency = null,
            IsExcluded = false
        };
    }

    // ------------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------------

    private static string FormatPrice(EbayListing l)
    {
        var isAuction = l.ListingType == "AUCTION";

        if (isAuction)
            return $"{l.Price} {l.Currency} (Auction)";

        return $"{l.Price} {l.Currency} Buy It Now";
    }

    private static string FormatEndTime(EbayListing l)
    {
        var isAuction = l.ListingType == "AUCTION";

        if (!isAuction || l.EndTime == null)
            return "";

        var remaining = l.EndTime.Value - DateTime.UtcNow;

        if (remaining.TotalHours < 1)
            return $"Ends in {remaining.Minutes}m";

        if (remaining.TotalHours < 24)
            return $"Ends in {remaining.Hours}h {remaining.Minutes}m";

        return $"Ends {l.EndTime.Value:ddd HH:mm}";
    }

    private static string FormatFooter(EbayListing l)
    {
        return $"{l.Condition} — {l.SellerLocation}";
    }
}
