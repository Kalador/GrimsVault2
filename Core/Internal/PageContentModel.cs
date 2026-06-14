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
//          -> Meta (page-level metadata)
//          -> Inputs (optional page-level input fields)
//          -> PageButtons (global actions)
//          -> GroupButtons (optional group-level actions)
//          -> Items (cards)
//              -> Elements (ordered UI controls inside the card)
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

// ============================================================================
//  ELEMENT TYPES
// ============================================================================

public enum PageContentElementType
{
    Input,
    Checkbox,
    Button,
    Hidden
}

/// <summary>
/// A single UI control inside a card.
/// Features populate these; ViewModes render them generically.
/// </summary>
public class PageContentElement
{
    /// <summary>
    /// The type of UI element (Input, Checkbox, Button).
    /// </summary>
    public PageContentElementType Type { get; set; }

    /// <summary>
    /// The form field name (for inputs + checkboxes).
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Optional value for inputs.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Label for checkboxes.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Whether a checkbox is checked.
    /// </summary>
    public bool Checked { get; set; }

    /// <summary>
    /// Button text (for Button type).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Optional URL for button actions.
    /// If null, the form's action is used.
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Optional placeholder text for inputs.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Optional target for button actions (e.g., "_blank").
    /// Only applies to Button elements.
    /// </summary>
    public string? FormTarget { get; set; }
}

// ============================================================================
//  PAGE-LEVEL INPUTS + BUTTONS
// ============================================================================

/// <summary>
/// Represents a single input field rendered at the page level.
/// ViewModes treat these generically and do not attach meaning.
/// </summary>
public class PageContentInput
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Value { get; set; }
    public string Type { get; set; } = "text";
}

/// <summary>
/// Represents a clickable UI action at the page level.
/// </summary>
public class PageContentButton
{
    public string Text { get; set; } = "";
    public string ActionUrl { get; set; } = "";
}

// ============================================================================
//  PAGE CONTENT MODEL
// ============================================================================

public class PageContentModel
{
    public Dictionary<string, object> Meta { get; set; } = [];
    public List<PageContentInput> Inputs { get; set; } = [];
    public List<PageContentButton> PageButtons { get; set; } = [];

    /// <summary>
    /// Optional actions rendered at the *group* level.
    /// Keys correspond to the Group string assigned by features.
    /// ViewModes render these generically beneath each group.
    /// </summary>
    public Dictionary<string, List<PageContentButton>> GroupButtons { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public List<PageContentItem> Items { get; set; } = [];
}

// ============================================================================
//  CARD MODEL
// ============================================================================

public class PageContentItem
{
    public string Group { get; set; } = "";
    public string Secondary { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Primary { get; set; }
    public string? Ticker { get; set; }
    public string? SecondFooter { get; set; }
    public string? Url { get; set; }
    public bool HasError { get; set; }
    public Dictionary<string, object> Meta { get; set; } = [];

    /// <summary>
    /// Ordered UI controls inside the card.
    /// Replaces the old Buttons list and the hardcoded search form.
    /// </summary>
    public List<PageContentElement> Elements { get; set; } = [];

    /// <summary>
    /// Optional form method for the Elements block.
    /// Defaults to GET.
    /// </summary>
    public string FormMethod { get; set; } = "get";

    /// <summary>
    /// Optional form action for the Elements block.
    /// If null, ViewModes may choose not to render a form wrapper.
    /// </summary>
    public string? FormAction { get; set; }
}
