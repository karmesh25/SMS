using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ABR.Infrastructure.Services.Accounting;

internal sealed class JournalVoucherExportRow
{
    public DateOnly VoucherDate { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public string? Narration { get; set; }
    public int LineNo { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string MainLedgerName { get; set; } = string.Empty;
    public string SubLedgerName { get; set; } = string.Empty;
}

internal static class JournalVoucherExportBuilder
{
    public static byte[] BuildExcel(IReadOnlyList<JournalVoucherExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("JV Ledger");
        var headers = new[] { "DATE", "VOUCHER NO", "LINE NO", "MAIN LEDGER", "SUB LEDGER", "ENTRY TYPE", "AMOUNT", "NARRATION" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var rowNum = 2;
        foreach (var row in rows)
        {
            ws.Cell(rowNum, 1).Value = row.VoucherDate.ToString("dd-MM-yyyy");
            ws.Cell(rowNum, 2).Value = row.VoucherNo;
            ws.Cell(rowNum, 3).Value = row.LineNo;
            ws.Cell(rowNum, 4).Value = row.MainLedgerName;
            ws.Cell(rowNum, 5).Value = row.SubLedgerName;
            ws.Cell(rowNum, 6).Value = row.EntryType.ToUpperInvariant();
            ws.Cell(rowNum, 7).Value = row.Amount;
            ws.Cell(rowNum, 8).Value = row.Narration ?? string.Empty;
            ws.Cell(rowNum, 7).Style.NumberFormat.Format = "0.00";
            rowNum++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public static byte[] BuildPdf(IReadOnlyList<JournalVoucherExportRow> rows, string siteName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text("Journal Voucher Ledger").Bold().FontSize(14);
                    col.Item().Text($"Site: {siteName}").FontSize(9);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.3f);
                        columns.ConstantColumn(38);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                        columns.ConstantColumn(44);
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(2f);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("DATE");
                        header.Cell().Element(HeaderCell).Text("VOUCHER NO");
                        header.Cell().Element(HeaderCell).AlignRight().Text("LINE");
                        header.Cell().Element(HeaderCell).Text("MAIN");
                        header.Cell().Element(HeaderCell).Text("SUB");
                        header.Cell().Element(HeaderCell).Text("TYPE");
                        header.Cell().Element(HeaderCell).AlignRight().Text("AMOUNT");
                        header.Cell().Element(HeaderCell).Text("NARRATION");
                    });

                    foreach (var row in rows)
                    {
                        table.Cell().Element(BodyCell).Text(row.VoucherDate.ToString("dd-MM-yyyy"));
                        table.Cell().Element(BodyCell).Text(row.VoucherNo);
                        table.Cell().Element(BodyCell).AlignRight().Text(row.LineNo.ToString());
                        table.Cell().Element(BodyCell).Text(row.MainLedgerName);
                        table.Cell().Element(BodyCell).Text(row.SubLedgerName);
                        table.Cell().Element(BodyCell).Text(row.EntryType.ToUpperInvariant());
                        table.Cell().Element(BodyCell).AlignRight().Text(row.Amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture));
                        table.Cell().Element(BodyCell).Text(row.Narration ?? string.Empty);
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.DefaultTextStyle(x => x.SemiBold()).Padding(2).BorderBottom(1).BorderColor(Colors.Grey.Medium);

    private static IContainer BodyCell(IContainer container) =>
        container.PaddingVertical(2).PaddingHorizontal(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
}
