using UnityEngine;

namespace WizardGarden.Data
{
    /// <summary>식물 데이터 (기획서 4장 — 티어 1~5 × 4계열 = 20종).</summary>
    [CreateAssetMenu(menuName = "WizardGarden/Plant Data", fileName = "Plant_New")]
    public class PlantData : ItemData
    {
        [Header("식물")]
        [Tooltip("티어 (1~5)")]
        [Range(1, 5)]
        public int tier = 1;

        [Tooltip("성장 시간 (초) — 티어1 3초 / 티어2 15초 / 티어3 60초 / 티어4 300초 / 티어5 1500초")]
        [Min(0f)]
        public float growthSeconds = 3f;

        [Header("표시 (플레이스홀더)")]
        [Tooltip("플레이스홀더 표시용 이모지 — UI는 이 필드를 참조해서만 표시 (정식 아트로 후반 교체)")]
        public string displayEmoji = "🌱";
    }
}
