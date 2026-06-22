using ABR.Application.Common;
using ABR.Application.DTOs.Reports;
using ABR.Application.Interfaces;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services.Reports;

public sealed class ReportExportService : IReportExportService
{
    private readonly IReportService _reportService;
    private readonly AbrDbContext _context;

    public ReportExportService(IReportService reportService, AbrDbContext context)
    {
        _reportService = reportService;
        _context = context;
    }

    public Task<ReportExportResultDto> GenerateExcelAsync(ReportExportRequestDto request, CancellationToken cancellationToken = default)
        => GenerateAsync(request, ReportExportExcelBuilder.Build, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx", cancellationToken);

    public Task<ReportExportResultDto> GeneratePdfAsync(ReportExportRequestDto request, CancellationToken cancellationToken = default)
        => GenerateAsync(request, ReportExportPdfBuilder.Build, "application/pdf", "pdf", cancellationToken);

    public Task<ReportExportResultDto> GenerateWordAsync(ReportExportRequestDto request, CancellationToken cancellationToken = default)
        => GenerateAsync(request, ReportExportWordBuilder.Build, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx", cancellationToken);

    private async Task<ReportExportResultDto> GenerateAsync(
        ReportExportRequestDto request,
        Func<ReportExportContext, byte[]> builder,
        string contentType,
        string extension,
        CancellationToken cancellationToken)
    {
        ValidateReportType(request.ReportType);
        var siteName = await GetSiteNameAsync(request.SiteId, cancellationToken);
        var context = await BuildContextAsync(request, siteName, cancellationToken);
        var content = builder(context);
        return new ReportExportResultDto
        {
            Content = content,
            ContentType = contentType,
            FileName = ReportExportHelpers.BuildFileName(request.ReportType, siteName, extension)
        };
    }

    private static void ValidateReportType(string reportType)
    {
        if (!ReportTypes.IsValid(reportType))
            throw new ArgumentException($"Invalid report type: {reportType}");
    }

    private async Task<string> GetSiteNameAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var site = await _context.Sites.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken)
            ?? throw new KeyNotFoundException("Site not found.");
        return site.SiteName;
    }

    private async Task<ReportExportContext> BuildContextAsync(ReportExportRequestDto request, string siteName, CancellationToken cancellationToken)
    {
        return request.ReportType switch
        {
            ReportTypes.AllEntry => await BuildAllEntryContextAsync(request, siteName, cancellationToken),
            ReportTypes.BalanceSheet => await BuildBalanceSheetContextAsync(request, siteName, cancellationToken),
            ReportTypes.TillDate => await BuildTillDateContextAsync(request, siteName, cancellationToken),
            ReportTypes.Monthwise => await BuildMonthwiseContextAsync(request, siteName, cancellationToken),
            ReportTypes.BankStatement => await BuildBankStatementContextAsync(request, siteName, cancellationToken),
            ReportTypes.SellDetails => await BuildSellDetailsContextAsync(request, siteName, cancellationToken),
            ReportTypes.Installment => await BuildInstallmentContextAsync(request, siteName, cancellationToken),
            _ => throw new ArgumentException($"Unsupported report type: {request.ReportType}")
        };
    }

    private async Task<ReportExportContext> BuildAllEntryContextAsync(ReportExportRequestDto request, string siteName, CancellationToken cancellationToken)
    {
        if (!request.DateFrom.HasValue || !request.DateTo.HasValue)
            throw new ArgumentException("DateFrom and DateTo are required for all-entry export.");

        var filter = new AllEntryReportFilterDto
        {
            SiteId = request.SiteId,
            DateFrom = request.DateFrom.Value,
            DateTo = request.DateTo.Value,
            MainLedgerId = request.MainLedgerId,
            SubLedgerId = request.SubLedgerId,
            FlatNo = request.FlatNo
        };
        var rows = await _reportService.GetAllEntryForExportAsync(filter, cancellationToken);
        EnsureRowLimit(rows.Count);

        var filters = new List<string>
        {
            $"Period: {ReportExportHelpers.FormatDate(filter.DateFrom)} to {ReportExportHelpers.FormatDate(filter.DateTo)}"
        };
        if (request.MainLedgerId.HasValue)
            filters.Add($"Main Ledger filter applied");
        if (request.SubLedgerId.HasValue)
            filters.Add($"Sub Ledger filter applied");
        if (!string.IsNullOrWhiteSpace(request.FlatNo))
            filters.Add($"Flat No: {request.FlatNo}");

        return new ReportExportContext
        {
            ReportType = ReportTypes.AllEntry,
            Title = "All Daily Entry Report",
            SiteName = siteName,
            FilterLines = filters,
            AllEntryRows = rows
        };
    }

