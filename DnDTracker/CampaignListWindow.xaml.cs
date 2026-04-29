using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DnDTracker
{
    public partial class CampaignListWindow : Window
    {
        public CampaignListWindow()
        {
            InitializeComponent();

            LoadSampleCampaigns();
        }

        private void LoadSampleCampaigns()
        {
            CampaignListBox.Items.Add("Errae");
            CampaignListBox.Items.Add("Lake Mizkagi");
            CampaignListBox.Items.Add("Keep on the Borderlands");
        }


        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    

    }
}