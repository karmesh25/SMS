using ABR.Application.DTOs.Accounting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ABR.Infrastructure.Services.Accounting;

internal static class DailyEntryLedgerPdfBuilder
{
    public static byte[] Build(IReadOnlyList<LedgerExportRow> rows, string siteName)
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
                    col.Item().Text("Daily Entry Ledger").Bold().FontSize(14);
                    col.Item().Text($"Site: {siteName}").FontSize(9);
                    col.Item().Text($"Generated: {DateTime.UtcNow:dd-MM-yyyy HH:mm} UTC").FontSize(7).Italic();
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);
                        columns.ConstantColumn(28);
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.1f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("DATE");
                        header.Cell().Element(HeaderCell).Text("A/J");
                        header.Cell().Element(HeaderCell).Text("MAIN");
                        header.Cell().Element(HeaderCell).Text("SUB");
                        header.Cell().Element(HeaderCell).Text("REMARK");
                        header.Cell().Element(HeaderCell).AlignRight().Text("CR");
                        header.Cell().Element(HeaderCell).AlignRight().Text("DR");
                        header.Cell().Element(HeaderCell).AlignRight().Text("BAL");
                    });

                    decimal balance = 0;
                    foreach (var row in rows)
                    {
                        var cr = row.EntryType == "aavak" ? row.Amount : 0m;
                        var dr = row.EntryType == "javak" ? row.Amount : 0m;
                        balance += cr - dr;

                        table.Cell().Element(BodyCell).Text(row.EntryDate.ToString("dd-MM-yyyy"));
                        table.Cell().Element(BodyCell).Text(row.EntryType == "aavak" ? "A" : "J");
                        table.Cell().Element(BodyCell).Text(row.MainLedgerName);
                        table.Cell().Element(BodyCell).Text(row.SubLedgerName);
                        table.Cell().Element(BodyCell).Text(row.Description ?? string.Empty);
                        table.Cell().Element(BodyCell).AlignRight().Text(cr > 0 ? FormatAmount(cr) : string.Empty);
                        table.Cell().Element(BodyCell).AlignRight().Text(dr > 0 ? FormatAmount(dr) : string.Empty);
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatAmount(balance));
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.DefaultTextStyle(x => x.SemiBold()).Padding(2).BorderBottom(1).BorderColor(Colors.Grey.Medium);

    private static IContainer BodyCell(IContainer container) =>
        container.PaddingVertical(2).PaddingHorizontal(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);

    private static string FormatAmount(decimal amount) =>
        amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
}
