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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

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

        public string ProjectName { get; private set; }
        public string ProjectPath { get; private set; }

        public static StackPanel LayersPanel { get; private set; }

        private MingleTab currentTab;

        private bool isPreviewPlaying = false;
        private DateTime lastTick;

        private DispatcherTimer autoSaveTimer;

        private readonly System.Drawing.Size THUMBNAIL_SIZE = new System.Drawing.Size(150, 100);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public MainWindow(string projectName, string projectPath)
        {
            InitializeComponent();

            CurrentTab = MingleTab.Media;

            ProjectName = projectName;
            ProjectPath = projectPath;

            LoadProjectFiles();

            ProjectNameText.Content = ProjectName;

            this.DataContext = this;

            LayersPanel = layersPanel;

            Timeline.SetPreviewPlayer(PreviewPlayer);

            this.KeyDown += MainWindow_KeyDown;

            autoSaveTimer = new DispatcherTimer();
            autoSaveTimer.Interval = TimeSpan.FromMinutes(2);
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            autoSaveTimer.Start();
        }

        private void SaveFiles()
        {
            List<FileData> allFiles = new List<FileData>();
            foreach (UC_File fileUC in libraryPanel.Children)
            {
                allFiles.Add(fileUC.FileData);
            }

            ProjectSaveData saveData = new ProjectSaveData
            {
                ImportedFiles = allFiles,
                PlayheadSeconds = Timeline.PlayheadSeconds
            };

            foreach (var layerPair in Timeline.TimelineData.Layers)
            {
                var layer = layerPair.Value;

                LayerSaveData layerSave = new LayerSaveData
                {
                    LayerID = layer.LayerID,
                    LayerTitle = layer.LayerTitle,
                    LayerOrder = layer.LayerOrder,
                    LayerType = layer.LayerType
                };

                foreach (var clip in layer.Clips)
                {
                    ClipSaveData clipSave = new ClipSaveData
                    {
                        ClipTitle = clip.ClipTitle,
                        StartSecondsOnTimeline = clip.StartSecondsOnTimeline,
                        EndSecondsOnTimeline = clip.EndSecondsOnTimeline,
                        VideoStartSecond = clip.VideoStartSecond,
                        ClipDuration = clip.ClipDuration,
                        LayerID = layer.LayerID,
                        FilePath = clip.FileData.Path
                    };

                    layerSave.Clips.Add(clipSave);
                }

                saveData.Layers.Add(layerSave);
            }

            SaveHandler.SaveProject(ProjectPath, ProjectName, saveData);
        }

        private async void LoadProjectFiles()
        {
            ProjectSaveData loadedProject = SaveHandler.LoadProject(ProjectPath, ProjectName);

            var ImportedFiles = loadedProject.ImportedFiles;

            foreach (var fileData in ImportedFiles)
            {
                switch (fileData.Type)
                {
                    case FileType.Video:
                        fileData.Thumbnail = await CreateThumbnailFromVideo(fileData.ThumbnailPath, THUMBNAIL_SIZE);
                        break;
                    case FileType.Image:
                        fileData.Thumbnail = CreateThumbnailFromImage(fileData.ThumbnailPath, THUMBNAIL_SIZE);
                        break;
                }

                UC_File file = new UC_File(fileData);
                file.FileName = fileData.Name;
                file.FilePath = fileData.Path;
                file.FileType = fileData.Type;

                libraryPanel.Children.Add(file);
            }

            UpdateLibrary();

            if (loadedProject.Layers == null || loadedProject.Layers.Count == 0)
            {
                CreateLayerControl("Video Layer 01", LayerType.Video);
                CreateLayerControl("Video Layer 02", LayerType.Video);
                CreateLayerControl("Audio (Coming Soon)", LayerType.Audio);
            }
            else
            {
                Timeline.TimelineData.Layers.Clear();
                Timeline.TimelineData.Clips.Clear();
                layersPanel.Children.Clear();

                loadedProject.Layers = loadedProject.Layers.OrderBy(l => l.LayerOrder).ToList();
                foreach (var layerSave in loadedProject.Layers)
                {
                    TimelineLayer newLayer = new TimelineLayer()
                    {
                        LayerID = layerSave.LayerID,
                        LayerOrder = layerSave.LayerOrder,
                        LayerTitle = layerSave.LayerTitle,
                        LayerType = layerSave.LayerType
                    };

                    CreateLayerControl(layerSave.LayerTitle, layerSave.LayerType, layerSave.LayerID);
                    Timeline.TimelineData.Layers.Add(layerSave.LayerID, newLayer);

                    foreach (var clipSave in layerSave.Clips)
                    {
                        var clip = new TimelineClip
                        {
                            ClipID = clipSave.ClipId,
                            ClipTitle = clipSave.ClipTitle,
                            StartSecondsOnTimeline = clipSave.StartSecondsOnTimeline,
                            EndSecondsOnTimeline = clipSave.EndSecondsOnTimeline,
                            ClipDuration = clipSave.ClipDuration,
                            LayerID = clipSave.LayerID,
                            VideoStartSecond = clipSave.VideoStartSecond,
                            VideoDuration = clipSave.VideoDuration
                        };

                        foreach (var fileData in ImportedFiles)
                        {
                            if (fileData.Path == clipSave.FilePath)
                            {
                                clip.FileData = fileData;
                                break;
                            }
                        }

                        Timeline.TimelineData.AddClip(clip, newLayer);
                        InvalidateVisual();
                    }

                }
            }

            Timeline.PlayheadSeconds = loadedProject.PlayheadSeconds;
            Timeline.InvalidateVisual();
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

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double elapsedSeconds = (now - lastTick).TotalSeconds;
            lastTick = now;

            Timeline.PlayheadSeconds += elapsedSeconds;

            Timeline.InvalidateVisual();

            PreviewPlayer.UpdatePreview(Timeline.TimelineData, Timeline.PlayheadSeconds, isPlaying: true);
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            SaveFiles();
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

        private void CreateLayerControl(string layerName, LayerType layerType, uint layerID = 0)
        {
            int layerOrder = layersPanel.Children.Count;
            int index = layersPanel.Children.Count - 1;
            if (layerType == LayerType.Audio)
            {
                layerOrder = 0;
                index = layersPanel.Children.Count;
            }

            foreach (UC_LayerControl _layer in layersPanel.Children)
            {
                _layer.Margin = new Thickness(0, 0, 0, 0);
                Timeline.TimelineData.Layers[_layer.LayerID].LayerOrder = index;
                index--;
            }

            if (layerID == 0)
                Timeline.TimelineData.AddLayer(layerName, layerType, layerOrder, out layerID);

            UC_LayerControl layer = new UC_LayerControl(layerID);
            layer.LayerName = layerName;
            layer.LayerType = layerType;

            if (layer.LayerType == LayerType.Audio)
                layersPanel.Children.Insert(layersPanel.Children.Count, layer);
            else
                layersPanel.Children.Insert(0, layer);

            if (layersPanel.Children.Count > 0)
            {
                var firstLayer = (UC_LayerControl)(layersPanel.Children[0]);
                firstLayer.Margin = new Thickness(0, 10, 0, 0);

                var lastLayer = (UC_LayerControl)(layersPanel.Children[layersPanel.Children.Count - 1]);
                lastLayer.Margin = new Thickness(0, 0, 0, 10);
            }

            Timeline.Height = Math.Max(TimelineScrollViewer.ActualHeight, Timeline.TimelineTopHeight + layersPanel.Children.Count * Timeline.LayerHeight + 10);
            Timeline.InvalidateVisual();
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

            bitmap.Freeze();

            return bitmap;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Timeline.Height = Math.Max(TimelineScrollViewer.ActualHeight, layersPanel.Children.Count * Timeline.LayerHeight);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveFiles();
            }
        }

        #region Menu Eventleri

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportDialogWindow exportWindow = new ExportDialogWindow(Timeline.TimelineData);
            exportWindow.ShowDialog();
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectSetupWindow setupWindow = new ProjectSetupWindow();
            setupWindow.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            DatabaseHandler.CurrentUserId = -1;
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectSelectionWindow selectionWindow = new ProjectSelectionWindow();
            selectionWindow.Show();
            this.Close();
        }

        #endregion

        #region Button Eventleri

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            isPreviewPlaying = !isPreviewPlaying;

            if (isPreviewPlaying)
            {
                lastTick = DateTime.Now;
                CompositionTarget.Rendering += PlaybackTimer_Tick;
                PreviewPlayer.Play();
                PlaybackButtonIcon.Kind = MahApps.Metro.IconPacks.PackIconBootstrapIconsKind.PauseFill;
            }
            else
            {
                CompositionTarget.Rendering -= PlaybackTimer_Tick;
                PreviewPlayer.Pause();
                PreviewPlayer.UpdatePreview(Timeline.TimelineData, Timeline.PlayheadSeconds);
                PlaybackButtonIcon.Kind = MahApps.Metro.IconPacks.PackIconBootstrapIconsKind.PlayFill;
            }
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
                LayerType type = (LayerType)Enum.Parse(typeof(LayerType), addDialog.LayerTypeInput);
                CreateLayerControl(addDialog.LayerNameInput, type);
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
                        CurrentTab = MingleTab.Media;
                        break;
                    case "png":
                    case "jpg":
                    case "jpeg":
                        thumbnail = CreateThumbnailFromImage(selectedFilePath, THUMBNAIL_SIZE);
                        fileType = FileType.Image;
                        CurrentTab = MingleTab.Media;
                        break;
                    case "mp3":
                    case "wav":
                        fileType = FileType.Audio;
                        CurrentTab = MingleTab.Audio;
                        break;
                    default:
                        MessageBox.Show("Unsupported File Format: ." + selectedFileExtension);
                        return;
                }

                UC_File file = new UC_File();
                file.FileName = selectedFileName;
                file.FileThumbnail = thumbnail;
                file.FilePath = selectedFilePath;
                file.FileType = fileType;

                libraryPanel.Children.Add(file);
                UpdateLibrary();

                SaveFiles();
            }
        }

        #endregion

        private void LayerScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                TimelineScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        private void TimelineScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                LayerScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }
    }
}