using ABR.Domain.Entities;
using ABR.Infrastructure.Services.Booking;

namespace ABR.Infrastructure.Tests;

public class BookingServiceTests
{
    [Theory]
    [InlineData(1000, 5000, 1, 5000000, 50000)]
    [InlineData(850.5, 4200, 2, 3572100, 71442)]
    public void CalculatePricing_ComputesTotalAndBrokerage(
        decimal sqft, decimal rate, decimal brokeragePct, decimal expectedTotal, decimal expectedBrokerage)
    {
        var (total, brokerage) = BookingService.CalculatePricing(sqft, rate, brokeragePct);
        Assert.Equal(expectedTotal, total);
        Assert.Equal(expectedBrokerage, brokerage);
    }

    [Fact]
    public void GenerateInstallments_CreatesMilestonesFromCondition()
    {
        var condition = new Condition
        {
            Items =
            [
                new ConditionItem { MilestoneName = "Booking", Percentage = 10, DueAfterDays = 0, SortOrder = 1 },
                new ConditionItem { MilestoneName = "Slab", Percentage = 20, DueAfterDays = 90, SortOrder = 2 }
            ]
        };
        var booking = new Booking { Id = Guid.NewGuid() };
        var bookingDate = new DateOnly(2026, 1, 15);

        BookingService.GenerateInstallments(booking, condition, bookingDate, 1_000_000m);

        Assert.Equal(2, booking.Installments.Count);
        Assert.Equal(100_000m, booking.Installments.First(i => i.SortOrder == 1).DueAmount);
        Assert.Equal(200_000m, booking.Installments.First(i => i.SortOrder == 2).DueAmount);
        Assert.Equal(bookingDate.AddDays(90), booking.Installments.First(i => i.SortOrder == 2).DueDate);
    }

    [Fact]
    public void GenerateInstallments_UsesFixedAmountWhenPercentageMissing()
    {
        var condition = new Condition
        {
            Items =
            [
                new ConditionItem { MilestoneName = "Token", Amount = 50_000, DueAfterDays = 0, SortOrder = 1 }
            ]
        };
        var booking = new Booking { Id = Guid.NewGuid() };

        BookingService.GenerateInstallments(booking, condition, DateOnly.FromDateTime(DateTime.UtcNow), 1_000_000m);

        Assert.Single(booking.Installments);
        Assert.Equal(50_000m, booking.Installments.First().DueAmount);
    }
}
