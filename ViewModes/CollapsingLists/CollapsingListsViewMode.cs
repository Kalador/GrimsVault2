using Vault.Core;


namespace Vault.ViewModes;

/// <summary>
/// CollapsingLists ViewMode (Vault 2.0).
/// Renders grouped items where each group has a collapsible body section.
/// This ViewMode is fully feature‑agnostic and consumes only the
/// generic <see cref="PageContentModel"/> contract.
/// </summary>
public sealed class CollapsingListsViewMode : VaultViewMode
{
    /// <summary>
    /// Singleton instance used by all features that select this ViewMode.
    /// Registered automatically via <see cref="VaultViewMode.Register"/>.
    /// </summary>
    public static readonly CollapsingListsViewMode Instance =
        new CollapsingListsViewMode().Register();

    private CollapsingListsViewMode()
        : base("CollapsingLists")
    {
    }

    /// <summary>
    /// Path to the main Razor view that renders the full page layout.
    /// </summary>
    public override string ViewPath =>
        "/ViewModes/CollapsingLists/Views/CollapsingLists.cshtml";

    /// <summary>
    /// Path to the card partial used for each <see cref="PageContentItem"/>.
    /// </summary>
    public override string CardPartialPath =>
        "/ViewModes/CollapsingLists/Views/_VaultCard.cshtml";

    /// <summary>
    /// Stylesheets required by this ViewMode.
    /// Cache-busted automatically via <see cref="VaultViewMode.CacheBust"/>.
    /// </summary>
    public override IReadOnlyList<string> Styles =>
        [$"/ViewModes/CollapsingLists/css/collapsinglists.css?v={CacheBust}"];

    /// <summary>
    /// Optional JavaScript for this ViewMode.
    /// </summary>
    public override IReadOnlyList<string> Scripts =>
        [$"/ViewModes/CollapsingLists/js/collapsinglists.js?v={CacheBust}"];

    

}
