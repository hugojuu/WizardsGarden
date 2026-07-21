using System;
using System.Collections.Generic;
using UnityEngine;
using WizardGarden.Core;

namespace WizardGarden.Data
{
    /// <summary>포션 데이터 (기획서 6장 — 30종 + 실험 일지 3종). baseValue = 판매가.</summary>
    [CreateAssetMenu(menuName = "WizardGarden/Potion Data", fileName = "Potion_New")]
    public class PotionData : ItemData
    {
        [Header("포션")]
        [Tooltip("카테고리 (공격/회복/방어/보조/특수)")]
        public PotionCategory category = PotionCategory.Attack;

        [Tooltip("지정 재료 목록 — 조성이 맞아도 이 재료가 없으면 실패 (매칭 2단계). 비었으면 조성만으로 판정")]
        public List<IngredientRequirement> requiredIngredients = new List<IngredientRequirement>();

        [Tooltip("조건 태그 — 야간/날씨/계절 게이트 (매칭 3단계, 해석은 S05). 예: night_only, weather:rain, weather:eclipse")]
        public List<string> conditionTags = new List<string>();

        [Tooltip("장비 효과 ID — 장비 가능 포션만 (예: equip_crit_up_15). 아니면 비움")]
        public string equipEffectId;
    }

    /// <summary>지정 재료 1건 — 식물(PlantData) 또는 가공재료(MaterialData) 참조 + 개수.</summary>
    [Serializable]
    public class IngredientRequirement
    {
        [Tooltip("지정 재료 (식물 또는 가공재료 SO)")]
        public ItemData item;

        [Tooltip("필요 개수")]
        [Min(1)]
        public int count = 1;
    }
}
