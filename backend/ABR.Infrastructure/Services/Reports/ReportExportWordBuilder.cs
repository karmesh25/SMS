using ABR.Application.DTOs.Reports;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ABR.Infrastructure.Services.Reports;

internal static class ReportExportWordBuilder
{
    public static byte[] Build(ReportExportContext ctx)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            AddParagraph(body, ctx.Title, bold: true, size: 28);
            AddParagraph(body, $"Site: {ctx.SiteName}");
            foreach (var line in ctx.FilterLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    AddParagraph(body, line);
            }

            AddParagraph(body, $"Generated: {DateTime.UtcNow:dd-MM-yyyy HH:mm} UTC", italic: true, size: 18);
            AddParagraph(body, string.Empty);

            switch (ctx.ReportType)
            {
                case ReportTypes.AllEntry:
                    BuildAllEntry(body, ctx.AllEntryRows);
                    break;
                case ReportTypes.BalanceSheet:
                    BuildBalanceSheet(body, ctx.BalanceSheet!);
                    break;
                case ReportTypes.TillDate:
                    BuildTillDate(body, ctx.TillDateRows);
                    break;
                case ReportTypes.Monthwise:
                    BuildMonthwise(body, ctx.MonthwiseRows);
                    break;
                case ReportTypes.BankStatement:
                    BuildBankStatement(body, ctx.BankStatement!);
                    break;
                case ReportTypes.SellDetails:
                    BuildSellDetails(body, ctx.SellDetails!);
                    break;
                case ReportTypes.Installment:
                    BuildInstallment(body, ctx.InstallmentRows);
                    break;
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static void BuildAllEntry(Body body, IReadOnlyList<AllEntryReportRowDto> rows)
    {
        var headers = new[]
        {
            "Date", "Aavak Ledger", "Aavak Sub", "Aavak Flat", "Aavak Cash/Bank", "Aavak Amt", "Aavak Desc",
            "Javak Ledger", "Javak Sub", "Javak Cash/Bank", "Javak Amt", "Javak Desc"
        };
        var table = CreateTable(headers.Length);
        AddHeaderRow(table, headers);
        foreach (var r in rows)
        {
            AddDataRow(table,
                ReportExportHelpers.FormatDate(r.Date),
                r.AavakLedger, r.AavakSubLedger, r.AavakFlatNo, r.AavakCashBank,
                ReportExportHelpers.FormatIndianAmount(r.AavakAmount), r.AavakDescription,
                r.JavakLedger, r.JavakSubLedger, r.JavakCashBank,
                ReportExportHelpers.FormatIndianAmount(r.JavakAmount), r.JavakDescription);
        }

        body.Append(table);
    }

    private static void BuildBalanceSheet(Body body, BalanceSheetReportDto data)
    {
        AddParagraph(body, "Aavak", bold: true);
        var aavakTable = CreateTable(2);
        AddHeaderRow(aavakTable, "Ledger", "Total");
        foreach (var item in data.AavakItems)
            AddDataRow(aavakTable, item.LedgerName, ReportExportHelpers.FormatIndianAmount(item.TotalAmount));
        AddBoldDataRow(aavakTable, "Total", ReportExportHelpers.FormatIndianAmount(data.TotalAavak));
        body.Append(aavakTable);
        AddParagraph(body, string.Empty);

        AddParagraph(body, "Javak", bold: true);
        var javakTable = CreateTable(2);
        AddHeaderRow(javakTable, "Ledger", "Total");
        foreach (var item in data.JavakItems)
            AddDataRow(javakTable, item.LedgerName, ReportExportHelpers.FormatIndianAmount(item.TotalAmount));
        AddBoldDataRow(javakTable, "Total", ReportExportHelpers.FormatIndianAmount(data.TotalJavak));
        body.Append(javakTable);
        AddParagraph(body, string.Empty);

        AddParagraph(body, $"{(data.Profit >= 0 ? "Net Profit" : "Net Loss")}: {ReportExportHelpers.FormatIndianAmount(Math.Abs(data.Profit))}", bold: true);
    }

    private static void BuildTillDate(Body body, IReadOnlyList<TillDateReportRowDto> rows)
    {
        var headers = new[]
        {
            "Site", "Wing", "Flat", "Member", "Contact", "Broker", "Broker Contact",
            "Booking Date", "SQFT", "Rate", "Total", "Paid",
            "Remain (Condition)", "Remain (Total)", "Last Payment", "Days Last Pmt",
            "Days Booking", "% Paid", "Dastavej", "Service Tax"
        };
        var table = CreateTable(headers.Length);
        AddHeaderRow(table, headers);
        foreach (var r in rows)
        {
            AddDataRow(table,
                r.SiteName, r.WingName, r.FlatNo, r.MemberName, r.CustomerContact,
                r.BrokerName, r.BrokerContact, ReportExportHelpers.FormatDate(r.BookingDate),
                ReportExportHelpers.FormatIndianAmount(r.Sqft), ReportExportHelpers.FormatIndianAmount(r.Rate),
                ReportExportHelpers.FormatIndianAmount(r.TotalPrice), ReportExportHelpers.FormatIndianAmount(r.TotalPaid),
                ReportExportHelpers.FormatIndianAmount(r.RemainingAsPerCondition), ReportExportHelpers.FormatIndianAmount(r.TotalRemaining),
                ReportExportHelpers.FormatDate(r.LastPaymentDate), r.DaysFromLastPayment?.ToString(),
                r.DaysFromBooking.ToString(), r.PercentagePaid.ToString(),
                ReportExportHelpers.FormatDate(r.DastavejDate),
                r.ServiceTax.HasValue ? ReportExportHelpers.FormatIndianAmount(r.ServiceTax) : string.Empty);
        }

        body.Append(table);
    }

    private static void BuildMonthwise(Body body, IReadOnlyList<MonthwiseReportRowDto> rows)
    {
        var table = CreateTable(4);
        AddHeaderRow(table, "Month", "Aavak", "Javak", "Net");
        foreach (var r in rows)
        {
            AddDataRow(table, r.MonthLabel,
                ReportExportHelpers.FormatIndianAmount(r.AavakTotal),
                ReportExportHelpers.FormatIndianAmount(r.JavakTotal),
                ReportExportHelpers.FormatIndianAmount(r.Net));
        }

        body.Append(table);
    }

    private static void BuildBankStatement(Body body, BankStatementReportDto data)
    {
        AddParagraph(body, $"Opening Balance: {ReportExportHelpers.FormatIndianAmount(data.OpeningBalance)}", bold: true);
        var table = CreateTable(5);
        AddHeaderRow(table, "Date", "Description", "Debit", "Credit", "Balance");
        foreach (var r in data.Rows)
        {
            AddDataRow(table,
                ReportExportHelpers.FormatDate(r.EntryDate), r.Description,
                r.Debit > 0 ? ReportExportHelpers.FormatIndianAmount(r.Debit) : string.Empty,
                r.Credit > 0 ? ReportExportHelpers.FormatIndianAmount(r.Credit) : string.Empty,
                ReportExportHelpers.FormatIndianAmount(r.Balance));
        }

        body.Append(table);
        AddParagraph(body, $"Closing Balance: {ReportExportHelpers.FormatIndianAmount(data.ClosingBalance)}", bold: true);
    }

    private static void BuildSellDetails(Body body, SellDetailsReportDto data)
    {
        var table = CreateTable(8);
        AddHeaderRow(table, "Flat", "Wing", "Customer", "Booking Date", "Total", "Paid", "Remaining", "Status");
        foreach (var r in data.Items)
        {
            AddDataRow(table, r.FlatNo, r.WingName, r.MemberName,
                ReportExportHelpers.FormatDate(r.BookingDate),
                ReportExportHelpers.FormatIndianAmount(r.TotalPrice),
                ReportExportHelpers.FormatIndianAmount(r.Paid),
                ReportExportHelpers.FormatIndianAmount(r.Remaining), r.Status);
        }

        body.Append(table);
        AddParagraph(body,
            $"Totals — Total: {ReportExportHelpers.FormatIndianAmount(data.TotalPrice)}, Paid: {ReportExportHelpers.FormatIndianAmount(data.TotalPaid)}, Remaining: {ReportExportHelpers.FormatIndianAmount(data.TotalRemaining)}",
            bold: true);
    }

    private static void BuildInstallment(Body body, IReadOnlyList<InstallmentReportRowDto> rows)
    {
        var table = CreateTable(9);
        AddHeaderRow(table, "Flat", "Member", "Milestone", "Due Date", "Due Amt", "Paid Amt", "Remaining", "Paid Date", "Status");
        foreach (var r in rows)
        {
            AddDataRow(table, r.FlatNo, r.MemberName, r.MilestoneName,
                ReportExportHelpers.FormatDate(r.DueDate),
                ReportExportHelpers.FormatIndianAmount(r.DueAmount),
                ReportExportHelpers.FormatIndianAmount(r.PaidAmount),
                ReportExportHelpers.FormatIndianAmount(r.Remaining),
                ReportExportHelpers.FormatDate(r.PaidDate), r.Status);
        }

        body.Append(table);
    }

    private static Table CreateTable(int columnCount)
    {
        var table = new Table();
        var props = new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }));
        table.AppendChild(props);

        var grid = new TableGrid();
        for (var i = 0; i < columnCount; i++)
            grid.Append(new GridColumn());
        table.Append(grid);
        return table;
    }

    private static void AddHeaderRow(Table table, params string[] cells)
    {
        var row = new TableRow();
        foreach (var text in cells)
            row.Append(CreateCell(text ?? string.Empty, bold: true));
        table.Append(row);
    }

    private static void AddDataRow(Table table, params string?[] cells)
    {
        var row = new TableRow();
        foreach (var text in cells)
            row.Append(CreateCell(text ?? string.Empty));
        table.Append(row);
    }

    private static void AddBoldDataRow(Table table, params string?[] cells)
    {
        var row = new TableRow();
        foreach (var text in cells)
            row.Append(CreateCell(text ?? string.Empty, bold: true));
        table.Append(row);
    }

    private static TableCell CreateCell(string text, bool bold = false)
    {
        var runProps = bold ? new RunProperties(new Bold()) : null;
        var run = new Run();
        if (runProps != null)
            run.Append(runProps);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        var para = new Paragraph(run);
        return new TableCell(para);
    }

    private static void AddParagraph(Body body, string text, bool bold = false, bool italic = false, int size = 22)
    {
        var props = new RunProperties();
        if (bold) props.Append(new Bold());
        if (italic) props.Append(new Italic());
        props.Append(new FontSize { Val = size.ToString() });

        var run = new Run(props, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        body.Append(new Paragraph(run));
    }
}
