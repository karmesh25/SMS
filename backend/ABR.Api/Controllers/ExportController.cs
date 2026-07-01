using ABR.Api.Helpers;
using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.Reports;
using ABR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly IReportExportService _exportService;
    private readonly IExportFileStorage _exportStorage;

    public ExportController(IReportExportService exportService, IExportFileStorage exportStorage)
    {
        _exportService = exportService;
        _exportStorage = exportStorage;
    }

    [HttpGet("excel")]
    [RequirePermission(AppModules.Reports, PermissionLevel.View)]
    public Task<IActionResult> ExportExcel([FromQuery] ReportExportRequestDto request, CancellationToken cancellationToken)
        => ExportFileAsync(request, _exportService.GenerateExcelAsync, cancellationToken);

    [HttpGet("pdf")]
    [RequirePermission(AppModules.Reports, PermissionLevel.View)]
    public Task<IActionResult> ExportPdf([FromQuery] ReportExportRequestDto request, CancellationToken cancellationToken)
        => ExportFileAsync(request, _exportService.GeneratePdfAsync, cancellationToken);

    [HttpGet("word")]
    [RequirePermission(AppModules.Reports, PermissionLevel.View)]
    public Task<IActionResult> ExportWord([FromQuery] ReportExportRequestDto request, CancellationToken cancellationToken)
        => ExportFileAsync(request, _exportService.GenerateWordAsync, cancellationToken);

    private async Task<IActionResult> ExportFileAsync(
        ReportExportRequestDto request,
        Func<ReportExportRequestDto, CancellationToken, Task<ReportExportResultDto>> generator,
        CancellationToken cancellationToken)
    {
        if (request.SiteId == Guid.Empty)
            return BadRequest(new { message = "SiteId is required." });

        if (string.IsNullOrWhiteSpace(request.ReportType))
            return BadRequest(new { message = "reportType is required." });

        try
        {
            var result = await generator(request, cancellationToken);
            return await ExportDownloadResults.FromBytesAsync(
                _exportStorage,
                result.Content,
                result.ContentType,
                result.FileName,
                cancellationToken);
        }
        catch (ExportLimitExceededException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
