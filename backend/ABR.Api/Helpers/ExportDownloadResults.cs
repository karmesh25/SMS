using ABR.Application.Common;
using ABR.Application.DTOs.Reports;
using ABR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Helpers;

public static class ExportDownloadResults
{
    public static async Task<IActionResult> FromBytesAsync(
        IExportFileStorage storage,
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken)
    {
        var delivery = await storage.PrepareDeliveryAsync(content, contentType, fileName, cancellationToken);

        if (delivery.PendriveOnly)
        {
            return new OkObjectResult(ApiResponse<ExportSavedDto>.Ok(
                new ExportSavedDto { FileName = delivery.FileName, SavedPath = delivery.SavedPath },
                $"Saved to pendrive: {delivery.SavedPath}"));
        }

        return new FileContentResult(delivery.Content, delivery.ContentType)
        {
            FileDownloadName = delivery.FileName
        };
    }
}
