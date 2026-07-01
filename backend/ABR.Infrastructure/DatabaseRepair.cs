using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ABR.Infrastructure;

public static class DatabaseRepair
{
    private const string CustomRolesMigrationId = "20260630125519_AddCustomRolesAndPermissions";
    private const string PlotWingsMigrationId = "20260701095744_AddPlotWings";

    public static async Task RepairRoleSchemaAsync(AbrDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await context.Database.CanConnectAsync(cancellationToken))
                return;

            logger.LogInformation("Ensuring role schema is complete...");

            await RoleSeeder.EnsureRolesAsync(context, logger, cancellationToken);
            await RoleSeeder.BackfillUserRoleIdsAsync(context, cancellationToken);
            await TryEnsureUserRoleForeignKeyAsync(context, cancellationToken);
            await MarkMigrationAppliedAsync(context, CustomRolesMigrationId, cancellationToken);
            await MarkMigrationAppliedAsync(context, PlotWingsMigrationId, cancellationToken);

            logger.LogInformation("Role schema is ready.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Role schema repair encountered an error.");
        }
    }

    private static async Task TryEnsureUserRoleForeignKeyAsync(AbrDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE users
                ADD CONSTRAINT "FK_users_app_roles_role_id"
                FOREIGN KEY (role_id) REFERENCES app_roles (id) ON DELETE RESTRICT;
                """,
                cancellationToken);
        }
        catch (Exception)
        {
            // Constraint already exists or role data not ready yet.
        }
    }

    private static async Task MarkMigrationAppliedAsync(
        AbrDbContext context,
        string migrationId,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT {migrationId}, {"8.0.11"}
            WHERE NOT EXISTS (
                SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = {migrationId}
            );
            """,
            cancellationToken);
    }
}
