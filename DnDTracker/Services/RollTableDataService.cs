using DnDTracker.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DnDTracker.Services
{
    public class RollTableDataService
    {
        private readonly string _dataFolder;
        private readonly string _dataFilePath;

        public RollTableDataService()
        {
            _dataFolder = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "DnDTracker");

            _dataFilePath = Path.Combine(_dataFolder, "rolltables.json");
        }

        public List<RollTable> LoadRollTables()
        {
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            if (!File.Exists(_dataFilePath))
            {
                return new List<RollTable>();
            }

            string json = File.ReadAllText(_dataFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<RollTable>();
            }

            List<RollTable>? rollTables = JsonSerializer.Deserialize<List<RollTable>>(json);

            return rollTables ?? new List<RollTable>();
        }

        public void SaveRollTables(List<RollTable> rollTables)
        {
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(rollTables, options);
            File.WriteAllText(_dataFilePath, json);
        }
    }
}
