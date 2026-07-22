using UnityEngine;
using WizardGarden.Core;

namespace WizardGarden
{
    /// <summary>
    /// 플레이스홀더 아트 규약 색상 (색 사각형 + 이모지/텍스트) — 정식 아트 교체 전까지 사용.
    /// 원소별 색은 기획서 3장 원소 카드 색감 기준.
    /// </summary>
    public static class PlaceholderPalette
    {
        public static readonly Color Fire = new Color(0.85f, 0.33f, 0.19f);
        public static readonly Color Water = new Color(0.23f, 0.51f, 0.77f);
        public static readonly Color Earth = new Color(0.55f, 0.42f, 0.25f);
        public static readonly Color Wind = new Color(0.42f, 0.76f, 0.65f);
        public static readonly Color Star = new Color(0.85f, 0.78f, 0.35f);
        public static readonly Color Neutral = new Color(0.45f, 0.45f, 0.45f);

        /// <summary>빈 밭 슬롯 (흙색).</summary>
        public static readonly Color EmptySoil = new Color(0.25f, 0.20f, 0.16f);

        /// <summary>패널 배경.</summary>
        public static readonly Color PanelBackground = new Color(0.10f, 0.10f, 0.12f, 0.92f);

        public static Color ForElement(Element element)
        {
            switch (element)
            {
                case Element.Fire: return Fire;
                case Element.Water: return Water;
                case Element.Earth: return Earth;
                case Element.Wind: return Wind;
                case Element.Star: return Star;
                default: return Neutral;
            }
        }

        /// <summary>조성의 최다 원소 색 (동률·빈 조성은 회색).</summary>
        public static Color ForComposition(ElementComposition composition)
        {
            return composition.TryGetDominantElement(out Element dominant) ? ForElement(dominant) : Neutral;
        }
    }
}
