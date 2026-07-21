using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>ElementComposition 연산 검증 (S01 스키마 토대).</summary>
    public class ElementCompositionTests
    {
        [Test]
        public void Add_SumsEachSlot()
        {
            var a = new ElementComposition(1, 2, 0, 0);
            var b = new ElementComposition(2, 0, 3, 1, 1);

            var sum = a + b;

            Assert.AreEqual(new ElementComposition(3, 2, 3, 1, 1), sum);
            Assert.AreEqual(10, sum.Total);
        }

        [Test]
        public void Matches_TrueOnlyWhenAllSlotsEqual()
        {
            var flame = new ElementComposition(3, 0, 0, 0);

            Assert.IsTrue(flame.Matches(new ElementComposition(3, 0, 0, 0)));
            Assert.IsFalse(flame.Matches(new ElementComposition(3, 1, 0, 0)));
            // 별⭐ 예약 슬롯도 비교에 참여
            Assert.IsFalse(flame.Matches(new ElementComposition(3, 0, 0, 0, 1)));
        }

        [Test]
        public void Contains_TrueWhenEverySlotIsAtLeastOther()
        {
            var stock = new ElementComposition(4, 2, 1, 0);

            Assert.IsTrue(stock.Contains(new ElementComposition(3, 2, 0, 0)));
            Assert.IsTrue(stock.Contains(stock));
            Assert.IsFalse(stock.Contains(new ElementComposition(0, 0, 0, 1)));
        }

        [Test]
        public void TryGetDominantElement_ReturnsSingleMax()
        {
            var comp = new ElementComposition(1, 4, 2, 0);

            Assert.IsTrue(comp.TryGetDominantElement(out var dominant));
            Assert.AreEqual(Element.Water, dominant);
        }

        [Test]
        public void TryGetDominantElement_FalseOnTieOrEmpty()
        {
            // 최다 원소 동률 → 실패 부산물 "탁한 포션" 분기 (기획서 6장)
            var tie = new ElementComposition(2, 2, 0, 0);
            Assert.IsFalse(tie.TryGetDominantElement(out _));

            var empty = new ElementComposition();
            Assert.IsFalse(empty.TryGetDominantElement(out _));
            Assert.IsTrue(empty.IsEmpty);
        }

        [Test]
        public void Indexer_ReadsAndWritesAllFiveSlots()
        {
            var comp = new ElementComposition();
            for (int i = 0; i < ElementComposition.SlotCount; i++)
            {
                comp[(Element)i] = i + 1;
            }

            Assert.AreEqual(1, comp.fire);
            Assert.AreEqual(2, comp.water);
            Assert.AreEqual(3, comp.earth);
            Assert.AreEqual(4, comp.wind);
            Assert.AreEqual(5, comp.star);
        }
    }
}
