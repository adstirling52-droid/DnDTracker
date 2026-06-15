using System.Collections.Generic;

namespace DnDTracker.Models
{
    public class RollTable
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public List<RollTableRow> Rows { get; set; } = new List<RollTableRow>();

        public override string ToString()
        {
            return Name;
        }
    }
}
