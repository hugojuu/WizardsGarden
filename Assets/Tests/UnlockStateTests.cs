using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>골드 해금 상태 검증 (S04).</summary>
    public class UnlockStateTests
    {
        const string Seed = "plant_flame_poppy";

        [Test]
        public void TryPurchaseUnlock_SpendsGold_AndUnlocks()
        {
            var wallet = new Wallet();
            wallet.Add(150);
            var unlocks = new UnlockState();

            Assert.IsTrue(unlocks.TryPurchaseUnlock(Seed, 100, wallet));
            Assert.IsTrue(unlocks.IsUnlocked(Seed));
            Assert.AreEqual(50, wallet.Gold);
        }

        [Test]
        public void TryPurchaseUnlock_AlreadyUnlocked_FailsWithoutCharge()
        {
            var wallet = new Wallet();
            wallet.Add(300);
            var unlocks = new UnlockState();
            unlocks.TryPurchaseUnlock(Seed, 100, wallet);

            Assert.IsFalse(unlocks.TryPurchaseUnlock(Seed, 100, wallet));
            Assert.AreEqual(200, wallet.Gold); // 중복 지불 없음
        }

        [Test]
        public void TryPurchaseUnlock_InsufficientGold_Fails()
        {
            var wallet = new Wallet();
            wallet.Add(99);
            var unlocks = new UnlockState();

            Assert.IsFalse(unlocks.TryPurchaseUnlock(Seed, 100, wallet));
            Assert.IsFalse(unlocks.IsUnlocked(Seed));
            Assert.AreEqual(99, wallet.Gold);
        }

        [Test]
        public void IsUnlocked_UnknownOrEmptyId_IsFalse()
        {
            var unlocks = new UnlockState();
            Assert.IsFalse(unlocks.IsUnlocked("plant_unknown"));
            Assert.IsFalse(unlocks.IsUnlocked(null));
            Assert.IsFalse(unlocks.IsUnlocked(""));
        }

        [Test]
        public void WriteToThenRestoreFrom_RoundTripsUnlockedIds()
        {
            var wallet = new Wallet();
            wallet.Add(500);
            var unlocks = new UnlockState();
            unlocks.TryPurchaseUnlock("plant_flame_poppy", 100, wallet);
            unlocks.TryPurchaseUnlock("plant_blue_lily", 100, wallet);

            var data = SaveData.CreateNew();
            unlocks.WriteTo(data);
            CollectionAssert.AreEqual(new[] { "plant_blue_lily", "plant_flame_poppy" }, data.unlockedIds); // id 오름차순

            var restored = new UnlockState();
            restored.RestoreFrom(data);
            Assert.IsTrue(restored.IsUnlocked("plant_flame_poppy"));
            Assert.IsTrue(restored.IsUnlocked("plant_blue_lily"));
            Assert.IsFalse(restored.IsUnlocked(Seed + "_x"));
        }
    }
}
