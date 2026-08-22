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
        /// <param name="pageSize">PageSize for results.</param>
        /// <param name="query">Optional QueryDefinition to filter results.</param>
        /// <returns>A list of VaultItem objects.</returns>
        Task<List<VaultItem>> GetItemsAsync(QueryDefinition? query = null, int? pageSize = null);

        /// <summary>
        /// Request a single VaultItem object using the item Key as reference.
        /// </summary>
        /// <param name="pageID">The GUID / Key of a single VaultItem.</param>
        /// <returns>A single VaultItem object.</returns>
        Task<VaultItem?> GetItemAsync(string pageID);

        /// <summary>
        /// Updates a single VaultItem in the backing store.
        /// </summary>
        Task UpdateItemAsync(VaultItem item);

        /// <summary>
        /// Updates multiple VaultItems in the backing store.
        /// Implementations may parallelise internally.
        /// </summary>
        Task UpdateItemsAsync(IEnumerable<VaultItem> items);

        /// <summary>
        /// Requests a list of store definitions from the Stores database.
        /// </summary>
        /// <param name="query">Optional QueryDefinition to filter results.</param>
        /// <param name="pageSize">Optional page size.</param>
        /// <returns>A list of IVaultStore objects representing all matching stores.</returns>
        Task<List<VaultStore>> GetStoresAsync(
            QueryDefinition? query = null,
            int? pageSize = null);

        /// <summary>
        /// Adds one or more listing exclusions for a given VaultItem.
        /// Implementations should create exclusion entries and update the item's relation field.
        /// ListingType is stored so the exclusion DB can differentiate marketplace sources.
        /// </summary>
        /// <param name="pageID">The GUID / Key of the VaultItem to update.</param>
        /// <param name="listingIds">A collection of listing IDs to exclude.</param>
        /// <param name="listingType">Marketplace type for the exclusions (e.g. "eBay").</param>
        Task AddListingExclusionsAsync(string pageID, IEnumerable<string> listingIds, string listingType);
    }
}
