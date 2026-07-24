using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>견습생 효율·속도 공식 — 직군 기본(60/80/100%)·스탯 가중·작업 시간·이동 속도 검증 (S09).</summary>
    public class ApprenticeEfficiencyTests
    {
        [Test]
        public void JobBaseEfficiency_MatchesDesignChapter10()
        {
            Assert.AreEqual(0.60, ApprenticeEfficiency.JobBaseEfficiency(Job.Gardener), 1e-9);
            Assert.AreEqual(0.80, ApprenticeEfficiency.JobBaseEfficiency(Job.Alchemist), 1e-9);
            Assert.AreEqual(1.00, ApprenticeEfficiency.JobBaseEfficiency(Job.Clerk), 1e-9);
        }

        [Test]
        public void EffectiveEfficiency_ScalesWithPrimaryStat()
        {
            // 정원사 친화력 5 → 0.6 × (1 + 5×0.04) = 0.6 × 1.2 = 0.72
            var g = new ApprenticeState("g", Job.Gardener, Rarity.Common, 5, 0, 0, 0);
            Assert.AreEqual(0.72, ApprenticeEfficiency.EffectiveEfficiency(g), 1e-9);
        }

        [Test]
        public void EffectiveEfficiency_UsesPrimaryStatOnly()
        {
            // 점원은 상술이 주 스탯 — 친화력이 높아도 무관
            var c = new ApprenticeState("c", Job.Clerk, Rarity.Common, 20, 20, 5, 20);
            Assert.AreEqual(1.0 * (1 + 5 * 0.04), ApprenticeEfficiency.EffectiveEfficiency(c), 1e-9);
        }

        [Test]
        public void WorkSeconds_IsInverseOfEfficiency()
        {
            var g = new ApprenticeState("g", Job.Gardener, Rarity.Common, 5, 0, 0, 0); // eff 0.72
            Assert.AreEqual(1.4 / 0.72, ApprenticeEfficiency.WorkSeconds(1.4, g), 1e-6);
        }

        [Test]
        public void EffectiveEfficiency_NullIsZero()
        {
            Assert.AreEqual(0.0, ApprenticeEfficiency.EffectiveEfficiency(null), 1e-9);
        }

        [Test]
        public void MoveSpeed_ScalesLightlyWithPrimaryStat()
        {
            var g = new ApprenticeState("g", Job.Gardener, Rarity.Common, 6, 0, 0, 0);
            Assert.AreEqual(2.2 + 6 * 0.05, ApprenticeEfficiency.MoveSpeed(g), 1e-9);
        }
    }
}
