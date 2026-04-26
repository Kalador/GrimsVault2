using System.Text.Json;

namespace Vault.Data.Notion;

/// <summary>
/// Provides strongly-typed extraction helpers for reading Notion JSON properties.
/// 
/// Responsibilities:
/// - Read raw Notion JSON (JsonElement)
/// - Extract primitive values (string, int, bool, DateTime, lists)
/// - Handle Notion-specific property structures (title, rich_text, rollup, files, etc.)
/// - Fail safely when properties are missing, null, or in unexpected formats
/// 
/// This class performs no business logic and constructs no domain models.
/// </summary>
internal static class NotionParser
{
    // ------------------------------------------------------------
    //  SELECT
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts the selected option's name from a Notion "select" property.
    /// Returns null if the property is missing, null, or not an object.
    /// </summary>
    public static string? ExtractSelectName(JsonElement props, string key)
    {
        if (!props.TryGetProperty(key, out var prop))
            return null;

        if (!prop.TryGetProperty("select", out var select))
            return null;

        if (select.ValueKind != JsonValueKind.Object)
            return null;

        if (!select.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
            return null;

        return name.GetString();
    }

    // ------------------------------------------------------------
    //  CHECKBOX
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts a boolean from a Notion "checkbox" property.
    /// Returns false if missing or invalid.
    /// </summary>
    public static bool ExtractCheckbox(JsonElement props, string key)
    {
        if (!props.TryGetProperty(key, out var prop))
            return false;

        if (!prop.TryGetProperty("checkbox", out var checkbox))
            return false;

        return checkbox.ValueKind == JsonValueKind.True;
    }

    // ------------------------------------------------------------
    //  NUMBER
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts a number from a Notion "number" property.
    /// Returns null if missing or not numeric.
    /// </summary>
    public static int? ExtractNumber(JsonElement props, string key)
    {
        if (!props.TryGetProperty(key, out var prop))
            return null;

        if (!prop.TryGetProperty("number", out var number))
            return null;

        return number.ValueKind == JsonValueKind.Number
            ? number.GetInt32()
            : null;
    }

    // ------------------------------------------------------------
    //  TITLE
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts the first title block's text content from a Notion "title" property.
    /// Returns null if missing or empty.
    /// </summary>
    public static string? ExtractTitle(JsonElement props, string key)
    {
        if (!props.TryGetProperty(key, out var prop))
            return null;

        if (!prop.TryGetProperty("title", out var titleArr))
            return null;

        if (titleArr.ValueKind != JsonValueKind.Array)
            return null;

        var first = GetFirstArrayElement(titleArr);
        if (first == null)
            return null;

        return first.Value.TryGetProperty("text", out var text) &&
               text.TryGetProperty("content", out var content) &&
               content.ValueKind == JsonValueKind.String
            ? content.GetString()
            : null;
    }

    // ------------------------------------------------------------
    //  RICH TEXT
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts the first rich text block's content from a Notion "rich_text" property.
    /// Returns null if missing or empty.
    /// </summary>
    public static string? ExtractRichText(JsonElement props, string key)
    {
        if (!props.TryGetProperty(key, out var prop))
            return null;

        if (!prop.TryGetProperty("rich_text", out var richArr))
            return null;

        if (richArr.ValueKind != JsonValueKind.Array)
            return null;

        var first = GetFirstArrayElement(richArr);
        if (first == null)
            return null;

        return first.Value.TryGetProperty("text", out var text) &&
               text.TryGetProperty("content", out var content) &&
               content.ValueKind == JsonValueKind.String
            ? content.GetString()
            : null;
    }

    // ------------------------------------------------------------
    //  DATE
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts the start date from a Notion "date" property.
    /// Returns null if missing, null, or invalid.
    /// </summary>
    public static DateTime? ExtractDate(JsonElement props, string key)
    {
        if (!props.TryGetProperty(key, out var prop))
            return null;

        if (!prop.TryGetProperty("date", out var dateObj))
            return null;

        if (dateObj.ValueKind != JsonValueKind.Object)
            return null;

        if (!dateObj.TryGetProperty("start", out var start) ||
            start.ValueKind != JsonValueKind.String)
            return null;

        return DateTime.TryParse(start.GetString(), out var parsed)
            ? parsed
            : null;
    }

    // ------------------------------------------------------------
    //  FILES
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts all URLs from a Notion "files" property.
    /// Supports both "file" and "external" types.
    /// Returns an empty list if missing or invalid.
    /// </summary>
    public static List<string> ExtractMediaUrls(JsonElement props, string key)
    {
        var urls = new List<string>();

        if (!props.TryGetProperty(key, out var prop))
            return urls;

        if (!prop.TryGetProperty("files", out var files))
            return urls;

        if (files.ValueKind != JsonValueKind.Array)
            return urls;

        foreach (var f in files.EnumerateArray())
        {
            if (f.TryGetProperty("file", out var fileObj) &&
                fileObj.TryGetProperty("url", out var fileUrl) &&
                fileUrl.ValueKind == JsonValueKind.String)
            {
                urls.Add(fileUrl.GetString()!);
            }
            else if (f.TryGetProperty("external", out var extObj) &&
                     extObj.TryGetProperty("url", out var extUrl) &&
                     extUrl.ValueKind == JsonValueKind.String)
            {
                urls.Add(extUrl.GetString()!);
            }
        }

        return urls;
    }

    // ------------------------------------------------------------
    //  FORMULA
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts a string from a Notion "formula" property.
    /// Only returns string formulas.
    /// </summary>
    public static string? ExtractFormulaString(JsonElement props, string key)
    {
        if (!props.TryGetProperty(key, out var prop))
            return null;

        if (!prop.TryGetProperty("formula", out var formula))
            return null;

        return formula.TryGetProperty("string", out var str) &&
               str.ValueKind == JsonValueKind.String
            ? str.GetString()
            : null;
    }

    // ------------------------------------------------------------
    //  ROLLUP (TEXT)
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts the first rich text content from a Notion rollup array.
    /// Used for 1:1 relations.
    /// </summary>
    public static string? ExtractRollupText(JsonElement props, string key)
    {
        if (!props.TryGetProperty(key, out var prop))
            return null;

        if (!prop.TryGetProperty("rollup", out var rollup))
            return null;

        if (!rollup.TryGetProperty("array", out var array) ||
            array.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in array.EnumerateArray())
        {
            if (item.TryGetProperty("rich_text", out var richArr) &&
                richArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var rich in richArr.EnumerateArray())
                {
                    if (rich.TryGetProperty("text", out var text) &&
                        text.TryGetProperty("content", out var content) &&
                        content.ValueKind == JsonValueKind.String)
                    {
                        return content.GetString();
                    }
                }
            }
        }

        return null;
    }

