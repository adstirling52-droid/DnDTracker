namespace DnDTracker.Web.Models;

public class RollTable
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = "";

    public string Name { get; set; } = "";

    public string Category { get; set; } = "";

    public string TableType { get; set; } = "Generic";

    public List<RollTableRow> Rows { get; set; } = new();
}
