using System.Windows;
using System.Windows.Input;

namespace MingleWPF
{
    public partial class ProjectSelectionWindow : Window
    {
        public ProjectSelectionWindow()
        {
            InitializeComponent();
            LoadProjects();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void LoadProjects()
        {
            var projects = DatabaseHandler.GetProjectsByUserId(DatabaseHandler.CurrentUserId);
            ProjectsListBox.ItemsSource = projects;
        }

        private void OpenSelectedProject()
        {
            if (ProjectsListBox.SelectedItem is ProjectModel selectedProject)
            {
                MainWindow mainWindow = new MainWindow(selectedProject.ProjectName, selectedProject.RootPath);
                mainWindow.Show();

                this.Close();
            }
            else
            {
                MessageBox.Show("Please Select A Project From The List First.", "No Selection");
            }
        }

        private void OpenProjectButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedProject();
        }

        private void ProjectsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelectedProject();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectSetupWindow setupWindow = new ProjectSetupWindow();
            setupWindow.Show();
            this.Close();
        }
    }
}