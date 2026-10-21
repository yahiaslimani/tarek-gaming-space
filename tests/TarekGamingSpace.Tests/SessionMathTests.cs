using System;
using TarekGamingSpace.Core;
using TarekGamingSpace.Models;
using Xunit;

namespace TarekGamingSpace.Tests
{
    public class SessionMathTests
    {
        static readonly DateTime T0 = new DateTime(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
        static DateTime At(double minutes) => T0.AddMinutes(minutes);

        [Theory]
        [InlineData(37, 150, 90)]    // 92.5 → 90
        [InlineData(38, 150, 90)]    // 95 → 90
        [InlineData(39.6, 150, 90)]  // 99 → 90
        [InlineData(60, 150, 150)]
        [InlineData(40, 150, 100)]   // exact amounts stay exact
        [InlineData(90, 150, 220)]   // 225 → 220
        [InlineData(0, 150, 0)]
        [InlineData(3, 150, 0)]      // 7.5 → 0
        [InlineData(72, 120, 140)]   // 144 → 140
        [InlineData(20, 120, 40)]    // exactly 40
        public void Price_rounds_down_to_previous_10(double minutes, int rate, int expected) =>
            Assert.Equal(expected, SessionMath.Price(minutes * 60, rate));

        [Fact]
        public void Open_session_counts_up()
        {
            var s = SessionMath.Start(SessionMode.Open, 0, T0);
            Assert.Equal(25 * 60, SessionMath.Played(s, At(25)), 3);
        }

        [Fact]
        public void Paused_time_is_not_counted()
        {
            var s = SessionMath.Start(SessionMode.Open, 0, T0);
            SessionMath.Pause(s, At(10));
            SessionMath.Resume(s, At(25));   // 15 min break
            Assert.Equal(20 * 60, SessionMath.Played(s, At(35)), 3);
        }

        [Fact]
        public void Timer_ends_once_and_freezes_at_booked_time()
        {
            var s = SessionMath.Start(SessionMode.Timer, 30, T0);
            Assert.False(SessionMath.CheckEnd(s, At(29.9)));
            Assert.True(SessionMath.CheckEnd(s, At(30)));
            Assert.False(SessionMath.CheckEnd(s, At(31)));
            Assert.True(s.Ended);
            Assert.Equal(30 * 60, SessionMath.Played(s, At(90)), 3);   // no overtime counting
        }

        [Fact]
        public void Timer_that_ran_out_while_laptop_was_off_is_ended_on_restart()
        {
            var s = SessionMath.Start(SessionMode.Timer, 30, T0);
            Assert.True(SessionMath.CheckEnd(s, At(120)));
            Assert.False(s.Acknowledged);
        }

        [Fact]
        public void Leaving_early_charges_only_time_played()
        {
            var s = SessionMath.Start(SessionMode.Timer, 60, T0);
            Assert.Equal(100, SessionMath.Price(SessionMath.Played(s, At(40)), 150));
        }

        [Fact]
        public void Add15_after_end_restarts_and_keeps_played_time()
        {
            var s = SessionMath.Start(SessionMode.Timer, 30, T0);
            SessionMath.CheckEnd(s, At(32));
            SessionMath.AddMinutes(s, 15, At(33));
            Assert.False(s.Ended);
            Assert.Equal(45 * 60, s.BookedSeconds, 3);
            Assert.Equal(35 * 60, SessionMath.Played(s, At(38)), 3);
            Assert.True(SessionMath.CheckEnd(s, At(48)));
        }

        [Fact]
        public void MakeOpen_after_end_keeps_counting()
        {
            var s = SessionMath.Start(SessionMode.Timer, 30, T0);
            SessionMath.CheckEnd(s, At(30));
            SessionMath.MakeOpen(s, At(31));
            Assert.Equal(SessionMode.Open, s.Mode);
            Assert.Equal(40 * 60, SessionMath.Played(s, At(41)), 3);
        }

        [Fact]
        public void SetTimer_add_on_top()
        {
            var s = SessionMath.Start(SessionMode.Open, 0, T0);
            Assert.True(SessionMath.SetTimer(s, 30, fromStart: false, At(20)));
            Assert.Equal(50 * 60, s.BookedSeconds, 3);
            Assert.Equal(30 * 60, SessionMath.Remaining(s, At(20)), 3);
        }

        [Fact]
        public void SetTimer_total_from_start()
        {
            var s = SessionMath.Start(SessionMode.Open, 0, T0);
            Assert.True(SessionMath.SetTimer(s, 60, fromStart: true, At(20)));
            Assert.Equal(60 * 60, s.BookedSeconds, 3);
            Assert.Equal(40 * 60, SessionMath.Remaining(s, At(20)), 3);
        }

        [Fact]
        public void SetTimer_total_shorter_than_played_is_refused()
        {
            var s = SessionMath.Start(SessionMode.Open, 0, T0);
            Assert.False(SessionMath.SetTimer(s, 15, fromStart: true, At(20)));
            Assert.Equal(SessionMode.Open, s.Mode);
        }

        [Fact]
        public void Clock_going_backwards_never_gives_negative_time()
        {
            var s = SessionMath.Start(SessionMode.Open, 0, T0);
            Assert.Equal(0, SessionMath.Played(s, At(-5)), 3);
        }

        [Theory]
        [InlineData(0, "00:00:00")]
        [InlineData(4325, "01:12:05")]
        [InlineData(-3, "00:00:00")]
        public void Hms_formats(double seconds, string expected) =>
            Assert.Equal(expected, SessionMath.Hms(seconds));
    }
}
