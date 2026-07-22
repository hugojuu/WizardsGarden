using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>경제 곡선 공식 검증 (S04) — Cost(n) = 20 × 1.15^n (기획서 8장).</summary>
    public class EconomyFormulasTests
    {
        [Test]
        public void GardenSlotCost_FollowsCurve()
        {
            Assert.AreEqual(20, EconomyFormulas.GardenSlotCost(0));  // 20 × 1.15^0
            Assert.AreEqual(23, EconomyFormulas.GardenSlotCost(1));  // 23.0
            Assert.AreEqual(26, EconomyFormulas.GardenSlotCost(2));  // 26.45
            Assert.AreEqual(30, EconomyFormulas.GardenSlotCost(3));  // 30.42
            Assert.AreEqual(35, EconomyFormulas.GardenSlotCost(4));  // 34.98
            Assert.AreEqual(40, EconomyFormulas.GardenSlotCost(5));  // 40.23
        }

        [Test]
        public void GardenSlotCost_IsMonotonicallyIncreasing()
        {
            for (int n = 0; n < 20; n++)
                Assert.Less(EconomyFormulas.GardenSlotCost(n), EconomyFormulas.GardenSlotCost(n + 1));
        }

        [Test]
        public void GardenSlotCost_NegativeCount_ClampsToBase()
        {
            Assert.AreEqual(EconomyFormulas.GardenSlotBaseCost, EconomyFormulas.GardenSlotCost(-3));
        }
    }
}
