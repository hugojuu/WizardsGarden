using System;
using UnityEngine;

namespace WizardGarden
{
    /// <summary>
    /// 맵 아트 참조 묶음 (A02). 스프라이트는 전부 인스펙터 필드로만 연결한다 —
    /// 코드에 경로를 박지 않는 것이 아트 파이프라인 방침(CLAUDE.md). 비어 있으면
    /// MapScreen이 S04b 플레이스홀더(색 사각형)로 되돌아간다.
    /// 배선은 에디터 메뉴 WizardGarden > Setup Map Scene (S04b)가 담당.
    /// </summary>
    [Serializable]
    public sealed class MapArtSet
    {
        [Tooltip("맵 지면 한 장 (Bake Map Ground로 구운 결과 — 잔디 + 구역 바닥)")]
        public Sprite ground;

        [Tooltip("밭 한 칸 흙 (1.5 유닛 = 48px)")]
        public Sprite gardenPlot;

        [Tooltip("정원 울타리 한 조각")]
        public Sprite fence;

        [Tooltip("정원 물통")]
        public Sprite waterBucket;

        [Tooltip("공방 작업대")]
        public Sprite workbench;

        [Tooltip("조합 가마솥")]
        public Sprite cauldron;

        [Tooltip("도감 책")]
        public Sprite codexBook;

        [Tooltip("상점 진열대 (3칸 공용)")]
        public Sprite shopShelf;

        [Tooltip("상점 간판")]
        public Sprite shopSign;

        [Tooltip("상점 건물 (배경 소품)")]
        public Sprite shopStall;

        [Tooltip("여백 장식 — 나무")]
        public Sprite tree;

        [Tooltip("여백 장식 — 돌")]
        public Sprite rock;

        [Tooltip("여백 장식 — 꽃")]
        public Sprite flowers;

        /// <summary>지면 아트가 배선되었는가 (구역 바닥이 지면에 구워져 있으므로 색 패치를 생략).</summary>
        public bool HasGround => ground != null;
    }
}
