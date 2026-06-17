using ABR.Domain.Entities;
using ABR.Domain.Enums;
using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ABR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AbrDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AbrDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AbrDbContext>>();

        await context.Database.MigrateAsync();
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
        if (await context.Users.AnyAsync())
            return;

        logger.LogInformation("Seeding initial database data...");

        var admin = new User
        {
            Username = "admin",
            Email = "admin@abr.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
            Role = UserRole.SuperAdmin.ToString(),
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
}
