using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 조합 판정 엔진(순수 C#, MonoBehaviour 비의존 — 기획서 6·7장 4단계 파이프라인).
    /// 레시피 목록을 조성 인덱스(런타임 조회 구조)로 변환해 주입받는다. SO 의존은 어댑터에서.
    ///   1) 조성 매칭   — 투입 조성 합 == 레시피 조성
    ///   2) 재료 지정   — 지정 재료 개수 충족, 미충족 시 힌트
    ///   3) 조건 게이트 — conditionTags 충족(시간/날씨/계절 주입)
    ///   4) 실패 부산물 — 어느 레시피에도 조성 불일치 시 최다 원소로 분기
    /// "같은 조성, 다른 조건"(약초/생명수)은 조건이 더 구체적인(태그 많은) 레시피를 우선한다.
    /// </summary>
    public sealed class BrewMatcher
    {
        /// <summary>지정 재료 부족 시 기본 힌트(기획서 6장).</summary>
        public const string MissingIngredientHint = "핵심 재료가 빠진 것 같다";

        /// <summary>조건 불충족 시 기본 힌트.</summary>
        public const string ConditionNotMetHint = "지금은 이 포션이 완성되지 않는다";

        readonly Dictionary<ElementComposition, List<BrewRecipe>> _byComposition;
        readonly FailureByproductSet _byproducts;

        public BrewMatcher(IEnumerable<BrewRecipe> recipes, FailureByproductSet byproducts)
        {
            if (recipes == null) throw new ArgumentNullException(nameof(recipes));
            _byproducts = byproducts ?? throw new ArgumentNullException(nameof(byproducts));

            _byComposition = new Dictionary<ElementComposition, List<BrewRecipe>>();
            foreach (var recipe in recipes)
            {
                if (recipe == null) continue;
                if (!_byComposition.TryGetValue(recipe.Composition, out var bucket))
                {
                    bucket = new List<BrewRecipe>();
                    _byComposition[recipe.Composition] = bucket;
                }
                bucket.Add(recipe);
            }
        }

        /// <summary>투입 묶음 목록으로 판정(게임 런타임 진입점). 조성·id별 개수·총 가치를 집계 후 판정.</summary>
        public BrewResult Evaluate(IReadOnlyList<BrewInputItem> inputs, IBrewContext context)
        {
            var total = new ElementComposition();
            var counts = new Dictionary<string, int>();
            int value = 0;

            if (inputs != null)
            {
                for (int i = 0; i < inputs.Count; i++)
                {
                    var item = inputs[i];
                    if (item.Count <= 0) continue;

                    for (int c = 0; c < item.Count; c++)
                        total += item.UnitComposition;

                    value += item.UnitValue * item.Count;

                    if (!string.IsNullOrEmpty(item.ItemId))
                    {
                        counts.TryGetValue(item.ItemId, out int have);
                        counts[item.ItemId] = have + item.Count;
                    }
                }
            }

            return Evaluate(total, counts, value, context);
        }

        /// <summary>집계값으로 직접 판정(어댑터/테스트 진입점).</summary>
        public BrewResult Evaluate(
            ElementComposition totalComposition,
            IReadOnlyDictionary<string, int> ingredientCounts,
            int totalInputValue,
            IBrewContext context)
        {
            // 1단계: 조성 매칭
            if (!_byComposition.TryGetValue(totalComposition, out var candidates) || candidates.Count == 0)
                return MakeByproduct(totalComposition, totalInputValue);

            BrewRecipe bestSuccess = null;
            int bestSpecificity = -1;

            BrewRecipe conditionBlocked = null;
            List<string> conditionBlockedUnmet = null;

            BrewRecipe ingredientBlocked = null;
            List<IngredientRequirementSpec> ingredientBlockedMissing = null;

            foreach (var recipe in candidates)
            {
                // 2단계: 재료 지정
                if (!CheckIngredients(recipe, ingredientCounts, out var missing))
                {
                    if (ingredientBlocked == null)
                    {
                        ingredientBlocked = recipe;
                        ingredientBlockedMissing = missing;
                    }
                    continue;
                }

                // 3단계: 조건 게이트
                if (!BrewConditionInterpreter.AllSatisfied(recipe.ConditionTags, context, out var unmet))
                {
                    if (conditionBlocked == null)
                    {
                        conditionBlocked = recipe;
                        conditionBlockedUnmet = unmet;
                    }
                    continue;
                }

                // 성공 후보 — "같은 조성 다른 조건": 조건이 더 구체적(태그 많음)인 레시피 우선,
                // 동률이면 판매가 높은 쪽(결정적 타이브레이크).
                int specificity = recipe.ConditionTags?.Count ?? 0;
                if (specificity > bestSpecificity ||
                    (specificity == bestSpecificity && bestSuccess != null && recipe.BaseValue > bestSuccess.BaseValue))
                {
                    bestSuccess = recipe;
                    bestSpecificity = specificity;
                }
            }

            if (bestSuccess != null)
                return BrewResult.Success(bestSuccess);

            // 조성·재료는 맞았지만 조건만 불충족 → ConditionNotMet(재료 미충족보다 우선 안내).
            if (conditionBlocked != null)
                return BrewResult.ConditionNotMet(conditionBlocked, conditionBlockedUnmet, ConditionNotMetHint);

            // 조성만 맞고 지정 재료 미충족 → 힌트.
            if (ingredientBlocked != null)
                return BrewResult.MissingIngredient(ingredientBlocked, ingredientBlockedMissing, MissingIngredientHint);

            // 도달 불가(candidates 비어있지 않음) — 방어적으로 부산물.
            return MakeByproduct(totalComposition, totalInputValue);
        }

        static bool CheckIngredients(
            BrewRecipe recipe,
            IReadOnlyDictionary<string, int> ingredientCounts,
            out List<IngredientRequirementSpec> missing)
        {
            missing = null;
            var reqs = recipe.RequiredIngredients;
            if (reqs == null || reqs.Count == 0)
                return true;

            for (int i = 0; i < reqs.Count; i++)
            {
                var req = reqs[i];
                int have = 0;
                ingredientCounts?.TryGetValue(req.ItemId, out have);
                if (have < req.Count)
                {
                    missing ??= new List<IngredientRequirementSpec>();
                    missing.Add(new IngredientRequirementSpec(req.ItemId, req.Count - have));
                }
            }
            return missing == null;
        }

        BrewResult MakeByproduct(ElementComposition total, int inputValue)
        {
            bool hasDominant = total.TryGetDominantElement(out var dominant);

            FailureByproductKind kind;
            BrewByproduct product;

            if (total.Total <= 4 || !hasDominant)
            {
                kind = FailureByproductKind.Murky;
                product = _byproducts.Murky;
            }
            else
            {
                switch (dominant)
                {
                    case Element.Fire:
                    case Element.Earth:
                        kind = FailureByproductKind.Sediment;
                        product = _byproducts.Sediment;
                        break;
                    case Element.Water:
                    case Element.Wind:
                        kind = FailureByproductKind.Mist;
                        product = _byproducts.Mist;
                        break;
                    default: // Star(예약) 등 — 명세 없음, 안전하게 탁한 포션.
                        kind = FailureByproductKind.Murky;
                        product = _byproducts.Murky;
                        break;
                }
            }

            int cap = (int)Math.Floor(inputValue * 0.30);
            if (cap < 0) cap = 0;
            int salePrice = Math.Min(product.BaseValue, cap);
            return BrewResult.Failure(kind, product, salePrice);
        }
    }
}
