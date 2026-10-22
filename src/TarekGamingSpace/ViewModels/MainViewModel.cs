using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using TarekGamingSpace.Core;
using TarekGamingSpace.Models;
using TarekGamingSpace.Services;

namespace TarekGamingSpace.ViewModels
{
    /// <summary>What the start / set-timer dialog returns.</summary>
    public sealed class StartChoice
    {
        public SessionMode Mode { get; set; }
        public int Minutes { get; set; }
        /// <summary>Open → Timer only: minutes include the time already played.</summary>
        public bool FromStart { get; set; }
    }

    public sealed class MainViewModel : ObservableObject
    {
        readonly SoundService _sound;

        public MainViewModel(AppState state, SoundService sound)
        {
            State = state;
            _sound = sound;
            Postes = new ObservableCollection<PosteViewModel>(state.Postes.Select(p => new PosteViewModel(this, p)));

            AddPosteCommand = new RelayCommand(AddPoste, () => CanAddPoste);
            ToggleStripCommand = new RelayCommand(() => StripVisible = !StripVisible);
            TestSoundCommand = new RelayCommand(() => _sound.PlayOnce(Settings.Sound));
            ShowPanelCommand = new RelayCommand(() => ShowPanelRequested?.Invoke());

            Loc.I.LanguageChanged += () =>
            {
                Raise(nameof(ToggleStripText));
                RefreshAll();
            };
            RefreshAll();
        }

        public AppState State { get; }
        public Settings Settings => State.Settings;
        public ObservableCollection<PosteViewModel> Postes { get; }

        public ICommand AddPosteCommand { get; }
        public ICommand ToggleStripCommand { get; }
        public ICommand TestSoundCommand { get; }
        public ICommand ShowPanelCommand { get; }

        /// <summary>Shows the start dialog (switching = Open → Timer). Returns null when cancelled.</summary>
        public Func<PosteViewModel, bool, StartChoice> AskStart { get; set; }
        /// <summary>Asks how many minutes to add to a timer. Returns null when cancelled.</summary>
        public Func<PosteViewModel, int?> AskAddTime { get; set; }
        /// <summary>Shows the receipt. Returns true when the poste should be freed.</summary>
        public Func<PosteViewModel, bool> ConfirmStop { get; set; }

        public event Action<PosteViewModel> TimeUp;
        public event Action ShowPanelRequested;

        static DateTime Now => DateTime.UtcNow;

        public void Save() => Storage.Save(State);

        // ---------- clock ----------

        public void Tick()
        {
            var now = Now;
            bool ended = false;
            foreach (var p in Postes)
            {
                if (p.Data.Session != null && SessionMath.CheckEnd(p.Data.Session, now))
                {
                    ended = true;
                    TimeUp?.Invoke(p);
                }
            }
            if (ended) Save();
            foreach (var p in Postes) p.Refresh(now);
            UpdateSound();
            UpdateFade();
        }

        void RefreshAll()
        {
            var now = Now;
            foreach (var p in Postes) p.Refresh(now);
            UpdateFade();
        }

        void UpdateSound() =>
            _sound.SetAlert(Settings.SoundEnabled && Postes.Any(p => p.IsAlerting), Settings.Sound);

        // ---------- session actions ----------

        void Commit(PosteViewModel p)
        {
            Save();
            p.Refresh(Now);
            UpdateSound();
            UpdateFade();
        }

        public void Mutate(PosteViewModel p, Action<SessionData, DateTime> action)
        {
            if (p.Data.Session == null) return;
            action(p.Data.Session, Now);
            Commit(p);
        }

        bool _dialogOpen;

        /// <summary>Runs a dialog unless one is already open (only one dialog at a time).</summary>
        T WithDialog<T>(Func<T> show)
        {
            if (_dialogOpen) return default(T);
            _dialogOpen = true;
            try { return show(); }
            finally { _dialogOpen = false; }
        }

        public void StartSession(PosteViewModel p)
        {
            if (p.Data.Session != null) return;
            var choice = WithDialog(() => AskStart?.Invoke(p, false));
            if (choice == null || p.Data.Session != null) return;
            p.Data.Session = SessionMath.Start(choice.Mode, choice.Minutes, Now);
            Commit(p);
        }

        public void SetTimer(PosteViewModel p)
        {
            if (p.Data.Session == null) return;
            var choice = WithDialog(() => AskStart?.Invoke(p, true));
            if (choice == null || p.Data.Session == null) return;
            SessionMath.SetTimer(p.Data.Session, choice.Minutes, choice.FromStart, Now);
            Commit(p);
        }

        public void AddTime(PosteViewModel p)
        {
            if (p.Data.Session == null || !p.IsTimer) return;
            int? minutes = WithDialog(() => AskAddTime?.Invoke(p));
            if (minutes == null || minutes <= 0) return;
            Mutate(p, (s, now) => SessionMath.AddMinutes(s, minutes.Value, now));
        }

        public void Stop(PosteViewModel p)
        {
            if (p.Data.Session == null) return;
            if (!WithDialog(() => ConfirmStop?.Invoke(p) == true)) return;
            p.Data.Session = null;
            Commit(p);
        }

        public void Acknowledge(PosteViewModel p)
        {
            if (!p.IsAlerting) return;
            p.Data.Session.Acknowledged = true;
            Commit(p);
        }

        // ---------- postes ----------

        public bool CanAddPoste => Postes.Count < Settings.MaxPostes;

