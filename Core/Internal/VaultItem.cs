namespace Vault.Core;

/// <summary>
/// Represents an entry in the GrimsVault Notion "Data" database.
/// Essentially, Games and Gaming Accessories
/// </summary>
public class VaultItem
{
    /// <summary>
    /// The unique identifier of the Notion page.
    /// </summary>
    public required string PageId { get; set; }

    /// <summary>
    /// The name of the Item.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// A list of URLs pointing to media files associated with the Item.
    /// </summary>
    public List<string>? Media { get; set; }

    /// <summary>
    /// The RAWG API identifier for the Item.
    /// </summary>
    public string? RawgId { get; set; }

    /// <summary>
    /// The scrape status of the Item. Expected values: "Scraped" or "Error".
    /// </summary>
    public string? ScrapeStatus { get; set; }

    /// <summary>
    /// The release date of the Item.
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// The platform ID, derived from a rollup relationship in Notion.
    /// </summary>
    public string? PlatformId { get; set; }

    /// <summary>
    /// The name of the platform, derived from a rollup relationship in Notion.
    /// Used only for output; not posted back.
    /// </summary>
    public string? PlatformName { get; set; }

    /// <summary>
    /// A textual description of the Item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The publisher of the Item.
    /// </summary>
    public string? Publisher { get; set; }

    /// <summary>
    /// The developer of the Item.
    /// </summary>
    public string? Developer { get; set; }

    /// <summary>
    /// The date the Item was scraped into the database.
    /// </summary>
    public DateTime? Scraped { get; set; }

    /// <summary>
    /// The name of the related Set from the Sets database.
    /// This is a read-only enrichment field in practice, but may be populated from multiple sources.
    /// </summary>
    public string? SetName { get; set; }

    /// <summary>
    /// A text field used to group games within a Set.
    /// </summary>
    public string? Subset { get; set; }

    /// <summary>
    /// A numeric field used to determine wishlist sequencing.
    /// </summary>
    public string? SortOrder { get; set; }

    /// <summary>
    /// References the CEX Api  for grabbing information about pricing and availability.
    /// </summary>
    public string? CexID { get; set; }

    //Binary flag indicating whether to search Ebay for this game. Used for filtering in the UI and as a hint for the scraping process.
    public bool SearchEbay { get; set; }

    //List of eBay item IDs to exclude from results for this game. Populated from a multi-select property in Notion, stored as comma-separated values. Used to filter out irrelevant or duplicate listings.
    public List<string>? ExcludedListingIds { get; set; }

 

}