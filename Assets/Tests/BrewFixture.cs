using System.Collections.Generic;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>
    /// 기획서 6장 포션 33종(30 레시피 + 실패 부산물 3종) 테스트 픽스처.
    /// 조성·판매가·지정 재료·조건 태그를 기획서 표에서 정확히 옮긴 것.
    /// 지정 재료 id(별빛 분말/용의 입김초/인어의 머리카락/세계수 묘목/무지개 수정 촉매)는
    /// 아직 식물·재료 테이블에 없어 잠정 id — S06 데이터 authoring 시 확정.
    /// </summary>
    public static class BrewFixture
    {
        // --- 지정 재료 잠정 id ---
        public const string StarlightPowder = "material_starlight_powder";      // 별빛 분말
        public const string DragonBreathHerb = "plant_dragon_breath_herb";      // 용의 입김초
        public const string MermaidHair = "material_mermaid_hair";              // 인어의 머리카락
        public const string WorldTreeSapling = "plant_world_tree_sapling";      // 세계수 묘목
        public const string RainbowCrystal = "material_rainbow_crystal";        // 무지개 수정 촉매

        static ElementComposition C(int fire, int water, int earth, int wind, int star = 0)
            => new ElementComposition(fire, water, earth, wind, star);

        static IReadOnlyList<IngredientRequirementSpec> Req(string id, int n)
            => new List<IngredientRequirementSpec> { new IngredientRequirementSpec(id, n) };

        static IReadOnlyList<string> Tags(params string[] t) => new List<string>(t);

        public static List<BrewRecipe> Recipes()
        {
            return new List<BrewRecipe>
            {
                // 단일 원소 (4) — 입문
                new BrewRecipe("potion_minor_flame", "작은 화염 포션", C(3,0,0,0), 50, PotionCategory.Attack),
                new BrewRecipe("potion_minor_heal", "작은 치유 포션", C(0,3,0,0), 50, PotionCategory.Recovery),
                new BrewRecipe("potion_minor_guard", "작은 견고함 포션", C(0,0,3,0), 50, PotionCategory.Defense),
                new BrewRecipe("potion_minor_haste", "작은 신속 포션", C(0,0,0,3), 50, PotionCategory.Support),

                // 2원소 (6) — 중급 (기획서 표에 카테고리 미명시 → Special 잠정)
                new BrewRecipe("potion_steam", "증기 포션", C(3,3,0,0), 400),
                new BrewRecipe("potion_lava", "용암 포션", C(3,0,3,0), 400),
                new BrewRecipe("potion_storm", "폭풍 포션", C(3,0,0,3), 400),
                new BrewRecipe("potion_herb", "약초 포션", C(0,3,3,0), 400),
                new BrewRecipe("potion_raincloud", "비구름 포션", C(0,3,0,3), 400),
                new BrewRecipe("potion_sandstorm", "모래폭풍 포션", C(0,0,3,3), 400),

                // 3원소 (4) — 고급
                new BrewRecipe("potion_polymorph", "변신 약", C(2,2,2,0), 3200),
                new BrewRecipe("potion_flight", "비행 포션", C(0,2,2,2), 3200),
                new BrewRecipe("potion_invisibility", "투명화 포션", C(2,0,2,2), 3200),
                new BrewRecipe("potion_spirit", "영혼 포션", C(2,2,0,2), 3200),

                // 전설 4원소 (1) — 별빛 분말 지정(별⭐은 예약이라 조성은 4원소, star=0)
                new BrewRecipe("potion_sages_elixir", "현자의 엘릭서", C(3,3,3,3), 50000,
                    PotionCategory.Special, Req(StarlightPowder, 1)),

                // 비대칭 (6) — 카테고리는 기획서 비고 열
                new BrewRecipe("potion_geyser", "간헐천 포션", C(4,2,0,0), 600, PotionCategory.Attack),
                new BrewRecipe("potion_downpour", "폭우 포션", C(0,4,0,2), 650, PotionCategory.Recovery),
                new BrewRecipe("potion_obsidian", "흑요석 포션", C(2,0,4,0), 700, PotionCategory.Defense),
                new BrewRecipe("potion_lightning", "번개 포션", C(2,0,0,4), 550, PotionCategory.Support),
                new BrewRecipe("potion_dragon_blood", "용의 피 포션", C(4,1,1,0), 3000, PotionCategory.Special),
                new BrewRecipe("potion_bloom", "개화 포션", C(0,1,3,2), 3600, PotionCategory.Special),

                // 재료 지정 (3)
                new BrewRecipe("potion_dragon_breath", "용의 숨결 포션", C(4,0,0,2), 900,
                    PotionCategory.Special, Req(DragonBreathHerb, 2)),
                new BrewRecipe("potion_mermaid_song", "인어의 노래 포션", C(0,4,2,0), 900,
                    PotionCategory.Special, Req(MermaidHair, 2)),
                new BrewRecipe("potion_world_tree_sap", "세계수의 수액", C(0,2,5,0), 12000,
                    PotionCategory.Special, Req(WorldTreeSapling, 1)),

                // 조건부/숨김 (3)
                new BrewRecipe("potion_moonlight", "달빛 포션", C(0,2,0,2), 600,
                    PotionCategory.Special, null, Tags("night_only")),
                new BrewRecipe("potion_elixir_of_life", "생명수 포션", C(0,3,3,0), 1200,
                    PotionCategory.Special, null, Tags("weather:rain")),
                new BrewRecipe("potion_black_sun", "검은 태양의 비약", C(2,2,2,2), 20000,
                    PotionCategory.Special, null, Tags("weather:eclipse")),

                // 전용 (3) — 마음의 묘약: 무지개 수정 촉매 지정 + 야간(비매품). 와일드카드 치환 로직은 미구현(존재 여부만 판정)
                new BrewRecipe("potion_hearts_brew", "마음의 묘약", C(1,1,1,1), 0,
                    PotionCategory.Special, Req(RainbowCrystal, 1), Tags("night_only")),
                new BrewRecipe("potion_guardian", "수호의 포션", C(0,1,2,0), 150, PotionCategory.Defense),
                new BrewRecipe("potion_luck", "행운 포션", C(0,0,1,2), 120, PotionCategory.Support),
            };
        }

        public static FailureByproductSet Byproducts()
        {
            return new FailureByproductSet(
                new BrewByproduct("potion_murky", "탁한 포션", 5),
                new BrewByproduct("potion_sediment", "수상한 침전물", 15),
                new BrewByproduct("potion_mist", "희뿌연 안개병", 12));
        }

        public static BrewMatcher Matcher() => new BrewMatcher(Recipes(), Byproducts());

        public static Dictionary<string, int> Count(string id, int n)
            => new Dictionary<string, int> { { id, n } };
    }
}
