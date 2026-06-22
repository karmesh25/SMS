using ABR.Application.DTOs.Reports;

namespace ABR.Application.Interfaces;

public interface IReportService
{
    Task<PagedReportDto<AllEntryReportRowDto>> GetAllEntryAsync(AllEntryReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllEntryReportRowDto>> GetAllEntryForExportAsync(AllEntryReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(BalanceSheetFilterDto filter, CancellationToken cancellationToken = default);
    Task<PagedReportDto<TillDateReportRowDto>> GetTillDateAsync(TillDateReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TillDateReportRowDto>> GetTillDateForExportAsync(TillDateReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthwiseReportRowDto>> GetMonthwiseAsync(MonthwiseReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<BankStatementReportDto> GetBankStatementAsync(BankStatementFilterDto filter, CancellationToken cancellationToken = default);
    Task<SellDetailsReportDto> GetSellDetailsAsync(SellDetailsFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstallmentReportRowDto>> GetInstallmentAsync(InstallmentReportFilterDto filter, CancellationToken cancellationToken = default);
}
