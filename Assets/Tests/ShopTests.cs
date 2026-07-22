using System.Collections.Generic;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>상점 진열·회수·손님 방문 판매 검증 (S04).</summary>
    public class ShopTests
    {
        const string Leaf = "material_dried_flame_leaf"; // 5G
        const string Grass = "plant_ember_grass";         // 1G

        static readonly Dictionary<string, int> Prices = new Dictionary<string, int>
        {
            { Leaf, 5 },
            { Grass, 1 },
            { "item_free", 0 }
        };

        static int PriceOf(string id) => Prices.TryGetValue(id, out int price) ? price : 0;

        Inventory _inventory;
        Wallet _wallet;
        Shop _shop;
        List<Shop.SaleRecord> _sales;

        [SetUp]
        public void SetUp()
        {
            _inventory = new Inventory();
            _wallet = new Wallet();
            _shop = new Shop();
            _sales = new List<Shop.SaleRecord>();
        }

        [Test]
        public void Display_MovesFromInventory_CapsAtInventoryCount()
        {
            _inventory.Add(Leaf, 3);

            Assert.AreEqual(3, _shop.Display(0, Leaf, 10, _inventory));
            Assert.AreEqual(0, _inventory.GetCount(Leaf));
            Assert.AreEqual(Leaf, _shop.Slots[0].ItemId);
            Assert.AreEqual(3, _shop.Slots[0].Count);
        }

        [Test]
        public void Display_SameItem_Accumulates()
        {
            _inventory.Add(Leaf, 6);

            _shop.Display(0, Leaf, 4, _inventory);
            _shop.Display(0, Leaf, 4, _inventory);

            Assert.AreEqual(6, _shop.Slots[0].Count);
            Assert.AreEqual(0, _inventory.GetCount(Leaf));
        }

        [Test]
        public void Display_DifferentItemInOccupiedSlot_Fails()
        {
            _inventory.Add(Leaf, 1);
            _inventory.Add(Grass, 1);
            _shop.Display(0, Leaf, 1, _inventory);

            Assert.AreEqual(0, _shop.Display(0, Grass, 1, _inventory));
            Assert.AreEqual(1, _inventory.GetCount(Grass));
        }

        [Test]
        public void Display_NothingInInventory_Fails()
        {
            Assert.AreEqual(0, _shop.Display(0, Leaf, 5, _inventory));
            Assert.IsTrue(_shop.Slots[0].IsEmpty);
        }

        [Test]
        public void TryTakeBack_ReturnsAllToInventory()
        {
            _inventory.Add(Leaf, 4);
            _shop.Display(1, Leaf, 4, _inventory);

            Assert.IsTrue(_shop.TryTakeBack(1, _inventory));
            Assert.AreEqual(4, _inventory.GetCount(Leaf));
            Assert.IsTrue(_shop.Slots[1].IsEmpty);
            Assert.IsFalse(_shop.TryTakeBack(1, _inventory)); // 빈 칸 회수 불가
        }

        [Test]
        public void TickCustomers_BeforeInterval_NoSale()
        {
            _inventory.Add(Leaf, 5);
            _shop.Display(0, Leaf, 5, _inventory);

            _shop.TickCustomers(Shop.CustomerIntervalSeconds - 0.01, PriceOf, _wallet, _sales);

            Assert.AreEqual(0, _sales.Count);
            Assert.AreEqual(0, _wallet.Gold);
            Assert.AreEqual(5, _shop.Slots[0].Count);
        }

        [Test]
        public void TickCustomers_OneVisit_BuysUpToFive_AndPays()
        {
            _inventory.Add(Leaf, 8);
            _shop.Display(0, Leaf, 8, _inventory);

            _shop.TickCustomers(Shop.CustomerIntervalSeconds, PriceOf, _wallet, _sales);

            Assert.AreEqual(1, _sales.Count);
            Assert.AreEqual(5, _sales[0].Count);          // 1회 최대 5개
            Assert.AreEqual(25, _sales[0].Gold);          // 5개 × 5G
            Assert.AreEqual(25, _wallet.Gold);
            Assert.AreEqual(3, _shop.Slots[0].Count);     // 남은 진열
        }

        [Test]
        public void TickCustomers_MultipleIntervals_MultipleVisits()
        {
            _inventory.Add(Leaf, 8);
            _shop.Display(0, Leaf, 8, _inventory);

            _shop.TickCustomers(Shop.CustomerIntervalSeconds * 2.0, PriceOf, _wallet, _sales);

            Assert.AreEqual(2, _sales.Count);
            Assert.AreEqual(5, _sales[0].Count);
            Assert.AreEqual(3, _sales[1].Count);          // 남은 3개 전부
            Assert.AreEqual(40, _wallet.Gold);            // 8개 × 5G
            Assert.IsTrue(_shop.Slots[0].IsEmpty);        // 다 팔리면 칸 비움
        }

        [Test]
        public void TickCustomers_EmptyDisplay_ConsumesTimerWithoutSale()
        {
            _shop.TickCustomers(Shop.CustomerIntervalSeconds * 3.0, PriceOf, _wallet, _sales);

            Assert.AreEqual(0, _sales.Count);
            Assert.AreEqual(Shop.CustomerIntervalSeconds * 3.0, _shop.LastCustomerAtResourceSeconds, 1e-9);
            // 이후 진열해도 밀린 손님이 몰려오지 않음
            _inventory.Add(Leaf, 5);
            _shop.Display(0, Leaf, 5, _inventory);
            _shop.TickCustomers(Shop.CustomerIntervalSeconds * 3.0 + 1.0, PriceOf, _wallet, _sales);
            Assert.AreEqual(0, _sales.Count);
        }

        [Test]
        public void TickCustomers_SkipsUnpricedItem_BuysNextSlot()
        {
            _inventory.Add("item_free", 5);
            _inventory.Add(Leaf, 5);
            _shop.Display(0, "item_free", 5, _inventory);
            _shop.Display(1, Leaf, 5, _inventory);

            _shop.TickCustomers(Shop.CustomerIntervalSeconds, PriceOf, _wallet, _sales);

            Assert.AreEqual(1, _sales.Count);
            Assert.AreEqual(Leaf, _sales[0].ItemId);
            Assert.AreEqual(5, _shop.Slots[0].Count); // 값 없는 품목은 그대로
        }

        [Test]
        public void SecondsUntilNextCustomer_CountsDown()
        {
            Assert.AreEqual(Shop.CustomerIntervalSeconds, _shop.SecondsUntilNextCustomer(0.0), 1e-9);
            Assert.AreEqual(4.0, _shop.SecondsUntilNextCustomer(6.0), 1e-9);
            Assert.AreEqual(0.0, _shop.SecondsUntilNextCustomer(99.0), 1e-9);
        }

        [Test]
        public void WriteToThenRestoreFrom_RoundTripsSlotsAndTimer()
        {
            _inventory.Add(Leaf, 7);
            _shop.Display(2, Leaf, 7, _inventory);
            _shop.TickCustomers(Shop.CustomerIntervalSeconds, PriceOf, _wallet, _sales);

            var data = SaveData.CreateNew();
            _shop.WriteTo(data);

            var restored = new Shop();
            restored.RestoreFrom(data);
            Assert.IsTrue(restored.Slots[0].IsEmpty);
            Assert.AreEqual(Leaf, restored.Slots[2].ItemId);
            Assert.AreEqual(2, restored.Slots[2].Count); // 7 - 5(판매)
            Assert.AreEqual(Shop.CustomerIntervalSeconds, restored.LastCustomerAtResourceSeconds, 1e-9);
        }
    }
}
