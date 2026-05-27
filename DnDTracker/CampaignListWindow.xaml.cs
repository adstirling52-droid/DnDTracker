using DnDTracker.Models;
using DnDTracker.Services;
using System;
using System.Collections.Generic;
using System.Windows;


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
                selectedCampaign.Name = editCampaignWindow.CampaignName;
                RefreshCampaignList();
                CampaignListBox.SelectedItem = selectedCampaign;
                SaveCampaigns();
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
    }
}