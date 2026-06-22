using System.Text;
using ABR.Application;
using ABR.Application.Common;
using ABR.Application.Interfaces;
using ABR.Domain.Enums;
using ABR.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;
using QuestPDF.Infrastructure;
using Serilog;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/abr-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ABR API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (isDevelopment)
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    logger.LogWarning(context.Exception, "JWT authentication failed.");
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader)
                    || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }

                var token = authHeader["Bearer ".Length..].Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Task.CompletedTask;
                }

                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                if (authService.IsTokenBlacklisted(token))
                    context.Fail("Token has been revoked.");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole(nameof(UserRole.SuperAdmin)));
    options.AddPolicy("Admin", policy => policy.RequireRole(nameof(UserRole.SuperAdmin), nameof(UserRole.Admin)));
    options.AddPolicy("Staff", policy => policy.RequireRole(
        nameof(UserRole.SuperAdmin), nameof(UserRole.Admin), nameof(UserRole.OfficeStaff)));
    options.AddPolicy("Viewer", policy => policy.RequireRole(
        nameof(UserRole.SuperAdmin), nameof(UserRole.Admin), nameof(UserRole.OfficeStaff), nameof(UserRole.ViewOnly)));
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://localhost:5050");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

var frontendPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "frontend"));
if (Directory.Exists(frontendPath))
{
    var fileProvider = new PhysicalFileProvider(frontendPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
}

app.MapControllers();

try
{
    await app.Services.InitializeDatabaseAsync();
}
catch (Exception ex)
{
    Log.Warning(ex, "Database initialization skipped — ensure PostgreSQL is running and connection string is correct.");
}

app.Run();

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("An unexpected error occurred."));
        }
    }
}
