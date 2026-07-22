using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 골드 해금 상태 (순수 C#, S04) — 해금된 아이템 id 집합. 해금은 영구.
    /// SortedSet으로 세이브 순서 결정적 유지.
    /// </summary>
    public sealed class UnlockState
    {
        readonly SortedSet<string> _unlockedIds = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>해금 상태 변경 알림 (UI 갱신용).</summary>
        public event Action Changed;

        public IEnumerable<string> UnlockedIds => _unlockedIds;

        public bool IsUnlocked(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _unlockedIds.Contains(itemId);
        }

        /// <summary>골드를 지불하고 해금. 이미 해금됨·잔액 부족이면 false (중복 지불 방지).</summary>
        public bool TryPurchaseUnlock(string itemId, long cost, Wallet wallet)
        {
            if (string.IsNullOrEmpty(itemId) || wallet == null || _unlockedIds.Contains(itemId))
                return false;
            if (!wallet.TrySpend(cost))
                return false;

            _unlockedIds.Add(itemId);
            Changed?.Invoke();
            return true;
        }

        /// <summary>세이브 데이터에서 복원.</summary>
        public void RestoreFrom(SaveData data)
        {
            _unlockedIds.Clear();
            if (data.unlockedIds != null)
            {
                foreach (string id in data.unlockedIds)
                {
                    if (!string.IsNullOrEmpty(id))
                        _unlockedIds.Add(id);
                }
            }
            Changed?.Invoke();
        }

        /// <summary>세이브 데이터에 기록.</summary>
        public void WriteTo(SaveData data)
        {
            data.unlockedIds = new List<string>(_unlockedIds);
        }
    }
}
