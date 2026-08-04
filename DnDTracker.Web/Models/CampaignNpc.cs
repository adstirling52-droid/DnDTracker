namespace DnDTracker.Web.Models;

public class CampaignNpc
{
    public Guid Id { get; set; }

    public Guid CampaignId { get; set; }

    public DateTime SavedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string Name { get; set; } = "";

    public string Ancestry { get; set; } = "";

    public string GenderPresentation { get; set; } = "";

    public string AgeCategory { get; set; } = "";

    public string Occupation { get; set; } = "";

    public string Location { get; set; } = "";

    public bool IsCurrent { get; set; }

    public string Appearance { get; set; } = "";

    public string DistinctiveFeature { get; set; } = "";

    public string Personality { get; set; } = "";

    public string Mannerism { get; set; } = "";

    public string Voice { get; set; } = "";

    public string Background { get; set; } = "";

    public string Motivation { get; set; } = "";

    public string Secret { get; set; } = "";

    public string CurrentProblem { get; set; } = "";

    public string QuestHook { get; set; } = "";

    public string DangerOrComplication { get; set; } = "";

    public string DmSummary { get; set; } = "";

    public string ImagePrompt { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public Campaign Campaign { get; set; } = null!;
}
