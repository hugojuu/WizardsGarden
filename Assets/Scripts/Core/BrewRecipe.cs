using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>지정 재료 요구 1건 — 아이템 id + 필요 개수(재료 지정 판정용).</summary>
    public readonly struct IngredientRequirementSpec
    {
        public readonly string ItemId;
        public readonly int Count;

        public IngredientRequirementSpec(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    /// <summary>
    /// 조합 판정에 쓰는 런타임 레시피(순수 데이터) — PotionData(SO)를 어댑터에서 변환해 주입한다.
    /// 코어는 SO에 의존하지 않는다.
    /// </summary>
    public sealed class BrewRecipe
    {
        public string PotionId { get; }
        public string DisplayName { get; }

        /// <summary>목표 조성 — 투입 재료 조성 합이 이것과 정확히 같아야 매칭(1단계).</summary>
        public ElementComposition Composition { get; }

        /// <summary>지정 재료(2단계). 비어 있으면 조성만으로 판정.</summary>
        public IReadOnlyList<IngredientRequirementSpec> RequiredIngredients { get; }

        /// <summary>조건 태그(3단계). 비어 있으면 상시 제조.</summary>
        public IReadOnlyList<string> ConditionTags { get; }

        public int BaseValue { get; }
        public PotionCategory Category { get; }

        public bool HasRequiredIngredients => RequiredIngredients != null && RequiredIngredients.Count > 0;
        public bool HasConditions => ConditionTags != null && ConditionTags.Count > 0;

        public BrewRecipe(
            string potionId,
            string displayName,
            ElementComposition composition,
            int baseValue,
            PotionCategory category = PotionCategory.Special,
            IReadOnlyList<IngredientRequirementSpec> requiredIngredients = null,
            IReadOnlyList<string> conditionTags = null)
        {
            PotionId = potionId;
            DisplayName = displayName;
            Composition = composition;
            BaseValue = baseValue;
            Category = category;
            RequiredIngredients = requiredIngredients;
            ConditionTags = conditionTags;
        }
    }

    /// <summary>실패 부산물 1종(id·이름·표기가). 실제 판매가는 매칭 엔진이 투입 가치로 산정.</summary>
    public sealed class BrewByproduct
    {
        public string Id { get; }
        public string DisplayName { get; }

        /// <summary>표기가(상한). 실제 판매가 = min(표기가, 투입 재료 가치 × 30%).</summary>
        public int BaseValue { get; }

        public BrewByproduct(string id, string displayName, int baseValue)
        {
            Id = id;
            DisplayName = displayName;
            BaseValue = baseValue;
        }
    }

    /// <summary>실패 부산물 3종 세트(기획서 6장 실험 일지) — 최다 투입 원소에 따라 분기.</summary>
    public sealed class FailureByproductSet
    {
        /// <summary>탁한 포션 — 총합 4 이하 또는 최다 원소 동률.</summary>
        public BrewByproduct Murky { get; }

        /// <summary>수상한 침전물 — 최다 투입 원소가 🔥·🌍(무거움).</summary>
        public BrewByproduct Sediment { get; }

        /// <summary>희뿌연 안개병 — 최다 투입 원소가 💧·💨(가벼움).</summary>
        public BrewByproduct Mist { get; }

        public FailureByproductSet(BrewByproduct murky, BrewByproduct sediment, BrewByproduct mist)
        {
            Murky = murky;
            Sediment = sediment;
            Mist = mist;
        }
    }
}
