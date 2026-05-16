using Vault.Core;

namespace Vault.Data.Notion
{
    /// <summary>
    /// Concrete Notion-backed implementation of <see cref="IDataHandler"/>.
    /// 
    /// Responsibilities:
    /// - Query Notion databases using <see cref="NotionClient"/>
    /// - Apply optional <see cref="QueryDefinition"/> filters
    /// - Apply optional ORDER BY clauses when provided
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

        // Cached property types for the main database
        private Dictionary<string, string>? _propertyTypes;

        // =====================================================================
        //  SCHEMA LOADER
        // =====================================================================

        /// <summary>
        /// Loads the Notion database schema once and caches property types.
        /// </summary>
        private async Task EnsureSchemaLoadedAsync()
        {
            if (_propertyTypes != null)
                return;

            var db = await _client.GetAsync($"/databases/{_databaseId}");

            _propertyTypes = db.RootElement
                .GetProperty("properties")
                .EnumerateObject()
                .ToDictionary(
                    p => p.Name,
                    p => p.Value.GetProperty("type").GetString()!
                );
        }

        // =====================================================================
        //  ITEMS
        // =====================================================================

        public async Task<List<VaultItem>> GetItemsAsync(
            QueryDefinition? query = null,
            int? pageSize = null)
        {
            await EnsureSchemaLoadedAsync();

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

        public async Task<List<VaultStore>> GetStoresAsync(
            QueryDefinition? query = null,
            int? pageSize = null)
        {
            await EnsureSchemaLoadedAsync();

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

        private object BuildNotionPayload(QueryDefinition query)
        {
            var filters = query.Filters
                .Select(BuildNotionFilter)
                .ToList();

            var sorts = query.Sorts
                .Select(s => new
                {
                    property = s.Property,
                    direction = s.Direction == QuerySortDirection.Asc
                        ? "ascending"
                        : "descending"
                })
                .ToList();

            // Build base payload
            var payload = new Dictionary<string, object>
            {
                ["filter"] = new { and = filters }
            };

            // Only include sorts if non-empty
            if (sorts.Count > 0)
                payload["sorts"] = sorts;

            return payload;
        }


        // =====================================================================
        //  FILTER BUILDER (SMART TYPE DETECTION)
        // =====================================================================

        /// <summary>
        /// Converts a <see cref="QueryFilter"/> into a Notion-compatible filter object.
        /// Automatically detects property type from the Notion schema.
        /// </summary>
        private object BuildNotionFilter(QueryFilter f)
        {
            // Determine property type from schema
            var type = _propertyTypes!.TryGetValue(f.Property, out var t)
                ? t
                : "rich_text"; // fallback

            // ------------------------------------------------------------
            // BOOLEAN SUPPORT (checkbox)
            // ------------------------------------------------------------
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

            // ------------------------------------------------------------
            // DATE PROPERTIES
            // ------------------------------------------------------------
            if (type == "date")
            {
                return f.Operator switch
                {
                    QueryOperator.NotEmpty => new
                    {
                        property = f.Property,
                        date = new { is_not_empty = true }
                    },

                    QueryOperator.Empty => new
                    {
                        property = f.Property,
                        date = new { is_empty = true }
                    },

                    QueryOperator.GreaterThan => new
                    {
                        property = f.Property,
                        date = new { after = f.Value }
                    },

                    QueryOperator.LessThan => new
                    {
                        property = f.Property,
                        date = new { before = f.Value }
                    },

                    _ => throw new NotSupportedException(
                        $"Operator {f.Operator} not supported for date property '{f.Property}'")
                };
            }

            // ------------------------------------------------------------
            // NUMBER PROPERTIES
            // ------------------------------------------------------------
            if (type == "number")
            {
                return f.Operator switch
                {
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

                    QueryOperator.Equals => new
                    {
                        property = f.Property,
                        number = new { equals = f.Value }
                    },

                    QueryOperator.NotEquals => new
                    {
                        property = f.Property,
                        number = new { does_not_equal = f.Value }
                    },

                    _ => throw new NotSupportedException(
                        $"Operator {f.Operator} not supported for number property '{f.Property}'")
                };
            }

            // ------------------------------------------------------------
            // SELECT / MULTI_SELECT
            // ------------------------------------------------------------
            if (type == "select")
            {
                return new
                {
                    property = f.Property,
                    select = new { equals = f.Value }
                };
            }

            if (type == "multi_select")
            {
                return new
                {
                    property = f.Property,
                    multi_select = new { contains = f.Value }
                };
            }

            // ------------------------------------------------------------
            // DEFAULT: treat as rich_text (title, rich_text, etc.)
            // ------------------------------------------------------------
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

                _ => throw new NotSupportedException(
                    $"Operator {f.Operator} not supported for property '{f.Property}'")
            };
        }
    }
}
