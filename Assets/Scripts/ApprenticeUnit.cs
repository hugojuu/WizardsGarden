using UnityEngine;
using WizardGarden.Core;
using WizardGarden.Data;

namespace WizardGarden
{
    /// <summary>
    /// 맵 위 견습생 유닛 (S09 표시 어댑터 — 플레이스홀더). 순수 상태 기계(ApprenticeAgent)를 감싸
    /// 색 사각형 + 직군 이모지 + 이름표로 그린다. 위치·단계는 에이전트가, 다음 작업 결정·게임 효과는
    /// MapScreen이 담당한다. 정식 도트 스프라이트는 A04 — SO(ApprenticeData.mapSprite) 참조로 스왑 대비.
    /// </summary>
    public sealed class ApprenticeUnit : MonoBehaviour
    {
        public const int UnitSortingOrder = 20;   // 지면 -100 ~ 유닛 20 (S04b 소팅 규약)

        /// <summary>표시 대상 견습생 id (클릭 라우팅용).</summary>
        public string ApprenticeId { get; private set; }

        /// <summary>이동·작업 상태 기계 (순수 C#).</summary>
        public ApprenticeAgent Agent { get; } = new ApprenticeAgent();

        /// <summary>MapScreen이 이번 작업으로 지시한 내용 — 작업 완료 시 이걸로 게임 효과를 실행한다.</summary>
        public WorkOrder PendingOrder;

        SpriteRenderer _body;
        TextMesh _emoji;
        TextMesh _name;
        TextMesh _bubble;   // 작업/운반 중 말풍선 이모지

        Color _jobColor;
        string _jobEmoji;

        /// <summary>유닛 구성 (스폰 시 1회). 색+이모지 플레이스홀더, 스프라이트가 있으면 스프라이트.</summary>
        public void Configure(ApprenticeState state, ApprenticeData data)
        {
            ApprenticeId = state.Id;
            _jobColor = JobColor(state.Job);
            _jobEmoji = data != null && !string.IsNullOrEmpty(data.displayEmoji) ? data.displayEmoji : JobEmoji(state.Job);
            string displayName = data != null && !string.IsNullOrEmpty(data.displayName) ? data.displayName : state.Id;

            Sprite sprite = data != null ? data.mapSprite : null;
            bool isArt = sprite != null;
            _body = isArt
                ? MapPlaceholderFactory.CreateSprite(transform, "Body", sprite, UnitSortingOrder)
                : MapPlaceholderFactory.CreateSquare(transform, "Body", new Vector2(0.7f, 0.7f), _jobColor, UnitSortingOrder);

            _emoji = MapPlaceholderFactory.CreateText(transform, "Emoji", isArt ? "" : _jobEmoji, 44, 0.08f,
                Color.white, UnitSortingOrder + 1, new Vector3(0f, isArt ? 0.55f : 0.02f, 0f));
            _name = MapPlaceholderFactory.CreateText(transform, "Name", displayName, 30, 0.045f,
                Color.white, UnitSortingOrder + 1, new Vector3(0f, -0.52f, 0f));
            _bubble = MapPlaceholderFactory.CreateText(transform, "Bubble", "", 40, 0.075f,
                new Color(1f, 0.97f, 0.75f), UnitSortingOrder + 1, new Vector3(0f, 0.58f, 0f));

            // 클릭 대상 — 상태 보기/해제. (콜라이더는 유닛 몸체 크기)
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 1.1f);

            SyncTransform();
        }

        /// <summary>에이전트 좌표를 트랜스폼에 반영하고 단계에 따른 말풍선을 갱신한다 (매 프레임).</summary>
        public void SyncTransform()
        {
            transform.localPosition = new Vector3(Agent.X, Agent.Y, 0f);
            if (_bubble == null)
                return;
            switch (Agent.Phase)
            {
                case ApprenticePhase.Working:
                    _bubble.text = WorkBubble(PendingOrder.Kind);
                    break;
                case ApprenticePhase.Returning:
                    _bubble.text = "📦";
                    break;
                default:
                    _bubble.text = "";
                    break;
            }
        }

        static string WorkBubble(WorkKind kind)
        {
            switch (kind)
            {
                case WorkKind.Harvest: return "🌾";
                case WorkKind.Plant: return "🌱";
                case WorkKind.StartProcess: return "⚗️";
                case WorkKind.Collect: return "✨";
                case WorkKind.Display: return "🏷️";
                default: return "💪";
            }
        }

        static Color JobColor(Job job)
        {
            switch (job)
            {
                case Job.Gardener: return new Color(0.36f, 0.62f, 0.32f);
                case Job.Alchemist: return new Color(0.52f, 0.38f, 0.66f);
                case Job.Clerk: return new Color(0.80f, 0.58f, 0.28f);
                default: return PlaceholderPalette.Neutral;
            }
        }

        static string JobEmoji(Job job)
        {
            switch (job)
            {
                case Job.Gardener: return "🌱";
                case Job.Alchemist: return "⚗️";
                case Job.Clerk: return "🏪";
                default: return "🧑";
            }
        }
    }
}
