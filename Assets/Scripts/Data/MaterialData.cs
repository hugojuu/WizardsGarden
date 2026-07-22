using UnityEngine;

namespace WizardGarden.Data
{
    /// <summary>가공재료 데이터 (기획서 5장 — 1차 8종 / 2차 4종 / 3차 3종 = 15종).</summary>
    [CreateAssetMenu(menuName = "WizardGarden/Material Data", fileName = "Material_New")]
    public class MaterialData : ItemData
    {
        [Header("가공")]
        [Tooltip("가공 단계 (1: 말리기/분쇄, 2: 증류/추출, 3: 희귀 변환)")]
        [Range(1, 3)]
        public int processingStage = 1;

        [Header("가공 레시피 (S04 — 1차 가공)")]
        [Tooltip("가공 원료 (SO 참조 — 1차 가공은 식물)")]
        public ItemData sourceItem;

        [Tooltip("원료 소비 수량")]
        [Min(1)]
        public int sourceCount = 1;

        [Tooltip("가공 시간 (자원 시간 초)")]
        [Min(0f)]
        public float processingSeconds = 8f;
    }
}
