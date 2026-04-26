using Vault.Core;

namespace Vault.Data.Notion;

public class NotionDataHandler(NotionClient client, IConfiguration config) : IDataHandler
{

    private readonly NotionClient _client = client;
    private readonly string _databaseId = config["Notion:NotionDatabaseId"]!;
    private readonly string _storeDatabaseId = config["Notion:NotionStoreDatabaseId"]!;

         public async Task<List<VaultItem>> GetItemsAsync(int? pageSize = null)
        {

            var endpoint = $"/databases/{_databaseId}/query";
            var payload = new { };
            var json = await _client.PostAsync(endpoint,payload,pageSize);

            return [.. json.RootElement
                .GetProperty("results")
                .EnumerateArray()
                .Select(NotionMapper.MapVaultItem)];
        }
}
