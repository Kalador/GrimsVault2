using System.Text.RegularExpressions;

namespace Vault.Core
{
    public static class Helpers
    {
        /// <summary>
        /// Returns an age string for a given DateTime. If the DateTime is in the future, it will return "just now". If the DateTime is more than 7 days old, it will return the date in "dd/MM/yy" format. Otherwise, it will return a string representing the age in minutes, hours, or days.
        /// </summary>
        /// <param name="ts">The DateTime to format.</param>
        /// <returns>A string representing the age of the DateTime.</returns>
        public static string FormatAge(this DateTime ts)
        {
            if (ts == DateTime.MinValue)
                return "never";

            var diff = DateTime.UtcNow - ts.ToUniversalTime();

            if (diff.TotalSeconds < 60)
                return "just now";

            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} mins";

            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hrs";

            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} days";

            return ts.ToLocalTime().ToString("dd/MM/yy");
        }

        /// <summary>
        /// Formats Decical Prices into natural current format.
        /// </summary>
        /// <param name="price">Decimal Price</param>
        public static string FormatPrice(this decimal price)
        {
            return price % 1 == 0
                ? $"£{price:0}"
                : $"£{price:0.00}";
        }

        /// <summary>
        /// Safely parses a nullable string into a DateTime.
        /// Returns null if parsing fails.
        /// </summary>
        public static DateTime? ToDate(this string? rawDate)
        {
            return DateTime.TryParse(rawDate, out var parsed) ? parsed : null;
        }

        /// <summary>
        /// Removes HTML Editing from a text block.
        /// Handy for cleaning Webpage info blocks into plaint text
        /// </summary>
        public static string Sanitize(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Strip HTML tags
            var noHtml = Regex.Replace(input, "<.*?>", string.Empty);

            // Normalize whitespace
            var normalized = Regex.Replace(noHtml, @"\s+", " ").Trim();

            return normalized;
        }

    }
}
