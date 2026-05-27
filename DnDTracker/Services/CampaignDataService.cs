using DnDTracker.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DnDTracker.Services
{
    public class CampaignDataService
    {
        private readonly string _dataFolder;
        private readonly string _dataFilePath;

        public CampaignDataService()
        {
            _dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DnDTracker");

            _dataFilePath = Path.Combine(_dataFolder, "campaigns.json");
        }

        public List<Campaign> LoadCampaigns()
        {
            if (!File.Exists(_dataFilePath))
            {
                return new List<Campaign>();
            }

            string json = File.ReadAllText(_dataFilePath);

            List<Campaign>? campaigns = JsonSerializer.Deserialize<List<Campaign>>(json);

            return campaigns ?? new List<Campaign>();
        }

        public void SaveCampaigns(List<Campaign> campaigns)
        {
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(campaigns, options);
            File.WriteAllText(_dataFilePath, json);
        }
    }
}
