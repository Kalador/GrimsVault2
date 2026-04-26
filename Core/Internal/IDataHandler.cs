using Vault.Core;

namespace Vault.Data
{
    /// <summary>
    /// Defines a contract for handling data operations.
    /// </summary>
    public interface IDataHandler
    {
        /// <summary>
        /// Request a list of VaultItem objects with a specified limit.
        /// </summary>
        /// <param name="pageSize">PageSize for results. If null, uses default file from Notion appsettings.</param>
        /// <returns>A list of VaultItem objects.</returns>
        Task<List<VaultItem>> GetItemsAsync(int? pageSize = null);

    }
}
