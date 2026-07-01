namespace ABR.Application.Common;

public sealed class ExportSettings
{
    /// <summary>
    /// When true, exports are saved to the configured output folder on the pendrive
    /// and are not sent to the browser download folder.
    /// </summary>
    public bool PendriveOnly { get; set; }

    /// <summary>
    /// Folder for saved exports. Relative paths resolve from the API content root
    /// (e.g. ../exports on pendrive resolves to the USB drive exports folder).
    /// </summary>
    public string OutputDirectory { get; set; } = "../exports";
}
