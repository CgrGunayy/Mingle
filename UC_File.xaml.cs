using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
    public partial class UC_File : UserControl
    {
        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        public string FilePath
        {
            get { return (string)GetValue(FilePathProperty); }
            set { SetValue(FilePathProperty, value); }
        }

        public BitmapImage FileThumbnail
        {
            get { return (BitmapImage)GetValue(FileThumbnailProperty); }
            set { SetValue(FileThumbnailProperty, value); }
        }

        public FileType FileType
        {
            get { return (FileType)GetValue(FileTypeProperty); }
            set { SetValue(FileTypeProperty, value); }
        }

        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register("FileName", typeof(string), typeof(UC_File), new PropertyMetadata("null", OnFileNameChanged));

        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register("FilePath", typeof(string), typeof(UC_File), new PropertyMetadata("null", OnFilePathChanged));

        public static readonly DependencyProperty FileThumbnailProperty =
            DependencyProperty.Register("FileThumbnail", typeof(BitmapImage), typeof(UC_File), new PropertyMetadata(null, OnFileThumbnailChanged));

        public static readonly DependencyProperty FileTypeProperty =
            DependencyProperty.Register("FileType", typeof(FileType), typeof(UC_File), new PropertyMetadata(FileType.Video, OnFileTypeChanged));


        private FileData fileData;
        private Point dragStartPoint;

        public UC_File()
        {
            InitializeComponent();

            fileData = new FileData();
            fileData.Name = FileName;
            fileData.Path = FilePath;
        }

        private void SetFileName(string name)
        {
            fileNameHolder.Content = name;
            fileData.Name = name;
        }

        private void SetFilePath(string path)
        {
            fileData.Path = path;
        }

        private void SetThumbnail(BitmapImage thumbnail)
        {
            if (thumbnail == null)
            {
                // Burada default bir resim olabilir, dosya silüeti gibi
                return;
            }

            fileImageHolder.Source = thumbnail;
            fileData.ThumbnailPath = thumbnail.UriSource.AbsolutePath;
        }

        private void SetFileType(FileType type)
        {
            fileData.Type = type;
        }

        private static void OnFileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UC_File control)
            {
                string newValue = (string)e.NewValue;
                control.SetFileName(newValue);
            }
        }

        private static void OnFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UC_File control)
            {
                string newValue = (string)e.NewValue;
                control.SetFilePath(newValue);
            }
        }

        private static void OnFileThumbnailChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UC_File control)
            {
                BitmapImage newValue = (BitmapImage)e.NewValue;
                control.SetThumbnail(newValue);
            }
        }

        private static void OnFileTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UC_File control)
            {
                FileType newValue = (FileType)e.NewValue;
                control.SetFileType(newValue);
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                dragStartPoint = e.GetPosition(null);
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(null);
                Vector diff = dragStartPoint - currentPos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DataObject dragData = new DataObject("MingleFile", fileData);
                    DragDrop.DoDragDrop(this, dragData, DragDropEffects.Copy);
                }
            }
        }
    }
}
