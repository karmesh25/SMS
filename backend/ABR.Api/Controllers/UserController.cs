using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.Auth;
using ABR.Application.DTOs.Users;
using ABR.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
[RequirePermission(AppModules.Users, PermissionLevel.Manage)]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IValidator<CreateUserRequest> _createValidator;

    public UserController(IUserService userService, IValidator<CreateUserRequest> createValidator)
    {
        _userService = userService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(users));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<UserDto>.Fail("User not found."));

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<UserDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var user = await _userService.CreateAsync(request, cancellationToken);
            return Ok(ApiResponse<UserDto>.Ok(user, "User created."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<UserDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateAsync(id, request, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse<UserDto>.Fail("User not found."));

        return Ok(ApiResponse<UserDto>.Ok(user, "User updated."));
    }

    [HttpPut("{id:guid}/toggle-active")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleActive(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _userService.ToggleActiveAsync(id, cancellationToken);
        if (!updated)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        return Ok(ApiResponse<object>.Ok(new { }, "User status updated."));
    }

    [HttpPut("{id:guid}/force-password-reset")]
    public async Task<ActionResult<ApiResponse<object>>> ForcePasswordReset(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _userService.ForcePasswordResetAsync(id, cancellationToken);
        if (!updated)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        return Ok(ApiResponse<object>.Ok(new { }, "Password reset flagged."));
    }
}
