using System;
using System.Collections.Generic;
using System.Text;

namespace DnDTracker.Models
{
    public class RollTableRow
    {
        public int Number { get; set; }
        public string Name { get; set; } = "";
        public string PhysicalDescription { get; set; } = "";
        public string SpecialCharacteristics { get; set; } = "";
    }
}
