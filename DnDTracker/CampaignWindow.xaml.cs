using DnDTracker.Models;
using DnDTracker.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;
using System.IO;
using System.Windows.Input;



namespace DnDTracker
{
    public partial class CampaignWindow : Window
    {

        private Campaign _campaign;
        private Character? _selectedCharacter;
        private Button? _selectedCharacterButton;
        private List<Campaign> _allCampaigns;
        private CampaignDataService _campaignDataService;

        public CampaignWindow(Campaign campaign, List<Campaign> allCampaigns, CampaignDataService campaignDataService)
        {
            InitializeComponent();

            _campaign = campaign;
            _allCampaigns = allCampaigns;
            _campaignDataService = campaignDataService;

            CampaignNameTextBlock.Text = _campaign.Name;
            Title = _campaign.Name;

            LoadCharacterButtons();

            if (CharacterButtonPanel.Children.Count > 0)
            {
                Button firstButton = (Button)CharacterButtonPanel.Children[0];
                Character firstCharacter = (Character)firstButton.Tag;

                SelectCharacter(firstCharacter, firstButton);
            }

            UpdateCampaignWindowButtonStates();
        }

        private void UpdateCampaignWindowButtonStates()
        {
            bool characterSelected = _selectedCharacter != null;
            bool itemSelected = CharacterItemsListBox.SelectedItem != null;

            EditCharacterButton.IsEnabled = characterSelected;
            RemoveCharacterButton.IsEnabled = characterSelected;
            AddItemButton.IsEnabled = characterSelected;

            EditItemButton.IsEnabled = characterSelected && itemSelected;
            RemoveItemButton.IsEnabled = characterSelected && itemSelected;
        }

