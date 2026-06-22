using ABR.Application.DTOs.Auth;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ABR.Infrastructure.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenPasswordIsWrong()
    {
        await using var context = TestDbContextFactory.Create();
        var user = new User
        {
            Username = "staff",
            Email = "staff@test.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass1"),
            Role = "Admin",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateAuthService(context, enforceDeviceLock: false);

        var result = await service.LoginAsync(new LoginRequest
        {
            Username = "staff",
            Password = "WrongPass1"
        });

        Assert.Null(result);
        var updated = await context.Users.FindAsync(user.Id);
        Assert.Equal(1, updated!.FailedAttempts);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenUserNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        var service = CreateAuthService(context, enforceDeviceLock: false);

        var result = await service.LoginAsync(new LoginRequest
        {
            Username = "missing",
            Password = "AnyPass123"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenDeviceLockEnforcedAndDeviceInvalid()
    {
        await using var context = TestDbContextFactory.Create();
        var user = new User
        {
            Username = "admin",
            Email = "admin@test.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "SuperAdmin",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var fingerprint = new Mock<IDeviceFingerprintService>();
        fingerprint.Setup(f => f.IsDeviceLockEnforced()).Returns(true);

        var deviceLicense = new Mock<IDeviceLicenseService>();
        deviceLicense.Setup(d => d.VerifyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeviceVerifyResponseDto { IsValid = false, Result = "InvalidDevice" });

        var service = CreateAuthService(context, enforceDeviceLock: true, fingerprint.Object, deviceLicense.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync(new LoginRequest { Username = "admin", Password = "Admin@123" }));
    }

    private static AuthService CreateAuthService(
        Persistence.AbrDbContext context,
        bool enforceDeviceLock,
        IDeviceFingerprintService? fingerprintService = null,
        IDeviceLicenseService? deviceLicenseService = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-min-32-chars-long!!",
                ["Jwt:Issuer"] = "abr-test",
                ["Jwt:Audience"] = "abr-test",
                ["Jwt:ExpiryHours"] = "8",
                ["Security:EnforceDeviceLock"] = enforceDeviceLock.ToString()
            })
            .Build();

        fingerprintService ??= Mock.Of<IDeviceFingerprintService>(f =>
            f.IsDeviceLockEnforced() == enforceDeviceLock);

        deviceLicenseService ??= Mock.Of<IDeviceLicenseService>(d =>
            d.VerifyAsync(It.IsAny<CancellationToken>()) == Task.FromResult(new DeviceVerifyResponseDto { IsValid = true }));

        return new AuthService(
            context,
            config,
            new MemoryCache(new MemoryCacheOptions()),
            Mock.Of<ILogger<AuthService>>(),
            fingerprintService,
            deviceLicenseService);
    }
}
