using UnityEngine;
using WizardGarden.Core;

namespace WizardGarden
{
    /// <summary>
    /// GameSession/GameClock의 얇은 MonoBehaviour 어댑터.
    /// 플레이 시작 시 자동 생성(씬 배치 불필요) — 로드·매 프레임 tick·주기 저장·종료 시 저장만 담당.
    /// </summary>
    public sealed class GameClockRunner : MonoBehaviour
    {
        [Tooltip("자동 저장 간격 (현실 초, 0 이하면 자동 저장 안 함)")]
        public float autoSaveIntervalSeconds = 60f;

        public static GameClockRunner Instance { get; private set; }

        public GameSession Session { get; private set; }
        public GameClock Clock => Session?.Clock;

        float _autoSaveTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null)
                return;
            var go = new GameObject(nameof(GameClockRunner));
            go.AddComponent<GameClockRunner>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Session = new GameSession(new SaveRepository(Application.persistentDataPath), new SystemUtcClock());
            Session.Begin();
            if (Session.LoadedFromSave)
                Debug.Log($"[GameClock] 세이브 복원 — 오프라인 경과 {Session.PendingOfflineSeconds:F0}초 (정산은 S08). {Session.Clock.DayIndex}일차 {Session.Clock.HourOfDay:F1}시");
            else
                Debug.Log("[GameClock] 새 게임 시작 — 1일차 06:00");
        }

        void Update()
        {
            Session.Clock.Tick(Time.unscaledDeltaTime);

            if (autoSaveIntervalSeconds > 0f)
            {
                _autoSaveTimer += Time.unscaledDeltaTime;
                if (_autoSaveTimer >= autoSaveIntervalSeconds)
                {
                    _autoSaveTimer = 0f;
                    Session.SaveNow();
                }
            }
        }

        void OnApplicationQuit()
        {
            Session?.SaveNow();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
