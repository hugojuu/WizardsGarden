using System;
using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>오프라인 정산 결과 요약 (복귀 패널·테스트용).</summary>
    public sealed class OfflineSettlementResult
    {
        /// <summary>실제 오프라인 경과 초 (캡·효율 미적용 raw).</summary>
        public double RawOfflineSeconds;

        /// <summary>자원 트랙에 반영된 유효 초 (min(raw, 캡) × 효율).</summary>
        public double EffectiveResourceSeconds;

        /// <summary>8시간 캡에 걸렸는가 (raw가 캡 초과).</summary>
        public bool WasCapped;

        /// <summary>정산으로 벌어들인 총 골드 (진열 재고 판매 + 수확분 자동 판매, 도감 보너스 반영).</summary>
        public long GoldEarned;

        /// <summary>정원 자동 수확 총 개수.</summary>
        public int TotalHarvested;

        /// <summary>수확분 중 보관함에 남은 개수 (자동 판매되지 않은 몫).</summary>
        public int HarvestedToStorage;

        /// <summary>자동 판매된 총 개수 (기존 진열 재고 + 수확분).</summary>
        public int SoldCount;

        /// <summary>보여줄 만한 변화가 있었는가 (없으면 복귀 패널 생략).</summary>
        public bool HasActivity =>
            EffectiveResourceSeconds > 0.0 && (GoldEarned > 0 || TotalHarvested > 0 || SoldCount > 0);
    }

    /// <summary>
    /// 오프라인 정산 (순수 C#, S08 — MonoBehaviour 비의존). 자원 시간만 진행시킨다:
    /// 유효 초 = min(raw, 8시간 캡) × 효율 60% → GameClock.AddResourceSeconds로 반영(식물 자동 성장).
    /// 자동 수확·판매는 임시 자동화 상수(IOfflineAutomation)로 계산 — ★ S09 견습생이 교체할 자리.
    /// 사건 시간(EventSeconds)은 건드리지 않는다 → 계절·날씨·VIP·모험은 오프라인에 정지(기획서 22장).
    /// </summary>
    public sealed class OfflineSettlement
    {
        /// <summary>오프라인 자원 누적 상한 — 8시간(기획서 22장).</summary>
        public const double DefaultCapSeconds = 28800.0;

        /// <summary>오프라인 효율 — 60%(기획서 22장).</summary>
        public const double DefaultEfficiency = 0.6;

        readonly IOfflineAutomation _automation;
        readonly double _capSeconds;
        readonly double _efficiency;
        readonly List<Shop.SaleRecord> _shopSales = new List<Shop.SaleRecord>();

        public OfflineSettlement(IOfflineAutomation automation,
            double capSeconds = DefaultCapSeconds, double efficiency = DefaultEfficiency)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _capSeconds = capSeconds > 0.0 ? capSeconds : DefaultCapSeconds;
            _efficiency = efficiency > 0.0 ? efficiency : DefaultEfficiency;
        }

        /// <summary>자원 트랙에 반영할 유효 초 = min(raw, 캡) × 효율 (음수는 0).</summary>
        public double EffectiveResourceSeconds(double rawSeconds)
        {
            double raw = rawSeconds > 0.0 ? rawSeconds : 0.0;
            double capped = raw < _capSeconds ? raw : _capSeconds;
            return capped * _efficiency;
        }

        /// <summary>raw가 캡을 초과했는가 (복귀 패널의 "8시간까지만 정산됨" 안내용).</summary>
        public bool WasCapped(double rawSeconds) => rawSeconds > _capSeconds;

        /// <summary>
        /// 정산 실행 — 자원초 반영·자동 수확·자동 판매를 처리하고 요약을 돌려준다.
        /// 상태(clock/garden/inventory/shop/wallet)를 직접 변경한다. growthSecondsOf·priceOf는 SO 조회(어댑터 주입).
        /// goldModifier: 판매 골드 훅(도감 완성도 보너스 등, null이면 원가).
        /// </summary>
        public OfflineSettlementResult Settle(
            GameClock clock, Garden garden, Inventory inventory, Shop shop, Wallet wallet,
            double rawOfflineSeconds,
            Func<string, double> growthSecondsOf,
            Func<string, int> priceOf,
            Func<long, long> goldModifier = null)
        {
            var result = new OfflineSettlementResult
            {
                RawOfflineSeconds = rawOfflineSeconds > 0.0 ? rawOfflineSeconds : 0.0
            };
            if (clock == null)
                return result;

            double effective = EffectiveResourceSeconds(rawOfflineSeconds);
            result.EffectiveResourceSeconds = effective;
            result.WasCapped = WasCapped(rawOfflineSeconds);
            if (effective <= 0.0)
                return result;

            // 자원 시간만 진행 — 식물은 심은 시점 파생 계산이라 이 한 줄로 자동 성장(S03 방침).
            clock.AddResourceSeconds(effective);
            double now = clock.ResourceSeconds;

            long gold = 0;

            // 채널 A: 이미 진열된 재고를 손님이 구매 (실제 메커니즘 — 견습생 없이도 동작).
            //   TickCustomers가 지갑에 직접 적립하므로 여기선 요약 집계만.
            if (shop != null && wallet != null && priceOf != null)
            {
                _shopSales.Clear();
                shop.TickCustomers(now, priceOf, wallet, _shopSales, goldModifier);
                foreach (Shop.SaleRecord sale in _shopSales)
                {
                    gold += sale.Gold;
                    result.SoldCount += sale.Count;
                }
            }

            // 채널 B: 정원 자동 수확(→ 보관함) + 수확분 자동 판매(→ 골드). ★ 임시 자동화 상수 사용.
            var harvested = new SortedDictionary<string, int>(StringComparer.Ordinal);
            if (garden != null && inventory != null && growthSecondsOf != null && _automation.GardenHarvestRate > 0.0)
            {
                foreach (GardenSlot slot in garden.Slots)
                {
                    if (slot.IsEmpty)
                        continue;
                    string plantId = slot.PlantId;
                    double growth = growthSecondsOf(plantId);
                    if (growth <= 0.0)
                        continue; // 성장초 불명·즉시작물은 무한 반복 방지로 건너뜀

                    int cycles = (int)Math.Floor(effective * _automation.GardenHarvestRate / growth);
                    if (cycles <= 0)
                        continue; // 아직 한 번도 못 여문 슬롯 — 그대로 두고 성장만

                    inventory.Add(plantId, cycles);
                    harvested.TryGetValue(plantId, out int prev);
                    harvested[plantId] = prev + cycles;
                    result.TotalHarvested += cycles;

                    // 복귀한 플레이어가 이어서 키우도록 같은 작물로 새로 심어 둔다(재파종).
                    slot.Clear();
                    slot.TryPlant(plantId, now);
                }
            }

            // 수확분 자동 판매 (점원 stand-in) — 판매 상한 = floor(유효초 × ShopSalesRate).
            long goldB = 0;
            int soldB = 0;
            if (harvested.Count > 0 && inventory != null && priceOf != null && _automation.ShopSalesRate > 0.0)
            {
                int capacity = (int)Math.Floor(effective * _automation.ShopSalesRate);
                foreach (KeyValuePair<string, int> pair in harvested)
                {
                    if (capacity <= 0)
                        break;
                    int price = priceOf(pair.Key);
                    if (price <= 0)
                        continue;
                    int sellable = Math.Min(Math.Min(pair.Value, capacity), inventory.GetCount(pair.Key));
                    if (sellable <= 0)
                        continue;
                    if (!inventory.TryRemove(pair.Key, sellable))
                        continue;

                    goldB += (long)price * sellable;
                    soldB += sellable;
                    capacity -= sellable;
                }

                if (goldModifier != null && goldB > 0)
                {
                    long modified = goldModifier(goldB);
                    if (modified >= 0)
                        goldB = modified;
                }
                if (wallet != null && goldB > 0)
                    wallet.Add(goldB);
            }

            result.SoldCount += soldB;
            result.GoldEarned = gold + goldB;
            result.HarvestedToStorage = result.TotalHarvested - soldB;
            if (result.HarvestedToStorage < 0)
                result.HarvestedToStorage = 0;
            return result;
        }
    }
}
