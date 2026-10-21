using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TarekGamingSpace.ViewModels;

namespace TarekGamingSpace.Views
{
    public partial class StripWindow : Window
    {
        readonly MainViewModel _vm;
        bool _placed;

        public StripWindow(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            SourceInitialized += (s, e) => HideFromAltTab();
            Loaded += (s, e) => Place();
            // Keep the right edge fixed: when a poste is added on the left, the strip grows to the left.
            SizeChanged += (s, e) => { if (_placed) Left = _vm.Settings.StripRight - ActualWidth; };
        }

        void Place()
        {
            var area = SystemParameters.WorkArea;
            var st = _vm.Settings;
            double right = st.StripRight, top = st.StripTop;

            // First run, or the saved spot is off-screen (resolution changed): top-right corner.
            bool invalid = double.IsNaN(right) || double.IsNaN(top)
                || right - 60 < area.Left || right > area.Right + 1
                || top < area.Top || top + 40 > area.Bottom;
            if (invalid)
            {
                right = area.Right - 16;
                top = area.Top + 16;
                st.StripRight = right;
                st.StripTop = top;
            }

            Left = right - ActualWidth;
            Top = top;
            _placed = true;
        }

        void BrandBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm.Locked) return;
            DragMove();
            _vm.Settings.StripRight = Left + ActualWidth;
            _vm.Settings.StripTop = Top;
            _vm.Save();
        }

        // A tool window does not appear in Alt+Tab or the taskbar.
        void HideFromAltTab()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, (ex | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
        }

        const int GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x80, WS_EX_APPWINDOW = 0x40000;
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
