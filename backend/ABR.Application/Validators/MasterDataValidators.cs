using ABR.Application.DTOs.MasterData;
using FluentValidation;

namespace ABR.Application.Validators;

public class CreateSiteDtoValidator : AbstractValidator<CreateSiteDto>
{
    public CreateSiteDtoValidator()
    {
        RuleFor(x => x.SiteName).NotEmpty().MaximumLength(200);
    }
}

public class CreateWingDtoValidator : AbstractValidator<CreateWingDto>
{
    public CreateWingDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.WingName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Floors).GreaterThan(0);
        RuleFor(x => x.FlatsPerFloor).GreaterThan(0);
        RuleFor(x => x.Shops).GreaterThanOrEqualTo(0);
    }
}

public class CreatePlotDtoValidator : AbstractValidator<CreatePlotDto>
{
    public CreatePlotDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.PlotName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PlotCount).GreaterThan(0);
    }
}

public class UpdatePlotDtoValidator : AbstractValidator<UpdatePlotDto>
{
    public UpdatePlotDtoValidator()
    {
        RuleFor(x => x.PlotName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PlotCount).GreaterThan(0);
    }
}

public class CreateMainLedgerDtoValidator : AbstractValidator<CreateMainLedgerDto>
{
    public CreateMainLedgerDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.LedgerName).NotEmpty().MaximumLength(200);
    }
}

public class CreateSubLedgerDtoValidator : AbstractValidator<CreateSubLedgerDto>
{
    public CreateSubLedgerDtoValidator()
    {
        RuleFor(x => x.MainLedgerId).NotEmpty();
        RuleFor(x => x.LedgerName).NotEmpty().MaximumLength(200);
    }
}

public class CreateConditionDtoValidator : AbstractValidator<CreateConditionDto>
{
    public CreateConditionDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.ConditionName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ConditionType).Must(t => t is "auto" or "manual");
    }
}

public class CreateConditionItemDtoValidator : AbstractValidator<CreateConditionItemDto>
{
    public CreateConditionItemDtoValidator()
    {
        RuleFor(x => x.MilestoneName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DueAfterDays).GreaterThanOrEqualTo(0);
    }
}

public class CreateBankAccountDtoValidator : AbstractValidator<CreateBankAccountDto>
{
    public CreateBankAccountDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AccountNo).NotEmpty().MaximumLength(50);
    }
}

public class CreateBrokerDtoValidator : AbstractValidator<CreateBrokerDto>
{
    public CreateBrokerDtoValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateConditionDtoValidator : AbstractValidator<UpdateConditionDto>
{
    public UpdateConditionDtoValidator()
    {
        RuleFor(x => x.ConditionName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ConditionType).Must(t => t is "auto" or "manual");
    }
}

public class UpdateConditionItemDtoValidator : AbstractValidator<UpdateConditionItemDto>
{
    public UpdateConditionItemDtoValidator()
    {
        RuleFor(x => x.MilestoneName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DueAfterDays).GreaterThanOrEqualTo(0);
    }
}
