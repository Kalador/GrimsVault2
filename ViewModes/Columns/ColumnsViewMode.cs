using Vault.Core;

namespace Vault.ViewModes;

public sealed class ColumnsViewMode : VaultViewMode
{
    public static readonly ColumnsViewMode Instance =
        new ColumnsViewMode().Register();

    private ColumnsViewMode()
        : base("Columns", "VaultColumns.cshtml")
    {
    }

    public override IReadOnlyList<string> Scripts =>
        ["/ViewModes/Columns/js/columns.js"];

    public override IReadOnlyList<string> Styles =>
        ["/ViewModes/Columns/css/vault-columns.css"];
}
