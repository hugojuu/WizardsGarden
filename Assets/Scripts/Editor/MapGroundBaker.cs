using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// A02 — 맵 지면 굽기. PixelLab Wang 타일셋(4×4=16타일, 32px)을 읽어
    /// 고정 레이아웃 맵의 지면 한 장(`Assets/Art/Tiles/MapGround.png`)으로 합성한다.
    /// 맵이 고정 구도라 런타임 오토타일 시스템 없이 굽는 방식 — 구역 사각형만 고치고 다시 구우면 된다.
    /// 메뉴: WizardGarden > Bake Map Ground (A02) — 여러 번 실행해도 안전.
    /// </summary>
    public static class MapGroundBaker
    {
        const string TilesFolder = "Assets/Art/Tiles";
        const string GrassSoilTileset = TilesFolder + "/Tileset_GardenSoilGrass_Bright.png";
        const string StoneTileset = TilesFolder + "/Tileset_WorkshopStone.png";
        const string WoodTileset = TilesFolder + "/Tileset_ShopWood.png";
        const string GroundOutput = TilesFolder + "/MapGround.png";
        const string PlotOutput = TilesFolder + "/Tile_GardenPlot.png";

        const int TilePixels = 32;          // A01 규격
        const int PlotPixels = 48;          // 밭 한 칸 = 1.5 월드 유닛 (PPU 32)

        // 지면 범위 (월드 유닛) — MapScreen 카메라 구도(가로 ±8.9 · 세로 ±5)를 덮는 크기
        const float GroundMinX = -9.5f;
        const float GroundMaxY = 5.5f;
        const int GroundCols = 19;
        const int GroundRows = 11;

        // 구역 바닥 (월드 유닛, 꼭짓점 기준). Wang 전이 타일이 여기서 한 칸 바깥으로 번진다.
        // 공방 + 조합(가마솥·도감) = 돌바닥 L자, 상점 = 나무바닥. 두 구역은 최소 2칸 떨어뜨려
        // "돌↔나무" 직접 경계(전이 타일 없음)가 생기지 않게 한다.
        static readonly Rect[] StoneRects =
        {
            new Rect(-0.5f, 0.5f, 6.0f, 3.0f),   // 공방 (작업대)
            new Rect(-0.5f, 0.5f, 3.0f, 4.0f)    // 조합 (가마솥·도감)
        };

        static readonly Rect[] WoodRects =
        {
            new Rect(1.5f, -4.5f, 6.0f, 3.0f)    // 상점 (진열대 3칸)
        };

        [MenuItem("WizardGarden/Bake Map Ground (A02)")]
        public static void BakeMapGround()
        {
            if (!TryLoadTileset(GrassSoilTileset, out Tileset gardenSet)
                || !TryLoadTileset(StoneTileset, out Tileset stoneSet)
                || !TryLoadTileset(WoodTileset, out Tileset woodSet))
                return;

            var ground = new Texture2D(GroundCols * TilePixels, GroundRows * TilePixels,
                TextureFormat.RGBA32, false);
            Color32[] grassTile = gardenSet.Tile(gardenSet.BaseTileIndex);

            for (int row = 0; row < GroundRows; row++)
            {
                for (int col = 0; col < GroundCols; col++)
                {
                    int mask = CornerMask(col, row, StoneRects);
                    Color32[] tile;
                    if (mask != 0)
                        tile = stoneSet.Tile(stoneSet.TileForMask(mask));
                    else
                    {
                        int woodMask = CornerMask(col, row, WoodRects);
                        tile = woodMask != 0 ? woodSet.Tile(woodSet.TileForMask(woodMask)) : grassTile;
                    }
                    Blit(ground, tile, col, row, GroundRows);
                }
            }
            ground.Apply();
            int groundWidth = ground.width;
            int groundHeight = ground.height;
            WritePng(ground, GroundOutput);

            // 밭 한 칸 (1.5유닛) — 정원 흙 타일을 이어붙여 자른다
            Color32[] soil = gardenSet.Tile(gardenSet.OverlayTileIndex);
            var plot = new Texture2D(PlotPixels, PlotPixels, TextureFormat.RGBA32, false);
            var plotPixels = new Color32[PlotPixels * PlotPixels];
            for (int y = 0; y < PlotPixels; y++)
                for (int x = 0; x < PlotPixels; x++)
                    plotPixels[y * PlotPixels + x] = soil[(y % TilePixels) * TilePixels + (x % TilePixels)];
            plot.SetPixels32(plotPixels);
            plot.Apply();
            WritePng(plot, PlotOutput);

            AssetDatabase.Refresh();
            Debug.Log($"[A02] 지면 굽기 완료 — {GroundOutput} ({groundWidth}×{groundHeight}) · {PlotOutput}");
        }

        // ---- Wang 코너 판정 ----

        // 셀의 네 꼭짓점이 구역 안인지로 마스크 계산 (bit0 좌상 · bit1 우상 · bit2 우하 · bit3 좌하).
        static int CornerMask(int col, int row, Rect[] rects)
        {
            float left = GroundMinX + col;
            float top = GroundMaxY - row;
            int mask = 0;
            if (Inside(left, top, rects)) mask |= 1;
            if (Inside(left + 1f, top, rects)) mask |= 2;
            if (Inside(left + 1f, top - 1f, rects)) mask |= 4;
            if (Inside(left, top - 1f, rects)) mask |= 8;
            return mask;
        }

        static bool Inside(float x, float y, Rect[] rects)
        {
            const float eps = 0.001f;
            foreach (Rect rect in rects)
            {
                if (x >= rect.xMin - eps && x <= rect.xMax + eps
                    && y >= rect.yMin - eps && y <= rect.yMax + eps)
                    return true;
            }
            return false;
        }

        static void Blit(Texture2D target, Color32[] tile, int col, int row, int rows)
        {
            int originX = col * TilePixels;
            int originY = (rows - 1 - row) * TilePixels;   // Texture2D는 아래가 y=0
            for (int y = 0; y < TilePixels; y++)
                for (int x = 0; x < TilePixels; x++)
                    target.SetPixel(originX + x, originY + y, tile[y * TilePixels + x]);
        }

        // ---- 타일셋 판독 ----

        /// <summary>
        /// 4×4 Wang 타일셋 한 장. 타일별 네 모서리 색을 두 지형 대표색과 비교해
        /// 코너 마스크 → 타일 인덱스 표를 만든다 (레이아웃을 가정하지 않음).
        /// </summary>
        sealed class Tileset
        {
            public Color32[][] Tiles;                 // 16개, 각 32×32 (아래가 y=0)
            public int BaseTileIndex;                 // 잔디(마스크 0) 타일
            public int OverlayTileIndex;              // 반대 지형(마스크 15) 타일
            readonly int[] _maskToTile = new int[16];

            public Color32[] Tile(int index) => Tiles[index];
            public int TileForMask(int mask) => _maskToTile[mask];
            public int[] MaskTable => _maskToTile;
        }

        static bool TryLoadTileset(string assetPath, out Tileset tileset)
        {
            tileset = null;
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!File.Exists(full))
            {
                Debug.LogError($"[A02] 타일셋 없음: {assetPath}");
                return false;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(full)))
            {
                Debug.LogError($"[A02] 타일셋 판독 실패: {assetPath}");
                return false;
            }
            if (texture.width != TilePixels * 4 || texture.height != TilePixels * 4)
            {
                Debug.LogError($"[A02] 타일셋 규격 불일치({texture.width}×{texture.height}): {assetPath} — 32px 4×4여야 함");
                return false;
            }

            var set = new Tileset { Tiles = new Color32[16][] };
            int sourceWidth = texture.width;
            Color32[] source = texture.GetPixels32();
            Object.DestroyImmediate(texture);
            for (int index = 0; index < 16; index++)
            {
                int gridRow = index / 4;
                int gridCol = index % 4;
                var tile = new Color32[TilePixels * TilePixels];
                for (int y = 0; y < TilePixels; y++)
                {
                    int sourceY = (3 - gridRow) * TilePixels + y;
                    for (int x = 0; x < TilePixels; x++)
                        tile[y * TilePixels + x] = source[sourceY * sourceWidth + gridCol * TilePixels + x];
                }
                set.Tiles[index] = tile;
            }

            // 대표색: 가장 초록인 타일 = 잔디, 가장 덜 초록인 타일 = 구역 바닥
            Vector3[] averages = new Vector3[16];
            int greenest = 0, leastGreen = 0;
            for (int i = 0; i < 16; i++)
            {
                averages[i] = Average(set.Tiles[i], 0, 0, TilePixels);
                float greenness = Greenness(averages[i]);
                if (greenness > Greenness(averages[greenest])) greenest = i;
                if (greenness < Greenness(averages[leastGreen])) leastGreen = i;
            }
            Vector3 grassRef = averages[greenest];
            Vector3 overlayRef = averages[leastGreen];

            const int probe = 10;
            var seen = new HashSet<int>();
            for (int i = 0; i < 16; i++)
            {
                int mask = 0;
                // 코너 순서: 좌상 · 우상 · 우하 · 좌하 (텍스처는 아래가 y=0이므로 상단 = 높은 y)
                if (IsOverlay(set.Tiles[i], 0, TilePixels - probe, probe, grassRef, overlayRef)) mask |= 1;
                if (IsOverlay(set.Tiles[i], TilePixels - probe, TilePixels - probe, probe, grassRef, overlayRef)) mask |= 2;
                if (IsOverlay(set.Tiles[i], TilePixels - probe, 0, probe, grassRef, overlayRef)) mask |= 4;
                if (IsOverlay(set.Tiles[i], 0, 0, probe, grassRef, overlayRef)) mask |= 8;

                if (!seen.Add(mask))
                    Debug.LogWarning($"[A02] {Path.GetFileName(assetPath)} 코너 마스크 중복 {mask} (타일 {i}) — 전이가 어색할 수 있음");
                set.MaskTable[mask] = i;
            }
            set.BaseTileIndex = set.TileForMask(0);
            set.OverlayTileIndex = set.TileForMask(15);

            tileset = set;
            return true;
        }

        static bool IsOverlay(Color32[] tile, int x0, int y0, int size, Vector3 grassRef, Vector3 overlayRef)
        {
            Vector3 average = Average(tile, x0, y0, size);
            return (average - overlayRef).sqrMagnitude < (average - grassRef).sqrMagnitude;
        }

        static Vector3 Average(Color32[] tile, int x0, int y0, int size)
        {
            var sum = Vector3.zero;
            for (int y = y0; y < y0 + size; y++)
                for (int x = x0; x < x0 + size; x++)
                {
                    Color32 c = tile[y * TilePixels + x];
                    sum += new Vector3(c.r, c.g, c.b);
                }
            return sum / (size * size);
        }

        static float Greenness(Vector3 color) => color.y - (color.x + color.z) * 0.5f;

        static void WritePng(Texture2D texture, string assetPath)
        {
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), assetPath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
    }
}
