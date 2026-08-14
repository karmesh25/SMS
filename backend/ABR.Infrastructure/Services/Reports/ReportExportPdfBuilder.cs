using ABR.Application.DTOs.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ABR.Infrastructure.Services.Reports;

internal static class ReportExportPdfBuilder
{
    public static byte[] Build(ReportExportContext ctx)
    {
        var landscape = ReportTypes.IsLandscape(ctx.ReportType);
        var pageSize = landscape ? PageSizes.A4.Landscape() : PageSizes.A4;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(c => WriteHeader(c, ctx));
                page.Content().PaddingVertical(8).Element(c => WriteContent(c, ctx));
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

    private static void WriteHeader(IContainer container, ReportExportContext ctx)
    {
        container.Column(col =>
        {
            col.Item().Text(ctx.Title).Bold().FontSize(14);
            col.Item().Text($"Site: {ctx.SiteName}").FontSize(9);
            foreach (var line in ctx.FilterLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    col.Item().Text(line).FontSize(8);
            }

            col.Item().Text($"Generated: {DateTime.UtcNow:dd-MM-yyyy HH:mm} UTC").FontSize(7).Italic();
        });
    }

    private static void WriteContent(IContainer container, ReportExportContext ctx)
    {
        switch (ctx.ReportType)
        {
            case ReportTypes.AllEntry:
                WriteAllEntryTable(container, ctx.AllEntryRows);
                break;
            case ReportTypes.BalanceSheet:
                WriteBalanceSheet(container, ctx.BalanceSheet!);
                break;
            case ReportTypes.TillDate:
                WriteTillDateTable(container, ctx.TillDateRows);
                break;
            case ReportTypes.Monthwise:
                WriteMonthwiseTable(container, ctx.MonthwiseRows);
                break;
            case ReportTypes.BankStatement:
                WriteBankStatement(container, ctx.BankStatement!);
                break;
            case ReportTypes.SellDetails:
                WriteSellDetailsTable(container, ctx.SellDetails!);
                break;
            case ReportTypes.Installment:
                WriteInstallmentTable(container, ctx.InstallmentRows);
                break;
        }
    }

    private static void WriteAllEntryTable(IContainer container, IReadOnlyList<AllEntryReportRowDto> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(45);
                for (var i = 0; i < 11; i++)
                    cols.RelativeColumn();
            });

            table.Header(header =>
            {
                HeaderCell(header, "Date");
                HeaderCell(header, "A.Ledger");
                HeaderCell(header, "A.Sub");
                HeaderCell(header, "A.Flat");
                HeaderCell(header, "A.C/B");
                HeaderCell(header, "A.Amt");
                HeaderCell(header, "A.Desc");
                HeaderCell(header, "J.Ledger");
                HeaderCell(header, "J.Sub");
                HeaderCell(header, "J.C/B");
                HeaderCell(header, "J.Amt");
                HeaderCell(header, "J.Desc");
            });

