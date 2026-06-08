using System.IO;
using System.Windows;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;

namespace MingleWPF
{
    public partial class ProjectSetupWindow : Window
    {
        public ProjectSetupWindow()
        {
            InitializeComponent();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select a folder",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.SelectedPath;
                ProjectPathTextBox.Text = folderPath;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string projectName = ProjectNameTextBox.Text.Trim();
            string rootPath = ProjectPathTextBox.Text.Trim();

            if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(rootPath))
            {
                MessageBox.Show("Please provide a project name and a location.", "Missing Information");
                return;
            }

            string fullProjectPath = Path.Combine(rootPath, projectName);

            try
            {
                if (!Directory.Exists(fullProjectPath))
                {
                    Directory.CreateDirectory(fullProjectPath);
                }

                DatabaseHandler.CreateNewProject(DatabaseHandler.CurrentUserId, projectName, fullProjectPath);

                MainWindow mainWindow = new MainWindow(projectName, fullProjectPath);
                mainWindow.Show();
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to create project directory.\nError: {ex.Message}", "Error");
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectSelectionWindow selectionWindow = new ProjectSelectionWindow();
            selectionWindow.Show();
            this.Close();
        }
    }
}