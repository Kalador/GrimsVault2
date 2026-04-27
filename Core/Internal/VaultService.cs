using Vault.Data;

namespace Vault.Core;

/// <summary>
/// The Central Core Service. All calls to the VaultService are routed through this class.
/// It is responsible for managing the lifecycle of all features and services, and for
/// providing a single point of access to the Vault's functionality.
/// </summary>
/// <param name="data">An instance of IDataHandler</param>
public partial class VaultService(IDataHandler data)
{
    /// <summary>
    /// Checks the status of the VaultService and returns a simple string indicating that it is operational.
    /// </summary>
    public string Status() => "VaultService OK";

    /// <summary>
    /// Provices access to the VaultCache. Callers can access feature-specific caches through this property.
    /// The VaultCache is a singleton that is shared across all features and services.
    /// </summary>
    public VaultCache Cache { get; } = new VaultCache();





}
