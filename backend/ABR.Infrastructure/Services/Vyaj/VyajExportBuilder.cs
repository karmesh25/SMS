using ABR.Application.DTOs.Vyaj;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ABR.Infrastructure.Services.Vyaj;

internal static class VyajExportBuilder
{
    public static byte[] BuildExcel(string siteName, IReadOnlyList<VyajPartyDetailDto> parties)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Vyaj Khata");

        ws.Cell(1, 1).Value = "Vyaj Khata Ledger";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = $"Site: {siteName}";
        ws.Cell(3, 1).Value = $"Generated: {DateTime.Now:dd-MM-yyyy HH:mm}";

        var row = 5;
        var headers = new[]
        {
            "Party", "Main Ledger", "Sub Ledger", "Entry Start", "Principal", "First EMI",
            "Rate %", "Rate Basis", "Period Months", "Principal Due", "Vyaj Accrued",
            "Vyaj Paid", "Vyaj Due", "Status"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
        row++;

        foreach (var party in parties.OrderBy(p => p.Name))
        {
            var openEntries = party.Entries.Where(e => !e.IsClosed).ToList();
            if (openEntries.Count == 0 && party.Entries.Count == 0)
            {
                ws.Cell(row, 1).Value = party.Name;
                ws.Cell(row, 2).Value = party.MainLedgerName ?? string.Empty;
                ws.Cell(row, 3).Value = party.SubLedgerName ?? string.Empty;
                row++;
                continue;
            }

            foreach (var entry in party.Entries.OrderBy(e => e.StartDate))
            {
                ws.Cell(row, 1).Value = party.Name;
                ws.Cell(row, 2).Value = party.MainLedgerName ?? string.Empty;
                ws.Cell(row, 3).Value = party.SubLedgerName ?? string.Empty;
                ws.Cell(row, 4).Value = entry.StartDate.ToString("dd-MM-yyyy");
                ws.Cell(row, 5).Value = entry.Principal;
                ws.Cell(row, 6).Value = entry.EmiAmount ?? 0;
                ws.Cell(row, 7).Value = entry.RatePercent;
                ws.Cell(row, 8).Value = entry.RateBasis;
                ws.Cell(row, 9).Value = entry.RatePeriodMonths?.ToString() ?? string.Empty;
                ws.Cell(row, 10).Value = entry.PrincipalDue;
                ws.Cell(row, 11).Value = entry.GrossVyaj;
                ws.Cell(row, 12).Value = entry.InterestPaid;
                ws.Cell(row, 13).Value = entry.VyajDue;
                ws.Cell(row, 14).Value = entry.IsClosed ? "Closed" : "Open";
                foreach (var col in new[] { 5, 6, 7, 10, 11, 12, 13 })
                    ws.Cell(row, col).Style.NumberFormat.Format = "0.00";
                row++;
            }
        }

        row += 1;
        ws.Cell(row, 1).Value = "Site Totals (open entries)";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 10).Value = parties.Sum(p => p.TotalPrincipalDue);
        ws.Cell(row, 11).Value = parties.Sum(p => p.TotalGrossVyaj);
        ws.Cell(row, 12).Value = parties.Sum(p => p.TotalVyajPaid);
        ws.Cell(row, 13).Value = parties.Sum(p => p.TotalVyajDue);
        foreach (var col in new[] { 10, 11, 12, 13 })
        {
            ws.Cell(row, col).Style.Font.Bold = true;
            ws.Cell(row, col).Style.NumberFormat.Format = "0.00";
        }

        row += 2;
        ws.Cell(row, 1).Value = "Payments";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        var payHeaders = new[] { "Party", "Entry Start", "Payment Date", "Type", "Amount" };
        for (var i = 0; i < payHeaders.Length; i++)
            ws.Cell(row, i + 1).Value = payHeaders[i];
        ws.Range(row, 1, row, payHeaders.Length).Style.Font.Bold = true;
        row++;

        foreach (var party in parties.OrderBy(p => p.Name))
        {
            foreach (var entry in party.Entries.OrderBy(e => e.StartDate))
            {
                foreach (var payment in entry.Payments.OrderBy(p => p.PaymentDate))
                {
                    ws.Cell(row, 1).Value = party.Name;
                    ws.Cell(row, 2).Value = entry.StartDate.ToString("dd-MM-yyyy");
                    ws.Cell(row, 3).Value = payment.PaymentDate.ToString("dd-MM-yyyy");
                    ws.Cell(row, 4).Value = string.Equals(payment.PaymentType, "principal", StringComparison.OrdinalIgnoreCase)
                        ? "Principal"
                        : "Vyaj";
                    ws.Cell(row, 5).Value = payment.Amount;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "0.00";
                    row++;
                }
            }
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public static byte[] BuildPdf(string siteName, IReadOnlyList<VyajPartyDetailDto> parties)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text("Vyaj Khata Ledger").Bold().FontSize(14);
                    col.Item().Text($"Site: {siteName}").FontSize(9);
                    col.Item().Text($"Generated: {DateTime.Now:dd-MM-yyyy HH:mm}").FontSize(8);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.0f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(0.7f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(0.8f);
                            columns.ConstantColumn(48);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("PARTY");
                            header.Cell().Element(HeaderCell).Text("START");
                            header.Cell().Element(HeaderCell).AlignRight().Text("PRINCIPAL");
                            header.Cell().Element(HeaderCell).AlignRight().Text("FIRST EMI");
                            header.Cell().Element(HeaderCell).Text("RATE");
                            header.Cell().Element(HeaderCell).AlignRight().Text("PRIN DUE");
                            header.Cell().Element(HeaderCell).AlignRight().Text("VYAJ ACC");
                            header.Cell().Element(HeaderCell).AlignRight().Text("VYAJ PAID");
                            header.Cell().Element(HeaderCell).AlignRight().Text("VYAJ DUE");
                            header.Cell().Element(HeaderCell).Text("STATUS");
                        });

                        foreach (var party in parties.OrderBy(p => p.Name))
                        {
                            foreach (var entry in party.Entries.OrderBy(e => e.StartDate))
                            {
                                var rateLabel = entry.RatePeriodMonths is int months
                                    ? $"{entry.RatePercent}% / {entry.RateBasis}-{months}"
                                    : $"{entry.RatePercent}% / {entry.RateBasis}";

                                table.Cell().Element(BodyCell).Text(party.Name);
                                table.Cell().Element(BodyCell).Text(entry.StartDate.ToString("dd-MM-yyyy"));
                                table.Cell().Element(BodyCell).AlignRight().Text(entry.Principal.ToString("N2"));
                                table.Cell().Element(BodyCell).AlignRight().Text((entry.EmiAmount ?? 0).ToString("N2"));
                                table.Cell().Element(BodyCell).Text(rateLabel);
                                table.Cell().Element(BodyCell).AlignRight().Text(entry.PrincipalDue.ToString("N2"));
                                table.Cell().Element(BodyCell).AlignRight().Text(entry.GrossVyaj.ToString("N2"));
                                table.Cell().Element(BodyCell).AlignRight().Text(entry.InterestPaid.ToString("N2"));
                                table.Cell().Element(BodyCell).AlignRight().Text(entry.VyajDue.ToString("N2"));
                                table.Cell().Element(BodyCell).Text(entry.IsClosed ? "Closed" : "Open");
                            }
                        }
                    });

                    col.Item().PaddingTop(10).Text(
                        $"Totals (open) — Principal due: {parties.Sum(p => p.TotalPrincipalDue):N2}  |  " +
                        $"Vyaj due: {parties.Sum(p => p.TotalVyajDue):N2}").Bold();
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
