using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 조건 태그(문자열) 해석기 — 매칭 3단계 조건 게이트. 태그는 데이터(PotionData.conditionTags)에
    /// 저장되고 여기서만 해석한다(기획서 6장·CLAUDE.md S01 결정). 인식 못한 태그는 불충족(false)으로 처리해
    /// 오타·미구현 조건이 실수로 통과하지 않게 한다.
    /// 문법:
    ///   night_only            → 야간(21~06시) 한정
    ///   time:morning|day|evening|night
    ///   weather:clear|rain|storm|moonlit_night|meteor_shower|eclipse
    ///   season:spring|summer|autumn|winter
    /// </summary>
    public static class BrewConditionInterpreter
    {
        /// <summary>태그 하나가 현재 상황에서 충족되는가.</summary>
        public static bool IsSatisfied(string tag, IBrewContext context)
        {
            if (string.IsNullOrWhiteSpace(tag) || context == null)
                return false;

            string t = tag.Trim().ToLowerInvariant();

            if (t == "night_only")
                return context.TimeOfDay == TimeOfDay.Night;

            int colon = t.IndexOf(':');
            if (colon <= 0 || colon >= t.Length - 1)
                return false;

            string key = t.Substring(0, colon);
            string value = t.Substring(colon + 1);

            switch (key)
            {
                case "time": return TryParseTime(value, out var tod) && context.TimeOfDay == tod;
                case "weather": return TryParseWeather(value, out var w) && context.Weather == w;
                case "season": return TryParseSeason(value, out var s) && context.Season == s;
                default: return false;
            }
        }

        /// <summary>태그 목록이 모두 충족되는가. 미충족 태그는 unmet에 담아 반환(힌트용).</summary>
        public static bool AllSatisfied(IReadOnlyList<string> tags, IBrewContext context, out List<string> unmet)
        {
            unmet = null;
            if (tags == null || tags.Count == 0)
                return true;

            for (int i = 0; i < tags.Count; i++)
            {
                if (!IsSatisfied(tags[i], context))
                {
                    unmet ??= new List<string>();
                    unmet.Add(tags[i]);
                }
            }
            return unmet == null;
        }

        static bool TryParseTime(string value, out TimeOfDay result)
        {
            switch (value)
            {
                case "morning": result = TimeOfDay.Morning; return true;
                case "day": result = TimeOfDay.Day; return true;
                case "evening": result = TimeOfDay.Evening; return true;
                case "night": result = TimeOfDay.Night; return true;
                default: result = default; return false;
            }
        }

        static bool TryParseWeather(string value, out Weather result)
        {
            switch (value)
            {
                case "clear": result = Weather.Clear; return true;
                case "rain": result = Weather.Rain; return true;
                case "storm": result = Weather.Storm; return true;
                case "moonlit_night": result = Weather.MoonlitNight; return true;
                case "meteor_shower": result = Weather.MeteorShower; return true;
                case "eclipse": result = Weather.Eclipse; return true;
                default: result = default; return false;
            }
        }

        static bool TryParseSeason(string value, out Season result)
        {
            switch (value)
            {
                case "spring": result = Season.Spring; return true;
                case "summer": result = Season.Summer; return true;
                case "autumn": result = Season.Autumn; return true;
                case "winter": result = Season.Winter; return true;
                default: result = default; return false;
            }
        }
    }
}
