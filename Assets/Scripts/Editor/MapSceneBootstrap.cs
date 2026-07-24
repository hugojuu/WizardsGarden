using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardGarden.Data;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// S04b 맵 씬 구성기 — SampleScene에 MapScreen(맵 프레젠테이션)을 배치하고
    /// 카메라를 가로형 구도로 설정, 구 GameScreen 탭 UI는 비활성(디버그 화면 강등).
    /// 메뉴: WizardGarden > Setup Map Scene (S04b) — 여러 번 실행해도 안전.
    /// </summary>
    public static class MapSceneBootstrap
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        // S07 — 식물 20종 전량 (티어1 4종은 처음부터, 나머지는 해금)
        private static readonly string[] SeedAssetPaths =
        {
            "Assets/Data/Plants/Plant_EmberGrass.asset",
            "Assets/Data/Plants/Plant_FlamePoppy.asset",
            "Assets/Data/Plants/Plant_DragonBreathHerb.asset",
            "Assets/Data/Plants/Plant_PhoenixFeather.asset",
            "Assets/Data/Plants/Plant_SunCore.asset",
            "Assets/Data/Plants/Plant_DewMoss.asset",
            "Assets/Data/Plants/Plant_BlueLily.asset",
            "Assets/Data/Plants/Plant_MermaidHair.asset",
            "Assets/Data/Plants/Plant_PearlGrass.asset",
            "Assets/Data/Plants/Plant_MoonTear.asset",
            "Assets/Data/Plants/Plant_WildGrass.asset",
            "Assets/Data/Plants/Plant_MandrakeSprout.asset",
            "Assets/Data/Plants/Plant_GoldenRoot.asset",
            "Assets/Data/Plants/Plant_AncientOakLeaf.asset",
            "Assets/Data/Plants/Plant_WorldTreeSapling.asset",
            "Assets/Data/Plants/Plant_DandelionPuff.asset",
            "Assets/Data/Plants/Plant_WindSongGrass.asset",
            "Assets/Data/Plants/Plant_StormFeatherLeaf.asset",
            "Assets/Data/Plants/Plant_ThundercloudVine.asset",
            "Assets/Data/Plants/Plant_SkySpiritGrass.asset"
        };

        // S07 — 가공 재료 15종 전량 (1차 8 · 2차 4 · 3차 3). 작업대 가공 목록 + 조합 재료
        private static readonly string[] RecipeAssetPaths =
        {
            "Assets/Data/Materials/Material_DriedFlameLeaf.asset",
            "Assets/Data/Materials/Material_DriedDewLeaf.asset",
            "Assets/Data/Materials/Material_DriedEarthGrass.asset",
            "Assets/Data/Materials/Material_DriedWindLeaf.asset",
            "Assets/Data/Materials/Material_FlamePowder.asset",
            "Assets/Data/Materials/Material_WaterPowder.asset",
            "Assets/Data/Materials/Material_EarthPowder.asset",
            "Assets/Data/Materials/Material_WindPowder.asset",
            "Assets/Data/Materials/Material_FireEssence.asset",
            "Assets/Data/Materials/Material_WaterEssence.asset",
            "Assets/Data/Materials/Material_EarthEssence.asset",
            "Assets/Data/Materials/Material_WindEssence.asset",
            "Assets/Data/Materials/Material_StarlightPowder.asset",
            "Assets/Data/Materials/Material_TimeSand.asset",
            "Assets/Data/Materials/Material_RainbowCrystal.asset"
        };

        // S07 — 포션 30종 전량 (도감·조합 매칭·판매·완성도 분모)
        private static readonly string[] PotionAssetPaths =
        {
            "Assets/Data/Potions/Potion_MinorFlame.asset",
            "Assets/Data/Potions/Potion_MinorHeal.asset",
            "Assets/Data/Potions/Potion_MinorGuard.asset",
            "Assets/Data/Potions/Potion_MinorHaste.asset",
            "Assets/Data/Potions/Potion_Steam.asset",
            "Assets/Data/Potions/Potion_Lava.asset",
            "Assets/Data/Potions/Potion_Storm.asset",
            "Assets/Data/Potions/Potion_Herb.asset",
            "Assets/Data/Potions/Potion_Raincloud.asset",
            "Assets/Data/Potions/Potion_Sandstorm.asset",
            "Assets/Data/Potions/Potion_Polymorph.asset",
            "Assets/Data/Potions/Potion_Flight.asset",
            "Assets/Data/Potions/Potion_Invisibility.asset",
            "Assets/Data/Potions/Potion_Spirit.asset",
            "Assets/Data/Potions/Potion_SagesElixir.asset",
            "Assets/Data/Potions/Potion_Geyser.asset",
            "Assets/Data/Potions/Potion_Downpour.asset",
            "Assets/Data/Potions/Potion_Obsidian.asset",
            "Assets/Data/Potions/Potion_Lightning.asset",
            "Assets/Data/Potions/Potion_DragonBlood.asset",
            "Assets/Data/Potions/Potion_Bloom.asset",
            "Assets/Data/Potions/Potion_DragonBreath.asset",
            "Assets/Data/Potions/Potion_MermaidSong.asset",
            "Assets/Data/Potions/Potion_WorldTreeSap.asset",
            "Assets/Data/Potions/Potion_Moonlight.asset",
            "Assets/Data/Potions/Potion_ElixirOfLife.asset",
            "Assets/Data/Potions/Potion_BlackSun.asset",
            "Assets/Data/Potions/Potion_HeartsBrew.asset",
            "Assets/Data/Potions/Potion_Guardian.asset",
            "Assets/Data/Potions/Potion_Luck.asset"
        };

        // 실패 부산물 3종 (실험 일지)
        private static readonly string[] ByproductAssetPaths =
        {
            "Assets/Data/Potions/Potion_Murky.asset",
            "Assets/Data/Potions/Potion_Sediment.asset",
            "Assets/Data/Potions/Potion_Mist.asset"
        };

        // A02 — 맵 아트 (지면은 Bake Map Ground 결과, 소품은 PixelLab 생성분)
        private const string TilesFolder = "Assets/Art/Tiles/";
        private const string PropsFolder = "Assets/Art/Props/";

        private static void WireMapArt(MapScreen map)
        {
            map.art ??= new MapArtSet();
            map.art.ground = LoadSprite(TilesFolder + "MapGround.png");
            map.art.gardenPlot = LoadSprite(TilesFolder + "Tile_GardenPlot.png");
            map.art.fence = LoadSprite(PropsFolder + "Prop_Fence.png");
            map.art.waterBucket = LoadSprite(PropsFolder + "Prop_WaterBucket.png");
            map.art.workbench = LoadSprite(PropsFolder + "Prop_Workbench.png");
            map.art.cauldron = LoadSprite(PropsFolder + "Prop_Cauldron.png");
            map.art.codexBook = LoadSprite(PropsFolder + "Prop_CodexBook.png");
            map.art.shopShelf = LoadSprite(PropsFolder + "Prop_ShopShelf.png");
            map.art.shopSign = LoadSprite(PropsFolder + "Prop_ShopSign.png");
            map.art.shopStall = LoadSprite(PropsFolder + "Prop_ShopStall.png");
            map.art.tree = LoadSprite(PropsFolder + "Prop_Tree.png");
            map.art.rock = LoadSprite(PropsFolder + "Prop_Rock.png");
            map.art.flowers = LoadSprite(PropsFolder + "Prop_Flowers.png");
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[A02] 맵 아트 없음: {path} — 해당 요소는 플레이스홀더로 표시됨");
            return sprite;
        }

        [MenuItem("WizardGarden/Setup Map Scene (S04b)")]
        public static void SetupMapScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath);

            // 카메라 — 가로형 구도 (16:9 기준 세로 10유닛, 미니 모드 대비)
            Camera camera = Camera.main;
            if (camera == null)
                camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.20f, 0.13f);
                EditorUtility.SetDirty(camera);
            }

            var map = Object.FindFirstObjectByType<MapScreen>(FindObjectsInactive.Include);
            if (map == null)
            {
                var go = new GameObject("MapScreen");
                map = go.AddComponent<MapScreen>();
            }

            map.seedOptions.Clear();
            foreach (string path in SeedAssetPaths)
            {
                var plant = AssetDatabase.LoadAssetAtPath<PlantData>(path);
                if (plant != null)
                    map.seedOptions.Add(plant);
                else
                    Debug.LogWarning($"[S04b] 종자 에셋 없음: {path} — 먼저 WizardGarden > Create Sample Data 실행");
            }

            map.recipeOptions.Clear();
            foreach (string path in RecipeAssetPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<MaterialData>(path);
                if (material != null)
                    map.recipeOptions.Add(material);
                else
                    Debug.LogWarning($"[S04b] 재료 에셋 없음: {path} — 먼저 WizardGarden > Create Sample Data 실행");
            }

            map.potionOptions.Clear();
            foreach (string path in PotionAssetPaths)
            {
                var potion = AssetDatabase.LoadAssetAtPath<PotionData>(path);
                if (potion != null)
                    map.potionOptions.Add(potion);
                else
                    Debug.LogWarning($"[S06] 포션 에셋 없음: {path} — 먼저 WizardGarden > Create Sample Data 실행");
            }

            map.byproductOptions.Clear();
            foreach (string path in ByproductAssetPaths)
            {
                var byproduct = AssetDatabase.LoadAssetAtPath<PotionData>(path);
                if (byproduct != null)
                    map.byproductOptions.Add(byproduct);
                else
                    Debug.LogWarning($"[S06] 부산물 에셋 없음: {path} — 먼저 WizardGarden > Create Sample Data 실행");
            }

            WireMapArt(map);

            // 구 GameScreen 탭 UI → 디버그 화면 강등 (삭제 금지 — F12/메뉴로 토글)
            var debugScreen = Object.FindFirstObjectByType<GameScreen>(FindObjectsInactive.Include);
            if (debugScreen != null && debugScreen.gameObject.activeSelf)
            {
                debugScreen.gameObject.SetActive(false);
                Debug.Log("[S04b] GameScreen 탭 UI를 디버그 화면으로 강등 (비활성 — F12로 토글)");
            }

            EditorUtility.SetDirty(map);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EnsureSceneInBuildSettings();

            Debug.Log($"[S04b/S06] 맵 씬 구성 완료 — MapScreen + 종자 {map.seedOptions.Count}종 + 레시피 {map.recipeOptions.Count}종 + 포션 {map.potionOptions.Count}종 + 부산물 {map.byproductOptions.Count}종 ({ScenePath})");
        }

        [MenuItem("WizardGarden/Toggle Debug Screen (Play)")]
        public static void ToggleDebugScreen()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[S04b] 디버그 화면 토글은 플레이 모드에서만 동작 (또는 F12)");
                return;
            }

            var debugScreen = Object.FindFirstObjectByType<GameScreen>(FindObjectsInactive.Include);
            if (debugScreen == null)
            {
                Debug.LogWarning("[S04b] GameScreen(디버그 화면) 없음 — Setup Map Scene (S04b) 실행 확인");
                return;
            }
            debugScreen.gameObject.SetActive(!debugScreen.gameObject.activeSelf);
        }

        private static void EnsureSceneInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path == ScenePath)
                    return;
            }
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
