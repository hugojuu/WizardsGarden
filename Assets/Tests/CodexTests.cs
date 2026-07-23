using System.Collections.Generic;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>도감 발견 상태·완성도 집계·골드 보너스 배선 검증 (S06).</summary>
    public class CodexTests
    {
        Codex _codex;

        [SetUp]
        public void SetUp()
        {
            _codex = new Codex();
        }

        void RegisterSample()
        {
            // 포션 8종 + 부산물 2종 = 총 10 (완성율 계산이 깔끔하게 떨어지는 표본)
            for (int i = 0; i < 8; i++)
                _codex.RegisterPotion($"potion_{i}");
            _codex.RegisterByproduct("potion_murky");
            _codex.RegisterByproduct("potion_sediment");
        }

        [Test]
        public void Registration_CountsTotals()
        {
            RegisterSample();
            Assert.AreEqual(8, _codex.PotionTotal);
            Assert.AreEqual(2, _codex.ByproductTotal);
            Assert.AreEqual(10, _codex.TotalEntries);
            Assert.AreEqual(0, _codex.DiscoveredCount);
            Assert.AreEqual(0.0, _codex.CompletionRatio, 1e-9);
        }

        [Test]
        public void Registration_IsIdempotent()
        {
            _codex.RegisterPotion("potion_0");
            _codex.RegisterPotion("potion_0");
            Assert.AreEqual(1, _codex.PotionTotal);
        }

        [Test]
        public void Discover_NewReturnsTrue_DuplicateFalse()
        {
            RegisterSample();
            Assert.IsTrue(_codex.Discover("potion_0"));
            Assert.IsFalse(_codex.Discover("potion_0"));
            Assert.IsTrue(_codex.IsDiscovered("potion_0"));
            Assert.AreEqual(1, _codex.DiscoveredCount);
        }

        [Test]
        public void Discover_SeparatesPotionAndByproductCounts()
        {
            RegisterSample();
            _codex.Discover("potion_0");
            _codex.Discover("potion_1");
            _codex.Discover("potion_murky");

            Assert.AreEqual(2, _codex.PotionDiscoveredCount);
            Assert.AreEqual(1, _codex.ByproductDiscoveredCount);
            Assert.AreEqual(3, _codex.DiscoveredCount);
            Assert.AreEqual(0.30, _codex.CompletionRatio, 1e-9); // 3/10
        }

        [Test]
        public void CompletionRatio_MapsToBonusThresholds()
        {
            RegisterSample(); // 10 entries
            // 25% = 2.5 → 3개 발견해야 25% 초과. 정확히 25%를 만들기 위해 2개 + 부산물 로직 대신 비율 직접 확인.
            _codex.Discover("potion_0");
            _codex.Discover("potion_1");
            Assert.AreEqual(0.20, _codex.CompletionRatio, 1e-9);
            Assert.AreEqual(0.0, _codex.GoldBonusFraction, 1e-9); // 20% < 25%

            _codex.Discover("potion_2"); // 30%
            Assert.AreEqual(0.05, _codex.GoldBonusFraction, 1e-9);
        }

        [Test]
        public void UnregisteredDiscoveredId_DoesNotCountTowardCompletion()
        {
            RegisterSample();
            _codex.Discover("potion_from_future_content"); // 등록 우주 밖 (S07이 나중에 추가할 id)
            Assert.AreEqual(0, _codex.DiscoveredCount);
            Assert.AreEqual(0.0, _codex.CompletionRatio, 1e-9);
        }

        [Test]
        public void ApplyGoldBonus_UsesCompletionRatio()
        {
            RegisterSample();
            _codex.Discover("potion_0");
            _codex.Discover("potion_1");
            _codex.Discover("potion_2"); // 30% → +5%
            Assert.AreEqual(420, _codex.ApplyGoldBonus(400)); // 400 × 1.05
        }

        [Test]
        public void SaveRoundTrip_PreservesDiscoveredSet()
        {
            RegisterSample();
            _codex.Discover("potion_3");
            _codex.Discover("potion_murky");

            var data = SaveData.CreateNew();
            _codex.WriteTo(data);
            Assert.AreEqual(2, data.discoveredCodexIds.Count);

            var restored = new Codex();
            // 로드 순서: 발견 집합 먼저 복원 → 이후 우주 등록 (어댑터 흐름 재현)
            restored.RestoreFrom(data);
            for (int i = 0; i < 8; i++)
                restored.RegisterPotion($"potion_{i}");
            restored.RegisterByproduct("potion_murky");
            restored.RegisterByproduct("potion_sediment");

            Assert.IsTrue(restored.IsDiscovered("potion_3"));
            Assert.IsTrue(restored.IsDiscovered("potion_murky"));
            Assert.AreEqual(2, restored.DiscoveredCount);
            Assert.AreEqual(0.20, restored.CompletionRatio, 1e-9);
        }

        [Test]
        public void WriteTo_IsDeterministicOrder()
        {
            _codex.RegisterPotion("potion_b");
            _codex.RegisterPotion("potion_a");
            _codex.Discover("potion_b");
            _codex.Discover("potion_a");

            var data = SaveData.CreateNew();
            _codex.WriteTo(data);
            Assert.AreEqual(new List<string> { "potion_a", "potion_b" }, data.discoveredCodexIds);
        }

        // 완성도 골드 보너스가 실제 Shop 판매 흐름에 배선되는지 (saleGoldModifier 훅).
        [Test]
        public void Shop_AppliesCodexBonusToSaleGold()
        {
            RegisterSample();
            _codex.Discover("potion_0");
            _codex.Discover("potion_1");
            _codex.Discover("potion_2"); // 30% → +5%

            var inventory = new Inventory();
            inventory.Add("potion_0", 5);
            var wallet = new Wallet();
            var shop = new Shop();
            shop.Display(0, "potion_0", 5, inventory);

            int PriceOf(string id) => id == "potion_0" ? 400 : 0;
            shop.TickCustomers(Shop.CustomerIntervalSeconds, PriceOf, wallet, null, _codex.ApplyGoldBonus);

            // 5개 × 400 = 2000 → +5% = 2100
            Assert.AreEqual(2100, wallet.Gold);
        }
    }
}
