using UnityEditor;
using UnityEngine;
using WizardGarden.Core;
using WizardGarden.Data;

namespace WizardGarden.EditorTools
{
    /// <summary>
    /// 개발 치트 콘솔 (S02 시간 + S07 골드·해금). 플레이 모드에서 GameClock 배속·스킵·저장,
    /// 골드 지급, 전체 종자 해금을 조작한다. 경제 곡선·계절 검증에 사용.
    /// </summary>
    public class TimeCheatWindow : EditorWindow
    {
        static readonly double[] SpeedPresets = { 1, 10, 60, 360, 900, 3600 };
        static readonly long[] GoldGrants = { 1000, 100000, 10000000, 1000000000 };

        [MenuItem("WizardGarden/Time Cheat (S02+S07)")]
        static void Open()
        {
            GetWindow<TimeCheatWindow>("개발 치트");
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
            EditorGUILayout.LabelField($"골드·해금 (S07) — 보유 {session.Wallet.Gold:N0}G", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (long grant in GoldGrants)
                {
                    if (GUILayout.Button($"+{Abbreviate(grant)}G"))
                    {
                        session.Wallet.Add(grant);
                        Debug.Log($"[치트] 골드 +{grant:N0} (보유 {session.Wallet.Gold:N0})");
                    }
                }
            }
            if (GUILayout.Button("전체 종자 해금 (골드 무소모)"))
                UnlockAllSeeds(session);

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

        // 전체 종자 해금 — 각 식물 해금가를 지급했다가 즉시 해금해 순골드 변화 0 (치트).
        static void UnlockAllSeeds(GameSession session)
        {
            int unlocked = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:PlantData"))
            {
                var plant = AssetDatabase.LoadAssetAtPath<PlantData>(AssetDatabase.GUIDToAssetPath(guid));
                if (plant == null || string.IsNullOrEmpty(plant.id) || plant.unlockCost <= 0)
                    continue;
                if (session.Unlocks.IsUnlocked(plant.id))
                    continue;
                session.Wallet.Add(plant.unlockCost);
                if (session.TryPurchaseUnlock(plant.id, plant.unlockCost))
                    unlocked++;
            }
            Debug.Log($"[치트] 전체 종자 해금 — 신규 {unlocked}종 (골드 순변화 0)");
        }

        static string Abbreviate(long value)
        {
            if (value >= 1_000_000_000) return $"{value / 1_000_000_000}B";
            if (value >= 1_000_000) return $"{value / 1_000_000}M";
            if (value >= 1_000) return $"{value / 1_000}K";
            return value.ToString();
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
