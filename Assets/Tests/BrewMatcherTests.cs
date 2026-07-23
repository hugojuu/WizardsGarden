using System.Collections.Generic;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>
    /// 조합 매칭 엔진 검증(S05) — 기획서 6·7장 4단계 파이프라인.
    /// 33종 전 레시피 매칭 + 실패 3분기 + 재료 지정/힌트 + 조건부 + "같은 조성 다른 조건" 우선순위
    /// + 5번째 원소(별⭐) 칸 동작 + 경계 케이스.
    /// </summary>
    public class BrewMatcherTests
    {
        BrewMatcher _matcher;

        [SetUp]
        public void SetUp() => _matcher = BrewFixture.Matcher();

        static IBrewContext Day() => new BrewContext(TimeOfDay.Day, Weather.Clear);
        static IBrewContext Night() => new BrewContext(TimeOfDay.Night, Weather.Clear);
        static IBrewContext Rain() => new BrewContext(TimeOfDay.Day, Weather.Rain);
        static IBrewContext Eclipse() => new BrewContext(TimeOfDay.Day, Weather.Eclipse);

        // ---------- 픽스처 sanity ----------

        [Test]
        public void Fixture_Has30Recipes_And3Byproducts()
        {
            Assert.AreEqual(30, BrewFixture.Recipes().Count, "레시피 30종");
            var bp = BrewFixture.Byproducts();
            Assert.IsNotNull(bp.Murky);
            Assert.IsNotNull(bp.Sediment);
            Assert.IsNotNull(bp.Mist);
        }

        // ---------- 33종 전 레시피 성공 매칭 (재료·조건 충족 시) ----------

        static IEnumerable<TestCaseData> AllRecipes()
        {
            foreach (var r in BrewFixture.Recipes())
                yield return new TestCaseData(r).SetName($"Success_{r.PotionId}");
        }

        [TestCaseSource(nameof(AllRecipes))]
        public void EveryRecipe_WithSatisfiedInputs_Succeeds(BrewRecipe recipe)
        {
            var counts = CountsFor(recipe);
            var context = ContextFor(recipe);

            var result = _matcher.Evaluate(recipe.Composition, counts, 100, context);

            Assert.AreEqual(BrewOutcome.Success, result.Outcome,
                $"{recipe.DisplayName} 성공해야 함");
            Assert.AreEqual(recipe.PotionId, result.Recipe.PotionId,
                $"{recipe.DisplayName} 매칭 결과가 자기 자신이어야 함");
        }

        static Dictionary<string, int> CountsFor(BrewRecipe r)
        {
            if (!r.HasRequiredIngredients) return null;
            var d = new Dictionary<string, int>();
            foreach (var req in r.RequiredIngredients) d[req.ItemId] = req.Count;
            return d;
        }

        static IBrewContext ContextFor(BrewRecipe r)
        {
            var tod = TimeOfDay.Day;
            var weather = Weather.Clear;
            if (r.ConditionTags != null)
            {
                foreach (var tag in r.ConditionTags)
                {
                    switch (tag)
                    {
                        case "night_only": tod = TimeOfDay.Night; break;
                        case "weather:rain": weather = Weather.Rain; break;
                        case "weather:eclipse": weather = Weather.Eclipse; break;
                    }
                }
            }
            return new BrewContext(tod, weather);
        }

        // ---------- 실패 부산물 3분기 ----------

        [Test]
        public void Failure_Sediment_WhenDominantFire()
        {
            var r = _matcher.Evaluate(new ElementComposition(5, 0, 0, 0), null, 100, Day());
            Assert.AreEqual(BrewOutcome.FailureByproduct, r.Outcome);
            Assert.AreEqual(FailureByproductKind.Sediment, r.ByproductKind);
            Assert.AreEqual("potion_sediment", r.Byproduct.Id);
        }

        [Test]
        public void Failure_Sediment_WhenDominantEarth()
        {
            var r = _matcher.Evaluate(new ElementComposition(0, 0, 6, 1), null, 100, Day());
            Assert.AreEqual(FailureByproductKind.Sediment, r.ByproductKind);
        }

        [Test]
        public void Failure_Mist_WhenDominantWater()
        {
            var r = _matcher.Evaluate(new ElementComposition(0, 5, 0, 0), null, 100, Day());
            Assert.AreEqual(BrewOutcome.FailureByproduct, r.Outcome);
            Assert.AreEqual(FailureByproductKind.Mist, r.ByproductKind);
            Assert.AreEqual("potion_mist", r.Byproduct.Id);
        }

        [Test]
        public void Failure_Mist_WhenDominantWind()
        {
            var r = _matcher.Evaluate(new ElementComposition(0, 0, 1, 6), null, 100, Day());
            Assert.AreEqual(FailureByproductKind.Mist, r.ByproductKind);
        }

        [Test]
        public void Failure_Murky_WhenTotalLE4()
        {
            // 총합 4 이하 → 최다 원소(불)가 있어도 탁한 포션
            var r = _matcher.Evaluate(new ElementComposition(4, 0, 0, 0), null, 100, Day());
            Assert.AreEqual(FailureByproductKind.Murky, r.ByproductKind);
            Assert.AreEqual("potion_murky", r.Byproduct.Id);
        }

        [Test]
        public void Failure_Murky_WhenDominantTie_AboveThreshold()
        {
            // 총합 10, 불=물 동률 → 탁한 포션
            var r = _matcher.Evaluate(new ElementComposition(5, 5, 0, 0), null, 100, Day());
            Assert.AreEqual(FailureByproductKind.Murky, r.ByproductKind);
        }

        [Test]
        public void Failure_Murky_WhenEmptyInput()
        {
            var r = _matcher.Evaluate(new ElementComposition(), null, 0, Day());
            Assert.AreEqual(FailureByproductKind.Murky, r.ByproductKind);
            Assert.AreEqual(0, r.ByproductSalePrice);
        }

        // ---------- 부산물 판매가 = min(표기가, 투입 가치 30%) ----------

        [Test]
        public void ByproductSalePrice_CappedByListPrice()
        {
            // 침전물 표기가 15, 투입 100 → 30% = 30 → min(15,30)=15
            var r = _matcher.Evaluate(new ElementComposition(5, 0, 0, 0), null, 100, Day());
            Assert.AreEqual(15, r.ByproductSalePrice);
        }

        [Test]
        public void ByproductSalePrice_CappedByInputValue()
        {
            // 침전물 표기가 15, 투입 30 → 30% = 9 → min(15,9)=9
            var r = _matcher.Evaluate(new ElementComposition(5, 0, 0, 0), null, 30, Day());
            Assert.AreEqual(9, r.ByproductSalePrice);
        }

        [Test]
        public void ByproductSalePrice_ZeroInput_IsZero()
        {
            var r = _matcher.Evaluate(new ElementComposition(0, 6, 0, 0), null, 0, Day());
            Assert.AreEqual(0, r.ByproductSalePrice);
        }

        // ---------- 재료 지정 (2단계) ----------

        [Test]
        public void RequiredIngredient_Missing_ReturnsHint()
        {
            // 용의 숨결 조성(🔥4💨2)만 맞고 용의 입김초 없음
            var r = _matcher.Evaluate(new ElementComposition(4, 0, 0, 2), null, 100, Day());
            Assert.AreEqual(BrewOutcome.MissingIngredient, r.Outcome);
            Assert.AreEqual("potion_dragon_breath", r.Recipe.PotionId);
            Assert.AreEqual(BrewMatcher.MissingIngredientHint, r.Hint);
            Assert.IsNotNull(r.MissingIngredients);
            Assert.AreEqual(1, r.MissingIngredients.Count);
            Assert.AreEqual(BrewFixture.DragonBreathHerb, r.MissingIngredients[0].ItemId);
            Assert.AreEqual(2, r.MissingIngredients[0].Count); // 부족 수량
        }

        [Test]
        public void RequiredIngredient_PartialCount_ReportsShortfall()
        {
            var counts = BrewFixture.Count(BrewFixture.DragonBreathHerb, 1); // 2개 필요, 1개만
            var r = _matcher.Evaluate(new ElementComposition(4, 0, 0, 2), counts, 100, Day());
            Assert.AreEqual(BrewOutcome.MissingIngredient, r.Outcome);
            Assert.AreEqual(1, r.MissingIngredients[0].Count); // 부족 1
        }

        [Test]
        public void RequiredIngredient_Satisfied_Succeeds()
        {
            var counts = BrewFixture.Count(BrewFixture.DragonBreathHerb, 2);
            var r = _matcher.Evaluate(new ElementComposition(4, 0, 0, 2), counts, 100, Day());
            Assert.AreEqual(BrewOutcome.Success, r.Outcome);
            Assert.AreEqual("potion_dragon_breath", r.Recipe.PotionId);
        }

        [Test]
        public void SagesElixir_WithoutStarlightPowder_MissingIngredient()
        {
            var r = _matcher.Evaluate(new ElementComposition(3, 3, 3, 3), null, 100, Day());
            Assert.AreEqual(BrewOutcome.MissingIngredient, r.Outcome);
            Assert.AreEqual("potion_sages_elixir", r.Recipe.PotionId);
        }

        // ---------- 조건 게이트 (3단계) ----------

        [Test]
        public void Moonlight_Daytime_ConditionNotMet()
        {
            var r = _matcher.Evaluate(new ElementComposition(0, 2, 0, 2), null, 100, Day());
            Assert.AreEqual(BrewOutcome.ConditionNotMet, r.Outcome);
            Assert.AreEqual("potion_moonlight", r.Recipe.PotionId);
            CollectionAssert.Contains(new List<string>(r.UnmetConditionTags), "night_only");
        }

        [Test]
        public void Moonlight_Night_Succeeds()
        {
            var r = _matcher.Evaluate(new ElementComposition(0, 2, 0, 2), null, 100, Night());
            Assert.AreEqual(BrewOutcome.Success, r.Outcome);
            Assert.AreEqual("potion_moonlight", r.Recipe.PotionId);
        }

        [Test]
        public void BlackSun_NonEclipse_ConditionNotMet()
        {
            var r = _matcher.Evaluate(new ElementComposition(2, 2, 2, 2), null, 100, Day());
            Assert.AreEqual(BrewOutcome.ConditionNotMet, r.Outcome);
            Assert.AreEqual("potion_black_sun", r.Recipe.PotionId);
        }

        [Test]
        public void BlackSun_Eclipse_Succeeds()
        {
            var r = _matcher.Evaluate(new ElementComposition(2, 2, 2, 2), null, 100, Eclipse());
            Assert.AreEqual(BrewOutcome.Success, r.Outcome);
            Assert.AreEqual("potion_black_sun", r.Recipe.PotionId);
        }

        [Test]
        public void HeartsBrew_NightWithCatalyst_Succeeds()
        {
            var counts = BrewFixture.Count(BrewFixture.RainbowCrystal, 1);
            var r = _matcher.Evaluate(new ElementComposition(1, 1, 1, 1), counts, 100, Night());
            Assert.AreEqual(BrewOutcome.Success, r.Outcome);
            Assert.AreEqual("potion_hearts_brew", r.Recipe.PotionId);
        }

        [Test]
        public void HeartsBrew_NightWithoutCatalyst_MissingIngredient()
        {
            // 야간이지만 촉매 없음 → 재료 지정 실패가 조건보다 먼저 걸림
            var r = _matcher.Evaluate(new ElementComposition(1, 1, 1, 1), null, 100, Night());
            Assert.AreEqual(BrewOutcome.MissingIngredient, r.Outcome);
        }

        // ---------- "같은 조성, 다른 조건" (약초 💧3🌍3 / 생명수 💧3🌍3 비) ----------

        [Test]
        public void SameComposition_Clear_YieldsHerb()
        {
            var r = _matcher.Evaluate(new ElementComposition(0, 3, 3, 0), null, 100, Day());
            Assert.AreEqual(BrewOutcome.Success, r.Outcome);
            Assert.AreEqual("potion_herb", r.Recipe.PotionId);
        }

        [Test]
        public void SameComposition_Rain_YieldsElixirOfLife()
        {
            var r = _matcher.Evaluate(new ElementComposition(0, 3, 3, 0), null, 100, Rain());
            Assert.AreEqual(BrewOutcome.Success, r.Outcome);
            Assert.AreEqual("potion_elixir_of_life", r.Recipe.PotionId,
                "비 오는 날엔 더 구체적인 조건(생명수)이 우선");
        }

        // ---------- 5번째 원소(별⭐) 슬롯 동작 ----------

        [Test]
        public void StarSlot_ParticipatesInMatching()
        {
            // 별⭐을 쓰는 합성 레시피가 조성 매칭에 걸리는지(예약 슬롯이 파이프라인에 배선됨)
            var starRecipe = new BrewRecipe("potion_star_test", "별 시험 포션",
                new ElementComposition(0, 0, 0, 0, 3), 999);
            var matcher = new BrewMatcher(new List<BrewRecipe> { starRecipe }, BrewFixture.Byproducts());

            var hit = matcher.Evaluate(new ElementComposition(0, 0, 0, 0, 3), null, 100, Day());
            Assert.AreEqual(BrewOutcome.Success, hit.Outcome);
            Assert.AreEqual("potion_star_test", hit.Recipe.PotionId);
        }

        [Test]
        public void StarSlot_ChangesComposition_BreaksMatch()
        {
            // 현자의 엘릭서(🔥3💧3🌍3💨3, star=0)에 별⭐ 1을 더하면 조성이 달라져 매칭 실패 → 부산물
            var r = _matcher.Evaluate(new ElementComposition(3, 3, 3, 3, 1), null, 100, Day());
            Assert.AreEqual(BrewOutcome.FailureByproduct, r.Outcome,
                "별 슬롯이 조성 동일성 판정에 포함되어야 함");
        }

        [Test]
        public void StarSlot_SumsThroughInputAggregation()
        {
            var starRecipe = new BrewRecipe("potion_star_test", "별 시험 포션",
                new ElementComposition(0, 0, 0, 0, 4), 999);
            var matcher = new BrewMatcher(new List<BrewRecipe> { starRecipe }, BrewFixture.Byproducts());

            var inputs = new List<BrewInputItem>
            {
                new BrewInputItem("mat_star", new ElementComposition(0, 0, 0, 0, 2), 10, 2), // ⭐2 × 2 = ⭐4
            };
            var r = matcher.Evaluate(inputs, Day());
            Assert.AreEqual(BrewOutcome.Success, r.Outcome);
            Assert.AreEqual("potion_star_test", r.Recipe.PotionId);
        }

        // ---------- 투입 묶음 집계(list 오버로드) ----------

        [Test]
        public void ListAggregation_SumsComposition()
        {
            var inputs = new List<BrewInputItem>
            {
                new BrewInputItem("mat_fire", new ElementComposition(1, 0, 0, 0), 1, 3), // 🔥1 × 3 = 🔥3
            };
            var r = _matcher.Evaluate(inputs, Day());
            Assert.AreEqual(BrewOutcome.Success, r.Outcome);
            Assert.AreEqual("potion_minor_flame", r.Recipe.PotionId);
        }

        [Test]
        public void ListAggregation_CountsIngredientsAndComposition()
        {
            // 용의 입김초 2개(각 🔥2💨1) = 조성 🔥4💨2 + 지정 재료 2개 충족 → 용의 숨결 성공
            var inputs = new List<BrewInputItem>
            {
                new BrewInputItem(BrewFixture.DragonBreathHerb, new ElementComposition(2, 0, 0, 1), 20, 2),
            };
            var r = _matcher.Evaluate(inputs, Day());
            Assert.AreEqual(BrewOutcome.Success, r.Outcome);
            Assert.AreEqual("potion_dragon_breath", r.Recipe.PotionId);
        }

        [Test]
        public void ListAggregation_SumsInputValueForByproduct()
        {
            // 🔥5 (매칭 없음) × 가치 → 침전물, 판매가 = min(15, 총가치×30%)
            var inputs = new List<BrewInputItem>
            {
                new BrewInputItem("mat_fire", new ElementComposition(5, 0, 0, 0), 20, 1), // 가치 20
            };
            var r = _matcher.Evaluate(inputs, Day());
            Assert.AreEqual(FailureByproductKind.Sediment, r.ByproductKind);
            Assert.AreEqual(6, r.ByproductSalePrice); // floor(20*0.3)=6 < 15
        }

        // ---------- 경계: 조성 우선(재료 있어도 조성 틀리면 부산물) ----------

        [Test]
        public void WrongComposition_EvenWithIngredient_IsByproduct()
        {
            // 용의 입김초는 있지만 조성이 🔥4(💨 없음) → 어느 레시피도 아님 → 부산물
            var counts = BrewFixture.Count(BrewFixture.DragonBreathHerb, 2);
            var r = _matcher.Evaluate(new ElementComposition(4, 0, 0, 0), counts, 100, Day());
            Assert.AreEqual(BrewOutcome.FailureByproduct, r.Outcome);
        }

        // ---------- 경계: total 4/5 임계 ----------

        [Test]
        public void Boundary_Total5_ClearDominant_IsSediment()
        {
            var r = _matcher.Evaluate(new ElementComposition(5, 0, 0, 0), null, 100, Day());
            Assert.AreEqual(FailureByproductKind.Sediment, r.ByproductKind);
        }

        [Test]
        public void Boundary_Total4_IsMurky()
        {
            var r = _matcher.Evaluate(new ElementComposition(0, 0, 4, 0), null, 100, Day());
            Assert.AreEqual(FailureByproductKind.Murky, r.ByproductKind);
        }
    }
}
