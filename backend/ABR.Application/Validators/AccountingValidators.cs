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
        RuleFor(x => x.Description).MaximumLength(200);
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
        RuleFor(x => x.Description).MaximumLength(200);
    }
}

public class CreateJournalVoucherDtoValidator : AbstractValidator<CreateJournalVoucherDto>
{
    public CreateJournalVoucherDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.Lines).NotNull().Must(lines => lines.Count >= 2).WithMessage("At least 2 lines are required.");
        RuleForEach(x => x.Lines).SetValidator(new JournalVoucherLineUpsertDtoValidator());
        RuleFor(x => x).Must(AccountingValidatorRules.HaveMatchingDebitCreditTotals)
            .WithMessage("Debit and Credit totals must match before saving.");
    }
}

public class UpdateJournalVoucherDtoValidator : AbstractValidator<UpdateJournalVoucherDto>
{
    public UpdateJournalVoucherDtoValidator()
    {
        RuleFor(x => x.Lines).NotNull().Must(lines => lines.Count >= 2).WithMessage("At least 2 lines are required.");
        RuleForEach(x => x.Lines).SetValidator(new JournalVoucherLineUpsertDtoValidator());
        RuleFor(x => x).Must(AccountingValidatorRules.HaveMatchingDebitCreditTotals)
            .WithMessage("Debit and Credit totals must match before saving.");
    }
}

public class JournalVoucherLineUpsertDtoValidator : AbstractValidator<JournalVoucherLineUpsertDto>
{
    public JournalVoucherLineUpsertDtoValidator()
    {
        RuleFor(x => x.SubLedgerId).NotEmpty();
        RuleFor(x => x.EntryType).Must(v => v is "dr" or "cr");
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

internal static class AccountingValidatorRules
{
    public static bool HaveMatchingDebitCreditTotals(IEnumerable<JournalVoucherLineUpsertDto> lines)
    {
        var debit = lines.Where(l => l.EntryType == "dr").Sum(l => l.Amount);
        var credit = lines.Where(l => l.EntryType == "cr").Sum(l => l.Amount);
        return debit == credit;
    }

    public static bool HaveMatchingDebitCreditTotals(CreateJournalVoucherDto dto) => HaveMatchingDebitCreditTotals(dto.Lines);
    public static bool HaveMatchingDebitCreditTotals(UpdateJournalVoucherDto dto) => HaveMatchingDebitCreditTotals(dto.Lines);
}
