using System.Collections.Generic;

namespace WizardGarden.Core
{
    /// <summary>
    /// 보유 견습생 + 구역별 배치 슬롯 (순수 C#, S09). 초반 각 구역 1슬롯(총 3) — 보유 &gt; 슬롯 설계로
    /// "누구를 어디에 배치할지"가 실제 선택이 된다 (기획서 13장). 슬롯 확장은 이후 세션.
    /// 배치는 직군이 맞는 슬롯에만 가능(정원사→정원 등). 세이브 대상.
    /// </summary>
    public sealed class ApprenticeRoster
    {
        /// <summary>구역(직군)별 초기 배치 슬롯 수 (기획서 13장 "초반: 각 구역 1슬롯").</summary>
        public const int InitialSlotsPerJob = 1;

        readonly List<ApprenticeState> _all = new List<ApprenticeState>();
        readonly Dictionary<Job, int> _slots = new Dictionary<Job, int>
        {
            { Job.Gardener, InitialSlotsPerJob },
            { Job.Alchemist, InitialSlotsPerJob },
            { Job.Clerk, InitialSlotsPerJob }
        };

        public IReadOnlyList<ApprenticeState> All => _all;

        /// <summary>해당 직군 배치 슬롯 수.</summary>
        public int SlotsFor(Job job) => _slots.TryGetValue(job, out int n) ? n : 0;

        /// <summary>해당 직군에 현재 배치된 견습생 수.</summary>
        public int PlacedCountFor(Job job)
        {
            int count = 0;
            foreach (ApprenticeState a in _all)
                if (a.IsPlaced && a.Job == job)
                    count++;
            return count;
        }

        /// <summary>배치된 견습생 총 수.</summary>
        public int PlacedCount
        {
            get
            {
                int count = 0;
                foreach (ApprenticeState a in _all)
                    if (a.IsPlaced)
                        count++;
                return count;
            }
        }

        public bool Contains(string id) => Get(id) != null;

        public ApprenticeState Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            foreach (ApprenticeState a in _all)
                if (a.Id == id)
                    return a;
            return null;
        }

        /// <summary>없으면 추가 (영입/시딩). 이미 있으면 무시하고 기존 상태 유지.</summary>
        public ApprenticeState AddIfMissing(ApprenticeState apprentice)
        {
            if (apprentice == null || string.IsNullOrEmpty(apprentice.Id))
                return null;
            ApprenticeState existing = Get(apprentice.Id);
            if (existing != null)
                return existing;
            _all.Add(apprentice);
            return apprentice;
        }

        /// <summary>이 견습생을 지금 배치할 수 있는가 (미배치 + 직군 슬롯 여유).</summary>
        public bool CanPlace(ApprenticeState apprentice)
        {
            return apprentice != null
                && !apprentice.IsPlaced
                && PlacedCountFor(apprentice.Job) < SlotsFor(apprentice.Job);
        }

        /// <summary>배치 — 직군 슬롯이 남아 있어야 성공.</summary>
        public bool TryPlace(string id)
        {
            ApprenticeState a = Get(id);
            if (!CanPlace(a))
                return false;
            a.IsPlaced = true;
            return true;
        }

        /// <summary>배치 해제.</summary>
        public bool Unplace(string id)
        {
            ApprenticeState a = Get(id);
            if (a == null || !a.IsPlaced)
                return false;
            a.IsPlaced = false;
            return true;
        }

        /// <summary>배치된 견습생 나열 (오프라인 정산·유닛 스폰용).</summary>
        public IEnumerable<ApprenticeState> Placed
        {
            get
            {
                foreach (ApprenticeState a in _all)
                    if (a.IsPlaced)
                        yield return a;
            }
        }

        // ---- 세이브 ----

        /// <summary>세이브 데이터에서 복원 — 저장된 스탯·레벨·배치를 그대로 되돌린다(슬롯 초과 배치는 방어).</summary>
        public void RestoreFrom(SaveData data)
        {
            _all.Clear();
            if (data?.apprentices == null)
                return;

            foreach (SaveData.ApprenticeEntry entry in data.apprentices)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || Contains(entry.id))
                    continue;

                var state = new ApprenticeState(
                    entry.id, (Job)entry.job, (Rarity)entry.rarity,
                    entry.affinity, entry.magic, entry.trade, entry.luck,
                    entry.level, entry.experience);

                // 슬롯 초과 배치 상태는 방어적으로 무시(데이터 이상치 → 미배치).
                if (entry.placed && PlacedCountFor(state.Job) < SlotsFor(state.Job))
                    state.RestorePlacement(true);
                _all.Add(state);
            }
        }

        /// <summary>세이브 데이터에 기록.</summary>
        public void WriteTo(SaveData data)
        {
            data.apprentices = new List<SaveData.ApprenticeEntry>(_all.Count);
            foreach (ApprenticeState a in _all)
            {
                data.apprentices.Add(new SaveData.ApprenticeEntry
                {
                    id = a.Id,
                    job = (int)a.Job,
                    rarity = (int)a.Rarity,
                    affinity = a.Affinity,
                    magic = a.Magic,
                    trade = a.Trade,
                    luck = a.Luck,
                    level = a.Level,
                    experience = a.Experience,
                    placed = a.IsPlaced
                });
            }
        }
    }
}
