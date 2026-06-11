namespace Vault.Features.Ebay;

/// <summary>
/// Represents a single eBay listing returned from a search or detail lookup.
/// Includes optional auction data and exclusion state.
/// </summary>
public sealed class EbayListing
{
    /// <summary>Unique eBay item identifier.</summary>
    public string? ItemId { get; set; }

    /// <summary>Listing title as provided by the seller.</summary>
    public string? Title { get; set; }

    /// <summary>Primary image URL for the listing.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Buy‑it‑now or starting price.</summary>
    public decimal Price { get; set; }

    /// <summary>Currency code for the price.</summary>
    public string? Currency { get; set; }

    /// <summary>Direct link to the listing on eBay.</summary>
    public string? ListingUrl { get; set; }

    /// <summary>Condition label (e.g., New, Used, Good).</summary>
    public string? Condition { get; set; }

    /// <summary>Listing format (e.g., AUCTION, FIXED_PRICE).</summary>
    public string? ListingType { get; set; }

    /// <summary>End time for auctions or time‑limited listings.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Country of the seller.</summary>
    public string? SellerLocation { get; set; }

    /// <summary>Indicates whether this listing is excluded by user rules.</summary>
    public bool IsExcluded { get; set; }

    /// <summary>Current auction bid amount, if applicable.</summary>
    public decimal? CurrentBid { get; set; }

    /// <summary>Currency code for the current bid.</summary>
    public string? CurrentBidCurrency { get; set; }
}

/// <summary>
/// Represents a request to exclude one or more eBay listings
/// from appearing in search results for a specific item.
/// </summary>
public sealed class EbayExclusionRequest
{
    /// <summary>
    /// Identifier of the item in Vault for which exclusions apply.
    /// </summary>
    public string ItemId { get; set; } = "";

    /// <summary>
    /// List of eBay item IDs to exclude.
    /// </summary>
    public List<string> EbayIds { get; set; } = new();
}