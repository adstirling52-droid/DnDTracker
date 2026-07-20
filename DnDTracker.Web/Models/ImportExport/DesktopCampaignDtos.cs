namespace DnDTracker.Web.Models.ImportExport;

public class DesktopCampaignDto
{
    public string Name { get; set; } = "";

    public List<DesktopCharacterDto> Characters { get; set; } = [];

    public List<DesktopItemDto> UnassignedItems { get; set; } = [];
}

public class DesktopCharacterDto
{
    public string Name { get; set; } = "";

    public List<DesktopItemDto> Items { get; set; } = [];

    public List<DesktopSkillDto> Skills { get; set; } = [];
}

public class DesktopItemDto
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string WhereFound { get; set; } = "";

    public string WhenFound { get; set; } = "";

    public string CurrentStatus { get; set; } = "";

    public string Notes { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public List<DesktopProvenanceEntryDto>? ProvenanceEntries { get; set; }
}

public class DesktopProvenanceEntryDto
{
    public string What { get; set; } = "";

    public string Where { get; set; } = "";

    public string When { get; set; } = "";

    public string Notes { get; set; } = "";
}

public class DesktopSkillDto
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string Notes { get; set; } = "";
}
