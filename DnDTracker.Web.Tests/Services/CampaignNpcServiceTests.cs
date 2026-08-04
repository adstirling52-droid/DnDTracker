using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using DnDTracker.Web.Models.NpcGenerator;
using DnDTracker.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace DnDTracker.Web.Tests.Services;

public class CampaignNpcServiceTests
{
    [Fact]
    public async Task SaveFromGeneratedAsync_PersistsNpcFieldsAndEditedImagePrompt()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Test Campaign");
        var service = new CampaignNpcService(db, new CampaignNpcImageService(CreateEnvironment(), db));
        var generated = CreateSampleGeneratedNpc();

        var (savedNpc, error) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            generated,
            "Edited portrait prompt.");

        Assert.Null(error);
        Assert.NotNull(savedNpc);
        Assert.Equal("Edited portrait prompt.", savedNpc!.ImagePrompt);
        Assert.Equal("Helga Ironvein", savedNpc.Name);
        Assert.Equal(campaignId, savedNpc.CampaignId);

        var stored = await db.CampaignNpcs.SingleAsync();
        Assert.Equal(savedNpc.Id, stored.Id);
        Assert.Equal(generated.DmSummary, stored.DmSummary);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCampaignNpcsForOwnerOnly()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Campaign A");
        var otherCampaignId = await SeedCampaignAsync(db, "user-2", "Campaign B");
        var service = new CampaignNpcService(db, new CampaignNpcImageService(CreateEnvironment(), db));

        await service.SaveFromGeneratedAsync("user-1", campaignId, CreateSampleGeneratedNpc(), "Prompt A");
        await service.SaveFromGeneratedAsync("user-2", otherCampaignId, CreateSampleGeneratedNpc(), "Prompt B");

        var results = await service.GetAllAsync("user-1", campaignId);

        Assert.Single(results);
        Assert.Equal("Helga Ironvein", results[0].Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSavedNpc()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Test Campaign");
        var service = new CampaignNpcService(db, new CampaignNpcImageService(CreateEnvironment(), db));
        var (savedNpc, _) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            CreateSampleGeneratedNpc(),
            "Prompt");

        var deleteError = await service.DeleteAsync("user-1", campaignId, savedNpc!.Id);

        Assert.Null(deleteError);
        Assert.Empty(await db.CampaignNpcs.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEditableFieldsAndSetsUpdatedAtUtc()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Test Campaign");
        var service = CreateService(db);
        var (savedNpc, _) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            CreateSampleGeneratedNpc(),
            "Prompt");

        var beforeUpdate = DateTime.UtcNow;
        var updateError = await service.UpdateAsync(
            "user-1",
            campaignId,
            savedNpc!.Id,
            new CampaignNpcUpdateInput(
                "Helga Updated",
                "Harbour master",
                "Deepwater Ferry",
                "Updated summary.",
                IsCurrent: true));

        Assert.Null(updateError);

        var stored = await db.CampaignNpcs.SingleAsync();
        Assert.Equal("Helga Updated", stored.Name);
        Assert.Equal("Harbour master", stored.Occupation);
        Assert.Equal("Deepwater Ferry", stored.Location);
        Assert.Equal("Updated summary.", stored.DmSummary);
        Assert.True(stored.IsCurrent);
        Assert.NotNull(stored.UpdatedAtUtc);
        Assert.True(stored.UpdatedAtUtc >= beforeUpdate);
    }

    [Fact]
    public async Task UpdateAsync_PreservesNonEditableFields()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Test Campaign");
        var service = CreateService(db);
        var generated = CreateSampleGeneratedNpc();
        var (savedNpc, _) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            generated,
            "Original prompt.");

        savedNpc!.ImagePath = "user-1/test.png";
        await db.SaveChangesAsync();

        var updateError = await service.UpdateAsync(
            "user-1",
            campaignId,
            savedNpc.Id,
            new CampaignNpcUpdateInput(
                "Helga Updated",
                "Harbour master",
                "Deepwater Ferry",
                "Updated summary.",
                IsCurrent: false));

        Assert.Null(updateError);

        var stored = await db.CampaignNpcs.SingleAsync();
        Assert.Equal(generated.Ancestry, stored.Ancestry);
        Assert.Equal(generated.Appearance, stored.Appearance);
        Assert.Equal(generated.Secret, stored.Secret);
        Assert.Equal("Original prompt.", stored.ImagePrompt);
        Assert.Equal("user-1/test.png", stored.ImagePath);
        Assert.Equal(savedNpc.SavedAtUtc, stored.SavedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_RejectsEmptyName()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Test Campaign");
        var service = CreateService(db);
        var (savedNpc, _) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            CreateSampleGeneratedNpc(),
            "Prompt");

        var updateError = await service.UpdateAsync(
            "user-1",
            campaignId,
            savedNpc!.Id,
            new CampaignNpcUpdateInput("   ", "Occupation", "", "Summary", false));

        Assert.Equal("Please enter an NPC name.", updateError);
    }

    [Fact]
    public async Task UpdateAsync_RejectsLocationOverMaxLength()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Test Campaign");
        var service = CreateService(db);
        var (savedNpc, _) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            CreateSampleGeneratedNpc(),
            "Prompt");

        var updateError = await service.UpdateAsync(
            "user-1",
            campaignId,
            savedNpc!.Id,
            new CampaignNpcUpdateInput(
                "Helga",
                "Occupation",
                new string('x', CampaignNpcService.MaxLocationLength + 1),
                "Summary",
                false));

        Assert.Equal(
            $"Location must be {CampaignNpcService.MaxLocationLength} characters or fewer.",
            updateError);
    }

    [Fact]
    public async Task UpdateAsync_FailsForNonOwner()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Test Campaign");
        var service = CreateService(db);
        var (savedNpc, _) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            CreateSampleGeneratedNpc(),
            "Prompt");

        var updateError = await service.UpdateAsync(
            "user-2",
            campaignId,
            savedNpc!.Id,
            new CampaignNpcUpdateInput("Helga", "Occupation", "", "Summary", false));

        Assert.Equal("Saved NPC not found.", updateError);
    }

    [Fact]
    public async Task UpdateAsync_MarkingOneNpcCurrentDoesNotUnsetOthers()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Test Campaign");
        var service = CreateService(db);
        var (firstNpc, _) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            CreateSampleGeneratedNpc(),
            "Prompt A");
        var (secondNpc, _) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            CreateSampleGeneratedNpc(name: "Bruno Stonehand"),
            "Prompt B");

        await service.UpdateAsync(
            "user-1",
            campaignId,
            firstNpc!.Id,
            new CampaignNpcUpdateInput("Helga Ironvein", "Ferry operator", "", firstNpc.DmSummary, true));

        await service.UpdateAsync(
            "user-1",
            campaignId,
            secondNpc!.Id,
            new CampaignNpcUpdateInput("Bruno Stonehand", "Blacksmith", "", secondNpc.DmSummary, true));

        var currentNpcs = await service.GetCurrentAsync("user-1", campaignId);

        Assert.Equal(2, currentNpcs.Count);
        Assert.Contains(currentNpcs, npc => npc.Id == firstNpc.Id && npc.IsCurrent);
        Assert.Contains(currentNpcs, npc => npc.Id == secondNpc.Id && npc.IsCurrent);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsOnlyCurrentNpcsForOwner()
    {
        await using var db = CreateDbContext();
        var campaignId = await SeedCampaignAsync(db, "user-1", "Campaign A");
        var otherCampaignId = await SeedCampaignAsync(db, "user-2", "Campaign B");
        var service = CreateService(db);
        var (currentNpc, _) = await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            CreateSampleGeneratedNpc(),
            "Prompt A");
        await service.SaveFromGeneratedAsync(
            "user-1",
            campaignId,
            CreateSampleGeneratedNpc(name: "Bruno Stonehand"),
            "Prompt B");
        var (otherCampaignNpc, _) = await service.SaveFromGeneratedAsync(
            "user-2",
            otherCampaignId,
            CreateSampleGeneratedNpc(),
            "Prompt C");

        await service.UpdateAsync(
            "user-1",
            campaignId,
            currentNpc!.Id,
            new CampaignNpcUpdateInput("Helga Ironvein", "Ferry operator", "The Silver Net", currentNpc.DmSummary, true));
        await service.UpdateAsync(
            "user-2",
            otherCampaignId,
            otherCampaignNpc!.Id,
            new CampaignNpcUpdateInput("Helga Ironvein", "Ferry operator", "", otherCampaignNpc.DmSummary, true));

        var results = await service.GetCurrentAsync("user-1", campaignId);

        Assert.Single(results);
        Assert.Equal(currentNpc.Id, results[0].Id);
        Assert.Equal("The Silver Net", results[0].Location);
    }

    private static CampaignNpcService CreateService(DnDTrackerDbContext db) =>
        new(db, new CampaignNpcImageService(CreateEnvironment(), db));

    private static GeneratedNpc CreateSampleGeneratedNpc(string name = "Helga Ironvein") => new()
    {
        Name = name,
        Ancestry = "Dwarf",
        GenderPresentation = "feminine",
        AgeCategory = "young adult",
        Occupation = "Ferry operator",
        Appearance = "Lean and alert.",
        DistinctiveFeature = "A neatly notched ear.",
        Personality = "Warm with strangers but quietly watchful.",
        Mannerism = "Hums under their breath while working.",
        Voice = "Talks quickly, with a warm regional lilt.",
        Background = "Inherited a modest family trade.",
        Motivation = "Keep their home safe.",
        Secret = "Quietly passes messages for a smuggler.",
        CurrentProblem = "Owes money to someone impatient.",
        QuestHook = "Offers reliable local information.",
        DangerOrComplication = "Their creditor has ties to violent people.",
        DmSummary = "Helga is a young adult dwarven ferry operator.",
        ImagePrompt = "Fantasy character portrait."
    };

    private static async Task<Guid> SeedCampaignAsync(DnDTrackerDbContext db, string userId, string name)
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();
        return campaign.Id;
    }

    private static DnDTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DnDTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DnDTrackerDbContext(options);
    }

    private static TestWebHostEnvironment CreateEnvironment() =>
        new(Path.Combine(Path.GetTempPath(), "dndtracker-npc-image-tests", Guid.NewGuid().ToString()));

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }

        public string ApplicationName { get; set; } = "DnDTracker.Web.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = null!;

        public string ContentRootPath { get; set; }

        public string EnvironmentName { get; set; } = "Development";

        public IFileProvider WebRootFileProvider { get; set; } = null!;

        public string WebRootPath { get; set; } = "";
    }
}
