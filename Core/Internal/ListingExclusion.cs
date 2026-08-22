namespace Vault.Core.Internal;

/// <summary>
/// Represents an exclusion of an external listing (eBay, CEX, Amazon, etc.)
/// for a specific VaultItem.
/// </summary>
public class ListingExclusion
{
    /// <summary>
    /// The VaultItem PageID this exclusion applies to.
    /// Maps to the "Item" relation field in Notion.
    /// </summary>
    public string ItemId { get; set; } = "";

    /// <summary>
    /// The external listing identifier being excluded.
    /// Maps to the "Listing ID" text field in Notion.
    /// </summary>
    public string ListingId { get; set; } = "";

    /// <summary>
    /// The type of listing being excluded (e.g., "eBay").
    /// Maps to the "Listing Type" select field in Notion.
    /// </summary>
    public string ListingType { get; set; } = "";
}
