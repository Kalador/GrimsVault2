using System.Text;

namespace Vault.Features.Cex;

public static class CexEmailBuilder
{
    public static string BuildDailyCexHtmlSubject()
    {
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return $"CeX Stock Check — {date}";
    }

    public static string BuildDailyCexHtmlEmail(
        IEnumerable<CexEmailItem> items,
        string summaryText
    )
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");

        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; background:#ffffff; color:#000; padding:20px; }");
        sb.AppendLine(".header { font-size:24px; font-weight:bold; margin-bottom:5px; }");
        sb.AppendLine(".timestamp { color:#444; font-size:13px; margin-bottom:4px; }");
        sb.AppendLine(".viewall { font-size:13px; margin-bottom:20px; }");

        sb.AppendLine(".card-table {");
        sb.AppendLine("  width:288px;");
        sb.AppendLine("  height:150px;");
        sb.AppendLine("  border:1px solid #ddd;");
        sb.AppendLine("  border-radius:8px;");
        sb.AppendLine("  box-shadow:0 1px 4px rgba(0,0,0,0.15);");
        sb.AppendLine("  background:#fff;");
        sb.AppendLine("  margin:0 auto;");
        sb.AppendLine("}");
        sb.AppendLine(".card-inner { padding:12px; height:150px; }");

        sb.AppendLine(".title { font-size:14px; font-weight:bold; }");
        sb.AppendLine(".price { font-size:14px; font-weight:bold; margin-top:4px; }");
        sb.AppendLine(".stock { font-size:12px; color:#444; margin-top:4px; }");
        sb.AppendLine(".scan { font-size:11px; color:#777; margin-top:4px; }");
        sb.AppendLine(".view { font-size:12px; margin-top:8px; color:#0066cc; text-decoration:underline; }");

        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<div class=\"header\">CeX Stock Check</div>");
        sb.AppendLine($"<div class=\"timestamp\">Generated: {timestamp} UTC</div>");
        sb.AppendLine("<div class=\"viewall\"><a href=\"/cex\" style=\"color:#0066cc; text-decoration:underline;\">View All</a></div>");

        sb.AppendLine("<table width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\">");

        int col = 0;
        foreach (var item in items)
        {
            if (col == 0)
                sb.AppendLine("<tr>");

            sb.AppendLine("<td width=\"33%\" valign=\"top\" style=\"padding:12px;\">");

            var stockSummary = StripHtml(item.StockSummaryHtml ?? "");

            sb.AppendLine("<table class=\"card-table\" role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\">");
            sb.AppendLine("<tr><td class=\"card-inner\" valign=\"middle\">");

            sb.AppendLine("<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\" height=\"100%\" border=\"0\">");
            sb.AppendLine("<tr>");

            sb.AppendLine("<td width=\"60\" valign=\"middle\">");
            if (!string.IsNullOrWhiteSpace(item.ImageUrl))
            {
                sb.AppendLine($"<img src=\"{item.ImageUrl}\" width=\"60\" height=\"60\" style=\"display:block; object-fit:contain; background:#eee; border-radius:4px;\" alt=\"{item.BoxName}\" />");
            }
            else
            {
                sb.AppendLine("<div style=\"width:60px; height:60px; background:#eee; border-radius:4px;\"></div>");
            }
            sb.AppendLine("</td>");

            sb.AppendLine("<td valign=\"top\" style=\"padding-left:12px;\">");
            sb.AppendLine($"<div class=\"title\">{item.BoxName}</div>");
            sb.AppendLine($"<div class=\"price\">£{item.SellPriceFormatted}</div>");
            sb.AppendLine($"<div class=\"stock\">{stockSummary}</div>");
            sb.AppendLine($"<div class=\"scan\">Last Scan: {item.LastScanAge}</div>");
            sb.AppendLine($"<div class=\"view\"><a href=\"{item.CexUrl}\" style=\"color:#0066cc; text-decoration:underline;\">View on CeX</a></div>");
            sb.AppendLine("</td>");

            sb.AppendLine("</tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("</td>");

            col++;
            if (col == 3)
            {
                sb.AppendLine("</tr>");
                col = 0;
            }
        }

        if (col != 0)
        {
            while (col < 3)
            {
                sb.AppendLine("<td></td>");
                col++;
            }
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var array = new char[html.Length];
        int arrayIndex = 0;
        bool inside = false;

        foreach (char let in html)
        {
            if (let == '<') { inside = true; continue; }
            if (let == '>') { inside = false; continue; }
            if (!inside) { array[arrayIndex++] = let; }
        }

        return new string(array, 0, arrayIndex).Trim();
    }
}

/// <summary>
/// A lightweight DTO used exclusively for generating CeX email reports.
/// This is NOT a domain model and NOT a UI model.
/// It contains only the formatted fields needed by the EmailBuilder.
/// </summary>
public class CexEmailItem
{
    public string BoxName { get; set; } = "";
    public string SellPriceFormatted { get; set; } = "";
    public string StockSummaryHtml { get; set; } = "";
    public string LastScanAge { get; set; } = "";
    public string CexUrl { get; set; } = "";
    public string? ImageUrl { get; set; }
}
