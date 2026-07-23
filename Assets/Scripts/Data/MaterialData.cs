using System.Collections.Generic;
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

        [Header("가공 레시피 — 주 원료 (S04 — 1차 가공)")]
        [Tooltip("가공 주 원료 (SO 참조 — 1차 가공은 식물, 2차는 가루, 3차는 정수/티어5 식물)")]
        public ItemData sourceItem;

        [Tooltip("주 원료 소비 수량")]
        [Min(1)]
        public int sourceCount = 1;

        [Tooltip("가공 시간 (자원 시간 초)")]
        [Min(0f)]
        public float processingSeconds = 8f;

        [Header("추가 원료 (S07 — 2차·3차 다중 입력)")]
        [Tooltip("주 원료 외에 함께 소비하는 원료 (예: 시간의 모래 = 정수 4종, 무지개 수정 = 물의 정수+불의 정수). 비었으면 1차 단일 입력 가공")]
        public List<IngredientRequirement> extraInputs = new List<IngredientRequirement>();
    }
}
