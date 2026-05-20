using System;
using System.Collections.Generic;
using System.Text;

namespace DnDTracker.Models
{
    public class Campaign
    {
        public string Name { get; set; } = "";
        public List<Character> Characters { get; set; } = new List<Character>();

        public override string ToString()
        {
            return Name;
        }
    }
}
