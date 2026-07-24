using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 배치된 견습생 기반 오프라인 자동화율 (순수 C#, S09 — ★ FixedOfflineAutomation 교체).
    /// 기획서 22장 "오프라인 = 유닛이 일한 셈"을 구현: 배치된 정원사가 정원 수확율을,
    /// 배치된 점원이 상점 판매율을 만든다. 각 직군의 유효 효율(ApprenticeEfficiency)을 합산한다.
    ///
    /// ★ 핵심 규칙: 배치된 견습생이 0명인 직군은 처리율 0 — 견습생 없으면 오프라인 자동화 없음.
    /// 이래야 "수동 → 견습생 영입·배치 → 자동화"의 진행이 성립한다(기획서 2-2장 척추 곡선).
    /// 연금술사(공방 가공)의 오프라인 처리는 정산 인터페이스(정원·상점) 밖이라 이후 세션 몫.
    /// </summary>
    public sealed class ApprenticeOfflineAutomation : IOfflineAutomation
    {
        /// <summary>정원 자동 수확 배율 (성장 완료 대비 — 최대 1.0). 배치 정원사 없으면 0.</summary>
        public double GardenHarvestRate { get; }

        /// <summary>상점 자동 판매 처리율 (개/초). 배치 점원 없으면 0.</summary>
        public double ShopSalesRate { get; }

        /// <summary>배치된 정원사 수 (요약·검증용).</summary>
        public int GardenerCount { get; }

        /// <summary>배치된 점원 수 (요약·검증용).</summary>
        public int ClerkCount { get; }

        public ApprenticeOfflineAutomation(IEnumerable<ApprenticeState> apprentices)
        {
            double gardenRate = 0.0;
            double salesRate = 0.0;
            int gardeners = 0;
            int clerks = 0;

            if (apprentices != null)
            {
                foreach (ApprenticeState a in apprentices)
                {
                    if (a == null || !a.IsPlaced)
                        continue;
                    double eff = ApprenticeEfficiency.EffectiveEfficiency(a);
                    switch (a.Job)
                    {
                        case Job.Gardener:
                            gardenRate += eff;
                            gardeners++;
                            break;
                        case Job.Clerk:
                            // 점원 100% 효율 = 온라인 상점 처리량(0.5개/초) 기준.
                            salesRate += eff * FixedOfflineAutomation.DefaultShopSalesRate;
                            clerks++;
                            break;
                    }
                }
            }

            // 정원 수확 배율은 성장 완료 속도를 넘을 수 없어 1.0으로 캡(추가 정원사는 병렬 밭 커버 — 밸런스는 이후).
            GardenHarvestRate = gardenRate > 1.0 ? 1.0 : gardenRate;
            ShopSalesRate = salesRate;
            GardenerCount = gardeners;
            ClerkCount = clerks;
        }
    }
}