            foreach (var r in rows)
            {
                DataCell(table, ReportExportHelpers.FormatDate(r.Date));
                DataCell(table, r.AavakLedger);
                DataCell(table, r.AavakSubLedger);
                DataCell(table, r.AavakFlatNo);
                DataCell(table, r.AavakCashBank);
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.AavakAmount));
                DataCell(table, r.AavakDescription);
                DataCell(table, r.JavakLedger);
                DataCell(table, r.JavakSubLedger);
                DataCell(table, r.JavakCashBank);
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.JavakAmount));
                DataCell(table, r.JavakDescription);
            }
        });
    }

    private static void WriteBalanceSheet(IContainer container, BalanceSheetReportDto data)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(4).Text("Aavak").Bold().FontSize(10);
            col.Item().Element(c => WriteTwoColumnTable(c, data.AavakItems.Select(i => (i.LedgerName, ReportExportHelpers.FormatIndianAmount(i.TotalAmount)))));
            col.Item().PaddingTop(2).Row(r =>
            {
                r.RelativeItem().Text("Total").Bold();
                r.ConstantItem(80).AlignRight().Text(ReportExportHelpers.FormatIndianAmount(data.TotalAavak)).Bold();
            });

            col.Item().PaddingTop(10).PaddingBottom(4).Text("Javak").Bold().FontSize(10);
            col.Item().Element(c => WriteTwoColumnTable(c, data.JavakItems.Select(i => (i.LedgerName, ReportExportHelpers.FormatIndianAmount(i.TotalAmount)))));
            col.Item().PaddingTop(2).Row(r =>
            {
                r.RelativeItem().Text("Total").Bold();
                r.ConstantItem(80).AlignRight().Text(ReportExportHelpers.FormatIndianAmount(data.TotalJavak)).Bold();
            });

            col.Item().PaddingTop(10).Text($"{(data.Profit >= 0 ? "Net Profit" : "Net Loss")}: {ReportExportHelpers.FormatIndianAmount(Math.Abs(data.Profit))}").Bold();
        });
    }

    private static void WriteTwoColumnTable(IContainer container, IEnumerable<(string Col1, string Col2)> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn(1);
            });
            table.Header(header =>
            {
                HeaderCell(header, "Ledger");
                HeaderCell(header, "Total");
            });
            foreach (var (c1, c2) in rows)
            {
                DataCell(table, c1);
                DataCell(table, c2);
            }
        });
    }

    private static void WriteTillDateTable(IContainer container, IReadOnlyList<TillDateReportRowDto> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < 20; i++)
                    cols.RelativeColumn();
            });

            table.Header(header =>
            {
                foreach (var h in new[]
                {
                    "Site", "Wing", "Flat", "Member", "Contact", "Broker", "Br.Contact",
                    "Bk.Date", "SQFT", "Rate", "Total", "Paid", "Rem.Cond", "Rem.Tot",
                    "Last Pmt", "Days LP", "Days Bk", "%Paid", "Dastavej", "Svc Tax"
                })
                    HeaderCell(header, h);
            });

            foreach (var r in rows)
            {
                DataCell(table, r.SiteName);
                DataCell(table, r.WingName);
                DataCell(table, r.FlatNo);
                DataCell(table, r.MemberName);
                DataCell(table, r.CustomerContact);
                DataCell(table, r.BrokerName);
                DataCell(table, r.BrokerContact);
                DataCell(table, ReportExportHelpers.FormatDate(r.BookingDate));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.Sqft));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.Rate));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.TotalPrice));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.TotalPaid));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.RemainingAsPerCondition));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.TotalRemaining));
                DataCell(table, ReportExportHelpers.FormatDate(r.LastPaymentDate));
                DataCell(table, r.DaysFromLastPayment?.ToString());
                DataCell(table, r.DaysFromBooking.ToString());
                DataCell(table, r.PercentagePaid.ToString());
                DataCell(table, ReportExportHelpers.FormatDate(r.DastavejDate));
                DataCell(table, r.ServiceTax.HasValue ? ReportExportHelpers.FormatIndianAmount(r.ServiceTax) : string.Empty);
            }
        });
    }

    private static void WriteMonthwiseTable(IContainer container, IReadOnlyList<MonthwiseReportRowDto> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
            });
            table.Header(header =>
            {
                HeaderCell(header, "Month");
                HeaderCell(header, "Aavak");
                HeaderCell(header, "Javak");
                HeaderCell(header, "Net");
            });
            foreach (var r in rows)
            {
                DataCell(table, r.MonthLabel);
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.AavakTotal));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.JavakTotal));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.Net));
            }
        });
    }

    private static void WriteBankStatement(IContainer container, BankStatementReportDto data)
    {
        container.Column(col =>
        {
            col.Item().Text($"Opening Balance: {ReportExportHelpers.FormatIndianAmount(data.OpeningBalance)}").Bold();
            col.Item().PaddingTop(6).Element(c =>
            {
                c.Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(55);
                        cols.RelativeColumn(3);
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        HeaderCell(header, "Date");
                        HeaderCell(header, "Description");
                        HeaderCell(header, "Debit");
                        HeaderCell(header, "Credit");
                        HeaderCell(header, "Balance");
                    });
                    foreach (var r in data.Rows)
                    {
                        DataCell(table, ReportExportHelpers.FormatDate(r.EntryDate));
                        DataCell(table, r.Description);
                        DataCell(table, r.Debit > 0 ? ReportExportHelpers.FormatIndianAmount(r.Debit) : string.Empty);
                        DataCell(table, r.Credit > 0 ? ReportExportHelpers.FormatIndianAmount(r.Credit) : string.Empty);
                        DataCell(table, ReportExportHelpers.FormatIndianAmount(r.Balance));
                    }
                });
            });
            col.Item().PaddingTop(6).Text($"Closing Balance: {ReportExportHelpers.FormatIndianAmount(data.ClosingBalance)}").Bold();
        });
    }

    private static void WriteSellDetailsTable(IContainer container, SellDetailsReportDto data)
    {
        container.Column(col =>
        {
            col.Item().Element(c =>
            {
                c.Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (var i = 0; i < 8; i++)
                            cols.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        foreach (var h in new[] { "Flat", "Wing", "Customer", "Booking Date", "Total", "Paid", "Remaining", "Status" })
                            HeaderCell(header, h);
                    });
                    foreach (var r in data.Items)
                    {
                        DataCell(table, r.FlatNo);
                        DataCell(table, r.WingName);
                        DataCell(table, r.MemberName);
                        DataCell(table, ReportExportHelpers.FormatDate(r.BookingDate));
                        DataCell(table, ReportExportHelpers.FormatIndianAmount(r.TotalPrice));
                        DataCell(table, ReportExportHelpers.FormatIndianAmount(r.Paid));
                        DataCell(table, ReportExportHelpers.FormatIndianAmount(r.Remaining));
                        DataCell(table, r.Status);
                    }
                });
            });
            col.Item().PaddingTop(6).Text(
                $"Totals — Total: {ReportExportHelpers.FormatIndianAmount(data.TotalPrice)}, Paid: {ReportExportHelpers.FormatIndianAmount(data.TotalPaid)}, Remaining: {ReportExportHelpers.FormatIndianAmount(data.TotalRemaining)}")
                .Bold();
        });
    }

    private static void WriteInstallmentTable(IContainer container, IReadOnlyList<InstallmentReportRowDto> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < 9; i++)
                    cols.RelativeColumn();
            });
            table.Header(header =>
            {
                foreach (var h in new[] { "Flat", "Member", "Milestone", "Due Date", "Due Amt", "Paid Amt", "Remaining", "Paid Date", "Status" })
                    HeaderCell(header, h);
            });
            foreach (var r in rows)
            {
                DataCell(table, r.FlatNo);
                DataCell(table, r.MemberName);
                DataCell(table, r.MilestoneName);
                DataCell(table, ReportExportHelpers.FormatDate(r.DueDate));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.DueAmount));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.PaidAmount));
                DataCell(table, ReportExportHelpers.FormatIndianAmount(r.Remaining));
                DataCell(table, ReportExportHelpers.FormatDate(r.PaidDate));
                DataCell(table, r.Status);
            }
        });
    }

    private static void HeaderCell(TableCellDescriptor header, string text)
    {
        header.Cell().Background(Colors.Blue.Darken3).Padding(2)
            .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(7))
            .Text(text ?? string.Empty);
    }

    private static void DataCell(TableDescriptor table, string? text)
    {
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2)
            .Text(text ?? string.Empty).FontSize(7);
    }
}
