using ABR.Application.DTOs.Reports;

namespace ABR.Application.Interfaces;

public interface IReportExportService
{
    Task<ReportExportResultDto> GenerateExcelAsync(ReportExportRequestDto request, CancellationToken cancellationToken = default);
    Task<ReportExportResultDto> GeneratePdfAsync(ReportExportRequestDto request, CancellationToken cancellationToken = default);
    Task<ReportExportResultDto> GenerateWordAsync(ReportExportRequestDto request, CancellationToken cancellationToken = default);
}
