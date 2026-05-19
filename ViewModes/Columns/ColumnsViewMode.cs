using Vault.Core;

namespace Vault.ViewModes;

public sealed class ColumnsViewMode : VaultViewMode
{
    public static readonly ColumnsViewMode Instance =
        new ColumnsViewMode().Register();

    private ColumnsViewMode()
        : base("Columns")
    {
    }

    public override string ViewPath =>
        "/ViewModes/Columns/Views/VaultColumns.cshtml";

    public override string CardPartialPath =>
        "/ViewModes/Columns/Views/_VaultCard.cshtml";

    public override IReadOnlyList<string> Styles =>
    [$"/ViewModes/Columns/css/vault-columns.css?v={CacheBust}"];

    public override IReadOnlyList<string> Scripts =>
        [$"/ViewModes/Columns/js/columns.js?v={CacheBust}"];

}
