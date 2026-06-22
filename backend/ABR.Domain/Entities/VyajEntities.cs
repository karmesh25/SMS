using ABR.Domain.Common;

namespace ABR.Domain.Entities;

public class VyajParty : SoftDeleteEntity
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<VyajEntry> Entries { get; set; } = new List<VyajEntry>();
}

public class VyajEntry : SoftDeleteEntity
{
    public Guid PartyId { get; set; }
    public decimal Principal { get; set; }
    public decimal RatePercent { get; set; }
    public string RateBasis { get; set; } = "month";
    public DateOnly StartDate { get; set; }
    public bool IsClosed { get; set; }

    public VyajParty Party { get; set; } = null!;
    public ICollection<VyajPayment> Payments { get; set; } = new List<VyajPayment>();
}

public class VyajPayment : SoftDeleteEntity
{
    public Guid EntryId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = "interest";

    public VyajEntry Entry { get; set; } = null!;
}
