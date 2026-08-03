using DnDTracker.Web.Models.NpcGenerator;
using DnDTracker.Web.Services.NpcGenerator;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DnDTracker.Web.Tests.Services;

public class NpcGeneratorServiceTests
{
    [Fact]
    public void Generate_ReturnsCompleteNpc_WhenDataIsLoaded()
    {
        var service = CreateService();

        var (npc, error) = service.Generate();

        Assert.Null(error);
        Assert.NotNull(npc);
        Assert.False(string.IsNullOrWhiteSpace(npc.Name));
        Assert.False(string.IsNullOrWhiteSpace(npc.Ancestry));
        Assert.False(string.IsNullOrWhiteSpace(npc.DmSummary));
        Assert.False(string.IsNullOrWhiteSpace(npc.ImagePrompt));
    }

    [Fact]
    public void Generate_ReturnsError_WhenDataIsNotLoaded()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var provider = new NpcGenerationDataProvider(
            missingPath,
            NullLogger<NpcGenerationDataProvider>.Instance);
        var service = new NpcGeneratorService(provider);

        var (npc, error) = service.Generate();

        Assert.Null(npc);
        Assert.NotNull(error);
    }

    [Fact]
    public void Generate_ProducesNonEmptyFields_AcrossManyIterations()
    {
        var service = CreateService();

        for (var i = 0; i < 100; i++)
        {
            var (npc, error) = service.Generate();

            Assert.Null(error);
            Assert.NotNull(npc);
            AssertAllFieldsPopulated(npc);
        }
    }

    [Fact]
    public void Generate_NameBelongsToSelectedAncestry()
    {
        var service = CreateService();
        var provider = CreateProvider();
        var ancestryLabels = provider.Data.Ancestries.ToDictionary(
            ancestry => ancestry.Label,
            ancestry => ancestry.Id,
            StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < 50; i++)
        {
            var (npc, error) = service.Generate();

            Assert.Null(error);
            Assert.NotNull(npc);
            Assert.True(ancestryLabels.TryGetValue(npc.Ancestry, out var ancestryId));
            Assert.Contains(npc.Name, provider.Data.NamesByAncestry[ancestryId]);
        }
    }

    [Fact]
    public void Generate_ImagePromptExcludesHiddenStoryDetails()
    {
        var service = CreateService();

        for (var i = 0; i < 100; i++)
        {
            var (npc, error) = service.Generate();

            Assert.Null(error);
            Assert.NotNull(npc);
            Assert.DoesNotContain(npc.Secret, npc.ImagePrompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(npc.Motivation, npc.ImagePrompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(npc.QuestHook, npc.ImagePrompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(npc.CurrentProblem, npc.ImagePrompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(npc.DangerOrComplication, npc.ImagePrompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(npc.Background, npc.ImagePrompt, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Generate_DmSummaryIncludesStructuredFields()
    {
        var service = CreateService();

        var (npc, error) = service.Generate();

        Assert.Null(error);
        Assert.NotNull(npc);
        Assert.Contains(npc.Name, npc.DmSummary, StringComparison.Ordinal);
        Assert.Contains(npc.Secret, npc.DmSummary, StringComparison.Ordinal);
        Assert.Contains(npc.QuestHook, npc.DmSummary, StringComparison.Ordinal);
    }

    private static NpcGeneratorService CreateService() =>
        new(CreateProvider());

    private static NpcGenerationDataProvider CreateProvider()
    {
        var dataFilePath = Path.Combine(GetWebProjectRoot(), "Data", "NpcGenerator", "npc-generation-data.json");
        return new NpcGenerationDataProvider(
            dataFilePath,
            NullLogger<NpcGenerationDataProvider>.Instance);
    }

    private static string GetWebProjectRoot() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "DnDTracker.Web"));

    private static void AssertAllFieldsPopulated(GeneratedNpc npc)
    {
        Assert.False(string.IsNullOrWhiteSpace(npc.Name));
        Assert.False(string.IsNullOrWhiteSpace(npc.Ancestry));
        Assert.False(string.IsNullOrWhiteSpace(npc.GenderPresentation));
        Assert.False(string.IsNullOrWhiteSpace(npc.AgeCategory));
        Assert.False(string.IsNullOrWhiteSpace(npc.Occupation));
        Assert.False(string.IsNullOrWhiteSpace(npc.Appearance));
        Assert.False(string.IsNullOrWhiteSpace(npc.DistinctiveFeature));
        Assert.False(string.IsNullOrWhiteSpace(npc.Personality));
        Assert.False(string.IsNullOrWhiteSpace(npc.Mannerism));
        Assert.False(string.IsNullOrWhiteSpace(npc.Voice));
        Assert.False(string.IsNullOrWhiteSpace(npc.Background));
        Assert.False(string.IsNullOrWhiteSpace(npc.Motivation));
        Assert.False(string.IsNullOrWhiteSpace(npc.Secret));
        Assert.False(string.IsNullOrWhiteSpace(npc.CurrentProblem));
        Assert.False(string.IsNullOrWhiteSpace(npc.QuestHook));
        Assert.False(string.IsNullOrWhiteSpace(npc.DangerOrComplication));
        Assert.False(string.IsNullOrWhiteSpace(npc.DmSummary));
        Assert.False(string.IsNullOrWhiteSpace(npc.ImagePrompt));
    }
}
