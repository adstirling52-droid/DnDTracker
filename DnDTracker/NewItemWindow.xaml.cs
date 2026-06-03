using DnDTracker.Models;
using System.Windows;
using System.Collections.Generic;

namespace DnDTracker
{
    public partial class NewItemWindow : Window
    {
        public Item NewItem { get; private set; } = new Item();

        public NewItemWindow()
        {
            InitializeComponent();

            Title = "New Item";
            WindowHeadingTextBlock.Text = "Create New Item";
        }

        public NewItemWindow(Item existingItem)
        {
            InitializeComponent();

            Title = "Edit Item";
            WindowHeadingTextBlock.Text = "Edit Item";

            ItemNameTextBox.Text = existingItem.Name;
            DescriptionTextBox.Text = existingItem.Description;
            WhereFoundTextBox.Text = existingItem.WhereFound;
            WhenFoundTextBox.Text = existingItem.WhenFound;
            CurrentStatusTextBox.Text = existingItem.CurrentStatus;
            NotesTextBox.Text = existingItem.Notes;
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
                Notes = NotesTextBox.Text.Trim(),
                ProvenanceEntries = new List<ProvenanceEntry>
                {
                    new ProvenanceEntry
                    {
                        What = "Found",
                        Where = WhereFoundTextBox.Text.Trim(),
                        When = WhenFoundTextBox.Text.Trim(),
                        Notes = NotesTextBox.Text.Trim()
                    }
                }
            };

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