    private async Task<ReportExportContext> BuildBalanceSheetContextAsync(ReportExportRequestDto request, string siteName, CancellationToken cancellationToken)
    {
        var filter = new BalanceSheetFilterDto
        {
            SiteId = request.SiteId,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            MainLedgerId = request.MainLedgerId
        };
        var data = await _reportService.GetBalanceSheetAsync(filter, cancellationToken);
        var rowCount = data.AavakItems.Count + data.JavakItems.Count;
        EnsureRowLimit(rowCount);

        var filters = new List<string>();
        if (request.DateFrom.HasValue || request.DateTo.HasValue)
            filters.Add($"Period: {ReportExportHelpers.FormatDate(request.DateFrom)} to {ReportExportHelpers.FormatDate(request.DateTo)}");

        return new ReportExportContext
        {
            ReportType = ReportTypes.BalanceSheet,
            Title = "Balance Sheet",
            SiteName = data.SiteName,
            FilterLines = filters,
            BalanceSheet = data
        };
    }

    private async Task<ReportExportContext> BuildTillDateContextAsync(ReportExportRequestDto request, string siteName, CancellationToken cancellationToken)
    {
        var filter = new TillDateReportFilterDto
        {
            SiteId = request.SiteId,
            AsOfDate = request.AsOfDate,
            DaysFromLastPayment = request.DaysFromLastPayment,
            MovementType = request.MovementType,
            ExtraReturnOnly = request.ExtraReturnOnly
        };
        var rows = await _reportService.GetTillDateForExportAsync(filter, cancellationToken);
        EnsureRowLimit(rows.Count);

        var filters = new List<string>
        {
            $"As of: {ReportExportHelpers.FormatDate(filter.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow))}",
            $"Movement: {filter.MovementType}"
        };
        if (filter.ExtraReturnOnly)
            filters.Add("Extra return only: Yes");

        return new ReportExportContext
        {
            ReportType = ReportTypes.TillDate,
            Title = "Till Date Report",
            SiteName = siteName,
            FilterLines = filters,
            TillDateRows = rows
        };
    }

    private async Task<ReportExportContext> BuildMonthwiseContextAsync(ReportExportRequestDto request, string siteName, CancellationToken cancellationToken)
    {
        var filter = new MonthwiseReportFilterDto
        {
            SiteId = request.SiteId,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo
        };
        var rows = await _reportService.GetMonthwiseAsync(filter, cancellationToken);
        EnsureRowLimit(rows.Count);

        var filters = new List<string>();
        if (request.DateFrom.HasValue || request.DateTo.HasValue)
            filters.Add($"Period: {ReportExportHelpers.FormatDate(request.DateFrom)} to {ReportExportHelpers.FormatDate(request.DateTo)}");

        return new ReportExportContext
        {
            ReportType = ReportTypes.Monthwise,
            Title = "Monthwise Report",
            SiteName = siteName,
            FilterLines = filters,
            MonthwiseRows = rows
        };
    }

