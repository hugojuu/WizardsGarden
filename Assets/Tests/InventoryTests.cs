using System.Collections.Generic;
using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>수확물 인벤토리 기초 (id → 수량) 검증 (S03).</summary>
    public class InventoryTests
    {
        [Test]
        public void Add_AccumulatesCountPerId()
        {
            var inventory = new Inventory();

            inventory.Add("plant_ember_grass");
            inventory.Add("plant_ember_grass", 2);
            inventory.Add("plant_dew_moss");

            Assert.AreEqual(3, inventory.GetCount("plant_ember_grass"));
            Assert.AreEqual(1, inventory.GetCount("plant_dew_moss"));
            Assert.AreEqual(0, inventory.GetCount("plant_wild_grass"));
            Assert.AreEqual(2, inventory.UniqueItemCount);
        }

        [Test]
        public void Add_InvalidIdOrCount_IsIgnored()
        {
            var inventory = new Inventory();
            int changedCount = 0;
            inventory.Changed += () => changedCount++;

            inventory.Add(null);
            inventory.Add("");
            inventory.Add("plant_ember_grass", 0);
            inventory.Add("plant_ember_grass", -5);

            Assert.AreEqual(0, inventory.UniqueItemCount);
            Assert.AreEqual(0, changedCount);
        }

        [Test]
        public void Add_RaisesChangedEvent()
        {
            var inventory = new Inventory();
            int changedCount = 0;
            inventory.Changed += () => changedCount++;

            inventory.Add("plant_ember_grass");
            inventory.Add("plant_ember_grass");

            Assert.AreEqual(2, changedCount);
        }

        [Test]
        public void Entries_AreSortedById()
        {
            var inventory = new Inventory();
            inventory.Add("plant_wild_grass");
            inventory.Add("plant_dew_moss");
            inventory.Add("plant_ember_grass");

            var ids = new List<string>();
            foreach (KeyValuePair<string, int> entry in inventory.Entries)
                ids.Add(entry.Key);

            CollectionAssert.AreEqual(
                new[] { "plant_dew_moss", "plant_ember_grass", "plant_wild_grass" }, ids);
        }

        [Test]
        public void WriteToThenRestoreFrom_RoundTripsCounts()
        {
            var inventory = new Inventory();
            inventory.Add("plant_ember_grass", 3);
            inventory.Add("plant_dandelion_puff", 7);

            var data = SaveData.CreateNew();
            inventory.WriteTo(data);

            var restored = new Inventory();
            restored.Add("plant_wild_grass"); // 복원 시 기존 내용은 대체되어야 함
            restored.RestoreFrom(data);

            Assert.AreEqual(2, restored.UniqueItemCount);
            Assert.AreEqual(3, restored.GetCount("plant_ember_grass"));
            Assert.AreEqual(7, restored.GetCount("plant_dandelion_puff"));
            Assert.AreEqual(0, restored.GetCount("plant_wild_grass"));
        }

        [Test]
        public void RestoreFrom_SkipsInvalidEntries_AndMergesDuplicates()
        {
            var data = SaveData.CreateNew();
            data.inventoryItems = new List<SaveData.InventoryEntry>
            {
                new SaveData.InventoryEntry { itemId = "plant_ember_grass", count = 2 },
                new SaveData.InventoryEntry { itemId = "", count = 5 },
                new SaveData.InventoryEntry { itemId = "plant_dew_moss", count = 0 },
                new SaveData.InventoryEntry { itemId = "plant_ember_grass", count = 3 },
                null
            };

            var inventory = new Inventory();
            inventory.RestoreFrom(data);

            Assert.AreEqual(1, inventory.UniqueItemCount);
            Assert.AreEqual(5, inventory.GetCount("plant_ember_grass"));
        }
    }
}
