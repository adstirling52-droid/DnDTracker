using DnDTracker.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace DnDTracker
{
    public partial class RollTablesWindow : Window
    {
        private readonly Campaign _campaign;
        private readonly Action _saveCampaigns;
        private readonly Action _refreshCampaignViews;
        private readonly Random _random = new Random();
        private RollTableRow? _lastRolledRow;
        private RollTable? _lastRolledTable;

        public RollTablesWindow(Campaign campaign, Action saveCampaigns, Action refreshCampaignViews)
        {
            InitializeComponent();

            _campaign = campaign;
            _saveCampaigns = saveCampaigns;
            _refreshCampaignViews = refreshCampaignViews;

            RefreshRollTablesList();
            ClearRollResult();
            UpdateRollResultActionButtons(null);
        }

        private void RefreshRollTablesList()
        {
            RollTablesListBox.Items.Clear();

            foreach (RollTable table in _campaign.RollTables)
            {
                RollTablesListBox.Items.Add(table);
            }

            NoTablesPlaceholderTextBlock.Visibility =
                _campaign.RollTables.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void ImportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Import Rollable Table CSV";
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";

            bool? fileResult = openFileDialog.ShowDialog();

            if (fileResult != true)
            {
                return;
            }

            SelectTableTypeWindow selectTableTypeWindow = new SelectTableTypeWindow();
            selectTableTypeWindow.Owner = this;

            bool? typeResult = selectTableTypeWindow.ShowDialog();

            if (typeResult != true)
            {
                return;
            }

            try
            {
                RollTable importedTable = ImportRollTableFromCsv(
                    openFileDialog.FileName,
                    selectTableTypeWindow.SelectedTableType);

                if (_campaign.RollTables.Any(table =>
                    string.Equals(table.Name, importedTable.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("A rollable table with that name already exists.", "Duplicate Table Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _campaign.RollTables.Add(importedTable);
                RefreshRollTablesList();

                RollTablesListBox.SelectedItem = importedTable;
                RollTableRowsDataGrid.ItemsSource = importedTable.Rows;

                _saveCampaigns();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import CSV.\n\n{ex.Message}", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private RollTable ImportRollTableFromCsv(string filePath, string tableType)
        {
            string[] lines = File.ReadAllLines(filePath);

            if (lines.Length < 2)
            {
                throw new InvalidOperationException("The CSV file must contain a header row and at least one data row.");
            }

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            RollTable rollTable = new RollTable
            {
                Name = fileNameWithoutExtension,
                Category = "",
                TableType = tableType
            };

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Split(',');

                if (parts.Length < 4)
                {
                    throw new InvalidOperationException($"Row {i + 1} does not contain 4 columns.");
                }

                if (!int.TryParse(parts[0].Trim(), out int number))
                {
                    throw new InvalidOperationException($"Row {i + 1} has an invalid Number value.");
                }

                RollTableRow row = new RollTableRow
                {
                    Number = number,
                    Name = parts[1].Trim(),
                    PhysicalDescription = parts[2].Trim(),
                    SpecialCharacteristics = parts[3].Trim()
                };

                rollTable.Rows.Add(row);
            }

            return rollTable;
        }

        private void RollTablesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (RollTablesListBox.SelectedItem == null)
            {
                RollTableRowsDataGrid.ItemsSource = null;
                ClearRollResult();
                return;
            }

            RollTable selectedTable = (RollTable)RollTablesListBox.SelectedItem;
            RollTableRowsDataGrid.ItemsSource = selectedTable.Rows;
            ClearRollResult();
        }

        //Roll
        private void RollButton_Click(object sender, RoutedEventArgs e)
        {
            if (RollTablesListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a rollable table first.", "No Table Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RollTable selectedTable = (RollTable)RollTablesListBox.SelectedItem;

            if (selectedTable.Rows == null || selectedTable.Rows.Count == 0)
            {
                MessageBox.Show("The selected table has no rows to roll on.", "Empty Table", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int rollIndex = _random.Next(selectedTable.Rows.Count);
            RollTableRow rolledRow = selectedTable.Rows[rollIndex];

            RollTableRowsDataGrid.SelectedItem = rolledRow;
            RollTableRowsDataGrid.ScrollIntoView(rolledRow);

            ShowRollResult(selectedTable, rolledRow);
        }

        private void RemoveTableButton_Click(object sender, RoutedEventArgs e)
        {
            if (RollTablesListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a rollable table first.", "No Table Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RollTable selectedTable = (RollTable)RollTablesListBox.SelectedItem;

            MessageBoxResult result = MessageBox.Show(
                $"Remove rollable table '{selectedTable.Name}'?",
                "Confirm Remove Table",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _campaign.RollTables.Remove(selectedTable);
            RefreshRollTablesList();
            RollTableRowsDataGrid.ItemsSource = null;
            _saveCampaigns();
        }

        private void UpdateRollResultActionButtons(RollTable? table)
        {
            bool itemTableRolled = table != null &&
                                   _lastRolledRow != null &&
                                   string.Equals(table.TableType, "Item", StringComparison.OrdinalIgnoreCase);

            bool skillTableRolled = table != null &&
                                    _lastRolledRow != null &&
                                    string.Equals(table.TableType, "Skill", StringComparison.OrdinalIgnoreCase);

            AddRolledItemToCharacterButton.IsEnabled = itemTableRolled;
            AddRolledItemToUnassignedButton.IsEnabled = itemTableRolled;
            AddRolledSkillToCharacterButton.IsEnabled = skillTableRolled;
        }

        private void ClearRollResult()
        {
            _lastRolledRow = null;
            _lastRolledTable = null;

            RolledResultNameTextBlock.Text = "No result rolled yet.";
            RolledResultDescriptionTextBlock.Text = "";
            RolledResultSpecialTextBlock.Text = "";
            RolledResultMetaTextBlock.Text = "";

            UpdateRollResultActionButtons(null);
        }

        private void ShowRollResult(RollTable table, RollTableRow row)
        {
            _lastRolledTable = table;
            _lastRolledRow = row;

            RolledResultNameTextBlock.Text = row.Name;
            RolledResultDescriptionTextBlock.Text = $"Physical Description: {row.PhysicalDescription}";
            RolledResultSpecialTextBlock.Text = $"Special Characteristics: {row.SpecialCharacteristics}";
            RolledResultMetaTextBlock.Text = $"Rolled {row.Number} on {table.Name} ({table.TableType})";

            UpdateRollResultActionButtons(table);
        }

        private void AddRolledItemToUnassignedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastRolledTable == null || _lastRolledRow == null)
            {
                MessageBox.Show("Please roll an item table first.", "No Rolled Result", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Item newItem = CreateItemFromRolledResult(_lastRolledTable, _lastRolledRow);
            newItem.CurrentStatus = "Unassigned";
            newItem.Notes = _lastRolledRow.SpecialCharacteristics;

            if (_campaign.UnassignedItems.Any(item =>
                string.Equals(item.Name, newItem.Name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("An unassigned item with that name already exists.", "Duplicate Item Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _campaign.UnassignedItems.Add(newItem);
            _saveCampaigns();
            _refreshCampaignViews();

            MessageBox.Show($"'{newItem.Name}' was added to the unassigned item pool.", "Item Added", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddRolledItemToCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastRolledTable == null || _lastRolledRow == null)
            {
                MessageBox.Show("Please roll an item table first.", "No Rolled Result", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_campaign.Characters.Count == 0)
            {
                MessageBox.Show("There are no characters in this campaign.", "No Characters Available", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectCharacterWindow selectCharacterWindow = new SelectCharacterWindow(_campaign.Characters);
            selectCharacterWindow.Owner = this;

            bool? result = selectCharacterWindow.ShowDialog();

            if (result != true || selectCharacterWindow.SelectedCharacter == null)
            {
                return;
            }

            Character targetCharacter = selectCharacterWindow.SelectedCharacter;

            Item newItem = CreateItemFromRolledResult(_lastRolledTable, _lastRolledRow);
            newItem.CurrentStatus = $"Carried by {targetCharacter.Name}";
            newItem.Notes = _lastRolledRow.SpecialCharacteristics;

            if (targetCharacter.Items.Any(item =>
                string.Equals(item.Name, newItem.Name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("That character already has an item with that name.", "Duplicate Item Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            targetCharacter.Items.Add(newItem);
            _saveCampaigns();
            _refreshCampaignViews();

            MessageBox.Show($"'{newItem.Name}' was added to {targetCharacter.Name}.", "Item Added", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private Item CreateItemFromRolledResult(RollTable table, RollTableRow row)
        {
            return new Item
            {
                Name = row.Name,
                Description = row.PhysicalDescription,
                WhereFound = "",
                WhenFound = "",
                CurrentStatus = "",
                Notes = row.SpecialCharacteristics,
                ImagePath = "",
                ProvenanceEntries = new System.Collections.Generic.List<ProvenanceEntry>
        {
            new ProvenanceEntry
            {
                What = "Generated from Roll Table",
                Where = "",
                When = "",
                Notes = $"Generated from table '{table.Name}' with roll {row.Number}."
            }
        }
            };
        }

        private void AddRolledSkillToCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastRolledTable == null || _lastRolledRow == null)
            {
                MessageBox.Show("Please roll a skill table first.", "No Rolled Result", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_campaign.Characters.Count == 0)
            {
                MessageBox.Show("There are no characters in this campaign.", "No Characters Available", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectCharacterWindow selectCharacterWindow = new SelectCharacterWindow(_campaign.Characters);
            selectCharacterWindow.Owner = this;

            bool? result = selectCharacterWindow.ShowDialog();

            if (result != true || selectCharacterWindow.SelectedCharacter == null)
            {
                return;
            }

            Character targetCharacter = selectCharacterWindow.SelectedCharacter;
            Skill newSkill = CreateSkillFromRolledResult(_lastRolledRow);

            if (targetCharacter.Skills.Any(skill =>
                string.Equals(skill.Name, newSkill.Name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("That character already has a skill with that name.", "Duplicate Skill Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            targetCharacter.Skills.Add(newSkill);
            _saveCampaigns();
            _refreshCampaignViews();

            MessageBox.Show($"'{newSkill.Name}' was added to {targetCharacter.Name} as a skill.", "Skill Added", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private Skill CreateSkillFromRolledResult(RollTableRow row)
        {
            return new Skill
            {
                Name = row.Name,
                Description = row.PhysicalDescription,
                Notes = row.SpecialCharacteristics
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}