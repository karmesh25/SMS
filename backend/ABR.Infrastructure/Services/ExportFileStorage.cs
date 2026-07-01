using ABR.Application.Common;
using ABR.Application.Interfaces;

namespace ABR.Infrastructure.Services;

public sealed class ExportFileStorage : IExportFileStorage
{
    private readonly ExportSettings _settings;

    public ExportFileStorage(ExportSettings settings)
    {
        _settings = settings;
    }

    public bool PendriveOnly => _settings.PendriveOnly;

    public async Task<ExportDeliveryResult> PrepareDeliveryAsync(
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = $"export-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

        if (_settings.PendriveOnly)
        {
            var savedPath = await SaveAsync(content, safeName, cancellationToken);
            return new ExportDeliveryResult
            {
                PendriveOnly = true,
                FileName = safeName,
                SavedPath = savedPath
            };
        }

        return new ExportDeliveryResult
        {
            PendriveOnly = false,
            Content = content,
            ContentType = contentType,
            FileName = safeName
        };
    }

    private async Task<string> SaveAsync(byte[] content, string fileName, CancellationToken cancellationToken)
    {
        var directory = ResolveOutputDirectory();
        Directory.CreateDirectory(directory);

        var targetPath = Path.Combine(directory, fileName);
        if (File.Exists(targetPath))
        {
            var stem = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            targetPath = Path.Combine(directory, $"{stem}-{DateTime.UtcNow:yyyyMMdd-HHmmss}{ext}");
        }

        await File.WriteAllBytesAsync(targetPath, content, cancellationToken);
        return Path.GetFullPath(targetPath);
    }

    private string ResolveOutputDirectory()
    {
        var configured = string.IsNullOrWhiteSpace(_settings.OutputDirectory)
            ? "exports"
            : _settings.OutputDirectory;

        return Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));
    }
}
