using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TarekGamingSpace.ViewModels;

namespace TarekGamingSpace.Views
{
    /// <summary>Closing only hides the panel; the app keeps running in the tray.</summary>
    public partial class ControlPanelWindow : Window
    {
        public ControlPanelWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        public void ShowAndFocus()
        {
            Show();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!((App)Application.Current).IsExiting)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnClosing(e);
        }

        void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        void Close_Click(object sender, RoutedEventArgs e) => Close();

        void Digits_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !e.Text.All(char.IsDigit);

        /// <summary>Enter commits a rename.</summary>
        void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            ((TextBox)sender).GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            Keyboard.ClearFocus();
        }
    }
}
