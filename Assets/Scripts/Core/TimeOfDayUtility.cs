namespace WizardGarden.Core
{
    /// <summary>
    /// 게임 내 시각(0~24h) → 시각 구간 변환 (기획서 22장).
    /// 아침 06~09 / 낮 09~18 / 저녁 18~21 / 야간 21~06.
    /// </summary>
    public static class TimeOfDayUtility
    {
        public const double MorningStartHour = 6.0;
        public const double DayStartHour = 9.0;
        public const double EveningStartHour = 18.0;
        public const double NightStartHour = 21.0;

        /// <summary>시각(시간 단위, 범위 밖 값은 24시간 기준으로 정규화)을 시각 구간으로 변환.</summary>
        public static TimeOfDay FromHour(double hourOfDay)
        {
            double h = hourOfDay % 24.0;
            if (h < 0.0)
                h += 24.0;

            if (h < MorningStartHour) return TimeOfDay.Night;
            if (h < DayStartHour) return TimeOfDay.Morning;
            if (h < EveningStartHour) return TimeOfDay.Day;
            if (h < NightStartHour) return TimeOfDay.Evening;
            return TimeOfDay.Night;
        }
    }
}
