namespace WizardGarden.Core
{
    /// <summary>
    /// 조합 조건 게이트(매칭 3단계)에 주입되는 현재 상황 — 시간대/날씨/계절.
    /// 실제 값은 S11(사건 시간·날씨)이 공급한다. S05는 이 인터페이스로만 의존해
    /// 매칭 엔진을 순수하게 유지한다.
    /// </summary>
    public interface IBrewContext
    {
        TimeOfDay TimeOfDay { get; }
        Weather Weather { get; }
        Season Season { get; }
    }

    /// <summary>테스트·어댑터용 단순 IBrewContext 구현.</summary>
    public readonly struct BrewContext : IBrewContext
    {
        public TimeOfDay TimeOfDay { get; }
        public Weather Weather { get; }
        public Season Season { get; }

        public BrewContext(TimeOfDay timeOfDay, Weather weather, Season season = Season.Spring)
        {
            TimeOfDay = timeOfDay;
            Weather = weather;
            Season = season;
        }
    }
}
