using System;
using System.IO;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>세이브 v3 → v4 마이그레이션 + 도감 발견·별빛 조각 라운드트립 검증 (S06).</summary>
    public class SaveMigrationV4Tests
    {
        sealed class FakeUtcClock : IUtcClock
        {
            public DateTime Now = new DateTime(2026, 7, 23, 9, 0, 0, DateTimeKind.Utc);
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
        public void CurrentVersion_IsFive()
        {
            // S09에서 v5로 상향 (견습생 필드 추가 — vN 대응 갱신)
            Assert.AreEqual(5, SaveData.CurrentVersion);
        }

        [Test]
        public void TryMigrate_V3_AddsCodexAndStarlightDefaults()
        {
            var data = new SaveData
            {
                version = 3,
                gold = 555,
                discoveredCodexIds = null
            };

            Assert.IsTrue(SaveMigrator.TryMigrate(data));
            Assert.AreEqual(SaveData.CurrentVersion, data.version); // v3 → 현재 버전까지 체인
            Assert.IsNotNull(data.discoveredCodexIds);
            Assert.AreEqual(0, data.discoveredCodexIds.Count);
            Assert.AreEqual(0, data.starlightShards);
            Assert.AreEqual(555, data.gold); // 기존 필드 보존
        }

        [Test]
        public void TryLoad_V3JsonFile_MigratesAndLoads()
        {
            // S04가 남길 법한 v3 파일 (도감·별빛 필드 없음)
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_repository.FilePath,
                "{\"version\":3,\"eventSeconds\":10.0,\"resourceSeconds\":20.0,\"lastSavedUtcTicks\":638000000000000000," +
                "\"gold\":123,\"unlockedIds\":[\"plant_flame_poppy\"]}");

            Assert.IsTrue(_repository.TryLoad(out SaveData loaded));
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.IsNotNull(loaded.discoveredCodexIds);
            Assert.AreEqual(0, loaded.discoveredCodexIds.Count);
            Assert.AreEqual(0, loaded.starlightShards);
            Assert.AreEqual(123, loaded.gold); // 기존 상태 보존
        }

        [Test]
        public void SessionRoundTrip_RestoresCodexAndStarlight()
        {
            var firstRun = new GameSession(_repository, _utcClock);
            firstRun.Begin();
            firstRun.Codex.Discover("potion_minor_flame");
            firstRun.Codex.Discover("potion_steam");
            firstRun.Codex.Discover("potion_murky");
            firstRun.AddStarlight(2);
            firstRun.SaveNow();

            var secondRun = new GameSession(_repository, _utcClock);
            secondRun.Begin();

            Assert.IsTrue(secondRun.LoadedFromSave);
            Assert.IsTrue(secondRun.Codex.IsDiscovered("potion_minor_flame"));
            Assert.IsTrue(secondRun.Codex.IsDiscovered("potion_steam"));
            Assert.IsTrue(secondRun.Codex.IsDiscovered("potion_murky"));
            Assert.AreEqual(2, secondRun.StarlightShards);

            // 로드 후 우주 등록 → 완성도 계산 (어댑터 흐름 재현)
            secondRun.Codex.RegisterPotion("potion_minor_flame");
            secondRun.Codex.RegisterPotion("potion_steam");
            secondRun.Codex.RegisterByproduct("potion_murky");
            Assert.AreEqual(3, secondRun.Codex.DiscoveredCount);
            Assert.AreEqual(1.0, secondRun.Codex.CompletionRatio, 1e-9);
        }

        [Test]
        public void NewGame_StartsWithEmptyCodexAndZeroStarlight()
        {
            var session = new GameSession(_repository, _utcClock);
            session.Begin();

            Assert.AreEqual(0, session.Codex.DiscoveredCount);
            Assert.AreEqual(0, session.StarlightShards);
        }
    }
}
