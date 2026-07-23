using System.Collections.Generic;
using WizardGarden.Core;
using WizardGarden.Data;

namespace WizardGarden
{
    /// <summary>
    /// PotionData(SO) → BrewRecipe(순수 코어) 변환 어댑터. 매칭 엔진은 SO에 의존하지 않으므로
    /// SO 접점은 여기 한 곳에 모은다(S06 조합 UI가 이 팩토리로 BrewMatcher를 구성).
    /// </summary>
    public static class BrewRecipeFactory
    {
        /// <summary>실패 부산물 3종의 기본 id(기획서 6장 실험 일지 — SO 에셋 authoring 시 이 id 사용 권장).</summary>
        public const string MurkyId = "potion_murky";
        public const string SedimentId = "potion_sediment";
        public const string MistId = "potion_mist";

        public static BrewRecipe ToRecipe(PotionData potion)
        {
            if (potion == null) return null;

            List<IngredientRequirementSpec> reqs = null;
            if (potion.requiredIngredients != null && potion.requiredIngredients.Count > 0)
            {
                reqs = new List<IngredientRequirementSpec>(potion.requiredIngredients.Count);
                foreach (var r in potion.requiredIngredients)
                {
                    if (r == null || r.item == null || string.IsNullOrEmpty(r.item.id)) continue;
                    reqs.Add(new IngredientRequirementSpec(r.item.id, r.count));
                }
            }

            List<string> tags = null;
            if (potion.conditionTags != null && potion.conditionTags.Count > 0)
                tags = new List<string>(potion.conditionTags);

            return new BrewRecipe(
                potion.id,
                potion.displayName,
                potion.composition,
                potion.baseValue,
                potion.category,
                reqs,
                tags);
        }

        /// <summary>포션 목록을 런타임 레시피 목록으로 변환(부산물 3종은 별도로 분리해 제외).</summary>
        public static List<BrewRecipe> ToRecipes(IEnumerable<PotionData> potions)
        {
            var list = new List<BrewRecipe>();
            if (potions == null) return list;

            foreach (var potion in potions)
            {
                if (potion == null || string.IsNullOrEmpty(potion.id)) continue;
                if (potion.id == MurkyId || potion.id == SedimentId || potion.id == MistId) continue;
                list.Add(ToRecipe(potion));
            }
            return list;
        }

        /// <summary>부산물 3종 SO에서 세트 구성. 누락 시 기획서 6장 기본값으로 대체.</summary>
        public static FailureByproductSet BuildByproducts(
            PotionData murky = null, PotionData sediment = null, PotionData mist = null)
        {
            return new FailureByproductSet(
                ToByproduct(murky, MurkyId, "탁한 포션", 5),
                ToByproduct(sediment, SedimentId, "수상한 침전물", 15),
                ToByproduct(mist, MistId, "희뿌연 안개병", 12));
        }

        static BrewByproduct ToByproduct(PotionData so, string fallbackId, string fallbackName, int fallbackValue)
        {
            if (so != null && !string.IsNullOrEmpty(so.id))
                return new BrewByproduct(so.id, so.displayName, so.baseValue);
            return new BrewByproduct(fallbackId, fallbackName, fallbackValue);
        }
    }
}
