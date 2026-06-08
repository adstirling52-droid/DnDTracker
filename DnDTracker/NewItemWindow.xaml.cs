using DnDTracker.Models;
using DnDTracker.Services;
using System.Windows;
using System.Collections.Generic;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace DnDTracker
{
    public partial class NewItemWindow : Window
    {
        public Item NewItem { get; private set; } = new Item();
        private string _selectedImagePath = "";
        private CampaignDataService _campaignDataService = new CampaignDataService();

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

            _selectedImagePath = existingItem.ImagePath;

            if (!string.IsNullOrWhiteSpace(_selectedImagePath) && File.Exists(_selectedImagePath))
            {
                ItemPreviewImage.Source = new BitmapImage(new Uri(_selectedImagePath, UriKind.Absolute));
            }
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
                ImagePath = _selectedImagePath,
                ProvenanceEntries = new System.Collections.Generic.List<ProvenanceEntry>
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

        private void ChooseImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Choose Item Image";
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string copiedImagePath = _campaignDataService.CopyItemImageToAppFolder(openFileDialog.FileName);

                if (!string.IsNullOrWhiteSpace(copiedImagePath) && File.Exists(copiedImagePath))
                {
                    _selectedImagePath = copiedImagePath;
                    ItemPreviewImage.Source = new BitmapImage(new Uri(_selectedImagePath, UriKind.Absolute));
                }
            }
        }

        private void ClearImageButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedImagePath = "";
            ItemPreviewImage.Source = null;
        }
    }
}
