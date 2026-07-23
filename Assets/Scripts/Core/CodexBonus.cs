using System;

namespace WizardGarden.Core
{
    /// <summary>
    /// 도감 완성도 → 글로벌 골드 보너스 곡선 (기획서 7장). 순수 함수 — EditMode 테스트 대상.
    ///   발견율 25% → +5% / 50% → +15% / 75% → +30% / 100% → +50%.
    /// 판매 골드에 곱연산으로 얹힌다(내림).
    /// </summary>
    public static class CodexBonus
    {
        // 부동소수 완성율(예: 5/20)이 경계에서 미세하게 못 미치는 것을 방지하는 여유값.
        const double Epsilon = 1e-9;

        /// <summary>완성율(0~1)에 대응하는 골드 보너스 배율의 추가분(0, 0.05, 0.15, 0.30, 0.50).</summary>
        public static double GoldBonusFraction(double completionRatio)
        {
            if (completionRatio >= 1.0 - Epsilon) return 0.50;
            if (completionRatio >= 0.75 - Epsilon) return 0.30;
            if (completionRatio >= 0.50 - Epsilon) return 0.15;
            if (completionRatio >= 0.25 - Epsilon) return 0.05;
            return 0.0;
        }

        /// <summary>판매 골드에 완성도 보너스를 곱연산으로 적용(내림). 0 이하 골드는 그대로.</summary>
        public static long ApplyBonus(long baseGold, double completionRatio)
        {
            if (baseGold <= 0)
                return baseGold;

            double fraction = GoldBonusFraction(completionRatio);
            long bonus = (long)Math.Floor(baseGold * fraction);
            return baseGold + bonus;
        }
    }
}
