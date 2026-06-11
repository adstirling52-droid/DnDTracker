using System;
using System.Collections.Generic;
using System.Text;

namespace DnDTracker.Models
{
    public class Skill
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Notes { get; set; } = "";

        public override string ToString()
        {
            return Name;
        }
    }
}
