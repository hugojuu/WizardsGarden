using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>시각 → 시각 구간 변환 검증 (기획서 22장: 아침06-09/낮09-18/저녁18-21/야간21-06).</summary>
    public class TimeOfDayUtilityTests
    {
        [TestCase(6.0, TimeOfDay.Morning)]
        [TestCase(8.99, TimeOfDay.Morning)]
        [TestCase(9.0, TimeOfDay.Day)]
        [TestCase(17.99, TimeOfDay.Day)]
        [TestCase(18.0, TimeOfDay.Evening)]
        [TestCase(20.99, TimeOfDay.Evening)]
        [TestCase(21.0, TimeOfDay.Night)]
        [TestCase(23.5, TimeOfDay.Night)]
        [TestCase(0.0, TimeOfDay.Night)]
        [TestCase(5.99, TimeOfDay.Night)]
        public void FromHour_MapsBoundariesToDesignWindows(double hour, TimeOfDay expected)
        {
            Assert.AreEqual(expected, TimeOfDayUtility.FromHour(hour));
        }

        [Test]
        public void FromHour_NormalizesOutOfRangeValues()
        {
            // 24 → 0시(야간), 30 → 6시(아침), -2 → 22시(야간)
            Assert.AreEqual(TimeOfDay.Night, TimeOfDayUtility.FromHour(24.0));
            Assert.AreEqual(TimeOfDay.Morning, TimeOfDayUtility.FromHour(30.0));
            Assert.AreEqual(TimeOfDay.Night, TimeOfDayUtility.FromHour(-2.0));
        }
    }
}
