using ABR.Application.DTOs.Booking;
using FluentValidation;

namespace ABR.Application.Validators;

public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingDtoValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.MemberName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ConditionId).NotEmpty();
        RuleFor(x => x.Sqft).GreaterThan(0);
        RuleFor(x => x.Rate).GreaterThan(0);
        RuleFor(x => x.BrokeragePct).GreaterThanOrEqualTo(0);
        RuleFor(x => x)
            .Must(dto => BookingValidationRules.BrokerageWithinTotal(dto.Sqft, dto.Rate, dto.BrokeragePct))
            .WithMessage("Brokerage cannot exceed total price.");
        RuleFor(x => x.CustomerType).Must(t => t is "real" or "investor");
    }
}

public class UpdateBookingDtoValidator : AbstractValidator<UpdateBookingDto>
{
    public UpdateBookingDtoValidator()
    {
        RuleFor(x => x.MemberName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ConditionId).NotEmpty();
        RuleFor(x => x.Sqft).GreaterThan(0);
        RuleFor(x => x.Rate).GreaterThan(0);
        RuleFor(x => x.BrokeragePct).GreaterThanOrEqualTo(0);
        RuleFor(x => x)
            .Must(dto => BookingValidationRules.BrokerageWithinTotal(dto.Sqft, dto.Rate, dto.BrokeragePct))
            .WithMessage("Brokerage cannot exceed total price.");
        RuleFor(x => x.CustomerType).Must(t => t is "real" or "investor");
    }
}

internal static class BookingValidationRules
{
    internal static bool BrokerageWithinTotal(decimal sqft, decimal rate, decimal brokeragePct)
    {
        var total = sqft * rate;
        if (total <= 0)
            return true;

        var brokerage = Math.Round(total * (brokeragePct / 100m), 2);
        return brokerage <= total;
    }
}

public class CancelBookingDtoValidator : AbstractValidator<CancelBookingDto>
{
    public CancelBookingDtoValidator()
    {
        RuleFor(x => x.CancelDate).NotEmpty();
    }
}

public class RecordPaymentDtoValidator : AbstractValidator<RecordPaymentDto>
{
    public RecordPaymentDtoValidator()
    {
        RuleFor(x => x.BookingInstallmentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaidDate).NotEmpty();
    }
}
