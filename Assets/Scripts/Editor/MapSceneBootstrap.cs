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

            Debug.Log($"[S04b] 맵 씬 구성 완료 — MapScreen + 종자 {map.seedOptions.Count}종 + 레시피 {map.recipeOptions.Count}종 ({ScenePath})");
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
