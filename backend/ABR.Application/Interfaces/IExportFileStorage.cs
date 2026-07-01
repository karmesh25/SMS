namespace ABR.Application.Interfaces;

public interface IExportFileStorage
{
    bool PendriveOnly { get; }

    Task<ExportDeliveryResult> PrepareDeliveryAsync(
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default);
}

public sealed class ExportDeliveryResult
{
    public bool PendriveOnly { get; init; }
    public byte[] Content { get; init; } = Array.Empty<byte>();
    public string ContentType { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string SavedPath { get; init; } = string.Empty;
}
