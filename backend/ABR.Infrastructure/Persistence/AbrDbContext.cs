using ABR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Persistence;

public class AbrDbContext : DbContext
{
    public AbrDbContext(DbContextOptions<AbrDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSiteAccess> UserSiteAccesses => Set<UserSiteAccess>();
    public DbSet<DeviceLicense> DeviceLicenses => Set<DeviceLicense>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Wing> Wings => Set<Wing>();
    public DbSet<Flat> Flats => Set<Flat>();
    public DbSet<MainLedger> MainLedgers => Set<MainLedger>();
    public DbSet<SubLedger> SubLedgers => Set<SubLedger>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Broker> Brokers => Set<Broker>();
    public DbSet<Condition> Conditions => Set<Condition>();
    public DbSet<ConditionItem> ConditionItems => Set<ConditionItem>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<DailyEntry> DailyEntries => Set<DailyEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("pgcrypto");

        ConfigureUser(modelBuilder);
        ConfigureUserSiteAccess(modelBuilder);
        ConfigureDeviceLicense(modelBuilder);
        ConfigureSite(modelBuilder);
        ConfigureWing(modelBuilder);
        ConfigureFlat(modelBuilder);
        ConfigureMainLedger(modelBuilder);
        ConfigureSubLedger(modelBuilder);
        ConfigureBankAccount(modelBuilder);
        ConfigureBroker(modelBuilder);
        ConfigureCondition(modelBuilder);
        ConfigureConditionItem(modelBuilder);
        ConfigureBooking(modelBuilder);
        ConfigureDailyEntry(modelBuilder);
        ConfigureAuditLog(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ABR.Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();
        entity.ToTable("users");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
        entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.Property(e => e.FailedAttempts).HasColumnName("failed_attempts");
        entity.Property(e => e.LockedUntil).HasColumnName("locked_until");
        entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
        entity.Property(e => e.ForcePasswordChange).HasColumnName("force_password_change");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasIndex(e => e.Username).IsUnique();
    }

    private static void ConfigureUserSiteAccess(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserSiteAccess>();
        entity.ToTable("user_site_access");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.UserId).HasColumnName("user_id");
        entity.Property(e => e.SiteId).HasColumnName("site_id");
        entity.Property(e => e.CanRead).HasColumnName("can_read");
        entity.Property(e => e.CanWrite).HasColumnName("can_write");
        entity.Property(e => e.CanDelete).HasColumnName("can_delete");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.User).WithMany(u => u.SiteAccesses).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Site).WithMany(s => s.UserAccesses).HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDeviceLicense(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DeviceLicense>();
        entity.ToTable("device_licenses");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.DeviceName).HasColumnName("device_name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.FingerprintHash).HasColumnName("fingerprint_hash").HasMaxLength(128).IsRequired();
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.Property(e => e.LastVerifiedAt).HasColumnName("last_verified_at");
        entity.Property(e => e.AuthorizedById).HasColumnName("authorized_by");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.AuthorizedBy).WithMany(u => u.AuthorizedDevices).HasForeignKey(e => e.AuthorizedById).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureSite(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Site>();
        entity.ToTable("sites");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.SiteName).HasColumnName("site_name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.StartDate).HasColumnName("start_date");
        entity.Property(e => e.Address).HasColumnName("address");
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }

    private static void ConfigureWing(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Wing>();
        entity.ToTable("wings");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.SiteId).HasColumnName("site_id");
        entity.Property(e => e.WingName).HasColumnName("wing_name").HasMaxLength(50).IsRequired();
        entity.Property(e => e.Floors).HasColumnName("floors");
        entity.Property(e => e.FlatsPerFloor).HasColumnName("flats_per_floor");
        entity.Property(e => e.Shops).HasColumnName("shops");
        entity.Property(e => e.IsBungalow).HasColumnName("is_bungalow");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.Site).WithMany(s => s.Wings).HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureFlat(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Flat>();
        entity.ToTable("flats");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.WingId).HasColumnName("wing_id");
        entity.Property(e => e.FlatNo).HasColumnName("flat_no").HasMaxLength(20).IsRequired();
        entity.Property(e => e.Sqft).HasColumnName("sqft").HasColumnType("decimal(15,2)");
        entity.Property(e => e.FlatType).HasColumnName("flat_type");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.Wing).WithMany(w => w.Flats).HasForeignKey(e => e.WingId).OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.WingId, e.FlatNo }).IsUnique();
    }

    private static void ConfigureMainLedger(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<MainLedger>();
        entity.ToTable("main_ledgers");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.SiteId).HasColumnName("site_id");
        entity.Property(e => e.LedgerName).HasColumnName("ledger_name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.Site).WithMany(s => s.MainLedgers).HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureSubLedger(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SubLedger>();
        entity.ToTable("sub_ledgers");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.MainLedgerId).HasColumnName("main_ledger_id");
        entity.Property(e => e.LedgerName).HasColumnName("ledger_name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.FlatId).HasColumnName("flat_id");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.MainLedger).WithMany(m => m.SubLedgers).HasForeignKey(e => e.MainLedgerId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Flat).WithMany(f => f.SubLedgers).HasForeignKey(e => e.FlatId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureBankAccount(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BankAccount>();
        entity.ToTable("bank_accounts");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.SiteId).HasColumnName("site_id");
        entity.Property(e => e.BankName).HasColumnName("bank_name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.AccountNo).HasColumnName("account_no").HasMaxLength(50).IsRequired();
        entity.Property(e => e.IfscCode).HasColumnName("ifsc_code");
        entity.Property(e => e.Branch).HasColumnName("branch");
        entity.Property(e => e.OpeningBalance).HasColumnName("opening_balance").HasColumnType("decimal(15,2)");
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.Site).WithMany(s => s.BankAccounts).HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureBroker(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Broker>();
        entity.ToTable("brokers");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.SiteId).HasColumnName("site_id");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.ContactNo).HasColumnName("contact_no");
        entity.Property(e => e.ContactNo2).HasColumnName("contact_no_2");
        entity.Property(e => e.Address).HasColumnName("address");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.Site).WithMany(s => s.Brokers).HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureCondition(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Condition>();
        entity.ToTable("conditions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.SiteId).HasColumnName("site_id");
        entity.Property(e => e.ConditionName).HasColumnName("condition_name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.ConditionType).HasColumnName("condition_type").HasMaxLength(20);
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.Site).WithMany(s => s.Conditions).HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureConditionItem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ConditionItem>();
        entity.ToTable("condition_items");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.ConditionId).HasColumnName("condition_id");
        entity.Property(e => e.MilestoneName).HasColumnName("milestone_name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Percentage).HasColumnName("percentage").HasColumnType("decimal(15,2)");
        entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(15,2)");
        entity.Property(e => e.DueAfterDays).HasColumnName("due_after_days");
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.Condition).WithMany(c => c.Items).HasForeignKey(e => e.ConditionId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureBooking(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Booking>();
        entity.ToTable("bookings");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.FlatId).HasColumnName("flat_id");
        entity.Property(e => e.MemberSubLedgerId).HasColumnName("member_sub_ledger_id");
        entity.Property(e => e.BrokerId).HasColumnName("broker_id");
        entity.Property(e => e.ConditionId).HasColumnName("condition_id");
        entity.Property(e => e.BookingDate).HasColumnName("booking_date");
        entity.Property(e => e.CustomerContact).HasColumnName("customer_contact");
        entity.Property(e => e.Sqft).HasColumnName("sqft").HasColumnType("decimal(15,2)");
        entity.Property(e => e.Rate).HasColumnName("rate").HasColumnType("decimal(15,2)");
        entity.Property(e => e.TotalPrice).HasColumnName("total_price").HasColumnType("decimal(15,2)");
        entity.Property(e => e.BrokeragePct).HasColumnName("brokerage_pct").HasColumnType("decimal(15,2)");
        entity.Property(e => e.BrokerageAmount).HasColumnName("brokerage_amount").HasColumnType("decimal(15,2)");
        entity.Property(e => e.CustomerType).HasColumnName("customer_type");
        entity.Property(e => e.IsArjaMarjaSell).HasColumnName("is_arja_marja_sell");
        entity.Property(e => e.Status).HasColumnName("status");
        entity.Property(e => e.CancelDate).HasColumnName("cancel_date");
        entity.Property(e => e.DastavejDate).HasColumnName("dastavej_date");
        entity.Property(e => e.SatakhatDate).HasColumnName("satakhat_date");
        entity.Property(e => e.DocumentNumber).HasColumnName("document_number");
        entity.Property(e => e.ServiceTax).HasColumnName("service_tax").HasColumnType("decimal(15,2)");
        entity.Property(e => e.Notes).HasColumnName("notes");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.Flat).WithMany(f => f.Bookings).HasForeignKey(e => e.FlatId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.MemberSubLedger).WithMany(s => s.MemberBookings).HasForeignKey(e => e.MemberSubLedgerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Broker).WithMany(b => b.Bookings).HasForeignKey(e => e.BrokerId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.Condition).WithMany(c => c.Bookings).HasForeignKey(e => e.ConditionId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureDailyEntry(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DailyEntry>();
        entity.ToTable("daily_entries");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.SiteId).HasColumnName("site_id");
        entity.Property(e => e.EntryType).HasColumnName("entry_type").HasMaxLength(10);
        entity.Property(e => e.EntryDate).HasColumnName("entry_date");
        entity.Property(e => e.MainLedgerId).HasColumnName("main_ledger_id");
        entity.Property(e => e.SubLedgerId).HasColumnName("sub_ledger_id");
        entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(15,2)");
        entity.Property(e => e.CashBank).HasColumnName("cash_bank");
        entity.Property(e => e.Description).HasColumnName("description");
        entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.Site).WithMany(s => s.DailyEntries).HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.MainLedger).WithMany(m => m.DailyEntries).HasForeignKey(e => e.MainLedgerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.SubLedger).WithMany(s => s.DailyEntries).HasForeignKey(e => e.SubLedgerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.SiteId, e.EntryDate, e.EntryType, e.IsDeleted });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditLog>();
        entity.ToTable("audit_logs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.UserId).HasColumnName("user_id");
        entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(50);
        entity.Property(e => e.TableName).HasColumnName("table_name").HasMaxLength(100);
        entity.Property(e => e.RecordId).HasColumnName("record_id");
        entity.Property(e => e.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        entity.Property(e => e.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.HasOne(e => e.User).WithMany(u => u.AuditLogs).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
