using System;

namespace WizardGarden.Core
{
    /// <summary>
    /// 골드 지갑 (순수 C#, S04). 경제 곡선이 억 단위까지 가므로(기획서 8장) long 사용.
    /// </summary>
    public sealed class Wallet
    {
        /// <summary>보유 골드.</summary>
        public long Gold { get; private set; }

        /// <summary>잔액 변경 알림 (UI 갱신용).</summary>
        public event Action Changed;

        /// <summary>골드 추가 (0 이하 무시).</summary>
        public void Add(long amount)
        {
            if (amount <= 0)
                return;
            Gold += amount;
            Changed?.Invoke();
        }

        public bool CanAfford(long cost) => cost >= 0 && Gold >= cost;

        /// <summary>지불 시도 — 잔액 부족·음수 비용이면 false. 비용 0은 항상 성공(변경 없음).</summary>
        public bool TrySpend(long cost)
        {
            if (cost < 0 || Gold < cost)
                return false;
            if (cost == 0)
                return true;

            Gold -= cost;
            Changed?.Invoke();
            return true;
        }

        /// <summary>세이브 데이터에서 복원 (음수는 0으로 클램프).</summary>
        public void RestoreFrom(SaveData data)
        {
            Gold = data.gold > 0 ? data.gold : 0;
            Changed?.Invoke();
        }

        /// <summary>세이브 데이터에 기록.</summary>
        public void WriteTo(SaveData data)
        {
            data.gold = Gold;
        }
    }
}
