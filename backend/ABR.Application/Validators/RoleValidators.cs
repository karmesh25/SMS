using ABR.Application.Common;
using ABR.Application.DTOs.Roles;
using FluentValidation;

namespace ABR.Application.Validators;

public class RolePermissionDtoValidator : AbstractValidator<RolePermissionDto>
{
    public RolePermissionDtoValidator()
    {
        RuleFor(x => x.ModuleKey).Must(AppModules.All.Contains).WithMessage("Invalid module key.");
        RuleFor(x => x).Must(p => !p.CanManage || p.CanView)
            .WithMessage("Manage permission requires view permission.");
    }
}

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Permissions).NotEmpty();
        RuleForEach(x => x.Permissions).SetValidator(new RolePermissionDtoValidator());
        RuleFor(x => x.Permissions).Must(p => p.Any(x => x.CanView))
            .WithMessage("At least one module must have view permission.");
        RuleFor(x => x.Name).Must(n => !SystemRoleNames.All.Contains(n))
            .WithMessage("Cannot use a reserved system role name.");
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Permissions).NotEmpty();
        RuleForEach(x => x.Permissions).SetValidator(new RolePermissionDtoValidator());
        RuleFor(x => x.Permissions).Must(p => p.Any(x => x.CanView))
            .WithMessage("At least one module must have view permission.");
    }
}
