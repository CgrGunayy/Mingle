using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MingleWPF
{
    public enum MingleTab
    {
        Media,
        Effects,
        Audio
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MingleTab CurrentTab
        {
            get { return currentTab; }
            set
            {
                if (currentTab != value)
                {
                    currentTab = value;
                    OnPropertyChanged();
                }
            }
        }

        private MingleTab currentTab;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public MainWindow()
        {
            InitializeComponent();

            CurrentTab = MingleTab.Media;
            this.DataContext = this;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MediaButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTab = MingleTab.Media;
        }
    }
}