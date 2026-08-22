using Vault.Core;
using Vault.Core.Internal;
using Vault.ViewModes;

namespace Vault.Features.Ebay;

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

    private async Task<List<PageContentItem>> BuildCardsForGame(VaultItem game)
    {
        var searchQuery = $"{game.Name} {game.PlatformName}".Trim();
        var summaries = await _api.SearchAsync(searchQuery);

        var listings = summaries?
            .Select(MapSummary)
            .ToList() ?? new List<EbayListing>();

        var excludedIds = game.ExcludedListingIds ?? new List<string>();

        var included = listings
            .Where(l => l.ItemId != null && !excludedIds.Contains(l.ItemId))
            .ToList();

        var excluded = listings
            .Where(l => l.ItemId != null && excludedIds.Contains(l.ItemId))
            .ToList();

        var ordered = included.Concat(excluded).ToList();

        var groupName = game.Name ?? "Unknown Item";

        var cards = new List<PageContentItem>();

        foreach (var listing in ordered)
        {
            var isExcluded = listing.ItemId != null && excludedIds.Contains(listing.ItemId);

            var card = new PageContentItem
            {
                Group = groupName,
                Primary = listing.Title,
                Secondary = FormatListingType(listing),
                Ticker = FormatEndTime(listing),
                SecondFooter = FormatPrice(listing),
                ImageUrl = listing.ImageUrl,
                Url = listing.ListingUrl,
                HasError = isExcluded,

                Meta =
            {
                ["listingId"] = listing.ItemId ?? "",
                ["itemId"] = game.PageId
            },

                Elements = isExcluded
                    ? []
                    : [
                        new PageContentElement
                    {
                        Type = PageContentElementType.Checkbox,
                        Name = "ListingId",
                        Value = listing.ItemId ?? ""
                    }
                    ],

            };

            cards.Add(card);
        }

        return cards;
    }

    public async Task<IGrouping<string, PageContentItem>> LoadSingleItemAsync(string itemId)
    {
        var game = await _vault.GetItem(itemId);
        if (game == null)
            throw new InvalidOperationException($"Item {itemId} not found.");

        var cards = await BuildCardsForGame(game);

        return cards.GroupBy(c => c.Group).First();
    }


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
            var cards = await BuildCardsForGame(game);

            var groupName = game.Name ?? "Unknown Item";

            model.GroupButtons[groupName] = new List<PageContentButton>
        {
            new PageContentButton
            {
                Text = "Exclude Selected",
                //ActionUrl = "/ebay/exclude"
                ActionUrl = $"javascript:excludeSelected(this)"

            },
            new PageContentButton
            {
                Text = "Show/Hide Excluded",
                ActionUrl = $"javascript:toggleErrorRows('{groupName}')"
            }
        };

            foreach (var card in cards)
                model.Items.Add(card);
        }

        return model;
    }


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

    private static string FormatListingType(EbayListing l)
    {
        return l.ListingType == "AUCTION"
            ? "🔨 Auction"
            : "🛒 Buy It Now";
    }

    private static string FormatPrice(EbayListing l)
    {
        var symbol = l.Currency == "GBP" ? "£" : l.Currency + " ";
        return $"{symbol}{l.Price:0.00}";
    }

    private static string FormatEndTime(EbayListing l)
    {
        if (l.EndTime == null)
            return "⏰ -";

        var remaining = l.EndTime.Value - DateTime.UtcNow;

        if (remaining.TotalMinutes <= 0)
            return "⏰ -";

        if (remaining.TotalHours < 1)
            return $"⏰ {remaining.Minutes}m";

        if (remaining.TotalHours < 24)
            return $"⏰ {remaining.Hours}h {remaining.Minutes}m";

        return $"⏰ {l.EndTime.Value:ddd HH:mm}";
    }
}
