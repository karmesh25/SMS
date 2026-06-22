using ABR.Application.DTOs.Reports;
using FluentValidation;

namespace ABR.Application.Validators;

public class AllEntryReportFilterDtoValidator : AbstractValidator<AllEntryReportFilterDto>
{
    public AllEntryReportFilterDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.DateTo).GreaterThanOrEqualTo(x => x.DateFrom);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class BalanceSheetFilterDtoValidator : AbstractValidator<BalanceSheetFilterDto>
{
    public BalanceSheetFilterDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.DateTo).GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}

public class TillDateReportFilterDtoValidator : AbstractValidator<TillDateReportFilterDto>
{
    public TillDateReportFilterDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.MovementType).Must(t => t is "all" or "no-movement");
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class MonthwiseReportFilterDtoValidator : AbstractValidator<MonthwiseReportFilterDto>
{
    public MonthwiseReportFilterDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.DateTo).GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}

public class BankStatementFilterDtoValidator : AbstractValidator<BankStatementFilterDto>
{
    public BankStatementFilterDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.DateTo).GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}

public class SellDetailsFilterDtoValidator : AbstractValidator<SellDetailsFilterDto>
{
    public SellDetailsFilterDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
    }
}

public class InstallmentReportFilterDtoValidator : AbstractValidator<InstallmentReportFilterDto>
{
    public InstallmentReportFilterDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x).Must(x => x.BookingId.HasValue || !string.IsNullOrWhiteSpace(x.FlatNo))
            .WithMessage("BookingId or FlatNo is required.");
    }
}
