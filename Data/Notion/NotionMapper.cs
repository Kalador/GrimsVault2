using System.Text.Json;
using Vault.Core;

namespace Vault.Data.Notion
{
    /// <summary>
    /// Maps raw Notion JSON objects (pages) into strongly-typed Vault models,
    /// and maps VaultItem instances back into Notion PATCH payloads.
    /// </summary>
    internal static class NotionMapper
    {
        // =====================================================================
        //  STORES (INBOUND)
        // =====================================================================

        /// <summary>
        /// Converts a Notion page JSON object into a VaultStore.
        /// </summary>
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

        // =====================================================================
        //  ITEMS (INBOUND)
        // =====================================================================

        /// <summary>
        /// Converts a Notion page JSON object into a VaultItem.
        /// </summary>
        public static VaultItem MapVaultItem(JsonElement page)
        {
            var props = page.TryGetProperty("properties", out var p)
                ? p
                : default;

            return new VaultItem
            {
                PageId = page.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                Name = NotionParser.ExtractTitle(props, "Name"),
                Media = NotionParser.ExtractMediaUrls(props, "Media"),
                RawgId = NotionParser.ExtractRichText(props, "RAWG ID"),
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
                SortOrder = NotionParser.ExtractRichText(props, "Sort Order"),
                CexID = NotionParser.ExtractRichText(props, "CEX Product Code"),
                SearchEbay = NotionParser.ExtractCheckbox(props, "Search eBay"),
                ExcludedEbayIds = NotionParser.ExtractRollupRichTextArray(props, "Excluded eBay IDs")
            };
        }

        // =====================================================================
        //  ITEMS (OUTBOUND)
        // =====================================================================

        /// <summary>
        /// Builds a Notion PATCH payload for updating a VaultItem.
        /// Only writable fields are included.
        /// Rollups and read-only fields are automatically skipped.
        /// </summary>
        public static object MapToNotionUpdatePayload(
            VaultItem item,
            Dictionary<string, string> propertyTypes)
        {
            var props = new Dictionary<string, object>();

            // ------------------------------------------------------------
            // TITLE: Name
            // ------------------------------------------------------------
            if (item.Name != null && propertyTypes.ContainsKey("Name"))
            {
                props["Name"] = new
                {
                    title = new[]
                    {
                        new { type = "text", text = new { content = item.Name } }
                    }
                };
            }

            // ------------------------------------------------------------
            // RICH TEXT: RAWG ID
            // ------------------------------------------------------------
            if (item.RawgId != null && propertyTypes.ContainsKey("RAWG ID"))
            {
                props["RAWG ID"] = new
                {
                    rich_text = new[]
                    {
                        new { type = "text", text = new { content = item.RawgId } }
                    }
                };
            }

            // ------------------------------------------------------------
            // SELECT: Scrape Status
            // ------------------------------------------------------------
            if (item.ScrapeStatus != null && propertyTypes.ContainsKey("Scrape Status"))
            {
                props["Scrape Status"] = new
                {
                    select = new { name = item.ScrapeStatus }
                };
            }

            // ------------------------------------------------------------
            // DATE: Release Date
            // ------------------------------------------------------------
            if (item.ReleaseDate.HasValue && propertyTypes.ContainsKey("Release Date"))
            {
                props["Release Date"] = new
                {
                    date = new { start = item.ReleaseDate.Value.ToString("yyyy-MM-dd") }
                };
            }

            // ------------------------------------------------------------
            // RICH TEXT: Description
            // ------------------------------------------------------------
            if (item.Description != null && propertyTypes.ContainsKey("Description"))
            {
                props["Description"] = item.Description.ToChunkedRichText();

            }

            // ------------------------------------------------------------
            // RICH TEXT: Publisher
            // ------------------------------------------------------------
            if (item.Publisher != null && propertyTypes.ContainsKey("Publisher"))
            {
                props["Publisher"] = new
                {
                    rich_text = new[]
                    {
                        new { type = "text", text = new { content = item.Publisher } }
                    }
                };
            }

            // ------------------------------------------------------------
            // RICH TEXT: Developer
            // ------------------------------------------------------------
            if (item.Developer != null && propertyTypes.ContainsKey("Developer"))
            {
                props["Developer"] = new
                {
                    rich_text = new[]
                    {
                        new { type = "text", text = new { content = item.Developer } }
                    }
                };
            }

            // ------------------------------------------------------------
            // DATE: Scraped
            // ------------------------------------------------------------
            if (item.Scraped.HasValue && propertyTypes.ContainsKey("Scraped"))
            {
                props["Scraped"] = new
                {
                    date = new { start = item.Scraped.Value.ToString("yyyy-MM-ddTHH:mm:ssK") }
                };
            }



            // ------------------------------------------------------------
            // FILES: Media
            // ------------------------------------------------------------
            if (item.Media != null && propertyTypes.ContainsKey("Media"))
            {
                props["Media"] = new
                {
                    files = item.Media.Select(url => new
                    {
                        type = "external",
                        name = "screenshot",
                        external = new { url }
                    }).ToArray()
                };
            }

            // ------------------------------------------------------------
            // NOTE: Rollups cannot be written:
            // - Platform ID
            // - Platform Name
            // - Excluded eBay IDs
            // ------------------------------------------------------------

            return new
            {
                properties = props
            };
        }
    }
}
