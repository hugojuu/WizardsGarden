using System;

namespace WizardGarden.Core
{
    /// <summary>
    /// 경제 곡선 공식 (기획서 8장). 같은 티어 내부 n번째 구매: Cost(n) = BaseCost × 1.15^n.
    /// </summary>
    public static class EconomyFormulas
    {
        /// <summary>밭 슬롯 기본가 (골드) — 첫 추가 구매(n=0) 가격. S04 결정: 초반 5분 수입으로 1~2칸 확장 가능선.</summary>
        public const int GardenSlotBaseCost = 20;

        /// <summary>티어 내부 반복 구매 가격 상승률 (기획서 8장 공식).</summary>
        public const double CostGrowthRate = 1.15;

        /// <summary>밭 슬롯 n번째 추가 구매 비용 (n = 이미 구매한 슬롯 수, 0부터). 반올림.</summary>
        public static int GardenSlotCost(int purchasedCount)
        {
            if (purchasedCount < 0)
                purchasedCount = 0;
            return (int)Math.Round(GardenSlotBaseCost * Math.Pow(CostGrowthRate, purchasedCount),
                MidpointRounding.AwayFromZero);
        }
    }
}
