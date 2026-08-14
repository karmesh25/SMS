using ABR.Application.DTOs.Reports;
using ClosedXML.Excel;

namespace ABR.Infrastructure.Services.Reports;

internal static class ReportExportExcelBuilder
{
    public static byte[] Build(ReportExportContext ctx)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Report");
        var row = 1;
        ReportExportHelpers.WriteExcelHeaderBlock(ws, ref row, ctx.Title, ctx.SiteName, ctx.FilterLines);

        switch (ctx.ReportType)
        {
            case ReportTypes.AllEntry:
                BuildAllEntry(ws, ref row, ctx.AllEntryRows);
                break;
            case ReportTypes.BalanceSheet:
                BuildBalanceSheet(ws, ref row, ctx.BalanceSheet!);
                break;
            case ReportTypes.TillDate:
                BuildTillDate(ws, ref row, ctx.TillDateRows);
                break;
            case ReportTypes.Monthwise:
                BuildMonthwise(ws, ref row, ctx.MonthwiseRows);
                break;
            case ReportTypes.BankStatement:
                BuildBankStatement(ws, ref row, ctx.BankStatement!);
                break;
            case ReportTypes.SellDetails:
                BuildSellDetails(ws, ref row, ctx.SellDetails!);
                break;
            case ReportTypes.Installment:
                BuildInstallment(ws, ref row, ctx.InstallmentRows);
                break;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(row > 1 ? FindHeaderRow(ws) : 1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static int FindHeaderRow(IXLWorksheet ws)
    {
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 1; r <= lastRow; r++)
        {
            var cell = ws.Cell(r, 1).GetString();
            if (cell is "Date" or "Month" or "Ledger" or "Flat" or "Site")
                return r;
        }

        return 1;
    }

    private static void BuildAllEntry(IXLWorksheet ws, ref int row, IReadOnlyList<AllEntryReportRowDto> rows)
    {
        var headers = new[]
        {
            "Date", "Aavak Ledger", "Aavak Sub", "Aavak Flat", "Aavak Cash/Bank", "Aavak Amt", "Aavak Desc",
            "Javak Ledger", "Javak Sub", "Javak Cash/Bank", "Javak Amt", "Javak Desc"
        };
        WriteHeaderRow(ws, ref row, headers);

        var dataStart = row;
        var idx = 0;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = ReportExportHelpers.FormatDate(r.Date);
            ws.Cell(row, 2).Value = r.AavakLedger ?? string.Empty;
            ws.Cell(row, 3).Value = r.AavakSubLedger ?? string.Empty;
            ws.Cell(row, 4).Value = r.AavakFlatNo ?? string.Empty;
            ws.Cell(row, 5).Value = r.AavakCashBank ?? string.Empty;
            ws.Cell(row, 6).Value = r.AavakAmount.HasValue ? ReportExportHelpers.FormatIndianAmount(r.AavakAmount) : string.Empty;
            ws.Cell(row, 7).Value = r.AavakDescription ?? string.Empty;
            ws.Cell(row, 8).Value = r.JavakLedger ?? string.Empty;
            ws.Cell(row, 9).Value = r.JavakSubLedger ?? string.Empty;
            ws.Cell(row, 10).Value = r.JavakCashBank ?? string.Empty;
            ws.Cell(row, 11).Value = r.JavakAmount.HasValue ? ReportExportHelpers.FormatIndianAmount(r.JavakAmount) : string.Empty;
            ws.Cell(row, 12).Value = r.JavakDescription ?? string.Empty;
            ReportExportHelpers.ApplyAlternatingRow(ws.Range(row, 1, row, 12), idx++);
            row++;
        }

        if (rows.Count == 0)
            row = dataStart;
    }

