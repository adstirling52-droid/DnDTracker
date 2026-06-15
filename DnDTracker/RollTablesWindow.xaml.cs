using System.Windows;

namespace DnDTracker
{
    public partial class RollTablesWindow : Window
    {
        public RollTablesWindow()
        {
            InitializeComponent();
        }

        private void ImportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("CSV import will be implemented next.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
