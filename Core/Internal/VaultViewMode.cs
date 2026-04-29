/// <summary>
/// Base type for all Vault ViewModes.
///
/// A ViewMode defines *how* a Vault page is rendered.
/// Each ViewMode is a drop-in module located under:
///     /ViewModes/{Name}/
///
/// To create a new ViewMode:
/// 1. Create a folder: /ViewModes/MyMode/
/// 2. Add a class: MyModeViewMode : VaultViewMode
/// 3. Add a partial class file: VaultViewModes.MyMode.cs
/// 4. Add a partial view: /ViewModes/MyMode/Views/_VaultMyMode.cshtml
/// 5. The static field will auto-register itself into VaultViewModes.All
///
/// This system ensures:
/// - IntelliSense discoverability
/// - No central list to maintain
/// - No folder drift (paths derived from Name)
/// - Drop-in extensibility
/// </summary>
namespace Vault.Core;

public abstract class VaultViewMode(string name, string partialName)
{
    public string Name { get; } = name;
    public string PartialName { get; } = partialName;

    public string Folder => $"/ViewModes/{Name}";
    public string PartialView => $"{Folder}/Views/{PartialName}";
}

public static class VaultViewModes
{
    private static readonly List<VaultViewMode> _all = new();

    public static IReadOnlyList<VaultViewMode> All => _all;

    internal static void Register(VaultViewMode mode) => _all.Add(mode);
}

public static class VaultViewModeExtensions
{
    public static T Register<T>(this T mode) where T : VaultViewMode
    {
        VaultViewModes.Register(mode);
        return mode;
    }
}
