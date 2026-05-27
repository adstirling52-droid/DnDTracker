using System.Text;
using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DnDTracker.Models;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Generic;


namespace DnDTracker
{
    public partial class CampaignListWindow : Window
    {

        private List<Campaign> _campaigns = new List<Campaign>();

        public CampaignListWindow()
        {
            InitializeComponent();

            LoadSampleCampaigns();

            
        }

        private void LoadSampleCampaigns()
        {
            _campaigns.Add(CampaignWindow.CreateSampleCampaign("Errae"));
            _campaigns.Add(new Campaign { Name = "Lake Mizkagi" });
            _campaigns.Add(new Campaign { Name = "Keep on the Borderlands" });

            RefreshCampaignList();
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

            CampaignWindow campaignWindow = new CampaignWindow(selectedCampaign);
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
        }
    }
}