using System.Windows;
using System.Windows.Controls;

namespace DnDTracker
{
    public partial class SelectTableTypeWindow : Window
    {
        public string SelectedTableType { get; private set; } = "Generic";

        public SelectTableTypeWindow()
        {
            InitializeComponent();

            TableTypeComboBox.SelectedIndex = 0;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (TableTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                SelectedTableType = selectedItem.Content.ToString() ?? "Generic";
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
