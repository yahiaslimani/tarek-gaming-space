using System;
using TarekGamingSpace.Models;

namespace TarekGamingSpace.Core
{
    /// <summary>All time and money rules for a session, kept pure so they can be tested.</summary>
    public static class SessionMath
    {
        public static double Played(SessionData s, DateTime nowUtc)
        {
            double running = s.RunningSinceUtc.HasValue
                ? Math.Max(0, (nowUtc - s.RunningSinceUtc.Value).TotalSeconds)
                : 0;
            return s.AccumulatedSeconds + running;
        }

        public static double Remaining(SessionData s, DateTime nowUtc) =>
            Math.Max(0, s.BookedSeconds - Played(s, nowUtc));

        /// <summary>Played time × hourly rate, always rounded down to the previous 10 DZD (225 → 220, 99 → 90).</summary>
        public static int Price(double playedSeconds, int hourlyRate) =>
            // The tiny epsilon keeps exact amounts exact (40 min at 150/h is 100, not 99.999… → 90).
            (int)(Math.Floor(Math.Max(0, playedSeconds) * hourlyRate / 36000.0 + 1e-9) * 10);

        public static SessionData Start(SessionMode mode, int minutes, DateTime nowUtc) => new SessionData
        {
            Mode = mode,
            BookedSeconds = mode == SessionMode.Timer ? minutes * 60 : 0,
            AccumulatedSeconds = 0,
            RunningSinceUtc = nowUtc,
        };

        public static void Pause(SessionData s, DateTime nowUtc)
        {
            if (s.Ended || !s.RunningSinceUtc.HasValue) return;
            s.AccumulatedSeconds = Played(s, nowUtc);
            s.RunningSinceUtc = null;
        }

        public static void Resume(SessionData s, DateTime nowUtc)
        {
            if (s.Ended || s.RunningSinceUtc.HasValue) return;
            s.RunningSinceUtc = nowUtc;
        }

        /// <summary>Marks a timer as ended once its booked time is used. Returns true only at the moment it ends.</summary>
        public static bool CheckEnd(SessionData s, DateTime nowUtc)
        {
            if (s.Mode != SessionMode.Timer || s.Ended) return false;
            if (Played(s, nowUtc) < s.BookedSeconds) return false;
            s.AccumulatedSeconds = s.BookedSeconds;
            s.RunningSinceUtc = null;
            s.Ended = true;
            s.Acknowledged = false;
            return true;
        }

        public static void AddMinutes(SessionData s, int minutes, DateTime nowUtc)
        {
            if (s.Mode != SessionMode.Timer) return;
            s.BookedSeconds += minutes * 60;
            if (s.Ended) Restart(s, nowUtc);
        }

        public static void MakeOpen(SessionData s, DateTime nowUtc)
        {
            s.Mode = SessionMode.Open;
            s.BookedSeconds = 0;
            if (s.Ended) Restart(s, nowUtc);
        }

        /// <summary>
        /// Open → Timer. With <paramref name="fromStart"/> the minutes include the time already played;
        /// otherwise they are added on top. Played time is always kept (and charged).
        /// Returns false if the resulting limit would already be over.
        /// </summary>
        public static bool SetTimer(SessionData s, int minutes, bool fromStart, DateTime nowUtc)
        {
            double played = Played(s, nowUtc);
            double booked = fromStart ? minutes * 60 : played + minutes * 60;
            if (booked <= played) return false;
            s.Mode = SessionMode.Timer;
            s.BookedSeconds = booked;
            return true;
        }

        static void Restart(SessionData s, DateTime nowUtc)
        {
            s.Ended = false;
            s.Acknowledged = false;
            s.RunningSinceUtc = nowUtc;
        }

        public static string Hms(double seconds)
        {
            long t = (long)Math.Max(0, seconds);
            return $"{t / 3600:00}:{t % 3600 / 60:00}:{t % 60:00}";
        }
    }
}
