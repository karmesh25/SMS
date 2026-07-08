using ABR.Application.Interfaces;
using ABR.Application.Common;
using ABR.Domain.Entities;
using ABR.Domain.Enums;
using ABR.Infrastructure.Persistence;
using ABR.Infrastructure.Security;
using ABR.Infrastructure.Services;
using ABR.Infrastructure.Services.Accounting;
using ABR.Infrastructure.Services.Booking;
using ABR.Infrastructure.Services.Dashboard;
using ABR.Infrastructure.Services.MasterData;
using ABR.Infrastructure.Services.Reports;
using ABR.Infrastructure.Services.Vyaj;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ABR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration configuration)
    {
        var exportSettings = new ExportSettings();
        configuration.GetSection("Export").Bind(exportSettings);
        services.AddSingleton(exportSettings);
        services.AddSingleton<IExportFileStorage, ExportFileStorage>();

        services.AddSingleton<SlowQueryInterceptor>();
        services.AddDbContext<AbrDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>()));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IDeviceLicenseService, DeviceLicenseService>();
        services.AddSingleton<IDeviceFingerprintService, DeviceFingerprintService>();

        services.AddScoped<ISiteService, SiteService>();
        services.AddScoped<IWingService, WingService>();
        services.AddScoped<IFlatService, FlatService>();
        services.AddScoped<IMainLedgerService, MainLedgerService>();
        services.AddScoped<ISubLedgerService, SubLedgerService>();
        services.AddScoped<IConditionService, ConditionService>();
        services.AddScoped<IBankAccountService, BankAccountService>();
        services.AddScoped<IBrokerService, BrokerService>();

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IInstallmentService, InstallmentService>();
        services.AddScoped<IDailyEntryService, DailyEntryService>();
        services.AddScoped<IDailyEntryExcelService, DailyEntryExcelService>();
        services.AddScoped<IJournalVoucherService, JournalVoucherService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IVyajService, VyajService>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AbrDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AbrDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database migration failed; attempting schema repair.");
            await DatabaseRepair.RepairRoleSchemaAsync(context, logger);

            try
            {
                await context.Database.MigrateAsync();
            }
            catch (Exception retryEx)
            {
                logger.LogWarning(retryEx, "Database migration retry failed; applying role repair again.");
                await DatabaseRepair.RepairRoleSchemaAsync(context, logger);
            }
        }

        await DatabaseRepair.RepairRoleSchemaAsync(context, logger);
        await DbInitializer.SeedAsync(context, logger);
    }
}

public static class DbInitializer
{
    private static readonly string[] DefaultMainLedgers =
    {
        "Member A/c", "Broker A/c", "Construction", "Contractor", "Salary",
        "Plumbing Saman", "Electric", "Office", "Marble", "Sales Exp", "Professional Fees",
        "SMC", "General Income", "Arja Marja Purchase A/c", "Purchase A/c", "Cancel Flat A/c"
    };

    private static readonly string[] ConstructionSubLedgers =
    {
        "Steel", "Reti", "Kapchi", "Cement", "Bricks", "Greet", "Carting", "Chharu",
        "Mati", "Khodan Puran", "General Construction Majuri", "Sink"
    };

    public static async Task SeedAsync(AbrDbContext context, ILogger logger)
    {
        if (!await context.Users.AnyAsync())
        {
            logger.LogInformation("Seeding initial database data...");

            var superAdminRole = await context.AppRoles.FirstAsync(r => r.Name == SystemRoleNames.SuperAdmin);

            var admin = new User
            {
                Username = "admin",
                Email = "admin@abr.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
                RoleId = superAdminRole.Id,
                Role = superAdminRole.Name,
                IsActive = true
            };

            var site = new Site
            {
                SiteName = "Tapi",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Address = "Default Site",
                IsActive = true
            };

            context.Users.Add(admin);
            context.Sites.Add(site);
            await context.SaveChangesAsync();

            context.UserSiteAccesses.Add(new UserSiteAccess
            {
                UserId = admin.Id,
                SiteId = site.Id,
                CanRead = true,
                CanWrite = true,
                CanDelete = true
            });

            foreach (var ledgerName in DefaultMainLedgers)
            {
                var mainLedger = new MainLedger
                {
                    SiteId = site.Id,
                    LedgerName = ledgerName,
                    Description = $"{ledgerName} ledger"
                };
                context.MainLedgers.Add(mainLedger);
                await context.SaveChangesAsync();

                if (ledgerName == "Construction")
                {
                    foreach (var subName in ConstructionSubLedgers)
                    {
                        context.SubLedgers.Add(new SubLedger
                        {
                            MainLedgerId = mainLedger.Id,
                            LedgerName = subName
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Database seed completed. Default admin: admin / Admin@123");
        }

        await EnsureDemoSandboxAsync(context, logger);
    }

    public static async Task EnsureDemoSandboxAsync(AbrDbContext context, ILogger logger)
    {
        var demoSite = await context.Sites.FirstOrDefaultAsync(s => s.SiteName == "Demo");
        if (demoSite is null)
        {
            demoSite = new Site
            {
                SiteName = "Demo",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Address = "Empty sandbox for demo user",
                IsActive = true,
                IsSandbox = true
            };
            context.Sites.Add(demoSite);
            await context.SaveChangesAsync();
            logger.LogInformation("Created empty Demo sandbox site.");
        }
        else if (!demoSite.IsSandbox)
        {
            demoSite.IsSandbox = true;
            await context.SaveChangesAsync();
        }

        var officeRole = await context.AppRoles.FirstOrDefaultAsync(r => r.Name == SystemRoleNames.OfficeStaff)
            ?? await context.AppRoles.FirstAsync(r => r.Name == SystemRoleNames.ViewOnly);

        var demoUser = await context.Users
            .Include(u => u.SiteAccesses)
            .FirstOrDefaultAsync(u => u.Username == "demo");

        if (demoUser is null)
        {
            demoUser = new User
            {
                Username = "demo",
                Email = "demo@abr.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@123", workFactor: 12),
                RoleId = officeRole.Id,
                Role = officeRole.Name,
                IsActive = true
            };
            context.Users.Add(demoUser);
            await context.SaveChangesAsync();

            context.UserSiteAccesses.Add(new UserSiteAccess
            {
                UserId = demoUser.Id,
                SiteId = demoSite.Id,
                CanRead = true,
                CanWrite = true,
                CanDelete = false
            });
            await context.SaveChangesAsync();
            logger.LogInformation("Created demo user: demo / Demo@123 (Demo site only).");
            return;
        }

        var hasDemoAccess = demoUser.SiteAccesses.Any(a => a.SiteId == demoSite.Id);
        if (!hasDemoAccess)
        {
            context.UserSiteAccesses.Add(new UserSiteAccess
            {
                UserId = demoUser.Id,
                SiteId = demoSite.Id,
                CanRead = true,
                CanWrite = true,
                CanDelete = false
            });
            await context.SaveChangesAsync();
        }
    }
}
