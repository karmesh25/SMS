using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ABR.Application.DTOs.Auth;
using ABR.Application.Interfaces;
using ABR.Domain.Entities;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ABR.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly AbrDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthService> _logger;
    private readonly IDeviceFingerprintService _fingerprintService;
    private readonly IDeviceLicenseService _deviceLicenseService;

    public AuthService(
        AbrDbContext context,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<AuthService> logger,
        IDeviceFingerprintService fingerprintService,
        IDeviceLicenseService deviceLicenseService)
    {
        _context = context;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
        _fingerprintService = fingerprintService;
        _deviceLicenseService = deviceLicenseService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.SiteAccesses)
            .ThenInclude(sa => sa.Site)
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (user is null || !user.IsActive)
            return null;

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTimeOffset.UtcNow)
            throw new InvalidOperationException($"Account locked until {user.LockedUntil.Value:u}.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
                user.FailedAttempts = 0;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (_fingerprintService.IsDeviceLockEnforced())
        {
            var deviceVerify = await _deviceLicenseService.VerifyAsync(cancellationToken);
            if (!deviceVerify.IsValid)
            {
                _logger.LogWarning("Login blocked for {Username}: device not authorized.", user.Username);
                throw new InvalidOperationException("This device is not authorized to access the system.");
            }
        }

        user.FailedAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return await BuildLoginResponseAsync(user, cancellationToken);
    }

    public async Task<LoginResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.SiteAccesses)
            .ThenInclude(sa => sa.Site)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked, cancellationToken);

        if (stored is null || stored.ExpiresAt <= DateTimeOffset.UtcNow || !stored.User.IsActive)
            return null;

        stored.IsRevoked = true;
        await _context.SaveChangesAsync(cancellationToken);

        return await BuildLoginResponseAsync(stored.User, cancellationToken);
    }

    public Task LogoutAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return Task.CompletedTask;

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken))
            return Task.CompletedTask;

        var jwt = handler.ReadJwtToken(accessToken);
        var expiry = jwt.ValidTo;
        var remaining = expiry - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
            remaining = TimeSpan.FromMinutes(1);

        _cache.Set(GetBlacklistKey(accessToken), true, remaining);
        return Task.CompletedTask;
    }

    public bool IsTokenBlacklisted(string accessToken) =>
        _cache.TryGetValue(GetBlacklistKey(accessToken), out _);

    private async Task<LoginResponse> BuildLoginResponseAsync(User user, CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(_configuration.GetValue("Jwt:ExpiryHours", 8));
        var token = GenerateJwt(user, expiresAt);
        var refreshToken = GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime)
        });
        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapUser(user)
        };
    }

    private string GenerateJwt(User user, DateTimeOffset expiresAt)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("role", user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static UserDto MapUser(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        ForcePasswordChange = user.ForcePasswordChange,
        LastLoginAt = user.LastLoginAt,
        SiteAccess = user.SiteAccesses.Select(sa => new SiteAccessDto
        {
            SiteId = sa.SiteId,
            SiteName = sa.Site?.SiteName ?? string.Empty,
            CanRead = sa.CanRead,
            CanWrite = sa.CanWrite,
            CanDelete = sa.CanDelete
        }).ToList()
    };

    private static string GetBlacklistKey(string accessToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(accessToken)));
        return $"blacklist:{hash}";
    }
}
