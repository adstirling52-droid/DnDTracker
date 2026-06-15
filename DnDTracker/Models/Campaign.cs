using System.Collections.Generic;

namespace DnDTracker.Models
{
    public class Campaign
    {
        public string Name { get; set; } = "";
        public List<Character> Characters { get; set; } = new List<Character>();
        public List<Item> UnassignedItems { get; set; } = new List<Item>();
        public List<RollTable> RollTables { get; set; } = new List<RollTable>();

        public override string ToString()
        {
            return Name;
        }
    }
}
