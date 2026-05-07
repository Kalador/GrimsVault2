namespace Vault.Features.Cex
{
    public static class CexHelpers
    {
        public static string BuildCexUrl(string? boxId)
        {
            if (string.IsNullOrWhiteSpace(boxId))
                return "https://uk.webuy.com";

            return $"https://uk.webuy.com/product-detail?id={boxId}";
        }
    }
}
