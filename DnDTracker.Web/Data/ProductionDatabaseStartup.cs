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
        var startupLog = StartupLogWriter.ForContentRoot(contentRootPath);

        try
        {
            startupLog.Write("Production database startup began.");

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
                    "This usually means an older build (PR #56) was deployed without the EF Designer file. " +
                    "Redeploy a build that includes migration " +
                    AddCampaignNpcsMigrationId +
                    ", even if the database is already up to date.";
                startupLog.Write("ERROR: " + message);
                logger.LogCritical(message);
                throw new InvalidOperationException(message);
            }

            RecoverOrphanedCampaignNpcsMigration(db, appliedMigrations, pendingMigrations, logger, startupLog);

            db.Database.Migrate();

            startupLog.Write("Database migrations applied successfully.");
            logger.LogInformation("Database migrations applied successfully.");

            EnsureDataFolder(contentRootPath, "item-images", startupLog, logger);
            EnsureDataFolder(contentRootPath, "npc-images", startupLog, logger);
            startupLog.Write("Data folders verified.");
        }
        catch (Exception ex)
        {
            startupLog.Write("Production database startup failed: " + ex);
            logger.LogCritical(ex, "Database migration failed during startup. See Data/startup.log for details.");
            throw;
        }
    }

    private static void EnsureDataFolder(
        string contentRootPath,
        string folderName,
        StartupLogWriter startupLog,
        ILogger logger)
    {
        var folderPath = Path.Combine(contentRootPath, "Data", folderName);
        try
        {
            Directory.CreateDirectory(folderPath);
        }
        catch (Exception ex)
        {
            var message = $"Unable to create data folder '{folderPath}'. Grant Modify permission to the IIS app pool identity.";
            startupLog.Write("ERROR: " + message + " " + ex.Message);
            logger.LogCritical(ex, message);
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
}

internal sealed class StartupLogWriter(string logPath)
{
    public static StartupLogWriter ForContentRoot(string contentRootPath) =>
        new(Path.Combine(contentRootPath, "Data", "startup.log"));

    public static StartupLogWriter ForAppDirectory() =>
        new(Path.Combine(AppContext.BaseDirectory, "Data", "startup.log"));

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
