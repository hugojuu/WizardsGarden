using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>조합 판정 결과 4분류(기획서 6·7장 4단계 파이프라인).</summary>
    public enum BrewOutcome
    {
        /// <summary>포션 완성 — 조성·재료·조건 모두 충족.</summary>
        Success,

        /// <summary>조성은 맞으나 지정 재료 미포함(2단계 실패) — 힌트 반환, 브루트포스 방지.</summary>
        MissingIngredient,

        /// <summary>조성·재료는 맞으나 조건 불충족(3단계 실패, 예: 야간/비/일식 아님).</summary>
        ConditionNotMet,

        /// <summary>어느 레시피에도 조성 불일치 — 실패 부산물 산출(4단계).</summary>
        FailureByproduct
    }

    /// <summary>실패 부산물 분기 종류.</summary>
    public enum FailureByproductKind
    {
        /// <summary>탁한 포션 — 총합 4 이하 또는 최다 원소 동률.</summary>
        Murky,

        /// <summary>수상한 침전물 — 최다 투입 원소 🔥·🌍.</summary>
        Sediment,

        /// <summary>희뿌연 안개병 — 최다 투입 원소 💧·💨.</summary>
        Mist
    }

    /// <summary>조합 판정 결과 구조체. Outcome에 따라 유효 필드가 다르다(팩토리로만 생성).</summary>
    public readonly struct BrewResult
    {
        public BrewOutcome Outcome { get; }

        /// <summary>관련 레시피 — Success/MissingIngredient/ConditionNotMet에서 유효(부산물은 null).</summary>
        public BrewRecipe Recipe { get; }

        /// <summary>부족한 지정 재료(id + 부족 수량) — MissingIngredient에서 유효.</summary>
        public IReadOnlyList<IngredientRequirementSpec> MissingIngredients { get; }

        /// <summary>미충족 조건 태그 — ConditionNotMet에서 유효.</summary>
        public IReadOnlyList<string> UnmetConditionTags { get; }

        /// <summary>플레이어 힌트 문구(MissingIngredient/ConditionNotMet).</summary>
        public string Hint { get; }

        /// <summary>부산물 분기 종류 — FailureByproduct에서 유효.</summary>
        public FailureByproductKind ByproductKind { get; }

        /// <summary>산출 부산물 데이터 — FailureByproduct에서 유효.</summary>
        public BrewByproduct Byproduct { get; }

        /// <summary>부산물 실제 판매가 = min(표기가, 투입 가치 × 30%) — FailureByproduct에서 유효.</summary>
        public int ByproductSalePrice { get; }

        public bool IsSuccess => Outcome == BrewOutcome.Success;

        BrewResult(
            BrewOutcome outcome,
            BrewRecipe recipe,
            IReadOnlyList<IngredientRequirementSpec> missing,
            IReadOnlyList<string> unmet,
            string hint,
            FailureByproductKind byproductKind,
            BrewByproduct byproduct,
            int byproductSalePrice)
        {
            Outcome = outcome;
            Recipe = recipe;
            MissingIngredients = missing;
            UnmetConditionTags = unmet;
            Hint = hint;
            ByproductKind = byproductKind;
            Byproduct = byproduct;
            ByproductSalePrice = byproductSalePrice;
        }

        public static BrewResult Success(BrewRecipe recipe) =>
            new BrewResult(BrewOutcome.Success, recipe, null, null, null, default, null, 0);

        public static BrewResult MissingIngredient(
            BrewRecipe recipe, IReadOnlyList<IngredientRequirementSpec> missing, string hint) =>
            new BrewResult(BrewOutcome.MissingIngredient, recipe, missing, null, hint, default, null, 0);

        public static BrewResult ConditionNotMet(
            BrewRecipe recipe, IReadOnlyList<string> unmet, string hint) =>
            new BrewResult(BrewOutcome.ConditionNotMet, recipe, null, unmet, hint, default, null, 0);

        public static BrewResult Failure(
            FailureByproductKind kind, BrewByproduct byproduct, int salePrice) =>
            new BrewResult(BrewOutcome.FailureByproduct, null, null, null, null, kind, byproduct, salePrice);
    }
}
