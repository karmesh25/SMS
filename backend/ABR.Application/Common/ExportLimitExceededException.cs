namespace ABR.Application.Common;

public sealed class ExportLimitExceededException : Exception
{
    public const string DefaultMessage = "Export exceeds 50,000 rows. Narrow your date range or filters.";

    public ExportLimitExceededException() : base(DefaultMessage) { }
}