    // ------------------------------------------------------------
    //  ROLLUP (TITLE)
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts the first title content from a Notion rollup array.
    /// Used for 1:1 relations.
    /// </summary>
    public static string? ExtractRollupTitle(JsonElement props, string key)
    {
        if (!props.TryGetProperty(key, out var prop))
            return null;

        if (!prop.TryGetProperty("rollup", out var rollup))
            return null;

        if (!rollup.TryGetProperty("array", out var array) ||
            array.ValueKind != JsonValueKind.Array ||
            array.GetArrayLength() == 0)
            return null;

        var first = array[0];

        if (first.TryGetProperty("title", out var titleArr) &&
            titleArr.ValueKind == JsonValueKind.Array &&
            titleArr.GetArrayLength() > 0)
        {
            var titleItem = titleArr[0];

            if (titleItem.TryGetProperty("text", out var text) &&
                text.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }
        }

        return null;
    }

    // ------------------------------------------------------------
    //  ROLLUP (RICH TEXT ARRAY)
    // ------------------------------------------------------------

    /// <summary>
    /// Extracts all rich text values from a Notion rollup array.
    /// Used for multi-value relations.
    /// </summary>
    public static List<string> ExtractRollupRichTextArray(JsonElement props, string key)
    {
        var results = new List<string>();

        if (!props.TryGetProperty(key, out var prop))
            return results;

        if (!prop.TryGetProperty("rollup", out var rollup))
            return results;

        if (!rollup.TryGetProperty("array", out var array) ||
            array.ValueKind != JsonValueKind.Array)
            return results;

        foreach (var item in array.EnumerateArray())
        {
            if (item.TryGetProperty("rich_text", out var richArr) &&
                richArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var rich in richArr.EnumerateArray())
                {
                    if (rich.TryGetProperty("text", out var text) &&
                        text.TryGetProperty("content", out var content) &&
                        content.ValueKind == JsonValueKind.String)
                    {
                        var value = content.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            results.Add(value);
                    }
                }
            }
        }

        return results;
    }

    // ------------------------------------------------------------
    //  HELPERS
    // ------------------------------------------------------------

    /// <summary>
    /// Returns the first element of a JSON array, or null if empty.
    /// </summary>
    private static JsonElement? GetFirstArrayElement(JsonElement array)
    {
        return array.ValueKind == JsonValueKind.Array && array.GetArrayLength() > 0
            ? array[0]
            : (JsonElement?)null;
    }
}
