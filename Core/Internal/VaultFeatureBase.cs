using System.Text.Json;

namespace Vault.Core;

public abstract class VaultFeatureBase
{
    protected static readonly HttpClient Http = new();

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected T? FromJson<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    protected async Task<T?> GetJsonAsync<T>(string url)
    {
        var json = await Http.GetStringAsync(url);
        return FromJson<T>(json);
    }

    protected async Task<T> TryAsync<T>(Func<Task<T>> action, Func<T> onError)
    {
        try
        {
            return await action();
        }
        catch
        {
            return onError();
        }
    }

    protected T CreateError<T>(Action<T> fill) where T : new()
    {
        var obj = new T();
        fill(obj);
        return obj;
    }
}
