using System.Collections.Generic;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>
    /// 오프라인 정산 검증 (S08) — 캡 8h·효율 60%·임시 자동화(수확·판매)·사건 시간 정지.
    /// 기획서 22장 "8시간 자고 일어났을 때" 시나리오와 캡 초과 케이스를 재현한다.
    /// </summary>
    public class OfflineSettlementTests
    {
        const string Grass = "plant_ember_grass"; // 성장 3초 · 가치 1G (티어1)
        const double Cap = OfflineSettlement.DefaultCapSeconds;   // 28800 (8h)
        const double Eff = OfflineSettlement.DefaultEfficiency;   // 0.6

        /// <summary>테스트용 고정 자동화율 (결정적).</summary>
        sealed class FakeAutomation : IOfflineAutomation
        {
            public double GardenHarvestRate { get; set; } = 1.0;
            public double ShopSalesRate { get; set; } = 0.5;
        }

        static readonly Dictionary<string, double> Growth = new Dictionary<string, double> { { Grass, 3.0 } };
        static readonly Dictionary<string, int> Prices = new Dictionary<string, int> { { Grass, 1 } };

        static double GrowthOf(string id) => Growth.TryGetValue(id, out double g) ? g : 0.0;
        static int PriceOf(string id) => Prices.TryGetValue(id, out int p) ? p : 0;

        // ---- 순수 계산: 유효 초 = min(raw, 캡) × 효율 ----

        [Test]
        public void EffectiveSeconds_BelowCap_AppliesEfficiencyOnly()
        {
            var settlement = new OfflineSettlement(new FakeAutomation());
            // 2시간 = 7200초 (캡 미만) → 7200 × 0.6 = 4320
            Assert.AreEqual(4320.0, settlement.EffectiveResourceSeconds(7200.0), 1e-9);
            Assert.IsFalse(settlement.WasCapped(7200.0));
        }

        [Test]
        public void EffectiveSeconds_AtCapBoundary_NotCapped()
        {
            var settlement = new OfflineSettlement(new FakeAutomation());
            // 정확히 8시간 = 28800 → 28800 × 0.6 = 17280, 캡에 걸리지 않음(초과 아님)
            Assert.AreEqual(17280.0, settlement.EffectiveResourceSeconds(Cap), 1e-9);
            Assert.IsFalse(settlement.WasCapped(Cap));
        }

        [Test]
        public void EffectiveSeconds_AboveCap_ClampsAndFlagsCapped()
        {
            var settlement = new OfflineSettlement(new FakeAutomation());
            // 10시간 = 36000 → min(36000, 28800) × 0.6 = 17280 (8h와 동일), 캡 플래그 on
            Assert.AreEqual(17280.0, settlement.EffectiveResourceSeconds(36000.0), 1e-9);
            Assert.IsTrue(settlement.WasCapped(36000.0));
        }

        [Test]
        public void EffectiveSeconds_NonPositive_IsZero()
        {
            var settlement = new OfflineSettlement(new FakeAutomation());
            Assert.AreEqual(0.0, settlement.EffectiveResourceSeconds(0.0), 1e-9);
            Assert.AreEqual(0.0, settlement.EffectiveResourceSeconds(-500.0), 1e-9);
        }

        [Test]
        public void CustomCapAndEfficiency_AreHonored()
        {
            var settlement = new OfflineSettlement(new FakeAutomation(), capSeconds: 100.0, efficiency: 0.5);
            Assert.AreEqual(25.0, settlement.EffectiveResourceSeconds(50.0), 1e-9);   // 50×0.5
            Assert.AreEqual(50.0, settlement.EffectiveResourceSeconds(200.0), 1e-9);  // min(200,100)×0.5
            Assert.IsTrue(settlement.WasCapped(200.0));
        }

        // ---- 정산 실행 ----

        [Test]
        public void Settle_AddsEffectiveToResourceTrack_LeavesEventTimeFrozen()
        {
            var clock = new GameClock();
            var settlement = new OfflineSettlement(new FakeAutomation());

            OfflineSettlementResult result = settlement.Settle(
                clock, new Garden(), new Inventory(), new Shop(), new Wallet(),
                7200.0, GrowthOf, PriceOf);

            Assert.AreEqual(4320.0, clock.ResourceSeconds, 1e-6);   // 자원 시간만 진행
            Assert.AreEqual(0.0, clock.EventSeconds, 1e-9);         // 사건 시간 정지 (기획서 22장)
            Assert.AreEqual(4320.0, result.EffectiveResourceSeconds, 1e-6);
        }

        [Test]
        public void Settle_ZeroOffline_NoActivity()
        {
            var clock = new GameClock();
            var settlement = new OfflineSettlement(new FakeAutomation());

            OfflineSettlementResult result = settlement.Settle(
                clock, new Garden(), new Inventory(), new Shop(), new Wallet(),
                0.0, GrowthOf, PriceOf);

            Assert.IsFalse(result.HasActivity);
            Assert.AreEqual(0.0, clock.ResourceSeconds, 1e-9);
            Assert.AreEqual(0, result.TotalHarvested);
            Assert.AreEqual(0L, result.GoldEarned);
        }

        [Test]
        public void Settle_AutoHarvestsPlantedSlots_ByCycleCount()
        {
            var clock = new GameClock();
            var garden = new Garden();
            var inventory = new Inventory();
            garden.TryPlant(0, Grass, clock.ResourceSeconds);   // 성장 3초
            garden.TryPlant(1, Grass, clock.ResourceSeconds);

            // 자동 판매는 끄고(수확만) 수확 사이클을 순수 검증
            var automation = new FakeAutomation { GardenHarvestRate = 1.0, ShopSalesRate = 0.0 };
            var settlement = new OfflineSettlement(automation);

            // 7200 raw → effective 4320 → cycles/슬롯 = floor(4320/3) = 1440
            OfflineSettlementResult result = settlement.Settle(
                clock, garden, inventory, new Shop(), new Wallet(),
                7200.0, GrowthOf, PriceOf);

            Assert.AreEqual(2880, result.TotalHarvested);            // 1440 × 2
            Assert.AreEqual(2880, inventory.GetCount(Grass));        // 판매 없음 → 전량 보관함
            Assert.AreEqual(2880, result.HarvestedToStorage);
            Assert.AreEqual(0, result.SoldCount);
            Assert.AreEqual(0L, result.GoldEarned);
        }

        [Test]
        public void Settle_ReplantsHarvestedSlots_ForContinuedGrowth()
        {
            var clock = new GameClock();
            var garden = new Garden();
            garden.TryPlant(0, Grass, clock.ResourceSeconds);
            var settlement = new OfflineSettlement(new FakeAutomation { ShopSalesRate = 0.0 });

            settlement.Settle(clock, garden, new Inventory(), new Shop(), new Wallet(),
                7200.0, GrowthOf, PriceOf);

            // 수확 후 같은 작물로 재파종되어 복귀 플레이어가 이어서 키운다
            Assert.IsFalse(garden.Slots[0].IsEmpty);
            Assert.AreEqual(Grass, garden.Slots[0].PlantId);
            Assert.AreEqual(clock.ResourceSeconds, garden.Slots[0].PlantedAtResourceSeconds, 1e-6);
        }

        [Test]
        public void Settle_AutoSellsHarvest_UpToShopCapacity_RestToStorage()
        {
            var clock = new GameClock();
            var garden = new Garden();
            var inventory = new Inventory();
            var wallet = new Wallet();
            garden.TryPlant(0, Grass, clock.ResourceSeconds);

            // rate 1.0 / sales 0.5. 7200 raw → effective 4320.
            //   cycles = floor(4320/3) = 1440 수확
            //   판매 상한 = floor(4320 × 0.5) = 2160 → min(1440, 2160) = 1440 전량 판매
            var settlement = new OfflineSettlement(new FakeAutomation());
            OfflineSettlementResult result = settlement.Settle(
                clock, garden, inventory, new Shop(), wallet, 7200.0, GrowthOf, PriceOf);

            Assert.AreEqual(1440, result.TotalHarvested);
            Assert.AreEqual(1440, result.SoldCount);
            Assert.AreEqual(1440L, result.GoldEarned);      // 1440 × 1G
            Assert.AreEqual(1440L, wallet.Gold);
            Assert.AreEqual(0, inventory.GetCount(Grass));   // 전량 판매
            Assert.AreEqual(0, result.HarvestedToStorage);
        }

        [Test]
        public void Settle_SalesCapacityLimitsGold_RemainderStaysInStorage()
        {
            var clock = new GameClock();
            var garden = new Garden();
            var inventory = new Inventory();
            var wallet = new Wallet();
            garden.TryPlant(0, Grass, clock.ResourceSeconds);

            // 판매율을 낮춰 상한이 수확량보다 작게: sales 0.1 → 상한 floor(4320×0.1)=432 < 수확 1440
            var settlement = new OfflineSettlement(new FakeAutomation { ShopSalesRate = 0.1 });
            OfflineSettlementResult result = settlement.Settle(
                clock, garden, inventory, new Shop(), wallet, 7200.0, GrowthOf, PriceOf);

            Assert.AreEqual(1440, result.TotalHarvested);
            Assert.AreEqual(432, result.SoldCount);
            Assert.AreEqual(432L, wallet.Gold);
            Assert.AreEqual(1440 - 432, inventory.GetCount(Grass));
            Assert.AreEqual(1440 - 432, result.HarvestedToStorage);
        }

        [Test]
        public void Settle_AppliesGoldModifier_ToAutoSell()
        {
            var clock = new GameClock();
            var garden = new Garden();
            var wallet = new Wallet();
            garden.TryPlant(0, Grass, clock.ResourceSeconds);

            var settlement = new OfflineSettlement(new FakeAutomation());
            // 도감 보너스 흉내 — 판매 골드 2배
            OfflineSettlementResult result = settlement.Settle(
                clock, garden, new Inventory(), new Shop(), wallet,
                7200.0, GrowthOf, PriceOf, g => g * 2);

            Assert.AreEqual(1440 * 2L, result.GoldEarned);
            Assert.AreEqual(1440 * 2L, wallet.Gold);
        }

        [Test]
        public void Settle_SellsPreDisplayedShopStock_AsChannelA()
        {
            var clock = new GameClock();
            var inventory = new Inventory();
            var shop = new Shop();
            var wallet = new Wallet();
            inventory.Add(Grass, 100);
            shop.Display(0, Grass, 100, inventory);   // 진열대에 100개 (인벤 비움)

            // 정원은 비어 있음 → 채널 B 없음. 진열 재고만 손님이 오프라인 동안 구매.
            var settlement = new OfflineSettlement(new FakeAutomation());
            OfflineSettlementResult result = settlement.Settle(
                clock, new Garden(), inventory, shop, wallet, 7200.0, GrowthOf, PriceOf);

            // effective 4320초 / 10초 주기 = 432 손님. 손님당 최대 5개 → 100개 전량 소진(20손님분).
            Assert.AreEqual(100, result.SoldCount);
            Assert.AreEqual(100L, wallet.Gold);
            Assert.AreEqual(0, result.TotalHarvested);
            Assert.IsTrue(result.HasActivity);
        }

        // ---- 기획서 22장 시나리오: 8시간 vs 10시간(캡 초과) ----

        [Test]
        public void EightHourScenario_MatchesDesignChapter22()
        {
            var clock = new GameClock();
            var garden = new Garden();          // 초기 4칸
            var inventory = new Inventory();
            var wallet = new Wallet();
            for (int i = 0; i < 4; i++)
                garden.TryPlant(i, Grass, clock.ResourceSeconds);

            var settlement = new OfflineSettlement(new FixedOfflineAutomation());
            OfflineSettlementResult result = settlement.Settle(
                clock, garden, inventory, new Shop(), wallet, Cap, GrowthOf, PriceOf);

            // effective = 28800 × 0.6 = 17280. cycles/슬롯 = floor(17280/3) = 5760 → 4칸 = 23040 수확.
            Assert.AreEqual(17280.0, result.EffectiveResourceSeconds, 1e-6);
            Assert.IsFalse(result.WasCapped);                       // 정확히 8h — 캡 초과 아님
            Assert.AreEqual(23040, result.TotalHarvested);
            // 판매 상한 = floor(17280 × 0.5) = 8640 → 8640개 판매(1G), 나머지 14400 보관함
            Assert.AreEqual(8640, result.SoldCount);
            Assert.AreEqual(8640L, result.GoldEarned);
            Assert.AreEqual(14400, result.HarvestedToStorage);
            Assert.AreEqual(14400, inventory.GetCount(Grass));
        }

        [Test]
        public void TenHourScenario_IsCappedToEightHours_SameYield()
        {
            var eight = SettleHours(8.0);
            var ten = SettleHours(10.0);

            // 10시간이어도 8시간까지만 정산 → 유효초·수확·골드가 8시간과 동일, 캡 플래그만 켜짐
            Assert.IsFalse(eight.WasCapped);
            Assert.IsTrue(ten.WasCapped);
            Assert.AreEqual(eight.EffectiveResourceSeconds, ten.EffectiveResourceSeconds, 1e-6);
            Assert.AreEqual(eight.TotalHarvested, ten.TotalHarvested);
            Assert.AreEqual(eight.GoldEarned, ten.GoldEarned);
        }

        static OfflineSettlementResult SettleHours(double hours)
        {
            var clock = new GameClock();
            var garden = new Garden();
            for (int i = 0; i < 4; i++)
                garden.TryPlant(i, Grass, clock.ResourceSeconds);
            var settlement = new OfflineSettlement(new FixedOfflineAutomation());
            return settlement.Settle(clock, garden, new Inventory(), new Shop(), new Wallet(),
                hours * 3600.0, GrowthOf, PriceOf);
        }

        // ---- 고정 자동화 상수(임시값) ----

        [Test]
        public void FixedAutomation_HasDocumentedDefaults()
        {
            var automation = new FixedOfflineAutomation();
            Assert.AreEqual(1.0, automation.GardenHarvestRate, 1e-9);
            Assert.AreEqual(0.5, automation.ShopSalesRate, 1e-9);
        }

        [Test]
        public void FixedAutomation_ClampsNegativeRatesToZero()
        {
            var automation = new FixedOfflineAutomation(gardenHarvestRate: -1.0, shopSalesRate: -2.0);
            Assert.AreEqual(0.0, automation.GardenHarvestRate, 1e-9);
            Assert.AreEqual(0.0, automation.ShopSalesRate, 1e-9);
        }
    }
}
