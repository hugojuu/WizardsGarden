using System;

namespace WizardGarden.Core
{
    /// <summary>
    /// 이중 시간 시계 (기획서 22장). 순수 C# — MonoBehaviour가 Tick을 위임한다.
    /// - 자원 시간(ResourceSeconds): 실시간 기반 누적. 오프라인 경과는 S08 정산이 AddResourceSeconds로 얹는다.
    /// - 사건 시간(EventSeconds): 앱 실행 중에만 tick. 게임 내 일일 사이클(1일 = 현실 15분)·시각 구간을 구동.
    /// </summary>
    public sealed class GameClock
    {
        /// <summary>게임 내 1일 = 현실 15분.</summary>
        public const double RealSecondsPerGameDay = 900.0;

        /// <summary>현실 1초당 게임 내 시간(시간 단위) 진행량.</summary>
        public const double GameHoursPerRealSecond = 24.0 / RealSecondsPerGameDay;

        /// <summary>새 게임 시작 시각 — 1일차 아침 06:00 (아침 구간 시작).</summary>
        public const double StartHourOfDay = 6.0;

        /// <summary>사건 시간 누적 초 (배속 반영, 앱 실행 중에만 증가).</summary>
        public double EventSeconds { get; private set; }

        /// <summary>자원 시간 누적 초 (배속 반영. 오프라인 정산분은 S08이 AddResourceSeconds로 추가).</summary>
        public double ResourceSeconds { get; private set; }

        double _timeScale = 1.0;

        /// <summary>시간 배속 (에디터 치트용, 1 = 정상. 0 이하 불가).</summary>
        public double TimeScale
        {
            get => _timeScale;
            set => _timeScale = value > 0.0 ? value : 1.0;
        }

        /// <summary>게임 시작 이후 총 게임 내 시간(시간 단위) — 시작 오프셋 06:00 포함.</summary>
        public double TotalGameHours => StartHourOfDay + EventSeconds * GameHoursPerRealSecond;

        /// <summary>현재 일차 (1일차부터).</summary>
        public int DayIndex => 1 + (int)(TotalGameHours / 24.0);

        /// <summary>게임 내 현재 시각 (0~24).</summary>
        public double HourOfDay => TotalGameHours % 24.0;

        /// <summary>현재 시각 구간.</summary>
        public TimeOfDay CurrentTimeOfDay => TimeOfDayUtility.FromHour(HourOfDay);

        /// <summary>시각 구간 전환 이벤트 (이전, 현재). 큰 배속으로 구간을 건너뛰면 중간 구간 이벤트는 생략된다.</summary>
        public event Action<TimeOfDay, TimeOfDay> TimeOfDayChanged;

        /// <summary>일차 변경 이벤트 (새 일차).</summary>
        public event Action<int> DayChanged;

        /// <summary>현실 경과 초를 배속 적용해 두 트랙에 반영. 매 프레임 호출.</summary>
        public void Tick(double realDeltaSeconds)
        {
            if (realDeltaSeconds <= 0.0)
                return;
            Advance(realDeltaSeconds * _timeScale);
        }

        /// <summary>게임 내 시간(시간 단위)을 배속과 무관하게 즉시 진행 (치트/테스트용).</summary>
        public void SkipGameHours(double gameHours)
        {
            if (gameHours <= 0.0)
                return;
            Advance(gameHours / GameHoursPerRealSecond);
        }

        /// <summary>자원 트랙에만 초를 추가 — S08 오프라인 정산(캡·효율 적용 후)이 사용할 훅.</summary>
        public void AddResourceSeconds(double seconds)
        {
            if (seconds <= 0.0)
                return;
            ResourceSeconds += seconds;
        }

        void Advance(double scaledSeconds)
        {
            TimeOfDay previousTimeOfDay = CurrentTimeOfDay;
            int previousDay = DayIndex;

            EventSeconds += scaledSeconds;
            ResourceSeconds += scaledSeconds;

            int day = DayIndex;
            if (day != previousDay)
                DayChanged?.Invoke(day);

            TimeOfDay timeOfDay = CurrentTimeOfDay;
            if (timeOfDay != previousTimeOfDay)
                TimeOfDayChanged?.Invoke(previousTimeOfDay, timeOfDay);
        }

        /// <summary>세이브 데이터에서 시계 상태 복원.</summary>
        public void RestoreFrom(SaveData data)
        {
            EventSeconds = data.eventSeconds;
            ResourceSeconds = data.resourceSeconds;
        }

        /// <summary>세이브 데이터에 시계 상태 기록.</summary>
        public void WriteTo(SaveData data)
        {
            data.eventSeconds = EventSeconds;
            data.resourceSeconds = ResourceSeconds;
        }
    }
}
