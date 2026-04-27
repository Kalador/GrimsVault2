using Vault.Core;
namespace Vault.Data.Notion;

public class NotionDataHandler(NotionClient client, IConfiguration config) : IDataHandler
{
    private readonly NotionClient _client = client;
    private readonly string _databaseId = config["Notion:NotionDatabaseId"]!;
    private readonly string _storeDatabaseId = config["Notion:NotionStoreDatabaseId"]!;

    /// <summary>
    /// Loads items from the Notion database.
    /// Optional QueryDefinition applies AND filters.
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

        return [.. json.RootElement
            .GetProperty("results")
            .EnumerateArray()
            .Select(NotionMapper.MapVaultItem)];
    }

    // ------------------------------------------------------------
    // Payload builder
    // ------------------------------------------------------------

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

    private object BuildNotionFilter(QueryFilter f)
    {
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
