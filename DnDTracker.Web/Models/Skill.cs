namespace DnDTracker.Web.Models;

public class Skill
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string Notes { get; set; } = "";

    public Character Character { get; set; } = null!;
}
