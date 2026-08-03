namespace DnDTracker.Web.Models.ImportExport;

public class CampaignExportDto
{
    public string Name { get; set; } = "";

    public List<CampaignExportCharacterDto> Characters { get; set; } = [];

    public List<CampaignExportItemDto> UnassignedItems { get; set; } = [];
}

public class CampaignExportCharacterDto
{
    public string Name { get; set; } = "";

    public List<CampaignExportItemDto> Items { get; set; } = [];

    public List<CampaignExportSkillDto> Skills { get; set; } = [];
}

public class CampaignExportItemDto
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string WhereFound { get; set; } = "";

    public string WhenFound { get; set; } = "";

    public string CurrentStatus { get; set; } = "";

    public string Notes { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public List<CampaignExportProvenanceEntryDto>? ProvenanceEntries { get; set; }
}

public class CampaignExportProvenanceEntryDto
{
    public string What { get; set; } = "";

    public string Where { get; set; } = "";

    public string When { get; set; } = "";

    public string Notes { get; set; } = "";
}

public class CampaignExportSkillDto
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string Notes { get; set; } = "";
}
