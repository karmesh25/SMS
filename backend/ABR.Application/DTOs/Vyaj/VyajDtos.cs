namespace ABR.Application.DTOs.Vyaj;

public sealed class VyajPartySummaryDto
{
    public Guid Id { get; init; }
    public Guid SiteId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public decimal VyajDue { get; init; }
    public decimal PrincipalDue { get; init; }
    public int OpenEntryCount { get; init; }
}

public sealed class VyajPartyDetailDto
{
    public Guid Id { get; init; }
    public Guid SiteId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public decimal TotalVyajDue { get; init; }
    public decimal TotalGrossVyaj { get; init; }
    public decimal TotalVyajPaid { get; init; }
    public decimal TotalPrincipalDue { get; init; }
    public IReadOnlyList<VyajEntryDto> Entries { get; init; } = [];
}

public sealed class VyajEntryDto
{
    public Guid Id { get; init; }
    public Guid PartyId { get; init; }
    public decimal Principal { get; init; }
    public decimal RatePercent { get; init; }
    public string RateBasis { get; init; } = "month";
    public DateOnly StartDate { get; init; }
    public bool IsClosed { get; init; }
    public decimal GrossVyaj { get; init; }
    public decimal InterestPaid { get; init; }
    public decimal PrincipalPaid { get; init; }
    public decimal VyajDue { get; init; }
    public decimal PrincipalDue { get; init; }
    public IReadOnlyList<VyajPaymentDto> Payments { get; init; } = [];
}

public sealed class VyajPaymentDto
{
    public Guid Id { get; init; }
    public Guid EntryId { get; init; }
    public DateOnly PaymentDate { get; init; }
    public decimal Amount { get; init; }
    public string PaymentType { get; init; } = "interest";
}

public sealed class CreateVyajPartyDto
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class UpdateVyajPartyDto
{
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class CreateVyajEntryDto
{
    public Guid PartyId { get; set; }
    public decimal Principal { get; set; }
    public decimal RatePercent { get; set; }
    public string RateBasis { get; set; } = "month";
    public DateOnly StartDate { get; set; }
}

public sealed class ToggleVyajEntryClosedDto
{
    public bool IsClosed { get; set; }
}

public sealed class CreateVyajPaymentDto
{
    public Guid EntryId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = "interest";
}
