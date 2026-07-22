using System.Collections.Generic;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>이중 시간 시계 검증 — 1일 = 현실 900초, 시작 06:00, 배속, 구간/일차 이벤트.</summary>
    public class GameClockTests
    {
        [Test]
        public void NewClock_StartsDay1MorningAtSix()
        {
            var clock = new GameClock();

            Assert.AreEqual(1, clock.DayIndex);
            Assert.AreEqual(6.0, clock.HourOfDay, 1e-9);
            Assert.AreEqual(TimeOfDay.Morning, clock.CurrentTimeOfDay);
            Assert.AreEqual(0.0, clock.EventSeconds);
            Assert.AreEqual(0.0, clock.ResourceSeconds);
        }

        [Test]
        public void Tick_AdvancesBothTracksEqually()
        {
            var clock = new GameClock();

            clock.Tick(10.0);

            Assert.AreEqual(10.0, clock.EventSeconds, 1e-9);
            Assert.AreEqual(10.0, clock.ResourceSeconds, 1e-9);
        }

        [Test]
        public void Tick_AppliesTimeScale()
        {
            var clock = new GameClock { TimeScale = 10.0 };

            clock.Tick(5.0);

            Assert.AreEqual(50.0, clock.EventSeconds, 1e-9);
            Assert.AreEqual(50.0, clock.ResourceSeconds, 1e-9);
        }

        [Test]
        public void TimeScale_RejectsZeroOrNegative()
        {
            var clock = new GameClock();

            clock.TimeScale = 0.0;
            Assert.AreEqual(1.0, clock.TimeScale);

            clock.TimeScale = -5.0;
            Assert.AreEqual(1.0, clock.TimeScale);
        }

        [Test]
        public void GameHourConversion_QuarterDayIs225RealSeconds()
        {
            var clock = new GameClock();

            // 6게임시간 = 하루의 1/4 = 900/4 = 225 현실 초 → 06:00 + 6h = 12:00
            clock.Tick(225.0);

            Assert.AreEqual(12.0, clock.HourOfDay, 1e-6);
            Assert.AreEqual(TimeOfDay.Day, clock.CurrentTimeOfDay);
        }

        [Test]
        public void TimeOfDayChanged_FiresWithPreviousAndCurrent()
        {
            var clock = new GameClock();
            var transitions = new List<(TimeOfDay from, TimeOfDay to)>();
            clock.TimeOfDayChanged += (from, to) => transitions.Add((from, to));

            // 06:00 → 09:01 (약 3게임시간 = 113 현실 초): 아침 → 낮 전환 1회
            clock.Tick(113.0);

            Assert.AreEqual(1, transitions.Count);
            Assert.AreEqual((TimeOfDay.Morning, TimeOfDay.Day), transitions[0]);
            Assert.AreEqual(TimeOfDay.Day, clock.CurrentTimeOfDay);
        }

        [Test]
        public void DayChanged_FiresWhenMidnightCrossed()
        {
            var clock = new GameClock();
            int? newDay = null;
            clock.DayChanged += day => newDay = day;

            // 06:00 → 다음날 00:01 (약 18게임시간 = 676 현실 초)
            clock.Tick(676.0);

            Assert.AreEqual(2, newDay);
            Assert.AreEqual(2, clock.DayIndex);
            Assert.AreEqual(TimeOfDay.Night, clock.CurrentTimeOfDay);
        }

        [Test]
        public void FullDayAtMaxPreset_CyclesThroughAllWindows()
        {
            // 배속 x900 = 현실 1초에 게임 내 하루 (완료 기준: 배속으로 하루가 돌아감)
            var clock = new GameClock { TimeScale = 900.0 };
            var transitions = new List<TimeOfDay>();
            clock.TimeOfDayChanged += (_, to) => transitions.Add(to);

            // 0.1초 스텝 x10 = 현실 1초 = 하루, + 경계 오차 방지용 소량 추가
            for (int i = 0; i < 10; i++)
                clock.Tick(0.1);
            clock.Tick(0.005);

            Assert.AreEqual(2, clock.DayIndex);
            Assert.AreEqual(TimeOfDay.Morning, clock.CurrentTimeOfDay);
            Assert.AreEqual(6.12, clock.HourOfDay, 0.01);
            // 아침→낮→저녁→야간→(자정 넘어 야간 유지)→아침: 구간 전환 4회
            CollectionAssert.AreEqual(
                new[] { TimeOfDay.Day, TimeOfDay.Evening, TimeOfDay.Night, TimeOfDay.Morning },
                transitions);
        }

        [Test]
        public void SkipGameHours_IgnoresTimeScale()
        {
            var clock = new GameClock { TimeScale = 100.0 };

            clock.SkipGameHours(24.0);

            Assert.AreEqual(2, clock.DayIndex);
            Assert.AreEqual(6.0, clock.HourOfDay, 1e-6);
            Assert.AreEqual(GameClock.RealSecondsPerGameDay, clock.EventSeconds, 1e-6);
        }

        [Test]
        public void AddResourceSeconds_TouchesOnlyResourceTrack()
        {
            var clock = new GameClock();

            clock.AddResourceSeconds(500.0);

            Assert.AreEqual(500.0, clock.ResourceSeconds, 1e-9);
            Assert.AreEqual(0.0, clock.EventSeconds);
        }

        [Test]
        public void SaveRoundTrip_RestoresBothTracks()
        {
            var clock = new GameClock();
            clock.Tick(1234.5);
            clock.AddResourceSeconds(100.0);

            var data = SaveData.CreateNew();
            clock.WriteTo(data);

            var restored = new GameClock();
            restored.RestoreFrom(data);

            Assert.AreEqual(clock.EventSeconds, restored.EventSeconds, 1e-9);
            Assert.AreEqual(clock.ResourceSeconds, restored.ResourceSeconds, 1e-9);
            Assert.AreEqual(clock.DayIndex, restored.DayIndex);
            Assert.AreEqual(clock.HourOfDay, restored.HourOfDay, 1e-9);
        }
    }
}
