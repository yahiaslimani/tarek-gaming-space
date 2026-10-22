using System;

namespace TarekGamingSpace.Core
{
    public struct FadeLevels
    {
        public FadeLevels(double window, double background) { Window = window; Background = background; }
        /// <summary>Opacity of the whole strip window.</summary>
        public double Window { get; }
        /// <summary>Opacity of the strip's dark panel only.</summary>
        public double Background { get; }
    }

    /// <summary>How see-through the strip is right now. Pure so it can be unit-tested.</summary>
    public static class StripFade
    {
        /// <summary>Below this the dark panel is barely there; with "All" the clock itself fades, so it stops higher.</summary>
        public static double Min(string fadeMode) => fadeMode == "All" ? 0.4 : 0.2;

        /// <summary>Keeps the value in range and on 5% steps.</summary>
        public static double Clamp(double opacity, string fadeMode) =>
            Math.Max(Min(fadeMode), Math.Min(1, Math.Round(opacity * 20) / 20));

        /// <summary>
        /// The mouse over the strip makes everything solid; an alert (last minutes or time up) makes
        /// the whole strip solid in "All" mode. In "Background" mode the alerting block gets its own
        /// solid panel instead (see PosteViewModel.SolidBackground).
        /// </summary>
        public static FadeLevels Compute(double opacity, string fadeMode,
            bool solidOnHover, bool hovered, bool solidOnAlert, bool anyAlert)
        {
            opacity = Clamp(opacity, fadeMode);
            if (solidOnHover && hovered) return new FadeLevels(1, 1);
            if (fadeMode == "All") return new FadeLevels(solidOnAlert && anyAlert ? 1 : opacity, 1);
            return new FadeLevels(1, opacity);
        }
    }
}
