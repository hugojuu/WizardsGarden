using System;
using UnityEngine;

namespace WizardGarden.Core
{
    /// <summary>
    /// 5칸 원소 조성 (불/물/대지/바람/별⭐예약 — 기획서 3장, CLAUDE.md 아키텍처 결정).
    /// 모든 식물·재료·포션이 이 값을 가진다. 값 타입 — 복사 안전.
    /// </summary>
    [Serializable]
    public struct ElementComposition : IEquatable<ElementComposition>
    {
        /// <summary>슬롯 수 (Star 예약 포함 5칸 고정).</summary>
        public const int SlotCount = 5;

        [Tooltip("불 🔥")] public int fire;
        [Tooltip("물 💧")] public int water;
        [Tooltip("대지 🌍")] public int earth;
        [Tooltip("바람 💨")] public int wind;
        [Tooltip("별 ⭐ (출시 후 보너스용 예약 — 현재 항상 0)")] public int star;

        public ElementComposition(int fire, int water, int earth, int wind, int star = 0)
        {
            this.fire = fire;
            this.water = water;
            this.earth = earth;
            this.wind = wind;
            this.star = star;
        }

        /// <summary>원소별 접근 인덱서.</summary>
        public int this[Element element]
        {
            get => element switch
            {
                Element.Fire => fire,
                Element.Water => water,
                Element.Earth => earth,
                Element.Wind => wind,
                Element.Star => star,
                _ => throw new ArgumentOutOfRangeException(nameof(element), element, null)
            };
            set
            {
                switch (element)
                {
                    case Element.Fire: fire = value; break;
                    case Element.Water: water = value; break;
                    case Element.Earth: earth = value; break;
                    case Element.Wind: wind = value; break;
                    case Element.Star: star = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(element), element, null);
                }
            }
        }

        /// <summary>전체 원소 합.</summary>
        public int Total => fire + water + earth + wind + star;

        /// <summary>모든 슬롯이 0인가.</summary>
        public bool IsEmpty => Total == 0;

        /// <summary>합산 (두 조성의 슬롯별 합).</summary>
        public static ElementComposition operator +(ElementComposition a, ElementComposition b)
        {
            return new ElementComposition(
                a.fire + b.fire,
                a.water + b.water,
                a.earth + b.earth,
                a.wind + b.wind,
                a.star + b.star);
        }

        /// <summary>일치 비교 — 다섯 슬롯이 전부 같아야 참 (조성 매칭 판정용).</summary>
        public bool Matches(ElementComposition other) => Equals(other);

        /// <summary>포함 여부 — 이 조성이 other의 모든 슬롯 이상을 갖는가 (재료 충분 판정용).</summary>
        public bool Contains(ElementComposition other)
        {
            return fire >= other.fire
                && water >= other.water
                && earth >= other.earth
                && wind >= other.wind
                && star >= other.star;
        }

        /// <summary>
        /// 최다 원소. 단독 최다가 있으면 true, 동률·전부 0이면 false
        /// (동률 = 실패 부산물 "탁한 포션" 분기 조건 — 기획서 6장).
        /// </summary>
        public bool TryGetDominantElement(out Element dominant)
        {
            dominant = Element.Fire;
            if (IsEmpty)
                return false;

            int best = int.MinValue;
            bool tie = false;
            for (int i = 0; i < SlotCount; i++)
            {
                int value = this[(Element)i];
                if (value > best)
                {
                    best = value;
                    dominant = (Element)i;
                    tie = false;
                }
                else if (value == best)
                {
                    tie = true;
                }
            }
            return !tie;
        }

        public bool Equals(ElementComposition other)
        {
            return fire == other.fire
                && water == other.water
                && earth == other.earth
                && wind == other.wind
                && star == other.star;
        }

        public override bool Equals(object obj) => obj is ElementComposition other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(fire, water, earth, wind, star);

        public static bool operator ==(ElementComposition a, ElementComposition b) => a.Equals(b);
        public static bool operator !=(ElementComposition a, ElementComposition b) => !a.Equals(b);

        public override string ToString()
        {
            return $"(🔥{fire} 💧{water} 🌍{earth} 💨{wind} ⭐{star})";
        }
    }
}