        private void LoadCharacterButtons()
        {
            CharacterButtonPanel.Children.Clear();
            _selectedCharacterButton = null;

            foreach (Character character in _campaign.Characters)
            {
                AddCharacterButton(character);
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
                                Notes = "Found beneath the old watchtower. Possibly linked to a forgotten noble line.",
                                ProvenanceEntries = new List<ProvenanceEntry>
                                {
                                    new ProvenanceEntry
                                    {
                                        What = "Found",
                                        Where = "Goblin Shrine",
                                        When = "Session 4",
                                        Notes = "Found beneath the old watchtower. Possibly linked to a forgotten noble line."
                                    }
                                }
                            },
                            new Item
                            {
                                Name = "Shield of the Stag",
                                Description = "A heavy round shield marked with an antlered crest.",
                                WhereFound = "Hunter's Barrow",
                                WhenFound = "Session 5",
                                CurrentStatus = "Carried by Oskar",
                                Notes = "Recovered from a barrow guardian. Still bears deep claw marks.",
                                ProvenanceEntries = new List<ProvenanceEntry>
                                {
                                    new ProvenanceEntry
                                    {
                                        What = "Found",
                                        Where = "Hunter's Barrow",
                                        When = "Session 5",
                                        Notes = "Recovered from a barrow guardian. Still bears deep claw marks."
                                    }
                                }
                            },
                            new Item
                            {
                                Name = "Potion of Healing",
                                Description = "A small glass vial filled with bright red liquid.",
                                WhereFound = "Starter Supplies",
                                WhenFound = "Session 1",
                                CurrentStatus = "In party gear",
                                Notes = "Common but valuable enough that everyone keeps forgetting who last had it.",
                                ProvenanceEntries = new List<ProvenanceEntry>
                                {
                                    new ProvenanceEntry
                                    {
                                        What = "Found",
                                        Where = "Starter Supplies",
                                        When = "Session 1",
                                        Notes = "Common but valuable enough that everyone keeps forgetting who last had it.."
                                    }
                                }
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

            SelectCharacter(selectedCharacter, clickedButton);
        }

        private void SelectCharacter(Character character, Button selectedButton)
        {
            _selectedCharacter = character;

            if (_selectedCharacterButton != null)
            {
                _selectedCharacterButton.ClearValue(Button.BackgroundProperty);
                _selectedCharacterButton.ClearValue(Button.BorderBrushProperty);
            }

            _selectedCharacterButton = selectedButton;
            _selectedCharacterButton.Background = Brushes.LightBlue;
            _selectedCharacterButton.BorderBrush = Brushes.SteelBlue;

            LoadItemsForCharacter(character);
            UpdateCampaignWindowButtonStates();
        }
        
        private void LoadItemsForCharacter(Character character)
        {
            CharacterItemsListBox.Items.Clear();
            ClearItemDetails();

            foreach (Item item in character.Items)
            {
                CharacterItemsListBox.Items.Add(item);
            }

            if (character.Items.Count > 0)
            {
                CharacterItemsListBox.SelectedIndex = 0;
            }
        }

        private bool CharacterNameExists(string characterName, Character? characterToIgnore = null)
        {
            foreach (Character character in _campaign.Characters)
            {
                if (character == characterToIgnore)
                {
                    continue;
                }

                if (string.Equals(character.Name, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearItemDetails()
        {
            SelectedItemNameTextBlock.Text = "Select an item";
            SelectedItemDescriptionTextBlock.Text = "Item description will appear here.";
            SelectedItemStatusTextBlock.Text = "Current Status:";
            LatestItemNotesTextBlock.Text = "Latest item note will appear here.";
            SelectedItemImage.Source = null;
            ProvenanceHistoryItemsControl.ItemsSource = null;
        }

        private void CharacterItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CharacterItemsListBox.SelectedItem == null)
            {
                ClearItemDetails();
                UpdateCampaignWindowButtonStates();
                return;
            }

            Item selectedItem = (Item)CharacterItemsListBox.SelectedItem;
            LoadProvenanceForItem(selectedItem);
            UpdateCampaignWindowButtonStates();
        }

        private void AddCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            NewCharacterWindow newCharacterWindow = new NewCharacterWindow();
            newCharacterWindow.Owner = this;

            bool? result = newCharacterWindow.ShowDialog();

            if (result == true)
            {
                if (CharacterNameExists(newCharacterWindow.CharacterName))
                {
                    MessageBox.Show("A character with that name already exists in this campaign.", "Duplicate Character Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Character newCharacter = new Character
                {
                    Name = newCharacterWindow.CharacterName
                };

                _campaign.Characters.Add(newCharacter);
                LoadCharacterButtons();

                Button newCharacterButton = (Button)CharacterButtonPanel.Children[CharacterButtonPanel.Children.Count - 1];
                SelectCharacter(newCharacter, newCharacterButton);

                SaveCampaigns();
            }
        }

        private void EditCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NewCharacterWindow editCharacterWindow = new NewCharacterWindow(_selectedCharacter);
            editCharacterWindow.Owner = this;

            bool? result = editCharacterWindow.ShowDialog();

            if (result == true)
            {
                if (CharacterNameExists(editCharacterWindow.CharacterName, _selectedCharacter))
                {
                    MessageBox.Show("A character with that name already exists in this campaign.", "Duplicate Character Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedCharacter.Name = editCharacterWindow.CharacterName;

                LoadCharacterButtons();

                foreach (Button button in CharacterButtonPanel.Children)
                {
                    Character character = (Character)button.Tag;

                    if (character == _selectedCharacter)
                    {
                        SelectCharacter(_selectedCharacter, button);
                        break;
                    }
                }

                SaveCampaigns();
            }
        }
        
        private void RemoveCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Remove character '{_selectedCharacter.Name}'?",
                "Confirm Remove Character",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _campaign.Characters.Remove(_selectedCharacter);

            LoadCharacterButtons();

            if (_campaign.Characters.Any() && CharacterButtonPanel.Children.Count > 0)
            {
                Button firstButton = (Button)CharacterButtonPanel.Children[0];
                Character firstCharacter = (Character)firstButton.Tag;

                SelectCharacter(firstCharacter, firstButton);
            }
            else
            {
                _selectedCharacter = null;
                _selectedCharacterButton = null;
                CharacterItemsListBox.Items.Clear();
                ClearItemDetails();
                UpdateCampaignWindowButtonStates();
            }

            SaveCampaigns();
        }

        private bool ItemNameExists(string itemName, Item? itemToIgnore = null)
        {
            if (_selectedCharacter == null)
            {
                return false;
            }

            foreach (Item item in _selectedCharacter.Items)
            {
                if (item == itemToIgnore)
                {
                    continue;
                }

                if (string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
                if (ItemNameExists(newItemWindow.NewItem.Name))
                {
                    MessageBox.Show("An item with that name already exists for this character.", "Duplicate Item Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedCharacter.Items.Add(newItemWindow.NewItem);
                LoadItemsForCharacter(_selectedCharacter);
                CharacterItemsListBox.SelectedItem = newItemWindow.NewItem;
                SaveCampaigns();
            }
        }
        
        private void EditItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (CharacterItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an item to edit.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item selectedItem = (Item)CharacterItemsListBox.SelectedItem;

            NewItemWindow editItemWindow = new NewItemWindow(selectedItem);
            editItemWindow.Owner = this;

            bool? result = editItemWindow.ShowDialog();

            if (result == true)
            {
                if (ItemNameExists(editItemWindow.NewItem.Name, selectedItem))
                {
                    MessageBox.Show("An item with that name already exists for this character.", "Duplicate Item Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int itemIndex = _selectedCharacter.Items.IndexOf(selectedItem);

                if (itemIndex >= 0)
                {
                    editItemWindow.NewItem.ProvenanceEntries = new List<ProvenanceEntry>(selectedItem.ProvenanceEntries);

                    if (ItemProvenanceFieldsChanged(selectedItem, editItemWindow.NewItem))
                    {
                        ProvenanceEntry updatedEntry = BuildUpdatedProvenanceEntry(selectedItem, editItemWindow.NewItem);
                        editItemWindow.NewItem.ProvenanceEntries.Add(updatedEntry);
                    }

                    _selectedCharacter.Items[itemIndex] = editItemWindow.NewItem;

                    LoadItemsForCharacter(_selectedCharacter);
                    CharacterItemsListBox.SelectedItem = editItemWindow.NewItem;
                    SaveCampaigns();
                }
            }
        }
        
        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (CharacterItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an item to remove.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item selectedItem = (Item)CharacterItemsListBox.SelectedItem;

            MessageBoxResult result = MessageBox.Show(
                $"Remove '{selectedItem.Name}' from {_selectedCharacter.Name}?",
                "Confirm Remove Item",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _selectedCharacter.Items.Remove(selectedItem);
            LoadItemsForCharacter(_selectedCharacter);
            SaveCampaigns();
        }

        private void LoadProvenanceForItem(Item item)
        {
            SelectedItemNameTextBlock.Text = item.Name;
            SelectedItemDescriptionTextBlock.Text = item.Description;
            SelectedItemStatusTextBlock.Text = $"Current Status: {item.CurrentStatus}";
            LatestItemNotesTextBlock.Text = item.Notes;

            if (!string.IsNullOrWhiteSpace(item.ImagePath) && File.Exists(item.ImagePath))
            {
                SelectedItemImage.Source = new BitmapImage(new Uri(item.ImagePath, UriKind.Absolute));
            }
            else
            {
                SelectedItemImage.Source = null;
            }

            if (item.ProvenanceEntries != null && item.ProvenanceEntries.Count > 0)
            {
                List<ProvenanceEntry> reversedEntries = new List<ProvenanceEntry>(item.ProvenanceEntries);
                reversedEntries.Reverse();

                ProvenanceHistoryItemsControl.ItemsSource = reversedEntries;
            }
            else
            {
                ProvenanceHistoryItemsControl.ItemsSource = new List<ProvenanceEntry>
        {
            new ProvenanceEntry
            {
                What = "Legacy Entry",
                Where = item.WhereFound,
                When = item.WhenFound,
                Notes = item.Notes
            }
        };
            }
        }

        private bool ItemProvenanceFieldsChanged(Item originalItem, Item editedItem)
        {
            return !string.Equals(originalItem.Description, editedItem.Description, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(originalItem.CurrentStatus, editedItem.CurrentStatus, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(originalItem.Notes, editedItem.Notes, StringComparison.OrdinalIgnoreCase);
        }

        private ProvenanceEntry BuildUpdatedProvenanceEntry(Item originalItem, Item editedItem)
        {
            List<string> changeNotes = new List<string>();

            bool descriptionChanged = !string.Equals(originalItem.Description, editedItem.Description, StringComparison.OrdinalIgnoreCase);
            bool statusChanged = !string.Equals(originalItem.CurrentStatus, editedItem.CurrentStatus, StringComparison.OrdinalIgnoreCase);
            bool notesChanged = !string.Equals(originalItem.Notes, editedItem.Notes, StringComparison.OrdinalIgnoreCase);

            if (descriptionChanged)
            {
                changeNotes.Add($"Description changed from '{originalItem.Description}' to '{editedItem.Description}'.");
            }

            if (statusChanged)
            {
                changeNotes.Add($"Current Status changed from '{originalItem.CurrentStatus}' to '{editedItem.CurrentStatus}'.");
            }

            if (notesChanged)
            {
                changeNotes.Add($"Notes changed from '{originalItem.Notes}' to '{editedItem.Notes}'.");
            }

            string what;

            int changeCount = 0;
            if (descriptionChanged) changeCount++;
            if (statusChanged) changeCount++;
            if (notesChanged) changeCount++;

            if (changeCount == 1)
            {
                if (descriptionChanged)
                {
                    what = "Description Updated";
                }
                else if (statusChanged)
                {
                    what = "Status Updated";
                }
                else
                {
                    what = "Notes Updated";
                }
            }
            else
            {
                what = "Item Updated";
            }

            return new ProvenanceEntry
            {
                What = what,
                Where = originalItem.WhereFound,
                When = originalItem.WhenFound,
                Notes = string.Join(" ", changeNotes)
            };
        }

        private void SelectedItemImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
            {
                return;
            }

            if (CharacterItemsListBox.SelectedItem == null)
            {
                return;
            }

            Item selectedItem = (Item)CharacterItemsListBox.SelectedItem;

            if (string.IsNullOrWhiteSpace(selectedItem.ImagePath))
            {
                return;
            }

            ItemImageWindow itemImageWindow = new ItemImageWindow(selectedItem.ImagePath, selectedItem.Name);
            itemImageWindow.Owner = this;
            itemImageWindow.ShowDialog();
        }

        private void CloseCampaignButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveCampaigns()
        {
            _campaignDataService.SaveCampaigns(_allCampaigns);
        }
    }


}
