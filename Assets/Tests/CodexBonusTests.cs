using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>도감 완성도 → 글로벌 골드 보너스 곡선 검증 (기획서 7장, S06).</summary>
    public class CodexBonusTests
    {
        [Test]
        public void Fraction_BelowQuarter_IsZero()
        {
            Assert.AreEqual(0.0, CodexBonus.GoldBonusFraction(0.0), 1e-9);
            Assert.AreEqual(0.0, CodexBonus.GoldBonusFraction(0.24), 1e-9);
        }

        [Test]
        public void Fraction_ThresholdBoundaries()
        {
            Assert.AreEqual(0.05, CodexBonus.GoldBonusFraction(0.25), 1e-9); // 25%
            Assert.AreEqual(0.05, CodexBonus.GoldBonusFraction(0.49), 1e-9);
            Assert.AreEqual(0.15, CodexBonus.GoldBonusFraction(0.50), 1e-9); // 50%
            Assert.AreEqual(0.15, CodexBonus.GoldBonusFraction(0.74), 1e-9);
            Assert.AreEqual(0.30, CodexBonus.GoldBonusFraction(0.75), 1e-9); // 75%
            Assert.AreEqual(0.30, CodexBonus.GoldBonusFraction(0.99), 1e-9);
            Assert.AreEqual(0.50, CodexBonus.GoldBonusFraction(1.0), 1e-9);  // 100%
        }

        [Test]
        public void Fraction_ExactFractionsSurviveFloatingPoint()
        {
            // 5/20 = 0.25, 15/20 = 0.75 등 정확 분수도 경계 통과 (epsilon 여유)
            Assert.AreEqual(0.05, CodexBonus.GoldBonusFraction(5.0 / 20.0), 1e-9);
            Assert.AreEqual(0.15, CodexBonus.GoldBonusFraction(10.0 / 20.0), 1e-9);
            Assert.AreEqual(0.30, CodexBonus.GoldBonusFraction(15.0 / 20.0), 1e-9);
            Assert.AreEqual(0.50, CodexBonus.GoldBonusFraction(20.0 / 20.0), 1e-9);
        }

        [Test]
        public void ApplyBonus_MultipliesAndFloors()
        {
            Assert.AreEqual(100, CodexBonus.ApplyBonus(100, 0.10)); // 보너스 구간 아님
            Assert.AreEqual(105, CodexBonus.ApplyBonus(100, 0.25)); // +5%
            Assert.AreEqual(115, CodexBonus.ApplyBonus(100, 0.50)); // +15%
            Assert.AreEqual(130, CodexBonus.ApplyBonus(100, 0.75)); // +30%
            Assert.AreEqual(150, CodexBonus.ApplyBonus(100, 1.0));  // +50%
        }

        [Test]
        public void ApplyBonus_FloorsFraction()
        {
            // 50G × 1.05 = 52.5 → 52 (내림)
            Assert.AreEqual(52, CodexBonus.ApplyBonus(50, 0.25));
        }

        [Test]
        public void ApplyBonus_NonPositiveGoldUnchanged()
        {
            Assert.AreEqual(0, CodexBonus.ApplyBonus(0, 1.0));
            Assert.AreEqual(-5, CodexBonus.ApplyBonus(-5, 1.0));
        }
    }
}
