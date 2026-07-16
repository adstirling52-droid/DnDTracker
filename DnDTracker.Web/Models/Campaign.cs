namespace DnDTracker.Web.Models;

public class Campaign
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = "";

    public string Name { get; set; } = "";

    public List<Character> Characters { get; set; } = new();

    public List<Item> Items { get; set; } = new();
}