    private static void BuildBalanceSheet(IXLWorksheet ws, ref int row, BalanceSheetReportDto data)
    {
        ws.Cell(row, 1).Value = "Aavak";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        WriteHeaderRow(ws, ref row, new[] { "Ledger", "Total" });
        var idx = 0;
        foreach (var item in data.AavakItems)
        {
            ws.Cell(row, 1).Value = item.LedgerName;
            ws.Cell(row, 2).Value = ReportExportHelpers.FormatIndianAmount(item.TotalAmount);
            ReportExportHelpers.ApplyAlternatingRow(ws.Range(row, 1, row, 2), idx++);
            row++;
        }

        ws.Cell(row, 1).Value = "Total";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = ReportExportHelpers.FormatIndianAmount(data.TotalAavak);
        ws.Cell(row, 2).Style.Font.Bold = true;
        row += 2;

        ws.Cell(row, 1).Value = "Javak";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        WriteHeaderRow(ws, ref row, new[] { "Ledger", "Total" });
        idx = 0;
        foreach (var item in data.JavakItems)
        {
            ws.Cell(row, 1).Value = item.LedgerName;
            ws.Cell(row, 2).Value = ReportExportHelpers.FormatIndianAmount(item.TotalAmount);
            ReportExportHelpers.ApplyAlternatingRow(ws.Range(row, 1, row, 2), idx++);
            row++;
        }

        ws.Cell(row, 1).Value = "Total";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = ReportExportHelpers.FormatIndianAmount(data.TotalJavak);
        ws.Cell(row, 2).Style.Font.Bold = true;
        row += 2;

        ws.Cell(row, 1).Value = data.Profit >= 0 ? "Net Profit" : "Net Loss";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = ReportExportHelpers.FormatIndianAmount(Math.Abs(data.Profit));
        ws.Cell(row, 2).Style.Font.Bold = true;
        row++;
    }

