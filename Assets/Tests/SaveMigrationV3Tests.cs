using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>세이브 v2 → v3 마이그레이션 + S04 상태(골드·상점·공방·해금) 라운드트립 + 수직 슬라이스 풀 루프 검증.</summary>
    public class SaveMigrationV3Tests
    {
        sealed class FakeUtcClock : IUtcClock
        {
            public DateTime Now = new DateTime(2026, 7, 22, 9, 0, 0, DateTimeKind.Utc);
            public DateTime UtcNow => Now;
        }

        const string Grass = "plant_ember_grass";          // 1G · 성장 3초
        const string Leaf = "material_dried_flame_leaf";   // 5G · 가공 8초
        const string Tier2Seed = "plant_flame_poppy";      // 해금 100G

        static readonly Dictionary<string, int> Prices = new Dictionary<string, int>
        {
            { Grass, 1 },
            { Leaf, 5 }
        };

        static int PriceOf(string id) => Prices.TryGetValue(id, out int price) ? price : 0;

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
        public void CurrentVersion_IsThree()
        {
            Assert.AreEqual(3, SaveData.CurrentVersion);
        }

        [Test]
        public void TryMigrate_V2_AddsGoldShopWorkshopUnlockDefaults()
        {
            var data = new SaveData
            {
                version = 2,
                eventSeconds = 100.0,
                resourceSeconds = 500.0,
                unlockedIds = null,
                shopDisplaySlots = null
            };

            Assert.IsTrue(SaveMigrator.TryMigrate(data));
            Assert.AreEqual(3, data.version);
            Assert.AreEqual(0, data.gold);
            Assert.IsNotNull(data.unlockedIds);
            Assert.IsNotNull(data.shopDisplaySlots);
            Assert.AreEqual("", data.workshopOutputId);
            // 손님 타이머는 저장된 자원초로 — 로드 직후 밀린 손님 몰림 방지
            Assert.AreEqual(500.0, data.shopLastCustomerAtResourceSeconds, 1e-9);
            Assert.AreEqual(100.0, data.eventSeconds); // 기존 필드 보존
        }

        [Test]
        public void TryMigrate_V1_ChainsToV3()
        {
            var data = new SaveData { version = 1, resourceSeconds = 42.0, gardenSlots = null, inventoryItems = null };

            Assert.IsTrue(SaveMigrator.TryMigrate(data));
            Assert.AreEqual(3, data.version);
            Assert.IsNotNull(data.gardenSlots);
            Assert.IsNotNull(data.unlockedIds);
            Assert.AreEqual(42.0, data.shopLastCustomerAtResourceSeconds, 1e-9);
        }

        [Test]
        public void TryLoad_V2JsonFile_MigratesAndLoads()
        {
            // S03가 실제로 남긴 형태의 v2 파일 (골드·상점·공방 필드 없음)
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_repository.FilePath,
                "{\"version\":2,\"eventSeconds\":123.0,\"resourceSeconds\":456.0,\"lastSavedUtcTicks\":638000000000000000," +
                "\"gardenSlots\":[{\"plantId\":\"plant_ember_grass\",\"plantedAtResourceSeconds\":400.0}]," +
                "\"inventoryItems\":[{\"itemId\":\"plant_dew_moss\",\"count\":3}]}");

            Assert.IsTrue(_repository.TryLoad(out SaveData loaded));
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(0, loaded.gold);
            Assert.AreEqual(456.0, loaded.shopLastCustomerAtResourceSeconds, 1e-9);
            Assert.AreEqual("plant_ember_grass", loaded.gardenSlots[0].plantId); // 기존 상태 보존
            Assert.AreEqual(3, loaded.inventoryItems[0].count);
        }

        [Test]
        public void SessionRoundTrip_RestoresGoldDisplayUnlocksWorkshopAndSlots()
        {
            // 1회차: 골드·해금·진열·가공·밭 확장까지 만들고 저장
            var firstRun = new GameSession(_repository, _utcClock);
            firstRun.Begin();
            firstRun.Wallet.Add(200);
            Assert.IsTrue(firstRun.TryPurchaseUnlock(Tier2Seed, 100));      // 잔액 100
            Assert.IsTrue(firstRun.TryBuyGardenSlot());                     // 20G → 잔액 80, 슬롯 5
            firstRun.Inventory.Add(Grass, 3);
            firstRun.Inventory.Add(Leaf, 4);
            Assert.AreEqual(2, firstRun.Shop.Display(1, Leaf, 2, firstRun.Inventory));
            Assert.IsTrue(firstRun.Workshop.TryStart(Leaf, 1, Grass, 1, firstRun.Inventory, firstRun.Clock.ResourceSeconds));
            firstRun.SaveNow();

            // 2회차: 상태 그대로 복원
            var secondRun = new GameSession(_repository, _utcClock);
            secondRun.Begin();

            Assert.IsTrue(secondRun.LoadedFromSave);
            Assert.AreEqual(80, secondRun.Wallet.Gold);
            Assert.IsTrue(secondRun.Unlocks.IsUnlocked(Tier2Seed));
            Assert.AreEqual(5, secondRun.Garden.SlotCount);
            Assert.AreEqual(Leaf, secondRun.Shop.Slots[1].ItemId);
            Assert.AreEqual(2, secondRun.Shop.Slots[1].Count);
            Assert.AreEqual(Leaf, secondRun.Workshop.OutputItemId);
            Assert.AreEqual(2, secondRun.Inventory.GetCount(Grass)); // 3 - 가공 소비 1
            Assert.AreEqual(2, secondRun.Inventory.GetCount(Leaf));  // 4 - 진열 2
        }

        [Test]
        public void TryBuyGardenSlot_CostFollowsCurve_AndStopsAtCapOrPoverty()
        {
            var session = new GameSession(_repository, _utcClock);
            session.Begin();

            Assert.AreEqual(20, session.NextGardenSlotCost);
            Assert.IsFalse(session.TryBuyGardenSlot()); // 무일푼

            session.Wallet.Add(43);
            Assert.IsTrue(session.TryBuyGardenSlot());  // 20G
            Assert.AreEqual(23, session.NextGardenSlotCost);
            Assert.IsTrue(session.TryBuyGardenSlot());  // 23G
            Assert.AreEqual(0, session.Wallet.Gold);
            Assert.AreEqual(6, session.Garden.SlotCount);

            session.Wallet.Add(1_000_000);
            while (session.Garden.SlotCount < Garden.MaxSlotCount)
                Assert.IsTrue(session.TryBuyGardenSlot());
            long goldAtCap = session.Wallet.Gold;
            Assert.IsFalse(session.TryBuyGardenSlot()); // 상한 도달 — 지불 없음
            Assert.AreEqual(goldAtCap, session.Wallet.Gold);
        }

        [Test]
        public void VerticalSliceFullLoop_PlantProcessSellExpandUnlock()
        {
            var session = new GameSession(_repository, _utcClock);
            session.Begin();
            double t = session.Clock.ResourceSeconds;

            // 심기 → 성장 → 수확
            Assert.IsTrue(session.TryPlant(0, Grass));
            session.Clock.Tick(3.0);
            Assert.IsTrue(session.TryHarvestToInventory(0, growthSeconds: 3.0));
            Assert.AreEqual(1, session.Inventory.GetCount(Grass));

            // 가공 (1G 풀 → 5G 마른 잎)
            Assert.IsTrue(session.Workshop.TryStart(Leaf, 1, Grass, 1, session.Inventory, session.Clock.ResourceSeconds));
            session.Clock.Tick(8.0);
            Assert.IsTrue(session.Workshop.TryCollect(session.Clock.ResourceSeconds, 8.0, session.Inventory, out _, out _));
            Assert.AreEqual(1, session.Inventory.GetCount(Leaf));

            // 진열 → 손님 구매 → 골드 (비축분 추가로 여러 손님 처리)
            session.Inventory.Add(Leaf, 29);
            Assert.AreEqual(10, session.Shop.Display(0, Leaf, 10, session.Inventory));
            Assert.AreEqual(10, session.Shop.Display(0, Leaf, 10, session.Inventory));
            Assert.AreEqual(10, session.Shop.Display(0, Leaf, 10, session.Inventory));
            double elapsed = session.Clock.ResourceSeconds - t;
            session.Clock.Tick(Shop.CustomerIntervalSeconds * 6.0 - elapsed); // 손님 6명 방문 시점까지
            session.Shop.TickCustomers(session.Clock.ResourceSeconds, PriceOf, session.Wallet);
            Assert.AreEqual(150, session.Wallet.Gold); // 30개 × 5G

            // 성장 루프: 밭 확장 (20G) → 티어2 해금 (100G)
            Assert.IsTrue(session.TryBuyGardenSlot());
            Assert.AreEqual(5, session.Garden.SlotCount);
            Assert.IsTrue(session.TryPurchaseUnlock(Tier2Seed, 100));
            Assert.AreEqual(30, session.Wallet.Gold);

            // 해금된 티어2를 새 슬롯에 심어 확장 체감
            Assert.IsTrue(session.TryPlant(4, Tier2Seed));
            session.Clock.Tick(15.0);
            Assert.IsTrue(session.TryHarvestToInventory(4, growthSeconds: 15.0));
            Assert.AreEqual(1, session.Inventory.GetCount(Tier2Seed));
        }
    }
}
