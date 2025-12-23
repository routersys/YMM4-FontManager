using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FontManager.ViewModels;

namespace FontManager.Views
{
    public partial class FontManagerView : UserControl
    {
        public FontManagerView() => InitializeComponent();

        private void PreviewTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        }

        private void PreviewTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is FontItemViewModel vm)
            {
                vm.IsEditing = false;
            }
        }

        private void PreviewTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is FontItemViewModel vm)
                {
                    vm.IsEditing = false;
                }
            }
        }
    }
}