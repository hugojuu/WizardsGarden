using UnityEngine;
using WizardGarden.Core;

namespace WizardGarden.Data
{
    /// <summary>
    /// 식물·가공재료·포션 공통 베이스 — 셋 다 원소 조성과 가치를 가진다 (기획서 3장).
    /// 데이터 컨테이너 SO는 public 필드 컨벤션 (CLAUDE.md).
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("고유 ID (세이브·크로스 참조용, snake_case — 예: plant_ember_grass)")]
        public string id;

        [Tooltip("표시 이름 (한국어)")]
        public string displayName;

        [Header("원소·가치")]
        [Tooltip("원소 조성 (5칸 — 별⭐은 예약)")]
        public ElementComposition composition;

        [Tooltip("기본 가치/판매가 (골드, 비매품은 0)")]
        public int baseValue;

        [Header("표시 (플레이스홀더)")]
        [Tooltip("플레이스홀더 표시용 이모지 — UI는 이 필드를 참조해서만 표시 (정식 아트로 후반 교체)")]
        public string displayEmoji = "❔";
    }
}
