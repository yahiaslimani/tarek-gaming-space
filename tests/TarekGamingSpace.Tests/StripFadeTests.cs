using TarekGamingSpace.Core;
using Xunit;

namespace TarekGamingSpace.Tests
{
    public class StripFadeTests
    {
        [Fact]
        public void BackgroundMode_FadesOnlyThePanel() =>
            Assert.Equal(new FadeLevels(1.0, 0.5), StripFade.Compute(0.5, "Background", true, false, true, false));

        [Fact]
        public void AllMode_FadesTheWholeStrip() =>
            Assert.Equal(new FadeLevels(0.5, 1.0), StripFade.Compute(0.5, "All", true, false, true, false));

        [Theory]
        [InlineData("Background")]
        [InlineData("All")]
        public void Hover_MakesItSolid(string mode) =>
            Assert.Equal(new FadeLevels(1.0, 1.0), StripFade.Compute(0.5, mode, true, true, true, false));

        [Fact]
        public void Hover_IgnoredWhenSwitchedOff() =>
            Assert.Equal(new FadeLevels(1.0, 0.5), StripFade.Compute(0.5, "Background", false, true, true, false));

        [Fact]
        public void AllMode_AlertMakesItSolid_UnlessSwitchedOff()
        {
            Assert.Equal(new FadeLevels(1.0, 1.0), StripFade.Compute(0.5, "All", true, false, true, true));
            Assert.Equal(new FadeLevels(0.5, 1.0), StripFade.Compute(0.5, "All", true, false, false, true));
        }

        [Theory]
        [InlineData(0.1, "Background", 0.2)]
        [InlineData(0.1, "All", 0.4)]
        [InlineData(0.33, "Background", 0.35)]
        [InlineData(1.5, "All", 1.0)]
        public void Clamp_KeepsRangeAndSteps(double input, string mode, double expected) =>
            Assert.Equal(expected, StripFade.Clamp(input, mode), 6);
    }
}
