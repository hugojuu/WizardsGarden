namespace WizardGarden.Core
{
    /// <summary>
    /// 작업대 1개 (순수 C#, S04 — 1차 가공만). 시작 시 원료를 인벤토리에서 소비하고
    /// 출력 id·수량·시작 시점(자원초)만 보관 — 진행도는 현재 자원 시간에서 파생 계산
    /// (S03 GardenSlot과 동일 방침). processingSeconds는 데이터(SO) 소관이라 호출 시 전달받는다.
    /// </summary>
    public sealed class Workshop
    {
        /// <summary>가공 중인 출력 아이템 id (비어 있으면 null).</summary>
        public string OutputItemId { get; private set; }

        /// <summary>완료 시 산출 수량.</summary>
        public int OutputCount { get; private set; }

        /// <summary>가공 시작 시점의 자원 시간 누적 초.</summary>
        public double StartedAtResourceSeconds { get; private set; }

        public bool IsIdle => string.IsNullOrEmpty(OutputItemId);

        /// <summary>가공 시작 — 원료를 인벤토리에서 소비. 작업 중·원료 부족이면 false.</summary>
        public bool TryStart(string outputItemId, int outputCount, string inputItemId, int inputCount,
            Inventory inventory, double nowResourceSeconds)
        {
            if (!IsIdle || string.IsNullOrEmpty(outputItemId) || outputCount < 1 || inventory == null)
                return false;
            if (!inventory.TryRemove(inputItemId, inputCount))
                return false;

            OutputItemId = outputItemId;
            OutputCount = outputCount;
            StartedAtResourceSeconds = nowResourceSeconds;
            return true;
        }

        /// <summary>가공 진행도 0~1 (대기 중 0, processingSeconds 0 이하면 즉시 1).</summary>
        public double GetProgress(double nowResourceSeconds, double processingSeconds)
        {
            if (IsIdle)
                return 0.0;
            if (processingSeconds <= 0.0)
                return 1.0;

            double progress = (nowResourceSeconds - StartedAtResourceSeconds) / processingSeconds;
            if (progress < 0.0)
                return 0.0;
            return progress > 1.0 ? 1.0 : progress;
        }

        public bool IsComplete(double nowResourceSeconds, double processingSeconds)
        {
            return !IsIdle && GetProgress(nowResourceSeconds, processingSeconds) >= 1.0;
        }

        /// <summary>완료 상태면 산출물을 인벤토리에 적재하고 작업대를 비운다.</summary>
        public bool TryCollect(double nowResourceSeconds, double processingSeconds, Inventory inventory,
            out string collectedItemId, out int collectedCount)
        {
            collectedItemId = null;
            collectedCount = 0;
            if (inventory == null || !IsComplete(nowResourceSeconds, processingSeconds))
                return false;

            collectedItemId = OutputItemId;
            collectedCount = OutputCount;
            inventory.Add(collectedItemId, collectedCount);
            Clear();
            return true;
        }

        void Clear()
        {
            OutputItemId = null;
            OutputCount = 0;
            StartedAtResourceSeconds = 0.0;
        }

        /// <summary>세이브 데이터에서 복원 (빈 출력 id는 대기 상태).</summary>
        public void RestoreFrom(SaveData data)
        {
            if (string.IsNullOrEmpty(data.workshopOutputId))
            {
                Clear();
                return;
            }
            OutputItemId = data.workshopOutputId;
            OutputCount = data.workshopOutputCount > 0 ? data.workshopOutputCount : 1;
            StartedAtResourceSeconds = data.workshopStartedAtResourceSeconds;
        }

        /// <summary>세이브 데이터에 기록 (대기 중이면 출력 id = "").</summary>
        public void WriteTo(SaveData data)
        {
            data.workshopOutputId = IsIdle ? "" : OutputItemId;
            data.workshopOutputCount = OutputCount;
            data.workshopStartedAtResourceSeconds = StartedAtResourceSeconds;
        }
    }
}
