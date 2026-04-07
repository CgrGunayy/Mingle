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
using System.Windows.Shapes;

namespace MingleWPF
{
    public partial class LayerAddDialogWindow : Window
    {
        public bool IsAborted { get; private set; } = false;
        public string LayerNameInput { get; private set; } = string.Empty;
        public string LayerTypeInput { get; private set; } = string.Empty;

        public LayerAddDialogWindow()
        {
            InitializeComponent();
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsAborted = true;
            this.Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (layerNameInputBox.Text == null || layerNameInputBox.Text.Trim().Length == 0)
            {
                layerNameInputBox.Focus();
                return;
            }

            LayerNameInput = layerNameInputBox.Text.Trim();
            LayerTypeInput = (string)((ComboBoxItem)layerTypeComboBox.SelectedValue).Content;

            this.Close();
        }
    }
}
