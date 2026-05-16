// ============================================================================
//  PageContentModel (Core/Internal)
// ----------------------------------------------------------------------------
//  This is the *generic UI content contract* between Features and ViewModes.
//
//  - Features produce PageContentModel instances.
//  - ViewModes consume PageContentModel instances.
//
//  The structure is intentionally generic:
//      PageContentModel
//          -> Items (cards)
//              -> Group (string category assigned by the feature)
//
//  ViewModes may interpret groups as:
//      - Columns
//      - Categories
//      - Sections
//      - Tabs
//      - Any other layout concept
//
//  No feature-specific or viewmode-specific types belong here.
//  This ensures ViewModes remain drop‑in modules and Features remain isolated.
// ============================================================================

namespace Vault.Core.Internal;

/// <summary>
/// Represents the full structured content of a Vault page.
/// A ViewMode receives this model and decides how to render it.
/// </summary>
public class PageContentModel
{
    /// <summary>
    /// A flat list of items. Each item carries its own Group string.
    /// ViewModes may group these dynamically (e.g., into columns).
    /// </summary>
    public List<PageContentItem> Items { get; set; } = [];
}

/// <summary>
/// A single UI card. ViewModes decide how to render it (tile, row, column card, etc.).
/// Features populate the fields with whatever data is relevant.
/// </summary>
public class PageContentItem
{
    /// <summary>
    /// The logical group/category this item belongs to.
    /// </summary>
    public string Group { get; set; } = "";

    /// <summary>
    /// Optional secondary text (e.g., price, platform, category).
    /// </summary>
    public string Secondary { get; set; } = "";

    /// <summary>
    /// Optional image associated with the card.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Primary display text for the card. Could be Name, could be Price.
    /// User Whatever proeprty is the primary concern of any given feature.
    /// </summary>
    public string? Primary { get; set; }

    /// <summary>
    /// Text for a scrolling ticker in the footer.
    /// </summary>
    public string? Ticker { get; set; }

    /// <summary>
    /// Optional footer text (e.g., timestamp, status).
    /// </summary>
    public string? SecondFooter { get; set; }

    /// <summary>
    /// Optional URL the card links to.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Indicates whether the feature encountered an error producing this item.
    /// ViewModes may choose to render error states differently.
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Arbitrary metadata for ViewModes or Features to use.
    /// ViewModes should treat this as optional and defensive.
    /// </summary>
    public Dictionary<string, object> Meta { get; set; } = [];
}
