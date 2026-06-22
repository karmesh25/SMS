using ABR.Application.DTOs.Booking;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services.Booking;

public sealed class BookingService : IBookingService
{
    private const string MemberLedgerName = "Member A/c";

    private readonly AbrDbContext _context;
    private readonly IAuditLogService _auditLog;

    public BookingService(AbrDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<PagedResultDto<BookingListDto>> GetBySiteAsync(Guid siteId, BookingQueryDto query, CancellationToken cancellationToken = default)
    {
        var q = _context.Bookings
            .Include(b => b.Flat).ThenInclude(f => f.Wing)
            .Include(b => b.MemberSubLedger)
            .Where(b => b.Flat.Wing.SiteId == siteId);

        if (query.WingId.HasValue)
            q = q.Where(b => b.Flat.WingId == query.WingId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(b => b.Status == query.Status);
        if (query.FromDate.HasValue)
            q = q.Where(b => b.BookingDate >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(b => b.BookingDate <= query.ToDate.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(b => b.BookingDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BookingListDto
            {
                Id = b.Id,
                FlatNo = b.Flat.FlatNo,
                WingName = b.Flat.Wing.WingName,
                MemberName = b.MemberSubLedger.LedgerName,
                BookingDate = b.BookingDate,
                TotalPrice = b.TotalPrice,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<BookingListDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<BookingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await LoadBookingQuery().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        return booking is null ? null : MapBooking(booking);
    }

    public async Task<BookingDto?> GetByFlatIdAsync(Guid flatId, CancellationToken cancellationToken = default)
    {
        var booking = await LoadBookingQuery()
            .Where(b => b.FlatId == flatId && b.Status == "active")
            .OrderByDescending(b => b.BookingDate)
            .FirstOrDefaultAsync(cancellationToken);

        booking ??= await LoadBookingQuery()
            .Where(b => b.FlatId == flatId)
            .OrderByDescending(b => b.BookingDate)
            .FirstOrDefaultAsync(cancellationToken);

        return booking is null ? null : MapBooking(booking);
    }

    public async Task<FlatDetailDto?> GetFlatDetailAsync(Guid flatId, CancellationToken cancellationToken = default)
    {
        var flat = await _context.Flats
            .Include(f => f.Wing)
            .FirstOrDefaultAsync(f => f.Id == flatId, cancellationToken);
        if (flat is null) return null;

        return new FlatDetailDto
        {
            Id = flat.Id,
            FlatNo = flat.FlatNo,
            Sqft = flat.Sqft,
            Status = flat.Status,
            WingName = flat.Wing.WingName
        };
    }

    public async Task<BookingDto> CreateAsync(CreateBookingDto dto, Guid? userId, CancellationToken cancellationToken = default)
    {
        var flat = await _context.Flats
            .Include(f => f.Wing)
            .FirstOrDefaultAsync(f => f.Id == dto.FlatId, cancellationToken)
            ?? throw new InvalidOperationException("Flat not found.");

        if (flat.Status != "available")
            throw new InvalidOperationException("Flat is not available for booking.");

        var condition = await _context.Conditions
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == dto.ConditionId, cancellationToken)
            ?? throw new InvalidOperationException("Condition not found.");

        if (condition.Items.Count == 0)
            throw new InvalidOperationException("Condition has no payment milestones.");

        var siteId = flat.Wing.SiteId;
        var memberSubLedger = await ResolveMemberSubLedgerAsync(siteId, dto.FlatId, dto.MemberName, cancellationToken);

        var (totalPrice, brokerageAmount) = CalculatePricing(dto.Sqft, dto.Rate, dto.BrokeragePct);

        var booking = new Domain.Entities.Booking
        {
            FlatId = dto.FlatId,
            MemberSubLedgerId = memberSubLedger.Id,
            BrokerId = dto.BrokerId,
            ConditionId = dto.ConditionId,
            BookingDate = dto.BookingDate,
            CustomerContact = dto.CustomerContact,
            Sqft = dto.Sqft,
            Rate = dto.Rate,
            TotalPrice = totalPrice,
            BrokeragePct = dto.BrokeragePct,
            BrokerageAmount = brokerageAmount,
            CustomerType = dto.CustomerType,
            IsArjaMarjaSell = dto.IsArjaMarjaSell,
            Status = "active",
            Notes = dto.Notes
        };

        flat.Status = "booked";
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(cancellationToken);

        GenerateInstallments(booking, condition, dto.BookingDate, totalPrice);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync(userId, "Create", "bookings", booking.Id, newValues: booking.Id.ToString(), cancellationToken: cancellationToken);

        return (await GetByIdAsync(booking.Id, cancellationToken))!;
    }

    public async Task<BookingDto?> UpdateAsync(Guid id, UpdateBookingDto dto, Guid? userId, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Flat).ThenInclude(f => f.Wing)
            .Include(b => b.Installments)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking is null) return null;
        if (booking.Status != "active")
            throw new InvalidOperationException("Only active bookings can be updated.");

        if (dto.ConditionId != booking.ConditionId && booking.Installments.Any(i => i.PaidAmount > 0))
            throw new InvalidOperationException("Cannot change condition after payments have been recorded.");

        var siteId = booking.Flat.Wing.SiteId;
        var memberSubLedger = await ResolveMemberSubLedgerAsync(siteId, booking.FlatId, dto.MemberName, cancellationToken);

        booking.MemberSubLedgerId = memberSubLedger.Id;
        booking.BrokerId = dto.BrokerId;
        booking.BookingDate = dto.BookingDate;
        booking.CustomerContact = dto.CustomerContact;
        booking.Sqft = dto.Sqft;
        booking.Rate = dto.Rate;
        booking.BrokeragePct = dto.BrokeragePct;
        booking.CustomerType = dto.CustomerType;
        booking.IsArjaMarjaSell = dto.IsArjaMarjaSell;
        booking.Notes = dto.Notes;

        var (totalPrice, brokerageAmount) = CalculatePricing(dto.Sqft, dto.Rate, dto.BrokeragePct);
        booking.TotalPrice = totalPrice;
        booking.BrokerageAmount = brokerageAmount;

        if (dto.ConditionId != booking.ConditionId)
        {
            booking.ConditionId = dto.ConditionId;
            _context.BookingInstallments.RemoveRange(booking.Installments);
            var condition = await _context.Conditions
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == dto.ConditionId, cancellationToken)
                ?? throw new InvalidOperationException("Condition not found.");
            GenerateInstallments(booking, condition, dto.BookingDate, totalPrice);
        }
        else if (booking.Installments.Count > 0)
        {
            RecalculateInstallmentDueAmounts(booking, totalPrice);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Update", "bookings", booking.Id, cancellationToken: cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<BookingDto?> CancelAsync(Guid id, CancelBookingDto dto, Guid? userId, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Flat)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking is null) return null;
        if (booking.Status == "cancelled")
            throw new InvalidOperationException("Booking is already cancelled.");

        booking.Status = "cancelled";
        booking.CancelDate = dto.CancelDate;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            booking.Notes = dto.Notes;

        booking.Flat.Status = booking.IsArjaMarjaSell ? "cancelled" : "available";

        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Update", "bookings", booking.Id, newValues: "cancelled", cancellationToken: cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<BookingDto?> UpdateDastavejAsync(Guid id, UpdateDastavejDto dto, Guid? userId, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings.FindAsync([id], cancellationToken);
        if (booking is null) return null;

        if (dto.DastavejDate.HasValue) booking.DastavejDate = dto.DastavejDate;
        if (dto.SatakhatDate.HasValue) booking.SatakhatDate = dto.SatakhatDate;
        if (dto.DocumentNumber is not null) booking.DocumentNumber = dto.DocumentNumber;
        if (dto.ServiceTax.HasValue) booking.ServiceTax = dto.ServiceTax;
        if (dto.Notes is not null) booking.Notes = dto.Notes;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Update", "bookings", booking.Id, newValues: "dastavej-satakhat", cancellationToken: cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<DastavejBookingListDto>> GetDastavejListAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Flat).ThenInclude(f => f.Wing)
            .Include(b => b.MemberSubLedger)
            .Where(b => b.Flat.Wing.SiteId == siteId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync(cancellationToken);

        return bookings.Select(b => new DastavejBookingListDto
        {
            Id = b.Id,
            FlatNo = b.Flat.FlatNo,
            MemberName = b.MemberSubLedger.LedgerName,
            BookingDate = b.BookingDate,
            DastavejDate = b.DastavejDate,
            SatakhatDate = b.SatakhatDate,
            DocumentNumber = b.DocumentNumber,
            ServiceTax = b.ServiceTax,
            Status = b.Status
        }).ToList();
    }

    private IQueryable<Domain.Entities.Booking> LoadBookingQuery() =>
        _context.Bookings
            .Include(b => b.Flat).ThenInclude(f => f.Wing)
            .Include(b => b.MemberSubLedger)
            .Include(b => b.Broker)
            .Include(b => b.Condition);

    private async Task<SubLedger> ResolveMemberSubLedgerAsync(Guid siteId, Guid flatId, string memberName, CancellationToken cancellationToken)
    {
        var mainLedger = await _context.MainLedgers
            .FirstOrDefaultAsync(m => m.SiteId == siteId && m.LedgerName == MemberLedgerName, cancellationToken)
            ?? throw new InvalidOperationException("Member A/c ledger not found for site.");

        var trimmedName = memberName.Trim();
        var existing = await _context.SubLedgers
            .FirstOrDefaultAsync(s => s.MainLedgerId == mainLedger.Id && s.FlatId == flatId && s.LedgerName == trimmedName, cancellationToken);

        if (existing is not null) return existing;

        existing = await _context.SubLedgers
            .FirstOrDefaultAsync(s => s.MainLedgerId == mainLedger.Id && s.FlatId == flatId, cancellationToken);

        if (existing is not null)
        {
            existing.LedgerName = trimmedName;
            return existing;
        }

        var sub = new SubLedger
        {
            MainLedgerId = mainLedger.Id,
            LedgerName = trimmedName,
            FlatId = flatId
        };
        _context.SubLedgers.Add(sub);
        await _context.SaveChangesAsync(cancellationToken);
        return sub;
    }

    internal static (decimal TotalPrice, decimal BrokerageAmount) CalculatePricing(decimal sqft, decimal rate, decimal brokeragePct)
    {
        var totalPrice = Math.Round(sqft * rate, 2);
        var brokerageAmount = Math.Round(totalPrice * (brokeragePct / 100m), 2);
        return (totalPrice, brokerageAmount);
    }

    internal static void GenerateInstallments(Domain.Entities.Booking booking, Condition condition, DateOnly bookingDate, decimal totalPrice)
    {
        foreach (var item in condition.Items.OrderBy(i => i.SortOrder))
        {
            var dueAmount = item.Percentage.HasValue
                ? Math.Round(totalPrice * (item.Percentage.Value / 100m), 2)
                : item.Amount ?? 0m;

            booking.Installments.Add(new BookingInstallment
            {
                BookingId = booking.Id,
                ConditionItemId = item.Id,
                MilestoneName = item.MilestoneName,
                SortOrder = item.SortOrder,
                DueAmount = dueAmount,
                PaidAmount = 0,
                DueDate = bookingDate.AddDays(item.DueAfterDays),
                Status = "pending"
            });
        }
    }

    internal static void RecalculateInstallmentDueAmounts(Domain.Entities.Booking booking, decimal totalPrice)
    {
        foreach (var inst in booking.Installments.Where(i => i.PaidAmount == 0))
        {
            if (inst.ConditionItemId.HasValue)
            {
                // keep proportional recalc only when no template reload happened
            }
        }
    }

    private static BookingDto MapBooking(Domain.Entities.Booking b) => new()
    {
        Id = b.Id,
        FlatId = b.FlatId,
        FlatNo = b.Flat.FlatNo,
        WingName = b.Flat.Wing.WingName,
        SiteId = b.Flat.Wing.SiteId,
        MemberSubLedgerId = b.MemberSubLedgerId,
        MemberName = b.MemberSubLedger.LedgerName,
        BrokerId = b.BrokerId,
        BrokerName = b.Broker?.Name,
        ConditionId = b.ConditionId,
        ConditionName = b.Condition.ConditionName,
        BookingDate = b.BookingDate,
        CustomerContact = b.CustomerContact,
        Sqft = b.Sqft,
        Rate = b.Rate,
        TotalPrice = b.TotalPrice,
        BrokeragePct = b.BrokeragePct,
        BrokerageAmount = b.BrokerageAmount,
        CustomerType = b.CustomerType,
        IsArjaMarjaSell = b.IsArjaMarjaSell,
        Status = b.Status,
        CancelDate = b.CancelDate,
        DastavejDate = b.DastavejDate,
        SatakhatDate = b.SatakhatDate,
        DocumentNumber = b.DocumentNumber,
        ServiceTax = b.ServiceTax,
        Notes = b.Notes
    };
}
