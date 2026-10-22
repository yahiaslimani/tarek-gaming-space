using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TarekGamingSpace.Core;
using TarekGamingSpace.ViewModels;

namespace TarekGamingSpace.Views
{
    /// <summary>Adds any number of minutes to a timer session, as the client asks.</summary>
    public partial class AddTimeDialog : Window
    {
        static readonly int[] PresetMinutes = { 5, 10, 15, 30, 45, 60 };

        readonly double _bookedSeconds, _remainingSeconds;
        readonly int _rate;
        int _minutes = 15;
        bool _refreshing;

        /// <summary>Minutes to add, set when the dialog closes with Add.</summary>
        public int Minutes { get; private set; }

        public AddTimeDialog(PosteViewModel poste, double bookedSeconds, double playedSeconds, int rate)
        {
            InitializeComponent();
            _bookedSeconds = bookedSeconds;
            _remainingSeconds = Math.Max(0, bookedSeconds - playedSeconds);
            _rate = rate;

            var L = Loc.I;
            TitleText.Text = $"{L["AddTime"]} · {poste.DisplayName}";
            NowText.Text = _remainingSeconds > 0
                ? $"{L["TimeLeft"]}: {L.HoursMinutes(_remainingSeconds + 59)} · {L["Booked"]} {Math.Round(bookedSeconds / 60)} {L["Min"]}"
                : $"{L["TimeUp"]} · {L["Booked"]} {Math.Round(bookedSeconds / 60)} {L["Min"]}";
            CustomLabel.Text = L["Custom"];
            MinLabel.Text = L["Min"];
            CancelBtn.Content = L["Cancel"];
            GoBtn.Content = L["Add"];

            foreach (int m in PresetMinutes)
            {
                var b = new ToggleButton
                {
                    Content = $"+{m} {L["Min"]}",
                    Tag = m,
                    Style = (Style)FindResource("Choice"),
                    Margin = new Thickness(3),
                };
                b.Checked += (s, e) =>
                {
                    if (_refreshing) return;
                    _minutes = (int)((ToggleButton)s).Tag;
                    CustomBox.Text = _minutes.ToString();
                };
                b.Unchecked += (s, e) => { if (!_refreshing) Refresh(); };
                Presets.Children.Add(b);
            }

            CustomBox.Text = _minutes.ToString();
            Refresh();
            // Ready to type: the client asks for 25 min, type 25 and press Enter.
            Loaded += (s, e) => { CustomBox.Focus(); CustomBox.SelectAll(); };
        }

        void Refresh()
        {
            var L = Loc.I;
            _refreshing = true;
            foreach (ToggleButton b in Presets.Children.OfType<ToggleButton>())
                b.IsChecked = (int)b.Tag == _minutes;
            _refreshing = false;

            double total = _bookedSeconds + _minutes * 60;
            EstimateText.Text = _minutes > 0
                ? $"{L["EndsIn"]} {Math.Ceiling((_remainingSeconds + _minutes * 60) / 60)} {L["Min"]} · {L["Total"]} {Math.Round(total / 60)} {L["Min"]} · ≈ {SessionMath.Price(total, _rate)} {L["DZD"]}"
                : "";
            GoBtn.IsEnabled = _minutes > 0;
        }

        void CustomBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _minutes = int.TryParse(CustomBox.Text, out int m) ? Math.Max(0, m) : 0;
            Refresh();
        }

        void CustomBox_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !e.Text.All(char.IsDigit);

        void GoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!GoBtn.IsEnabled) return;
            Minutes = _minutes;
            DialogResult = true;
        }

        void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }
    }
}
