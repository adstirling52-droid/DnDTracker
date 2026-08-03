using System.Text.Json;
using DnDTracker.Web.Models.NpcGenerator;

namespace DnDTracker.Web.Services.NpcGenerator;

public sealed class NpcGenerationDataProvider
{
    public const int SupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public NpcGenerationDataProvider(IWebHostEnvironment environment, ILogger<NpcGenerationDataProvider> logger)
        : this(Path.Combine(environment.ContentRootPath, "Data", "NpcGenerator", "npc-generation-data.json"), logger)
    {
    }

    internal NpcGenerationDataProvider(string dataFilePath, ILogger<NpcGenerationDataProvider> logger)
    {
        var (data, error) = Load(dataFilePath);
        Data = data ?? new NpcGenerationData();
        LoadError = error;

        if (error is null)
        {
            logger.LogInformation("NPC generation data loaded from {DataFilePath}.", dataFilePath);
        }
        else
        {
            logger.LogError("NPC generation data failed to load from {DataFilePath}: {LoadError}", dataFilePath, error);
        }
    }

    public NpcGenerationData Data { get; }

    public string? LoadError { get; }

    public bool IsLoaded => LoadError is null;

    public static (NpcGenerationData? Data, string? Error) Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return (null, $"NPC generation data file was not found at '{filePath}'.");
        }

        NpcGenerationData? data;
        try
        {
            var json = File.ReadAllText(filePath);
            data = JsonSerializer.Deserialize<NpcGenerationData>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return (null, $"NPC generation data file is not valid JSON: {ex.Message}");
        }

        if (data is null)
        {
            return (null, "NPC generation data file did not contain a valid data object.");
        }

        var validationError = Validate(data);
        if (validationError is not null)
        {
            return (null, validationError);
        }

        return (data, null);
    }

    internal static string? Validate(NpcGenerationData data)
    {
        if (data.SchemaVersion != SupportedSchemaVersion)
        {
            return $"Unsupported NPC generation schema version {data.SchemaVersion}. Expected {SupportedSchemaVersion}.";
        }

        if (string.IsNullOrWhiteSpace(data.Tone))
        {
            return "NPC generation data is missing a tone description.";
        }

        if (data.Ancestries.Count == 0)
        {
            return "NPC generation data must include at least one ancestry.";
        }

        var ancestryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ancestry in data.Ancestries)
        {
            if (string.IsNullOrWhiteSpace(ancestry.Id))
            {
                return "An ancestry entry is missing an id.";
            }

            if (string.IsNullOrWhiteSpace(ancestry.Label))
            {
                return $"Ancestry '{ancestry.Id}' is missing a label.";
            }

            if (!ancestryIds.Add(ancestry.Id))
            {
                return $"Duplicate ancestry id '{ancestry.Id}'.";
            }
        }

        if (data.NamesByAncestry.Count == 0)
        {
            return "NPC generation data must include names grouped by ancestry.";
        }

        foreach (var ancestry in data.Ancestries)
        {
            if (!data.NamesByAncestry.TryGetValue(ancestry.Id, out var names) || names.Count == 0)
            {
                return $"NPC generation data must include at least one name for ancestry '{ancestry.Id}'.";
            }

            if (names.Any(string.IsNullOrWhiteSpace))
            {
                return $"Ancestry '{ancestry.Id}' contains an empty name entry.";
            }
        }

        foreach (var unknownAncestryId in data.NamesByAncestry.Keys)
        {
            if (!ancestryIds.Contains(unknownAncestryId))
            {
                return $"NamesByAncestry contains an unknown ancestry id '{unknownAncestryId}'.";
            }
        }

        var requiredLists = new (string Name, int Count)[]
        {
            ("AgeCategories", data.AgeCategories.Count),
            ("GenderPresentations", data.GenderPresentations.Count),
            ("Occupations", data.Occupations.Count),
            ("Appearances", data.Appearances.Count),
            ("DistinctiveFeatures", data.DistinctiveFeatures.Count),
            ("Personalities", data.Personalities.Count),
            ("Mannerisms", data.Mannerisms.Count),
            ("Voices", data.Voices.Count),
            ("Backgrounds", data.Backgrounds.Count),
            ("Motivations", data.Motivations.Count),
            ("Secrets", data.Secrets.Count),
            ("Problems", data.Problems.Count),
            ("QuestHooks", data.QuestHooks.Count),
            ("Dangers", data.Dangers.Count),
            ("ImageEnvironments", data.ImageEnvironments.Count)
        };

        foreach (var (name, count) in requiredLists)
        {
            if (count == 0)
            {
                return $"NPC generation data must include at least one entry in {name}.";
            }
        }

        foreach (var occupation in data.Occupations)
        {
            if (string.IsNullOrWhiteSpace(occupation.Id))
            {
                return "An occupation entry is missing an id.";
            }

            if (string.IsNullOrWhiteSpace(occupation.Label))
            {
                return $"Occupation '{occupation.Id}' is missing a label.";
            }
        }

        var taggedLists = new (string Name, List<NpcTaggedTextEntry> Entries)[]
        {
            ("AgeCategories", data.AgeCategories),
            ("GenderPresentations", data.GenderPresentations),
            ("Appearances", data.Appearances),
            ("DistinctiveFeatures", data.DistinctiveFeatures),
            ("Personalities", data.Personalities),
            ("Mannerisms", data.Mannerisms),
            ("Voices", data.Voices),
            ("Backgrounds", data.Backgrounds),
            ("Motivations", data.Motivations),
            ("Secrets", data.Secrets),
            ("Problems", data.Problems),
            ("QuestHooks", data.QuestHooks),
            ("Dangers", data.Dangers),
            ("ImageEnvironments", data.ImageEnvironments)
        };

        foreach (var (listName, entries) in taggedLists)
        {
            var validationError = ValidateTaggedEntries(listName, entries);
            if (validationError is not null)
            {
                return validationError;
            }
        }

        return null;
    }

    private static string? ValidateTaggedEntries(string listName, List<NpcTaggedTextEntry> entries)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                return $"{listName} contains an entry without an id.";
            }

            if (string.IsNullOrWhiteSpace(entry.Text))
            {
                return $"{listName} entry '{entry.Id}' is missing text.";
            }

            if (!ids.Add(entry.Id))
            {
                return $"{listName} contains a duplicate id '{entry.Id}'.";
            }
        }

        return null;
    }
}
