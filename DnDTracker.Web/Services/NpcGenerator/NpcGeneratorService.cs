using System.Text.RegularExpressions;
using DnDTracker.Web.Models.NpcGenerator;

namespace DnDTracker.Web.Services.NpcGenerator;

public sealed class NpcGeneratorService(NpcGenerationDataProvider dataProvider)
{
    private static readonly (string ThirdPerson, string BaseForm)[] VerbBaseForms =
    [
        ("Asks", "ask"),
        ("Offers", "offer"),
        ("Claims", "claim"),
        ("Owes", "owe"),
        ("Keeps", "keep"),
        ("Finds", "find"),
        ("Settles", "settle"),
        ("Passes", "pass"),
        ("Hums", "hum"),
        ("Talks", "talk"),
        ("Speaks", "speak"),
        ("Uses", "use"),
        ("Glances", "glance"),
        ("Squints", "squint"),
        ("Taps", "tap")
    ];

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
        var pronouns = GetPronounSet(npc.GenderPresentation);
        var ancestryAdjective = ToAncestryAdjective(npc.Ancestry);

        var paragraphOne = JoinSentences(
            BuildIntroductionSentence(npc, ancestryAdjective),
            BuildDistinctiveSentence(npc.DistinctiveFeature),
            BuildBehaviourSentence(pronouns, npc.Personality, npc.Mannerism, npc.Voice));

        var paragraphTwo = JoinSentences(
            BuildBackgroundSentence(npc.Name, npc.Background),
            BuildMotivationSentence(pronouns, npc.Motivation),
            BuildSecretSentence(pronouns, npc.Secret),
            BuildProblemSentence(pronouns, npc.CurrentProblem),
            BuildQuestHookSentence(pronouns, npc.QuestHook),
            BuildDangerSentence(pronouns, npc.DangerOrComplication));

