using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Data;

internal static class ProductionDatabaseStartup
{
    internal const string AddCampaignNpcsMigrationId = "20260803143258_AddCampaignNpcs";

    private const string EfProductVersion = "10.0.10";

    public static void ApplyMigrationsAndPrepareDataFolders(
        DnDTrackerDbContext db,
        string contentRootPath,
        ILogger logger)
    {
        var startupLogPath = Path.Combine(contentRootPath, "Data", "startup.log");
        var startupLog = new StartupLogWriter(startupLogPath);

        try
        {
            startupLog.Write("Production startup began.");

            var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
            var pendingMigrations = db.Database.GetPendingMigrations().ToList();
            var allMigrations = db.Database.GetMigrations().ToList();
            var hasPendingModelChanges = db.Database.HasPendingModelChanges();

            startupLog.Write($"Registered migrations: {FormatList(allMigrations)}");
            startupLog.Write($"Applied migrations: {FormatList(appliedMigrations)}");
            startupLog.Write($"Pending migrations: {FormatList(pendingMigrations)}");
            startupLog.Write($"HasPendingModelChanges: {hasPendingModelChanges}");

            logger.LogInformation(
                "Migration state: applied=[{Applied}] pending=[{Pending}] hasPendingModelChanges={HasPendingModelChanges}",
                string.Join(", ", appliedMigrations),
                string.Join(", ", pendingMigrations),
                hasPendingModelChanges);

            if (hasPendingModelChanges)
            {
                var message =
                    "The compiled EF model does not match the latest migration snapshot. " +
                    "Redeploy a build that includes the AddCampaignNpcs migration Designer file " +
                    $"(expected migration id: {AddCampaignNpcsMigrationId}).";
                startupLog.Write("ERROR: " + message);
                logger.LogCritical(message);
                throw new InvalidOperationException(message);
            }

            RecoverOrphanedCampaignNpcsMigration(db, appliedMigrations, pendingMigrations, logger, startupLog);

            db.Database.Migrate();

            startupLog.Write("Database migrations applied successfully.");
            logger.LogInformation("Database migrations applied successfully.");

            Directory.CreateDirectory(Path.Combine(contentRootPath, "Data", "item-images"));
            Directory.CreateDirectory(Path.Combine(contentRootPath, "Data", "npc-images"));
            startupLog.Write("Data folders verified.");
        }
        catch (Exception ex)
        {
            startupLog.Write("Startup failed: " + ex);
            logger.LogCritical(ex, "Database migration failed during startup. See Data/startup.log for details.");
            throw;
        }
    }

    private static void RecoverOrphanedCampaignNpcsMigration(
        DnDTrackerDbContext db,
        IReadOnlyCollection<string> appliedMigrations,
        IReadOnlyCollection<string> pendingMigrations,
        ILogger logger,
        StartupLogWriter startupLog)
    {
        if (!pendingMigrations.Contains(AddCampaignNpcsMigrationId))
        {
            return;
        }

        if (appliedMigrations.Contains(AddCampaignNpcsMigrationId))
        {
            return;
        }

        if (!TableExists(db, "CampaignNpcs"))
        {
            return;
        }

        var message =
            "CampaignNpcs table exists but migration history is missing. " +
            "Recording migration as applied so startup can continue.";
        startupLog.Write("WARNING: " + message);
        logger.LogWarning(message);

        db.Database.ExecuteSqlRaw(
            """
            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = {0})
            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
            VALUES ({0}, {1})
            """,
            AddCampaignNpcsMigrationId,
            EfProductVersion);
    }

    private static bool TableExists(DnDTrackerDbContext db, string tableName)
    {
        var count = db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}",
                tableName)
            .AsEnumerable()
            .FirstOrDefault();

        return count > 0;
    }

    private static string FormatList(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? "(none)" : string.Join(", ", values);

    private sealed class StartupLogWriter(string logPath)
    {
        public void Write(string message)
        {
            try
            {
                var directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {message}{Environment.NewLine}";
                File.AppendAllText(logPath, line);
            }
            catch
            {
                // Best-effort logging only; IIS stdout/Event Viewer remain the fallback.
            }
        }
    }
}
