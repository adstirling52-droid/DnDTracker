using DnDTracker.Models;
using System.Windows;

namespace DnDTracker
{
    public partial class NewItemWindow : Window
    {
        public Item NewItem { get; private set; } = new Item();

        public NewItemWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string itemName = ItemNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(itemName))
            {
                MessageBox.Show("Please enter an item name.", "Missing Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewItem = new Item
            {
                Name = itemName,
                Description = DescriptionTextBox.Text.Trim(),
                WhereFound = WhereFoundTextBox.Text.Trim(),
                WhenFound = WhenFoundTextBox.Text.Trim(),
                CurrentStatus = CurrentStatusTextBox.Text.Trim(),
                Notes = NotesTextBox.Text.Trim()
            };

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
