using System.Text.RegularExpressions;
using DnDTracker.Web.Models.NpcGenerator;
using DnDTracker.Web.Services.NpcGenerator;
using Xunit;

namespace DnDTracker.Web.Tests.Services;

public class NpcGeneratorSummaryGrammarTests
{
    [Theory]
    [InlineData("elderly", "an")]
    [InlineData("adult in their prime", "a")]
    [InlineData("young adult", "a")]
    [InlineData("innkeeper", "an")]
    [InlineData("hourglass figure", "an")]
    public void SelectIndefiniteArticle_UsesCorrectArticle(string phrase, string expectedArticle)
    {
        Assert.Equal(expectedArticle, NpcGeneratorService.SelectIndefiniteArticle(phrase));
    }

    [Fact]
    public void ComposeDmSummary_ElderlyNpcUsesAnArticle()
    {
        var npc = CreateNpc(
            genderPresentation: "masculine presentation",
            ageCategory: "elderly",
            appearance: "Compact and weathered, with cracked knuckles and a posture shaped by long outdoor labour.");

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains(" is an elderly ", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void ComposeDmSummary_FeminineNpcUsesConsistentHerPronouns()
    {
        var npc = CreateHelgaNpc();
        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains("She is warm", summary, StringComparison.Ordinal);
        Assert.Contains("humming under her breath", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("She wants to keep her home", summary, StringComparison.Ordinal);
        Assert.Contains("she quietly passes messages", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("she owes money", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("She may offer reliable local information", summary, StringComparison.Ordinal);
        Assert.Contains("her creditor has ties", summary, StringComparison.OrdinalIgnoreCase);
        Assert.False(Regex.IsMatch(summary, @"\bShe\b[^.]*\btheir\b", RegexOptions.IgnoreCase));
    }

    [Fact]
    public void ComposeDmSummary_MasculineNpcUsesConsistentHisPronouns()
    {
        var npc = CreateHelgaNpc();
        npc.Name = "Garret Holt";
        npc.GenderPresentation = "masculine presentation";

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains("He is warm", summary, StringComparison.Ordinal);
        Assert.Contains("humming under his breath", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("He wants to keep his home", summary, StringComparison.Ordinal);
        Assert.Contains("He may offer reliable local information", summary, StringComparison.Ordinal);
        Assert.Contains("his creditor has ties", summary, StringComparison.OrdinalIgnoreCase);
        Assert.False(Regex.IsMatch(summary, @"\bHe\b[^.]*\btheir\b", RegexOptions.IgnoreCase));
    }

    [Fact]
    public void ComposeDmSummary_SingularTheyUsesPluralVerbAgreement()
    {
        var npc = CreateHelgaNpc();
        npc.Name = "Sera Vance";
        npc.GenderPresentation = "androgynous presentation";

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains("They are warm", summary, StringComparison.Ordinal);
        Assert.Contains("humming under their breath", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("They want to keep their home", summary, StringComparison.Ordinal);
        Assert.Contains("They may offer reliable local information", summary, StringComparison.Ordinal);
        Assert.Contains("their creditor has ties", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("They wants", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("They is ", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("may offers", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("they quietly pass messages", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("they quietly passes", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComposeDmSummary_UsesMayAskConstructionForAskQuestHooks()
    {
        var npc = CreateHelgaNpc();
        npc.QuestHook = "Asks the party to watch their workplace for whoever is causing the trouble.";

        var summary = NpcGeneratorService.ComposeDmSummary(npc);

        Assert.Contains("She may ask the party to watch her workplace", summary, StringComparison.Ordinal);
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
    [InlineData("feminine presentation", "She", "her")]
    [InlineData("masculine presentation", "He", "his")]
    [InlineData("androgynous presentation", "They", "their")]
    public void GetPronounSet_ReturnsExpectedForms(string genderPresentation, string subject, string possessiveAdjective)
    {
        var pronouns = NpcGeneratorService.GetPronounSet(genderPresentation);

        Assert.Equal(subject, pronouns.Subject);
        Assert.Equal(possessiveAdjective, pronouns.PossessiveAdjective);
    }

    private static GeneratedNpc CreateHelgaNpc() => CreateNpc(
        name: "Helga Ironvein",
        genderPresentation: "feminine presentation",
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
        string genderPresentation = "feminine presentation",
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
}
