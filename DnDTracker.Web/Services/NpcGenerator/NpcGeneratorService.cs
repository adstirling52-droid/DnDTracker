using System.Text.RegularExpressions;
using DnDTracker.Web.Models.NpcGenerator;

namespace DnDTracker.Web.Services.NpcGenerator;

public sealed class NpcGeneratorService(NpcGenerationDataProvider dataProvider)
{
    private static readonly (string Prefix, string Gerund)[] VerbGerundReplacements =
    [
        ("Speaks", "speaking"),
        ("Talks", "talking"),
        ("Uses", "using"),
        ("Hums", "humming"),
        ("Taps", "tapping"),
        ("Squints", "squinting"),
        ("Glances", "glancing"),
        ("Asks", "asking"),
        ("Offers", "offering"),
        ("Claims", "claiming")
    ];

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

        var environment = PickMatchingEntry(data.ImageEnvironments, occupationTags).Text;
        npc.DmSummary = ComposeDmSummary(npc);
        npc.ImagePrompt = ComposeImagePrompt(npc, environment);

        return (npc, null);
    }

    internal static string ComposeDmSummary(GeneratedNpc npc)
    {
        var pronoun = GetSubjectPronoun(npc.GenderPresentation);
        var ancestryAdjective = ToAncestryAdjective(npc.Ancestry);
        var appearancePhrase = LowercaseFirst(TrimTerminalPunctuation(npc.Appearance));

        var introduction =
            $"{npc.Name} is a {npc.AgeCategory} {ancestryAdjective} {npc.Occupation.ToLowerInvariant()}, {appearancePhrase}.";

        var distinctive = EnsureSentence(npc.DistinctiveFeature);
        var behaviour = BuildBehaviourSentence(pronoun, npc.Personality, npc.Mannerism, npc.Voice);

        var paragraphOne = JoinSentences(introduction, distinctive, behaviour);

        var paragraphTwo = JoinSentences(
            AdaptBackground(npc.Name, npc.Background),
            AdaptMotivation(pronoun, npc.Motivation),
            AdaptSecret(pronoun, npc.Secret),
            AdaptProblem(pronoun, npc.CurrentProblem),
            AdaptQuestHook(pronoun, npc.QuestHook),
            AdaptDanger(npc.DangerOrComplication));

        return FinalizeProse(paragraphOne, paragraphTwo);
    }

    internal static string ComposeImagePrompt(GeneratedNpc npc, string environment)
    {
        var presentation = TrimTerminalPunctuation(npc.GenderPresentation).ToLowerInvariant();
        var appearance = EnsureSentence(TrimTerminalPunctuation(npc.Appearance));
        var distinctive = EnsureDistinctiveFeaturePhrase(npc.DistinctiveFeature);
        var expression = BuildVisibleExpression(npc.Personality);
        var attire = InferVisibleAttire(npc.Occupation);
        var setting = FormatEnvironmentPhrase(environment);

        var prompt = JoinSentences(
            $"Fantasy character portrait of a {npc.AgeCategory} {npc.Ancestry.ToLowerInvariant()} {npc.Occupation.ToLowerInvariant()} with {presentation}.",
            appearance,
            distinctive,
            attire,
            expression,
            setting,
            "Grounded fantasy, painterly realism, detailed facial features, natural textures and cinematic portrait lighting.");

        return CollapseRepeatedPunctuation(prompt);
    }

    private static string BuildBehaviourSentence(
        string pronoun,
        string personality,
        string mannerism,
        string voice)
    {
        var personalityPhrase = AdaptPersonalityPhrase(pronoun, personality);
        var mannerismPhrase = ToConjunctivePhrase(mannerism);
        var voicePhrase = ToConjunctivePhrase(voice);

        return EnsureSentence($"{personalityPhrase}, {mannerismPhrase}, and {voicePhrase}");
    }

    private static string AdaptPersonalityPhrase(string pronoun, string personality)
    {
        var cleaned = TrimTerminalPunctuation(personality);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return $"{pronoun} has a guarded manner";
        }

        if (StartsWithPronoun(cleaned))
        {
            return cleaned;
        }

        if (char.IsUpper(cleaned[0]) && !cleaned.Contains(' '))
        {
            return $"{pronoun} is {cleaned.ToLowerInvariant()}";
        }

        var firstWord = cleaned.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
        if (IsLikelyAdjective(firstWord))
        {
            return $"{pronoun} is {LowercaseFirst(cleaned)}";
        }

        return $"{pronoun} {LowercaseFirst(cleaned)}";
    }

    private static string AdaptBackground(string name, string background)
    {
        var cleaned = TrimTerminalPunctuation(background);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithPronoun(cleaned) || cleaned.StartsWith(name, StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence(cleaned);
        }

        return EnsureSentence($"{name} {LowercaseFirst(cleaned)}");
    }

    private static string AdaptMotivation(string pronoun, string motivation)
    {
        var cleaned = TrimTerminalPunctuation(motivation);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithPronoun(cleaned))
        {
            return EnsureSentence(cleaned);
        }

        if (cleaned.StartsWith("Keep ", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("Settle ", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("Find ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"{pronoun} wants to {LowercaseFirst(cleaned)}");
        }

        return EnsureSentence($"{pronoun} is driven to {LowercaseFirst(cleaned)}");
    }

    private static string AdaptSecret(string pronoun, string secret)
    {
        var cleaned = TrimTerminalPunctuation(secret);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithPronoun(cleaned))
        {
            return EnsureSentence($"At the same time, {LowercaseFirst(cleaned)}");
        }

        return EnsureSentence($"At the same time, {LowercaseFirst(pronoun)} {LowercaseFirst(cleaned)}");
    }

    private static string AdaptProblem(string pronoun, string problem)
    {
        var cleaned = TrimTerminalPunctuation(problem);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithPronoun(cleaned))
        {
            return EnsureSentence($"Now {LowercaseFirst(cleaned)}");
        }

        if (cleaned.StartsWith("Owes ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"Now {pronoun} {LowercaseFirst(cleaned)}");
        }

        return EnsureSentence($"Now, {LowercaseFirst(cleaned)}");
    }

    private static string AdaptQuestHook(string pronoun, string questHook)
    {
        var cleaned = TrimTerminalPunctuation(questHook);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithPronoun(cleaned))
        {
            return EnsureSentence(cleaned);
        }

        if (cleaned.StartsWith("Asks ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"{pronoun} may {LowercaseFirst(cleaned)}");
        }

        if (cleaned.StartsWith("Offers ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"{pronoun} {LowercaseFirst(cleaned)}");
        }

        if (cleaned.StartsWith("Claims ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"{pronoun} {LowercaseFirst(cleaned)}");
        }

        return EnsureSentence($"{pronoun} may {LowercaseFirst(cleaned)}");
    }

    private static string AdaptDanger(string danger)
    {
        var cleaned = TrimTerminalPunctuation(danger);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("Helping ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"However, {LowercaseFirst(cleaned)}");
        }

        if (cleaned.StartsWith("Their ", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("An ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"However, {LowercaseFirst(cleaned)}");
        }

        return EnsureSentence($"However, {LowercaseFirst(cleaned)}");
    }

    private static string BuildVisibleExpression(string personality)
    {
        var cleaned = TrimTerminalPunctuation(personality);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "Their bearing is calm and attentive.";
        }

        var expressionSource = cleaned.Split(',')[0].Trim();
        expressionSource = TrimTerminalPunctuation(expressionSource);

        if (expressionSource.StartsWith("Warm ", StringComparison.OrdinalIgnoreCase) &&
            expressionSource.Contains(" but ", StringComparison.OrdinalIgnoreCase))
        {
            var butIndex = expressionSource.IndexOf(" but ", StringComparison.OrdinalIgnoreCase);
            var trailingClause = expressionSource[(butIndex + 5)..].Trim();
            return EnsureSentence($"They regard the viewer with a warm but {trailingClause.ToLowerInvariant()} expression");
        }

        if (expressionSource.StartsWith("Guarded ", StringComparison.OrdinalIgnoreCase) &&
            expressionSource.Contains(" but ", StringComparison.OrdinalIgnoreCase))
        {
            var butIndex = expressionSource.IndexOf(" but ", StringComparison.OrdinalIgnoreCase);
            var trailingClause = expressionSource[(butIndex + 5)..].Trim();
            return EnsureSentence($"They regard the viewer with a guarded but {trailingClause.ToLowerInvariant()} expression");
        }

        if (IsLikelyAdjective(expressionSource.Split(' ')[0]))
        {
            return EnsureSentence($"They regard the viewer with a {expressionSource.ToLowerInvariant()} expression");
        }

        return EnsureSentence($"Their expression suggests someone {LowercaseFirst(expressionSource)}");
    }

    private static string EnsureDistinctiveFeaturePhrase(string distinctiveFeature)
    {
        var cleaned = TrimTerminalPunctuation(distinctiveFeature);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("A ", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("An ", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("One ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence(cleaned);
        }

        return EnsureSentence($"Notable detail: {LowercaseFirst(cleaned)}");
    }

    private static string InferVisibleAttire(string occupation)
    {
        var normalized = occupation.ToLowerInvariant();

        if (normalized.Contains("guard", StringComparison.Ordinal))
        {
            return "They wear practical uniformed attire suited to patrol work.";
        }

        if (normalized.Contains("innkeeper", StringComparison.Ordinal))
        {
            return "They wear practical work clothes suited to tavern service.";
        }

        if (normalized.Contains("ferry", StringComparison.Ordinal))
        {
            return "They wear practical, weathered clothing suited to working around boats and river crossings.";
        }

        if (normalized.Contains("herbalist", StringComparison.Ordinal))
        {
            return "They wear practical clothing suited to working with herbs and simple tools.";
        }

        if (normalized.Contains("scribe", StringComparison.Ordinal))
        {
            return "They wear travel-worn but neat clothing suited to life on the road.";
        }

        return "They wear practical clothing suited to their work.";
    }

    private static string FormatEnvironmentPhrase(string environment)
    {
        var cleaned = TrimTerminalPunctuation(environment).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "Set in a quiet fantasy interior with soft, natural light.";
        }

        if (cleaned.StartsWith("a ", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("an ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"Set in {cleaned}");
        }

        return EnsureSentence($"Set in {cleaned}");
    }

    private static string ToConjunctivePhrase(string text)
    {
        var cleaned = TrimTerminalPunctuation(text.Trim());
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "moving with quiet purpose";
        }

        foreach (var (prefix, gerund) in VerbGerundReplacements)
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return gerund + cleaned[prefix.Length..].ToLowerInvariant();
            }
        }

        return LowercaseFirst(cleaned);
    }

    private static string GetSubjectPronoun(string genderPresentation)
    {
        var normalized = genderPresentation.ToLowerInvariant();
        if (normalized.Contains("feminine", StringComparison.Ordinal))
        {
            return "She";
        }

        if (normalized.Contains("masculine", StringComparison.Ordinal))
        {
            return "He";
        }

        return "They";
    }

    private static string ToAncestryAdjective(string ancestry) =>
        ancestry.Trim().ToLowerInvariant() switch
        {
            "dwarf" => "dwarven",
            "elf" => "elven",
            "human" => "human",
            _ => ancestry.ToLowerInvariant()
        };

    private static bool StartsWithPronoun(string text)
    {
        var normalized = text.TrimStart().ToLowerInvariant();
        return normalized.StartsWith("he ", StringComparison.Ordinal) ||
               normalized.StartsWith("she ", StringComparison.Ordinal) ||
               normalized.StartsWith("they ", StringComparison.Ordinal);
    }

    private static bool IsLikelyAdjective(string word)
    {
        var normalized = word.Trim().ToLowerInvariant();
        return normalized is "warm" or "dry" or "restless" or "guarded" or "lean" or "quiet" or "gravelly" or "soft";
    }

    private static string JoinSentences(params string?[] sentences)
    {
        var parts = sentences
            .Where(sentence => !string.IsNullOrWhiteSpace(sentence))
            .Select(sentence => CollapseRepeatedPunctuation(sentence!.Trim()))
            .ToList();

        return string.Join(" ", parts);
    }

    private static string FinalizeProse(params string?[] paragraphs)
    {
        var parts = paragraphs
            .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
            .Select(paragraph => CollapseRepeatedPunctuation(paragraph!.Trim()))
            .ToList();

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    private static string EnsureSentence(string text)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = TrimTerminalPunctuation(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = char.ToUpperInvariant(text[0]) + text[1..];
        return CollapseRepeatedPunctuation($"{text}.");
    }

    private static string TrimTerminalPunctuation(string text)
    {
        text = text.Trim();
        while (text.Length > 0 && text[^1] is '.' or '!' or '?')
        {
            text = text[..^1].TrimEnd();
        }

        return text.Trim();
    }

    private static string LowercaseFirst(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return char.ToLowerInvariant(text[0]) + text[1..];
    }

    private static string CollapseRepeatedPunctuation(string text)
    {
        text = Regex.Replace(text, @"\.{2,}", ".");
        text = Regex.Replace(text, @"!{2,}", "!");
        text = Regex.Replace(text, @"\?{2,}", "?");
        text = Regex.Replace(text, @"\.\s*\.", ".");
        return text.Trim();
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
            .Where(entry => entry.Tags.Any(tag =>
                tags.Any(candidate => string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        return PickRandom(matched.Count > 0 ? matched : entries);
    }

    private static T PickRandom<T>(IReadOnlyList<T> items) =>
        items[Random.Shared.Next(items.Count)];
}
