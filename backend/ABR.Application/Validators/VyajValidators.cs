using ABR.Application.DTOs.Vyaj;
using FluentValidation;

namespace ABR.Application.Validators;

public class CreateVyajPartyDtoValidator : AbstractValidator<CreateVyajPartyDto>
{
  private static readonly string[] AllowedKinds = ["direct", "asset"];

  public CreateVyajPartyDtoValidator()
  {
    RuleFor(x => x.SiteId).NotEmpty();
    RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
    RuleFor(x => x.PartyKind).Must(k => AllowedKinds.Contains(k, StringComparer.OrdinalIgnoreCase))
      .WithMessage("Party kind must be direct or asset.");
  }
}

public class UpdateVyajPartyDtoValidator : AbstractValidator<UpdateVyajPartyDto>
{
  private static readonly string[] AllowedKinds = ["direct", "asset"];

  public UpdateVyajPartyDtoValidator()
  {
    RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
    RuleFor(x => x.PartyKind).Must(k => AllowedKinds.Contains(k, StringComparer.OrdinalIgnoreCase))
      .WithMessage("Party kind must be direct or asset.");
  }
}

public class CreateVyajEntryDtoValidator : AbstractValidator<CreateVyajEntryDto>
{
  private static readonly string[] AllowedBases = ["flat", "month", "year", "day", "custom"];
  private static readonly int[] AllowedPeriods = [3, 6, 9];

  public CreateVyajEntryDtoValidator()
  {
    RuleFor(x => x.PartyId).NotEmpty();
    // Principal may be 0/null; service resolves from open entries on continuation.
    RuleFor(x => x.Principal).GreaterThanOrEqualTo(0).When(x => x.Principal.HasValue);
    RuleFor(x => x.RatePercent).GreaterThan(0);
    RuleFor(x => x.RateBasis).Must(b => AllowedBases.Contains(b, StringComparer.OrdinalIgnoreCase))
      .WithMessage("Rate basis must be flat, month, year, day, or custom.");
    RuleFor(x => x.RatePeriodMonths)
      .Must(p => p is null || AllowedPeriods.Contains(p.Value))
      .WithMessage("Rate period must be 3, 6, or 9 months.");
    RuleFor(x => x.CustomDays)
      .NotEmpty()
      .GreaterThan(0)
      .When(x => string.Equals(x.RateBasis, "custom", StringComparison.OrdinalIgnoreCase))
      .WithMessage("Custom days is required and must be greater than 0.");
    RuleFor(x => x.EmiAmount).GreaterThan(0).When(x => x.EmiAmount.HasValue);
    RuleFor(x => x.EmiAmount)
      .LessThan(x => x.Principal!.Value)
      .When(x => x.EmiAmount.HasValue && x.Principal is > 0)
      .WithMessage("First EMI must be less than principal.");
    RuleFor(x => x.StartDate).NotEmpty();
  }
}

public class CreateVyajRateBasisOptionDtoValidator : AbstractValidator<CreateVyajRateBasisOptionDto>
{
  public CreateVyajRateBasisOptionDtoValidator()
  {
    RuleFor(x => x.SiteId).NotEmpty();
    RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
    RuleFor(x => x.DayCount).GreaterThan(0);
  }
}

public class UpdateVyajRateBasisOptionDtoValidator : AbstractValidator<UpdateVyajRateBasisOptionDto>
{
  public UpdateVyajRateBasisOptionDtoValidator()
  {
    RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
    RuleFor(x => x.DayCount).GreaterThan(0);
  }
}

public class CreateVyajPaymentDtoValidator : AbstractValidator<CreateVyajPaymentDto>
{
  // capitalize is system-generated only; clients may only post interest/principal
  private static readonly string[] AllowedTypes = ["interest", "principal"];

  public CreateVyajPaymentDtoValidator()
  {
    RuleFor(x => x.EntryId).NotEmpty();
    RuleFor(x => x.Amount).GreaterThan(0);
    RuleFor(x => x.PaymentDate).NotEmpty();
    RuleFor(x => x.PaymentType).Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
      .WithMessage("Payment type must be interest or principal.");
  }
}
