using Vault.Core;
namespace Vault.ViewModes;

public sealed class ColumnsViewMode : VaultViewMode
{
    public static readonly ColumnsViewMode Instance =
        new ColumnsViewMode().Register();

    private ColumnsViewMode()
        : base("Columns", "_VaultColumns.cshtml")
    {
    }
}
