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
using MahApps.Metro.IconPacks;

namespace MingleWPF
{
    public partial class UC_LayerControl : UserControl
    {

        public string LayerName
        {
            get { return (string)GetValue(LayerNameProperty); }
            set { SetValue(LayerNameProperty, value); }
        }

        public string LayerType
        {
            get { return (string)GetValue(LayerTypeProperty); }
            set { SetValue(LayerTypeProperty, value); }
        }

        public static readonly DependencyProperty LayerTypeProperty =
            DependencyProperty.Register("LayerType", typeof(string), typeof(UC_LayerControl), new PropertyMetadata("Video", OnLayerTypeChanged));


        public static readonly DependencyProperty LayerNameProperty =
            DependencyProperty.Register("LayerName", typeof(string), typeof(UC_LayerControl), new PropertyMetadata("Layer Name", OnLayerNameChanged));


        public UC_LayerControl()
        {
            InitializeComponent();

            UpdateLayerVisuals(this.LayerType);
            UpdateLayerTitle(this.LayerName);
        }

        private void UpdateLayerVisuals(string type)
        {
            switch (type)
            {
                case "Video":
                    layerIcon.Kind = PackIconBootstrapIconsKind.CameraVideo;
                    layerButton01.Kind = PackIconBootstrapIconsKind.Eye;
                    layerButton02.Kind = PackIconBootstrapIconsKind.UnlockFill;
                    break;

                case "Audio":
                    layerIcon.Kind = PackIconBootstrapIconsKind.Speaker;
                    layerButton01.Kind = PackIconBootstrapIconsKind.Soundwave;
                    layerButton02.Kind = PackIconBootstrapIconsKind.MicFill;
                    break;
            }
        }

        private void UpdateLayerTitle(string name)
        {
            layerTitle.Content = name;
        }

        private static void OnLayerTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UC_LayerControl control)
            {
                string newValue = (string)e.NewValue;
                control.UpdateLayerVisuals(newValue);
            }
        }

        private static void OnLayerNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UC_LayerControl control)
            {
                string newValue = (string)e.NewValue;
                control.UpdateLayerTitle(newValue);
            }
        }
    }
}
