using DnDTracker.Models;
using System.Windows;

namespace DnDTracker
{
    public partial class NewProvenanceEntryWindow : Window
    {
        public ProvenanceEntry NewProvenanceEntry { get; private set; } = new ProvenanceEntry();

        public NewProvenanceEntryWindow()
        {
            InitializeComponent();

            Title = "New Provenance Entry";
            WindowHeadingTextBlock.Text = "Add Provenance Entry";
        }

        public NewProvenanceEntryWindow(ProvenanceEntry existingEntry)
        {
            InitializeComponent();

            Title = "Edit Provenance Entry";
            WindowHeadingTextBlock.Text = "Edit Provenance Entry";

            WhatTextBox.Text = existingEntry.What;
            WhereTextBox.Text = existingEntry.Where;
            WhenTextBox.Text = existingEntry.When;
            NotesTextBox.Text = existingEntry.Notes;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string whatText = WhatTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(whatText))
            {
                MessageBox.Show("Please enter what happened.", "Missing What", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewProvenanceEntry = new ProvenanceEntry
            {
                What = whatText,
                Where = WhereTextBox.Text.Trim(),
                When = WhenTextBox.Text.Trim(),
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