using DnDTracker.Models;
using System.Windows;

namespace DnDTracker
{
    public partial class NewCampaignWindow : Window
    {
        public string CampaignName { get; private set; } = "";

        public NewCampaignWindow()
        {
            InitializeComponent();

            Title = "New Campaign";
            WindowHeadingTextBlock.Text = "Create New Campaign";
        }

        public NewCampaignWindow(Campaign existingCampaign)
        {
            InitializeComponent();

            Title = "Edit Campaign";
            WindowHeadingTextBlock.Text = "Edit Campaign";

            CampaignNameTextBox.Text = existingCampaign.Name;
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredName = CampaignNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(enteredName))
            {
                MessageBox.Show("Please enter a campaign name.", "Missing Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CampaignName = enteredName;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
