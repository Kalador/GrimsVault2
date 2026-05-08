using Vault.Core;

namespace Vault.Data.Notion
{
    /// <summary>
    /// Concrete Notion-backed implementation of <see cref="IDataHandler"/>.
    /// 
    /// Responsibilities:
    /// - Query Notion databases using <see cref="NotionClient"/>
    /// - Apply optional <see cref="QueryDefinition"/> filters
    /// - Map raw Notion JSON pages into strongly-typed Core models
    /// - Return <see cref="VaultItem"/> and <see cref="VaultStore"/> collections
    /// 
    /// This class performs no business logic and stores no state beyond configuration.
    /// </summary>
    public class NotionDataHandler(NotionClient client, IConfiguration config) : IDataHandler
    {
        private readonly NotionClient _client = client;

        // Main Vault Items database
        private readonly string _databaseId = config["Notion:NotionDatabaseId"]!;

        // Stores database
        private readonly string _storeDatabaseId = config["Notion:NotionStoreDatabaseId"]!;

        // =====================================================================
        //  ITEMS
        // =====================================================================

        /// <summary>
        /// Loads items from the Notion database.
        /// Optional <see cref="QueryDefinition"/> applies AND filters.
        /// </summary>
        public async Task<List<VaultItem>> GetItemsAsync(
            QueryDefinition? query = null,
            int? pageSize = null)
        {
            var endpoint = $"/databases/{_databaseId}/query";

            object payload = query == null
                ? new { }
                : BuildNotionPayload(query);

            var json = await _client.PostAsync(endpoint, payload, pageSize);

            return
            [
                .. json.RootElement
                    .GetProperty("results")
                    .EnumerateArray()
                    .Select(NotionMapper.MapVaultItem)
            ];
        }

        // =====================================================================
        //  STORES
        // =====================================================================

        /// <summary>
        /// Loads store definitions from the Notion Stores database.
        /// Optional <see cref="QueryDefinition"/> applies AND filters.
        /// </summary>
        public async Task<List<VaultStore>> GetStoresAsync(
            QueryDefinition? query = null,
            int? pageSize = null)
        {
            var endpoint = $"/databases/{_storeDatabaseId}/query";

            object payload = query == null
                ? new { }
                : BuildNotionPayload(query);

            var json = await _client.PostAsync(endpoint, payload, pageSize);

            return
            [
                .. json.RootElement
                    .GetProperty("results")
                    .EnumerateArray()
                    .Select(NotionMapper.MapVaultStore)
            ];
        }

        // =====================================================================
        //  PAYLOAD BUILDER
        // =====================================================================

        /// <summary>
        /// Builds a Notion "filter" payload using AND semantics.
        /// </summary>
        private object BuildNotionPayload(QueryDefinition query)
        {
            var filters = query.Filters
                .Select(BuildNotionFilter)
                .ToList();

            return new
            {
                filter = new
                {
                    and = filters
                }
            };
        }

        /// <summary>
        /// Converts a <see cref="QueryFilter"/> into a Notion-compatible filter object.
        /// Supports text, number, and empty/not-empty operators.
        /// </summary>
        private object BuildNotionFilter(QueryFilter f)
        {
            // BOOLEAN SUPPORT (checkbox)
            if (f.Value is bool b)
            {
                return f.Operator switch
                {
                    QueryOperator.Equals => new
                    {
                        property = f.Property,
                        checkbox = new { equals = b }
                    },

                    QueryOperator.NotEquals => new
                    {
                        property = f.Property,
                        checkbox = new { does_not_equal = b }
                    },

                    _ => throw new NotSupportedException(
                        $"Operator {f.Operator} not supported for boolean filters")
                };
            }
            return f.Operator switch
            {
                QueryOperator.NotEmpty => new
                {
                    property = f.Property,
                    rich_text = new { is_not_empty = true }
                },

                QueryOperator.Empty => new
                {
                    property = f.Property,
                    rich_text = new { is_empty = true }
                },

                QueryOperator.Equals => new
                {
                    property = f.Property,
                    rich_text = new { equals = f.Value }
                },

                QueryOperator.NotEquals => new
                {
                    property = f.Property,
                    rich_text = new { does_not_equal = f.Value }
                },

                QueryOperator.Contains => new
                {
                    property = f.Property,
                    rich_text = new { contains = f.Value }
                },

                QueryOperator.NotContains => new
                {
                    property = f.Property,
                    rich_text = new { does_not_contain = f.Value }
                },

                QueryOperator.GreaterThan => new
                {
                    property = f.Property,
                    number = new { greater_than = f.Value }
                },

                QueryOperator.LessThan => new
                {
                    property = f.Property,
                    number = new { less_than = f.Value }
                },

                _ => throw new NotSupportedException(
                    $"Operator {f.Operator} not supported for Notion")
            };
        }

    }
}