        return FinalizeProse(paragraphOne, paragraphTwo);
    }

    internal static string SelectIndefiniteArticle(string phrase)
    {
        var trimmed = phrase.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "a";
        }

        var firstWord = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();

        if (firstWord is "hour" or "hourglass" or "honest" or "heir")
        {
            return "an";
        }

        if (firstWord is "adult" or "university" or "user" or "one" or "once" or "unique" or "european" or "union")
        {
            return "a";
        }

        return firstWord.Length > 0 && "aeiou".Contains(firstWord[0]) ? "an" : "a";
    }

    internal static PronounSet GetPronounSet(string genderPresentation) =>
        PronounSet.FromGenderPresentation(genderPresentation);

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

    private static string BuildIntroductionSentence(GeneratedNpc npc, string ancestryAdjective)
    {
        var occupation = npc.Occupation.ToLowerInvariant();
        var rolePhrase = $"{npc.AgeCategory} {ancestryAdjective} {occupation}";
        var article = SelectIndefiniteArticle(npc.AgeCategory);
        var appearance = NormalizeField(npc.Appearance);

        if (IsAdjectiveLedFragment(appearance))
        {
            return EnsureSentence(
                $"{npc.Name} is {article} {rolePhrase}, {LowercaseFirst(appearance)}");
        }

        if (IsNounFragment(appearance))
        {
            var appearanceBody = StripLeadingArticle(appearance);
            var appearanceArticle = SelectIndefiniteArticle(appearanceBody);
            return EnsureSentence(
                $"{npc.Name} is {article} {rolePhrase} with {appearanceArticle} {LowercaseFirst(appearanceBody)}");
        }

        return EnsureSentence($"{npc.Name} is {article} {rolePhrase}. {BuildAppearanceSentence(npc.Name, appearance)}");
    }

    private static string BuildAppearanceSentence(string name, string appearance)
    {
        var cleaned = NormalizeField(appearance);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithVerb(cleaned))
        {
            return EnsureSentence($"{name} {LowercaseFirst(cleaned)}");
        }

        return EnsureSentence($"{name} looks {LowercaseFirst(cleaned)}");
    }

    private static string BuildDistinctiveSentence(string distinctiveFeature)
    {
        var cleaned = NormalizeField(distinctiveFeature);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithArticle(cleaned))
        {
            return EnsureSentence(cleaned);
        }

        return EnsureSentence($"One notable feature is {LowercaseFirst(cleaned)}");
    }

    private static string BuildBehaviourSentence(
        PronounSet pronouns,
        string personality,
        string mannerism,
        string voice)
    {
        var personalityClause = BuildPersonalityClause(pronouns, personality);
        var mannerismClause = BuildMannerismClause(pronouns, mannerism);
        var voiceClause = BuildVoiceClause(pronouns, voice);

        return EnsureSentence($"{personalityClause}, {mannerismClause}, and {voiceClause}");
    }

    private static string BuildPersonalityClause(PronounSet pronouns, string personality)
    {
        var cleaned = ApplyGenericPossessives(NormalizeField(personality), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return $"{pronouns.Subject} {pronouns.Is} guarded";
        }

        if (StartsWithPronoun(cleaned))
        {
            return AlignPronoun(cleaned, pronouns);
        }

        return $"{pronouns.Subject} {pronouns.Is} {LowercaseFirst(cleaned)}";
    }

    private static string BuildMannerismClause(PronounSet pronouns, string mannerism)
    {
        var cleaned = ApplyGenericPossessives(NormalizeField(mannerism), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "often pausing to choose their words carefully".Replace("their", pronouns.PossessiveAdjective, StringComparison.Ordinal);
        }

        return ToGerundClause(cleaned);
    }

    private static string BuildVoiceClause(PronounSet pronouns, string voice)
    {
        var cleaned = ApplyGenericPossessives(NormalizeField(voice), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return $"{pronouns.SubjectLower} {pronouns.Speaks} in a steady voice";
        }

        if (cleaned.StartsWith("Speaks ", StringComparison.OrdinalIgnoreCase))
        {
            return "speaking" + cleaned[6..].ToLowerInvariant();
        }

        if (cleaned.StartsWith("Talks ", StringComparison.OrdinalIgnoreCase))
        {
            return "talking" + cleaned[5..].ToLowerInvariant();
        }

        if (cleaned.StartsWith("Uses ", StringComparison.OrdinalIgnoreCase))
        {
            return "using" + cleaned[4..].ToLowerInvariant();
        }

        return ToGerundClause(cleaned);
    }

    private static string BuildBackgroundSentence(string name, string background)
    {
        var cleaned = NormalizeField(background);
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

    private static string BuildMotivationSentence(PronounSet pronouns, string motivation)
    {
        var cleaned = ApplyGenericPossessives(NormalizeField(motivation), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithPronoun(cleaned))
        {
            return EnsureSentence(AlignPronoun(cleaned, pronouns));
        }

        if (cleaned.StartsWith("Keep ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"{pronouns.Subject} {pronouns.Wants} to keep {cleaned[5..].TrimStart().ToLowerInvariant()}");
        }

        if (StartsWithVerb(cleaned))
        {
            return EnsureSentence($"{pronouns.Subject} {pronouns.Wants} to {ToBaseVerbPhrase(cleaned)}");
        }

        return EnsureSentence($"{pronouns.Subject} {pronouns.Is} driven to {LowercaseFirst(cleaned)}");
    }

    private static string BuildSecretSentence(PronounSet pronouns, string secret)
    {
        var cleaned = ApplyGenericPossessives(NormalizeField(secret), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithPronoun(cleaned))
        {
            return EnsureSentence($"At the same time, {LowercaseFirst(AlignPronoun(cleaned, pronouns))}");
        }

        if (StartsWithVerb(cleaned))
        {
            return EnsureSentence($"At the same time, {pronouns.SubjectLower} {ToThirdPersonVerbPhrase(cleaned, pronouns)}");
        }

        return EnsureSentence($"At the same time, {pronouns.SubjectLower} {AdaptClauseForSubject(LowercaseFirst(cleaned), pronouns)}");
    }

    private static string BuildProblemSentence(PronounSet pronouns, string problem)
    {
        var cleaned = ApplyGenericPossessives(NormalizeField(problem), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithPronoun(cleaned))
        {
            return EnsureSentence($"Now {LowercaseFirst(AlignPronoun(cleaned, pronouns))}");
        }

        if (cleaned.StartsWith("Owes ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"Now {pronouns.SubjectLower} {pronouns.Owes} {cleaned[5..].TrimStart().ToLowerInvariant()}");
        }

        if (StartsWithVerb(cleaned))
        {
            return EnsureSentence($"Now {pronouns.SubjectLower} {ToThirdPersonVerbPhrase(cleaned, pronouns)}");
        }

        return EnsureSentence($"Now, {LowercaseFirst(cleaned)}");
    }

    private static string BuildQuestHookSentence(PronounSet pronouns, string questHook)
    {
        var cleaned = ApplyGenericPossessives(NormalizeField(questHook), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (StartsWithPronoun(cleaned))
        {
            return EnsureSentence(AlignPronoun(cleaned, pronouns));
        }

        if (StartsWithVerb(cleaned))
        {
            return EnsureSentence($"{pronouns.Subject} may {ToBaseVerbPhrase(cleaned)}");
        }

        return EnsureSentence($"{pronouns.Subject} may {LowercaseFirst(cleaned)}");
    }

    private static string BuildDangerSentence(PronounSet pronouns, string danger)
    {
        var cleaned = ApplyGenericPossessives(NormalizeField(danger), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("Helping ", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"However, {LowercaseFirst(cleaned)}");
        }

        if (cleaned.StartsWith("Their ", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = $"{pronouns.PossessiveAdjective} {cleaned[6..]}";
        }

        return EnsureSentence($"However, {LowercaseFirst(cleaned)}");
    }

    private static string AlignPronoun(string text, PronounSet pronouns)
    {
        var words = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return text;
        }

        var remainder = words.Length > 1 ? words[1] : string.Empty;
        return string.IsNullOrWhiteSpace(remainder)
            ? pronouns.Subject
            : $"{pronouns.Subject} {AdaptVerbAgreement(remainder, pronouns)}";
    }

    private static string AdaptVerbAgreement(string remainder, PronounSet pronouns)
    {
        var words = remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return remainder;
        }

        var firstWord = words[0];
        if (pronouns.Subject == "They")
        {
            if (firstWord.Equals("is", StringComparison.OrdinalIgnoreCase))
            {
                words[0] = "are";
            }
            else if (firstWord.Equals("has", StringComparison.OrdinalIgnoreCase))
            {
                words[0] = "have";
            }
            else if (firstWord.EndsWith('s') &&
                     !firstWord.EndsWith("ss", StringComparison.OrdinalIgnoreCase) &&
                     firstWord.Length > 2)
            {
                words[0] = firstWord[..^1];
            }
        }

        return string.Join(' ', words);
    }

    private static string AdaptClauseForSubject(string clause, PronounSet pronouns)
    {
        if (pronouns.Subject != "They")
        {
            return clause;
        }

        var words = clause.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            foreach (var (thirdPerson, baseForm) in VerbBaseForms)
            {
                if (words[i].Equals(thirdPerson, StringComparison.OrdinalIgnoreCase))
                {
                    words[i] = baseForm;
                    break;
                }
            }
        }

        return string.Join(' ', words);
    }

    private static string ToThirdPersonVerbPhrase(string text, PronounSet pronouns)
    {
        foreach (var adverb in new[] { "Quietly ", "Once ", "Secretly ", "Completed " })
        {
            if (text.StartsWith(adverb, StringComparison.OrdinalIgnoreCase))
            {
                var adaptedRemainder = ToThirdPersonVerbPhrase(text[adverb.Length..], pronouns);
                return $"{adverb.TrimEnd().ToLowerInvariant()} {adaptedRemainder}";
            }
        }

        foreach (var (thirdPerson, baseForm) in VerbBaseForms)
        {
            if (text.StartsWith(thirdPerson + " ", StringComparison.OrdinalIgnoreCase))
            {
                var rest = text[thirdPerson.Length..].ToLowerInvariant();
                var verb = pronouns.Subject == "They" ? baseForm : thirdPerson.ToLowerInvariant();
                return verb + rest;
            }
        }

        return AdaptClauseForSubject(LowercaseFirst(text), pronouns);
    }

    private static string ToBaseVerbPhrase(string text)
    {
        foreach (var (thirdPerson, baseForm) in VerbBaseForms)
        {
            if (text.StartsWith(thirdPerson + " ", StringComparison.OrdinalIgnoreCase))
            {
                return baseForm + text[thirdPerson.Length..].ToLowerInvariant();
            }
        }

        return LowercaseFirst(text);
    }

    private static string ToGerundClause(string text)
    {
        foreach (var (prefix, gerund) in VerbGerundReplacements)
        {
            if (text.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase) ||
                text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return gerund + text[prefix.Length..].ToLowerInvariant();
            }
        }

        return LowercaseFirst(text);
    }

    private static string ApplyGenericPossessives(string text, PronounSet pronouns) =>
        Regex.Replace(text, @"\btheir\b", pronouns.PossessiveAdjective, RegexOptions.IgnoreCase);

    private static string NormalizeField(string text) => TrimTerminalPunctuation(text.Trim());

    private static bool IsAdjectiveLedFragment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var firstWord = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return IsLikelyAdjective(firstWord) ||
               firstWord.StartsWith("Broad-", StringComparison.OrdinalIgnoreCase) ||
               firstWord.StartsWith("Compact", StringComparison.OrdinalIgnoreCase) ||
               firstWord.StartsWith("Otherwise", StringComparison.OrdinalIgnoreCase) ||
               firstWord.StartsWith("Lean", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripLeadingArticle(string text)
    {
        text = text.Trim();
        if (text.StartsWith("A ", StringComparison.OrdinalIgnoreCase))
        {
            return text[2..].TrimStart();
        }

        if (text.StartsWith("An ", StringComparison.OrdinalIgnoreCase))
        {
            return text[3..].TrimStart();
        }

        return text;
    }

    private static bool IsNounFragment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.StartsWith("A weathered", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("An old", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("A face", StringComparison.OrdinalIgnoreCase) ||
               (text.StartsWith("A ", StringComparison.OrdinalIgnoreCase) && !IsAdjectiveLedFragment(text));
    }

    private static bool StartsWithArticle(string text) =>
        text.StartsWith("A ", StringComparison.OrdinalIgnoreCase) ||
        text.StartsWith("An ", StringComparison.OrdinalIgnoreCase) ||
        text.StartsWith("One ", StringComparison.OrdinalIgnoreCase) ||
        text.StartsWith("The ", StringComparison.OrdinalIgnoreCase);

    private static bool StartsWithVerb(string text)
    {
        foreach (var (thirdPerson, _) in VerbBaseForms)
        {
            if (text.StartsWith(thirdPerson + " ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return text.StartsWith("Inherited ", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("Completed ", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("Once ", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("Quietly ", StringComparison.OrdinalIgnoreCase);
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

        if (StartsWithArticle(cleaned))
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
        return normalized is "warm" or "dry" or "restless" or "guarded" or "lean" or "quiet" or "gravelly" or "soft" or "weathered" or "broad-shouldered" or "compact";
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

    internal sealed record PronounSet(
        string Subject,
        string Object,
        string PossessiveAdjective,
        string PossessivePronoun,
        string Reflexive)
    {
        public string SubjectLower => Subject.ToLowerInvariant();

        public string Is => Subject == "They" ? "are" : "is";

        public string Has => Subject == "They" ? "have" : "has";

        public string Wants => Subject == "They" ? "want" : "wants";

        public string Owes => Subject == "They" ? "owe" : "owes";

        public string Speaks => Subject == "They" ? "speak" : "speaks";

        public static PronounSet FromGenderPresentation(string genderPresentation)
        {
            var normalized = genderPresentation.ToLowerInvariant();
            if (normalized.Contains("feminine", StringComparison.Ordinal))
            {
                return new PronounSet("She", "her", "her", "hers", "herself");
            }

            if (normalized.Contains("masculine", StringComparison.Ordinal))
            {
                return new PronounSet("He", "him", "his", "his", "himself");
            }

            return new PronounSet("They", "them", "their", "theirs", "themselves");
        }
    }
}
