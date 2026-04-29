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
//          -> Groups (e.g., columns, categories, sections)
//              -> Items (cards)
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
    /// Logical groups of items. A ViewMode may interpret these as:
    /// - Columns
    /// - Categories
    /// - Sections
    /// - Widgets
    /// - Any other layout concept
    /// </summary>
    public List<PageContentGroup> Groups { get; set; } = new();
}

/// <summary>
/// A logical grouping of items. The meaning of a group is defined by the feature.
/// ViewModes do not assume any semantics beyond "a group contains items".
/// </summary>
public class PageContentGroup
{
    /// <summary>
    /// Display label for the group (e.g., "Priority", "Out of Stock", "Recently Added").
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// The items/cards contained in this group.
    /// </summary>
    public List<PageContentItem> Items { get; set; } = new();
}

/// <summary>
/// A single UI card. ViewModes decide how to render it (tile, row, column card, etc.).
/// Features populate the fields with whatever data is relevant.
/// </summary>
public class PageContentItem
{
    /// <summary>
    /// Primary display text for the card.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Optional image associated with the card.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Optional secondary text (e.g., price, platform, category).
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Optional footer text (e.g., timestamp, status).
    /// </summary>
    public string? Footer { get; set; }

    /// <summary>
    /// Optional URL the card links to.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Arbitrary metadata for ViewModes to use.
    /// Examples:
    ///     Meta["category"] = "priority"
    ///     Meta["raw"] = originalFeatureModel
    ///
    /// ViewModes should treat this as optional and defensive.
    /// </summary>
    public Dictionary<string, object> Meta { get; set; } = [];
}
