using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>골드 지갑 검증 (S04).</summary>
    public class WalletTests
    {
        [Test]
        public void Add_IncreasesGold_AndIgnoresNonPositive()
        {
            var wallet = new Wallet();

            wallet.Add(30);
            wallet.Add(0);
            wallet.Add(-10);

            Assert.AreEqual(30, wallet.Gold);
        }

        [Test]
        public void TrySpend_Success_ReducesGold()
        {
            var wallet = new Wallet();
            wallet.Add(50);

            Assert.IsTrue(wallet.TrySpend(20));
            Assert.AreEqual(30, wallet.Gold);
        }

        [Test]
        public void TrySpend_Insufficient_FailsWithoutChange()
        {
            var wallet = new Wallet();
            wallet.Add(10);

            Assert.IsFalse(wallet.TrySpend(11));
            Assert.IsFalse(wallet.TrySpend(-1));
            Assert.AreEqual(10, wallet.Gold);
        }

        [Test]
        public void TrySpend_Zero_AlwaysSucceedsWithoutEvent()
        {
            var wallet = new Wallet();
            int changedCount = 0;
            wallet.Changed += () => changedCount++;

            Assert.IsTrue(wallet.TrySpend(0));
            Assert.AreEqual(0, wallet.Gold);
            Assert.AreEqual(0, changedCount);
        }

        [Test]
        public void AddAndSpend_RaiseChangedEvent()
        {
            var wallet = new Wallet();
            int changedCount = 0;
            wallet.Changed += () => changedCount++;

            wallet.Add(10);
            wallet.TrySpend(5);
            wallet.TrySpend(100); // 실패 — 이벤트 없음

            Assert.AreEqual(2, changedCount);
        }

        [Test]
        public void WriteToThenRestoreFrom_RoundTripsGold()
        {
            var wallet = new Wallet();
            wallet.Add(1234567890123L); // long 범위 확인

            var data = SaveData.CreateNew();
            wallet.WriteTo(data);

            var restored = new Wallet();
            restored.RestoreFrom(data);
            Assert.AreEqual(1234567890123L, restored.Gold);
        }

        [Test]
        public void RestoreFrom_NegativeGold_ClampsToZero()
        {
            var data = SaveData.CreateNew();
            data.gold = -50;

            var wallet = new Wallet();
            wallet.RestoreFrom(data);
            Assert.AreEqual(0, wallet.Gold);
        }
    }
}
