namespace Vault.Data.Notion;

public static class NotionRichTextExtensions
{
    private const int NotionTextLimit = 2000;

    public static object ToChunkedRichText(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new
            {
                rich_text = Array.Empty<object>()
            };
        }

        var chunks = Chunk(text, NotionTextLimit);

        return new
        {
            rich_text = chunks.Select(chunk => new
            {
                type = "text",
                text = new { content = chunk }
            }).ToArray()
        };
    }

    private static IEnumerable<string> Chunk(string text, int size)
    {
        for (int i = 0; i < text.Length; i += size)
        {
            yield return text.Substring(i, Math.Min(size, text.Length - i));
        }
    }
}
