using System;
using System.Collections.Generic;
using System.Linq;
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
    public enum FileType
    {
        Video,
        Image,
        Effect,
        Audio
    }

    public partial class UC_File : UserControl
    {
        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
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

        public static readonly DependencyProperty FileThumbnailProperty =
            DependencyProperty.Register("FileThumbnail", typeof(BitmapImage), typeof(UC_File), new PropertyMetadata(null, OnFileThumbnailChanged));

        public static readonly DependencyProperty FileTypeProperty =
            DependencyProperty.Register("FileType", typeof(FileType), typeof(UC_File), new PropertyMetadata(FileType.Video));

        public UC_File()
        {
            InitializeComponent();
        }

        private void SetFileName(string name)
        {
            fileNameHolder.Content = name;
        }

        private void SetThumbnail(BitmapImage thumbnail)
        {
            if (thumbnail == null)
            {
                // Burada default bir resim olabilir, dosya silüeti gibi
                return;
            }

            fileImageHolder.Source = thumbnail;
        }

        private static void OnFileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UC_File control)
            {
                string newValue = (string)e.NewValue;
                control.SetFileName(newValue);
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
    }
}
