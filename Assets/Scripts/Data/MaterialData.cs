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
    }
}
