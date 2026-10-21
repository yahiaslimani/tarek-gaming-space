using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TarekGamingSpace.Models
{
    public enum SessionMode { Open = 0, Timer = 1 }

    [DataContract]
    public sealed class AppState
    {
        [DataMember] public Settings Settings { get; set; }
        [DataMember] public List<PosteData> Postes { get; set; }

        public static AppState CreateDefault()
        {
            var state = new AppState { Settings = new Settings(), Postes = new List<PosteData>() };
            for (int i = 1; i <= 4; i++) state.Postes.Add(new PosteData { Id = i });
            return state;
        }

        /// <summary>Fills anything missing after loading an older or damaged file.</summary>
        public void Normalize()
        {
            if (Settings == null) Settings = new Settings();
            if (Postes == null) Postes = new List<PosteData>();
            Postes.RemoveAll(p => p == null);
            Postes.Sort((a, b) => a.Id.CompareTo(b.Id));
            Settings.Normalize();
        }
    }

    [DataContract]
    public sealed class Settings
    {
        public const int MaxPostes = 5;

        [DataMember] public int HourlyRate { get; set; }
        [DataMember] public string Language { get; set; }
        [DataMember] public bool RunAtStartup { get; set; }
        [DataMember] public int WarningMinutes { get; set; }
        [DataMember] public string Sound { get; set; }
        [DataMember] public double Opacity { get; set; }
        [DataMember] public bool AlwaysOnTop { get; set; }
        [DataMember] public bool Locked { get; set; }
        [DataMember] public bool StripVisible { get; set; }
        // The strip is anchored by its right edge so Poste 1 stays put when postes are added on the left.
        [DataMember] public double StripRight { get; set; }
        [DataMember] public double StripTop { get; set; }

        public Settings() => SetDefaults();

        [OnDeserializing]
        void OnDeserializing(StreamingContext _) => SetDefaults();

        void SetDefaults()
        {
            HourlyRate = 150;
            Language = "en";
            RunAtStartup = true;
            WarningMinutes = 5;
            Sound = "Chime";
            Opacity = 1.0;
            AlwaysOnTop = true;
            Locked = false;
            StripVisible = true;
            StripRight = double.NaN;
            StripTop = double.NaN;
        }

        public void Normalize()
        {
            if (HourlyRate < 0) HourlyRate = 0;
            if (Language != "ar") Language = "en";
            if (WarningMinutes < 0) WarningMinutes = 0;
            if (Sound != "Alarm" && Sound != "Arcade") Sound = "Chime";
            if (double.IsNaN(Opacity) || Opacity < 0.4 || Opacity > 1) Opacity = 1.0;
        }
    }

    [DataContract]
    public sealed class PosteData
    {
        [DataMember] public int Id { get; set; }
        /// <summary>Custom name, or null to use the translated default ("Poste 3").</summary>
        [DataMember] public string Name { get; set; }
        [DataMember] public SessionData Session { get; set; }
    }

    /// <summary>
    /// A session is stored as timestamps, not as a running counter, so the correct time
    /// comes back after a restart or crash.
    /// </summary>
    [DataContract]
    public sealed class SessionData
    {
        [DataMember] public SessionMode Mode { get; set; }
        /// <summary>Booked length in seconds (Timer mode only).</summary>
        [DataMember] public double BookedSeconds { get; set; }
        /// <summary>Seconds played before the current running stretch.</summary>
        [DataMember] public double AccumulatedSeconds { get; set; }
        /// <summary>When the current running stretch began; null while paused or ended.</summary>
        [DataMember] public DateTime? RunningSinceUtc { get; set; }
        [DataMember] public bool Ended { get; set; }
        [DataMember] public bool Acknowledged { get; set; }
    }
}
