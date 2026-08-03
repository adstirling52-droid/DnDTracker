using DnDTracker.Web.Models.NpcGenerator;

namespace DnDTracker.Web.Services.NpcGenerator;

public sealed class NpcGeneratorService(NpcGenerationDataProvider dataProvider)
{
    public (GeneratedNpc? Npc, string? Error) Generate()
    {
        if (!dataProvider.IsLoaded)
        {
            return (null, dataProvider.LoadError ?? "NPC generation data is not available.");
        }

        var data = dataProvider.Data;
        var ancestry = PickRandom(data.Ancestries);
        var ageCategoryEntry = PickMatchingEntry(
            data.AgeCategories,
            ancestry.Tags);
        var genderPresentation = PickRandom(data.GenderPresentations);
        var occupation = PickRandom(data.Occupations);

        if (!data.NamesByAncestry.TryGetValue(ancestry.Id, out var names) || names.Count == 0)
        {
            return (null, $"NPC generation data has no names for ancestry '{ancestry.Id}'.");
        }

        var appearanceTags = BuildAppearanceTags(ageCategoryEntry, occupation);
        var occupationTags = BuildOccupationTags(occupation);
        var background = PickMatchingEntry(
            data.Backgrounds,
            occupationTags
                .Concat(ancestry.Tags)
                .Concat(ageCategoryEntry.Tags)
                .Append(occupation.Id));

        var storyTags = background.Tags
            .Concat(occupationTags)
            .Concat(ancestry.Tags);

        var secret = PickMatchingEntry(data.Secrets, storyTags);
        var motivation = PickMatchingEntry(
            data.Motivations,
            background.Tags.Concat(secret.Tags));
        var problem = PickMatchingEntry(
            data.Problems,
            secret.Tags.Concat(occupationTags));
        var questHook = PickMatchingEntry(
            data.QuestHooks,
            problem.Tags.Concat(occupationTags).Concat(secret.Tags));
        var danger = PickMatchingEntry(
            data.Dangers,
            problem.Tags.Concat(secret.Tags).Concat(occupationTags));

        var npc = new GeneratedNpc
        {
            Name = PickRandom(names),
            Ancestry = ancestry.Label,
            GenderPresentation = genderPresentation.Text,
            AgeCategory = ageCategoryEntry.Text,
            Occupation = occupation.Label,
            Appearance = PickMatchingEntry(data.Appearances, appearanceTags).Text,
            DistinctiveFeature = PickMatchingEntry(
                data.DistinctiveFeatures,
                ancestry.Tags.Concat(occupationTags)).Text,
            Personality = PickMatchingEntry(data.Personalities, occupationTags).Text,
            Mannerism = PickMatchingEntry(data.Mannerisms, occupationTags).Text,
            Voice = PickMatchingEntry(
                data.Voices,
                ageCategoryEntry.Tags.Concat(occupationTags)).Text,
            Background = background.Text,
            Motivation = motivation.Text,
            Secret = secret.Text,
            CurrentProblem = problem.Text,
            QuestHook = questHook.Text,
            DangerOrComplication = danger.Text
        };

        npc.DmSummary = ComposeDmSummary(npc);
        npc.ImagePrompt = ComposeImagePrompt(
            npc,
            PickMatchingEntry(data.ImageEnvironments, occupationTags).Text);

        return (npc, null);
    }

    private static string ComposeDmSummary(GeneratedNpc npc) =>
        $"""
        {npc.Name} — {npc.Occupation} ({npc.Ancestry}, {npc.AgeCategory})
        Appearance: {npc.Appearance} Notable: {npc.DistinctiveFeature}.
        Personality: {npc.Personality} {npc.Mannerism} {npc.Voice}
        Background: {npc.Background}
        Motivation: {npc.Motivation}
        Secret: {npc.Secret}
        Current problem: {npc.CurrentProblem}
        Quest hook: {npc.QuestHook}
        Danger or complication: {npc.DangerOrComplication}
        """.Trim();

    private static string ComposeImagePrompt(GeneratedNpc npc, string environment)
    {
        var expression = npc.Personality.Split(',')[0].Trim();
        if (expression.Length > 60)
        {
            expression = expression[..60].TrimEnd();
        }

        return
            $"Fantasy character portrait of a {npc.AgeCategory} {npc.Ancestry} {npc.Occupation.ToLowerInvariant()} " +
            $"with {npc.GenderPresentation.ToLowerInvariant()}. {npc.Appearance} " +
            $"Distinctive detail: {npc.DistinctiveFeature} Expression: {expression}. " +
            $"Setting: {environment}. Grounded fantasy art style, painterly, detailed portrait lighting.";
    }

    private static IEnumerable<string> BuildOccupationTags(NpcOccupationEntry occupation) =>
        occupation.Tags
            .Append(occupation.Id)
            .Where(tag => !string.IsNullOrWhiteSpace(tag));

    private static IEnumerable<string> BuildAppearanceTags(
        NpcTaggedTextEntry ageCategoryEntry,
        NpcOccupationEntry occupation) =>
        ageCategoryEntry.Tags
            .Concat(occupation.AppearanceTags)
            .Concat(occupation.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag));

    private static NpcTaggedTextEntry PickMatchingEntry(
        IReadOnlyList<NpcTaggedTextEntry> entries,
        IEnumerable<string> preferredTags)
    {
        var tags = preferredTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tags.Count == 0)
        {
            return PickRandom(entries);
        }

        var matched = entries
            .Where(entry => entry.Tags.Any(tag => tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        return PickRandom(matched.Count > 0 ? matched : entries);
    }

    private static T PickRandom<T>(IReadOnlyList<T> items) =>
        items[Random.Shared.Next(items.Count)];
}
