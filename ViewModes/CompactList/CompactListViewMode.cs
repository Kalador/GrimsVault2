using Vault.Core;

namespace Vault.ViewModes;

public sealed class CompactListViewMode : VaultViewMode
{
    public static readonly CompactListViewMode Instance =
        new CompactListViewMode().Register();

    private CompactListViewMode()
        : base("CompactList")
    {
    }

    public override string ViewPath =>
        "/ViewModes/CompactList/Views/VaultCompactList.cshtml";

    public override string CardPartialPath =>
        "/ViewModes/CompactList/Views/_VaultCard.cshtml";

    public override IReadOnlyList<string> Styles =>
        [$"/ViewModes/CompactList/css/vault-compactlist.css?v={CacheBust}"];

    public override IReadOnlyList<string> Scripts =>
        [$"/ViewModes/CompactList/js/compactlist.js?v={CacheBust}"];
}
