using ABR.Application.DTOs.Booking;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Services.Booking;

public sealed class InstallmentService : IInstallmentService
{
    private const string MemberLedgerName = "Member A/c";

    private readonly AbrDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IDailyEntryService _dailyEntryService;

    public InstallmentService(AbrDbContext context, IAuditLogService auditLog, IDailyEntryService dailyEntryService)
    {
        _context = context;
        _auditLog = auditLog;
        _dailyEntryService = dailyEntryService;
    }

    public async Task<InstallmentSummaryDto> GetByBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Installments)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new KeyNotFoundException("Booking not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var milestones = booking.Installments
            .OrderBy(i => i.SortOrder)
            .Select(i => MapInstallment(i, today))
            .ToList();

        var totalDue = milestones.Sum(m => m.DueAmount);
        var totalPaid = milestones.Sum(m => m.PaidAmount);
        var lastPaid = booking.Installments
            .Where(i => i.PaidDate.HasValue)
            .OrderByDescending(i => i.PaidDate)
            .FirstOrDefault();

        return new InstallmentSummaryDto
        {
            BookingId = bookingId,
            TotalDue = totalDue,
            TotalPaid = totalPaid,
            TotalRemaining = totalDue - totalPaid,
            PercentagePaid = totalDue > 0 ? Math.Round(totalPaid / totalDue * 100m, 2) : 0,
            DaysSinceLastPayment = lastPaid?.PaidDate is DateOnly d
                ? today.DayNumber - d.DayNumber
                : null,
            DaysFromBooking = today.DayNumber - booking.BookingDate.DayNumber,
            Milestones = milestones
        };
    }

    public async Task<InstallmentDto> RecordPaymentAsync(RecordPaymentDto dto, Guid? userId, CancellationToken cancellationToken = default)
    {
        var installment = await _context.BookingInstallments
            .Include(i => i.Booking).ThenInclude(b => b.Flat).ThenInclude(f => f.Wing)
            .Include(i => i.Booking).ThenInclude(b => b.MemberSubLedger).ThenInclude(s => s.MainLedger)
            .FirstOrDefaultAsync(i => i.Id == dto.BookingInstallmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Installment not found.");

        if (installment.Booking.Status != "active")
            throw new InvalidOperationException("Cannot record payment on a cancelled booking.");

        installment.PaidAmount += dto.Amount;
        installment.PaidDate = dto.PaidDate;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            installment.PaymentNotes = dto.Notes;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        installment.Status = ComputeStatus(installment, today);

        await _context.SaveChangesAsync(cancellationToken);
        await _auditLog.LogAsync(userId, "Update", "booking_installments", installment.Id, newValues: dto.Amount.ToString(), cancellationToken: cancellationToken);

        var booking = installment.Booking;
        var siteId = booking.Flat.Wing.SiteId;
        var mainLedger = await _context.MainLedgers
            .FirstOrDefaultAsync(m => m.SiteId == siteId && m.LedgerName == MemberLedgerName, cancellationToken)
            ?? booking.MemberSubLedger.MainLedger;

        await _dailyEntryService.CreateFromInstallmentAsync(
            siteId,
            mainLedger.Id,
            booking.MemberSubLedgerId,
            dto.Amount,
            dto.PaidDate,
            installment.MilestoneName,
            userId,
            cancellationToken);

        return MapInstallment(installment, today);
    }

    internal static string ComputeStatus(BookingInstallment i, DateOnly today)
    {
        if (i.PaidAmount >= i.DueAmount) return "paid";
        if (i.PaidAmount > 0) return "partial";
        if (i.DueDate < today) return "overdue";
        return "pending";
    }

    private static InstallmentDto MapInstallment(BookingInstallment i, DateOnly today) => new()
    {
        Id = i.Id,
        BookingId = i.BookingId,
        MilestoneName = i.MilestoneName,
        SortOrder = i.SortOrder,
        DueAmount = i.DueAmount,
        PaidAmount = i.PaidAmount,
        RemainingAmount = Math.Max(0, i.DueAmount - i.PaidAmount),
        DueDate = i.DueDate,
        PaidDate = i.PaidDate,
        Status = ComputeStatus(i, today)
    };
}
