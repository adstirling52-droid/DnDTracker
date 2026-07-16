namespace DnDTracker.Web.Models;

public class Character
{
    public Guid Id { get; set; }

    public Guid CampaignId { get; set; }

    public string Name { get; set; } = "";

    public Campaign Campaign { get; set; } = null!;

    public List<Item> Items { get; set; } = new();

    public List<Skill> Skills { get; set; } = new();
}
