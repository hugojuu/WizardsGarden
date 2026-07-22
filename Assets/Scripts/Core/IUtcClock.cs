using System;

namespace WizardGarden.Core
{
    /// <summary>현재 UTC 시각 공급자 — 오프라인 경과 계산에 주입 (테스트에서 가짜 시계 대체).</summary>
    public interface IUtcClock
    {
        DateTime UtcNow { get; }
    }

    /// <summary>실제 시스템 시계.</summary>
    public sealed class SystemUtcClock : IUtcClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
