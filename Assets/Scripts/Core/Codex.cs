using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 포션 도감 — 발견 상태(영구 진행도, 기획서 7장) + 완성도 집계 + 글로벌 골드 보너스.
    /// 순수 C#(MonoBehaviour 비의존) — EditMode 테스트 대상. SO 접점은 어댑터가 담당한다.
    ///
    /// 발견 대상 우주(분모)는 두 부류로 등록된다: 포션 레시피 + 실패 부산물 3종(실험 일지).
    /// 등록은 어댑터가 SO 테이블에서 넣어주므로(RegisterPotion/RegisterByproduct), 데이터가
    /// 늘면 분모도 자동으로 늘어난다(S07이 33종을 다 만들면 완성율이 그에 맞게 재계산).
    /// 발견 id 집합은 세이브에 그대로 보존되어 등록 우주와 교집합으로 완성 수를 센다.
    /// </summary>
    public sealed class Codex
    {
        readonly HashSet<string> _potionUniverse = new HashSet<string>(System.StringComparer.Ordinal);
        readonly HashSet<string> _byproductUniverse = new HashSet<string>(System.StringComparer.Ordinal);

        // 발견 id — 세이브 순서를 결정적으로 유지(SortedSet).
        readonly SortedSet<string> _discovered = new SortedSet<string>(System.StringComparer.Ordinal);

        /// <summary>발견 상태 변경 알림(UI 갱신용).</summary>
        public event System.Action Changed;

        // ---- 우주 등록 (어댑터가 SO 테이블에서) ----

        /// <summary>포션 레시피 id를 완성도 분모에 등록.</summary>
        public void RegisterPotion(string potionId)
        {
            if (!string.IsNullOrEmpty(potionId))
                _potionUniverse.Add(potionId);
        }

        /// <summary>실패 부산물(실험 일지) id를 완성도 분모에 등록.</summary>
        public void RegisterByproduct(string byproductId)
        {
            if (!string.IsNullOrEmpty(byproductId))
                _byproductUniverse.Add(byproductId);
        }

        // ---- 조회 ----

        public int PotionTotal => _potionUniverse.Count;
        public int ByproductTotal => _byproductUniverse.Count;
        public int TotalEntries => PotionTotal + ByproductTotal;

        public int PotionDiscoveredCount => CountIn(_potionUniverse);
        public int ByproductDiscoveredCount => CountIn(_byproductUniverse);
        public int DiscoveredCount => PotionDiscoveredCount + ByproductDiscoveredCount;

        /// <summary>완성율(0~1) — 발견(∩등록 우주) / 전체 등록 수. 등록이 없으면 0.</summary>
        public double CompletionRatio => TotalEntries == 0 ? 0.0 : (double)DiscoveredCount / TotalEntries;

        /// <summary>완성도 골드 보너스 추가분(0/0.05/0.15/0.30/0.50).</summary>
        public double GoldBonusFraction => CodexBonus.GoldBonusFraction(CompletionRatio);

        public bool IsDiscovered(string id) => !string.IsNullOrEmpty(id) && _discovered.Contains(id);
        public bool IsRegisteredPotion(string id) => _potionUniverse.Contains(id);
        public bool IsRegisteredByproduct(string id) => _byproductUniverse.Contains(id);

        /// <summary>판매 골드에 완성도 보너스를 적용(곱연산·내림).</summary>
        public long ApplyGoldBonus(long baseGold) => CodexBonus.ApplyBonus(baseGold, CompletionRatio);

        // ---- 발견 ----

        /// <summary>발견 등록. 이미 알고 있던 id면 false(신규 발견만 true).</summary>
        public bool Discover(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            if (!_discovered.Add(id))
                return false;
            Changed?.Invoke();
            return true;
        }

        int CountIn(HashSet<string> universe)
        {
            int count = 0;
            foreach (string id in _discovered)
                if (universe.Contains(id))
                    count++;
            return count;
        }

        // ---- 세이브 ----

        /// <summary>세이브에서 발견 id 집합 복원(우주 등록과 무관 — 등록 전이라도 보존).</summary>
        public void RestoreFrom(SaveData data)
        {
            _discovered.Clear();
            if (data.discoveredCodexIds != null)
            {
                foreach (string id in data.discoveredCodexIds)
                    if (!string.IsNullOrEmpty(id))
                        _discovered.Add(id);
            }
            Changed?.Invoke();
        }

        /// <summary>발견 id 집합을 세이브에 기록(id 오름차순 — 결정적).</summary>
        public void WriteTo(SaveData data)
        {
            data.discoveredCodexIds = new List<string>(_discovered);
        }
    }
}
