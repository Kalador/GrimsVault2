using Vault.Core;

namespace Vault.ViewModes;

/// <summary>
/// CompactList ViewMode (Vault 2.0).
/// Renders grouped items using a compact vertical card layout.
/// This ViewMode is feature‑agnostic and consumes only the
/// generic <see cref="PageContentModel"/> contract.
/// </summary>
public sealed class CompactListViewMode : VaultViewMode
{
    /// <summary>
    /// Singleton instance used by all features that select this ViewMode.
    /// Registered automatically via <see cref="VaultViewMode.Register"/>.
    /// </summary>
    public static readonly CompactListViewMode Instance =
        new CompactListViewMode().Register();

    private CompactListViewMode()
        : base("CompactList")
    {
    }

    /// <summary>
    /// Path to the main Razor view that renders the full page layout.
    /// </summary>
    public override string ViewPath =>
        "/ViewModes/CompactList/Views/VaultCompactList.cshtml";

    /// <summary>
    /// Path to the card partial used for each <see cref="PageContentItem"/>.
    /// </summary>
    public override string CardPartialPath =>
        "/ViewModes/CompactList/Views/_VaultCard.cshtml";

    /// <summary>
    /// Stylesheets required by this ViewMode.
    /// Cache-busted automatically via <see cref="VaultViewMode.CacheBust"/>.
    /// </summary>
    public override IReadOnlyList<string> Styles =>
        [$"/ViewModes/CompactList/css/vault-compactlist.css?v={CacheBust}"];

    /// <summary>
    /// Optional JavaScript for this ViewMode.
    /// </summary>
    public override IReadOnlyList<string> Scripts =>
        [$"/ViewModes/CompactList/js/compactlist.js?v={CacheBust}"];
}
