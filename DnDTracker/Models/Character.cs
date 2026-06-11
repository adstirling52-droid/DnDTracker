using System;
using System.Collections.Generic;
using System.Text;

namespace DnDTracker.Models
{
    public class Character
    {
        public string Name { get; set; } = "";

        public List<Item> Items { get; set; } = new List<Item>();

        public override string ToString()
        {
            return Name;
        }
    }
}
