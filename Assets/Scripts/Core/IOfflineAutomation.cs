namespace WizardGarden.Core
{
    /// <summary>
    /// 오프라인 정산에 쓰이는 구역별 자동 처리율 (S08 — 임시 자동화 상수의 인터페이스).
    ///
    /// ★ S09 교체 지점: 실제 자동화는 견습생 유닛(정원사·연금술사·점원)이 담당한다.
    /// 아직 견습생 시스템이 없으므로 지금은 고정 상수(FixedOfflineAutomation)로 대체하고,
    /// S09가 배치된 견습생 스탯 기반 계산으로 이 인터페이스를 갈아끼운다.
    /// 정산 로직(OfflineSettlement)은 이 인터페이스에만 의존하므로 코어 변경 없이 교체된다.
    /// </summary>
    public interface IOfflineAutomation
    {
        /// <summary>
        /// 정원 자동 수확 배율 — 슬롯 1칸이 오프라인 동안 자동 수확·재파종되는 속도(견습생 정원사 몫).
        /// 슬롯당 수확 횟수 = floor(유효 자원초 × GardenHarvestRate / 성장초). 0이면 자동 수확 없음.
        /// </summary>
        double GardenHarvestRate { get; }

        /// <summary>
        /// 상점 자동 판매 처리율 — 초당 자동으로 팔리는 개수(견습생 점원 몫). 수확분을 골드로 환산.
        /// 자동 판매 상한 = floor(유효 자원초 × ShopSalesRate). 0이면 자동 판매 없음(수확분은 보관함에만).
        /// </summary>
        double ShopSalesRate { get; }
    }

    /// <summary>
    /// 견습생 부재 시의 고정 자동화 상수 (S08 임시값 — ★ S09가 견습생 기반으로 교체).
    /// 값 근거: GardenHarvestRate=1.0(성장 완료마다 1회 자동 수확 = 견습생 1명분 baseline),
    /// ShopSalesRate=0.5(온라인 상점 처리량 5개/10초 = 0.5개/초와 동일한 baseline).
    /// </summary>
    public sealed class FixedOfflineAutomation : IOfflineAutomation
    {
        /// <summary>기본 정원 자동 수확 배율 (S09 교체 대상).</summary>
        public const double DefaultGardenHarvestRate = 1.0;

        /// <summary>기본 상점 자동 판매 처리율 (개/초, S09 교체 대상).</summary>
        public const double DefaultShopSalesRate = 0.5;

        public double GardenHarvestRate { get; }
        public double ShopSalesRate { get; }

        public FixedOfflineAutomation(
            double gardenHarvestRate = DefaultGardenHarvestRate,
            double shopSalesRate = DefaultShopSalesRate)
        {
            GardenHarvestRate = gardenHarvestRate > 0.0 ? gardenHarvestRate : 0.0;
            ShopSalesRate = shopSalesRate > 0.0 ? shopSalesRate : 0.0;
        }
    }
}
