using ABR.Application.DTOs.Booking;
using ABR.Application.Validators;

namespace ABR.Application.Tests;

public class BookingValidatorsTests
{
    [Fact]
    public void CreateBookingDtoValidator_RejectsInvalidCustomerType()
    {
        var validator = new CreateBookingDtoValidator();
        var result = validator.Validate(new CreateBookingDto
        {
            FlatId = Guid.NewGuid(),
            MemberName = "Member",
            ConditionId = Guid.NewGuid(),
            Sqft = 1000,
            Rate = 5000,
            BrokeragePct = 1,
            CustomerType = "invalid"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateBookingDtoValidator_RejectsZeroSqft()
    {
        var validator = new CreateBookingDtoValidator();
        var result = validator.Validate(new CreateBookingDto
        {
            FlatId = Guid.NewGuid(),
            MemberName = "Member",
            ConditionId = Guid.NewGuid(),
            Sqft = 0,
            Rate = 5000,
            BrokeragePct = 1,
            CustomerType = "real"
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookingDto.Sqft));
    }

    [Fact]
    public void CreateBookingDtoValidator_AcceptsValidDto()
    {
        var validator = new CreateBookingDtoValidator();
        var result = validator.Validate(new CreateBookingDto
        {
            FlatId = Guid.NewGuid(),
            MemberName = "Member",
            ConditionId = Guid.NewGuid(),
            Sqft = 1000,
            Rate = 5000,
            BrokeragePct = 1.5m,
            CustomerType = "investor"
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateBookingDtoValidator_AcceptsBrokerageAboveTwoPercent_WhenWithinTotal()
    {
        var validator = new CreateBookingDtoValidator();
        var result = validator.Validate(new CreateBookingDto
        {
            FlatId = Guid.NewGuid(),
            MemberName = "Member",
            ConditionId = Guid.NewGuid(),
            Sqft = 1000,
            Rate = 15000,
            BrokeragePct = 5,
            CustomerType = "real"
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateBookingDtoValidator_RejectsBrokerageAboveTotal()
    {
        var validator = new CreateBookingDtoValidator();
        var result = validator.Validate(new CreateBookingDto
        {
            FlatId = Guid.NewGuid(),
            MemberName = "Member",
            ConditionId = Guid.NewGuid(),
            Sqft = 1000,
            Rate = 5000,
            BrokeragePct = 150,
            CustomerType = "real"
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Brokerage cannot exceed total price.");
    }
}
