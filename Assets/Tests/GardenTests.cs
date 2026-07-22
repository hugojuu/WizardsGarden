using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>밭 슬롯 심기·성장 진행도·수확 판정 검증 (S03).</summary>
    public class GardenTests
    {
        const string PlantId = "plant_ember_grass";
        const double Growth = 10.0;

        [Test]
        public void Garden_Default_HasFourEmptySlots()
        {
            var garden = new Garden();

            Assert.AreEqual(Garden.InitialSlotCount, garden.SlotCount);
            Assert.AreEqual(4, garden.SlotCount);
            foreach (GardenSlot slot in garden.Slots)
                Assert.IsTrue(slot.IsEmpty);
        }

        [Test]
        public void TryPlant_EmptySlot_Succeeds()
        {
            var garden = new Garden();

            Assert.IsTrue(garden.TryPlant(0, PlantId, 100.0));
            Assert.IsFalse(garden.Slots[0].IsEmpty);
            Assert.AreEqual(PlantId, garden.Slots[0].PlantId);
            Assert.AreEqual(100.0, garden.Slots[0].PlantedAtResourceSeconds);
        }

        [Test]
        public void TryPlant_OccupiedSlot_Fails()
        {
            var garden = new Garden();
            garden.TryPlant(0, PlantId, 100.0);

            Assert.IsFalse(garden.TryPlant(0, "plant_dew_moss", 101.0));
            Assert.AreEqual(PlantId, garden.Slots[0].PlantId);
        }

        [Test]
        public void TryPlant_InvalidIndexOrEmptyId_Fails()
        {
            var garden = new Garden();

            Assert.IsFalse(garden.TryPlant(-1, PlantId, 0.0));
            Assert.IsFalse(garden.TryPlant(4, PlantId, 0.0));
            Assert.IsFalse(garden.TryPlant(0, "", 0.0));
            Assert.IsFalse(garden.TryPlant(0, null, 0.0));
        }

        [Test]
        public void GetProgress_AdvancesWithResourceTime_AndClamps()
        {
            var slot = new GardenSlot();
            slot.TryPlant(PlantId, 100.0);

            Assert.AreEqual(0.0, slot.GetProgress(100.0, Growth), 1e-9);
            Assert.AreEqual(0.5, slot.GetProgress(105.0, Growth), 1e-9);
            Assert.AreEqual(1.0, slot.GetProgress(110.0, Growth), 1e-9);
            Assert.AreEqual(1.0, slot.GetProgress(999.0, Growth), 1e-9); // 상한 클램프
            Assert.AreEqual(0.0, slot.GetProgress(50.0, Growth), 1e-9);  // 역행 클램프
        }

        [Test]
        public void GetStage_MapsProgressToSproutGrowingMature()
        {
            var slot = new GardenSlot();
            slot.TryPlant(PlantId, 0.0);

            Assert.AreEqual(GrowthStage.Sprout, slot.GetStage(4.9, Growth));
            Assert.AreEqual(GrowthStage.Growing, slot.GetStage(5.0, Growth));
            Assert.AreEqual(GrowthStage.Growing, slot.GetStage(9.9, Growth));
            Assert.AreEqual(GrowthStage.Mature, slot.GetStage(10.0, Growth));
        }

        [Test]
        public void GrowthSecondsZero_IsImmediatelyMature()
        {
            var slot = new GardenSlot();
            slot.TryPlant(PlantId, 100.0);

            Assert.IsTrue(slot.IsMature(100.0, 0.0));
        }

        [Test]
        public void TryHarvest_BeforeMature_FailsAndKeepsPlant()
        {
            var garden = new Garden();
            garden.TryPlant(0, PlantId, 100.0);

            Assert.IsFalse(garden.TryHarvest(0, 105.0, Growth, out string harvested));
            Assert.IsNull(harvested);
            Assert.IsFalse(garden.Slots[0].IsEmpty);
        }

        [Test]
        public void TryHarvest_WhenMature_ReturnsPlantIdAndClearsSlot()
        {
            var garden = new Garden();
            garden.TryPlant(0, PlantId, 100.0);

            Assert.IsTrue(garden.TryHarvest(0, 110.0, Growth, out string harvested));
            Assert.AreEqual(PlantId, harvested);
            Assert.IsTrue(garden.Slots[0].IsEmpty);
            // 수확 후 재파종 가능
            Assert.IsTrue(garden.TryPlant(0, PlantId, 110.0));
        }

        [Test]
        public void TryHarvest_EmptyOrInvalidSlot_Fails()
        {
            var garden = new Garden();

            Assert.IsFalse(garden.TryHarvest(0, 100.0, Growth, out _));
            Assert.IsFalse(garden.TryHarvest(99, 100.0, Growth, out _));
        }

        [Test]
        public void WriteToThenRestoreFrom_RoundTripsSlotStates()
        {
            var garden = new Garden();
            garden.TryPlant(1, PlantId, 42.5);
            garden.TryPlant(3, "plant_dew_moss", 77.0);

            var data = SaveData.CreateNew();
            garden.WriteTo(data);

            var restored = new Garden();
            restored.RestoreFrom(data);

            Assert.AreEqual(garden.SlotCount, restored.SlotCount);
            Assert.IsTrue(restored.Slots[0].IsEmpty);
            Assert.AreEqual(PlantId, restored.Slots[1].PlantId);
            Assert.AreEqual(42.5, restored.Slots[1].PlantedAtResourceSeconds);
            Assert.IsTrue(restored.Slots[2].IsEmpty);
            Assert.AreEqual("plant_dew_moss", restored.Slots[3].PlantId);
            Assert.AreEqual(77.0, restored.Slots[3].PlantedAtResourceSeconds);
        }

        [Test]
        public void RestoreFrom_EmptyOrMissingList_RebuildsInitialSlots()
        {
            var data = SaveData.CreateNew();
            data.gardenSlots = null;

            var garden = new Garden();
            garden.TryPlant(0, PlantId, 1.0);
            garden.RestoreFrom(data);

            Assert.AreEqual(Garden.InitialSlotCount, garden.SlotCount);
            foreach (GardenSlot slot in garden.Slots)
                Assert.IsTrue(slot.IsEmpty);
        }
    }
}
