namespace ABR.Application.DTOs.MasterData;

public class SiteDto
{
    public Guid Id { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSiteDto
{
    public string SiteName { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public string? Address { get; set; }
}

public class UpdateSiteDto
{
    public string SiteName { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WingDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string WingName { get; set; } = string.Empty;
    public int Floors { get; set; }
    public int FlatsPerFloor { get; set; }
    public int Shops { get; set; }
    public bool IsBungalow { get; set; }
    public bool IsPlot { get; set; }
    public int FlatCount { get; set; }
}

public class CreatePlotDto
{
    public Guid SiteId { get; set; }
    public string PlotName { get; set; } = string.Empty;
    public int PlotCount { get; set; }
}

public class UpdatePlotDto
{
    public string PlotName { get; set; } = string.Empty;
    public int PlotCount { get; set; }
}

public class CreateWingDto
{
    public Guid SiteId { get; set; }
    public string WingName { get; set; } = string.Empty;
    public int Floors { get; set; }
    public int FlatsPerFloor { get; set; }
    public int Shops { get; set; }
    public bool IsBungalow { get; set; }
}

public class UpdateWingDto
{
    public string WingName { get; set; } = string.Empty;
    public int Floors { get; set; }
    public int FlatsPerFloor { get; set; }
    public int Shops { get; set; }
    public bool IsBungalow { get; set; }
}

public class FlatDto
{
    public Guid Id { get; set; }
    public Guid WingId { get; set; }
    public string FlatNo { get; set; } = string.Empty;
    public decimal Sqft { get; set; }
    public string? FlatType { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Floor { get; set; }
}

public class FlatGridDto
{
    public Guid WingId { get; set; }
    public string WingName { get; set; } = string.Empty;
    public int Floors { get; set; }
    public int FlatsPerFloor { get; set; }
    public bool IsBungalow { get; set; }
    public bool IsPlot { get; set; }
    public int BookedCount { get; set; }
    public int AvailableCount { get; set; }
    public int CancelledCount { get; set; }
    public List<FlatDto> Flats { get; set; } = new();
}

public class MainLedgerDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string LedgerName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateMainLedgerDto
{
    public Guid SiteId { get; set; }
    public string LedgerName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SubLedgerDto
{
    public Guid Id { get; set; }
    public Guid MainLedgerId { get; set; }
    public string LedgerName { get; set; } = string.Empty;
    public Guid? FlatId { get; set; }
    public string? FlatNo { get; set; }
}

public class CreateSubLedgerDto
{
    public Guid MainLedgerId { get; set; }
    public string LedgerName { get; set; } = string.Empty;
    public Guid? FlatId { get; set; }
}

public class ConditionDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public string ConditionType { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class CreateConditionDto
{
    public Guid SiteId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public string ConditionType { get; set; } = "manual";
}

public class UpdateConditionDto
{
    public string ConditionName { get; set; } = string.Empty;
    public string ConditionType { get; set; } = "manual";
}

public class UpdateConditionItemDto
{
    public string MilestoneName { get; set; } = string.Empty;
    public decimal? Percentage { get; set; }
    public decimal? Amount { get; set; }
    public int DueAfterDays { get; set; }
    public int SortOrder { get; set; }
}

public class ConditionItemDto
{
    public Guid Id { get; set; }
    public Guid ConditionId { get; set; }
    public string MilestoneName { get; set; } = string.Empty;
    public decimal? Percentage { get; set; }
    public decimal? Amount { get; set; }
    public int DueAfterDays { get; set; }
    public int SortOrder { get; set; }
}

public class CreateConditionItemDto
{
    public string MilestoneName { get; set; } = string.Empty;
    public decimal? Percentage { get; set; }
    public decimal? Amount { get; set; }
    public int DueAfterDays { get; set; }
    public int SortOrder { get; set; }
}

public class BankAccountDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string? IfscCode { get; set; }
    public string? Branch { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBankAccountDto
{
    public Guid SiteId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string? IfscCode { get; set; }
    public string? Branch { get; set; }
    public decimal OpeningBalance { get; set; }
}

public class UpdateBankAccountDto
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string? IfscCode { get; set; }
    public string? Branch { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BrokerDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactNo { get; set; }
    public string? ContactNo2 { get; set; }
    public string? Address { get; set; }
}

public class CreateBrokerDto
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactNo { get; set; }
    public string? ContactNo2 { get; set; }
    public string? Address { get; set; }
}
