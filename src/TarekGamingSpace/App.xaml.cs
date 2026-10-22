using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using TarekGamingSpace.Core;
using TarekGamingSpace.Models;
using TarekGamingSpace.Services;
using TarekGamingSpace.ViewModels;
using TarekGamingSpace.Views;

namespace TarekGamingSpace
{
    public partial class App : Application
    {
        // Also used by the installer (AppMutex) to detect that the app is running.
        const string MutexName = "TarekGamingSpace_SingleInstance";
        const string ShowPanelEventName = "TarekGamingSpace_ShowPanel";

        Mutex _mutex;
        EventWaitHandle _showPanelEvent;
        MainViewModel _vm;
        SoundService _sound;
        TrayService _tray;
        StripWindow _strip;
        ControlPanelWindow _panel;
        DispatcherTimer _clock;

        public bool IsExiting { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Only one copy: a second launch asks the first one to show its control panel, then quits.
            // A development run with its own data folder (TGS_DATA_DIR) counts as a separate copy.
            string suffix = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TGS_DATA_DIR")) ? "" : "_Dev";
            _mutex = new Mutex(true, MutexName + suffix, out bool firstInstance);
            if (!firstInstance)
            {
                try { EventWaitHandle.OpenExisting(ShowPanelEventName + suffix).Set(); } catch { }
                Shutdown();
                return;
            }
            _showPanelEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowPanelEventName + suffix);
            new Thread(() =>
            {
                while (_showPanelEvent.WaitOne())
                    Dispatcher.BeginInvoke(new Action(ShowPanel));
            }) { IsBackground = true }.Start();

            DispatcherUnhandledException += (s, ex) =>
            {
                Log.Error("Unhandled error", ex.Exception);
                ex.Handled = true;   // a timer app in a shop must keep running
            };

            RegisterFonts();

            bool firstRun = !Storage.Exists;
            var state = Storage.Load();
            Loc.I.Lang = state.Settings.Language;

            _sound = new SoundService();
            _vm = new MainViewModel(state, _sound)
            {
                AskStart = AskStart,
                AskAddTime = AskAddTime,
                ConfirmStop = ConfirmStop,
            };
            _vm.TimeUp += p => _tray.Notify(Loc.I["AppName"], Loc.I.Format("ToastTimeUp", p.DisplayName));
            _vm.ShowPanelRequested += ShowPanel;
            _vm.PropertyChanged += (s, ev) =>
            {
                if (ev.PropertyName == nameof(MainViewModel.StripVisible)) ApplyStripVisibility();
            };
            Loc.I.LanguageChanged += () => _tray.UpdateTexts(_vm.StripVisible);

            // Keep the startup entry pointing at this exe (the path can change after a reinstall).
            StartupService.Apply(state.Settings.RunAtStartup);
            if (firstRun) _vm.Save();

            _tray = new TrayService();
            _tray.UpdateTexts(_vm.StripVisible);
            _tray.ToggleStripRequested += () => _vm.StripVisible = !_vm.StripVisible;
            _tray.PanelRequested += ShowPanel;
            _tray.ExitRequested += ExitApp;

            _strip = new StripWindow(_vm);
            _panel = new ControlPanelWindow(_vm);
            ApplyStripVisibility();

            // 4 ticks a second so the seconds change on time; the work per tick is tiny.
            _clock = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(250) };
            _clock.Tick += (s, ev) => _vm.Tick();
            _clock.Start();
            _vm.Tick();

            bool fromStartup = e.Args.Any(a => a == StartupService.StartupArg);
            if (!fromStartup) ShowPanel();
        }

        void RegisterFonts()
        {
            var baseUri = new Uri("pack://application:,,,/");
            // Embedded faces have Latin glyphs only; Arabic falls back to Segoe UI, which ships with Windows.
            Resources["DisplayFont"] = new FontFamily(baseUri, "./Assets/Fonts/#Chakra Petch, Segoe UI");
            Resources["DigitsFont"] = new FontFamily(baseUri, "./Assets/Fonts/#Share Tech Mono, Consolas");
            Resources["BodyFont"] = new FontFamily("Segoe UI");
        }

        void ApplyStripVisibility()
        {
            if (_vm.StripVisible) _strip.Show();
            else _strip.Hide();
            _tray?.UpdateTexts(_vm.StripVisible);
        }

        void ShowPanel() => _panel?.ShowAndFocus();

        StartChoice AskStart(PosteViewModel p, bool switching)
        {
            double played = p.Data.Session == null ? 0 : SessionMath.Played(p.Data.Session, DateTime.UtcNow);
            var dlg = new StartDialog(p, switching, played, _vm.Settings.HourlyRate);
            return dlg.ShowDialog() == true ? dlg.Result : null;
        }

        int? AskAddTime(PosteViewModel p)
        {
            var s = p.Data.Session;
            var dlg = new AddTimeDialog(p, s.BookedSeconds, SessionMath.Played(s, DateTime.UtcNow), _vm.Settings.HourlyRate);
            return dlg.ShowDialog() == true ? dlg.Minutes : (int?)null;
        }

        bool ConfirmStop(PosteViewModel p)
        {
            var s = p.Data.Session;
            double played = SessionMath.Played(s, DateTime.UtcNow);
            var dlg = new ReceiptDialog(p, s, played, _vm.Settings.HourlyRate);
            return dlg.ShowDialog() == true;
        }

        void ExitApp()
        {
            // Running sessions are saved as timestamps, so they continue correctly on the next start.
            IsExiting = true;
            _clock?.Stop();
            _vm?.Save();
            _sound?.Dispose();
            _tray?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (!IsExiting)
            {
                _vm?.Save();
                _tray?.Dispose();
            }
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
