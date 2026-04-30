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

        private void SaveButton_Click(Object sender, RoutedEventArgs e)
        {
            CampaignName = CampaignNameTextBox.Text.Trim();
            DialogResult = true;


        }
    }
}
