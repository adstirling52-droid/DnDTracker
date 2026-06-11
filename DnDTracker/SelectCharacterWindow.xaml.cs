using DnDTracker.Models;
using System.Collections.Generic;
using System.Windows;

namespace DnDTracker
{
    public partial class SelectCharacterWindow : Window
    {
        public Character? SelectedCharacter { get; private set; }

        public SelectCharacterWindow(List<Character> characters)
        {
            InitializeComponent();

            foreach (Character character in characters)
            {
                CharacterSelectionListBox.Items.Add(character);
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (CharacterSelectionListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a character.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedCharacter = (Character)CharacterSelectionListBox.SelectedItem;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}