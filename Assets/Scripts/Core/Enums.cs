namespace WizardGarden.Core
{
    /// <summary>원소. Star는 출시 후 보너스용 예약 슬롯 (기획서 3장).</summary>
    public enum Element
    {
        Fire = 0,
        Water = 1,
        Earth = 2,
        Wind = 3,
        Star = 4
    }

    /// <summary>계절 (1계절 = 게임 내 7일 = 현실 1시간).</summary>
    public enum Season
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    /// <summary>날씨 6종 (맑음/비/폭풍우/달빛밤/유성우/일식).</summary>
    public enum Weather
    {
        Clear = 0,
        Rain = 1,
        Storm = 2,
        MoonlitNight = 3,
        MeteorShower = 4,
        Eclipse = 5
    }

    /// <summary>희귀도 4단계 (일반/희귀/영웅/전설 — 기획서 10장).</summary>
    public enum Rarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    /// <summary>견습생 직군 3종 (정원사/연금술사/점원 — 기획서 10장).</summary>
    public enum Job
    {
        Gardener = 0,
        Alchemist = 1,
        Clerk = 2
    }

    /// <summary>게임 내 시간대 (아침 06~09 / 낮 09~18 / 저녁 18~21 / 야간 21~06).</summary>
    public enum TimeOfDay
    {
        Morning = 0,
        Day = 1,
        Evening = 2,
        Night = 3
    }

    /// <summary>포션 카테고리 (공격/회복/방어/보조/특수 — 기획서 6장).</summary>
    public enum PotionCategory
    {
        Attack = 0,
        Recovery = 1,
        Defense = 2,
        Support = 3,
        Special = 4
    }
}