    private async Task<ReportExportContext> BuildBankStatementContextAsync(ReportExportRequestDto request, string siteName, CancellationToken cancellationToken)
    {
        if (!request.BankAccountId.HasValue)
            throw new ArgumentException("BankAccountId is required for bank-statement export.");

        var filter = new BankStatementFilterDto
        {
            SiteId = request.SiteId,
            BankAccountId = request.BankAccountId.Value,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo
        };
        var data = await _reportService.GetBankStatementAsync(filter, cancellationToken);
        EnsureRowLimit(data.Rows.Count);

        var filters = new List<string>
        {
            $"Bank: {data.BankName} ({data.AccountNo})"
        };
        if (request.DateFrom.HasValue || request.DateTo.HasValue)
            filters.Add($"Period: {ReportExportHelpers.FormatDate(request.DateFrom)} to {ReportExportHelpers.FormatDate(request.DateTo)}");

        return new ReportExportContext
        {
            ReportType = ReportTypes.BankStatement,
            Title = "Bank Statement",
            SiteName = siteName,
            FilterLines = filters,
            BankStatement = data
        };
    }

    private async Task<ReportExportContext> BuildSellDetailsContextAsync(ReportExportRequestDto request, string siteName, CancellationToken cancellationToken)
    {
        var filter = new SellDetailsFilterDto
        {
            SiteId = request.SiteId,
            WingId = request.WingId,
            Status = request.Status
        };
        var data = await _reportService.GetSellDetailsAsync(filter, cancellationToken);
        EnsureRowLimit(data.Items.Count);

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Status))
            filters.Add($"Status: {request.Status}");

        return new ReportExportContext
        {
            ReportType = ReportTypes.SellDetails,
            Title = "Sell Details Report",
            SiteName = siteName,
            FilterLines = filters,
            SellDetails = data
        };
    }

    private async Task<ReportExportContext> BuildInstallmentContextAsync(ReportExportRequestDto request, string siteName, CancellationToken cancellationToken)
    {
        if (!request.BookingId.HasValue && string.IsNullOrWhiteSpace(request.FlatNo))
            throw new ArgumentException("FlatNo or BookingId is required for installment export.");

        var filter = new InstallmentReportFilterDto
        {
            SiteId = request.SiteId,
            BookingId = request.BookingId,
            FlatNo = request.FlatNo
        };
        var rows = await _reportService.GetInstallmentAsync(filter, cancellationToken);
        EnsureRowLimit(rows.Count);

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.FlatNo))
            filters.Add($"Flat No: {request.FlatNo}");

        return new ReportExportContext
        {
            ReportType = ReportTypes.Installment,
            Title = "Installment Report",
            SiteName = siteName,
            FilterLines = filters,
            InstallmentRows = rows
        };
    }

    private static void EnsureRowLimit(int count)
    {
        if (count > ReportExportHelpers.MaxExportRows)
            throw new ExportLimitExceededException();
    }
}

internal static class ReportTypes
{
    public const string AllEntry = "all-entry";
    public const string BalanceSheet = "balance-sheet";
    public const string TillDate = "till-date";
    public const string Monthwise = "monthwise";
    public const string BankStatement = "bank-statement";
    public const string SellDetails = "sell-details";
    public const string Installment = "installment";

    public static bool IsValid(string reportType) => reportType switch
    {
        AllEntry or BalanceSheet or TillDate or Monthwise or BankStatement or SellDetails or Installment => true,
        _ => false
    };

    public static bool IsLandscape(string reportType) => reportType is AllEntry or TillDate or SellDetails;
}

internal sealed class ReportExportContext
{
    public string ReportType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string SiteName { get; init; } = string.Empty;
    public IReadOnlyList<string> FilterLines { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AllEntryReportRowDto> AllEntryRows { get; init; } = Array.Empty<AllEntryReportRowDto>();
    public BalanceSheetReportDto? BalanceSheet { get; init; }
    public IReadOnlyList<TillDateReportRowDto> TillDateRows { get; init; } = Array.Empty<TillDateReportRowDto>();
    public IReadOnlyList<MonthwiseReportRowDto> MonthwiseRows { get; init; } = Array.Empty<MonthwiseReportRowDto>();
    public BankStatementReportDto? BankStatement { get; init; }
    public SellDetailsReportDto? SellDetails { get; init; }
    public IReadOnlyList<InstallmentReportRowDto> InstallmentRows { get; init; } = Array.Empty<InstallmentReportRowDto>();
}
