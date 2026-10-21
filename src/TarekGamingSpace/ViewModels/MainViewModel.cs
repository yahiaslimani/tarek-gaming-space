using System;
using System.Collections.ObjectModel;
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
        }

        void RefreshAll()
        {
            var now = Now;
            foreach (var p in Postes) p.Refresh(now);
        }

        void UpdateSound() =>
            _sound.SetAlert(Settings.SoundEnabled && Postes.Any(p => p.IsAlerting), Settings.Sound);

        // ---------- session actions ----------

        void Commit(PosteViewModel p)
        {
            Save();
            p.Refresh(Now);
            UpdateSound();
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

        public double StripOpacity
        {
            get => Settings.Opacity;
            set
            {
                Settings.Opacity = Math.Max(0.4, Math.Min(1, value));
                Raise();
                Save();
            }
        }

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
