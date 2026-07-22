namespace WizardGarden.Core
{
    /// <summary>
    /// 밭 슬롯 하나 (순수 C#). 심은 식물 id와 심은 시점(자원 시간 초)만 보관 —
    /// 진행도는 현재 자원 시간에서 매번 계산한다 (이벤트 미사용, S02 인계 방침).
    /// growthSeconds는 데이터(SO) 소관이라 저장하지 않고 호출 시 전달받는다.
    /// </summary>
    public sealed class GardenSlot
    {
        /// <summary>심은 식물 id (비어 있으면 null).</summary>
        public string PlantId { get; private set; }

        /// <summary>심은 시점의 자원 시간 누적 초.</summary>
        public double PlantedAtResourceSeconds { get; private set; }

        public bool IsEmpty => string.IsNullOrEmpty(PlantId);

        /// <summary>빈 슬롯에 심기. 이미 차 있거나 id가 비면 false.</summary>
        public bool TryPlant(string plantId, double nowResourceSeconds)
        {
            if (!IsEmpty || string.IsNullOrEmpty(plantId))
                return false;

            PlantId = plantId;
            PlantedAtResourceSeconds = nowResourceSeconds;
            return true;
        }

        /// <summary>성장 진행도 0~1 (빈 슬롯 0, growthSeconds 0 이하면 즉시 1).</summary>
        public double GetProgress(double nowResourceSeconds, double growthSeconds)
        {
            if (IsEmpty)
                return 0.0;
            if (growthSeconds <= 0.0)
                return 1.0;

            double progress = (nowResourceSeconds - PlantedAtResourceSeconds) / growthSeconds;
            if (progress < 0.0)
                return 0.0;
            return progress > 1.0 ? 1.0 : progress;
        }

        /// <summary>현재 성장 단계 (빈 슬롯이면 Sprout 반환 — 호출 전 IsEmpty 확인 권장).</summary>
        public GrowthStage GetStage(double nowResourceSeconds, double growthSeconds)
        {
            return GrowthStageUtility.FromProgress(GetProgress(nowResourceSeconds, growthSeconds));
        }

        /// <summary>수확 가능 여부 (심겨 있고 진행도 100%).</summary>
        public bool IsMature(double nowResourceSeconds, double growthSeconds)
        {
            return !IsEmpty && GetProgress(nowResourceSeconds, growthSeconds) >= 1.0;
        }

        /// <summary>완료 상태면 수확 — 슬롯을 비우고 수확한 식물 id 반환.</summary>
        public bool TryHarvest(double nowResourceSeconds, double growthSeconds, out string harvestedPlantId)
        {
            harvestedPlantId = null;
            if (!IsMature(nowResourceSeconds, growthSeconds))
                return false;

            harvestedPlantId = PlantId;
            Clear();
            return true;
        }

        public void Clear()
        {
            PlantId = null;
            PlantedAtResourceSeconds = 0.0;
        }

        /// <summary>세이브 복원용 — 판정 없이 상태를 그대로 되돌린다.</summary>
        public void Restore(string plantId, double plantedAtResourceSeconds)
        {
            PlantId = string.IsNullOrEmpty(plantId) ? null : plantId;
            PlantedAtResourceSeconds = IsEmpty ? 0.0 : plantedAtResourceSeconds;
        }
    }
}