        void AddPoste()
        {
            if (!CanAddPoste) return;
            int id = 1;
            while (Postes.Any(p => p.Data.Id == id)) id++;
            var data = new PosteData { Id = id };
            int index = Postes.TakeWhile(p => p.Data.Id < id).Count();
            State.Postes.Insert(index, data);
            var vm = new PosteViewModel(this, data);
            vm.Refresh(Now);
            Postes.Insert(index, vm);
            Raise(nameof(CanAddPoste));
            Save();
        }

        public void DeletePoste(PosteViewModel p)
        {
            if (!p.IsIdle) return;
            State.Postes.Remove(p.Data);
            Postes.Remove(p);
            Raise(nameof(CanAddPoste));
            Save();
        }

        // ---------- settings ----------

        public string HourlyRateText
        {
            get => Settings.HourlyRate.ToString();
            set
            {
                if (!int.TryParse(value, out int v) || v < 0 || v > 1000000) return;
                Settings.HourlyRate = v;
                Raise();
                Save();
                RefreshAll();
            }
        }

        public string WarningMinutesText
        {
            get => Settings.WarningMinutes.ToString();
            set
            {
                if (!int.TryParse(value, out int v) || v < 0 || v > 120) return;
                Settings.WarningMinutes = v;
                Raise();
                Save();
                RefreshAll();
            }
        }

        public string Language
        {
            get => Settings.Language;
            set
            {
                if (value == null || value == Settings.Language) return;
                Settings.Language = value;
                Raise();
                Save();
                Loc.I.Lang = value;
            }
        }

        public bool RunAtStartup
        {
            get => Settings.RunAtStartup;
            set
            {
                Settings.RunAtStartup = value;
                StartupService.Apply(value);
                Raise();
                Save();
            }
        }

        public string Sound
        {
            get => Settings.Sound;
            set
            {
                if (value == null || value == Settings.Sound) return;
                Settings.Sound = value;
                Raise();
                Save();
                UpdateSound();
            }
        }

        public bool SoundEnabled
        {
            get => Settings.SoundEnabled;
            set
            {
                Settings.SoundEnabled = value;
                Raise();
                Save();
                UpdateSound();
            }
        }

        // ---------- opacity ----------

        public double StripOpacity
        {
            get => Settings.Opacity;
            set
            {
                double v = StripFade.Clamp(value, Settings.FadeMode);
                if (v == Settings.Opacity) { Raise(); return; }
                Settings.Opacity = v;
                Raise();
                Raise(nameof(OpacityText));
                Save();
                UpdateFade();
            }
        }

        public string OpacityText => Math.Round(Settings.Opacity * 100) + "%";
        public double MinOpacity => StripFade.Min(Settings.FadeMode);

        public string FadeMode
        {
            get => Settings.FadeMode;
            set
            {
                if (value == null || value == Settings.FadeMode) return;
                Settings.FadeMode = value;
                Raise();
                Raise(nameof(MinOpacity));
                StripOpacity = Settings.Opacity; // "All" has a higher minimum
                Save();
                UpdateFade();
            }
        }

        public bool SolidOnHover
        {
            get => Settings.SolidOnHover;
            set { Settings.SolidOnHover = value; Raise(); Save(); UpdateFade(); }
        }

        public bool SolidOnAlert
        {
            get => Settings.SolidOnAlert;
            set { Settings.SolidOnAlert = value; Raise(); Save(); RefreshAll(); }
        }

        bool _stripHovered;
        /// <summary>Set by the strip window when the mouse enters or leaves it.</summary>
        public bool StripHovered
        {
            get => _stripHovered;
            set { if (Set(ref _stripHovered, value)) UpdateFade(); }
        }

        double _windowOpacity = 1, _backgroundOpacity = 1;
        public double WindowOpacity { get => _windowOpacity; private set => Set(ref _windowOpacity, value); }
        public double BackgroundOpacity
        {
            get => _backgroundOpacity;
            private set { if (Set(ref _backgroundOpacity, value)) Raise(nameof(TextShadow)); }
        }
        /// <summary>A soft shadow keeps the text readable once the panel behind it is see-through.</summary>
        public bool TextShadow => BackgroundOpacity < 1;

        void UpdateFade()
        {
            var f = StripFade.Compute(Settings.Opacity, Settings.FadeMode, Settings.SolidOnHover, StripHovered,
                Settings.SolidOnAlert, Postes.Any(p => p.IsAlertVisual));
            WindowOpacity = f.Window;
            BackgroundOpacity = f.Background;
        }

        public ICommand SetOpacityCommand => _setOpacity ?? (_setOpacity = new RelayCommand(
            o => StripOpacity = Percent(o),
            o => Percent(o) >= MinOpacity));
        static double Percent(object o) => double.Parse((string)o, CultureInfo.InvariantCulture) / 100;
        ICommand _setOpacity;

        public bool AlwaysOnTop
        {
            get => Settings.AlwaysOnTop;
            set { Settings.AlwaysOnTop = value; Raise(); Save(); }
        }

        public bool Locked
        {
            get => Settings.Locked;
            set { Settings.Locked = value; Raise(); Save(); }
        }

        public bool StripVisible
        {
            get => Settings.StripVisible;
            set
            {
                Settings.StripVisible = value;
                Raise();
                Raise(nameof(ToggleStripText));
                Save();
            }
        }

        public string ToggleStripText => StripVisible ? Loc.I["HideAll"] : Loc.I["ShowAll"];
    }
}
