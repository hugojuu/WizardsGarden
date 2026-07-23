using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 상점 (순수 C#, S04) — 진열대 3칸 + 손님 방문 타이머 (자원 시간 기반).
    /// 손님은 일정 주기로 방문해 앞 진열 칸부터 가격이 매겨진 아이템을 최대 MaxItemsPerVisit개 구매.
    /// 가격 조회는 데이터(SO) 소관이라 델리게이트로 전달받는다.
    /// </summary>
    public sealed class Shop
    {
        /// <summary>진열대 칸 수 (S04 고정 — 진열대 확장은 이후 경영 세션).</summary>
        public const int DisplaySlotCount = 3;

        /// <summary>손님 방문 주기 (자원 시간 초).</summary>
        public const double CustomerIntervalSeconds = 10.0;

        /// <summary>손님 1명이 한 번에 사 가는 최대 수량.</summary>
        public const int MaxItemsPerVisit = 5;

        public sealed class DisplaySlot
        {
            public string ItemId { get; internal set; }
            public int Count { get; internal set; }
            public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
        }

        /// <summary>판매 1건 기록 (UI 로그용).</summary>
        public readonly struct SaleRecord
        {
            public readonly string ItemId;
            public readonly int Count;
            public readonly long Gold;

            public SaleRecord(string itemId, int count, long gold)
            {
                ItemId = itemId;
                Count = count;
                Gold = gold;
            }
        }

        readonly DisplaySlot[] _slots;

        /// <summary>마지막 손님 방문 시점 (자원초). 새 게임은 0 — 첫 손님은 주기 경과 후.</summary>
        public double LastCustomerAtResourceSeconds { get; private set; }

        public IReadOnlyList<DisplaySlot> Slots => _slots;

        public Shop()
        {
            _slots = new DisplaySlot[DisplaySlotCount];
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = new DisplaySlot();
        }

        public bool IsValidIndex(int slotIndex) => slotIndex >= 0 && slotIndex < _slots.Length;

        /// <summary>
        /// 인벤토리 → 진열 이동. 같은 아이템이면 합산, 다른 아이템이 차 있으면 실패.
        /// 실제 이동 수량(min(요청, 보유))을 반환 (0 = 실패/이동 없음).
        /// </summary>
        public int Display(int slotIndex, string itemId, int requestedCount, Inventory inventory)
        {
            if (!IsValidIndex(slotIndex) || inventory == null || string.IsNullOrEmpty(itemId) || requestedCount <= 0)
                return 0;

            DisplaySlot slot = _slots[slotIndex];
            if (!slot.IsEmpty && slot.ItemId != itemId)
                return 0;

            int moved = Math.Min(requestedCount, inventory.GetCount(itemId));
            if (moved <= 0 || !inventory.TryRemove(itemId, moved))
                return 0;

            slot.ItemId = itemId;
            slot.Count += moved;
            return moved;
        }

        /// <summary>진열 회수 — 칸 전체를 인벤토리로 되돌린다.</summary>
        public bool TryTakeBack(int slotIndex, Inventory inventory)
        {
            if (!IsValidIndex(slotIndex) || inventory == null)
                return false;

            DisplaySlot slot = _slots[slotIndex];
            if (slot.IsEmpty)
                return false;

            inventory.Add(slot.ItemId, slot.Count);
            slot.ItemId = null;
            slot.Count = 0;
            return true;
        }

        /// <summary>다음 손님까지 남은 자원초 (이미 지났으면 0).</summary>
        public double SecondsUntilNextCustomer(double nowResourceSeconds)
        {
            double next = LastCustomerAtResourceSeconds + CustomerIntervalSeconds;
            return next > nowResourceSeconds ? next - nowResourceSeconds : 0.0;
        }

        /// <summary>
        /// 경과한 주기만큼 손님 방문 처리 (매 프레임 호출). 판매 대금은 지갑으로,
        /// 판매 기록은 salesOut에 추가 (빈 진열·가격 없음이면 손님은 그냥 돌아간다 — 주기는 소모).
        /// saleGoldModifier: 판매 골드에 곱연산 보너스 등을 적용하는 훅(S06 도감 완성도 보너스). null이면 원가.
        /// </summary>
        public void TickCustomers(double nowResourceSeconds, Func<string, int> priceOf, Wallet wallet,
            List<SaleRecord> salesOut = null, Func<long, long> saleGoldModifier = null)
        {
            if (priceOf == null || wallet == null)
                return;
            if (LastCustomerAtResourceSeconds > nowResourceSeconds)
                LastCustomerAtResourceSeconds = nowResourceSeconds; // 세이브 이상치 방어

            while (nowResourceSeconds - LastCustomerAtResourceSeconds >= CustomerIntervalSeconds)
            {
                LastCustomerAtResourceSeconds += CustomerIntervalSeconds;
                ServeCustomer(priceOf, wallet, salesOut, saleGoldModifier);
            }
        }

        void ServeCustomer(Func<string, int> priceOf, Wallet wallet, List<SaleRecord> salesOut,
            Func<long, long> saleGoldModifier)
        {
            foreach (DisplaySlot slot in _slots)
            {
                if (slot.IsEmpty)
                    continue;

                string itemId = slot.ItemId;
                int unitPrice = priceOf(itemId);
                if (unitPrice <= 0)
                    continue; // 가격을 매길 수 없는 품목은 건너뜀

                int count = Math.Min(slot.Count, MaxItemsPerVisit);
                long gold = (long)unitPrice * count;
                if (saleGoldModifier != null)
                {
                    long modified = saleGoldModifier(gold);
                    if (modified >= 0)
                        gold = modified;
                }

                slot.Count -= count;
                if (slot.Count <= 0)
                {
                    slot.ItemId = null;
                    slot.Count = 0;
                }

                wallet.Add(gold);
                salesOut?.Add(new SaleRecord(itemId, count, gold));
                return; // 손님 1명 = 1회 구매
            }
        }

        /// <summary>세이브 데이터에서 복원.</summary>
        public void RestoreFrom(SaveData data)
        {
            List<SaveData.DisplaySlotEntry> saved = data.shopDisplaySlots;
            for (int i = 0; i < _slots.Length; i++)
            {
                SaveData.DisplaySlotEntry entry = saved != null && i < saved.Count ? saved[i] : null;
                bool valid = entry != null && !string.IsNullOrEmpty(entry.itemId) && entry.count > 0;
                _slots[i].ItemId = valid ? entry.itemId : null;
                _slots[i].Count = valid ? entry.count : 0;
            }
            LastCustomerAtResourceSeconds = data.shopLastCustomerAtResourceSeconds;
        }

        /// <summary>세이브 데이터에 기록 (빈 칸은 itemId = "").</summary>
        public void WriteTo(SaveData data)
        {
            data.shopDisplaySlots = new List<SaveData.DisplaySlotEntry>(_slots.Length);
            foreach (DisplaySlot slot in _slots)
            {
                data.shopDisplaySlots.Add(new SaveData.DisplaySlotEntry
                {
                    itemId = slot.IsEmpty ? "" : slot.ItemId,
                    count = slot.IsEmpty ? 0 : slot.Count
                });
            }
            data.shopLastCustomerAtResourceSeconds = LastCustomerAtResourceSeconds;
        }
    }
}
