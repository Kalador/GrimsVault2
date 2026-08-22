using System.Text.Json;
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
    /// - Update VaultItem records via PATCH operations
    /// - Create exclusion entries and update relation fields
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

        // Exclusions database
        private readonly string _exclusionDatabaseId = config["Notion:NotionExclusionDatabaseId"]!;

        // Cached property types for the main database
        private Dictionary<string, string>? _propertyTypes;

        // =====================================================================
        //  SCHEMA LOADER
        // =====================================================================

        private async Task EnsureSchemaLoadedAsync()
        {
            if (_propertyTypes != null)
                return;

            // Load primary DB schema
            var db = await _client.GetAsync($"/databases/{_databaseId}");

            _propertyTypes = db.RootElement
                .GetProperty("properties")
                .EnumerateObject()
                .ToDictionary(
                    p => p.Name,
                    p => p.Value.GetProperty("type").GetString()!
                );

            // Load secondary DB schema
            db = await _client.GetAsync($"/databases/{_storeDatabaseId}");

            foreach (var prop in db.RootElement.GetProperty("properties").EnumerateObject())
            {
                var name = prop.Name;
                var type = prop.Value.GetProperty("type").GetString()!;

                if (!_propertyTypes.ContainsKey(name))
                    _propertyTypes[name] = type;
            }
        }

        // =====================================================================
        //  ITEMS (READ)
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

            var json = await _client.PostQueryAsync(endpoint, payload, pageSize);

            return
            [
                .. json.RootElement
                    .GetProperty("results")
                    .EnumerateArray()
                    .Select(NotionMapper.MapVaultItem)
            ];
        }

        public async Task<VaultItem?> GetItemAsync(string pageId)
        {
            var json = await _client.GetAsync($"/pages/{pageId}");

            if (json is null || !json.RootElement.TryGetProperty("properties", out _))
                return null;

            return NotionMapper.MapVaultItem(json.RootElement);
        }

        // =====================================================================
        //  STORES (READ)
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

            var json = await _client.PostQueryAsync(endpoint, payload, pageSize);

            return
            [
                .. json.RootElement
                    .GetProperty("results")
                    .EnumerateArray()
                    .Select(NotionMapper.MapVaultStore)
            ];
        }

        // =====================================================================
        //  ITEMS (WRITE)
        // =====================================================================

        public async Task UpdateItemAsync(VaultItem item)
        {
            await EnsureSchemaLoadedAsync();

            var payload = NotionMapper.MapToNotionUpdatePayload(item, _propertyTypes!);

            var endpoint = $"/pages/{item.PageId}";
            await _client.PatchAsync(endpoint, payload);
        }

        public async Task UpdateItemsAsync(IEnumerable<VaultItem> items)
        {
            var semaphore = new SemaphoreSlim(4);

            var tasks = items.Select(async item =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await UpdateItemAsync(item);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        // =====================================================================
        //  EXCLUSIONS (WRITE)
        // =====================================================================

        /// <summary>
        /// Adds one or more listing exclusions for a given VaultItem.
        /// Creates exclusion pages and updates the item's relation field.
        /// </summary>
        public async Task AddListingExclusionsAsync(string pageID, IEnumerable<string> listingIds, string listingType)
        {
            foreach (var id in listingIds)
            {
                var payload = new
                {
                    parent = new { database_id = _exclusionDatabaseId },
                    properties = new Dictionary<string, object>
                    {
                        ["Listing ID"] = new
                        {
                            rich_text = new[]
                            {
                        new
                        {
                            type = "text",
                            text = new { content = id }
                        }
                    }
                        },
                        ["Listing Type"] = new
                        {
                            select = new { name = listingType }
                        },
                        ["Item"] = new
                        {
                            relation = new[]
                            {
                        new { id = pageID }
                    }
                        }
                    }
                };

                await _client.PostInsertAsync("/pages", payload);
            }
        }



        // =====================================================================
        //  PAYLOAD BUILDER (QUERY)
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

            var payload = new Dictionary<string, object>
            {
                ["filter"] = new { and = filters }
            };

            if (sorts.Count > 0)
                payload["sorts"] = sorts;

            return payload;
        }

        // =====================================================================
        //  FILTER BUILDER
        // =====================================================================

        private object BuildNotionFilter(QueryFilter f)
        {
            var type = _propertyTypes!.TryGetValue(f.Property, out var t)
                ? t
                : "rich_text";

            // BOOLEAN
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

            // DATE
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

            // NUMBER
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

            // SELECT
            if (type == "select")
            {
                return new
                {
                    property = f.Property,
                    select = new { equals = f.Value }
                };
            }

            // MULTI-SELECT
            if (type == "multi_select")
            {
                return new
                {
                    property = f.Property,
                    multi_select = new { contains = f.Value }
                };
            }

            // ROLLUP
            if (type == "rollup")
            {
                return f.Operator switch
                {
                    QueryOperator.Equals => new
                    {
                        property = f.Property,
                        rollup = new
                        {
                            any = new
                            {
                                number = new { equals = f.Value }
                            }
                        }
                    },

                    QueryOperator.NotEquals => new
                    {
                        property = f.Property,
                        rollup = new
                        {
                            any = new
                            {
                                number = new { does_not_equal = f.Value }
                            }
                        }
                    },

                    QueryOperator.NotEmpty => new
                    {
                        property = f.Property,
                        rollup = new
                        {
                            any = new
                            {
                                number = new { is_not_empty = true }
                            }
                        }
                    },

                    QueryOperator.Empty => new
                    {
                        property = f.Property,
                        rollup = new
                        {
                            any = new
                            {
                                number = new { is_empty = true }
                            }
                        }
                    },

                    _ => throw new NotSupportedException(
                        $"Operator {f.Operator} not supported for rollup property '{f.Property}'")
                };
            }

            // DEFAULT: rich_text
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
