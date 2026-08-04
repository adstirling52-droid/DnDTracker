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

    private static GeneratedNpc CreateSampleGeneratedNpc() => new()
    {
        Name = "Helga Ironvein",
        Ancestry = "Dwarf",
        GenderPresentation = "feminine presentation",
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
