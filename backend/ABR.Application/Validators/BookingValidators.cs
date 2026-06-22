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
        RuleFor(x => x.BrokeragePct).InclusiveBetween(0, 2);
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
        RuleFor(x => x.BrokeragePct).InclusiveBetween(0, 2);
        RuleFor(x => x.CustomerType).Must(t => t is "real" or "investor");
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
