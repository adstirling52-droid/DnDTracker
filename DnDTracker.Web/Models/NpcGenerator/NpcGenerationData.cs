namespace DnDTracker.Web.Models.NpcGenerator;

public sealed class NpcGenerationData
{
    public int SchemaVersion { get; set; }

    public string Tone { get; set; } = "";

    public List<NpcAncestryEntry> Ancestries { get; set; } = [];

    public Dictionary<string, List<string>> NamesByAncestry { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<NpcTaggedTextEntry> AgeCategories { get; set; } = [];

    public List<NpcTaggedTextEntry> GenderPresentations { get; set; } = [];

    public List<NpcOccupationEntry> Occupations { get; set; } = [];

    public List<NpcTaggedTextEntry> Appearances { get; set; } = [];

    public List<NpcTaggedTextEntry> DistinctiveFeatures { get; set; } = [];

    public List<NpcTaggedTextEntry> Personalities { get; set; } = [];

    public List<NpcTaggedTextEntry> Mannerisms { get; set; } = [];

    public List<NpcTaggedTextEntry> Voices { get; set; } = [];

    public List<NpcTaggedTextEntry> Backgrounds { get; set; } = [];

    public List<NpcTaggedTextEntry> Motivations { get; set; } = [];

    public List<NpcTaggedTextEntry> Secrets { get; set; } = [];

    public List<NpcTaggedTextEntry> Problems { get; set; } = [];

    public List<NpcTaggedTextEntry> QuestHooks { get; set; } = [];

    public List<NpcTaggedTextEntry> Dangers { get; set; } = [];

    public List<NpcTaggedTextEntry> ImageEnvironments { get; set; } = [];
}

public sealed class NpcAncestryEntry
{
    public string Id { get; set; } = "";

    public string Label { get; set; } = "";

    public List<string> Tags { get; set; } = [];
}

public sealed class NpcOccupationEntry
{
    public string Id { get; set; } = "";

    public string Label { get; set; } = "";

    public List<string> Tags { get; set; } = [];

    public List<string> AppearanceTags { get; set; } = [];
}

public sealed class NpcTaggedTextEntry
{
    public string Id { get; set; } = "";

    public string Text { get; set; } = "";

    public List<string> Tags { get; set; } = [];
}
