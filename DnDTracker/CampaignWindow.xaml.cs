using DnDTracker.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DnDTracker
{
    public partial class CampaignWindow : Window
    {

        private Campaign _campaign;

        private Character? _selectedCharacter;

        public CampaignWindow(Campaign campaign)
{
            InitializeComponent();

            _campaign = campaign;

            CampaignNameTextBlock.Text = _campaign.Name;
            Title = _campaign.Name;

            LoadCharacterButtons();

            if (_campaign.Characters.Any())
            {
                _selectedCharacter = _campaign.Characters.First();
                LoadItemsForCharacter(_selectedCharacter);
            }
        }

        public static Campaign CreateSampleCampaign(string campaignName)
        {
            return new Campaign
            {
                Name = campaignName,
                Characters = new List<Character>
                {
                    new Character
                    {
                        Name = "Oskar",
                        Items = new List<Item>
                        {
                            new Item
                            {
                                Name = "Moon-Touched Sword",
                                Description = "A pale silver blade that glows faintly in moonlight.",
                                WhereFound = "Goblin Shrine",
                                WhenFound = "Session 4",
                                CurrentStatus = "Carried by Oskar",
                                Notes = "Found beneath the old watchtower. Possibly linked to a forgotten noble line."
                            },
                            new Item
                            {
                                Name = "Shield of the Stag",
                                Description = "A heavy round shield marked with an antlered crest.",
                                WhereFound = "Hunter's Barrow",
                                WhenFound = "Session 5",
                                CurrentStatus = "Carried by Oskar",
                                Notes = "Recovered from a barrow guardian. Still bears deep claw marks."
                            },
                            new Item
                            {
                                Name = "Potion of Healing",
                                Description = "A small glass vial filled with bright red liquid.",
                                WhereFound = "Starter Supplies",
                                WhenFound = "Session 1",
                                CurrentStatus = "In party gear",
                                Notes = "Common but valuable enough that everyone keeps forgetting who last had it."
                            }
                        }
                    },
                    new Character
                    {
                        Name = "Lanvor",
                        Items = new List<Item>
                        {
                            new Item
                            {
                                Name = "Crystal Focus",
                                Description = "A narrow faceted crystal mounted in a bronze grip.",
                                WhereFound = "Wizard's Tower",
                                WhenFound = "Session 3",
                                CurrentStatus = "Carried by Lanvor",
                                Notes = "Seems to strengthen fire magic slightly, though not yet proven."
                            },
                            new Item
                            {
                                Name = "Scroll of Burning Hands",
                                Description = "A brittle scroll written in heat-darkened ink.",
                                WhereFound = "Ash Chest",
                                WhenFound = "Session 6",
                                CurrentStatus = "Carried by Lanvor",
                                Notes = "Single use. Smells faintly of smoke even when sealed."
                            },
                            new Item
                            {
                                Name = "Silver Ring",
                                Description = "A plain silver ring with an inscription worn almost smooth.",
                                WhereFound = "Fishers Rest",
                                WhenFound = "Session 8",
                                CurrentStatus = "Carried by Lanvor",
                                Notes = "Could be sentimental rather than magical, though Lanvor suspects otherwise."
                            }
                        }
                    },
                    new Character
                    {
                        Name = "Thradal",
                        Items = new List<Item>
                        {
                            new Item
                            {
                                Name = "Bone Wand",
                                Description = "A narrow wand carved from polished bone.",
                                WhereFound = "Crypt of Whispers",
                                WhenFound = "Session 7",
                                CurrentStatus = "Carried by Thradal",
                                Notes = "Unpleasantly warm to the touch at odd moments."
                            },
                            new Item
                            {
                                Name = "Black Journal",
                                Description = "A leather journal filled with cramped notes and strange diagrams.",
                                WhereFound = "Dead Scholar's Room",
                                WhenFound = "Session 2",
                                CurrentStatus = "Carried by Thradal",
                                Notes = "Useful for clues, but some pages seem to rewrite themselves."
                            },
                            new Item
                            {
                                Name = "Dust of Disappearance",
                                Description = "A small packet of glittering grey dust.",
                                WhereFound = "Hidden Cache",
                                WhenFound = "Session 9",
                                CurrentStatus = "Carried by Thradal",
                                Notes = "Thradal claims he is saving it for the perfect moment, which worries everyone."
                            }
                        }
                    }
                }
            };
        }

        private void LoadCharacterButtons()
        {
            CharacterButtonPanel.Children.Clear();

            foreach (Character character in _campaign.Characters)
            {
                AddCharacterButton(character);
            }
        }

        private void AddCharacterButton(Character character)
        {
            Button characterButton = new Button();
            characterButton.Content = character.Name;
            characterButton.Tag = character;
            characterButton.Margin = new Thickness(0, 0, 0, 8);
            characterButton.Click += CharacterButton_Click;

            CharacterButtonPanel.Children.Add(characterButton);
        }

        private void CharacterButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            Character selectedCharacter = (Character)clickedButton.Tag;

            _selectedCharacter = selectedCharacter;
            LoadItemsForCharacter(selectedCharacter);
        }

        private void LoadItemsForCharacter(Character character)
        {
            CharacterItemsListBox.Items.Clear();
            ClearItemDetails();

            foreach (Item item in character.Items)
            {
                CharacterItemsListBox.Items.Add(item);
            }
        }

        private void ClearItemDetails()
        {
            SelectedItemNameTextBlock.Text = "Select an item";
            SelectedItemDescriptionTextBlock.Text = "Item description will appear here.";
            SelectedItemWhereTextBlock.Text = "Where:";
            SelectedItemWhenTextBlock.Text = "When:";
            SelectedItemStatusTextBlock.Text = "Current Status:";
            SelectedItemNotesTextBlock.Text = "Item notes will appear here.";
        }

        private void CharacterItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CharacterItemsListBox.SelectedItem == null)
            {
                return;
            }

            Item selectedItem = (Item)CharacterItemsListBox.SelectedItem;
            LoadProvenanceForItem(selectedItem);
        }

        private void LoadProvenanceForItem(Item item)
        {
            SelectedItemNameTextBlock.Text = item.Name;
            SelectedItemDescriptionTextBlock.Text = item.Description;
            SelectedItemWhereTextBlock.Text = $"Where: {item.WhereFound}";
            SelectedItemWhenTextBlock.Text = $"When: {item.WhenFound}";
            SelectedItemStatusTextBlock.Text = $"Current Status: {item.CurrentStatus}";
            SelectedItemNotesTextBlock.Text = item.Notes;
        }

        private void AddCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            NewCharacterWindow newCharacterWindow = new NewCharacterWindow();
            newCharacterWindow.Owner = this;

            bool? result = newCharacterWindow.ShowDialog();

            if (result == true)
            {
                Character newCharacter = new Character
                {
                    Name = newCharacterWindow.CharacterName
                };

                _campaign.Characters.Add(newCharacter);
                _selectedCharacter = newCharacter;
                LoadCharacterButtons();
                LoadItemsForCharacter(newCharacter);
            }
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NewItemWindow newItemWindow = new NewItemWindow();
            newItemWindow.Owner = this;

            bool? result = newItemWindow.ShowDialog();

            if (result == true)
            {
                _selectedCharacter.Items.Add(newItemWindow.NewItem);
                LoadItemsForCharacter(_selectedCharacter);

                CharacterItemsListBox.SelectedItem = newItemWindow.NewItem;
               // LoadProvenanceForItem(newItemWindow.NewItem);
            }
        }
        private void CloseCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
