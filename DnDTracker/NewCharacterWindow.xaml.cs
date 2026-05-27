using DnDTracker.Models;
using System.Windows;

namespace DnDTracker
{
    public partial class NewCharacterWindow : Window
    {
        public string CharacterName { get; private set; } = "";

        public NewCharacterWindow()
        {
            InitializeComponent();

            Title = "New Character";
            WindowHeadingTextBlock.Text = "Create New Character";
        }

        public NewCharacterWindow(Character existingCharacter)
        {
            InitializeComponent();

            Title = "Edit Character";
            WindowHeadingTextBlock.Text = "Edit Character";

            CharacterNameTextBox.Text = existingCharacter.Name;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredName = CharacterNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(enteredName))
            {
                MessageBox.Show("Please enter a character name.", "Missing Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CharacterName = enteredName;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
