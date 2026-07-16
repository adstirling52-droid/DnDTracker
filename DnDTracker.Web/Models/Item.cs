namespace DnDTracker.Web.Models;

public class Item
{
    public Guid Id { get; set; }

    public Guid CampaignId { get; set; }

    public Guid? CharacterId { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string WhereFound { get; set; } = "";

    public string WhenFound { get; set; } = "";

    public string CurrentStatus { get; set; } = "";

    public string Notes { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public Campaign Campaign { get; set; } = null!;

    public Character? Character { get; set; }
}
