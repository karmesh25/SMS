using ABR.Application.DTOs.Auth;
using ABR.Application.Validators;

namespace ABR.Application.Tests;

public class AuthValidatorsTests
{
    [Fact]
    public void LoginRequestValidator_RejectsEmptyUsername()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest { Username = "", Password = "ValidPass1" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequest.Username));
    }

    [Fact]
    public void LoginRequestValidator_RejectsShortPassword()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest { Username = "admin", Password = "short" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void LoginRequestValidator_AcceptsValidRequest()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest { Username = "admin", Password = "Admin@123" });
        Assert.True(result.IsValid);
    }
}
