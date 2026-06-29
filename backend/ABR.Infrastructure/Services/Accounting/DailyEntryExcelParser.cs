using System.Globalization;
using ClosedXML.Excel;

namespace ABR.Infrastructure.Services.Accounting;

internal sealed class ParsedDailyEntryImportRow
{
    public int RowNumber { get; set; }
    public DateOnly EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string MainLedgerName { get; set; } = string.Empty;
    public string SubLedgerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}

internal static class DailyEntryExcelParser
{
    private static readonly string[] RequiredHeaders =
    [
        "DATE", "A/J", "MAIN", "SUB", "REMARK", "CR", "DR"
    ];

    public static (IReadOnlyList<ParsedDailyEntryImportRow> ValidRows, IReadOnlyList<(int RowNumber, string Message)> RowErrors)
        ParseImport(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var headerRow = FindHeaderRow(ws);
        if (headerRow is null)
            throw new InvalidOperationException("Header row not found. Expected a row starting with DATE.");

        var headerMap = BuildHeaderMap(ws, headerRow.Value);
        ValidateHeaders(headerMap);

        var validRows = new List<ParsedDailyEntryImportRow>();
        var errors = new List<(int RowNumber, string Message)>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow.Value;

        for (var row = headerRow.Value + 1; row <= lastRow; row++)
        {
            if (IsEmptyRow(ws, row, headerMap))
                continue;

            var rowResult = ParseDataRow(ws, row, headerMap);
            if (rowResult.Error is not null)
                errors.Add((row, rowResult.Error));
            else if (rowResult.Row is not null)
                validRows.Add(rowResult.Row);
        }

        if (validRows.Count == 0 && errors.Count == 0)
            throw new InvalidOperationException("No data rows found below the header row.");

        return (validRows, errors);
    }

    public static byte[] BuildSampleWorkbook()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import");
        ws.Cell(1, 1).Value = "Daily Entry Import Template";
        ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold();

        for (var i = 0; i < RequiredHeaders.Length; i++)
            ws.Cell(2, i + 1).Value = RequiredHeaders[i];

        StyleHeaderRow(ws, 2);

        ws.Cell(3, 1).Value = "01-02-2026";
        ws.Cell(3, 2).Value = "A";
        ws.Cell(3, 3).Value = "UNN";
        ws.Cell(3, 4).Value = "PARTH";
        ws.Cell(3, 5).Value = "1 ST INS PETE";
        ws.Cell(3, 6).Value = 100.000m;
        ws.Cell(3, 7).Value = string.Empty;

