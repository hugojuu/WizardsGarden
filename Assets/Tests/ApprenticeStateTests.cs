using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>견습생 런타임 상태 — 스탯 클램프·경험치·레벨업(주 스탯 +1)·각성(희귀도 상승) 검증 (S09).</summary>
    public class ApprenticeStateTests
    {
        static ApprenticeState Gardener(int affinity = 5) =>
            new ApprenticeState("appr_test", Job.Gardener, Rarity.Common, affinity, 2, 2, 2);

        [Test]
        public void Constructor_ClampsNegativeStats_AndLevelRange()
        {
            var a = new ApprenticeState("id", Job.Clerk, Rarity.Rare, -3, 4, 2, 1, level: 99);
            Assert.AreEqual(0, a.Affinity);
            Assert.AreEqual(ApprenticeState.MaxLevel, a.Level);
        }

        [Test]
        public void PrimaryStat_MatchesJob()
        {
            Assert.AreEqual(5, new ApprenticeState("g", Job.Gardener, Rarity.Common, 5, 1, 1, 1).PrimaryStat);
            Assert.AreEqual(6, new ApprenticeState("a", Job.Alchemist, Rarity.Common, 1, 6, 1, 1).PrimaryStat);
            Assert.AreEqual(7, new ApprenticeState("c", Job.Clerk, Rarity.Common, 1, 1, 7, 1).PrimaryStat);
        }

        [Test]
        public void AddExperience_LevelsUp_AndRaisesPrimaryStat()
        {
            var a = Gardener(affinity: 5);
            // Lv1 → 필요치 10*1 = 10
            int gained = a.AddExperience(10);
            Assert.AreEqual(1, gained);
            Assert.AreEqual(2, a.Level);
            Assert.AreEqual(6, a.Affinity);   // 정원사 주 스탯 +1
        }

        [Test]
        public void AddExperience_MultipleLevels_InOneCall()
        {
            var a = Gardener();
            // 10(→2) + 20(→3) = 30 필요, 넉넉히 투입
            int gained = a.AddExperience(100);
            Assert.GreaterOrEqual(gained, 2);
            Assert.Greater(a.Level, 2);
        }

        [Test]
        public void AddExperience_CapsAtMaxLevel()
        {
            var a = Gardener();
            a.AddExperience(1_000_000);
            Assert.AreEqual(ApprenticeState.MaxLevel, a.Level);
            Assert.AreEqual(0, a.AddExperience(500));   // 만렙은 더 오르지 않음
        }

        [Test]
        public void Awaken_RequiresLevelTen_AndRaisesRarity()
        {
            var a = Gardener();
            Assert.IsFalse(a.CanAwaken);          // 레벨 1
            Assert.IsFalse(a.TryAwaken());

            a.AddExperience(1_000_000);           // 만렙(>=10)
            Assert.IsTrue(a.CanAwaken);
            Assert.IsTrue(a.TryAwaken());
            Assert.AreEqual(Rarity.Rare, a.Rarity);   // 일반 → 희귀
        }

        [Test]
        public void Awaken_StopsAtLegendary()
        {
            var a = new ApprenticeState("l", Job.Clerk, Rarity.Legendary, 10, 10, 10, 10, level: 20);
            Assert.IsFalse(a.CanAwaken);
            Assert.IsFalse(a.TryAwaken());
        }

        [Test]
        public void StatTotal_SumsFourStats()
        {
            Assert.AreEqual(14, new ApprenticeState("x", Job.Clerk, Rarity.Common, 5, 2, 5, 2).StatTotal);
        }
    }
}
