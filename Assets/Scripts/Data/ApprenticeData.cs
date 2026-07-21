using System.Collections.Generic;
using UnityEngine;
using WizardGarden.Core;

namespace WizardGarden.Data
{
    /// <summary>견습생 데이터 (기획서 10장 — 직군·4스탯·희귀도. 출시 로스터 22명 + 보너스 6명).</summary>
    [CreateAssetMenu(menuName = "WizardGarden/Apprentice Data", fileName = "Apprentice_New")]
    public class ApprenticeData : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("고유 ID (예: appr_ignis)")]
        public string id;

        [Tooltip("표시 이름 (한국어)")]
        public string displayName;

        [Header("직군·희귀도")]
        [Tooltip("직군 (정원사: 정원 / 연금술사: 공방 / 점원: 상점)")]
        public Job job = Job.Gardener;

        [Tooltip("희귀도 — 스탯 총합 가이드: 일반 10~15 / 희귀 18~24 / 영웅 28~35 / 전설 40~50")]
        public Rarity rarity = Rarity.Common;

        [Header("4스탯")]
        [Tooltip("친화력 🌿 — 수확 속도·식물 품질")]
        [Min(0)]
        public int affinity;

        [Tooltip("마력 🔮 — 가공/조합 속도·크리티컬 확률")]
        [Min(0)]
        public int magic;

        [Tooltip("상술 💰 — 판매 속도·가격 보너스")]
        [Min(0)]
        public int trade;

        [Tooltip("운 ✨ — 희귀 부산물·도감 발견 보너스")]
        [Min(0)]
        public int luck;

        [Header("패시브")]
        [Tooltip("패시브 ID 목록 (기획서 11장 — 패시브 데이터 테이블은 후속 세션). 예: passive_green_fingers")]
        public List<string> passiveIds = new List<string>();

        /// <summary>스탯 총합 (희귀도 가이드 검증용).</summary>
        public int StatTotal => affinity + magic + trade + luck;
    }
}
