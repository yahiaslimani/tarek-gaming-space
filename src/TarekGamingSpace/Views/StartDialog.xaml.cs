using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TarekGamingSpace.Core;
using TarekGamingSpace.Models;
using TarekGamingSpace.ViewModels;

namespace TarekGamingSpace.Views
{
    /// <summary>
    /// Start a session (Open or Timer), or — when <c>switching</c> — turn an open session into a timer,
    /// either adding the minutes on top or counting them from the start.
    /// </summary>
    public partial class StartDialog : Window
    {
        static readonly int[] PresetMinutes = { 15, 30, 45, 60, 90, 120 };

        readonly bool _switching;
        readonly double _playedMinutes;
        readonly int _rate;
        bool _choiceB;   // start: Timer · switching: Total from the start
        int _minutes = 30;

        public StartChoice Result { get; private set; }

        public StartDialog(PosteViewModel poste, bool switching, double playedSeconds, int rate)
        {
            InitializeComponent();
            _switching = switching;
            _playedMinutes = switching ? playedSeconds / 60 : 0;
            _rate = rate;

            var L = Loc.I;
            TitleText.Text = $"{(switching ? L["DlgSwitch"] : L["DlgStart"])} · {poste.DisplayName}";
            if (switching)
            {
                PlayedText.Visibility = Visibility.Visible;
                PlayedText.Text = $"{L["AlreadyPlayed"]}: {L.HoursMinutes(playedSeconds)} · {SessionMath.Price(playedSeconds, rate)} {L["DZD"]}";
                ChoiceATitle.Text = L["AddOnTop"];
                ChoiceADesc.Text = L["AddOnTopDesc"];
                ChoiceBTitle.Text = L["TotalFromStart"];
                ChoiceBDesc.Text = L["TotalFromStartDesc"];
            }
            else
            {
                ChoiceATitle.Text = L["OpenChip"];
                ChoiceADesc.Text = L["OpenDesc"];
                ChoiceBTitle.Text = L["Timer"];
                ChoiceBDesc.Text = L["TimerDesc"];
            }
            System.Windows.Automation.AutomationProperties.SetName(ChoiceA, ChoiceATitle.Text);
            System.Windows.Automation.AutomationProperties.SetName(ChoiceB, ChoiceBTitle.Text);
            CustomLabel.Text = L["Custom"];
            MinLabel.Text = L["Min"];
            CancelBtn.Content = L["Cancel"];
            GoBtn.Content = switching ? L["SetTimer"] : L["Start"];

            foreach (int m in PresetMinutes)
            {
                var b = new ToggleButton
                {
                    Content = $"{m} {L["Min"]}",
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
                b.Unchecked += KeepSelection;
                Presets.Children.Add(b);
            }

            CustomBox.Text = _minutes.ToString();
            Refresh();
            Loaded += (s, e) => ChoiceA.Focus();
        }

        double TotalMinutes => _switching && _choiceB ? _minutes : _playedMinutes + _minutes;
        bool TimerSelected => _switching || _choiceB;
        bool TooShort => _switching && TotalMinutes <= _playedMinutes;

        bool _refreshing;

        void Refresh()
        {
            var L = Loc.I;
            _refreshing = true;
            ChoiceA.IsChecked = !_choiceB;
            ChoiceB.IsChecked = _choiceB;
            foreach (ToggleButton b in Presets.Children.OfType<ToggleButton>())
                b.IsChecked = (int)b.Tag == _minutes;
            _refreshing = false;
            TimerPanel.Visibility = TimerSelected ? Visibility.Visible : Visibility.Collapsed;

            if (TooShort)
            {
                EstimateText.Text = L["TooShort"];
            }
            else
            {
                string price = $"≈ {SessionMath.Price(TotalMinutes * 60, _rate)} {L["DZD"]}";
                EstimateText.Text = _switching
                    ? $"{L["EndsIn"]} {Math.Ceiling(TotalMinutes - _playedMinutes)} {L["Min"]} · {L["Total"]} {Math.Ceiling(TotalMinutes)} {L["Min"]} · {price}"
                    : price;
            }
            GoBtn.IsEnabled = !TimerSelected || (_minutes > 0 && !TooShort);
        }

        // Checked (not Click) so keyboard, mouse and screen readers all behave the same.
        void ChoiceA_Checked(object sender, RoutedEventArgs e) { if (!_refreshing) { _choiceB = false; Refresh(); } }
        void ChoiceB_Checked(object sender, RoutedEventArgs e) { if (!_refreshing) { _choiceB = true; Refresh(); } }

        /// <summary>Clicking the selected option again must not leave nothing selected.</summary>
        void KeepSelection(object sender, RoutedEventArgs e) { if (!_refreshing) Refresh(); }

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
            Result = new StartChoice
            {
                Mode = TimerSelected ? SessionMode.Timer : SessionMode.Open,
                Minutes = _minutes,
                FromStart = _switching && _choiceB,
            };
            DialogResult = true;
        }

        void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }
    }
}
