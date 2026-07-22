using UnityEditor;
using UnityEngine;
using WizardGarden.Core;
using WizardGarden.Data;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// S01 샘플 SO 에셋 생성기 (에디터 미연결 세션 대체 수단).
    /// 메뉴: WizardGarden > Create Sample Data (S01) — 여러 번 실행해도 안전 (기존 에셋은 값만 갱신).
    /// 생성물: 티어1 식물 4종 + 작은 화염 포션 1종 (기획서 4·6장).
    /// </summary>
    public static class SampleDataBootstrap
    {
        private const string PlantFolder = "Assets/Data/Plants";
        private const string PotionFolder = "Assets/Data/Potions";

        [MenuItem("WizardGarden/Create Sample Data (S01)")]
        public static void CreateSampleData()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(PlantFolder);
            EnsureFolder(PotionFolder);

            CreatePlant("Plant_EmberGrass", "plant_ember_grass", "작은 불꽃풀",
                new ElementComposition(1, 0, 0, 0), "🔥");
            CreatePlant("Plant_DewMoss", "plant_dew_moss", "이슬 이끼",
                new ElementComposition(0, 1, 0, 0), "💧");
            CreatePlant("Plant_WildGrass", "plant_wild_grass", "들풀",
                new ElementComposition(0, 0, 1, 0), "🌍");
            CreatePlant("Plant_DandelionPuff", "plant_dandelion_puff", "민들레 홀씨",
                new ElementComposition(0, 0, 0, 1), "💨");

            CreatePotion("Potion_MinorFlame", "potion_minor_flame", "작은 화염 포션",
                new ElementComposition(3, 0, 0, 0), 50, PotionCategory.Attack);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[S01] 샘플 데이터 생성 완료 — 티어1 식물 4종 (Assets/Data/Plants) + 포션 1종 (Assets/Data/Potions)");
        }

        private static void CreatePlant(string assetName, string id, string displayName,
            ElementComposition composition, string displayEmoji)
        {
            string path = $"{PlantFolder}/{assetName}.asset";
            var plant = LoadOrCreate<PlantData>(path);
            plant.id = id;
            plant.displayName = displayName;
            plant.composition = composition;
            plant.tier = 1;
            plant.growthSeconds = 3f;
            plant.baseValue = 1;
            plant.displayEmoji = displayEmoji;
            EditorUtility.SetDirty(plant);
        }

        private static void CreatePotion(string assetName, string id, string displayName,
            ElementComposition composition, int sellPrice, PotionCategory category)
        {
            string path = $"{PotionFolder}/{assetName}.asset";
            var potion = LoadOrCreate<PotionData>(path);
            potion.id = id;
            potion.displayName = displayName;
            potion.composition = composition;
            potion.baseValue = sellPrice;
            potion.category = category;
            potion.requiredIngredients.Clear();
            potion.conditionTags.Clear();
            potion.equipEffectId = string.Empty;
            EditorUtility.SetDirty(potion);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                int slash = path.LastIndexOf('/');
                AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
            }
        }
    }
}
