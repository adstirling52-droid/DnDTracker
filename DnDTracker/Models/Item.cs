using System;
using System.Collections.Generic;
using System.Text;

namespace DnDTracker.Models
{
    public class Item
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string WhereFound { get; set; } = "";
        public string WhenFound { get; set; } = "";
        public string CurrentStatus { get; set; } = "";
        public string Notes { get; set; } = "";
        public List<ProvenanceEntry> ProvenanceEntries { get; set; } = new List<ProvenanceEntry>();

        public override string ToString()
        {
            return Name;
        }
    }
}