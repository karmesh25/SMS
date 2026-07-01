using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.Auth;
using ABR.Application.DTOs.Roles;
using ABR.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/roles")]
[RequireRole("SuperAdmin")]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly IValidator<CreateRoleRequest> _createValidator;
    private readonly IValidator<UpdateRoleRequest> _updateValidator;

    public RoleController(
        IRoleService roleService,
        IValidator<CreateRoleRequest> createValidator,
        IValidator<UpdateRoleRequest> updateValidator)
    {
        _roleService = roleService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(roles));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetByIdAsync(id, cancellationToken);
        if (role is null)
            return NotFound(ApiResponse<RoleDto>.Fail("Role not found."));
        return Ok(ApiResponse<RoleDto>.Ok(role));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<RoleDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var role = await _roleService.CreateAsync(request, cancellationToken);
            return Ok(ApiResponse<RoleDto>.Ok(role, "Role created."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<RoleDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<RoleDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var role = await _roleService.UpdateAsync(id, request, cancellationToken);
            if (role is null)
                return NotFound(ApiResponse<RoleDto>.Fail("Role not found."));
            return Ok(ApiResponse<RoleDto>.Ok(role, "Role updated."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<RoleDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _roleService.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(ApiResponse<object>.Fail("Role not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Role deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("modules")]
    public ActionResult<ApiResponse<object>> GetModules()
    {
        var modules = AppModules.All.Select(key => new
        {
            key,
            label = AppModules.DisplayNames[key]
        });
        return Ok(ApiResponse<object>.Ok(modules));
    }
}
