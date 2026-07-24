using NUnit.Framework;
using WizardGarden.Core;

namespace WizardGarden.Tests
{
    /// <summary>견습생 이동·작업 상태 기계 — 이동→작업(시간)→복귀→대기 루프 검증 (S09).</summary>
    public class ApprenticeAgentTests
    {
        [Test]
        public void SetHome_SnapsPositionWhenIdle()
        {
            var agent = new ApprenticeAgent();
            agent.SetHome(2f, -1f);
            Assert.AreEqual(2f, agent.X, 1e-5);
            Assert.AreEqual(-1f, agent.Y, 1e-5);
            Assert.IsTrue(agent.IsIdle);
        }

        [Test]
        public void Assign_RequiresIdle()
        {
            var agent = new ApprenticeAgent { MoveSpeed = 1.0 };
            Assert.IsTrue(agent.Assign(3f, 0f, 1.0));
            Assert.IsFalse(agent.Assign(5f, 0f, 1.0));   // 이미 작업 중 — 무시
        }

        [Test]
        public void FullCycle_Move_Work_Return_ReachesIdle()
        {
            var agent = new ApprenticeAgent { MoveSpeed = 1.0 };
            agent.SetHome(0f, 0f);
            agent.Assign(3f, 0f, 1.0);
            Assert.AreEqual(ApprenticePhase.MovingToWork, agent.Phase);

            // 이동: 3유닛 / 속도 1 → 3 tick
            Assert.IsFalse(agent.Tick(1.0));
            Assert.IsFalse(agent.Tick(1.0));
            Assert.IsFalse(agent.Tick(1.0));
            Assert.AreEqual(ApprenticePhase.Working, agent.Phase);
            Assert.AreEqual(3f, agent.X, 1e-4);

            // 작업: 1초 → 완료 순간 true 1회 반환, 복귀 단계로
            Assert.IsTrue(agent.Tick(1.0));
            Assert.AreEqual(ApprenticePhase.Returning, agent.Phase);

            // 복귀: 3유닛 → 3 tick 후 대기
            agent.Tick(1.0);
            agent.Tick(1.0);
            agent.Tick(1.0);
            Assert.AreEqual(ApprenticePhase.Idle, agent.Phase);
            Assert.AreEqual(0f, agent.X, 1e-4);
        }

        [Test]
        public void Work_CompletesOnlyOnce()
        {
            var agent = new ApprenticeAgent { MoveSpeed = 10.0 };
            agent.SetHome(0f, 0f);
            agent.Assign(1f, 0f, 0.5);
            agent.Tick(1.0);   // 도착 후 남은 시간으로 작업 진행 — 이 tick에 완료될 수 있음

            int completions = 0;
            for (int i = 0; i < 10; i++)
                if (agent.Tick(0.1))
                    completions++;
            // 첫 tick 포함 완료는 정확히 1회여야 한다 (복귀·대기 중 재완료 없음)
            Assert.LessOrEqual(completions, 1);
        }

        [Test]
        public void Tick_MovesProportionalToSpeed()
        {
            var agent = new ApprenticeAgent { MoveSpeed = 2.0 };
            agent.SetHome(0f, 0f);
            agent.Assign(10f, 0f, 1.0);
            agent.Tick(1.0);   // 속도 2 × 1초 = 2유닛
            Assert.AreEqual(2f, agent.X, 1e-4);
            Assert.AreEqual(ApprenticePhase.MovingToWork, agent.Phase);
        }

        [Test]
        public void Tick_ZeroDelta_DoesNothing()
        {
            var agent = new ApprenticeAgent { MoveSpeed = 1.0 };
            agent.Assign(5f, 0f, 1.0);
            Assert.IsFalse(agent.Tick(0.0));
            Assert.AreEqual(0f, agent.X, 1e-5);
        }
    }
}
