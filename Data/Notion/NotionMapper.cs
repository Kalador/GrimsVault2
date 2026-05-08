using System.Text.Json;
using Vault.Core;

namespace Vault.Data.Notion
{
    /// <summary>
    /// Maps raw Notion JSON objects (pages) into strongly-typed <see cref="VaultItem"/> instances.
    /// 
    /// Responsibilities:
    /// - Accept a JsonElement representing a Notion page
    /// - Extract all known properties using <see cref="NotionParser"/>
    /// - Construct and return a fully populated VaultItem
    /// 
    /// </summary>
    internal static class NotionMapper
    {
        /// <summary>
        /// Converts a Notion page JSON object into a <see cref="VaultStore"/>.
        /// </summary>
        /// <param name="page">The JSON element representing the Notion page.</param>
        /// <returns>A fully populated <see cref="VaultStore"/> instance.</returns>
        public static VaultStore MapVaultStore(JsonElement page)
        {
            var props = page.GetProperty("properties");

            return new VaultStore
            {
                Name = NotionParser.ExtractTitle(props, "Name") ?? "",
                StoreType = NotionParser.ExtractSelectName(props, "StoreType") ?? "",
                IsPriority = NotionParser.ExtractCheckbox(props, "Priority"),
                Region = NotionParser.ExtractSelectName(props, "Region"),
                Number = NotionParser.ExtractNumber(props, "Number")
            };
        }

        /// <summary>
        /// Converts a Notion page JSON object into a <see cref="VaultItem"/>.
        /// </summary>
        public static VaultItem MapVaultItem(JsonElement page)
        {
            // Notion pages always contain a "properties" object.
            var props = page.TryGetProperty("properties", out var p)
                ? p
                : default;

            return new VaultItem
            {
                PageId = page.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                Name = NotionParser.ExtractTitle(props, "Name"),
                Media = NotionParser.ExtractMediaUrls(props, "Media"),
                RawgId = NotionParser.ExtractRichText(props, "RawgId"),
                ScrapeStatus = NotionParser.ExtractSelectName(props, "Scrape Status"),
                ReleaseDate = NotionParser.ExtractDate(props, "Release Date"),
                PlatformId = NotionParser.ExtractRollupText(props, "Platform ID"),
                PlatformName = NotionParser.ExtractRollupTitle(props, "Platform Name"),
                Description = NotionParser.ExtractRichText(props, "Description"),
                Publisher = NotionParser.ExtractRichText(props, "Publisher"),
                Developer = NotionParser.ExtractRichText(props, "Developer"),
                Scraped = NotionParser.ExtractDate(props, "Scraped"),
                SetName = NotionParser.ExtractSelectName(props, "Set Name"),
                Subset = NotionParser.ExtractSelectName(props, "Subset"),
                SortOrder = NotionParser.ExtractRichText(props, "SortOrder"),
                CexID = NotionParser.ExtractRichText(props, "CEX Product Code"),
                SearchEbay = NotionParser.ExtractCheckbox(props, "Search eBay"),
                ExcludedEbayIds = NotionParser.ExtractRollupRichTextArray(props, "Excluded eBay IDs")
            };
        }
    }
}
