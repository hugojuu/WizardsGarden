using UnityEditor;
using UnityEngine;
using WizardGarden.Core;
using WizardGarden.Data;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// 샘플 SO 에셋 생성기 (S01 티어1 식물 4종 + 포션 1종, S04 티어2 식물 1종 + 1차 가공 재료 4종).
    /// 메뉴: WizardGarden > Create Sample Data (S01+S04) — 여러 번 실행해도 안전 (기존 에셋은 값만 갱신).
    /// 값 출처: 식물은 기획서 4장, 재료는 5장(조성 유지·가치 ×4~5), 가공 시간·해금가는 S04 결정 (PROGRESS.md).
    /// </summary>
    public static class SampleDataBootstrap
    {
        private const string PlantFolder = "Assets/Data/Plants";
        private const string PotionFolder = "Assets/Data/Potions";
        private const string MaterialFolder = "Assets/Data/Materials";

        [MenuItem("WizardGarden/Create Sample Data (S01+S04+S06)")]
        public static void CreateSampleData()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(PlantFolder);
            EnsureFolder(PotionFolder);
            EnsureFolder(MaterialFolder);

            // 티어1 4종 (기획서 4장: 3초 · 1G · 해금 0)
            CreatePlant("Plant_EmberGrass", "plant_ember_grass", "작은 불꽃풀",
                new ElementComposition(1, 0, 0, 0), "🔥", 1, 3f, 1, 0);
            CreatePlant("Plant_DewMoss", "plant_dew_moss", "이슬 이끼",
                new ElementComposition(0, 1, 0, 0), "💧", 1, 3f, 1, 0);
            CreatePlant("Plant_WildGrass", "plant_wild_grass", "들풀",
                new ElementComposition(0, 0, 1, 0), "🌍", 1, 3f, 1, 0);
            CreatePlant("Plant_DandelionPuff", "plant_dandelion_puff", "민들레 홀씨",
                new ElementComposition(0, 0, 0, 1), "💨", 1, 3f, 1, 0);

            // 티어2 1종 (S04 해금 종자 — 기획서 4장: 15초 · 8G, 해금가 100G는 S04 결정)
            CreatePlant("Plant_FlamePoppy", "plant_flame_poppy", "화염 양귀비",
                new ElementComposition(2, 0, 0, 0), "🌺", 2, 15f, 8, 100);

            // 1차 가공 재료 4종 (기획서 5장: 조성 유지 · 가치 ×5 = 5G, 가공 8초는 S04 결정)
            CreateMaterial("Material_DriedFlameLeaf", "material_dried_flame_leaf", "마른 화염잎",
                new ElementComposition(1, 0, 0, 0), "🍂", 5, "Plant_EmberGrass");
            CreateMaterial("Material_DriedDewLeaf", "material_dried_dew_leaf", "마른 이슬잎",
                new ElementComposition(0, 1, 0, 0), "🍂", 5, "Plant_DewMoss");
            CreateMaterial("Material_DriedEarthGrass", "material_dried_earth_grass", "마른 흙풀",
                new ElementComposition(0, 0, 1, 0), "🍂", 5, "Plant_WildGrass");
            CreateMaterial("Material_DriedWindLeaf", "material_dried_wind_leaf", "마른 바람잎",
                new ElementComposition(0, 0, 0, 1), "🍂", 5, "Plant_DandelionPuff");

            // 포션 — S06 발견 시연용: 현재 재료(마른 잎 4종 = 🔥1/💧1/🌍1/💨1)로 도달 가능한 것만.
            // 단일 4종(원소 ×3 = 마른 잎 ×3) + 2원소 대칭 6종(3+3 = 마른 잎 ×6). 나머지 23종은 S07 몫.
            CreatePotion("Potion_MinorFlame", "potion_minor_flame", "작은 화염 포션",
                new ElementComposition(3, 0, 0, 0), 50, PotionCategory.Attack);
            CreatePotion("Potion_MinorHeal", "potion_minor_heal", "작은 치유 포션",
                new ElementComposition(0, 3, 0, 0), 50, PotionCategory.Recovery);
            CreatePotion("Potion_MinorGuard", "potion_minor_guard", "작은 견고함 포션",
                new ElementComposition(0, 0, 3, 0), 50, PotionCategory.Defense);
            CreatePotion("Potion_MinorHaste", "potion_minor_haste", "작은 신속 포션",
                new ElementComposition(0, 0, 0, 3), 50, PotionCategory.Support);
            CreatePotion("Potion_Steam", "potion_steam", "증기 포션",
                new ElementComposition(3, 3, 0, 0), 400, PotionCategory.Special);
            CreatePotion("Potion_Lava", "potion_lava", "용암 포션",
                new ElementComposition(3, 0, 3, 0), 400, PotionCategory.Special);
            CreatePotion("Potion_Storm", "potion_storm", "폭풍 포션",
                new ElementComposition(3, 0, 0, 3), 400, PotionCategory.Special);
            CreatePotion("Potion_Herb", "potion_herb", "약초 포션",
                new ElementComposition(0, 3, 3, 0), 400, PotionCategory.Special);
            CreatePotion("Potion_Raincloud", "potion_raincloud", "비구름 포션",
                new ElementComposition(0, 3, 0, 3), 400, PotionCategory.Special);
            CreatePotion("Potion_Sandstorm", "potion_sandstorm", "모래폭풍 포션",
                new ElementComposition(0, 0, 3, 3), 400, PotionCategory.Special);

            // 실패 부산물 3종 (실험 일지) — 조성은 산출물이라 0, id는 BrewRecipeFactory 상수와 일치.
            CreatePotion("Potion_Murky", "potion_murky", "탁한 포션",
                new ElementComposition(0, 0, 0, 0), 5, PotionCategory.Special, "🫗");
            CreatePotion("Potion_Sediment", "potion_sediment", "수상한 침전물",
                new ElementComposition(0, 0, 0, 0), 15, PotionCategory.Special, "🧫");
            CreatePotion("Potion_Mist", "potion_mist", "희뿌연 안개병",
                new ElementComposition(0, 0, 0, 0), 12, PotionCategory.Special, "🌫️");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[S01+S04+S06] 샘플 데이터 생성 완료 — 식물 5종 + 재료 4종 + 포션 10종 + 부산물 3종");
        }

        private static void CreatePlant(string assetName, string id, string displayName,
            ElementComposition composition, string displayEmoji, int tier, float growthSeconds,
            int baseValue, int unlockCost)
        {
            string path = $"{PlantFolder}/{assetName}.asset";
            var plant = LoadOrCreate<PlantData>(path);
            plant.id = id;
            plant.displayName = displayName;
            plant.composition = composition;
            plant.tier = tier;
            plant.growthSeconds = growthSeconds;
            plant.baseValue = baseValue;
            plant.unlockCost = unlockCost;
            plant.displayEmoji = displayEmoji;
            EditorUtility.SetDirty(plant);
        }

        private static void CreateMaterial(string assetName, string id, string displayName,
            ElementComposition composition, string displayEmoji, int baseValue, string sourcePlantAssetName)
        {
            string path = $"{MaterialFolder}/{assetName}.asset";
            var material = LoadOrCreate<MaterialData>(path);
            material.id = id;
            material.displayName = displayName;
            material.composition = composition;
            material.baseValue = baseValue;
            material.displayEmoji = displayEmoji;
            material.processingStage = 1;
            material.sourceItem = AssetDatabase.LoadAssetAtPath<PlantData>($"{PlantFolder}/{sourcePlantAssetName}.asset");
            material.sourceCount = 1;
            material.processingSeconds = 8f;
            if (material.sourceItem == null)
                Debug.LogWarning($"[S04] 가공 원료 에셋 없음: {sourcePlantAssetName} — {assetName}의 sourceItem 비어 있음");
            EditorUtility.SetDirty(material);
        }

        private static void CreatePotion(string assetName, string id, string displayName,
            ElementComposition composition, int sellPrice, PotionCategory category, string displayEmoji = "🧪")
        {
            string path = $"{PotionFolder}/{assetName}.asset";
            var potion = LoadOrCreate<PotionData>(path);
            potion.id = id;
            potion.displayName = displayName;
            potion.composition = composition;
            potion.baseValue = sellPrice;
            potion.category = category;
            potion.displayEmoji = displayEmoji;
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
