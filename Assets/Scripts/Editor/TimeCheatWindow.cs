using UnityEditor;
using UnityEngine;
using WizardGarden.Core;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// 시간 배속 치트 (S02). 플레이 모드에서 GameClock 배속·스킵·저장을 조작한다.
    /// 이후 세션의 경제·계절 검증에 계속 사용.
    /// </summary>
    public class TimeCheatWindow : EditorWindow
    {
        static readonly double[] SpeedPresets = { 1, 10, 60, 360, 900 };

        [MenuItem("WizardGarden/Time Cheat (S02)")]
        static void Open()
        {
            GetWindow<TimeCheatWindow>("시간 치트");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("세이브 경로", Application.persistentDataPath);

            if (!Application.isPlaying || GameClockRunner.Instance == null)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 시계 조작 가능 (GameClockRunner는 플레이 시작 시 자동 생성).", MessageType.Info);
                if (GUILayout.Button("세이브 파일 삭제"))
                {
                    var repository = new SaveRepository(Application.persistentDataPath);
                    repository.Delete();
                    Debug.Log($"[TimeCheat] 세이브 삭제: {repository.FilePath}");
                }
                return;
            }

            GameClock clock = GameClockRunner.Instance.Clock;
            GameSession session = GameClockRunner.Instance.Session;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("현재 시계", EditorStyles.boldLabel);
            int hour = (int)clock.HourOfDay;
            int minute = (int)((clock.HourOfDay - hour) * 60.0);
            EditorGUILayout.LabelField($"{clock.DayIndex}일차 {hour:D2}:{minute:D2} ({TimeOfDayLabel(clock.CurrentTimeOfDay)})");
            EditorGUILayout.LabelField($"사건 시간 {clock.EventSeconds:F1}s · 자원 시간 {clock.ResourceSeconds:F1}s");
            if (session.PendingOfflineSeconds > 0.0)
                EditorGUILayout.LabelField($"미정산 오프라인 경과: {session.PendingOfflineSeconds:F0}s (정산은 S08)");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"배속: x{clock.TimeScale:F0}", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (double preset in SpeedPresets)
                {
                    if (GUILayout.Button($"x{preset:F0}"))
                        clock.TimeScale = preset;
                }
            }
            clock.TimeScale = EditorGUILayout.Slider("배속 슬라이더", (float)clock.TimeScale, 1f, 3600f);
            EditorGUILayout.HelpBox("x900 = 현실 1초에 게임 내 하루", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("스킵", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+1시간"))
                    clock.SkipGameHours(1.0);
                if (GUILayout.Button("+1일"))
                    clock.SkipGameHours(24.0);
                if (GUILayout.Button("+7일(1계절 분량)"))
                    clock.SkipGameHours(24.0 * 7.0);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("지금 저장"))
                {
                    session.SaveNow();
                    Debug.Log("[TimeCheat] 저장 완료");
                }
            }
        }

        void Update()
        {
            if (Application.isPlaying)
                Repaint();
        }

        static string TimeOfDayLabel(TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case TimeOfDay.Morning: return "아침";
                case TimeOfDay.Day: return "낮";
                case TimeOfDay.Evening: return "저녁";
                case TimeOfDay.Night: return "야간";
                default: return timeOfDay.ToString();
            }
        }
    }
}
