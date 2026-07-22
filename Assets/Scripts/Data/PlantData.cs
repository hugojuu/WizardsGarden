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

        [Tooltip("종자 해금 비용 (골드) — 0이면 처음부터 심을 수 있음 (티어1)")]
        [Min(0)]
        public int unlockCost = 0;
    }
}
