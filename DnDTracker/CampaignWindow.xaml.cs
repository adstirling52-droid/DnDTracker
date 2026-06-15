using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DnDTracker.Models;
using DnDTracker.Services;




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
            LoadUnassignedItems();

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
            bool characterItemSelected = CharacterItemsListBox.SelectedItem != null;
            bool unassignedItemSelected = UnassignedItemsListBox.SelectedItem != null;
            bool anyItemSelected = characterItemSelected || unassignedItemSelected;
            bool provenanceSelected = ProvenanceHistoryListBox.SelectedItem != null;
            bool skillSelected = CharacterSkillsListBox.SelectedItem != null;

            EditCharacterButton.IsEnabled = characterSelected;
            RemoveCharacterButton.IsEnabled = characterSelected;
            AddItemButton.IsEnabled = characterSelected;

            EditItemButton.IsEnabled = characterSelected && characterItemSelected;
            RemoveItemButton.IsEnabled = characterSelected && characterItemSelected;
            UnassignItemButton.IsEnabled = characterSelected && characterItemSelected;
            AddProvenanceButton.IsEnabled = anyItemSelected;

            EditProvenanceButton.IsEnabled = anyItemSelected && provenanceSelected;
            DeleteProvenanceButton.IsEnabled = anyItemSelected && provenanceSelected;

            EditUnassignedItemButton.IsEnabled = unassignedItemSelected;
            RemoveUnassignedItemButton.IsEnabled = unassignedItemSelected;
            AssignUnassignedItemButton.IsEnabled = characterSelected && unassignedItemSelected;

            AddSkillButton.IsEnabled = characterSelected;
            EditSkillButton.IsEnabled = characterSelected && skillSelected;
            RemoveSkillButton.IsEnabled = characterSelected && skillSelected;
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
            _selectedCharacterButton.Background = System.Windows.Media.Brushes.LightBlue;
            _selectedCharacterButton.BorderBrush = System.Windows.Media.Brushes.SteelBlue;

            LoadItemsForCharacter(character);
            LoadSkillsForCharacter(character);

            if (UnassignedTab.IsSelected)
            {
                SkillsTab.IsSelected = true;
            }

            RefreshSelectedTabItemDetails();
            UpdateCampaignWindowButtonStates();
        }

        private void LoadItemsForCharacter(Character character)
        {
            CharacterItemsListBox.Items.Clear();

            foreach (Item item in character.Items)
            {
                CharacterItemsListBox.Items.Add(item);
            }

            if (ItemsTab.IsSelected)
            {
                if (character.Items.Count > 0)
                {
                    CharacterItemsListBox.SelectedIndex = 0;
                }
                else
                {
                    ClearItemDetails();
                }
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
            DetailsPanelTitleTextBlock.Text = "Item Provenance";

            SkillDetailsPanel.Visibility = Visibility.Collapsed;
            ItemDetailsPanel.Visibility = Visibility.Visible;

            SelectedItemNameTextBlock.Text = "Select an item";
            SelectedItemDescriptionTextBlock.Text = "Item description will appear here.";
            SelectedItemStatusTextBlock.Text = "Current Status:";
            LatestItemNotesTextBlock.Text = "Latest item note will appear here.";
            SelectedItemImage.Source = null;
            NoImagePlaceholderTextBlock.Visibility = Visibility.Visible;
            ProvenanceHistoryListBox.ItemsSource = null;
        }

        private void CharacterItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CharacterItemsListBox.SelectedItem == null)
            {
                if (ItemsTab.IsSelected)
                {
                    ClearItemDetails();
                }

                UpdateCampaignWindowButtonStates();
                return;
            }

            UnassignedItemsListBox.SelectedItem = null;
            CharacterSkillsListBox.SelectedItem = null;

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
            DetailsPanelTitleTextBlock.Text = "Item Provenance";
            SkillDetailsPanel.Visibility = Visibility.Collapsed;
            ItemDetailsPanel.Visibility = Visibility.Visible;

            SelectedItemNameTextBlock.Text = item.Name;
            SelectedItemDescriptionTextBlock.Text = item.Description;
            SelectedItemStatusTextBlock.Text = $"Current Status: {item.CurrentStatus}";
            LatestItemNotesTextBlock.Text = item.Notes;

            if (!string.IsNullOrWhiteSpace(item.ImagePath) && File.Exists(item.ImagePath))
            {
                SelectedItemImage.Source = new BitmapImage(new Uri(item.ImagePath, UriKind.Absolute));
                NoImagePlaceholderTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                SelectedItemImage.Source = null;
                NoImagePlaceholderTextBlock.Visibility = Visibility.Visible;
            }

            if (item.ProvenanceEntries != null && item.ProvenanceEntries.Count > 0)
            {
                List<ProvenanceEntry> reversedEntries = new List<ProvenanceEntry>(item.ProvenanceEntries);
                reversedEntries.Reverse();

                ProvenanceHistoryListBox.ItemsSource = reversedEntries;
            }
            else
            {
                ProvenanceHistoryListBox.ItemsSource = new List<ProvenanceEntry>
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

        private void AddProvenanceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (CharacterItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an item first.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item selectedItem = (Item)CharacterItemsListBox.SelectedItem;

            NewProvenanceEntryWindow provenanceWindow = new NewProvenanceEntryWindow();
            provenanceWindow.Owner = this;

            bool? result = provenanceWindow.ShowDialog();

            if (result == true)
            {
                selectedItem.ProvenanceEntries.Add(provenanceWindow.NewProvenanceEntry);

                LoadProvenanceForItem(selectedItem);
                SaveCampaigns();
            }
        }

        private void ProvenanceHistoryListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateCampaignWindowButtonStates();
        }

        private ProvenanceEntry? GetSelectedProvenanceEntry()
        {
            if (ProvenanceHistoryListBox.SelectedItem == null)
            {
                return null;
            }

            return (ProvenanceEntry)ProvenanceHistoryListBox.SelectedItem;
        }

        private void EditProvenanceButton_Click(object sender, RoutedEventArgs e)
        {
            if (CharacterItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an item first.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item selectedItem = (Item)CharacterItemsListBox.SelectedItem;
            ProvenanceEntry? selectedEntry = GetSelectedProvenanceEntry();

            if (selectedEntry == null)
            {
                MessageBox.Show("Please select a provenance entry first.", "No Provenance Entry Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NewProvenanceEntryWindow editWindow = new NewProvenanceEntryWindow(selectedEntry);
            editWindow.Owner = this;

            bool? result = editWindow.ShowDialog();

            if (result == true)
            {
                int entryIndex = selectedItem.ProvenanceEntries.IndexOf(selectedEntry);

                if (entryIndex >= 0)
                {
                    selectedItem.ProvenanceEntries[entryIndex] = editWindow.NewProvenanceEntry;

                    LoadProvenanceForItem(selectedItem);
                    SaveCampaigns();
                }
            }
        }

        private void DeleteProvenanceButton_Click(object sender, RoutedEventArgs e)
        {
            if (CharacterItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an item first.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item selectedItem = (Item)CharacterItemsListBox.SelectedItem;
            ProvenanceEntry? selectedEntry = GetSelectedProvenanceEntry();

            if (selectedEntry == null)
            {
                MessageBox.Show("Please select a provenance entry first.", "No Provenance Entry Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Delete provenance entry '{selectedEntry.What}'?",
                "Confirm Delete Provenance Entry",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            selectedItem.ProvenanceEntries.Remove(selectedEntry);

            LoadProvenanceForItem(selectedItem);
            SaveCampaigns();
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

        private void LoadUnassignedItems()
        {
            UnassignedItemsListBox.Items.Clear();

            foreach (Item item in _campaign.UnassignedItems)
            {
                UnassignedItemsListBox.Items.Add(item);
            }

            UnassignedItemsPlaceholderTextBlock.Visibility =
                _campaign.UnassignedItems.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
           
            if (UnassignedTab.IsSelected)
            {
                RefreshSelectedTabItemDetails();
            }
        }

        private void AddUnassignedItemButton_Click(object sender, RoutedEventArgs e)
        {
            NewItemWindow newItemWindow = new NewItemWindow();
            newItemWindow.Owner = this;

            bool? result = newItemWindow.ShowDialog();

            if (result == true)
            {
                if (_campaign.UnassignedItems.Any(item =>
                    string.Equals(item.Name, newItemWindow.NewItem.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("An unassigned item with that name already exists.", "Duplicate Item Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _campaign.UnassignedItems.Add(newItemWindow.NewItem);
                LoadUnassignedItems();
                SaveCampaigns();
            }
        }

        private void UnassignedItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnassignedItemsListBox.SelectedItem == null)
            {
                UpdateCampaignWindowButtonStates();
                return;
            }

            CharacterItemsListBox.SelectedItem = null;

            Item selectedItem = (Item)UnassignedItemsListBox.SelectedItem;
            LoadProvenanceForItem(selectedItem);
            UpdateCampaignWindowButtonStates();
        }

        private void EditUnassignedItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (UnassignedItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an unassigned item first.", "No Unassigned Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item selectedItem = (Item)UnassignedItemsListBox.SelectedItem;

            NewItemWindow editItemWindow = new NewItemWindow(selectedItem);
            editItemWindow.Owner = this;

            bool? result = editItemWindow.ShowDialog();

            if (result == true)
            {
                if (_campaign.UnassignedItems.Any(item =>
                    item != selectedItem &&
                    string.Equals(item.Name, editItemWindow.NewItem.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("An unassigned item with that name already exists.", "Duplicate Item Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int itemIndex = _campaign.UnassignedItems.IndexOf(selectedItem);

                if (itemIndex >= 0)
                {
                    editItemWindow.NewItem.ProvenanceEntries = new List<ProvenanceEntry>(selectedItem.ProvenanceEntries);

                    if (ItemProvenanceFieldsChanged(selectedItem, editItemWindow.NewItem))
                    {
                        ProvenanceEntry updatedEntry = BuildUpdatedProvenanceEntry(selectedItem, editItemWindow.NewItem);
                        editItemWindow.NewItem.ProvenanceEntries.Add(updatedEntry);
                    }

                    _campaign.UnassignedItems[itemIndex] = editItemWindow.NewItem;

                    LoadUnassignedItems();
                    UnassignedItemsListBox.SelectedItem = editItemWindow.NewItem;
                    SaveCampaigns();
                }
            }
        }

        private void RemoveUnassignedItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (UnassignedItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an unassigned item first.", "No Unassigned Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item selectedItem = (Item)UnassignedItemsListBox.SelectedItem;

            MessageBoxResult result = MessageBox.Show(
                $"Remove unassigned item '{selectedItem.Name}'?",
                "Confirm Remove Unassigned Item",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _campaign.UnassignedItems.Remove(selectedItem);
            LoadUnassignedItems();
            ClearItemDetails();
            SaveCampaigns();
            UpdateCampaignWindowButtonStates();
        }

        private void AssignUnassignedItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (UnassignedItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an unassigned item first.", "No Unassigned Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_campaign.Characters.Count == 0)
            {
                MessageBox.Show("There are no characters in this campaign.", "No Characters Available", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item selectedItem = (Item)UnassignedItemsListBox.SelectedItem;

            SelectCharacterWindow selectCharacterWindow = new SelectCharacterWindow(_campaign.Characters);
            selectCharacterWindow.Owner = this;

            bool? result = selectCharacterWindow.ShowDialog();

            if (result != true || selectCharacterWindow.SelectedCharacter == null)
            {
                return;
            }

            Character targetCharacter = selectCharacterWindow.SelectedCharacter;

            if (targetCharacter.Items.Any(item =>
                string.Equals(item.Name, selectedItem.Name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("That character already has an item with that name.", "Duplicate Item Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            selectedItem.CurrentStatus = $"Carried by {targetCharacter.Name}";
            selectedItem.Notes = $"Assigned to {targetCharacter.Name} from the unassigned pool.";

            selectedItem.ProvenanceEntries.Add(new ProvenanceEntry
            {
                What = "Assigned to Character",
                Where = selectedItem.WhereFound,
                When = selectedItem.WhenFound,
                Notes = $"Assigned to {targetCharacter.Name} from the unassigned pool."
            });

            _campaign.UnassignedItems.Remove(selectedItem);
            targetCharacter.Items.Add(selectedItem);

            LoadUnassignedItems();

            if (_selectedCharacter == targetCharacter)
            {
                LoadItemsForCharacter(targetCharacter);
                CharacterItemsListBox.SelectedItem = selectedItem;
            }

            SaveCampaigns();
            UpdateCampaignWindowButtonStates();
        }

        private void UnassignItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (CharacterItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a character item first.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item selectedItem = (Item)CharacterItemsListBox.SelectedItem;

            if (_campaign.UnassignedItems.Any(item =>
                string.Equals(item.Name, selectedItem.Name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("An unassigned item with that name already exists.", "Duplicate Item Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            selectedItem.CurrentStatus = "Unassigned";
            selectedItem.Notes = $"Moved to the unassigned item pool from {_selectedCharacter.Name}.";

            selectedItem.ProvenanceEntries.Add(new ProvenanceEntry
            {
                What = "Moved to Unassigned",
                Where = selectedItem.WhereFound,
                When = selectedItem.WhenFound,
                Notes = $"Moved to the unassigned item pool from {_selectedCharacter.Name}."
            });

            _selectedCharacter.Items.Remove(selectedItem);
            _campaign.UnassignedItems.Add(selectedItem);

            LoadItemsForCharacter(_selectedCharacter);
            LoadUnassignedItems();
            UnassignedItemsListBox.SelectedItem = selectedItem;

            SaveCampaigns();
            UpdateCampaignWindowButtonStates();
        }

        private void RefreshSelectedTabItemDetails()
        {
            if (ItemsTab.IsSelected)
            {
                if (CharacterItemsListBox.Items.Count > 0)
                {
                    CharacterItemsListBox.SelectedIndex = 0;
                }
                else
                {
                    ClearItemDetails();
                    UpdateCampaignWindowButtonStates();
                }
            }
            else if (UnassignedTab.IsSelected)
            {
                if (UnassignedItemsListBox.Items.Count > 0)
                {
                    UnassignedItemsListBox.SelectedIndex = 0;
                }
                else
                {
                    ClearItemDetails();
                    UpdateCampaignWindowButtonStates();
                }
            }
            else if (SkillsTab.IsSelected)
            {
                if (CharacterSkillsListBox.Items.Count > 0)
                {
                    CharacterSkillsListBox.SelectedIndex = 0;
                }
                else
                {
                    DetailsPanelTitleTextBlock.Text = "Skill Details";
                    ItemDetailsPanel.Visibility = Visibility.Collapsed;
                    SkillDetailsPanel.Visibility = Visibility.Visible;
                    ClearSkillDetails();
                    UpdateCampaignWindowButtonStates();
                }
            }
        }

        private void LoadSkillsForCharacter(Character character)
        {
            CharacterSkillsListBox.Items.Clear();

            foreach (Skill skill in character.Skills)
            {
                CharacterSkillsListBox.Items.Add(skill);
            }

            SkillsPlaceholderTextBlock.Visibility =
                character.Skills.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void AddSkillButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NewSkillWindow newSkillWindow = new NewSkillWindow();
            newSkillWindow.Owner = this;

            bool? result = newSkillWindow.ShowDialog();

            if (result == true)
            {
                if (_selectedCharacter.Skills.Any(skill =>
                    string.Equals(skill.Name, newSkillWindow.NewSkill.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("That character already has a skill with that name.", "Duplicate Skill Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedCharacter.Skills.Add(newSkillWindow.NewSkill);
                LoadSkillsForCharacter(_selectedCharacter);
                CharacterSkillsListBox.SelectedItem = newSkillWindow.NewSkill;
                SaveCampaigns();
                UpdateCampaignWindowButtonStates();
            }
        }

        private void EditSkillButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (CharacterSkillsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a skill first.", "No Skill Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Skill selectedSkill = (Skill)CharacterSkillsListBox.SelectedItem;

            NewSkillWindow editSkillWindow = new NewSkillWindow(selectedSkill);
            editSkillWindow.Owner = this;

            bool? result = editSkillWindow.ShowDialog();

            if (result == true)
            {
                if (_selectedCharacter.Skills.Any(skill =>
                    skill != selectedSkill &&
                    string.Equals(skill.Name, editSkillWindow.NewSkill.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("That character already has a skill with that name.", "Duplicate Skill Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int skillIndex = _selectedCharacter.Skills.IndexOf(selectedSkill);

                if (skillIndex >= 0)
                {
                    _selectedCharacter.Skills[skillIndex] = editSkillWindow.NewSkill;
                    LoadSkillsForCharacter(_selectedCharacter);
                    CharacterSkillsListBox.SelectedItem = editSkillWindow.NewSkill;
                    SaveCampaigns();
                    UpdateCampaignWindowButtonStates();
                }
            }
        }

        private void RemoveSkillButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCharacter == null)
            {
                MessageBox.Show("Please select a character first.", "No Character Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (CharacterSkillsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a skill first.", "No Skill Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Skill selectedSkill = (Skill)CharacterSkillsListBox.SelectedItem;

            MessageBoxResult result = MessageBox.Show(
                $"Remove skill '{selectedSkill.Name}'?",
                "Confirm Remove Skill",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _selectedCharacter.Skills.Remove(selectedSkill);
            LoadSkillsForCharacter(_selectedCharacter);
            SaveCampaigns();
            UpdateCampaignWindowButtonStates();
        }

        private void LoadSkillDetails(Skill skill)
        {
            DetailsPanelTitleTextBlock.Text = "Skill Details";

            ItemDetailsPanel.Visibility = Visibility.Collapsed;
            SkillDetailsPanel.Visibility = Visibility.Visible;

            SelectedSkillNameTextBlock.Text = skill.Name;
            SelectedSkillDescriptionTextBlock.Text = skill.Description;
            SelectedSkillNotesTextBlock.Text = skill.Notes;
        }

        private void ClearSkillDetails()
        {
            SelectedSkillNameTextBlock.Text = "Select a skill";
            SelectedSkillDescriptionTextBlock.Text = "Skill description will appear here.";
            SelectedSkillNotesTextBlock.Text = "Skill notes will appear here.";
        }

        private void CharacterSkillsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CharacterSkillsListBox.SelectedItem == null)
            {
                if (SkillsTab.IsSelected)
                {
                    DetailsPanelTitleTextBlock.Text = "Skill Details";
                    ItemDetailsPanel.Visibility = Visibility.Collapsed;
                    SkillDetailsPanel.Visibility = Visibility.Visible;
                    ClearSkillDetails();
                }

                UpdateCampaignWindowButtonStates();
                return;
            }

            CharacterItemsListBox.SelectedItem = null;
            UnassignedItemsListBox.SelectedItem = null;

            Skill selectedSkill = (Skill)CharacterSkillsListBox.SelectedItem;
            LoadSkillDetails(selectedSkill);
            UpdateCampaignWindowButtonStates();
        }

        private void MiddleTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != MiddleTabControl)
            {
                return;
            }

            RefreshSelectedTabItemDetails();
        }


        //Rollabel Tables

        private void OpenRollTablesButton_Click(object sender, RoutedEventArgs e)
        {
            RollTablesWindow rollTablesWindow = new RollTablesWindow();
            rollTablesWindow.Owner = this;
            rollTablesWindow.ShowDialog();
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
