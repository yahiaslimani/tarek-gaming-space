using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TarekGamingSpace.ViewModels;

namespace TarekGamingSpace.Views
{
    /// <summary>
    /// One poste block. State colours and the red blink are applied here in code:
    /// it keeps the XAML simple and the blink brush unfrozen.
    /// </summary>
    public partial class PosteView : UserControl
    {
        static readonly Color Muted = Rgb("#8B9A90"), Text = Rgb("#E6EFE8"), Dim = Rgb("#3A4640"),
            Green = Rgb("#52B043"), Warn = Rgb("#F2A93B"), Alert = Rgb("#FF4D4D"), Paused = Rgb("#C9B458");

        readonly SolidColorBrush _cardBg = new SolidColorBrush(Colors.Transparent);
        PosteViewModel _vm;

        public PosteView()
        {
            InitializeComponent();
            Card.Background = _cardBg;
            DataContextChanged += OnDataContextChanged;
            SizeChanged += (s, e) => UpdateProgress();
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null) _vm.PropertyChanged -= OnVmChanged;
            _vm = DataContext as PosteViewModel;
            if (_vm == null) return;
            _vm.PropertyChanged += OnVmChanged;
            ApplyVisual();
            UpdateProgress();
        }

        void OnVmChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PosteViewModel.Visual)) ApplyVisual();
            else if (e.PropertyName == nameof(PosteViewModel.Progress)) UpdateProgress();
        }

        void UpdateProgress()
        {
            if (_vm == null) return;
            double full = Math.Max(0, ActualWidth - 24);
            ProgressFill.Width = full * _vm.Progress;
        }

        void ApplyVisual()
        {
            string v = _vm?.Visual ?? "Idle";
            Color accent, time;
            switch (v)
            {
                case "Run": accent = Green; time = Text; break;
                case "Warn": accent = Warn; time = Warn; break;
                case "Paused": accent = Paused; time = Paused; break;
                case "Ended":
                case "Ack": accent = Alert; time = Alert; break;
                default: accent = Muted; time = Dim; break;
            }

            var accentBrush = new SolidColorBrush(accent);
            accentBrush.Freeze();
            Stripe.Fill = accentBrush;
            ProgressFill.Background = accentBrush;
            TimeLabel.Foreground = new SolidColorBrush(time);
            Chip.BorderBrush = v == "Idle" ? new SolidColorBrush(Rgb("#2A3530")) : accentBrush;
            ChipLabel.Foreground = v == "Idle" ? new SolidColorBrush(Muted) : accentBrush;

            _cardBg.BeginAnimation(SolidColorBrush.ColorProperty, null);
            if (v == "Ended")
            {
                var blink = new ColorAnimation(Color.FromArgb(0x66, 0xFF, 0x4D, 0x4D), Color.FromArgb(0x10, 0xFF, 0x4D, 0x4D),
                    TimeSpan.FromSeconds(0.5))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                };
                _cardBg.BeginAnimation(SolidColorBrush.ColorProperty, blink);
                Cursor = Cursors.Hand;
            }
            else
            {
                _cardBg.Color = v == "Ack" ? Color.FromArgb(0x24, 0xFF, 0x4D, 0x4D) : Colors.Transparent;
                Cursor = null;
            }
        }

        /// <summary>Clicking a blinking poste silences it (buttons handle their own clicks).</summary>
        void Root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_vm != null && _vm.IsAlerting) _vm.AcknowledgeCommand.Execute(null);
        }

        static Color Rgb(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    }
}
