using System;
using System.Windows;
using System.Windows.Input;
using TarekGamingSpace.Core;
using TarekGamingSpace.Models;
using TarekGamingSpace.ViewModels;

namespace TarekGamingSpace.Views
{
    /// <summary>Shown on Stop: time played and the amount to collect. Cancel keeps the session running.</summary>
    public partial class ReceiptDialog : Window
    {
        public ReceiptDialog(PosteViewModel poste, SessionData s, double playedSeconds, int rate)
        {
            InitializeComponent();
            var L = Loc.I;
            TitleText.Text = $"{L["Receipt"]} · {poste.DisplayName}";
            ModeKey.Text = L["RMode"];
            ModeVal.Text = s.Mode == SessionMode.Open
                ? L["Open"]
                : $"{L["Timer"]} · {Math.Round(s.BookedSeconds / 60)} {L["Min"]}";
            PlayedKey.Text = L["RPlayed"];
            PlayedVal.Text = SessionMath.Hms(playedSeconds);
            RateKey.Text = L["RRate"];
            RateVal.Text = $"{rate} {L["DZD"]}";
            TotalKey.Text = L["RTotal"];
            TotalVal.Text = $"{SessionMath.Price(playedSeconds, rate)} {L["DZD"]}";
            CancelBtn.Content = L["Cancel"];
            DoneBtn.Content = L["Done"];
            Loaded += (sender, e) => DoneBtn.Focus();
        }

        void DoneBtn_Click(object sender, RoutedEventArgs e) => DialogResult = true;
        void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }
    }
}
