using System;
using System.IO;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>세션 라운드트립 검증 — 껐다 켜면 상태 복원 + 오프라인 경과 초 계산 (S02 완료 기준).</summary>
    public class GameSessionTests
    {
        /// <summary>테스트용 가짜 UTC 시계.</summary>
        sealed class FakeUtcClock : IUtcClock
        {
            public DateTime Now = new DateTime(2026, 7, 22, 9, 0, 0, DateTimeKind.Utc);
            public DateTime UtcNow => Now;
        }

        string _directory;
        SaveRepository _repository;
        FakeUtcClock _utcClock;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "WizardGardenTests", Guid.NewGuid().ToString("N"));
            _repository = new SaveRepository(_directory);
            _utcClock = new FakeUtcClock();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void Begin_NoSave_StartsNewGame()
        {
            var session = new GameSession(_repository, _utcClock);

            session.Begin();

            Assert.IsFalse(session.LoadedFromSave);
            Assert.AreEqual(0.0, session.PendingOfflineSeconds);
            Assert.AreEqual(1, session.Clock.DayIndex);
            Assert.AreEqual(TimeOfDay.Morning, session.Clock.CurrentTimeOfDay);
        }

        [Test]
        public void SaveQuitRestart_RestoresClockAndComputesOfflineSeconds()
        {
            // 1회차 실행: 100초 플레이 후 저장(=종료 시각 기록)
            var firstRun = new GameSession(_repository, _utcClock);
            firstRun.Begin();
            firstRun.Clock.Tick(100.0);
            firstRun.SaveNow();

            // 1시간 뒤 재시작
            _utcClock.Now = _utcClock.Now.AddHours(1.0);
            var secondRun = new GameSession(_repository, _utcClock);
            secondRun.Begin();

            Assert.IsTrue(secondRun.LoadedFromSave);
            Assert.AreEqual(100.0, secondRun.Clock.EventSeconds, 1e-9);
            Assert.AreEqual(100.0, secondRun.Clock.ResourceSeconds, 1e-9);
            Assert.AreEqual(3600.0, secondRun.PendingOfflineSeconds, 1e-6);
        }

        [Test]
        public void PendingOfflineSeconds_ClearedAfterSettlementHook()
        {
            var firstRun = new GameSession(_repository, _utcClock);
            firstRun.Begin();
            firstRun.SaveNow();

            _utcClock.Now = _utcClock.Now.AddMinutes(30.0);
            var secondRun = new GameSession(_repository, _utcClock);
            secondRun.Begin();
            Assert.AreEqual(1800.0, secondRun.PendingOfflineSeconds, 1e-6);

            // S08 정산이 소비한 뒤 호출할 훅
            secondRun.ClearPendingOfflineSeconds();
            Assert.AreEqual(0.0, secondRun.PendingOfflineSeconds);
        }

        [Test]
        public void ComputeOfflineSeconds_ClampsClockSkewToZero()
        {
            // 시스템 시계가 뒤로 간 경우(음수 경과) → 0
            DateTime saved = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);
            DateTime earlierNow = saved.AddMinutes(-10.0);

            Assert.AreEqual(0.0, GameSession.ComputeOfflineSeconds(saved.Ticks, earlierNow));
            // 저장 기록이 없는(0) 경우도 0
            Assert.AreEqual(0.0, GameSession.ComputeOfflineSeconds(0, saved));
        }

        [Test]
        public void SaveNow_StampsCurrentUtcTime()
        {
            var session = new GameSession(_repository, _utcClock);
            session.Begin();

            session.SaveNow();

            Assert.IsTrue(_repository.TryLoad(out var data));
            Assert.AreEqual(_utcClock.Now.Ticks, data.lastSavedUtcTicks);
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
        }
    }
}
