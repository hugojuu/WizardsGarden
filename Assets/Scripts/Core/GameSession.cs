using System;

namespace WizardGarden.Core
{
    /// <summary>
    /// 세이브 ↔ 시계 오케스트레이션 (순수 C#). 시작 시 로드·오프라인 경과 계산, 저장 시 종료 시각 기록.
    /// 오프라인 경과는 raw 초만 제공 — 캡(8h)·효율(60%) 정산은 S08 몫.
    /// </summary>
    public sealed class GameSession
    {
        readonly SaveRepository _repository;
        readonly IUtcClock _utcClock;

        public GameClock Clock { get; }

        /// <summary>밭 슬롯 상태 (S03).</summary>
        public Garden Garden { get; }

        /// <summary>수확물 인벤토리 (S03).</summary>
        public Inventory Inventory { get; }

        /// <summary>골드 지갑 (S04).</summary>
        public Wallet Wallet { get; }

        /// <summary>공방 작업대 (S04).</summary>
        public Workshop Workshop { get; }

        /// <summary>상점 진열대·손님 (S04).</summary>
        public Shop Shop { get; }

        /// <summary>골드 해금 상태 (S04).</summary>
        public UnlockState Unlocks { get; }

        /// <summary>Begin에서 세이브를 복원했는가 (false = 새 게임).</summary>
        public bool LoadedFromSave { get; private set; }

        /// <summary>마지막 저장~지금 raw 경과 초 (정산 전 값 — S08이 소비 후 ClearPendingOfflineSeconds 호출).</summary>
        public double PendingOfflineSeconds { get; private set; }

        public GameSession(SaveRepository repository, IUtcClock utcClock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _utcClock = utcClock ?? throw new ArgumentNullException(nameof(utcClock));
            Clock = new GameClock();
            Garden = new Garden();
            Inventory = new Inventory();
            Wallet = new Wallet();
            Workshop = new Workshop();
            Shop = new Shop();
            Unlocks = new UnlockState();
        }

        /// <summary>세이브가 있으면 복원 + 오프라인 경과 계산, 없으면 새 게임 상태.</summary>
        public void Begin()
        {
            if (_repository.TryLoad(out SaveData data))
            {
                Clock.RestoreFrom(data);
                Garden.RestoreFrom(data);
                Inventory.RestoreFrom(data);
                Wallet.RestoreFrom(data);
                Workshop.RestoreFrom(data);
                Shop.RestoreFrom(data);
                Unlocks.RestoreFrom(data);
                PendingOfflineSeconds = ComputeOfflineSeconds(data.lastSavedUtcTicks, _utcClock.UtcNow);
                LoadedFromSave = true;
            }
            else
            {
                PendingOfflineSeconds = 0.0;
                LoadedFromSave = false;
            }
        }

        /// <summary>현재 상태 저장 (마지막 저장 UTC 시각 포함).</summary>
        public void SaveNow()
        {
            SaveData data = SaveData.CreateNew();
            Clock.WriteTo(data);
            Garden.WriteTo(data);
            Inventory.WriteTo(data);
            Wallet.WriteTo(data);
            Workshop.WriteTo(data);
            Shop.WriteTo(data);
            Unlocks.WriteTo(data);
            data.lastSavedUtcTicks = _utcClock.UtcNow.Ticks;
            _repository.Save(data);
        }

        /// <summary>현재 자원 시간 기준으로 슬롯에 심기.</summary>
        public bool TryPlant(int slotIndex, string plantId)
        {
            return Garden.TryPlant(slotIndex, plantId, Clock.ResourceSeconds);
        }

        /// <summary>현재 자원 시간 기준으로 수확 판정 — 성공 시 인벤토리에 적재.</summary>
        public bool TryHarvestToInventory(int slotIndex, double growthSeconds)
        {
            if (!Garden.TryHarvest(slotIndex, Clock.ResourceSeconds, growthSeconds, out string harvestedPlantId))
                return false;

            Inventory.Add(harvestedPlantId);
            return true;
        }

        /// <summary>다음 밭 슬롯 구매 비용 — Cost(n) = Base × 1.15^n, n = 이미 구매한 슬롯 수 (기획서 8장).</summary>
        public int NextGardenSlotCost => EconomyFormulas.GardenSlotCost(Garden.SlotCount - Garden.InitialSlotCount);

        /// <summary>골드를 지불하고 밭 슬롯 1칸 확장 (상한·잔액 판정 포함).</summary>
        public bool TryBuyGardenSlot()
        {
            if (Garden.SlotCount >= Garden.MaxSlotCount)
                return false;
            if (!Wallet.TrySpend(NextGardenSlotCost))
                return false;
            return Garden.TryAddSlot();
        }

        /// <summary>골드를 지불하고 아이템(종자 등) 해금 — 이미 해금됐으면 false (중복 지불 방지).</summary>
        public bool TryPurchaseUnlock(string itemId, int cost)
        {
            return Unlocks.TryPurchaseUnlock(itemId, cost, Wallet);
        }

        /// <summary>S08 오프라인 정산 완료 후 호출.</summary>
        public void ClearPendingOfflineSeconds()
        {
            PendingOfflineSeconds = 0.0;
        }

        /// <summary>마지막 저장 시각 기준 raw 오프라인 경과 초. 시계 역행(음수)은 0으로 클램프.</summary>
        public static double ComputeOfflineSeconds(long lastSavedUtcTicks, DateTime utcNow)
        {
            if (lastSavedUtcTicks <= 0)
                return 0.0;

            double seconds = (utcNow - new DateTime(lastSavedUtcTicks, DateTimeKind.Utc)).TotalSeconds;
            return seconds > 0.0 ? seconds : 0.0;
        }
    }
}
