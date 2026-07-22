using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 밭 슬롯 모음 (순수 C#). 초기 4칸 + 골드 확장(S04, 비용은 EconomyFormulas).
    /// 세이브 복원 시 슬롯 수는 max(초기 4, 저장된 수)로 재구성 — 슬롯 수 = gardenSlots 목록 길이.
    /// </summary>
    public sealed class Garden
    {
        /// <summary>초기 슬롯 수 (기획서 2장).</summary>
        public const int InitialSlotCount = 4;

        /// <summary>S04 확장 상한 (플레이스홀더 UI 3×4 그리드 한계 — 이후 세션에서 상향 가능).</summary>
        public const int MaxSlotCount = 12;

        readonly List<GardenSlot> _slots = new List<GardenSlot>();

        public IReadOnlyList<GardenSlot> Slots => _slots;

        public int SlotCount => _slots.Count;

        public Garden(int slotCount = InitialSlotCount)
        {
            if (slotCount < 1)
                throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount, "슬롯 수는 1 이상");
            for (int i = 0; i < slotCount; i++)
                _slots.Add(new GardenSlot());
        }

        public bool IsValidIndex(int slotIndex) => slotIndex >= 0 && slotIndex < _slots.Count;

        /// <summary>슬롯 1칸 추가 (상한 도달 시 false). 지불 판정은 GameSession.TryBuyGardenSlot 몫.</summary>
        public bool TryAddSlot()
        {
            if (_slots.Count >= MaxSlotCount)
                return false;
            _slots.Add(new GardenSlot());
            return true;
        }

        public bool TryPlant(int slotIndex, string plantId, double nowResourceSeconds)
        {
            return IsValidIndex(slotIndex) && _slots[slotIndex].TryPlant(plantId, nowResourceSeconds);
        }

        public bool TryHarvest(int slotIndex, double nowResourceSeconds, double growthSeconds, out string harvestedPlantId)
        {
            harvestedPlantId = null;
            return IsValidIndex(slotIndex)
                && _slots[slotIndex].TryHarvest(nowResourceSeconds, growthSeconds, out harvestedPlantId);
        }

        /// <summary>세이브 데이터에서 슬롯 상태 복원.</summary>
        public void RestoreFrom(SaveData data)
        {
            List<SaveData.GardenSlotEntry> saved = data.gardenSlots;
            int count = Math.Max(InitialSlotCount, saved?.Count ?? 0);

            _slots.Clear();
            for (int i = 0; i < count; i++)
            {
                var slot = new GardenSlot();
                if (saved != null && i < saved.Count && saved[i] != null)
                    slot.Restore(saved[i].plantId, saved[i].plantedAtResourceSeconds);
                _slots.Add(slot);
            }
        }

        /// <summary>세이브 데이터에 슬롯 상태 기록 (빈 슬롯은 plantId = "").</summary>
        public void WriteTo(SaveData data)
        {
            data.gardenSlots = new List<SaveData.GardenSlotEntry>(_slots.Count);
            foreach (GardenSlot slot in _slots)
            {
                data.gardenSlots.Add(new SaveData.GardenSlotEntry
                {
                    plantId = slot.IsEmpty ? "" : slot.PlantId,
                    plantedAtResourceSeconds = slot.PlantedAtResourceSeconds
                });
            }
        }
    }
}
