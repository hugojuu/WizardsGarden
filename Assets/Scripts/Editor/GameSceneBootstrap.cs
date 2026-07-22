using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardGarden.Data;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// S04 게임 씬 구성기 — SampleScene에 GameScreen(3구역 통합)을 배치하고
    /// 종자 5종(티어1 4 + 티어2 해금 1)·가공 레시피 4종을 연결. S03 GardenScreen 잔재는 제거.
    /// 메뉴: WizardGarden > Setup Game Scene (S04) — 여러 번 실행해도 안전.
    /// </summary>
    public static class GameSceneBootstrap
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        private static readonly string[] SeedAssetPaths =
        {
            "Assets/Data/Plants/Plant_EmberGrass.asset",
            "Assets/Data/Plants/Plant_DewMoss.asset",
            "Assets/Data/Plants/Plant_WildGrass.asset",
            "Assets/Data/Plants/Plant_DandelionPuff.asset",
            "Assets/Data/Plants/Plant_FlamePoppy.asset"
        };

        private static readonly string[] RecipeAssetPaths =
        {
            "Assets/Data/Materials/Material_DriedFlameLeaf.asset",
            "Assets/Data/Materials/Material_DriedDewLeaf.asset",
            "Assets/Data/Materials/Material_DriedEarthGrass.asset",
            "Assets/Data/Materials/Material_DriedWindLeaf.asset"
        };

        [MenuItem("WizardGarden/Setup Game Scene (S04)")]
        public static void SetupGameScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath);

            RemoveLegacyGardenScreen();

            var screen = Object.FindFirstObjectByType<GameScreen>();
            if (screen == null)
            {
                var go = new GameObject("GameScreen");
                screen = go.AddComponent<GameScreen>();
            }

            screen.seedOptions.Clear();
            foreach (string path in SeedAssetPaths)
            {
                var plant = AssetDatabase.LoadAssetAtPath<PlantData>(path);
                if (plant != null)
                    screen.seedOptions.Add(plant);
                else
                    Debug.LogWarning($"[S04] 종자 에셋 없음: {path} — 먼저 WizardGarden > Create Sample Data 실행");
            }

            screen.recipeOptions.Clear();
            foreach (string path in RecipeAssetPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<MaterialData>(path);
                if (material != null)
                    screen.recipeOptions.Add(material);
                else
                    Debug.LogWarning($"[S04] 재료 에셋 없음: {path} — 먼저 WizardGarden > Create Sample Data 실행");
            }

            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EnsureSceneInBuildSettings();

            Debug.Log($"[S04] 게임 씬 구성 완료 — GameScreen + 종자 {screen.seedOptions.Count}종 + 레시피 {screen.recipeOptions.Count}종 ({ScenePath})");
        }

        /// <summary>S03 GardenScreen GameObject(스크립트 삭제로 missing component) 정리.</summary>
        private static void RemoveLegacyGardenScreen()
        {
            var legacy = GameObject.Find("GardenScreen");
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy);
                Debug.Log("[S04] 구 GardenScreen 오브젝트 제거");
            }
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
