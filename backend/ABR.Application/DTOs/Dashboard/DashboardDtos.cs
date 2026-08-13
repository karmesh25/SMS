namespace ABR.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalFlats { get; set; }
    public int BookedFlats { get; set; }
    public int AvailableFlats { get; set; }
    public int CancelledFlats { get; set; }
    public decimal BookingPercentage { get; set; }
    public decimal TotalAavak { get; set; }
    public decimal TotalJavak { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalVyajDue { get; set; }
    public decimal TotalVyajPrincipalDue { get; set; }
    public IReadOnlyList<WingSummaryDto> WingSummary { get; set; } = Array.Empty<WingSummaryDto>();
    public IReadOnlyList<RecentEntryDto> RecentEntries { get; set; } = Array.Empty<RecentEntryDto>();
    public IReadOnlyList<DashboardVyajPartyDto> VyajParties { get; set; } = Array.Empty<DashboardVyajPartyDto>();
}

public class DashboardVyajPartyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal VyajDue { get; set; }
    public decimal PrincipalDue { get; set; }
}

public class WingSummaryDto
{
    public Guid WingId { get; set; }
    public string WingName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Booked { get; set; }
    public int Available { get; set; }
    public decimal BookingPercentage { get; set; }
}

public class RecentEntryDto
{
    public Guid Id { get; set; }
    public DateOnly EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string MainLedgerName { get; set; } = string.Empty;
    public string? SubLedgerName { get; set; }
    public decimal Amount { get; set; }
}
