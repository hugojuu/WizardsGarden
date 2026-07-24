using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>
    /// 견습생 기반 오프라인 자동화율 (S09 — FixedOfflineAutomation 교체) 검증.
    /// ★ 핵심 규칙: 배치 견습생 0명 → 처리율 0. 정원사→정원 수확율, 점원→상점 판매율.
    /// </summary>
    public class ApprenticeOfflineAutomationTests
    {
        static ApprenticeRoster RosterWith(params ApprenticeState[] apprentices)
        {
            var roster = new ApprenticeRoster();
            foreach (ApprenticeState a in apprentices)
                roster.AddIfMissing(a);
            return roster;
        }

        static ApprenticeState Gardener(string id, int affinity) =>
            new ApprenticeState(id, Job.Gardener, Rarity.Common, affinity, 2, 2, 2);
        static ApprenticeState Clerk(string id, int trade) =>
            new ApprenticeState(id, Job.Clerk, Rarity.Common, 2, 2, trade, 2);
        static ApprenticeState Alchemist(string id) =>
            new ApprenticeState(id, Job.Alchemist, Rarity.Common, 2, 5, 2, 2);

        [Test]
        public void NoPlacedApprentices_YieldsZeroRates()
        {
            var roster = RosterWith(Gardener("g", 5), Clerk("c", 5));   // 보유하지만 배치 안 함
            var automation = new ApprenticeOfflineAutomation(roster.Placed);

            Assert.AreEqual(0.0, automation.GardenHarvestRate, 1e-9);
            Assert.AreEqual(0.0, automation.ShopSalesRate, 1e-9);
            Assert.AreEqual(0, automation.GardenerCount);
            Assert.AreEqual(0, automation.ClerkCount);
        }

        [Test]
        public void NullApprentices_YieldsZeroRates()
        {
            var automation = new ApprenticeOfflineAutomation(null);
            Assert.AreEqual(0.0, automation.GardenHarvestRate, 1e-9);
            Assert.AreEqual(0.0, automation.ShopSalesRate, 1e-9);
        }

        [Test]
        public void PlacedGardener_ProducesGardenRate_NoSales()
        {
            var roster = RosterWith(Gardener("g", 5));
            roster.TryPlace("g");
            var automation = new ApprenticeOfflineAutomation(roster.Placed);

            // 정원사 친화력 5 → 0.6 × 1.2 = 0.72
            Assert.AreEqual(0.72, automation.GardenHarvestRate, 1e-9);
            Assert.AreEqual(0.0, automation.ShopSalesRate, 1e-9);
            Assert.AreEqual(1, automation.GardenerCount);
        }

        [Test]
        public void PlacedClerk_ProducesSalesRate_NoHarvest()
        {
            var roster = RosterWith(Clerk("c", 5));
            roster.TryPlace("c");
            var automation = new ApprenticeOfflineAutomation(roster.Placed);

            // 점원 상술 5 → 1.0 × 1.2 × 0.5(=온라인 상점 기준) = 0.6
            Assert.AreEqual(0.6, automation.ShopSalesRate, 1e-9);
            Assert.AreEqual(0.0, automation.GardenHarvestRate, 1e-9);
            Assert.AreEqual(1, automation.ClerkCount);
        }

        [Test]
        public void PlacedAlchemist_DoesNotAffectGardenOrShop()
        {
            var roster = RosterWith(Alchemist("a"));
            roster.TryPlace("a");
            var automation = new ApprenticeOfflineAutomation(roster.Placed);

            Assert.AreEqual(0.0, automation.GardenHarvestRate, 1e-9);
            Assert.AreEqual(0.0, automation.ShopSalesRate, 1e-9);
        }

        [Test]
        public void GardenHarvestRate_CapsAtOne()
        {
            // 친화력 20 → 0.6 × (1 + 0.8) = 1.08 → 캡 1.0
            var roster = RosterWith(Gardener("g", 20));
            roster.TryPlace("g");
            var automation = new ApprenticeOfflineAutomation(roster.Placed);
            Assert.AreEqual(1.0, automation.GardenHarvestRate, 1e-9);
        }

        [Test]
        public void IntegratesWithOfflineSettlement_ZeroWithoutApprentices()
        {
            // 견습생 0명 배치 → 정원에 심겨 있어도 오프라인 자동 수확 없음(성장만 진행)
            var clock = new GameClock();
            var garden = new Garden();
            garden.TryPlant(0, "grass", clock.ResourceSeconds);
            var inventory = new Inventory();

            var automation = new ApprenticeOfflineAutomation(new ApprenticeRoster().Placed);
            var settlement = new OfflineSettlement(automation);
            OfflineSettlementResult result = settlement.Settle(
                clock, garden, inventory, new Shop(), new Wallet(),
                7200.0, id => 3.0, id => 1);

            Assert.AreEqual(0, result.TotalHarvested);   // 자동 수확 없음
            Assert.AreEqual(0L, result.GoldEarned);
            Assert.Greater(clock.ResourceSeconds, 0.0);  // 성장(자원 시간)은 진행됨
        }
    }
}
