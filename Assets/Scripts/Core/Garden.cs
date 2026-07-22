using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 밭 슬롯 모음 (순수 C#). S03 초기 4칸 — 슬롯 확장 구매는 S04.
    /// 세이브 복원 시 슬롯 수는 max(초기 4, 저장된 수)로 재구성해 이후 확장과 호환.
    /// </summary>
    public sealed class Garden
    {
        /// <summary>초기 슬롯 수 (기획서 2장 — 슬롯 확장은 S04 경영 몫).</summary>
        public const int InitialSlotCount = 4;

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
