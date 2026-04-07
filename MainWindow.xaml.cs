using FFMpegCore;
using Microsoft.Win32;
using System.ComponentModel;
using System.Drawing;
using System.IO;
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

        private readonly System.Drawing.Size THUMBNAIL_SIZE = new System.Drawing.Size(150, 100);

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

        private void SearchFiles(string search)
        {
            var medias = libraryPanel.Children;
            string lowerCaseSearch = search.ToLower();
            foreach (UC_File media in medias)
            {
                bool inTab = ((media.FileType == FileType.Video || media.FileType == FileType.Image) && CurrentTab == MingleTab.Media)
                             || (media.FileType == FileType.Effect && CurrentTab == MingleTab.Effects)
                             || (media.FileType == FileType.Audio && CurrentTab == MingleTab.Audio);

                bool contains = search == "*" ? true : media.FileName.ToLower().Contains(lowerCaseSearch);
                media.Visibility = inTab && contains ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateLibrary()
        {
            if (searchBox.Text == null || searchBox.Text.Trim().Length == 0)
            {
                SearchFiles("*");
                return;
            }

            SearchFiles(searchBox.Text);
        }

        private async Task<BitmapImage?> CreateThumbnailFromVideo(string videoPath, System.Drawing.Size size)
        {
            string thumbnailPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".jpg");

            bool success = await FFMpeg.SnapshotAsync(videoPath, thumbnailPath, size, TimeSpan.FromSeconds(1));

            if (success == false)
                return null;

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(thumbnailPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        private BitmapImage? CreateThumbnailFromImage(string imagePath, System.Drawing.Size size)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);

            bitmap.DecodePixelWidth = size.Width;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            bitmap.EndInit();

            return bitmap;
        }

        #region Button Eventleri

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
            UpdateLibrary();
        }

        private void EffectButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTab = MingleTab.Effects;
            UpdateLibrary();
        }

        private void AudioButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTab = MingleTab.Audio;
            UpdateLibrary();
        }

        private void AddLayerButton_Click(object sender, RoutedEventArgs e)
        {
            LayerAddDialogWindow addDialog = new LayerAddDialogWindow();
            addDialog.ShowDialog();

            if (!addDialog.IsAborted)
            {
                UC_LayerControl layer = new UC_LayerControl();
                layer.LayerName = addDialog.LayerNameInput;
                layer.LayerType = addDialog.LayerTypeInput;
                layersPanel.Children.Insert(0, layer);
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (searchBox.Text == null || searchBox.Text.Trim().Length == 0)
                {
                    SearchFiles("*");
                    return;
                }

                SearchFiles(searchBox.Text);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (searchBox.Text == null || searchBox.Text.Trim().Length == 0)
                return;

            SearchFiles(searchBox.Text);
        }


        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchBox.Text == null || searchBox.Text.Trim().Length == 0)
            {
                SearchFiles("*");
                return;
            }

            SearchFiles(searchBox.Text);
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "All files (*.*)|*.*|Media files (*.mp4;*.png;*.jpeg;*.jpg)|*.mp4;*.png;*.jpeg;*.jpg|Audio files (*.mp3;*.wav)|*.mp3;*.wav";
            fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fileDialog.ShowDialog() == true)
            {
                string selectedFilePath = fileDialog.FileName;
                string selectedFileName = System.IO.Path.GetFileName(selectedFilePath);

                var pathParts = selectedFileName.Split(".");

                string selectedFileExtension = pathParts[pathParts.Length - 1];

                BitmapImage? thumbnail = null;
                FileType fileType = FileType.Video;

                switch (selectedFileExtension)
                {
                    case "mp4":
                    case "mov":
                        thumbnail = await CreateThumbnailFromVideo(selectedFilePath, THUMBNAIL_SIZE);
                        fileType = FileType.Video;
                        break;
                    case "png":
                    case "jpg":
                    case "jpeg":
                        thumbnail = CreateThumbnailFromImage(selectedFilePath, THUMBNAIL_SIZE);
                        fileType = FileType.Image;
                        break;
                    case "mp3":
                    case "wav":
                        fileType = FileType.Audio;
                        break;
                    default:
                        MessageBox.Show("Unsupported File Format: ." + selectedFileExtension);
                        return;
                }

                UC_File media = new UC_File();
                media.FileName = selectedFileName;
                media.FileThumbnail = thumbnail;
                media.FileType = fileType;

                libraryPanel.Children.Add(media);
            }
        }

        #endregion
    }
}