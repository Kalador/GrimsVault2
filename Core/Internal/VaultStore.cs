namespace Vault.Core;

public class VaultStore
{
    public string Name { get; set; } = "";
    public string StoreType { get; set; } = "";
    public bool IsPriority { get; set; }
    public string? Region { get; set; }
    public int? Number { get; set; }
}
