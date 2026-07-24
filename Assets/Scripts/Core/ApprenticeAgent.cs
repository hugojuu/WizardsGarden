using System;

namespace WizardGarden.Core
{
    /// <summary>견습생 유닛의 행동 단계 (상태 기계).</summary>
    public enum ApprenticePhase
    {
        /// <summary>할 일 없음 — 집(구역 대기 지점)에서 대기. 다음 작업 지시를 기다린다.</summary>
        Idle = 0,

        /// <summary>작업 대상으로 이동 중.</summary>
        MovingToWork = 1,

        /// <summary>대상에 도착해 작업 수행 중 (시간 소요).</summary>
        Working = 2,

        /// <summary>작업을 마치고 대기 지점으로 복귀 중 (운반 연출).</summary>
        Returning = 3
    }

    /// <summary>
    /// 견습생 유닛의 이동·작업 상태 기계 (순수 C#, S09 — MonoBehaviour 비의존, EditMode 테스트 가능).
    /// "대상 탐색 → 이동 → 작업(시간) → 복귀(운반)" 루프의 운동학만 담당한다.
    /// 무엇을 할지(대상 선택)와 실제 게임 효과(수확·가공 등)는 어댑터가 결정·실행하고,
    /// 이 에이전트는 좌표·단계·타이머만 진행한다. 좌표는 UnityEngine 비의존 float x/y.
    /// </summary>
    public sealed class ApprenticeAgent
    {
        /// <summary>현재 위치.</summary>
        public float X { get; private set; }
        public float Y { get; private set; }

        /// <summary>대기 지점(집) — Idle·복귀 목표.</summary>
        public float HomeX { get; private set; }
        public float HomeY { get; private set; }

        public ApprenticePhase Phase { get; private set; } = ApprenticePhase.Idle;

        /// <summary>이동 속도 (월드 유닛/초).</summary>
        public double MoveSpeed { get; set; } = 2.2;

        float _targetX;
        float _targetY;
        double _workRemaining;

        public bool IsIdle => Phase == ApprenticePhase.Idle;
        public bool IsBusy => Phase != ApprenticePhase.Idle;

        /// <summary>대기 지점 설정. Idle이면 현재 위치도 그 지점으로 스냅(스폰 위치).</summary>
        public void SetHome(float x, float y)
        {
            HomeX = x;
            HomeY = y;
            if (Phase == ApprenticePhase.Idle)
            {
                X = x;
                Y = y;
            }
        }

        /// <summary>현재 위치를 직접 설정 (스폰·세이브 복원용).</summary>
        public void SetPosition(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 작업 지시 — (tx,ty)로 이동 후 workSeconds 동안 작업, 이후 복귀. Idle일 때만 받는다.
        /// </summary>
        public bool Assign(float tx, float ty, double workSeconds)
        {
            if (Phase != ApprenticePhase.Idle)
                return false;
            _targetX = tx;
            _targetY = ty;
            _workRemaining = workSeconds > 0.0 ? workSeconds : 0.0;
            Phase = ApprenticePhase.MovingToWork;
            return true;
        }

        /// <summary>
        /// 한 프레임 진행. 이번 tick에 작업이 "막 완료"되면 true를 1회 반환한다
        /// (어댑터가 그 순간 실제 게임 효과를 실행). dt는 실시간 델타 초.
        /// </summary>
        public bool Tick(double dt)
        {
            if (dt <= 0.0)
                return false;

            bool workJustCompleted = false;
            switch (Phase)
            {
                case ApprenticePhase.MovingToWork:
                    if (StepToward(_targetX, _targetY, dt))
                        Phase = ApprenticePhase.Working;
                    break;

                case ApprenticePhase.Working:
                    _workRemaining -= dt;
                    if (_workRemaining <= 0.0)
                    {
                        _workRemaining = 0.0;
                        workJustCompleted = true;
                        Phase = ApprenticePhase.Returning;
                    }
                    break;

                case ApprenticePhase.Returning:
                    if (StepToward(HomeX, HomeY, dt))
                        Phase = ApprenticePhase.Idle;
                    break;
            }
            return workJustCompleted;
        }

        // 목표를 향해 dt만큼 이동. 이번 tick에 도착하면 true (좌표를 목표에 스냅).
        bool StepToward(float targetX, float targetY, double dt)
        {
            double dx = targetX - X;
            double dy = targetY - Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double step = MoveSpeed * dt;

            if (distance <= step || distance <= 1e-5)
            {
                X = targetX;
                Y = targetY;
                return true;
            }

            double t = step / distance;
            X += (float)(dx * t);
            Y += (float)(dy * t);
            return false;
        }
    }
}
