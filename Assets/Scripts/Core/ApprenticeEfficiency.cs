using System;

namespace WizardGarden.Core
{
    /// <summary>
    /// 견습생 자동화 효율·속도 공식 (순수 C#, S09). 기획서 10장 직군별 자동화 효율
    /// (정원사 60% / 연금술사 80% / 점원 100%)을 기준으로, 4스탯의 주 스탯이 효율·속도를 끌어올린다.
    /// 온라인 유닛(작업 시간·이동 속도)과 오프라인 정산(처리율) 양쪽이 이 공식을 공유한다.
    /// </summary>
    public static class ApprenticeEfficiency
    {
        /// <summary>정원사 자동화 효율 (기획서 10장 — 클릭의 60%).</summary>
        public const double GardenerBase = 0.60;

        /// <summary>연금술사 자동화 효율 (기획서 10장 — 수동의 80%).</summary>
        public const double AlchemistBase = 0.80;

        /// <summary>점원 자동화 효율 (기획서 10장 — 수동의 100%).</summary>
        public const double ClerkBase = 1.00;

        /// <summary>주 스탯 1점당 효율 가중 (스탯 총합 10~15 일반 → +40~60% 근방).</summary>
        public const double StatWeight = 0.04;

        /// <summary>직군 기본 효율.</summary>
        public static double JobBaseEfficiency(Job job)
        {
            switch (job)
            {
                case Job.Gardener: return GardenerBase;
                case Job.Alchemist: return AlchemistBase;
                case Job.Clerk: return ClerkBase;
                default: return 0.0;
            }
        }

        /// <summary>견습생 1명의 유효 효율 = 직군 기본 × (1 + 주 스탯 × 가중). 배치되지 않았으면 0.</summary>
        public static double EffectiveEfficiency(ApprenticeState apprentice)
        {
            if (apprentice == null)
                return 0.0;
            return JobBaseEfficiency(apprentice.Job) * (1.0 + apprentice.PrimaryStat * StatWeight);
        }

        /// <summary>온라인 작업 1회 소요 시간(초) = 기본 시간 / 효율 (효율 높을수록 빠름). 최소 효율 0.1로 클램프.</summary>
        public static double WorkSeconds(double baseWorkSeconds, ApprenticeState apprentice)
        {
            double eff = EffectiveEfficiency(apprentice);
            if (eff < 0.1)
                eff = 0.1;
            double seconds = baseWorkSeconds / eff;
            return seconds > 0.0 ? seconds : 0.0;
        }

        /// <summary>유닛 이동 속도 (월드 유닛/초) — 주 스탯이 조금 반영된다.</summary>
        public static double MoveSpeed(ApprenticeState apprentice)
        {
            double primary = apprentice != null ? apprentice.PrimaryStat : 0;
            return 2.2 + primary * 0.05;
        }
    }
}
