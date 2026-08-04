using DnDTracker.Web.Models.NpcGenerator;
using DnDTracker.Web.Services.NpcGenerator;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DnDTracker.Web.Tests.Services;

public class NpcGeneratorServiceTests
{
    private static readonly string[] DmSummaryFieldLabels =
    [
        "Appearance:",
        "Notable:",
        "Personality:",
        "Background:",
        "Motivation:",
        "Secret:",
        "Current problem:",
        "Quest hook:",
        "Danger or complication:"
    ];

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
    public void Generate_DmSummaryReadsAsProseWithoutFieldLabels()
    {
        var service = CreateService();

        for (var i = 0; i < 50; i++)
        {
            var (npc, error) = service.Generate();

            Assert.Null(error);
            Assert.NotNull(npc);
            AssertDmSummaryIsProse(npc);
        }
    }

    [Fact]
    public void Generate_DmSummaryIncludesImportantStructuredFields()
    {
        var service = CreateService();

        var (npc, error) = service.Generate();

        Assert.Null(error);
        Assert.NotNull(npc);
        Assert.Contains(npc.Name, npc.DmSummary, StringComparison.Ordinal);
        Assert.Contains(KeyPhrase(npc.Secret), npc.DmSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(KeyPhrase(npc.Background), npc.DmSummary, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            npc.DmSummary.Contains(KeyPhrase(npc.QuestHook), StringComparison.OrdinalIgnoreCase) ||
            npc.DmSummary.Contains("may ask", StringComparison.OrdinalIgnoreCase) ||
            npc.DmSummary.Contains("may offer", StringComparison.OrdinalIgnoreCase) ||
            npc.DmSummary.Contains("may claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(KeyPhrase(npc.Appearance), npc.DmSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_DmSummaryAndImagePromptAvoidDoubledPunctuation()
    {
        var npc = CreateExampleNpc();

        var summary = NpcGeneratorService.ComposeDmSummary(npc);
        var prompt = NpcGeneratorService.ComposeImagePrompt(npc, "a warm tavern interior with muted lantern light");

        Assert.DoesNotContain("..", summary);
        Assert.DoesNotContain("..", prompt);
    }

    [Fact]
    public void ComposeImagePrompt_IncludesVisualElementsAndArtDirection()
    {
        var npc = CreateExampleNpc();
        var prompt = NpcGeneratorService.ComposeImagePrompt(npc, "a misty river crossing at early morning");

        Assert.StartsWith("Fantasy character portrait of", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dwarf", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ferry operator", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("river crossing", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Grounded fantasy", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain(npc.Secret, prompt, StringComparison.OrdinalIgnoreCase);
        Assert.InRange(CountWords(prompt), 60, 120);
    }

    [Fact]
    public void ComposeDmSummary_ExampleNpcMatchesExpectedFlow()
    {
        var npc = CreateExampleNpc();
        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains("Helga Ironvein is a young adult dwarven ferry operator", summary, StringComparison.Ordinal);
        Assert.Contains("quietly passes messages", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reliable local information", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Appearance:", summary, StringComparison.Ordinal);
        Assert.Equal(2, summary.Split(Environment.NewLine + Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.InRange(CountWords(summary), 125, 220);
    }

    [Fact]
    public void ComposeImagePrompt_ExampleNpcProducesPolishedPrompt()
    {
        var npc = CreateExampleNpc();
        var prompt = NpcGeneratorService.ComposeImagePrompt(npc, "a cosy riverside tavern with muted amber lantern light");

        Assert.Contains("Fantasy character portrait of a feminine young adult dwarf ferry operator", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("watchful", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("warm but quietly watchful", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cosy riverside tavern", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.InRange(CountWords(prompt), 60, 120);
    }

    private static GeneratedNpc CreateExampleNpc() => new()
    {
        Name = "Helga Ironvein",
        Ancestry = "Dwarf",
        GenderPresentation = "feminine",
        AgeCategory = "young adult",
        Occupation = "Ferry operator",
        Appearance = "Lean and alert, with quick eyes and hands that look accustomed to fine work.",
        DistinctiveFeature = "A neatly notched ear, old enough to pass for a forgotten shaving mishap.",
        Personality = "Warm with strangers but quietly watchful.",
        Mannerism = "Hums under their breath while working.",
        Voice = "Talks quickly, with a warm regional lilt.",
        Background = "Inherited a modest family trade and has kept it alive through stubborn competence rather than ambition.",
        Motivation = "Keep their home and neighbours safe from trouble they understand but cannot ignore.",
        Secret = "Quietly passes messages for a smuggler in exchange for protection.",
        CurrentProblem = "Owes money to someone impatient and well connected.",
        QuestHook = "Offers reliable local information if the party helps with a personal errand first.",
        DangerOrComplication = "Their creditor has ties to violent people."
    };

    private static void AssertDmSummaryIsProse(GeneratedNpc npc)
    {
        foreach (var label in DmSummaryFieldLabels)
        {
            Assert.DoesNotContain(label, npc.DmSummary, StringComparison.Ordinal);
        }

        Assert.Contains(Environment.NewLine + Environment.NewLine, npc.DmSummary);
        Assert.DoesNotContain("..", npc.DmSummary);
        Assert.InRange(CountWords(npc.DmSummary), 110, 230);
        Assert.StartsWith(npc.Name, npc.DmSummary, StringComparison.Ordinal);
    }

    private static int CountWords(string text) =>
        text.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;

    private static string KeyPhrase(string text)
    {
        var cleaned = text.Trim().TrimEnd('.', '!', '?');
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var phraseLength = Math.Min(4, words.Length);
        return string.Join(' ', words[..phraseLength]);
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
