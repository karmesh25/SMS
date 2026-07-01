using ABR.Domain.Common;

namespace ABR.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int FailedAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public bool ForcePasswordChange { get; set; }

    public AppRole AppRole { get; set; } = null!;
    public ICollection<UserSiteAccess> SiteAccesses { get; set; } = new List<UserSiteAccess>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<DeviceLicense> AuthorizedDevices { get; set; } = new List<DeviceLicense>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class UserSiteAccess : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }

    public User User { get; set; } = null!;
    public Site Site { get; set; } = null!;
}

public class DeviceLicense : BaseEntity
{
    public string DeviceName { get; set; } = string.Empty;
    public string FingerprintHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastVerifiedAt { get; set; }
    public Guid? AuthorizedById { get; set; }

    public User? AuthorizedBy { get; set; }
}

public class Site : BaseEntity
{
    public string SiteName { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Wing> Wings { get; set; } = new List<Wing>();
    public ICollection<MainLedger> MainLedgers { get; set; } = new List<MainLedger>();
    public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
    public ICollection<Broker> Brokers { get; set; } = new List<Broker>();
    public ICollection<Condition> Conditions { get; set; } = new List<Condition>();
    public ICollection<DailyEntry> DailyEntries { get; set; } = new List<DailyEntry>();
    public ICollection<VyajParty> VyajParties { get; set; } = new List<VyajParty>();
    public ICollection<UserSiteAccess> UserAccesses { get; set; } = new List<UserSiteAccess>();
}

public class Wing : BaseEntity
{
    public Guid SiteId { get; set; }
    public string WingName { get; set; } = string.Empty;
    public int Floors { get; set; }
    public int FlatsPerFloor { get; set; }
    public int Shops { get; set; }
    public bool IsBungalow { get; set; }
    public bool IsPlot { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<Flat> Flats { get; set; } = new List<Flat>();
}

public class Flat : BaseEntity
{
    public Guid WingId { get; set; }
    public string FlatNo { get; set; } = string.Empty;
    public decimal Sqft { get; set; }
    public string? FlatType { get; set; }
    public string Status { get; set; } = "available";

    public Wing Wing { get; set; } = null!;
    public ICollection<SubLedger> SubLedgers { get; set; } = new List<SubLedger>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class MainLedger : BaseEntity
{
    public Guid SiteId { get; set; }
    public string LedgerName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<SubLedger> SubLedgers { get; set; } = new List<SubLedger>();
    public ICollection<DailyEntry> DailyEntries { get; set; } = new List<DailyEntry>();
}

public class SubLedger : BaseEntity
{
    public Guid MainLedgerId { get; set; }
    public string LedgerName { get; set; } = string.Empty;
    public Guid? FlatId { get; set; }

    public MainLedger MainLedger { get; set; } = null!;
    public Flat? Flat { get; set; }
    public ICollection<DailyEntry> DailyEntries { get; set; } = new List<DailyEntry>();
    public ICollection<Booking> MemberBookings { get; set; } = new List<Booking>();
}

public class BankAccount : BaseEntity
{
    public Guid SiteId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string? IfscCode { get; set; }
    public string? Branch { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; } = true;

    public Site Site { get; set; } = null!;
}

public class Broker : BaseEntity
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactNo { get; set; }
    public string? ContactNo2 { get; set; }
    public string? Address { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class Condition : BaseEntity
{
    public Guid SiteId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public string ConditionType { get; set; } = "manual";

    public Site Site { get; set; } = null!;
    public ICollection<ConditionItem> Items { get; set; } = new List<ConditionItem>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class ConditionItem : BaseEntity
{
    public Guid ConditionId { get; set; }
    public string MilestoneName { get; set; } = string.Empty;
    public decimal? Percentage { get; set; }
    public decimal? Amount { get; set; }
    public int DueAfterDays { get; set; }
    public int SortOrder { get; set; }

    public Condition Condition { get; set; } = null!;
}

public class Booking : BaseEntity
{
    public Guid FlatId { get; set; }
    public Guid MemberSubLedgerId { get; set; }
    public Guid? BrokerId { get; set; }
    public Guid ConditionId { get; set; }
    public DateOnly BookingDate { get; set; }
    public string? CustomerContact { get; set; }
    public decimal Sqft { get; set; }
    public decimal Rate { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal BrokeragePct { get; set; }
    public decimal BrokerageAmount { get; set; }
    public string CustomerType { get; set; } = "real";
    public bool IsArjaMarjaSell { get; set; }
    public string Status { get; set; } = "active";
    public DateOnly? CancelDate { get; set; }
    public DateOnly? DastavejDate { get; set; }
    public DateOnly? SatakhatDate { get; set; }
    public string? DocumentNumber { get; set; }
    public decimal? ServiceTax { get; set; }
    public string? Notes { get; set; }

    public Flat Flat { get; set; } = null!;
    public SubLedger MemberSubLedger { get; set; } = null!;
    public Broker? Broker { get; set; }
    public Condition Condition { get; set; } = null!;
    public ICollection<BookingInstallment> Installments { get; set; } = new List<BookingInstallment>();
}

public class BookingInstallment : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid? ConditionItemId { get; set; }
    public string MilestoneName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal DueAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public string Status { get; set; } = "pending";
    public string? PaymentNotes { get; set; }

    public Booking Booking { get; set; } = null!;
    public ConditionItem? ConditionItem { get; set; }
}

public class DailyEntry : SoftDeleteEntity
{
    public Guid SiteId { get; set; }
    public string EntryType { get; set; } = "aavak";
    public DateOnly EntryDate { get; set; }
    public Guid MainLedgerId { get; set; }
    public Guid SubLedgerId { get; set; }
    public decimal Amount { get; set; }
    public string CashBank { get; set; } = "Cash";
    public string? Description { get; set; }

    public Site Site { get; set; } = null!;
    public MainLedger MainLedger { get; set; } = null!;
    public SubLedger SubLedger { get; set; } = null!;
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    public User? User { get; set; }
}
