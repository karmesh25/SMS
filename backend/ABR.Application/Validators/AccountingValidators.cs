using ABR.Application.DTOs.Accounting;
using FluentValidation;

namespace ABR.Application.Validators;

public class CreateDailyEntryDtoValidator : AbstractValidator<CreateDailyEntryDto>
{
    public CreateDailyEntryDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.EntryType).Must(t => t is "aavak" or "javak");
        RuleFor(x => x.MainLedgerId).NotEmpty();
        RuleFor(x => x.SubLedgerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CashBank).NotEmpty().MaximumLength(200);
    }
}

public class UpdateDailyEntryDtoValidator : AbstractValidator<UpdateDailyEntryDto>
{
    public UpdateDailyEntryDtoValidator()
    {
        RuleFor(x => x.EntryType).Must(t => t is "aavak" or "javak");
        RuleFor(x => x.MainLedgerId).NotEmpty();
        RuleFor(x => x.SubLedgerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CashBank).NotEmpty().MaximumLength(200);
    }
}
