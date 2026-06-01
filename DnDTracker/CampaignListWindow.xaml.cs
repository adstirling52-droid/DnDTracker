using DnDTracker.Models;
using DnDTracker.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;


namespace DnDTracker
{
    public partial class CampaignListWindow : Window
    {

        private List<Campaign> _campaigns = new List<Campaign>();

        private CampaignDataService _campaignDataService = new CampaignDataService();

        public CampaignListWindow()
        {
            InitializeComponent();

            LoadCampaigns();

            
        }

        private void LoadSampleCampaigns()
        {

            _campaigns.Clear();

            _campaigns.Add(CampaignWindow.CreateSampleCampaign("Errae"));
            _campaigns.Add(new Campaign { Name = "Lake Mizkagi" });
            _campaigns.Add(new Campaign { Name = "Keep on the Borderlands" });

            RefreshCampaignList();
        }

        private void LoadCampaigns()
        {
            _campaigns = _campaignDataService.LoadCampaigns();

            if (_campaigns.Count == 0)
            {
                LoadSampleCampaigns();
                SaveCampaigns();
            }
            else
            {
                RefreshCampaignList();
            }
        }

        private void SaveCampaigns()
        {
            _campaignDataService.SaveCampaigns(_campaigns);
        }

        private void RefreshCampaignList()
        {
            CampaignListBox.Items.Clear();

            foreach (Campaign campaign in _campaigns)
            {
                CampaignListBox.Items.Add(campaign);
            }
        }

        private void CampaignWindow_Closed(object? sender, EventArgs e)
        {
            this.Show();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NewCampaignButton_Click(object sender, RoutedEventArgs e) 
        {
            NewCampaignWindow newCampaignWindow = new NewCampaignWindow();
            newCampaignWindow.Owner = this;

            bool? result = newCampaignWindow.ShowDialog();

            if (result == true)
            {
                if (CampaignNameExists(newCampaignWindow.CampaignName))
                {
                    MessageBox.Show("A campaign with that name already exists.", "Duplicate Campaign Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Campaign newCampaign = new Campaign { Name = newCampaignWindow.CampaignName };
                _campaigns.Add(newCampaign);
                RefreshCampaignList();
                CampaignListBox.SelectedItem = newCampaign;
                SaveCampaigns();
            }
        }

        private void OpenCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            if (CampaignListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a campaign first.", "No Campaign Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Campaign selectedCampaign = (Campaign)CampaignListBox.SelectedItem;

            CampaignWindow campaignWindow = new CampaignWindow(selectedCampaign, _campaigns, _campaignDataService);
            campaignWindow.Owner = this;
            campaignWindow.Closed += CampaignWindow_Closed;

            this.Hide();
            campaignWindow.Show();
        }

        private void EditCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            if (CampaignListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a campaign first.", "No Campaign Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Campaign selectedCampaign = (Campaign)CampaignListBox.SelectedItem;

            NewCampaignWindow editCampaignWindow = new NewCampaignWindow(selectedCampaign);
            editCampaignWindow.Owner = this;

            bool? result = editCampaignWindow.ShowDialog();

            if (result == true)
            {
                if (CampaignNameExists(editCampaignWindow.CampaignName, selectedCampaign))
                {
                    MessageBox.Show("A campaign with that name already exists.", "Duplicate Campaign Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selectedCampaign.Name = editCampaignWindow.CampaignName;
                RefreshCampaignList();
                CampaignListBox.SelectedItem = selectedCampaign;
                SaveCampaigns();
            }
        }

        private void ExportCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            if (CampaignListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a campaign first.", "No Campaign Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Campaign selectedCampaign = (Campaign)CampaignListBox.SelectedItem;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Export Campaign";
            saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog.DefaultExt = "json";
            saveFileDialog.FileName = selectedCampaign.Name + ".json";

            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                _campaignDataService.ExportCampaign(selectedCampaign, saveFileDialog.FileName);

                MessageBox.Show("Campaign exported successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Import Campaign";
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.DefaultExt = "json";

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                Campaign? importedCampaign = _campaignDataService.ImportCampaign(openFileDialog.FileName);

                if (importedCampaign == null)
                {
                    MessageBox.Show("The selected campaign file could not be imported.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (CampaignNameExists(importedCampaign.Name))
                {
                    MessageBox.Show("A campaign with that name already exists.", "Duplicate Campaign Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _campaigns.Add(importedCampaign);
                RefreshCampaignList();
                CampaignListBox.SelectedItem = importedCampaign;
                SaveCampaigns();

                MessageBox.Show("Campaign imported successfully.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            if (CampaignListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a campaign first.", "No Campaign Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Campaign selectedCampaign = (Campaign)CampaignListBox.SelectedItem;

            MessageBoxResult result = MessageBox.Show(
                $"Remove campaign '{selectedCampaign.Name}'?",
                "Confirm Remove Campaign",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _campaigns.Remove(selectedCampaign);
            RefreshCampaignList();

            if (CampaignListBox.Items.Count > 0)
            {
                CampaignListBox.SelectedIndex = 0;
            }

            SaveCampaigns();
        }

        private bool CampaignNameExists(string campaignName, Campaign? campaignToIgnore = null)
        {
            foreach (Campaign campaign in _campaigns)
            {
                if (campaign == campaignToIgnore)
                {
                    continue;
                }

                if (string.Equals(campaign.Name, campaignName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}