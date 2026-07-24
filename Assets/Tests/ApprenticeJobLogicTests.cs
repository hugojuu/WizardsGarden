using System.Collections.Generic;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>직군별 행동 결정 — 실제 코어 세계 상태로 정원사/연금술사/점원의 다음 작업을 검증 (S09).</summary>
    public class ApprenticeJobLogicTests
    {
        const string Grass = "plant_grass";     // 성장 3초
        const string Leaf = "material_leaf";
        static double GrowthOf(string id) => id == Grass ? 3.0 : 0.0;
        static double ProcessingOf(string id) => id == Leaf ? 5.0 : 0.0;
        static int PriceOf(string id) => id == Grass ? 1 : (id == Leaf ? 5 : 0);

        // ---- 정원사 ----

        [Test]
        public void Gardener_HarvestsMatureSlotFirst()
        {
            var garden = new Garden();
            garden.TryPlant(0, Grass, 0.0);
            garden.TryPlant(1, Grass, 0.0);

            WorkOrder order = ApprenticeJobLogic.DecideGardener(garden, now: 10.0, GrowthOf, Grass);
            Assert.AreEqual(WorkKind.Harvest, order.Kind);
            Assert.AreEqual(0, order.Index);   // 앞 슬롯 우선
        }

        [Test]
        public void Gardener_ReplantsEmptySlot_WhenNoneMature()
        {
            var garden = new Garden();   // 전부 빈 밭
            WorkOrder order = ApprenticeJobLogic.DecideGardener(garden, now: 1.0, GrowthOf, Grass);
            Assert.AreEqual(WorkKind.Plant, order.Kind);
            Assert.AreEqual(0, order.Index);
            Assert.AreEqual(Grass, order.ItemId);
        }

        [Test]
        public void Gardener_NoSeed_NoMature_ReturnsNone()
        {
            var garden = new Garden();
            garden.TryPlant(0, Grass, 0.0);   // 아직 안 여문 상태에서
            WorkOrder order = ApprenticeJobLogic.DecideGardener(garden, now: 0.5, GrowthOf, preferredSeedId: null);
            // 심을 종자 없음 + 빈 밭 남았지만 seed=null → 재파종 불가. 성숙 슬롯도 없음.
            // 단, 빈 슬롯(1~3)은 seed=null이라 재파종 안 함 → None
            Assert.AreEqual(WorkKind.None, order.Kind);
        }

        // ---- 연금술사 ----

        [Test]
        public void Alchemist_StartsProcess_WhenInputAvailable()
        {
            var workshop = new Workshop();
            var inv = new Inventory();
            inv.Add(Grass, 2);
            var recipes = new List<SimpleRecipe> { new SimpleRecipe(Leaf, Grass, 2) };

            WorkOrder order = ApprenticeJobLogic.DecideAlchemist(workshop, inv, 0.0, ProcessingOf, recipes);
            Assert.AreEqual(WorkKind.StartProcess, order.Kind);
            Assert.AreEqual(Leaf, order.ItemId);
            Assert.AreEqual(Grass, order.InputId);
            Assert.AreEqual(2, order.InputCount);
        }

        [Test]
        public void Alchemist_Collects_WhenComplete()
        {
            var workshop = new Workshop();
            var inv = new Inventory();
            inv.Add(Grass, 2);
            workshop.TryStart(Leaf, 1, Grass, 2, inv, 0.0);   // 5초 가공

            WorkOrder order = ApprenticeJobLogic.DecideAlchemist(workshop, inv, 10.0, ProcessingOf, null);
            Assert.AreEqual(WorkKind.Collect, order.Kind);
        }

        [Test]
        public void Alchemist_Waits_WhenProcessingInProgress()
        {
            var workshop = new Workshop();
            var inv = new Inventory();
            inv.Add(Grass, 2);
            workshop.TryStart(Leaf, 1, Grass, 2, inv, 0.0);

            WorkOrder order = ApprenticeJobLogic.DecideAlchemist(workshop, inv, 1.0, ProcessingOf, null);
            Assert.AreEqual(WorkKind.None, order.Kind);   // 아직 진행 중
        }

        [Test]
        public void Alchemist_Idle_NoInput_ReturnsNone()
        {
            var recipes = new List<SimpleRecipe> { new SimpleRecipe(Leaf, Grass, 2) };
            WorkOrder order = ApprenticeJobLogic.DecideAlchemist(new Workshop(), new Inventory(), 0.0, ProcessingOf, recipes);
            Assert.AreEqual(WorkKind.None, order.Kind);
        }

        // ---- 점원 ----

        [Test]
        public void Clerk_DisplaysStock_IntoEmptyStand()
        {
            var shop = new Shop();
            var inv = new Inventory();
            inv.Add(Grass, 25);

            WorkOrder order = ApprenticeJobLogic.DecideClerk(shop, inv, PriceOf);
            Assert.AreEqual(WorkKind.Display, order.Kind);
            Assert.AreEqual(0, order.Index);
            Assert.AreEqual(Grass, order.ItemId);
            Assert.AreEqual(ApprenticeJobLogic.DisplayBatch, order.Count);   // 25 중 10개
        }

        [Test]
        public void Clerk_NoEmptyStand_ReturnsNone()
        {
            var shop = new Shop();
            var inv = new Inventory();
            inv.Add(Grass, 100);
            // 3칸 모두 채움
            shop.Display(0, Grass, 5, inv);
            shop.Display(1, Grass, 5, inv);
            shop.Display(2, Grass, 5, inv);

            WorkOrder order = ApprenticeJobLogic.DecideClerk(shop, inv, PriceOf);
            Assert.AreEqual(WorkKind.None, order.Kind);
        }

        [Test]
        public void Clerk_SkipsPricelessStock()
        {
            var shop = new Shop();
            var inv = new Inventory();
            inv.Add("potion_murky", 10);   // PriceOf = 0

            WorkOrder order = ApprenticeJobLogic.DecideClerk(shop, inv, PriceOf);
            Assert.AreEqual(WorkKind.None, order.Kind);
        }
    }
}
