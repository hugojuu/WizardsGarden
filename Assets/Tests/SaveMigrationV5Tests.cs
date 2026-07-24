using System;
using System.IO;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>세이브 v4 → v5 마이그레이션 + 견습생 보유·배치·레벨 라운드트립 검증 (S09).</summary>
    public class SaveMigrationV5Tests
    {
        sealed class FakeUtcClock : IUtcClock
        {
            public DateTime Now = new DateTime(2026, 7, 24, 9, 0, 0, DateTimeKind.Utc);
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
        public void TryMigrate_V4_AddsEmptyApprentices()
        {
            var data = new SaveData { version = 4, gold = 777, apprentices = null };

            Assert.IsTrue(SaveMigrator.TryMigrate(data));
            Assert.AreEqual(5, data.version);
            Assert.IsNotNull(data.apprentices);
            Assert.AreEqual(0, data.apprentices.Count);
            Assert.AreEqual(777, data.gold);   // 기존 필드 보존
        }

        [Test]
        public void TryLoad_V4JsonFile_MigratesAndLoads()
        {
            // S06가 남길 법한 v4 파일 (견습생 필드 없음)
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_repository.FilePath,
                "{\"version\":4,\"eventSeconds\":5.0,\"resourceSeconds\":9.0,\"lastSavedUtcTicks\":638000000000000000," +
                "\"gold\":40,\"starlightShards\":3}");

            Assert.IsTrue(_repository.TryLoad(out SaveData loaded));
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.IsNotNull(loaded.apprentices);
            Assert.AreEqual(0, loaded.apprentices.Count);
            Assert.AreEqual(40, loaded.gold);
            Assert.AreEqual(3, loaded.starlightShards);
        }

        [Test]
        public void SessionRoundTrip_RestoresRosterPlacementAndLevel()
        {
            var firstRun = new GameSession(_repository, _utcClock);
            firstRun.Begin();
            ApprenticeState g = firstRun.Roster.AddIfMissing(
                new ApprenticeState("appr_bomi", Job.Gardener, Rarity.Common, 5, 2, 2, 2));
            firstRun.Roster.AddIfMissing(
                new ApprenticeState("appr_glim", Job.Alchemist, Rarity.Common, 2, 6, 3, 2));
            firstRun.Roster.TryPlace("appr_bomi");
            g.AddExperience(10);   // 레벨 2, 친화력 6
            firstRun.SaveNow();

            var secondRun = new GameSession(_repository, _utcClock);
            secondRun.Begin();

            Assert.IsTrue(secondRun.LoadedFromSave);
            Assert.AreEqual(2, secondRun.Roster.All.Count);
            ApprenticeState bomi = secondRun.Roster.Get("appr_bomi");
            Assert.IsNotNull(bomi);
            Assert.IsTrue(bomi.IsPlaced);
            Assert.AreEqual(2, bomi.Level);
            Assert.AreEqual(6, bomi.Affinity);
            Assert.IsFalse(secondRun.Roster.Get("appr_glim").IsPlaced);
        }

        [Test]
        public void NewGame_StartsWithEmptyRoster()
        {
            var session = new GameSession(_repository, _utcClock);
            session.Begin();
            Assert.AreEqual(0, session.Roster.All.Count);
        }
    }
}
