using System.Collections.Generic;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>
    /// 자유 투입 조합 오케스트레이터 검증 (S06) — S05 BrewMatcher 판정 + Codex 발견 + 인벤토리 소비/산출.
    /// 매칭은 BrewFixture(기획서 33종)를 그대로 사용한다.
    /// </summary>
    public class BrewStationTests
    {
        const string FireLeaf = "leaf_fire";   // 🔥1, 5G
        const string WaterLeaf = "leaf_water"; // 💧1
        const string EarthLeaf = "leaf_earth"; // 🌍1
        const string WindLeaf = "leaf_wind";   // 💨1

        static ElementComposition C(int f, int w, int e, int wi, int s = 0) => new ElementComposition(f, w, e, wi, s);

        BrewStation _station;
        Codex _codex;
        Inventory _inventory;

        [SetUp]
        public void SetUp()
        {
            _codex = new Codex();
            // 발견 시연에 필요한 우주 등록 (완성도 계산용 — 여기선 발견 여부만 검증)
            foreach (BrewRecipe recipe in BrewFixture.Recipes())
                _codex.RegisterPotion(recipe.PotionId);
            _codex.RegisterByproduct(BrewRecipeFactory.MurkyId);
            _codex.RegisterByproduct(BrewRecipeFactory.SedimentId);
            _codex.RegisterByproduct(BrewRecipeFactory.MistId);

            _station = new BrewStation(BrewFixture.Matcher(), _codex);
            _inventory = new Inventory();
        }

        static readonly Dictionary<string, ElementComposition> Unit = new Dictionary<string, ElementComposition>
        {
            { FireLeaf, C(1, 0, 0, 0) },
            { WaterLeaf, C(0, 1, 0, 0) },
            { EarthLeaf, C(0, 0, 1, 0) },
            { WindLeaf, C(0, 0, 0, 1) }
        };

        List<BrewInputItem> Inputs(params (string id, int count)[] picks)
        {
            var list = new List<BrewInputItem>();
            foreach (var pick in picks)
                list.Add(new BrewInputItem(pick.id, Unit[pick.id], 5, pick.count));
            return list;
        }

        static IBrewContext DayClear => new BrewContext(TimeOfDay.Day, Weather.Clear, Season.Spring);

        [Test]
        public void Success_NewPotion_DiscoversConsumesAndAwardsStarlight()
        {
            _inventory.Add(FireLeaf, 3);

            BrewAttemptResult result = _station.Attempt(Inputs((FireLeaf, 3)), DayClear, _inventory);

            Assert.AreEqual(BrewApplyStatus.Discovered, result.Status);
            Assert.AreEqual("potion_minor_flame", result.ProducedItemId);
            Assert.AreEqual(1, result.ProducedCount);
            Assert.IsTrue(result.NewlyDiscovered);
            Assert.AreEqual(1, result.StarlightAwarded);
            Assert.AreEqual(0, _inventory.GetCount(FireLeaf));            // 재료 3개 소비
            Assert.AreEqual(1, _inventory.GetCount("potion_minor_flame")); // 포션 1개 산출
            Assert.IsTrue(_codex.IsDiscovered("potion_minor_flame"));
        }

        [Test]
        public void Success_AlreadyKnown_NoStarlight()
        {
            _inventory.Add(FireLeaf, 6);
            _station.Attempt(Inputs((FireLeaf, 3)), DayClear, _inventory); // 첫 발견

            BrewAttemptResult second = _station.Attempt(Inputs((FireLeaf, 3)), DayClear, _inventory);

            Assert.AreEqual(BrewApplyStatus.AlreadyKnown, second.Status);
            Assert.IsFalse(second.NewlyDiscovered);
            Assert.AreEqual(0, second.StarlightAwarded);
            Assert.AreEqual(2, _inventory.GetCount("potion_minor_flame")); // 누적 2개
        }

        [Test]
        public void TwoElementPotion_Discovers()
        {
            _inventory.Add(FireLeaf, 3);
            _inventory.Add(WaterLeaf, 3);

            BrewAttemptResult result = _station.Attempt(Inputs((FireLeaf, 3), (WaterLeaf, 3)), DayClear, _inventory);

            Assert.AreEqual(BrewApplyStatus.Discovered, result.Status);
            Assert.AreEqual("potion_steam", result.ProducedItemId); // 🔥3💧3 = 증기 포션
        }

        [Test]
        public void InsufficientInventory_InvalidInput_NoConsumption()
        {
            _inventory.Add(FireLeaf, 2); // 3 필요한데 2개뿐

            BrewAttemptResult result = _station.Attempt(Inputs((FireLeaf, 3)), DayClear, _inventory);

            Assert.AreEqual(BrewApplyStatus.InvalidInput, result.Status);
            Assert.IsFalse(result.ConsumedInputs);
            Assert.AreEqual(2, _inventory.GetCount(FireLeaf)); // 그대로
        }

        [Test]
        public void EmptyInput_InvalidInput()
        {
            BrewAttemptResult result = _station.Attempt(new List<BrewInputItem>(), DayClear, _inventory);
            Assert.AreEqual(BrewApplyStatus.InvalidInput, result.Status);
        }

        [Test]
        public void Failure_LowTotal_YieldsMurky_AndLogsJournal()
        {
            _inventory.Add(FireLeaf, 1);

            BrewAttemptResult result = _station.Attempt(Inputs((FireLeaf, 1)), DayClear, _inventory);

            Assert.AreEqual(BrewApplyStatus.Byproduct, result.Status);
            Assert.AreEqual(BrewRecipeFactory.MurkyId, result.ProducedItemId); // 총합 1 ≤ 4 → 탁한 포션
            Assert.IsTrue(result.NewlyDiscovered);
            Assert.AreEqual(0, result.StarlightAwarded); // 부산물은 별빛 없음
            Assert.AreEqual(0, _inventory.GetCount(FireLeaf));
            Assert.AreEqual(1, _inventory.GetCount(BrewRecipeFactory.MurkyId));
            Assert.IsTrue(_codex.IsDiscovered(BrewRecipeFactory.MurkyId));
        }

        [Test]
        public void Failure_FireDominant_YieldsSediment()
        {
            _inventory.Add(FireLeaf, 5); // 🔥5, 총합 5 > 4, 최다 원소 🔥 → 침전물

            BrewAttemptResult result = _station.Attempt(Inputs((FireLeaf, 5)), DayClear, _inventory);

            Assert.AreEqual(BrewApplyStatus.Byproduct, result.Status);
            Assert.AreEqual(BrewRecipeFactory.SedimentId, result.ProducedItemId);
        }

        [Test]
        public void MissingIngredient_DoesNotConsumeOrDiscover()
        {
            // 용의 숨결 포션(🔥4💨2)은 용의 입김초 ×2 지정 재료 필요 — 조성만 맞고 재료 없음
            _inventory.Add(FireLeaf, 4);
            _inventory.Add(WindLeaf, 2);

            BrewAttemptResult result = _station.Attempt(Inputs((FireLeaf, 4), (WindLeaf, 2)), DayClear, _inventory);

            Assert.AreEqual(BrewApplyStatus.MissingIngredient, result.Status);
            Assert.IsFalse(result.ConsumedInputs);
            Assert.AreEqual(4, _inventory.GetCount(FireLeaf)); // 재료 반환(미소비)
            Assert.AreEqual(2, _inventory.GetCount(WindLeaf));
            Assert.AreEqual(0, _codex.DiscoveredCount);
            Assert.IsFalse(string.IsNullOrEmpty(result.Result.Hint));
        }

        [Test]
        public void ConditionNotMet_DoesNotConsumeOrDiscover()
        {
            // 달빛 포션(💧2💨2)은 야간 한정 — 낮이면 조건 불충족
            _inventory.Add(WaterLeaf, 2);
            _inventory.Add(WindLeaf, 2);

            BrewAttemptResult result = _station.Attempt(Inputs((WaterLeaf, 2), (WindLeaf, 2)), DayClear, _inventory);

            Assert.AreEqual(BrewApplyStatus.ConditionNotMet, result.Status);
            Assert.IsFalse(result.ConsumedInputs);
            Assert.AreEqual(2, _inventory.GetCount(WaterLeaf));
            Assert.AreEqual(2, _inventory.GetCount(WindLeaf));
            Assert.AreEqual(0, _codex.DiscoveredCount);
        }

        [Test]
        public void ConditionMet_AtNight_Discovers()
        {
            _inventory.Add(WaterLeaf, 2);
            _inventory.Add(WindLeaf, 2);
            var night = new BrewContext(TimeOfDay.Night, Weather.Clear, Season.Spring);

            BrewAttemptResult result = _station.Attempt(Inputs((WaterLeaf, 2), (WindLeaf, 2)), night, _inventory);

            Assert.AreEqual(BrewApplyStatus.Discovered, result.Status);
            Assert.AreEqual("potion_moonlight", result.ProducedItemId); // 야간이면 달빛 포션 완성
        }
    }
}
