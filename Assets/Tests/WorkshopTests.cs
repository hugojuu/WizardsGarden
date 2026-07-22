using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>작업대 1차 가공 검증 (S04) — 시작(원료 소비)·진행도 파생·수령·세이브 라운드트립.</summary>
    public class WorkshopTests
    {
        const string Input = "plant_ember_grass";
        const string Output = "material_dried_flame_leaf";

        Inventory _inventory;
        Workshop _workshop;

        [SetUp]
        public void SetUp()
        {
            _inventory = new Inventory();
            _workshop = new Workshop();
        }

        [Test]
        public void TryStart_ConsumesInput_AndSetsState()
        {
            _inventory.Add(Input, 3);

            Assert.IsTrue(_workshop.TryStart(Output, 1, Input, 1, _inventory, nowResourceSeconds: 100.0));
            Assert.AreEqual(2, _inventory.GetCount(Input));
            Assert.IsFalse(_workshop.IsIdle);
            Assert.AreEqual(Output, _workshop.OutputItemId);
            Assert.AreEqual(1, _workshop.OutputCount);
            Assert.AreEqual(100.0, _workshop.StartedAtResourceSeconds, 1e-9);
        }

        [Test]
        public void TryStart_WhenBusy_Fails()
        {
            _inventory.Add(Input, 3);
            _workshop.TryStart(Output, 1, Input, 1, _inventory, 0.0);

            Assert.IsFalse(_workshop.TryStart(Output, 1, Input, 1, _inventory, 1.0));
            Assert.AreEqual(2, _inventory.GetCount(Input)); // 추가 소비 없음
        }

        [Test]
        public void TryStart_InsufficientInput_Fails()
        {
            _inventory.Add(Input, 1);

            Assert.IsFalse(_workshop.TryStart(Output, 1, Input, 2, _inventory, 0.0));
            Assert.IsTrue(_workshop.IsIdle);
            Assert.AreEqual(1, _inventory.GetCount(Input));
        }

        [Test]
        public void GetProgress_DerivedFromResourceTime()
        {
            _inventory.Add(Input, 1);
            _workshop.TryStart(Output, 1, Input, 1, _inventory, 100.0);

            Assert.AreEqual(0.0, _workshop.GetProgress(100.0, 8.0), 1e-9);
            Assert.AreEqual(0.5, _workshop.GetProgress(104.0, 8.0), 1e-9);
            Assert.AreEqual(1.0, _workshop.GetProgress(108.0, 8.0), 1e-9);
            Assert.AreEqual(1.0, _workshop.GetProgress(999.0, 8.0), 1e-9); // 캡
        }

        [Test]
        public void TryCollect_BeforeComplete_Fails()
        {
            _inventory.Add(Input, 1);
            _workshop.TryStart(Output, 1, Input, 1, _inventory, 0.0);

            Assert.IsFalse(_workshop.TryCollect(4.0, 8.0, _inventory, out _, out _));
            Assert.IsFalse(_workshop.IsIdle);
            Assert.AreEqual(0, _inventory.GetCount(Output));
        }

        [Test]
        public void TryCollect_Complete_AddsOutputAndIdles()
        {
            _inventory.Add(Input, 1);
            _workshop.TryStart(Output, 1, Input, 1, _inventory, 0.0);

            Assert.IsTrue(_workshop.TryCollect(8.0, 8.0, _inventory, out string itemId, out int count));
            Assert.AreEqual(Output, itemId);
            Assert.AreEqual(1, count);
            Assert.AreEqual(1, _inventory.GetCount(Output));
            Assert.IsTrue(_workshop.IsIdle);
        }

        [Test]
        public void WriteToThenRestoreFrom_RoundTripsActiveJob()
        {
            _inventory.Add(Input, 1);
            _workshop.TryStart(Output, 1, Input, 1, _inventory, 42.0);

            var data = SaveData.CreateNew();
            _workshop.WriteTo(data);

            var restored = new Workshop();
            restored.RestoreFrom(data);
            Assert.AreEqual(Output, restored.OutputItemId);
            Assert.AreEqual(1, restored.OutputCount);
            Assert.AreEqual(42.0, restored.StartedAtResourceSeconds, 1e-9);
        }

        [Test]
        public void RestoreFrom_EmptyOutputId_IsIdle()
        {
            var data = SaveData.CreateNew();
            data.workshopOutputId = "";
            data.workshopOutputCount = 3;

            var restored = new Workshop();
            restored.RestoreFrom(data);
            Assert.IsTrue(restored.IsIdle);
        }
    }
}
