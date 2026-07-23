using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WizardGarden.Core;
using WizardGarden.Data;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// 전체 콘텐츠 SO 생성기 (S07 — 식물 20 · 재료 15 · 포션 30 + 실패 부산물 3 = 68종).
    /// 메뉴: WizardGarden > Create Sample Data (S07) — 여러 번 실행해도 안전 (기존 에셋은 값만 갱신).
    /// 값 출처: 식물은 기획서 4장, 재료는 5장(조성 유지·가치 ×4~5), 포션은 6장(BrewFixture와 1:1).
    /// 해금가·가공 시간·재료 세부가는 S07 경제 검증 튜닝값 (PROGRESS.md 대조표 참조).
    /// 가공 체인: 식물 → 1차(마른 잎/가루) → 2차(정수) → 3차(별빛 분말/시간의 모래/무지개 수정).
    /// </summary>
    public static class SampleDataBootstrap
    {
        private const string PlantFolder = "Assets/Data/Plants";
        private const string PotionFolder = "Assets/Data/Potions";
        private const string MaterialFolder = "Assets/Data/Materials";

        // 티어별 해금가 (식물 1종당 — 4종 전부 해금 시 이 값 ×4가 "티어 도약 비용" = 이전 티어 누적수입 × 0.1, 기획서 8장).
        // S07 경제 검증 튜닝값 (4종합을 각 티어 도달 시점 누적골드의 ~10%에 맞춤 — PROGRESS.md 대조표).
        private const int UnlockT2 = 100;     // 4종합 400G  (티어2 도달 ~500G 시점)
        private const int UnlockT3 = 800;     // 4종합 3.2K  (티어3 도달 ~5~50K 시점)
        private const int UnlockT4 = 12000;   // 4종합 48K   (티어4 도달 ~500K 시점 → ×0.1)
        private const int UnlockT5 = 250000;  // 4종합 1M    (티어5 도달 ~10M 시점 → ×0.1)

        // 티어별 성장 시간 (초) — 기획서 4장 (3초 / 15초 / 1분 / 5분 / 25분)
        private static readonly float[] GrowthByTier = { 0f, 3f, 15f, 60f, 300f, 1500f };
        // 티어별 식물 가치 (골드) — 기획서 4장 (×7~8 배율)
        private static readonly int[] ValueByTier = { 0, 1, 8, 60, 450, 3400 };

        [MenuItem("WizardGarden/Create Sample Data (S07)")]
        public static void CreateSampleData()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(PlantFolder);
            EnsureFolder(PotionFolder);
            EnsureFolder(MaterialFolder);

            CreatePlants();
            CreateMaterials();
            CreatePotions();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[S07] 전체 콘텐츠 생성 완료 — 식물 20종 + 재료 15종 + 포션 30종 + 부산물 3종");
        }

        // ---- 식물 20종 (기획서 4장: 티어1~5 × 4계열) ----

        static void CreatePlants()
        {
            // 🔥 불 계열
            CreatePlant("Plant_EmberGrass", "plant_ember_grass", "작은 불꽃풀", C(1, 0, 0, 0), "🔥", 1, 0);
            CreatePlant("Plant_FlamePoppy", "plant_flame_poppy", "화염 양귀비", C(2, 0, 0, 0), "🌺", 2, UnlockT2);
            CreatePlant("Plant_DragonBreathHerb", "plant_dragon_breath_herb", "용의 입김초", C(2, 0, 0, 1), "🐉", 3, UnlockT3);
            CreatePlant("Plant_PhoenixFeather", "plant_phoenix_feather", "불사조 깃털꽃", C(3, 0, 0, 1), "🪶", 4, UnlockT4);
            CreatePlant("Plant_SunCore", "plant_sun_core", "태양의 핵", C(4, 0, 1, 0), "☀️", 5, UnlockT5);

            // 💧 물 계열
            CreatePlant("Plant_DewMoss", "plant_dew_moss", "이슬 이끼", C(0, 1, 0, 0), "💧", 1, 0);
            CreatePlant("Plant_BlueLily", "plant_blue_lily", "푸른 수련", C(0, 2, 0, 0), "🪷", 2, UnlockT2);
            CreatePlant("Plant_MermaidHair", "plant_mermaid_hair", "인어의 머리카락", C(0, 2, 1, 0), "🧜", 3, UnlockT3);
            CreatePlant("Plant_PearlGrass", "plant_pearl_grass", "심해 진주초", C(0, 3, 1, 0), "🫧", 4, UnlockT4);
            CreatePlant("Plant_MoonTear", "plant_moon_tear", "달의 눈물", C(0, 4, 0, 1), "🌙", 5, UnlockT5);

            // 🌍 대지 계열
            CreatePlant("Plant_WildGrass", "plant_wild_grass", "들풀", C(0, 0, 1, 0), "🌍", 1, 0);
            CreatePlant("Plant_MandrakeSprout", "plant_mandrake_sprout", "만드라고라 새싹", C(0, 0, 2, 0), "🌱", 2, UnlockT2);
            CreatePlant("Plant_GoldenRoot", "plant_golden_root", "황금 뿌리", C(1, 0, 2, 0), "🥕", 3, UnlockT3);
            CreatePlant("Plant_AncientOakLeaf", "plant_ancient_oak_leaf", "고대 떡갈잎", C(0, 0, 3, 1), "🍁", 4, UnlockT4);
            CreatePlant("Plant_WorldTreeSapling", "plant_world_tree_sapling", "세계수 묘목", C(0, 1, 4, 0), "🌳", 5, UnlockT5);

            // 💨 바람 계열
            CreatePlant("Plant_DandelionPuff", "plant_dandelion_puff", "민들레 홀씨", C(0, 0, 0, 1), "💨", 1, 0);
            CreatePlant("Plant_WindSongGrass", "plant_wind_song_grass", "바람의 노래풀", C(0, 0, 0, 2), "🎐", 2, UnlockT2);
            CreatePlant("Plant_StormFeatherLeaf", "plant_storm_feather_leaf", "폭풍의 깃털잎", C(0, 1, 0, 2), "🪁", 3, UnlockT3);
            CreatePlant("Plant_ThundercloudVine", "plant_thundercloud_vine", "천둥구름 덩굴", C(1, 0, 0, 3), "⛈️", 4, UnlockT4);
            CreatePlant("Plant_SkySpiritGrass", "plant_sky_spirit_grass", "하늘 정령초", C(1, 0, 0, 4), "🌫️", 5, UnlockT5);
        }

        // ---- 재료 15종 (기획서 5장: 1차 8 · 2차 4 · 3차 3) ----

        static void CreateMaterials()
        {
            // 1차 가공 (8종) — 조성 유지, 가치 ×5
            // 마른 잎 4종 (티어1 식물, 5G, 8초) — S04 기존
            CreateMaterial("Material_DriedFlameLeaf", "material_dried_flame_leaf", "마른 화염잎", C(1, 0, 0, 0), "🍂", 5, 1,
                "Plant_EmberGrass", 1, 8f);
            CreateMaterial("Material_DriedDewLeaf", "material_dried_dew_leaf", "마른 이슬잎", C(0, 1, 0, 0), "🍂", 5, 1,
                "Plant_DewMoss", 1, 8f);
            CreateMaterial("Material_DriedEarthGrass", "material_dried_earth_grass", "마른 흙풀", C(0, 0, 1, 0), "🍂", 5, 1,
                "Plant_WildGrass", 1, 8f);
            CreateMaterial("Material_DriedWindLeaf", "material_dried_wind_leaf", "마른 바람잎", C(0, 0, 0, 1), "🍂", 5, 1,
                "Plant_DandelionPuff", 1, 8f);
            // 가루 4종 (티어2 식물 농축, 40G, 12초)
            CreateMaterial("Material_FlamePowder", "material_flame_powder", "화염 가루", C(2, 0, 0, 0), "🟥", 40, 1,
                "Plant_FlamePoppy", 1, 12f);
            CreateMaterial("Material_WaterPowder", "material_water_powder", "정수 가루", C(0, 2, 0, 0), "🟦", 40, 1,
                "Plant_BlueLily", 1, 12f);
            CreateMaterial("Material_EarthPowder", "material_earth_powder", "흙 가루", C(0, 0, 2, 0), "🟫", 40, 1,
                "Plant_MandrakeSprout", 1, 12f);
            CreateMaterial("Material_WindPowder", "material_wind_powder", "풍령 가루", C(0, 0, 0, 2), "⬜", 40, 1,
                "Plant_WindSongGrass", 1, 12f);

            // 2차 가공 (4종) — 정수(증류/추출): 가루 ×2 → 정수 1, 150G, 30초
            CreateMaterial("Material_FireEssence", "material_fire_essence", "불의 정수", C(3, 0, 0, 0), "🔥", 150, 2,
                "Material_FlamePowder", 2, 30f);
            CreateMaterial("Material_WaterEssence", "material_water_essence", "물의 정수", C(0, 3, 0, 0), "💧", 150, 2,
                "Material_WaterPowder", 2, 30f);
            CreateMaterial("Material_EarthEssence", "material_earth_essence", "대지의 정수", C(0, 0, 3, 0), "🌍", 150, 2,
                "Material_EarthPowder", 2, 30f);
            CreateMaterial("Material_WindEssence", "material_wind_essence", "바람의 정수", C(0, 0, 0, 3), "💨", 150, 2,
                "Material_WindPowder", 2, 30f);

            // 3차 가공 (3종) — 희귀 변환
            // 별빛 분말 ⭐ — 티어5 식물 ×2 (단일 입력, 태양의 핵을 대표 티어5로. star 예약이라 조성 0). 5000G, 60초
            CreateMaterial("Material_StarlightPowder", "material_starlight_powder", "별빛 분말", C(0, 0, 0, 0), "⭐", 5000, 3,
                "Plant_SunCore", 2, 60f);
            // 시간의 모래 ⏳ — 정수 4종 모두 사용 (다중 입력). 2000G, 60초
            CreateMaterial("Material_TimeSand", "material_time_sand", "시간의 모래", C(0, 0, 0, 0), "⏳", 2000, 3,
                "Material_FireEssence", 1, 60f,
                Extra("Material_WaterEssence", 1), Extra("Material_EarthEssence", 1), Extra("Material_WindEssence", 1));
            // 무지개 수정 🌈 — 원래 비 오는 날 증기 제조 부산물(S11). 지금은 "물방울+열기=무지개" 정수 합성으로 대체. 800G, 40초
            CreateMaterial("Material_RainbowCrystal", "material_rainbow_crystal", "무지개 수정", C(0, 0, 0, 0), "🌈", 800, 3,
                "Material_WaterEssence", 1, 40f,
                Extra("Material_FireEssence", 1));
        }

        // ---- 포션 30종 + 부산물 3종 (기획서 6장 — BrewFixture와 1:1) ----

        static void CreatePotions()
        {
            // 단일 원소 (4종) — 입문
            CreatePotion("Potion_MinorFlame", "potion_minor_flame", "작은 화염 포션", C(3, 0, 0, 0), 50, PotionCategory.Attack);
            CreatePotion("Potion_MinorHeal", "potion_minor_heal", "작은 치유 포션", C(0, 3, 0, 0), 50, PotionCategory.Recovery);
            CreatePotion("Potion_MinorGuard", "potion_minor_guard", "작은 견고함 포션", C(0, 0, 3, 0), 50, PotionCategory.Defense);
            CreatePotion("Potion_MinorHaste", "potion_minor_haste", "작은 신속 포션", C(0, 0, 0, 3), 50, PotionCategory.Support);

            // 2원소 (6종) — 중급
            CreatePotion("Potion_Steam", "potion_steam", "증기 포션", C(3, 3, 0, 0), 400, PotionCategory.Special);
            CreatePotion("Potion_Lava", "potion_lava", "용암 포션", C(3, 0, 3, 0), 400, PotionCategory.Special);
            CreatePotion("Potion_Storm", "potion_storm", "폭풍 포션", C(3, 0, 0, 3), 400, PotionCategory.Special);
            CreatePotion("Potion_Herb", "potion_herb", "약초 포션", C(0, 3, 3, 0), 400, PotionCategory.Special);
            CreatePotion("Potion_Raincloud", "potion_raincloud", "비구름 포션", C(0, 3, 0, 3), 400, PotionCategory.Special);
            CreatePotion("Potion_Sandstorm", "potion_sandstorm", "모래폭풍 포션", C(0, 0, 3, 3), 400, PotionCategory.Special);

            // 3원소 (4종) — 고급
            CreatePotion("Potion_Polymorph", "potion_polymorph", "변신 약", C(2, 2, 2, 0), 3200, PotionCategory.Special);
            CreatePotion("Potion_Flight", "potion_flight", "비행 포션", C(0, 2, 2, 2), 3200, PotionCategory.Special);
            CreatePotion("Potion_Invisibility", "potion_invisibility", "투명화 포션", C(2, 0, 2, 2), 3200, PotionCategory.Special);
            CreatePotion("Potion_Spirit", "potion_spirit", "영혼 포션", C(2, 2, 0, 2), 3200, PotionCategory.Special);

            // 전설 4원소 (1종) — 별빛 분말 지정
            CreatePotion("Potion_SagesElixir", "potion_sages_elixir", "현자의 엘릭서", C(3, 3, 3, 3), 50000,
                PotionCategory.Special, "🌟", Req("Material_StarlightPowder", 1));

            // 비대칭 (6종)
            CreatePotion("Potion_Geyser", "potion_geyser", "간헐천 포션", C(4, 2, 0, 0), 600, PotionCategory.Attack);
            CreatePotion("Potion_Downpour", "potion_downpour", "폭우 포션", C(0, 4, 0, 2), 650, PotionCategory.Recovery);
            CreatePotion("Potion_Obsidian", "potion_obsidian", "흑요석 포션", C(2, 0, 4, 0), 700, PotionCategory.Defense);
            CreatePotion("Potion_Lightning", "potion_lightning", "번개 포션", C(2, 0, 0, 4), 550, PotionCategory.Support);
            CreatePotion("Potion_DragonBlood", "potion_dragon_blood", "용의 피 포션", C(4, 1, 1, 0), 3000, PotionCategory.Special);
            CreatePotion("Potion_Bloom", "potion_bloom", "개화 포션", C(0, 1, 3, 2), 3600, PotionCategory.Special);

            // 재료 지정 (3종)
            CreatePotion("Potion_DragonBreath", "potion_dragon_breath", "용의 숨결 포션", C(4, 0, 0, 2), 900,
                PotionCategory.Special, "🧪", Req("Plant_DragonBreathHerb", 2));
            CreatePotion("Potion_MermaidSong", "potion_mermaid_song", "인어의 노래 포션", C(0, 4, 2, 0), 900,
                PotionCategory.Special, "🧪", Req("Plant_MermaidHair", 2));
            CreatePotion("Potion_WorldTreeSap", "potion_world_tree_sap", "세계수의 수액", C(0, 2, 5, 0), 12000,
                PotionCategory.Special, "🧪", Req("Plant_WorldTreeSapling", 1));

            // 조건부/숨김 (3종) — 조건 태그. 실제 개방은 S11 (야간만 지금 동작)
            CreatePotion("Potion_Moonlight", "potion_moonlight", "달빛 포션", C(0, 2, 0, 2), 600,
                PotionCategory.Special, "🌛", null, Tags("night_only"));
            CreatePotion("Potion_ElixirOfLife", "potion_elixir_of_life", "생명수 포션", C(0, 3, 3, 0), 1200,
                PotionCategory.Special, "🧪", null, Tags("weather:rain"));
            CreatePotion("Potion_BlackSun", "potion_black_sun", "검은 태양의 비약", C(2, 2, 2, 2), 20000,
                PotionCategory.Special, "🌑", null, Tags("weather:eclipse"));

            // 전용 (3종)
            CreatePotion("Potion_HeartsBrew", "potion_hearts_brew", "마음의 묘약", C(1, 1, 1, 1), 0,
                PotionCategory.Special, "💖", Req("Material_RainbowCrystal", 1), Tags("night_only"));
            CreatePotion("Potion_Guardian", "potion_guardian", "수호의 포션", C(0, 1, 2, 0), 150, PotionCategory.Defense);
            CreatePotion("Potion_Luck", "potion_luck", "행운 포션", C(0, 0, 1, 2), 120, PotionCategory.Support);

            // 실패 부산물 3종 (실험 일지) — 조성 0, id는 BrewRecipeFactory 상수와 일치
            CreatePotion("Potion_Murky", "potion_murky", "탁한 포션", C(0, 0, 0, 0), 5, PotionCategory.Special, "🫗");
            CreatePotion("Potion_Sediment", "potion_sediment", "수상한 침전물", C(0, 0, 0, 0), 15, PotionCategory.Special, "🧫");
            CreatePotion("Potion_Mist", "potion_mist", "희뿌연 안개병", C(0, 0, 0, 0), 12, PotionCategory.Special, "🌫️");
        }

        // ---- 생성 헬퍼 ----

        static ElementComposition C(int fire, int water, int earth, int wind, int star = 0)
            => new ElementComposition(fire, water, earth, wind, star);

        static void CreatePlant(string assetName, string id, string displayName,
            ElementComposition composition, string displayEmoji, int tier, int unlockCost)
        {
            string path = $"{PlantFolder}/{assetName}.asset";
            var plant = LoadOrCreate<PlantData>(path);
            plant.id = id;
            plant.displayName = displayName;
            plant.composition = composition;
            plant.tier = tier;
            plant.growthSeconds = GrowthByTier[tier];
            plant.baseValue = ValueByTier[tier];
            plant.unlockCost = unlockCost;
            plant.displayEmoji = displayEmoji;
            EditorUtility.SetDirty(plant);
        }

        // (assetName, count) 추가 원료 지정용
        static (string, int) Extra(string assetName, int count) => (assetName, count);

        static void CreateMaterial(string assetName, string id, string displayName,
            ElementComposition composition, string displayEmoji, int baseValue, int stage,
            string sourceAssetName, int sourceCount, float processingSeconds,
            params (string assetName, int count)[] extras)
        {
            string path = $"{MaterialFolder}/{assetName}.asset";
            var material = LoadOrCreate<MaterialData>(path);
            material.id = id;
            material.displayName = displayName;
            material.composition = composition;
            material.baseValue = baseValue;
            material.displayEmoji = displayEmoji;
            material.processingStage = stage;
            material.sourceItem = LoadItem(sourceAssetName);
            material.sourceCount = sourceCount;
            material.processingSeconds = processingSeconds;
            material.extraInputs = new List<IngredientRequirement>();
            if (material.sourceItem == null)
                Debug.LogWarning($"[S07] 가공 주 원료 없음: {sourceAssetName} — {assetName}의 sourceItem 비어 있음");
            if (extras != null)
            {
                foreach ((string extraAssetName, int count) in extras)
                {
                    ItemData extraItem = LoadItem(extraAssetName);
                    if (extraItem == null)
                    {
                        Debug.LogWarning($"[S07] 추가 원료 없음: {extraAssetName} — {assetName}");
                        continue;
                    }
                    material.extraInputs.Add(new IngredientRequirement { item = extraItem, count = count });
                }
            }
            EditorUtility.SetDirty(material);
        }

        // (assetName, count) 지정 재료용
        static (string, int)[] Req(string assetName, int count) => new[] { (assetName, count) };

        static List<string> Tags(params string[] tags) => new List<string>(tags);

        static void CreatePotion(string assetName, string id, string displayName,
            ElementComposition composition, int sellPrice, PotionCategory category, string displayEmoji = "🧪",
            (string assetName, int count)[] requiredIngredients = null, List<string> conditionTags = null)
        {
            string path = $"{PotionFolder}/{assetName}.asset";
            var potion = LoadOrCreate<PotionData>(path);
            potion.id = id;
            potion.displayName = displayName;
            potion.composition = composition;
            potion.baseValue = sellPrice;
            potion.category = category;
            potion.displayEmoji = displayEmoji;
            potion.equipEffectId = string.Empty;

            potion.requiredIngredients.Clear();
            if (requiredIngredients != null)
            {
                foreach ((string reqAssetName, int count) in requiredIngredients)
                {
                    ItemData item = LoadItem(reqAssetName);
                    if (item == null)
                    {
                        Debug.LogWarning($"[S07] 지정 재료 없음: {reqAssetName} — {assetName}");
                        continue;
                    }
                    potion.requiredIngredients.Add(new IngredientRequirement { item = item, count = count });
                }
            }

            potion.conditionTags.Clear();
            if (conditionTags != null)
                potion.conditionTags.AddRange(conditionTags);

            EditorUtility.SetDirty(potion);
        }

        // 에셋 이름 접두어로 폴더를 판별해 SO 로드 (Plant_ → 식물, Material_ → 재료)
        static ItemData LoadItem(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                return null;
            string folder = assetName.StartsWith("Plant_") ? PlantFolder
                : assetName.StartsWith("Material_") ? MaterialFolder
                : PotionFolder;
            return AssetDatabase.LoadAssetAtPath<ItemData>($"{folder}/{assetName}.asset");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                int slash = path.LastIndexOf('/');
                AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
            }
        }
    }
}
