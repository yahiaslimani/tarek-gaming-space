using System;
using System.Windows.Input;
using TarekGamingSpace.Core;
using TarekGamingSpace.Models;

namespace TarekGamingSpace.ViewModels
{
    public enum PosteState { Idle, Running, Paused, Ended }

    public sealed class PosteViewModel : ObservableObject
    {
        readonly MainViewModel _main;

        public PosteViewModel(MainViewModel main, PosteData data)
        {
            _main = main;
            Data = data;
            StartCommand = new RelayCommand(() => _main.StartSession(this));
            PauseCommand = new RelayCommand(() => _main.Mutate(this, SessionMath.Pause));
            ResumeCommand = new RelayCommand(() => _main.Mutate(this, SessionMath.Resume));
            AddTimeCommand = new RelayCommand(() => _main.AddTime(this));
            MakeOpenCommand = new RelayCommand(() => _main.Mutate(this, SessionMath.MakeOpen));
            SetTimerCommand = new RelayCommand(() => _main.SetTimer(this));
            StopCommand = new RelayCommand(() => _main.Stop(this));
            AcknowledgeCommand = new RelayCommand(() => _main.Acknowledge(this));
            DeleteCommand = new RelayCommand(() => _main.DeletePoste(this), () => IsIdle);
        }

        public PosteData Data { get; }
        public MainViewModel Main => _main;
        SessionData S => Data.Session;

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand AddTimeCommand { get; }
        public ICommand MakeOpenCommand { get; }
        public ICommand SetTimerCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand AcknowledgeCommand { get; }
        public ICommand DeleteCommand { get; }

        public PosteState State =>
            S == null ? PosteState.Idle
            : S.Ended ? PosteState.Ended
            : S.RunningSinceUtc.HasValue ? PosteState.Running
            : PosteState.Paused;

        public bool IsIdle => S == null;
        public bool IsTimer => S != null && S.Mode == SessionMode.Timer;
        public bool IsAlerting => S != null && S.Ended && !S.Acknowledged;
        /// <summary>Orange or red: last minutes, or time's up (blinking or silenced).</summary>
        public bool IsAlertVisual => Visual == "Warn" || Visual == "Ended" || Visual == "Ack";

        string DefaultName => $"{Loc.I["Poste"]} {Data.Id}";
        public string DisplayName => string.IsNullOrWhiteSpace(Data.Name) ? DefaultName : Data.Name.Trim();

        /// <summary>Bound to the rename box in the control panel. Clearing it brings back the default name.</summary>
        public string NameEdit
        {
            get => DisplayName;
            set
            {
                string v = value?.Trim();
                Data.Name = string.IsNullOrEmpty(v) || v == DefaultName ? null : v;
                Raise();
                Raise(nameof(DisplayName));
                _main.Save();
            }
        }

        // ---- display state, recomputed on every tick ----
        string _timeText, _chipText, _moneyText, _subText, _visual, _lastName;
        double _progress;
        bool _solidBackground, _canStart, _canPause, _canResume, _canAddTime, _canMakeOpen, _canSetTimer, _canStop, _showProgress, _isIdleShown;

        public string TimeText { get => _timeText; private set => Set(ref _timeText, value); }
        public string ChipText { get => _chipText; private set => Set(ref _chipText, value); }
        public string MoneyText { get => _moneyText; private set => Set(ref _moneyText, value); }
        public string SubText { get => _subText; private set => Set(ref _subText, value); }
        /// <summary>Idle, Run, Warn, Paused, Ended (blinking) or Ack (time's up, silenced).</summary>
        public string Visual { get => _visual; private set => Set(ref _visual, value); }
        public double Progress { get => _progress; private set => Set(ref _progress, value); }
        /// <summary>An alerting block keeps its own dark panel when the strip background is faded.</summary>
        public bool SolidBackground { get => _solidBackground; private set => Set(ref _solidBackground, value); }
        public bool CanStart { get => _canStart; private set => Set(ref _canStart, value); }
        public bool CanPause { get => _canPause; private set => Set(ref _canPause, value); }
        public bool CanResume { get => _canResume; private set => Set(ref _canResume, value); }
        public bool CanAddTime { get => _canAddTime; private set => Set(ref _canAddTime, value); }
        public bool CanMakeOpen { get => _canMakeOpen; private set => Set(ref _canMakeOpen, value); }
        public bool CanSetTimer { get => _canSetTimer; private set => Set(ref _canSetTimer, value); }
        public bool CanStop { get => _canStop; private set => Set(ref _canStop, value); }
        public bool ShowProgress { get => _showProgress; private set => Set(ref _showProgress, value); }
        public bool IsIdleShown { get => _isIdleShown; private set => Set(ref _isIdleShown, value); }

        public void Refresh(DateTime nowUtc)
        {
            var L = Loc.I;
            var st = State;
            var settings = _main.Settings;

            if (_lastName != DisplayName)
            {
                _lastName = DisplayName;
                Raise(nameof(DisplayName));
                Raise(nameof(NameEdit));
            }

            if (S == null)
            {
                TimeText = "00:00:00";
                ChipText = L["Free"];
                MoneyText = "—";
                SubText = "";
                Progress = 0;
                Visual = "Idle";
            }
            else
            {
                double played = SessionMath.Played(S, nowUtc);
                bool timer = S.Mode == SessionMode.Timer;
                double remaining = timer ? Math.Max(0, S.BookedSeconds - played) : 0;

                // Count down shows 00:00:01 until the very end, count up shows whole seconds played.
                TimeText = timer ? SessionMath.Hms(Math.Ceiling(remaining - 1e-6)) : SessionMath.Hms(played);
                ChipText = timer ? $"{Math.Round(S.BookedSeconds / 60)} {L["Min"]}" : L["OpenChip"];
                MoneyText = SessionMath.Price(played, settings.HourlyRate).ToString();
                Progress = timer && S.BookedSeconds > 0 ? Math.Min(1, played / S.BookedSeconds) : 0;

                if (st == PosteState.Paused) SubText = L["Paused"];
                else if (st == PosteState.Ended) SubText = S.Acknowledged ? L["TimeUp"] : L["ClickToSilence"];
                else if (timer) SubText = L.Format("LeftFmt", L.HoursMinutes(remaining + 59));
                else SubText = L.Format("PlayedFmt", L.HoursMinutes(played));

                if (st == PosteState.Paused) Visual = "Paused";
                else if (st == PosteState.Ended) Visual = S.Acknowledged ? "Ack" : "Ended";
                else if (timer && settings.WarningMinutes > 0 && remaining <= settings.WarningMinutes * 60) Visual = "Warn";
                else Visual = "Run";
            }

            SolidBackground = settings.SolidOnAlert && IsAlertVisual;
            CanStart = st == PosteState.Idle;
            CanPause = st == PosteState.Running;
            CanResume = st == PosteState.Paused;
            CanAddTime = IsTimer && (st == PosteState.Running || st == PosteState.Ended);
            CanMakeOpen = CanAddTime;
            CanSetTimer = !IsTimer && st == PosteState.Running;
            CanStop = st != PosteState.Idle;
            ShowProgress = IsTimer;
            IsIdleShown = st == PosteState.Idle;
        }
    }
}
