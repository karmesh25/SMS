using ABR.Application.DTOs.Vyaj;
using FluentValidation;

namespace ABR.Application.Validators;

public class CreateVyajPartyDtoValidator : AbstractValidator<CreateVyajPartyDto>
{
  public CreateVyajPartyDtoValidator()
  {
    RuleFor(x => x.SiteId).NotEmpty();
    RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
  }
}

public class UpdateVyajPartyDtoValidator : AbstractValidator<UpdateVyajPartyDto>
{
  public UpdateVyajPartyDtoValidator()
  {
    RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
  }
}

public class CreateVyajEntryDtoValidator : AbstractValidator<CreateVyajEntryDto>
{
  private static readonly string[] AllowedBases = ["flat", "month", "year", "day"];

  public CreateVyajEntryDtoValidator()
  {
    RuleFor(x => x.PartyId).NotEmpty();
    RuleFor(x => x.Principal).GreaterThan(0);
    RuleFor(x => x.RatePercent).GreaterThan(0);
    RuleFor(x => x.RateBasis).Must(b => AllowedBases.Contains(b, StringComparer.OrdinalIgnoreCase))
      .WithMessage("Rate basis must be flat, month, year, or day.");
    RuleFor(x => x.StartDate).NotEmpty();
  }
}

public class CreateVyajPaymentDtoValidator : AbstractValidator<CreateVyajPaymentDto>
{
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
