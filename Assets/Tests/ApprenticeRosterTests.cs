using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>보유·배치 로스터 — 슬롯 제한(보유 &gt; 슬롯)·배치/해제·세이브 라운드트립 검증 (S09).</summary>
    public class ApprenticeRosterTests
    {
        static ApprenticeState G(string id) => new ApprenticeState(id, Job.Gardener, Rarity.Common, 5, 2, 2, 2);
        static ApprenticeState C(string id) => new ApprenticeState(id, Job.Clerk, Rarity.Common, 2, 2, 5, 2);

        [Test]
        public void AddIfMissing_IsIdempotent()
        {
            var roster = new ApprenticeRoster();
            roster.AddIfMissing(G("a"));
            roster.AddIfMissing(G("a"));   // 중복 무시
            Assert.AreEqual(1, roster.All.Count);
            Assert.IsTrue(roster.Contains("a"));
        }

        [Test]
        public void InitialSlots_AreOnePerJob()
        {
            var roster = new ApprenticeRoster();
            Assert.AreEqual(1, roster.SlotsFor(Job.Gardener));
            Assert.AreEqual(1, roster.SlotsFor(Job.Alchemist));
            Assert.AreEqual(1, roster.SlotsFor(Job.Clerk));
        }

        [Test]
        public void Place_RespectsSlotLimit_OwnedExceedsSlots()
        {
            var roster = new ApprenticeRoster();
            roster.AddIfMissing(G("g1"));
            roster.AddIfMissing(G("g2"));   // 보유 2 > 슬롯 1

            Assert.IsTrue(roster.TryPlace("g1"));
            Assert.IsFalse(roster.CanPlace(roster.Get("g2")));   // 슬롯 참
            Assert.IsFalse(roster.TryPlace("g2"));
            Assert.AreEqual(1, roster.PlacedCountFor(Job.Gardener));
        }

        [Test]
        public void Unplace_FreesSlot_ForAnother()
        {
            var roster = new ApprenticeRoster();
            roster.AddIfMissing(G("g1"));
            roster.AddIfMissing(G("g2"));
            roster.TryPlace("g1");

            Assert.IsTrue(roster.Unplace("g1"));
            Assert.IsTrue(roster.TryPlace("g2"));
            Assert.IsFalse(roster.Get("g1").IsPlaced);
            Assert.IsTrue(roster.Get("g2").IsPlaced);
        }

        [Test]
        public void DifferentJobs_HaveIndependentSlots()
        {
            var roster = new ApprenticeRoster();
            roster.AddIfMissing(G("g"));
            roster.AddIfMissing(C("c"));
            Assert.IsTrue(roster.TryPlace("g"));
            Assert.IsTrue(roster.TryPlace("c"));   // 다른 직군 슬롯
            Assert.AreEqual(2, roster.PlacedCount);
        }

        [Test]
        public void Placed_EnumeratesOnlyPlaced()
        {
            var roster = new ApprenticeRoster();
            roster.AddIfMissing(G("g1"));
            roster.AddIfMissing(C("c1"));
            roster.TryPlace("c1");

            int count = 0;
            foreach (ApprenticeState a in roster.Placed)
            {
                Assert.AreEqual("c1", a.Id);
                count++;
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void SaveRoundTrip_PreservesStateAndPlacement()
        {
            var roster = new ApprenticeRoster();
            ApprenticeState g = roster.AddIfMissing(G("g1"));
            roster.AddIfMissing(C("c1"));
            roster.TryPlace("g1");
            g.AddExperience(10);   // 레벨 2, 친화력 6

            var data = SaveData.CreateNew();
            roster.WriteTo(data);

            var restored = new ApprenticeRoster();
            restored.RestoreFrom(data);

            Assert.AreEqual(2, restored.All.Count);
            ApprenticeState rg = restored.Get("g1");
            Assert.IsNotNull(rg);
            Assert.AreEqual(2, rg.Level);
            Assert.AreEqual(6, rg.Affinity);
            Assert.IsTrue(rg.IsPlaced);
            Assert.IsFalse(restored.Get("c1").IsPlaced);
        }

        [Test]
        public void Restore_DefendsAgainstOverPlacement()
        {
            // 이상치: 같은 직군 2명이 배치됨으로 저장 → 슬롯 1이라 하나만 배치 유지
            var data = SaveData.CreateNew();
            data.apprentices.Add(Entry("g1", Job.Gardener, placed: true));
            data.apprentices.Add(Entry("g2", Job.Gardener, placed: true));

            var roster = new ApprenticeRoster();
            roster.RestoreFrom(data);
            Assert.AreEqual(1, roster.PlacedCountFor(Job.Gardener));
        }

        static SaveData.ApprenticeEntry Entry(string id, Job job, bool placed) => new SaveData.ApprenticeEntry
        {
            id = id, job = (int)job, rarity = 0, affinity = 5, magic = 2, trade = 2, luck = 2,
            level = 1, experience = 0, placed = placed
        };
    }
}
