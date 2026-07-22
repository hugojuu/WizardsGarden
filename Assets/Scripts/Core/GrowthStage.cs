namespace WizardGarden.Core
{
    /// <summary>식물 성장 단계 (새싹 → 성장 → 완료).</summary>
    public enum GrowthStage
    {
        Sprout = 0,
        Growing = 1,
        Mature = 2
    }

    /// <summary>진행도(0~1) → 성장 단계 변환.</summary>
    public static class GrowthStageUtility
    {
        /// <summary>새싹 → 성장 전환 진행도.</summary>
        public const double GrowingThreshold = 0.5;

        public static GrowthStage FromProgress(double progress)
        {
            if (progress >= 1.0)
                return GrowthStage.Mature;
            return progress >= GrowingThreshold ? GrowthStage.Growing : GrowthStage.Sprout;
        }
    }
}
