using System.Text.RegularExpressions;
using DnDTracker.Web.Models.NpcGenerator;
using DnDTracker.Web.Services.NpcGenerator;
using Xunit;

namespace DnDTracker.Web.Tests.Services;

public class NpcGeneratorSummaryGrammarTests
{
    [Theory]
    [InlineData("elderly", "an")]
    [InlineData("adult in their prime", "an")]
    [InlineData("young adult", "a")]
    [InlineData("expected payment", "an")]
    [InlineData("innkeeper", "an")]
    [InlineData("hourglass figure", "an")]
    public void SelectIndefiniteArticle_UsesCorrectArticle(string phrase, string expectedArticle)
    {
        Assert.Equal(expectedArticle, NpcGeneratorService.SelectIndefiniteArticle(phrase));
    }

    [Fact]
    public void ComposeDmSummary_ThorinRegressionCaseProducesNaturalProse()
    {
        var npc = CreateThorinNpc();
        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains("an adult dwarven itinerant scribe in the prime of life", summary, StringComparison.Ordinal);
        Assert.Contains("permanently stained with ink", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dry, precise manner", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("An expected shipment or payment has failed to arrive", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("a adult", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("a expected", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("a elderly", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("an young", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("At the same time", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("However,", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("Now ", summary, StringComparison.Ordinal);
        Assert.False(Regex.IsMatch(summary, @"\bshe\b[^.]{0,120}\btheir\b", RegexOptions.IgnoreCase));
        Assert.Equal(2, summary.Split(Environment.NewLine + Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.InRange(CountWords(summary), 120, 200);
    }

    [Fact]
    public void ComposeDmSummary_ElderlyNpcUsesNaturalAgePhrase()
    {
        var npc = CreateNpc(
            genderPresentation: "masculine",
            ageCategory: "elderly",
            appearance: "Compact and weathered, with cracked knuckles and a posture shaped by long outdoor labour.");

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains(" is an elderly ", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("a elderly", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComposeDmSummary_FeminineNpcUsesConsistentHerPronouns()
    {
        var summary = NpcGeneratorService.ComposeDmSummary(CreateHelgaNpc());

        Assert.Contains("She is warm", summary, StringComparison.Ordinal);
        Assert.Contains("hums under her breath", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("She wants to keep her home", summary, StringComparison.Ordinal);
        Assert.Contains("she quietly passes messages", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("she owes money", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("she may offer reliable local information", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("her creditor has ties", summary, StringComparison.OrdinalIgnoreCase);
        Assert.False(Regex.IsMatch(summary, @"\bshe\b[^.]{0,120}\btheir\b", RegexOptions.IgnoreCase));
    }

    [Fact]
    public void ComposeDmSummary_MasculineNpcUsesConsistentHisPronouns()
    {
        var npc = CreateHelgaNpc();
        npc.Name = "Garret Holt";
        npc.GenderPresentation = "masculine";

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains("He is warm", summary, StringComparison.Ordinal);
        Assert.Contains("hums under his breath", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("He wants to keep his home", summary, StringComparison.Ordinal);
        Assert.Contains("he may offer reliable local information", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("his creditor has ties", summary, StringComparison.OrdinalIgnoreCase);
        Assert.False(Regex.IsMatch(summary, @"\bhe\b[^.]{0,120}\btheir\b", RegexOptions.IgnoreCase));
    }

    [Fact]
    public void ComposeDmSummary_SingularTheyUsesPluralVerbAgreement()
    {
        var npc = CreateHelgaNpc();
        npc.Name = "Sera Vance";
        npc.GenderPresentation = "Unknown";

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains("They are warm", summary, StringComparison.Ordinal);
        Assert.Contains("hums under their breath", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("They want to keep their home", summary, StringComparison.Ordinal);
        Assert.Contains("they may offer reliable local information", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("their creditor has ties", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("They wants", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("They is ", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("may offers", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("they quietly pass messages", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("they quietly passes", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("trouble they understand", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("they understands", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComposeDmSummary_UsesMayAskConstructionForAskQuestHooks()
    {
        var npc = CreateHelgaNpc();
        npc.QuestHook = "Asks the party to watch their workplace for whoever is causing the trouble.";

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains("she may ask the party to watch her workplace", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("may asks", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComposeDmSummary_JoinsNounAppearanceFragmentIntoCompleteSentence()
    {
        var npc = CreateHelgaNpc();
        npc.Appearance = "A weathered face lined by years of outdoor work.";

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains(
            "Helga Ironvein is a young adult dwarven ferry operator with a weathered face lined by years of outdoor work.",
            summary,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ComposeDmSummary_HandlesFieldsThatAlreadyEndWithPunctuation()
    {
        var npc = CreateHelgaNpc();
        npc.Personality = "Warm with strangers but quietly watchful!";
        npc.Mannerism = "Hums under their breath while working?";
        npc.Voice = "Talks quickly, with a warm regional lilt.";

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.DoesNotContain("!!", summary);
        Assert.DoesNotContain("?.", summary);
        Assert.DoesNotContain("..", summary);
        foreach (var paragraph in summary.Split(Environment.NewLine + Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.Matches(@".+\.$", paragraph.Trim());
        }
    }

    [Fact]
    public void ComposeDmSummary_DoesNotProduceLabelledFieldDumpOrFragments()
    {
        var summary = NpcGeneratorService.ComposeDmSummary(CreateHelgaNpc());

        Assert.DoesNotContain("Appearance:", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("Secret:", summary, StringComparison.Ordinal);
        Assert.All(GetSentences(summary), sentence =>
        {
            var trimmed = sentence.Trim();
            Assert.True(trimmed.Length > 10, $"Sentence too short to be complete: '{trimmed}'");
            Assert.Contains(' ', trimmed);
        });
    }

    [Theory]
    [InlineData("feminine", "She", "her")]
    [InlineData("masculine", "He", "his")]
    [InlineData("Unknown", "They", "their")]
    public void GetPronounSet_ReturnsExpectedForms(string genderPresentation, string subject, string possessiveAdjective)
    {
        var pronouns = NpcGeneratorService.GetPronounSet(genderPresentation);

        Assert.Equal(subject, pronouns.Subject);
        Assert.Equal(possessiveAdjective, pronouns.PossessiveAdjective);
    }

    private static GeneratedNpc CreateThorinNpc() => new()
    {
        Name = "Thorin Brickforge",
        Ancestry = "Dwarf",
        AgeCategory = "adult in their prime",
        GenderPresentation = "feminine",
        Occupation = "Itinerant scribe",
        Appearance = "Otherwise unremarkable at a glance, except for unusually clear grey eyes that seem to notice everything.",
        DistinctiveFeature = "Ink-stained fingers that never quite wash clean.",
        Personality = "Dry, precise, and difficult to fluster.",
        Mannerism = "Squints slightly when trying to recall a detail.",
        Voice = "Uses soft, carefully chosen words, as if afraid of being overheard.",
        Background = "Completed most of a respectable apprenticeship, then fled after discovering something unsettling in their master's records.",
        Motivation = "Settle an old debt before it ruins someone they care about.",
        Secret = "Uses a false name because their real one would attract the wrong attention.",
        CurrentProblem = "A expected shipment or payment has failed to arrive.",
        QuestHook = "Offers reliable local information if the party helps with a personal errand first.",
        DangerOrComplication = "Their creditor has ties to violent people."
    };

    private static GeneratedNpc CreateHelgaNpc() => CreateNpc(
        name: "Helga Ironvein",
        genderPresentation: "feminine",
        ageCategory: "young adult",
        ancestry: "Dwarf",
        occupation: "Ferry operator",
        appearance: "Lean and alert, with quick eyes and hands that look accustomed to fine work.",
        distinctiveFeature: "A neatly notched ear, old enough to pass for a forgotten shaving mishap.",
        personality: "Warm with strangers but quietly watchful.",
        mannerism: "Hums under their breath while working.",
        voice: "Talks quickly, with a warm regional lilt.",
        background: "Inherited a modest family trade and has kept it alive through stubborn competence rather than ambition.",
        motivation: "Keep their home and neighbours safe from trouble they understand but cannot ignore.",
        secret: "Quietly passes messages for a smuggler in exchange for protection.",
        currentProblem: "Owes money to someone impatient and well connected.",
        questHook: "Offers reliable local information if the party helps with a personal errand first.",
        danger: "Their creditor has ties to violent people.");

    private static GeneratedNpc CreateNpc(
        string name = "Test NPC",
        string genderPresentation = "feminine",
        string ageCategory = "adult in their prime",
        string ancestry = "Human",
        string occupation = "Innkeeper",
        string appearance = "Broad-shouldered and steady-looking, with sun-browned skin and short, practical hair.",
        string distinctiveFeature = "A neatly notched ear, old enough to pass for a forgotten shaving mishap.",
        string personality = "Warm with strangers but quietly watchful.",
        string mannerism = "Hums under their breath while working.",
        string voice = "Talks quickly, with a warm regional lilt.",
        string background = "Inherited a modest family trade and has kept it alive through stubborn competence rather than ambition.",
        string motivation = "Keep their home and neighbours safe from trouble they understand but cannot ignore.",
        string secret = "Quietly passes messages for a smuggler in exchange for protection.",
        string currentProblem = "Owes money to someone impatient and well connected.",
        string questHook = "Offers reliable local information if the party helps with a personal errand first.",
        string danger = "Their creditor has ties to violent people.")
    {
        return new GeneratedNpc
        {
            Name = name,
            Ancestry = ancestry,
            GenderPresentation = genderPresentation,
            AgeCategory = ageCategory,
            Occupation = occupation,
            Appearance = appearance,
            DistinctiveFeature = distinctiveFeature,
            Personality = personality,
            Mannerism = mannerism,
            Voice = voice,
            Background = background,
            Motivation = motivation,
            Secret = secret,
            CurrentProblem = currentProblem,
            QuestHook = questHook,
            DangerOrComplication = danger
        };
    }

    private static IEnumerable<string> GetSentences(string summary) =>
        summary.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static int CountWords(string text) =>
        text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
}
