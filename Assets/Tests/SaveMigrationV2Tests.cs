using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>세이브 v1 → v2 마이그레이션 + 밭·인벤토리 포함 세션 라운드트립 검증 (S03).</summary>
    public class SaveMigrationV2Tests
    {
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
        public void CurrentVersion_IsTwo()
        {
            Assert.AreEqual(2, SaveData.CurrentVersion);
        }

        [Test]
        public void TryMigrate_V1_UpgradesToV2WithEmptyGardenAndInventory()
        {
            var data = new SaveData
            {
                version = 1,
                eventSeconds = 10.0,
                resourceSeconds = 20.0,
                gardenSlots = null,
                inventoryItems = null
            };

            Assert.IsTrue(SaveMigrator.TryMigrate(data));
            Assert.AreEqual(2, data.version);
            Assert.IsNotNull(data.gardenSlots);
            Assert.IsNotNull(data.inventoryItems);
            Assert.AreEqual(0, data.gardenSlots.Count);
            Assert.AreEqual(0, data.inventoryItems.Count);
            Assert.AreEqual(10.0, data.eventSeconds); // 기존 필드 보존
            Assert.AreEqual(20.0, data.resourceSeconds);
        }

        [Test]
        public void TryLoad_V1JsonFile_MigratesAndLoads()
        {
            // S02가 실제로 남긴 형태의 v1 파일 (밭·인벤토리 필드 없음)
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_repository.FilePath,
                "{\"version\":1,\"eventSeconds\":123.0,\"resourceSeconds\":456.0,\"lastSavedUtcTicks\":638000000000000000}");

            Assert.IsTrue(_repository.TryLoad(out SaveData loaded));
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(123.0, loaded.eventSeconds);
            Assert.AreEqual(456.0, loaded.resourceSeconds);
            Assert.IsNotNull(loaded.gardenSlots);
            Assert.IsNotNull(loaded.inventoryItems);
        }

        [Test]
        public void TryMigrate_UnknownVersionZero_StillRejected()
        {
            Assert.IsFalse(SaveMigrator.TryMigrate(new SaveData { version = 0 }));
        }

        [Test]
        public void SessionRoundTrip_RestoresGardenSlotsAndInventory()
        {
            // 1회차: 심고(슬롯0 유지, 슬롯1 수확) 저장
            var firstRun = new GameSession(_repository, _utcClock);
            firstRun.Begin();
            firstRun.Clock.Tick(50.0);
            Assert.IsTrue(firstRun.TryPlant(0, "plant_ember_grass"));
            Assert.IsTrue(firstRun.TryPlant(1, "plant_dew_moss"));
            firstRun.Clock.Tick(10.0); // 자원 시간 60초
            Assert.IsTrue(firstRun.TryHarvestToInventory(1, growthSeconds: 3.0));
            Assert.AreEqual(1, firstRun.Inventory.GetCount("plant_dew_moss"));
            firstRun.SaveNow();

            // 2회차: 상태 그대로 복원 (오프라인 정산 없음 — S08 몫)
            var secondRun = new GameSession(_repository, _utcClock);
            secondRun.Begin();

            Assert.IsTrue(secondRun.LoadedFromSave);
            Assert.AreEqual("plant_ember_grass", secondRun.Garden.Slots[0].PlantId);
            Assert.AreEqual(50.0, secondRun.Garden.Slots[0].PlantedAtResourceSeconds, 1e-9);
            Assert.IsTrue(secondRun.Garden.Slots[1].IsEmpty);
            Assert.IsTrue(secondRun.Garden.Slots[2].IsEmpty);
            Assert.AreEqual(1, secondRun.Inventory.GetCount("plant_dew_moss"));
            // 자원 시간도 저장 시점 그대로 → 성장 진행도 보존
            Assert.AreEqual(60.0, secondRun.Clock.ResourceSeconds, 1e-9);
        }

        [Test]
        public void TryHarvestToInventory_BeforeMature_DoesNotAddItem()
        {
            var session = new GameSession(_repository, _utcClock);
            session.Begin();
            session.TryPlant(0, "plant_ember_grass");
            session.Clock.Tick(1.0); // 성장 3초 중 1초

            Assert.IsFalse(session.TryHarvestToInventory(0, growthSeconds: 3.0));
            Assert.AreEqual(0, session.Inventory.GetCount("plant_ember_grass"));
            Assert.IsFalse(session.Garden.Slots[0].IsEmpty);
        }

        [Test]
        public void SavedJson_ContainsGardenAndInventoryFields()
        {
            var session = new GameSession(_repository, _utcClock);
            session.Begin();
            session.TryPlant(2, "plant_wild_grass");
            session.Inventory.Add("plant_wild_grass", 4);
            session.SaveNow();

            Assert.IsTrue(_repository.TryLoad(out SaveData loaded));
            Assert.AreEqual(Garden.InitialSlotCount, loaded.gardenSlots.Count);
            Assert.AreEqual("", loaded.gardenSlots[0].plantId);
            Assert.AreEqual("plant_wild_grass", loaded.gardenSlots[2].plantId);
            Assert.AreEqual(1, loaded.inventoryItems.Count);
            Assert.AreEqual("plant_wild_grass", loaded.inventoryItems[0].itemId);
            Assert.AreEqual(4, loaded.inventoryItems[0].count);
        }
    }
}