        ws.Cell(4, 1).Value = "01-02-2026";
        ws.Cell(4, 2).Value = "J";
        ws.Cell(4, 3).Value = "INT";
        ws.Cell(4, 4).Value = "PARTH";
        ws.Cell(4, 5).Value = "1% 6 MONTH MATE";
        ws.Cell(4, 6).Value = string.Empty;
        ws.Cell(4, 7).Value = 100.000m;

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public static byte[] BuildExportWorkbook(IReadOnlyList<LedgerExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Ledger");
        var headers = new[] { "DATE", "A/J", "MAIN", "SUB", "REMARK", "CR", "DR", "BAL" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        StyleHeaderRow(ws, 1);

        var rowNum = 2;
        decimal balance = 0;
        foreach (var row in rows)
        {
            var cr = row.EntryType == "aavak" ? row.Amount : 0m;
            var dr = row.EntryType == "javak" ? row.Amount : 0m;
            balance += cr - dr;

            ws.Cell(rowNum, 1).Value = row.EntryDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            ws.Cell(rowNum, 2).Value = row.EntryType == "aavak" ? "A" : "J";
            ws.Cell(rowNum, 3).Value = row.MainLedgerName;
            ws.Cell(rowNum, 4).Value = row.SubLedgerName;
            ws.Cell(rowNum, 5).Value = row.Description ?? string.Empty;
            ws.Cell(rowNum, 6).Value = cr > 0 ? cr : string.Empty;
            ws.Cell(rowNum, 7).Value = dr > 0 ? dr : string.Empty;
            ws.Cell(rowNum, 8).Value = balance;
            ws.Cell(rowNum, 6).Style.NumberFormat.Format = "0.000";
            ws.Cell(rowNum, 7).Style.NumberFormat.Format = "0.000";
            ws.Cell(rowNum, 8).Style.NumberFormat.Format = "0.000";
            rowNum++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static int? FindHeaderRow(IXLWorksheet ws)
    {
        var lastRow = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 20, 20);
        for (var row = 1; row <= lastRow; row++)
        {
            var first = ws.Cell(row, 1).GetString().Trim();
            if (string.Equals(first, "DATE", StringComparison.OrdinalIgnoreCase))
                return row;
        }

        return null;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet ws, int headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 10;
        for (var col = 1; col <= lastCol; col++)
        {
            var header = ws.Cell(headerRow, col).GetString().Trim();
            if (string.IsNullOrWhiteSpace(header))
                continue;
            map[header] = col;
        }

        return map;
    }

    private static void ValidateHeaders(Dictionary<string, int> headerMap)
    {
        if (headerMap.ContainsKey("CLOSE"))
            throw new InvalidOperationException("CLOSE column is no longer supported. Remove it and use: DATE, A/J, MAIN, SUB, REMARK, CR, DR.");

        foreach (var required in RequiredHeaders)
        {
            if (!headerMap.ContainsKey(required))
                throw new InvalidOperationException($"Missing required column: {required}.");
        }

        for (var i = 0; i < RequiredHeaders.Length; i++)
        {
            if (!headerMap.TryGetValue(RequiredHeaders[i], out var col) || col != i + 1)
            {
                // Allow BAL after DR but required headers must exist in order for first 7 columns
                // Relaxed: only check presence, not strict order except DATE must be col A
                break;
            }
        }

        if (!headerMap.TryGetValue("DATE", out var dateCol) || dateCol != 1)
            throw new InvalidOperationException("DATE must be the first column.");
    }

    private static bool IsEmptyRow(IXLWorksheet ws, int row, Dictionary<string, int> headerMap)
    {
        foreach (var key in RequiredHeaders)
        {
            if (!headerMap.TryGetValue(key, out var col))
                continue;
            if (!string.IsNullOrWhiteSpace(ws.Cell(row, col).GetString()))
                return false;
            if (ws.Cell(row, col).TryGetValue<decimal>(out var num) && num != 0)
                return false;
        }

        return true;
    }

    private static (ParsedDailyEntryImportRow? Row, string? Error) ParseDataRow(IXLWorksheet ws, int row, Dictionary<string, int> headerMap)
    {
        if (!TryParseDate(ws.Cell(row, headerMap["DATE"]), out var entryDate))
            return (null, "Invalid DATE.");

        var aj = ws.Cell(row, headerMap["A/J"]).GetString().Trim();
        string entryType;
        if (string.Equals(aj, "A", StringComparison.OrdinalIgnoreCase))
            entryType = "aavak";
        else if (string.Equals(aj, "J", StringComparison.OrdinalIgnoreCase))
            entryType = "javak";
        else
            return (null, "A/J must be A or J.");

        var main = ws.Cell(row, headerMap["MAIN"]).GetString().Trim();
        if (string.IsNullOrWhiteSpace(main))
            return (null, "MAIN is required.");

        var sub = ws.Cell(row, headerMap["SUB"]).GetString().Trim();
        if (string.IsNullOrWhiteSpace(sub))
            sub = "General";

        var remark = ws.Cell(row, headerMap["REMARK"]).GetString().Trim();
        var cr = ParseAmount(ws.Cell(row, headerMap["CR"]));
        var dr = ParseAmount(ws.Cell(row, headerMap["DR"]));

        if (cr > 0 && dr > 0)
            return (null, "Only one of CR or DR may have a value.");
        if (cr <= 0 && dr <= 0)
            return (null, "Either CR or DR must be greater than zero.");

        var amount = entryType == "aavak" ? cr : dr;
        if (amount <= 0)
            return (null, "Amount must be greater than zero.");

        return (new ParsedDailyEntryImportRow
        {
            RowNumber = row,
            EntryDate = entryDate,
            EntryType = entryType,
            MainLedgerName = main,
            SubLedgerName = sub,
            Description = string.IsNullOrWhiteSpace(remark) ? null : remark,
            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero)
        }, null);
    }

    private static bool TryParseDate(IXLCell cell, out DateOnly date)
    {
        if (cell.DataType == XLDataType.DateTime)
        {
            date = DateOnly.FromDateTime(cell.GetDateTime());
            return true;
        }

        if (cell.TryGetValue<double>(out var serial) && serial > 0)
        {
            try
            {
                date = DateOnly.FromDateTime(DateTime.FromOADate(serial));
                return true;
            }
            catch
            {
                // fall through
            }
        }

        var text = cell.GetString().Trim();
        if (DateOnly.TryParseExact(text, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;
        if (DateOnly.TryParseExact(text, "d-M-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;
        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        date = default;
        return false;
    }

    private static decimal ParseAmount(IXLCell cell)
    {
        if (cell.TryGetValue<decimal>(out var value))
            return value;
        if (cell.TryGetValue<double>(out var d))
            return (decimal)d;

        var text = cell.GetString().Trim().Replace(",", string.Empty);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
    }

    private static void StyleHeaderRow(IXLWorksheet ws, int row)
    {
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 8;
        var range = ws.Range(row, 1, row, lastCol);
        range.Style.Font.SetBold();
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F4F8");
    }
}

internal sealed class LedgerExportRow
{
    public DateOnly EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string MainLedgerName { get; set; } = string.Empty;
    public string SubLedgerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}
