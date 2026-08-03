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
            BuildIntroWithAppearance(npc, ancestryAdjective),
            EnsureSentence(FormatDistinctiveFeature(npc.DistinctiveFeature, pronouns)),
            EnsureSentence(FormatBehaviour(npc.Personality, npc.Mannerism, npc.Voice, pronouns)));

        var paragraphTwo = JoinSentences(
            BuildBackgroundClause(npc, pronouns),
            BuildMotivationWithSecret(npc, pronouns),
            BuildSituationWithHookAndDanger(npc, pronouns));

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

        if (firstWord is "hour" or "hourglass" or "honest" or "heir" or "elderly" or "expected" or "innkeeper" or "itinerant" or "old" or "unusual" or "unknown" or "impossible")
        {
            return "an";
        }

        if (firstWord is "adult" or "university" or "user" or "one" or "once" or "unique" or "european" or "union" or "young" or "middle-aged" or "middle" or "dwarven" or "human" or "elven" or "lean" or "compact" or "broad-shouldered" or "weathered")
        {
            return firstWord is "adult" ? "an" : "a";
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

    private static string BuildIntroWithAppearance(GeneratedNpc npc, string ancestryAdjective)
    {
        var rolePhrase = FormatRolePhrase(npc.AgeCategory, ancestryAdjective, npc.Occupation);
        var appearance = NormalizeField(npc.Appearance);

        if (appearance.StartsWith("Otherwise unremarkable", StringComparison.OrdinalIgnoreCase))
        {
            var detail = appearance.StartsWith("Otherwise unremarkable at a glance, except for ", StringComparison.OrdinalIgnoreCase)
                ? appearance["Otherwise unremarkable at a glance, except for ".Length..]
                : appearance["Otherwise unremarkable, except for ".Length..];
            detail = LowercaseFirst(TrimTerminalPunctuation(detail));
            return EnsureSentence(
                $"{npc.Name} is {rolePhrase}, otherwise unremarkable at a glance save for {detail}");
        }

        if (IsAdjectiveLedFragment(appearance))
        {
            return EnsureSentence($"{npc.Name} is {rolePhrase}, {LowercaseFirst(appearance)}");
        }

        if (IsNounFragment(appearance))
        {
            var appearanceBody = StripLeadingArticle(appearance);
            var appearanceArticle = SelectIndefiniteArticle(appearanceBody);
            return EnsureSentence(
                $"{npc.Name} is {rolePhrase} with {appearanceArticle} {LowercaseFirst(appearanceBody)}");
        }

        if (string.IsNullOrWhiteSpace(appearance))
        {
            return EnsureSentence($"{npc.Name} is {rolePhrase}");
        }

        return EnsureSentence($"{npc.Name} is {rolePhrase}, {LowercaseFirst(appearance)}");
    }

    private static string FormatRolePhrase(string ageCategory, string ancestryAdjective, string occupation)
    {
        var occupationLower = occupation.ToLowerInvariant();
        var normalizedAge = ageCategory.ToLowerInvariant().Trim();

        return normalizedAge switch
        {
            "adult in their prime" => $"an adult {ancestryAdjective} {occupationLower} in the prime of life",
            "elderly" => $"an elderly {ancestryAdjective} {occupationLower}",
            "young adult" => $"a young adult {ancestryAdjective} {occupationLower}",
            "middle-aged" => $"a middle-aged {ancestryAdjective} {occupationLower}",
            _ => $"{SelectIndefiniteArticle(normalizedAge)} {normalizedAge} {ancestryAdjective} {occupationLower}"
        };
    }

    private static string FormatDistinctiveFeature(string distinctiveFeature, PronounSet pronouns)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(distinctiveFeature), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("Ink-stained fingers", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.PossessiveAdjective} fingers are permanently stained with ink that never quite washes clean";
        }

        if (cleaned.StartsWith("A faint scent of dried herbs", StringComparison.OrdinalIgnoreCase))
        {
            return $"A faint scent of dried herbs clings to {pronouns.PossessiveAdjective} clothes";
        }

        if (cleaned.StartsWith("A braided beard", StringComparison.OrdinalIgnoreCase))
        {
            return cleaned;
        }

        if (StartsWithArticle(cleaned))
        {
            return cleaned;
        }

        return $"{pronouns.Subject} has {LowercaseFirst(cleaned)}";
    }

    private static string FormatBehaviour(string personality, string mannerism, string voice, PronounSet pronouns)
    {
        var personalityPhrase = FormatPersonality(personality, pronouns);
        var mannerismPhrase = FormatMannerism(mannerism, pronouns, includeSubject: false);
        var voicePhrase = FormatVoice(voice, pronouns, includeSubject: false);

        var trailing = new[] { mannerismPhrase, voicePhrase }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        if (trailing.Count == 0)
        {
            return personalityPhrase;
        }

        if (trailing.Count == 1)
        {
            return $"{personalityPhrase}, {trailing[0]}";
        }

        return $"{personalityPhrase}, {trailing[0]}, and {trailing[1]}";
    }

    private static string FormatPersonality(string personality, PronounSet pronouns)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(personality), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return $"{pronouns.Subject} {pronouns.Is} guarded";
        }

        if (cleaned.StartsWith("Dry, precise, and difficult to fluster", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.Subject} has a dry, precise manner and is difficult to unsettle";
        }

        if (cleaned.StartsWith("Warm with strangers but quietly watchful", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.Subject} {pronouns.Is} warm with strangers but quietly watchful";
        }

        if (cleaned.StartsWith("Guarded at first, but unexpectedly kind once trust is earned", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.Subject} {pronouns.Is} guarded at first, but unexpectedly kind once trust is earned";
        }

        if (cleaned.StartsWith("Restlessly curious", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.Subject} {pronouns.Is} restlessly curious, with a habit of turning conversations back to other people";
        }

        if (StartsWithPronoun(cleaned))
        {
            return AlignPronoun(cleaned, pronouns);
        }

        if (IsLikelyAdjective(cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]))
        {
            return $"{pronouns.Subject} {pronouns.Is} {LowercaseFirst(cleaned)}";
        }

        return $"{pronouns.Subject} {pronouns.Has} a {LowercaseFirst(cleaned)} manner";
    }

    private static string FormatMannerism(string mannerism, PronounSet pronouns, bool includeSubject = true)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(mannerism), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("Squints slightly when trying to recall a detail", StringComparison.OrdinalIgnoreCase))
        {
            return includeSubject
                ? $"{pronouns.Subject} squints slightly when trying to recall a detail"
                : "squints slightly when trying to recall a detail";
        }

        if (cleaned.StartsWith("Hums under", StringComparison.OrdinalIgnoreCase))
        {
            return includeSubject
                ? $"{pronouns.Subject} hums under {pronouns.PossessiveAdjective} breath while working"
                : $"hums under {pronouns.PossessiveAdjective} breath while working";
        }

        if (cleaned.StartsWith("Glances toward exits and windows when entering a new room", StringComparison.OrdinalIgnoreCase))
        {
            return includeSubject
                ? $"{pronouns.Subject} glances toward exits and windows when entering a new room"
                : "glances toward exits and windows when entering a new room";
        }

        if (StartsWithVerb(cleaned))
        {
            var phrase = AdaptClauseForSubject(LowercaseFirst(cleaned), pronouns);
            return includeSubject ? $"{pronouns.Subject} {phrase}" : phrase;
        }

        var fallback = AdaptClauseForSubject(LowercaseFirst(cleaned), pronouns);
        return includeSubject ? $"{pronouns.Subject} {fallback}" : fallback;
    }

    private static string FormatVoice(string voice, PronounSet pronouns, bool includeSubject = true)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(voice), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return includeSubject
                ? $"{pronouns.Subject} {pronouns.Speaks} in a steady voice"
                : $"{pronouns.Speaks} in a steady voice";
        }

        if (cleaned.StartsWith("Uses soft, carefully chosen words", StringComparison.OrdinalIgnoreCase))
        {
            return includeSubject
                ? $"{pronouns.Subject} {pronouns.Speaks} in soft, carefully chosen words, as if afraid of being overheard"
                : $"{pronouns.Speaks} in soft, carefully chosen words, as if afraid of being overheard";
        }

        if (cleaned.StartsWith("Speaks in a low, measured voice", StringComparison.OrdinalIgnoreCase))
        {
            return includeSubject
                ? $"{pronouns.Subject} {pronouns.Speaks} in a low, measured voice"
                : $"{pronouns.Speaks} in a low, measured voice";
        }

        if (cleaned.StartsWith("Talks quickly, with a warm regional lilt", StringComparison.OrdinalIgnoreCase))
        {
            return includeSubject
                ? $"{pronouns.Subject} talks quickly, with a warm regional lilt"
                : "talks quickly, with a warm regional lilt";
        }

        if (cleaned.StartsWith("Gravelly and direct", StringComparison.OrdinalIgnoreCase))
        {
            return includeSubject
                ? $"{pronouns.Subject} {pronouns.Is} gravelly and direct, with little patience for evasion"
                : $"{pronouns.Is} gravelly and direct, with little patience for evasion";
        }

        if (cleaned.StartsWith("Speaks ", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("Talks ", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("Uses ", StringComparison.OrdinalIgnoreCase))
        {
            var phrase = AdaptClauseForSubject(LowercaseFirst(cleaned), pronouns);
            return includeSubject ? $"{pronouns.Subject} {phrase}" : phrase;
        }

        return includeSubject
            ? $"{pronouns.Subject} {pronouns.Speaks} with a {LowercaseFirst(cleaned)} voice"
            : $"{pronouns.Speaks} with a {LowercaseFirst(cleaned)} voice";
    }

    private static string BuildBackgroundClause(GeneratedNpc npc, PronounSet pronouns)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(npc.Background), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("Completed most of a respectable apprenticeship", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence(
                $"{pronouns.Subject} completed most of a respectable apprenticeship, then fled after discovering something unsettling in {pronouns.PossessiveAdjective} master's records");
        }

        if (cleaned.StartsWith("Inherited a modest family trade", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"{npc.Name} inherited a modest family trade and has kept it alive through stubborn competence rather than ambition");
        }

        if (cleaned.StartsWith("Once served in a border company", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSentence($"{pronouns.Subject} once served in a border company and left after a mission went badly wrong");
        }

        if (StartsWithPronoun(cleaned))
        {
            return EnsureSentence(AlignPronoun(cleaned, pronouns));
        }

        if (StartsWithVerb(cleaned))
        {
            return EnsureSentence($"{pronouns.Subject} {AdaptClauseForSubject(LowercaseFirst(cleaned), pronouns)}");
        }

        return EnsureSentence($"{npc.Name} {LowercaseFirst(cleaned)}");
    }

    private static string BuildMotivationWithSecret(GeneratedNpc npc, PronounSet pronouns)
    {
        var motivation = FormatMotivation(npc.Motivation, pronouns);
        var secret = FormatSecret(npc.Secret, pronouns);

        if (string.IsNullOrWhiteSpace(motivation))
        {
            return EnsureSentence(secret);
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            return EnsureSentence(motivation);
        }

        return EnsureSentence($"{motivation}, though {LowercaseFirst(secret)}");
    }

    private static string FormatMotivation(string motivation, PronounSet pronouns)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(motivation), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("Settle an old debt before it ruins someone", StringComparison.OrdinalIgnoreCase))
        {
            var carePhrase = pronouns.Subject == "They"
                ? "someone they care about"
                : $"someone {pronouns.SubjectLower} cares about";
            return $"{pronouns.Subject} {pronouns.Wants} to settle an old debt before it ruins {carePhrase}";
        }

        if (cleaned.StartsWith("Keep ", StringComparison.OrdinalIgnoreCase))
        {
            var remainder = ApplyNpcPronouns(cleaned[5..].TrimStart(), pronouns);
            return $"{pronouns.Subject} {pronouns.Wants} to keep {LowercaseFirst(remainder)}";
        }

        if (cleaned.StartsWith("Find out what happened to someone who vanished", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.Subject} {pronouns.Wants} to find out what happened to someone who vanished without explanation";
        }

        if (StartsWithVerb(cleaned))
        {
            return $"{pronouns.Subject} {pronouns.Wants} to {ToBaseVerbPhrase(cleaned)}";
        }

        return $"{pronouns.Subject} {pronouns.Is} driven to {LowercaseFirst(cleaned)}";
    }

    private static string FormatSecret(string secret, PronounSet pronouns)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(secret), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("Uses a false name because", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.SubjectLower} uses a false name because {pronouns.PossessiveAdjective} real one would attract the wrong attention";
        }

        if (cleaned.StartsWith("Quietly passes messages for a smuggler", StringComparison.OrdinalIgnoreCase))
        {
            var verb = pronouns.Subject == "They" ? "pass" : "passes";
            return $"{pronouns.SubjectLower} quietly {verb} messages for a smuggler in exchange for protection";
        }

        if (cleaned.StartsWith("Hides a valuable heirloom", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.SubjectLower} hides a valuable heirloom that would be seized if discovered";
        }

        if (StartsWithPronoun(cleaned))
        {
            return LowercaseFirst(AlignPronoun(cleaned, pronouns));
        }

        if (StartsWithVerb(cleaned))
        {
            return $"{pronouns.SubjectLower} {ToThirdPersonVerbPhrase(cleaned, pronouns)}";
        }

        return $"{pronouns.SubjectLower} {AdaptClauseForSubject(LowercaseFirst(cleaned), pronouns)}";
    }

    private static string BuildSituationWithHookAndDanger(GeneratedNpc npc, PronounSet pronouns)
    {
        var problem = FormatProblem(npc.CurrentProblem, pronouns);
        var questHook = FormatQuestHook(npc.QuestHook, pronouns);
        var danger = FormatDanger(npc.DangerOrComplication, pronouns);

        if (string.IsNullOrWhiteSpace(problem) && string.IsNullOrWhiteSpace(questHook) && string.IsNullOrWhiteSpace(danger))
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(problem) && !string.IsNullOrWhiteSpace(questHook) && !string.IsNullOrWhiteSpace(danger))
        {
            return EnsureSentence($"{problem}, and {LowercaseFirst(questHook)}—though {LowercaseFirst(danger)}");
        }

        if (!string.IsNullOrWhiteSpace(problem) && !string.IsNullOrWhiteSpace(questHook))
        {
            return EnsureSentence($"{problem}, and {LowercaseFirst(questHook)}");
        }

        if (!string.IsNullOrWhiteSpace(problem) && !string.IsNullOrWhiteSpace(danger))
        {
            return EnsureSentence($"{problem}, though {LowercaseFirst(danger)}");
        }

        return EnsureSentence(problem ?? questHook ?? danger ?? string.Empty);
    }

    private static string FormatProblem(string problem, PronounSet pronouns)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(problem), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("A expected shipment", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("An expected shipment", StringComparison.OrdinalIgnoreCase))
        {
            return "An expected shipment or payment has failed to arrive";
        }

        if (cleaned.StartsWith("Owes money to someone impatient and well connected", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.Subject} {pronouns.Owes} money to someone impatient and well connected";
        }

        if (cleaned.StartsWith("Someone is quietly sabotaging", StringComparison.OrdinalIgnoreCase))
        {
            return $"someone is quietly sabotaging {pronouns.PossessiveAdjective} livelihood";
        }

        if (StartsWithPronoun(cleaned))
        {
            return AlignPronoun(cleaned, pronouns);
        }

        if (StartsWithVerb(cleaned))
        {
            return $"{pronouns.Subject} {ToThirdPersonVerbPhrase(cleaned, pronouns)}";
        }

        var article = SelectIndefiniteArticle(cleaned);
        return $"{char.ToUpperInvariant(article[0])}{article[1..]} {LowercaseFirst(cleaned)}";
    }

    private static string FormatQuestHook(string questHook, PronounSet pronouns)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(questHook), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("Offers reliable local information if the party helps with a personal errand first", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.Subject} may offer reliable local information if the party helps with a personal errand first";
        }

        if (cleaned.StartsWith("Asks the party to watch", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.Subject} may ask the party to watch {pronouns.PossessiveAdjective} workplace for whoever is causing the trouble";
        }

        if (cleaned.StartsWith("Claims to have witnessed something impossible near town", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.Subject} may claim to have witnessed something impossible near town and needs someone credible to investigate";
        }

        if (StartsWithVerb(cleaned))
        {
            return $"{pronouns.Subject} may {ToBaseVerbPhrase(cleaned)}";
        }

        return $"{pronouns.Subject} may {LowercaseFirst(cleaned)}";
    }

    private static string FormatDanger(string danger, PronounSet pronouns)
    {
        var cleaned = ApplyNpcPronouns(NormalizeField(danger), pronouns);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (cleaned.StartsWith("Their creditor has ties to violent people", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith($"{pronouns.PossessiveAdjective} creditor has ties to violent people", StringComparison.OrdinalIgnoreCase))
        {
            return $"{pronouns.PossessiveAdjective} creditor has ties to violent people";
        }

        if (cleaned.StartsWith("Helping them may draw attention from local authorities", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith($"Helping {pronouns.Object} may draw attention from local authorities", StringComparison.OrdinalIgnoreCase))
        {
            return $"helping {pronouns.Object} may draw attention from local authorities";
        }

        if (cleaned.StartsWith("An unknown enemy already knows the party spoke with them", StringComparison.OrdinalIgnoreCase))
        {
            return $"an unknown enemy already knows the party spoke with {pronouns.Object}";
        }

        return LowercaseFirst(cleaned);
    }

    private static string ApplyNpcPronouns(string text, PronounSet pronouns)
    {
        text = Regex.Replace(text, @"\btheir\b", pronouns.PossessiveAdjective, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bthemselves\b", pronouns.Reflexive, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bthem\b", pronouns.Object, RegexOptions.IgnoreCase);
        text = AdaptNpcSubjectPronouns(text, pronouns);
        return text;
    }

    private static string AdaptNpcSubjectPronouns(string text, PronounSet pronouns)
    {
        text = Regex.Replace(
            text,
            @"\bthey (?<verb>[a-z]+)\b",
            match => $"{pronouns.SubjectLower} {ConjugateVerbForSubject(match.Groups["verb"].Value, pronouns)}",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(text, @"\bthey\b", pronouns.SubjectLower, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bshe\b", pronouns.SubjectLower, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bhe\b", pronouns.SubjectLower, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bher\b", pronouns.PossessiveAdjective, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bhis\b", pronouns.PossessiveAdjective, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bhim\b", pronouns.Object, RegexOptions.IgnoreCase);

        return text;
    }

    private static string ConjugateVerbForSubject(string verb, PronounSet pronouns)
    {
        var lowerVerb = verb.ToLowerInvariant();
        if (pronouns.Subject == "They")
        {
            if (lowerVerb.EndsWith("ies", StringComparison.Ordinal))
            {
                return lowerVerb[..^3] + "y";
            }

            if (lowerVerb.EndsWith("s", StringComparison.Ordinal) &&
                !lowerVerb.EndsWith("ss", StringComparison.Ordinal) &&
                lowerVerb is not "is" and not "has")
            {
                return lowerVerb[..^1];
            }

            if (lowerVerb == "is")
            {
                return "are";
            }

            if (lowerVerb == "has")
            {
                return "have";
            }

            return lowerVerb;
        }

        if (lowerVerb == "are")
        {
            return "is";
        }

        if (lowerVerb == "have")
        {
            return "has";
        }

        if (lowerVerb == "understand")
        {
            return "understands";
        }

        if (lowerVerb == "care")
        {
            return "cares";
        }

        if (lowerVerb == "pass")
        {
            return "passes";
        }

        if (!lowerVerb.EndsWith('s') || lowerVerb.EndsWith("ss", StringComparison.Ordinal))
        {
            return lowerVerb + "s";
        }

        return lowerVerb;
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
