using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DnDTracker
{
    /// <summary>
    /// Interaction logic for NewCampaignWindow.xaml
    /// </summary>
    public partial class NewCampaignWindow : Window
    {

        public string CampaignName { get; private set; } = "";


        public NewCampaignWindow()
        {
            InitializeComponent();
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();

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
    }
}
