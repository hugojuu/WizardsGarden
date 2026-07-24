using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>견습생이 수행할 작업 종류 (직군별 행동 결정 결과).</summary>
    public enum WorkKind
    {
        None = 0,
        Harvest = 1,       // 정원사: 다 자란 밭 수확
        Plant = 2,         // 정원사: 빈 밭에 재파종
        StartProcess = 3,  // 연금술사: 작업대 가공 시작
        Collect = 4,       // 연금술사: 완료된 가공물 수령
        Display = 5        // 점원: 창고 재고 → 진열대
    }

    /// <summary>
    /// 견습생 작업 지시 (순수 데이터). 어댑터가 이 지시의 대상 인덱스를 월드 좌표로 바꿔 유닛을 보내고,
    /// 유닛이 작업을 마치는 순간 이 지시대로 실제 게임 효과(수확·가공·진열)를 실행한다.
    /// </summary>
    public readonly struct WorkOrder
    {
        public readonly WorkKind Kind;
        public readonly int Index;      // 밭 슬롯 / 진열대 인덱스
        public readonly string ItemId;  // 심을 종자 / 가공 출력물 / 진열할 아이템
        public readonly int Count;       // 진열 수량 등
        public readonly string InputId;  // 가공 주 원료
        public readonly int InputCount;  // 가공 주 원료 수량

        WorkOrder(WorkKind kind, int index, string itemId, int count, string inputId, int inputCount)
        {
            Kind = kind;
            Index = index;
            ItemId = itemId;
            Count = count;
            InputId = inputId;
            InputCount = inputCount;
        }

        public bool HasWork => Kind != WorkKind.None;

        public static readonly WorkOrder None = new WorkOrder(WorkKind.None, -1, null, 0, null, 0);

        public static WorkOrder Harvest(int slotIndex) =>
            new WorkOrder(WorkKind.Harvest, slotIndex, null, 0, null, 0);

        public static WorkOrder Plant(int slotIndex, string seedId) =>
            new WorkOrder(WorkKind.Plant, slotIndex, seedId, 0, null, 0);

        public static WorkOrder StartProcess(string outputId, string inputId, int inputCount) =>
            new WorkOrder(WorkKind.StartProcess, 0, outputId, 1, inputId, inputCount);

        public static WorkOrder Collect() =>
            new WorkOrder(WorkKind.Collect, 0, null, 0, null, 0);

        public static WorkOrder Display(int standIndex, string itemId, int count) =>
            new WorkOrder(WorkKind.Display, standIndex, itemId, count, null, 0);
    }

    /// <summary>연금술사 자동 가공용 단순 레시피 (주 원료 1종만 — 다중 입력 가공은 수동 유지). 어댑터가 SO에서 구성.</summary>
    public readonly struct SimpleRecipe
    {
        public readonly string OutputId;
        public readonly string InputId;
        public readonly int InputCount;

        public SimpleRecipe(string outputId, string inputId, int inputCount)
        {
            OutputId = outputId;
            InputId = inputId;
            InputCount = inputCount;
        }
    }

    /// <summary>
    /// 직군별 행동 결정 (순수 C#, S09). 세계 상태(Garden·Workshop·Shop·Inventory)를 읽어
    /// "다음에 할 작업"을 고른다 — 실제 코어 객체로 그대로 테스트 가능. 대상 좌표·게임 효과 실행은 어댑터 몫.
    /// 배치된 구역에서만 동작(어댑터가 직군에 맞는 브레인만 호출).
    /// </summary>
    public static class ApprenticeJobLogic
    {
        /// <summary>점원이 1회에 진열대로 옮기는 수량 (어댑터 DisplayMoveCount와 동일 기준).</summary>
        public const int DisplayBatch = 10;

        /// <summary>
        /// 정원사: 다 자란 밭을 우선 수확, 없으면 빈 밭에 재파종(preferredSeedId), 둘 다 없으면 None.
        /// </summary>
        public static WorkOrder DecideGardener(Garden garden, double now,
            Func<string, double> growthSecondsOf, string preferredSeedId)
        {
            if (garden == null || growthSecondsOf == null)
                return WorkOrder.None;

            // 1) 수확 우선 — 앞 슬롯부터 다 자란 밭.
            for (int i = 0; i < garden.SlotCount; i++)
            {
                GardenSlot slot = garden.Slots[i];
                if (!slot.IsEmpty && slot.IsMature(now, growthSecondsOf(slot.PlantId)))
                    return WorkOrder.Harvest(i);
            }

            // 2) 재파종 — 빈 밭이 있고 심을 종자가 지정되어 있으면.
            if (!string.IsNullOrEmpty(preferredSeedId))
            {
                for (int i = 0; i < garden.SlotCount; i++)
                {
                    if (garden.Slots[i].IsEmpty)
                        return WorkOrder.Plant(i, preferredSeedId);
                }
            }

            return WorkOrder.None;
        }

        /// <summary>
        /// 연금술사: 작업대가 완료 상태면 수령, 대기 상태면 원료가 있는 레시피를 시작, 진행 중이면 None(기다림).
        /// </summary>
        public static WorkOrder DecideAlchemist(Workshop workshop, Inventory inventory, double now,
            Func<string, double> processingSecondsOf, IReadOnlyList<SimpleRecipe> recipes)
        {
            if (workshop == null)
                return WorkOrder.None;

            if (!workshop.IsIdle)
            {
                double processing = processingSecondsOf != null ? processingSecondsOf(workshop.OutputItemId) : 0.0;
                return workshop.IsComplete(now, processing) ? WorkOrder.Collect() : WorkOrder.None;
            }

            if (inventory == null || recipes == null)
                return WorkOrder.None;

            for (int i = 0; i < recipes.Count; i++)
            {
                SimpleRecipe recipe = recipes[i];
                if (string.IsNullOrEmpty(recipe.OutputId) || string.IsNullOrEmpty(recipe.InputId) || recipe.InputCount <= 0)
                    continue;
                if (inventory.GetCount(recipe.InputId) >= recipe.InputCount)
                    return WorkOrder.StartProcess(recipe.OutputId, recipe.InputId, recipe.InputCount);
            }

            return WorkOrder.None;
        }

        /// <summary>
        /// 점원: 빈 진열대가 있고 창고에 값이 매겨진 재고가 있으면 진열, 없으면 None.
        /// 이미 진열된 것과 같은 아이템이 우선(합산 진열 방지 없이 앞 빈 칸에).
        /// </summary>
        public static WorkOrder DecideClerk(Shop shop, Inventory inventory, Func<string, int> priceOf)
        {
            if (shop == null || inventory == null || priceOf == null)
                return WorkOrder.None;

            int emptyStand = -1;
            for (int i = 0; i < shop.Slots.Count; i++)
            {
                if (shop.Slots[i].IsEmpty)
                {
                    emptyStand = i;
                    break;
                }
            }
            if (emptyStand < 0)
                return WorkOrder.None;

            foreach (KeyValuePair<string, int> entry in inventory.Entries)
            {
                if (entry.Value <= 0)
                    continue;
                if (priceOf(entry.Key) <= 0)
                    continue; // 값을 매길 수 없는 품목(부산물 원가 0 등)은 진열 안 함
                int count = Math.Min(DisplayBatch, entry.Value);
                return WorkOrder.Display(emptyStand, entry.Key, count);
            }

            return WorkOrder.None;
        }
    }
}
