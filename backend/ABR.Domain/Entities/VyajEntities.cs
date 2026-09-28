using ABR.Domain.Common;

namespace ABR.Domain.Entities;

public class VyajParty : SoftDeleteEntity
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    /// <summary>direct | asset</summary>
    public string PartyKind { get; set; } = "direct";
    public Guid? MainLedgerId { get; set; }
    public Guid? SubLedgerId { get; set; }
    public Guid? BookingId { get; set; }

    public Site Site { get; set; } = null!;
    public MainLedger? MainLedger { get; set; }
    public SubLedger? SubLedger { get; set; }
    public Booking? Booking { get; set; }
    public ICollection<VyajEntry> Entries { get; set; } = new List<VyajEntry>();
}

public class VyajEntry : SoftDeleteEntity
{
    public Guid PartyId { get; set; }
    public decimal Principal { get; set; }
    public decimal RatePercent { get; set; }
    public string RateBasis { get; set; } = "month";
    public int? RatePeriodMonths { get; set; }
    /// <summary>Day count for custom rate basis (compound interval).</summary>
    public int? CustomDays { get; set; }
    public decimal? EmiAmount { get; set; }
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

public class VyajRateBasisOption : SoftDeleteEntity
{
    public Guid SiteId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int DayCount { get; set; }
    public bool IsActive { get; set; } = true;

    public Site Site { get; set; } = null!;
}

public class UserNotification : SoftDeleteEntity
{
    public Guid SiteId { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public bool IsRead { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }

    public Site Site { get; set; } = null!;
    public User? RecipientUser { get; set; }
    public User? ActorUser { get; set; }
    public User? ResolvedByUser { get; set; }
}
