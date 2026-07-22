using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 수확물 인벤토리 기초 (순수 C#) — id → 수량. 제거·판매는 S04.
    /// SortedDictionary로 나열·세이브 순서를 id 기준 결정적으로 유지.
    /// </summary>
    public sealed class Inventory
    {
        readonly SortedDictionary<string, int> _counts = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>수량 변경 알림 (UI 갱신용).</summary>
        public event Action Changed;

        /// <summary>보유 품목 종류 수.</summary>
        public int UniqueItemCount => _counts.Count;

        /// <summary>id 오름차순 (id, 수량) 나열.</summary>
        public IEnumerable<KeyValuePair<string, int>> Entries => _counts;

        public int GetCount(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _counts.TryGetValue(itemId, out int count) ? count : 0;
        }

        /// <summary>수량 추가 (잘못된 id·0 이하 수량은 무시).</summary>
        public void Add(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
                return;

            _counts[itemId] = GetCount(itemId) + count;
            Changed?.Invoke();
        }

        /// <summary>세이브 데이터에서 복원 (중복 id는 합산).</summary>
        public void RestoreFrom(SaveData data)
        {
            _counts.Clear();
            if (data.inventoryItems != null)
            {
                foreach (SaveData.InventoryEntry entry in data.inventoryItems)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.itemId) && entry.count > 0)
                        _counts[entry.itemId] = GetCount(entry.itemId) + entry.count;
                }
            }
            Changed?.Invoke();
        }

        /// <summary>세이브 데이터에 기록.</summary>
        public void WriteTo(SaveData data)
        {
            data.inventoryItems = new List<SaveData.InventoryEntry>(_counts.Count);
            foreach (KeyValuePair<string, int> pair in _counts)
                data.inventoryItems.Add(new SaveData.InventoryEntry { itemId = pair.Key, count = pair.Value });
        }
    }
}
