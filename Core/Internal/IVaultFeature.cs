using Vault.ViewModes;
namespace Vault.Core;

/// <summary>
/// All services added by modular features MUST implement this interface to be enumerated and registered by the VaultService.
/// </summary>
public interface IVaultFeature
{
    IVaultFeatureMetaData MetaData { get; }

    String Status();

    /// <summary>
    /// The Default ViewMode for the feature. This is used to determine which view mode to use when displaying the feature in the VaultService.
    /// </summary>
    VaultViewMode DefaultViewMode { get; }

}
/// <summary>
/// Features that should be listed on the home page should implement this intrerface to provide metadata for display and routing.
/// </summary>
public interface IVaultFeatureMetaData
{
    /// <summary>
    /// Gets the display name of the feature.Display Name of the feature, used in the VaultService to display the list of features.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Destination route for the feature.
    /// </summary>
    string Route { get; }

}