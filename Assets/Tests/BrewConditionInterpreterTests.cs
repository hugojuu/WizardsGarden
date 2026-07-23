using System.Collections.Generic;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>조건 태그 해석기 검증(S05 매칭 3단계) — night_only/time/weather/season 문법.</summary>
    public class BrewConditionInterpreterTests
    {
        [Test]
        public void NightOnly_TrueAtNight_FalseOtherwise()
        {
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("night_only",
                new BrewContext(TimeOfDay.Night, Weather.Clear)));
            Assert.IsFalse(BrewConditionInterpreter.IsSatisfied("night_only",
                new BrewContext(TimeOfDay.Day, Weather.Clear)));
        }

        [Test]
        public void WeatherRain_MatchesRainOnly()
        {
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("weather:rain",
                new BrewContext(TimeOfDay.Day, Weather.Rain)));
            Assert.IsFalse(BrewConditionInterpreter.IsSatisfied("weather:rain",
                new BrewContext(TimeOfDay.Day, Weather.Clear)));
        }

        [Test]
        public void WeatherEclipse_MatchesEclipse()
        {
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("weather:eclipse",
                new BrewContext(TimeOfDay.Day, Weather.Eclipse)));
        }

        [Test]
        public void WeatherVariants_Parse()
        {
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("weather:storm",
                new BrewContext(TimeOfDay.Day, Weather.Storm)));
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("weather:moonlit_night",
                new BrewContext(TimeOfDay.Night, Weather.MoonlitNight)));
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("weather:meteor_shower",
                new BrewContext(TimeOfDay.Night, Weather.MeteorShower)));
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("weather:clear",
                new BrewContext(TimeOfDay.Day, Weather.Clear)));
        }

        [Test]
        public void TimeTags_Parse()
        {
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("time:morning",
                new BrewContext(TimeOfDay.Morning, Weather.Clear)));
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("time:evening",
                new BrewContext(TimeOfDay.Evening, Weather.Clear)));
        }

        [Test]
        public void SeasonTags_Parse()
        {
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("season:spring",
                new BrewContext(TimeOfDay.Day, Weather.Clear, Season.Spring)));
            Assert.IsFalse(BrewConditionInterpreter.IsSatisfied("season:winter",
                new BrewContext(TimeOfDay.Day, Weather.Clear, Season.Spring)));
        }

        [Test]
        public void CaseInsensitive_AndTrimmed()
        {
            Assert.IsTrue(BrewConditionInterpreter.IsSatisfied("  WEATHER:RAIN  ",
                new BrewContext(TimeOfDay.Day, Weather.Rain)));
        }

        [Test]
        public void UnknownTag_IsUnsatisfied()
        {
            Assert.IsFalse(BrewConditionInterpreter.IsSatisfied("full_moon_lunar_eclipse",
                new BrewContext(TimeOfDay.Night, Weather.Eclipse)));
            Assert.IsFalse(BrewConditionInterpreter.IsSatisfied("weather:tornado",
                new BrewContext(TimeOfDay.Day, Weather.Storm)));
        }

        [Test]
        public void EmptyOrNullTag_IsUnsatisfied()
        {
            var ctx = new BrewContext(TimeOfDay.Night, Weather.Rain);
            Assert.IsFalse(BrewConditionInterpreter.IsSatisfied("", ctx));
            Assert.IsFalse(BrewConditionInterpreter.IsSatisfied(null, ctx));
            Assert.IsFalse(BrewConditionInterpreter.IsSatisfied("weather:", ctx));
        }

        [Test]
        public void AllSatisfied_EmptyTags_True()
        {
            Assert.IsTrue(BrewConditionInterpreter.AllSatisfied(null,
                new BrewContext(TimeOfDay.Day, Weather.Clear), out var unmet));
            Assert.IsNull(unmet);
            Assert.IsTrue(BrewConditionInterpreter.AllSatisfied(new List<string>(),
                new BrewContext(TimeOfDay.Day, Weather.Clear), out _));
        }

        [Test]
        public void AllSatisfied_PartialMet_ReturnsFalseWithUnmet()
        {
            var tags = new List<string> { "night_only", "weather:rain" };
            var ctx = new BrewContext(TimeOfDay.Night, Weather.Clear); // 야간 O, 비 X
            Assert.IsFalse(BrewConditionInterpreter.AllSatisfied(tags, ctx, out var unmet));
            Assert.IsNotNull(unmet);
            Assert.AreEqual(1, unmet.Count);
            Assert.AreEqual("weather:rain", unmet[0]);
        }

        [Test]
        public void AllSatisfied_AllMet_True()
        {
            var tags = new List<string> { "night_only", "weather:rain" };
            var ctx = new BrewContext(TimeOfDay.Night, Weather.Rain);
            Assert.IsTrue(BrewConditionInterpreter.AllSatisfied(tags, ctx, out var unmet));
            Assert.IsNull(unmet);
        }
    }
}
