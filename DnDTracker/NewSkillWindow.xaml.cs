using DnDTracker.Models;
using System.Windows;

namespace DnDTracker
{
    public partial class NewSkillWindow : Window
    {
        public Skill NewSkill { get; private set; } = new Skill();

        public NewSkillWindow()
        {
            InitializeComponent();

            Title = "New Skill";
            WindowHeadingTextBlock.Text = "Create New Skill";
        }

        public NewSkillWindow(Skill existingSkill)
        {
            InitializeComponent();

            Title = "Edit Skill";
            WindowHeadingTextBlock.Text = "Edit Skill";

            SkillNameTextBox.Text = existingSkill.Name;
            DescriptionTextBox.Text = existingSkill.Description;
            NotesTextBox.Text = existingSkill.Notes;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string skillName = SkillNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(skillName))
            {
                MessageBox.Show("Please enter a skill name.", "Missing Skill Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewSkill = new Skill
            {
                Name = skillName,
                Description = DescriptionTextBox.Text.Trim(),
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