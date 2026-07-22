using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// JSON 세이브 스키마 (S02, v2는 S03). 버전 필드로 마이그레이션 대비.
    /// 필드 기본값 version = 0: 버전 필드가 없는(손상된) JSON을 구버전으로 식별하기 위함 —
    /// 새 세이브는 반드시 CreateNew()로 생성할 것.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>현재 스키마 버전. 스키마 변경 시 +1 하고 SaveMigrator에 단계 추가.</summary>
        public const int CurrentVersion = 2;

        /// <summary>밭 슬롯 저장 항목 (v2/S03). 빈 슬롯은 plantId = "".</summary>
        [Serializable]
        public class GardenSlotEntry
        {
            public string plantId = "";
            public double plantedAtResourceSeconds;
        }

        /// <summary>인벤토리 저장 항목 (v2/S03) — id → 수량.</summary>
        [Serializable]
        public class InventoryEntry
        {
            public string itemId = "";
            public int count;
        }

        public int version;

        /// <summary>사건 시간 누적 초.</summary>
        public double eventSeconds;

        /// <summary>자원 시간 누적 초.</summary>
        public double resourceSeconds;

        /// <summary>마지막 저장 UTC 시각 (DateTime.Ticks) — 오프라인 경과 계산 기준.</summary>
        public long lastSavedUtcTicks;

        /// <summary>밭 슬롯 상태 (v2/S03).</summary>
        public List<GardenSlotEntry> gardenSlots = new List<GardenSlotEntry>();

        /// <summary>수확물 인벤토리 (v2/S03).</summary>
        public List<InventoryEntry> inventoryItems = new List<InventoryEntry>();

        /// <summary>현재 버전으로 초기화된 새 세이브 생성.</summary>
        public static SaveData CreateNew()
        {
            return new SaveData { version = CurrentVersion };
        }
    }

    /// <summary>구버전 세이브 → 현재 버전 마이그레이션 체인.</summary>
    public static class SaveMigrator
    {
        /// <summary>단계별 마이그레이션 시도. 미래 버전·알 수 없는 버전이면 false (로드 거부).</summary>
        public static bool TryMigrate(SaveData data)
        {
            if (data == null || data.version > SaveData.CurrentVersion)
                return false;

            while (data.version < SaveData.CurrentVersion)
            {
                switch (data.version)
                {
                    case 1:
                        // v1 → v2 (S03): 밭 슬롯·인벤토리 필드 추가 — 구세이브는 빈 밭·빈 인벤토리
                        data.gardenSlots ??= new List<SaveData.GardenSlotEntry>();
                        data.inventoryItems ??= new List<SaveData.InventoryEntry>();
                        data.version = 2;
                        break;
                    default:
                        return false;
                }
            }
            return data.version == SaveData.CurrentVersion;
        }
    }
}
