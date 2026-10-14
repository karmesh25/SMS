using System.Globalization;
using ClosedXML.Excel;
using ABR.Infrastructure.Formatting;

namespace ABR.Infrastructure.Services.Reports;

internal static class ReportExportHelpers
{
    public const int MaxExportRows = 50_000;
    public const string HeaderBlue = "#1F4E79";

    public static string FormatIndianAmount(decimal? amount) => DisplayAmountFormatter.Format(amount);

    public static string FormatIndianAmount(decimal amount) => DisplayAmountFormatter.Format(amount);

    /// <summary>Non-money quantities (e.g. sqft) — no ÷1000 obfuscation.</summary>
    public static string FormatPlainNumber(decimal? amount)
    {
        if (!amount.HasValue) return string.Empty;
        return amount.Value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static string FormatPlainNumber(decimal amount) =>
        amount.ToString("0.##", CultureInfo.InvariantCulture);

    public static string FormatDate(DateOnly? date) => date?.ToString("dd-MM-yyyy") ?? string.Empty;

    public static string FormatDate(DateOnly date) => date.ToString("dd-MM-yyyy");

    public static string BuildFileName(string reportType, string siteName, string extension)
    {
        var safeSite = SanitizeFileName(siteName);
        var date = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
        return $"{reportType}-{safeSite}-{date}.{extension}";
    }

    public static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(' ', '-');
    }

    public static void ApplyHeaderStyle(IXLRange row)
    {
        row.Style.Font.Bold = true;
        row.Style.Font.FontColor = XLColor.White;
        row.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBlue);
        row.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    public static void ApplyAlternatingRow(IXLRange row, int dataRowIndex)
    {
        if (dataRowIndex % 2 == 0)
            row.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
    }

    public static void WriteExcelHeaderBlock(IXLWorksheet ws, ref int row, string title, string siteName, IReadOnlyList<string> filterLines)
    {
        ws.Cell(row, 1).Value = title;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row++;
        ws.Cell(row, 1).Value = $"Site: {siteName}";
        row++;
        foreach (var line in filterLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                ws.Cell(row, 1).Value = line;
                row++;
            }
        }

        ws.Cell(row, 1).Value = $"Generated: {DateTime.UtcNow:dd-MM-yyyy HH:mm} UTC";
        row += 2;
    }
}
