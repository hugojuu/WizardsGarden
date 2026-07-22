using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardGarden.Data;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// S03 정원 씬 구성기 — SampleScene에 GardenScreen을 배치하고 티어1 종자 4종을 연결.
    /// 메뉴: WizardGarden > Setup Garden Scene (S03) — 여러 번 실행해도 안전.
    /// Build Settings에 씬이 없으면 등록한다.
    /// </summary>
    public static class GardenSceneBootstrap
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        private static readonly string[] SeedAssetPaths =
        {
            "Assets/Data/Plants/Plant_EmberGrass.asset",
            "Assets/Data/Plants/Plant_DewMoss.asset",
            "Assets/Data/Plants/Plant_WildGrass.asset",
            "Assets/Data/Plants/Plant_DandelionPuff.asset"
        };

        [MenuItem("WizardGarden/Setup Garden Scene (S03)")]
        public static void SetupGardenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath);

            var screen = Object.FindFirstObjectByType<GardenScreen>();
            if (screen == null)
            {
                var go = new GameObject("GardenScreen");
                screen = go.AddComponent<GardenScreen>();
            }

            screen.seedOptions.Clear();
            foreach (string path in SeedAssetPaths)
            {
                var plant = AssetDatabase.LoadAssetAtPath<PlantData>(path);
                if (plant != null)
                    screen.seedOptions.Add(plant);
                else
                    Debug.LogWarning($"[S03] 종자 에셋 없음: {path} — 먼저 WizardGarden > Create Sample Data (S01) 실행");
            }

            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EnsureSceneInBuildSettings();

            Debug.Log($"[S03] 정원 씬 구성 완료 — GardenScreen + 종자 {screen.seedOptions.Count}종 ({ScenePath})");
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
