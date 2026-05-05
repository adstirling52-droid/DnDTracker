using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
        public CampaignWindow(string campaignName)
        {
            InitializeComponent();

            CampaignNameTextBlock.Text = campaignName;
            Title = campaignName;

            LoadSampleCharacters();
        }

        private void LoadSampleCharacters()
        {
            AddCharacterButton("Oskar");
            AddCharacterButton("Lanvor");
            AddCharacterButton("Thradal");
        }

        private void AddCharacterButton(string characterName)
        {
            Button characterButton = new Button();
            characterButton.Content = characterName;
            characterButton.Margin = new Thickness(0, 0, 0, 8);
            characterButton.Click += CharacterButton_Click;

            CharacterButtonPanel.Children.Add(characterButton);
        }

        private void CharacterButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            string characterName = clickedButton.Content.ToString()!;

            LoadItemsForCharacter(characterName);
        }

        private void LoadItemsForCharacter(string characterName)
        {
            CharacterItemsListBox.Items.Clear();
            ClearItemDetails();

            if (characterName == "Oskar")
            {
                CharacterItemsListBox.Items.Add("Moon-Touched Sword");
                CharacterItemsListBox.Items.Add("Shield of the Stag");
                CharacterItemsListBox.Items.Add("Potion of Healing");
            }
            else if (characterName == "Lanvor")
            {
                CharacterItemsListBox.Items.Add("Crystal Focus");
                CharacterItemsListBox.Items.Add("Scroll of Burning Hands");
                CharacterItemsListBox.Items.Add("Silver Ring");
            }
            else if (characterName == "Thradal")
            {
                CharacterItemsListBox.Items.Add("Bone Wand");
                CharacterItemsListBox.Items.Add("Black Journal");
                CharacterItemsListBox.Items.Add("Dust of Disappearance");
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

            string itemName = CharacterItemsListBox.SelectedItem.ToString()!;
            LoadProvenanceForItem(itemName);
        }

        private void LoadProvenanceForItem(string itemName)
        {
            if (itemName == "Moon-Touched Sword")
            {
                SelectedItemNameTextBlock.Text = "Moon-Touched Sword";
                SelectedItemDescriptionTextBlock.Text = "A pale silver blade that glows faintly in moonlight.";
                SelectedItemWhereTextBlock.Text = "Where: Goblin Shrine";
                SelectedItemWhenTextBlock.Text = "When: Session 4";
                SelectedItemStatusTextBlock.Text = "Current Status: Carried by Oskar";
                SelectedItemNotesTextBlock.Text = "Found beneath the old watchtower. Possibly linked to a forgotten noble line.";
            }
            else if (itemName == "Shield of the Stag")
            {
                SelectedItemNameTextBlock.Text = "Shield of the Stag";
                SelectedItemDescriptionTextBlock.Text = "A heavy round shield marked with an antlered crest.";
                SelectedItemWhereTextBlock.Text = "Where: Hunter's Barrow";
                SelectedItemWhenTextBlock.Text = "When: Session 5";
                SelectedItemStatusTextBlock.Text = "Current Status: Carried by Oskar";
                SelectedItemNotesTextBlock.Text = "Recovered from a barrow guardian. Still bears deep claw marks.";
            }
            else if (itemName == "Potion of Healing")
            {
                SelectedItemNameTextBlock.Text = "Potion of Healing";
                SelectedItemDescriptionTextBlock.Text = "A small glass vial filled with bright red liquid.";
                SelectedItemWhereTextBlock.Text = "Where: Starter Supplies";
                SelectedItemWhenTextBlock.Text = "When: Session 1";
                SelectedItemStatusTextBlock.Text = "Current Status: In party gear";
                SelectedItemNotesTextBlock.Text = "Common but valuable enough that everyone keeps forgetting who last had it.";
            }
            else if (itemName == "Crystal Focus")
            {
                SelectedItemNameTextBlock.Text = "Crystal Focus";
                SelectedItemDescriptionTextBlock.Text = "A narrow faceted crystal mounted in a bronze grip.";
                SelectedItemWhereTextBlock.Text = "Where: Wizard's Tower";
                SelectedItemWhenTextBlock.Text = "When: Session 3";
                SelectedItemStatusTextBlock.Text = "Current Status: Carried by Lanvor";
                SelectedItemNotesTextBlock.Text = "Seems to strengthen fire magic slightly, though not yet proven.";
            }
            else if (itemName == "Scroll of Burning Hands")
            {
                SelectedItemNameTextBlock.Text = "Scroll of Burning Hands";
                SelectedItemDescriptionTextBlock.Text = "A brittle scroll written in heat-darkened ink.";
                SelectedItemWhereTextBlock.Text = "Where: Ash Chest";
                SelectedItemWhenTextBlock.Text = "When: Session 6";
                SelectedItemStatusTextBlock.Text = "Current Status: Carried by Lanvor";
                SelectedItemNotesTextBlock.Text = "Single use. Smells faintly of smoke even when sealed.";
            }
            else if (itemName == "Silver Ring")
            {
                SelectedItemNameTextBlock.Text = "Silver Ring";
                SelectedItemDescriptionTextBlock.Text = "A plain silver ring with an inscription worn almost smooth.";
                SelectedItemWhereTextBlock.Text = "Where: Fishers Rest";
                SelectedItemWhenTextBlock.Text = "When: Session 8";
                SelectedItemStatusTextBlock.Text = "Current Status: Carried by Lanvor";
                SelectedItemNotesTextBlock.Text = "Could be sentimental rather than magical, though Lanvor suspects otherwise.";
            }
            else if (itemName == "Bone Wand")
            {
                SelectedItemNameTextBlock.Text = "Bone Wand";
                SelectedItemDescriptionTextBlock.Text = "A narrow wand carved from polished bone.";
                SelectedItemWhereTextBlock.Text = "Where: Crypt of Whispers";
                SelectedItemWhenTextBlock.Text = "When: Session 7";
                SelectedItemStatusTextBlock.Text = "Current Status: Carried by Thradal";
                SelectedItemNotesTextBlock.Text = "Unpleasantly warm to the touch at odd moments.";
            }
            else if (itemName == "Black Journal")
            {
                SelectedItemNameTextBlock.Text = "Black Journal";
                SelectedItemDescriptionTextBlock.Text = "A leather journal filled with cramped notes and strange diagrams.";
                SelectedItemWhereTextBlock.Text = "Where: Dead Scholar's Room";
                SelectedItemWhenTextBlock.Text = "When: Session 2";
                SelectedItemStatusTextBlock.Text = "Current Status: Carried by Thradal";
                SelectedItemNotesTextBlock.Text = "Useful for clues, but some pages seem to rewrite themselves.";
            }
            else if (itemName == "Dust of Disappearance")
            {
                SelectedItemNameTextBlock.Text = "Dust of Disappearance";
                SelectedItemDescriptionTextBlock.Text = "A small packet of glittering grey dust.";
                SelectedItemWhereTextBlock.Text = "Where: Hidden Cache";
                SelectedItemWhenTextBlock.Text = "When: Session 9";
                SelectedItemStatusTextBlock.Text = "Current Status: Carried by Thradal";
                SelectedItemNotesTextBlock.Text = "Thradal claims he is saving it for the perfect moment, which worries everyone.";
            }
        }
    }
}