    private static void BuildTillDate(IXLWorksheet ws, ref int row, IReadOnlyList<TillDateReportRowDto> rows)
    {
        var headers = new[]
        {
            "Site", "Wing", "Flat", "Member", "Contact", "Broker", "Broker Contact",
            "Booking Date", "SQFT", "Rate", "Total", "Paid",
            "Remain (Condition)", "Remain (Total)", "Last Payment", "Days Last Pmt",
            "Days Booking", "% Paid", "Dastavej", "Service Tax"
        };
        WriteHeaderRow(ws, ref row, headers);

        var idx = 0;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.SiteName;
            ws.Cell(row, 2).Value = r.WingName;
            ws.Cell(row, 3).Value = r.FlatNo;
            ws.Cell(row, 4).Value = r.MemberName;
            ws.Cell(row, 5).Value = r.CustomerContact ?? string.Empty;
            ws.Cell(row, 6).Value = r.BrokerName ?? string.Empty;
            ws.Cell(row, 7).Value = r.BrokerContact ?? string.Empty;
            ws.Cell(row, 8).Value = ReportExportHelpers.FormatDate(r.BookingDate);
            ws.Cell(row, 9).Value = ReportExportHelpers.FormatIndianAmount(r.Sqft);
            ws.Cell(row, 10).Value = ReportExportHelpers.FormatIndianAmount(r.Rate);
            ws.Cell(row, 11).Value = ReportExportHelpers.FormatIndianAmount(r.TotalPrice);
            ws.Cell(row, 12).Value = ReportExportHelpers.FormatIndianAmount(r.TotalPaid);
            ws.Cell(row, 13).Value = ReportExportHelpers.FormatIndianAmount(r.RemainingAsPerCondition);
            ws.Cell(row, 14).Value = ReportExportHelpers.FormatIndianAmount(r.TotalRemaining);
            ws.Cell(row, 15).Value = ReportExportHelpers.FormatDate(r.LastPaymentDate);
            ws.Cell(row, 16).Value = r.DaysFromLastPayment?.ToString() ?? string.Empty;
            ws.Cell(row, 17).Value = r.DaysFromBooking;
            ws.Cell(row, 18).Value = r.PercentagePaid;
            ws.Cell(row, 19).Value = ReportExportHelpers.FormatDate(r.DastavejDate);
            ws.Cell(row, 20).Value = r.ServiceTax.HasValue ? ReportExportHelpers.FormatIndianAmount(r.ServiceTax) : string.Empty;
            ReportExportHelpers.ApplyAlternatingRow(ws.Range(row, 1, row, 20), idx++);
            row++;
        }
    }

    private static void BuildMonthwise(IXLWorksheet ws, ref int row, IReadOnlyList<MonthwiseReportRowDto> rows)
    {
        WriteHeaderRow(ws, ref row, new[] { "Month", "Aavak", "Javak", "Net" });
        var idx = 0;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.MonthLabel;
            ws.Cell(row, 2).Value = ReportExportHelpers.FormatIndianAmount(r.AavakTotal);
            ws.Cell(row, 3).Value = ReportExportHelpers.FormatIndianAmount(r.JavakTotal);
            ws.Cell(row, 4).Value = ReportExportHelpers.FormatIndianAmount(r.Net);
            ReportExportHelpers.ApplyAlternatingRow(ws.Range(row, 1, row, 4), idx++);
            row++;
        }
    }

    private static void BuildBankStatement(IXLWorksheet ws, ref int row, BankStatementReportDto data)
    {
        ws.Cell(row, 1).Value = "Opening Balance";
        ws.Cell(row, 5).Value = ReportExportHelpers.FormatIndianAmount(data.OpeningBalance);
        row += 2;

        WriteHeaderRow(ws, ref row, new[] { "Date", "Description", "Debit", "Credit", "Balance" });
        var idx = 0;
        foreach (var r in data.Rows)
        {
            ws.Cell(row, 1).Value = ReportExportHelpers.FormatDate(r.EntryDate);
            ws.Cell(row, 2).Value = r.Description ?? string.Empty;
            ws.Cell(row, 3).Value = r.Debit > 0 ? ReportExportHelpers.FormatIndianAmount(r.Debit) : string.Empty;
            ws.Cell(row, 4).Value = r.Credit > 0 ? ReportExportHelpers.FormatIndianAmount(r.Credit) : string.Empty;
            ws.Cell(row, 5).Value = ReportExportHelpers.FormatIndianAmount(r.Balance);
            ReportExportHelpers.ApplyAlternatingRow(ws.Range(row, 1, row, 5), idx++);
            row++;
        }

        row++;
        ws.Cell(row, 1).Value = "Closing Balance";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = ReportExportHelpers.FormatIndianAmount(data.ClosingBalance);
        ws.Cell(row, 5).Style.Font.Bold = true;
        row++;
    }

    private static void BuildSellDetails(IXLWorksheet ws, ref int row, SellDetailsReportDto data)
    {
        WriteHeaderRow(ws, ref row, new[] { "Flat", "Wing", "Customer", "Booking Date", "Total", "Paid", "Remaining", "Status" });
        var idx = 0;
        foreach (var r in data.Items)
        {
            ws.Cell(row, 1).Value = r.FlatNo;
            ws.Cell(row, 2).Value = r.WingName;
            ws.Cell(row, 3).Value = r.MemberName;
            ws.Cell(row, 4).Value = ReportExportHelpers.FormatDate(r.BookingDate);
            ws.Cell(row, 5).Value = ReportExportHelpers.FormatIndianAmount(r.TotalPrice);
            ws.Cell(row, 6).Value = ReportExportHelpers.FormatIndianAmount(r.Paid);
            ws.Cell(row, 7).Value = ReportExportHelpers.FormatIndianAmount(r.Remaining);
            ws.Cell(row, 8).Value = r.Status;
            ReportExportHelpers.ApplyAlternatingRow(ws.Range(row, 1, row, 8), idx++);
            row++;
        }

        row++;
        ws.Cell(row, 4).Value = "Totals";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = ReportExportHelpers.FormatIndianAmount(data.TotalPrice);
        ws.Cell(row, 6).Value = ReportExportHelpers.FormatIndianAmount(data.TotalPaid);
        ws.Cell(row, 7).Value = ReportExportHelpers.FormatIndianAmount(data.TotalRemaining);
        row++;
    }

    private static void BuildInstallment(IXLWorksheet ws, ref int row, IReadOnlyList<InstallmentReportRowDto> rows)
    {
        WriteHeaderRow(ws, ref row, new[] { "Flat", "Member", "Milestone", "Due Date", "Due Amt", "Paid Amt", "Remaining", "Paid Date", "Status" });
        var idx = 0;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.FlatNo;
            ws.Cell(row, 2).Value = r.MemberName;
            ws.Cell(row, 3).Value = r.MilestoneName;
            ws.Cell(row, 4).Value = ReportExportHelpers.FormatDate(r.DueDate);
            ws.Cell(row, 5).Value = ReportExportHelpers.FormatIndianAmount(r.DueAmount);
            ws.Cell(row, 6).Value = ReportExportHelpers.FormatIndianAmount(r.PaidAmount);
            ws.Cell(row, 7).Value = ReportExportHelpers.FormatIndianAmount(r.Remaining);
            ws.Cell(row, 8).Value = ReportExportHelpers.FormatDate(r.PaidDate);
            ws.Cell(row, 9).Value = r.Status;
            ReportExportHelpers.ApplyAlternatingRow(ws.Range(row, 1, row, 9), idx++);
            row++;
        }
    }

    private static void WriteHeaderRow(IXLWorksheet ws, ref int row, string[] headers)
    {
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(row, c + 1).Value = headers[c];
        ReportExportHelpers.ApplyHeaderStyle(ws.Range(row, 1, row, headers.Length));
        row++;
    }
}
