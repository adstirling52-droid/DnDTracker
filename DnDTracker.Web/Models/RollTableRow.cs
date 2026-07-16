namespace DnDTracker.Web.Models;

public class RollTableRow
{
    public Guid Id { get; set; }

    public Guid RollTableId { get; set; }

    public int Number { get; set; }

    public string Name { get; set; } = "";

    public string PhysicalDescription { get; set; } = "";

    public string SpecialCharacteristics { get; set; } = "";

    public RollTable RollTable { get; set; } = null!;
}
