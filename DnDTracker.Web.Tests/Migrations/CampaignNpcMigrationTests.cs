using DnDTracker.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace DnDTracker.Web.Tests.Migrations;

public class CampaignNpcMigrationTests
{
    [Fact]
    public void AddCampaignNpcs_migration_is_registered_with_ef()
    {
        using var context = CreateContext();

        var migrations = context.Database.GetMigrations().ToList();

        Assert.Contains("20260803143258_AddCampaignNpcs", migrations);
    }

    [Fact]
    public void Model_has_no_pending_changes_after_AddCampaignNpcs_migration()
    {
        using var context = CreateContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static DnDTrackerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DnDTrackerDbContext>()
            .UseSqlServer("Server=localhost;Database=MigrationTest;TrustServerCertificate=True")
            .Options;

        return new DnDTrackerDbContext(options);
    }
}
