using UnityEngine;

namespace WizardGarden
{
    /// <summary>
    /// 맵 클릭 대상 마커 (S04b) — 종류·인덱스만 보관. 판정·동작 라우팅은 MapScreen 몫.
    /// </summary>
    public sealed class MapTile : MonoBehaviour
    {
        public enum Kind
        {
            GardenTile = 0,
            Bench = 1,
            ShopSlot = 2,
            Cauldron = 3,
            Codex = 4,
            Board = 5   // 견습생 관리 게시판 (S09)
        }

        [Tooltip("클릭 대상 종류")]
        public Kind kind;

        [Tooltip("슬롯 인덱스 (정원 밭·진열대 — 작업대는 0)")]
        public int index;
    }
}
