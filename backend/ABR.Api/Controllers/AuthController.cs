using ABR.Application.Common;
using ABR.Application.DTOs.Auth;
using ABR.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(IAuthService authService, IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<LoginResponse>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            if (result is null)
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid username or password."));

            return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status423Locked, ApiResponse<LoginResponse>.Fail(ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(ApiResponse<LoginResponse>.Fail("Refresh token is required."));

        var result = await _authService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (result is null)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid or expired refresh token."));

        return Ok(ApiResponse<LoginResponse>.Ok(result, "Token refreshed."));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken cancellationToken)
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
        await _authService.LogoutAsync(token, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Logged out successfully."));
    }
}
