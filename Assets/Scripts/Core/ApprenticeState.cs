namespace WizardGarden.Core
{
    /// <summary>
    /// 견습생 런타임 상태 (순수 C#, S09 — MonoBehaviour 비의존). SO(ApprenticeData)에서 기본값을 받아
    /// 구성하되, 이후 레벨·경험치·스탯 성장·배치는 여기서 관리한다 (SO는 불변 기본값, 상태는 세이브 대상).
    /// 스탯은 "기본 + 성장분"을 합산한 현재값을 직접 보관 — 세이브에 현재값을 남겨 SO 없이도 복원된다.
    /// </summary>
    public sealed class ApprenticeState
    {
        /// <summary>최대 레벨 (기획서 12장).</summary>
        public const int MaxLevel = 20;

        /// <summary>각성 가능 레벨 — 도달 시 희귀도 1단계 상승 (기획서 10장·12장).</summary>
        public const int AwakenLevel = 10;

        public string Id { get; }
        public Job Job { get; private set; }
        public Rarity Rarity { get; private set; }

        public int Affinity { get; private set; }
        public int Magic { get; private set; }
        public int Trade { get; private set; }
        public int Luck { get; private set; }

        public int Level { get; private set; }
        public int Experience { get; private set; }

        /// <summary>이 구역(직군)에 배치되어 실제로 일하는가. 배치 판정·슬롯 관리는 ApprenticeRoster.</summary>
        public bool IsPlaced { get; internal set; }

        public ApprenticeState(string id, Job job, Rarity rarity,
            int affinity, int magic, int trade, int luck, int level = 1, int experience = 0)
        {
            Id = id ?? "";
            Job = job;
            Rarity = rarity;
            Affinity = Clamp0(affinity);
            Magic = Clamp0(magic);
            Trade = Clamp0(trade);
            Luck = Clamp0(luck);
            Level = level < 1 ? 1 : (level > MaxLevel ? MaxLevel : level);
            Experience = experience > 0 ? experience : 0;
        }

        /// <summary>4스탯 합계 (희귀도 가이드 검증용).</summary>
        public int StatTotal => Affinity + Magic + Trade + Luck;

        /// <summary>직군 주 스탯 — 정원사=친화력 / 연금술사=마력 / 점원=상술 (기획서 10장).</summary>
        public int PrimaryStat
        {
            get
            {
                switch (Job)
                {
                    case Job.Gardener: return Affinity;
                    case Job.Alchemist: return Magic;
                    case Job.Clerk: return Trade;
                    default: return 0;
                }
            }
        }

        /// <summary>다음 레벨까지 필요한 경험치 (최대 레벨이면 0).</summary>
        public int ExperienceToNextLevel => Level >= MaxLevel ? 0 : 10 * Level;

        /// <summary>
        /// 경험치 적립 — 필요치를 넘으면 레벨업(주 스탯 +1). 오른 레벨 수를 반환한다.
        /// (기획서 12장 "레벨업 시 스탯 +1"의 결정적 구현 — 랜덤 +2·스킬 슬롯은 이후 세션.)
        /// </summary>
        public int AddExperience(int amount)
        {
            if (amount <= 0 || Level >= MaxLevel)
                return 0;

            Experience += amount;
            int gained = 0;
            while (Level < MaxLevel && Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                GainStatOnLevelUp();
                gained++;
            }
            if (Level >= MaxLevel)
                Experience = 0;
            return gained;
        }

        void GainStatOnLevelUp()
        {
            switch (Job)
            {
                case Job.Gardener: Affinity++; break;
                case Job.Alchemist: Magic++; break;
                case Job.Clerk: Trade++; break;
            }
        }

        /// <summary>각성 가능 여부 — 각성 레벨 도달 + 전설 미만.</summary>
        public bool CanAwaken => Level >= AwakenLevel && Rarity < Rarity.Legendary;

        /// <summary>각성 — 희귀도 1단계 상승 (기획서 10장 "일반도 영웅까지 키울 수 있다").</summary>
        public bool TryAwaken()
        {
            if (!CanAwaken)
                return false;
            Rarity = (Rarity)((int)Rarity + 1);
            return true;
        }

        /// <summary>세이브 복원용 — 판정 없이 배치 상태를 그대로 되돌린다 (슬롯 검증은 로스터 복원에서).</summary>
        internal void RestorePlacement(bool placed) => IsPlaced = placed;

        static int Clamp0(int value) => value > 0 ? value : 0;
    }
}
