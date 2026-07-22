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

        /// <summary>Begin에서 세이브를 복원했는가 (false = 새 게임).</summary>
        public bool LoadedFromSave { get; private set; }

        /// <summary>마지막 저장~지금 raw 경과 초 (정산 전 값 — S08이 소비 후 ClearPendingOfflineSeconds 호출).</summary>
        public double PendingOfflineSeconds { get; private set; }

        public GameSession(SaveRepository repository, IUtcClock utcClock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _utcClock = utcClock ?? throw new ArgumentNullException(nameof(utcClock));
            Clock = new GameClock();
        }

        /// <summary>세이브가 있으면 복원 + 오프라인 경과 계산, 없으면 새 게임 상태.</summary>
        public void Begin()
        {
            if (_repository.TryLoad(out SaveData data))
            {
                Clock.RestoreFrom(data);
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
            data.lastSavedUtcTicks = _utcClock.UtcNow.Ticks;
            _repository.Save(data);
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
