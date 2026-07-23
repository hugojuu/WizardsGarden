using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>조합 시도의 어댑터용 결과 분류(발견 연출·소비·보상 반영 후).</summary>
    public enum BrewApplyStatus
    {
        /// <summary>포션 완성 + 신규 발견(✨ 별빛 조각 보상).</summary>
        Discovered,

        /// <summary>포션 완성 — 이미 발견된 레시피(보상 없음).</summary>
        AlreadyKnown,

        /// <summary>실패 부산물 산출(실험 일지 기록, 신규 여부는 NewlyDiscovered).</summary>
        Byproduct,

        /// <summary>조성은 맞으나 지정 재료 부족 — 재료 미소비(안내만).</summary>
        MissingIngredient,

        /// <summary>조성·재료는 맞으나 조건 불충족 — 재료 미소비(안내만).</summary>
        ConditionNotMet,

        /// <summary>투입이 비었거나 보유량 부족 — 판정 안 함.</summary>
        InvalidInput
    }

    /// <summary>조합 1회 시도 결과(엔진 판정 + 인벤토리·도감 반영 요약).</summary>
    public readonly struct BrewAttemptResult
    {
        public BrewApplyStatus Status { get; }
        public BrewResult Result { get; }

        /// <summary>산출물 id(포션/부산물) — 없으면 null.</summary>
        public string ProducedItemId { get; }
        public int ProducedCount { get; }

        /// <summary>도감에 새로 등록됐는가(포션·부산물 공통).</summary>
        public bool NewlyDiscovered { get; }

        /// <summary>지급된 별빛 조각(신규 포션 발견 시 1).</summary>
        public int StarlightAwarded { get; }

        /// <summary>재료를 실제로 소비했는가.</summary>
        public bool ConsumedInputs { get; }

        public bool IsPotionSuccess => Status == BrewApplyStatus.Discovered || Status == BrewApplyStatus.AlreadyKnown;

        internal BrewAttemptResult(BrewApplyStatus status, BrewResult result, string producedItemId,
            int producedCount, bool newlyDiscovered, int starlightAwarded, bool consumedInputs)
        {
            Status = status;
            Result = result;
            ProducedItemId = producedItemId;
            ProducedCount = producedCount;
            NewlyDiscovered = newlyDiscovered;
            StarlightAwarded = starlightAwarded;
            ConsumedInputs = consumedInputs;
        }
    }

    /// <summary>
    /// 자유 투입 조합의 어댑터용 오케스트레이터(순수 C#) — S05 BrewMatcher(판정) + Codex(발견)를 묶어
    /// 재료 소비·산출물 적재·발견 등록·별빛 보상까지 한 번에 처리한다. 매칭 로직은 재구현하지 않는다.
    ///
    /// 소비 규칙(S06 결정): 성공(포션)·실패 부산물만 재료를 소비한다. 지정 재료 부족·조건 불충족은
    /// "안내" 결과라 재료를 돌려준다(버리게 하지 않음 — S05 인계 메모대로 UI에서 결정).
    /// </summary>
    public sealed class BrewStation
    {
        /// <summary>신규 포션 발견 1건당 별빛 조각(기획서 7장).</summary>
        public const int StarlightPerDiscovery = 1;

        readonly BrewMatcher _matcher;
        readonly Codex _codex;

        public BrewStation(BrewMatcher matcher, Codex codex)
        {
            _matcher = matcher ?? throw new System.ArgumentNullException(nameof(matcher));
            _codex = codex ?? throw new System.ArgumentNullException(nameof(codex));
        }

        /// <summary>투입 묶음으로 조합 시도 — 판정 후 인벤토리·도감·보상까지 반영.</summary>
        public BrewAttemptResult Attempt(IReadOnlyList<BrewInputItem> inputs, IBrewContext context, Inventory inventory)
        {
            if (inventory == null || inputs == null || inputs.Count == 0)
                return Invalid();

            // id별 소요 집계 + 보유량 확인(부족하면 소비 없이 InvalidInput).
            var need = new Dictionary<string, int>(System.StringComparer.Ordinal);
            int totalCount = 0;
            for (int i = 0; i < inputs.Count; i++)
            {
                BrewInputItem item = inputs[i];
                if (item.Count <= 0)
                    continue;
                totalCount += item.Count;
                if (string.IsNullOrEmpty(item.ItemId))
                    continue;
                need.TryGetValue(item.ItemId, out int have);
                need[item.ItemId] = have + item.Count;
            }
            if (totalCount <= 0)
                return Invalid();
            foreach (KeyValuePair<string, int> pair in need)
                if (inventory.GetCount(pair.Key) < pair.Value)
                    return Invalid();

            BrewResult result = _matcher.Evaluate(inputs, context);

            switch (result.Outcome)
            {
                case BrewOutcome.Success:
                {
                    Consume(need, inventory);
                    string potionId = result.Recipe.PotionId;
                    inventory.Add(potionId, 1);
                    bool newly = _codex.Discover(potionId);
                    int shards = newly ? StarlightPerDiscovery : 0;
                    return new BrewAttemptResult(
                        newly ? BrewApplyStatus.Discovered : BrewApplyStatus.AlreadyKnown,
                        result, potionId, 1, newly, shards, consumedInputs: true);
                }

                case BrewOutcome.FailureByproduct:
                {
                    Consume(need, inventory);
                    string byproductId = result.Byproduct.Id;
                    inventory.Add(byproductId, 1);
                    bool newly = _codex.Discover(byproductId);
                    return new BrewAttemptResult(
                        BrewApplyStatus.Byproduct, result, byproductId, 1, newly, 0, consumedInputs: true);
                }

                case BrewOutcome.MissingIngredient:
                    return new BrewAttemptResult(
                        BrewApplyStatus.MissingIngredient, result, null, 0, false, 0, consumedInputs: false);

                case BrewOutcome.ConditionNotMet:
                    return new BrewAttemptResult(
                        BrewApplyStatus.ConditionNotMet, result, null, 0, false, 0, consumedInputs: false);

                default:
                    return Invalid();
            }
        }

        static void Consume(Dictionary<string, int> need, Inventory inventory)
        {
            foreach (KeyValuePair<string, int> pair in need)
                inventory.TryRemove(pair.Key, pair.Value);
        }

        static BrewAttemptResult Invalid() =>
            new BrewAttemptResult(BrewApplyStatus.InvalidInput, default, null, 0, false, 0, consumedInputs: false);
    }
}
