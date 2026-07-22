using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WizardGarden.Core;
using WizardGarden.Data;

namespace WizardGarden
{
    /// <summary>
    /// 정원 화면 어댑터 (S03) — 밭 슬롯 그리드·종자 선택·인벤토리 표시/입력만 담당.
    /// 로직은 전부 Core(Garden/Inventory/GameSession). UI는 코드로 생성하는 플레이스홀더
    /// (색 사각형 + 이모지/텍스트) — 정식 아트·UI는 후반 교체.
    /// </summary>
    public sealed class GardenScreen : MonoBehaviour
    {
        [Tooltip("종자 선택지 (티어1 식물 4종 — SO 참조, 표시는 데이터 필드 참조로만)")]
        public List<PlantData> seedOptions = new List<PlantData>();

        GameSession _session;
        Font _font;
        Text _clockLabel;
        Text _inventoryLabel;
        GameObject _seedPanel;
        int _pendingSlotIndex = -1;

        readonly Dictionary<string, PlantData> _plantsById = new Dictionary<string, PlantData>();
        readonly List<SlotWidget> _slotWidgets = new List<SlotWidget>();

        sealed class SlotWidget
        {
            public int Index;
            public Image Background;
            public Text Emoji;
            public Text Label;
        }

        void Start()
        {
            _session = GameClockRunner.Instance != null ? GameClockRunner.Instance.Session : null;
            if (_session == null)
            {
                Debug.LogError("[GardenScreen] GameClockRunner 세션 없음 — 화면 비활성");
                enabled = false;
                return;
            }

            foreach (PlantData plant in seedOptions)
            {
                if (plant != null && !string.IsNullOrEmpty(plant.id))
                    _plantsById[plant.id] = plant;
            }

            BuildUi();
            _session.Inventory.Changed += RefreshInventory;
            RefreshInventory();
        }

        void OnDestroy()
        {
            if (_session != null)
                _session.Inventory.Changed -= RefreshInventory;
        }

        void Update()
        {
            if (_session == null)
                return;
            RefreshClockLabel();
            RefreshSlots();
        }

        // ---- 입력 (버튼·스모크 테스트 공용 공개 API) ----

        public void OnSlotClicked(int slotIndex)
        {
            if (!_session.Garden.IsValidIndex(slotIndex))
                return;

            GardenSlot slot = _session.Garden.Slots[slotIndex];
            if (slot.IsEmpty)
            {
                OpenSeedPanel(slotIndex);
                return;
            }
            TryHarvestSlot(slotIndex);
        }

        /// <summary>종자 심기 (성공 시 종자 패널 닫힘).</summary>
        public bool TryPlantSeed(int slotIndex, PlantData plant)
        {
            if (plant == null || !_session.TryPlant(slotIndex, plant.id))
                return false;

            CloseSeedPanel();
            return true;
        }

        /// <summary>수확 시도 — 완료 상태일 때만 성공, 수확물은 인벤토리로.</summary>
        public bool TryHarvestSlot(int slotIndex)
        {
            if (!_session.Garden.IsValidIndex(slotIndex))
                return false;

            GardenSlot slot = _session.Garden.Slots[slotIndex];
            if (slot.IsEmpty)
                return false;

            return _session.TryHarvestToInventory(slotIndex, GetGrowthSeconds(slot.PlantId));
        }

        double GetGrowthSeconds(string plantId)
        {
            if (_plantsById.TryGetValue(plantId, out PlantData plant))
                return plant.growthSeconds;

            Debug.LogWarning($"[GardenScreen] 알 수 없는 식물 id '{plantId}' — 즉시 수확 가능으로 처리");
            return 0.0;
        }

        // ---- 갱신 (이벤트 대신 현재 상태 폴링 — S02 인계 방침) ----

        void RefreshClockLabel()
        {
            GameClock clock = _session.Clock;
            int hour = (int)clock.HourOfDay;
            int minute = (int)((clock.HourOfDay - hour) * 60.0);
            _clockLabel.text = $"{clock.DayIndex}일차 {hour:00}:{minute:00} ({TimeOfDayLabel(clock.CurrentTimeOfDay)})";
        }

        static string TimeOfDayLabel(TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case TimeOfDay.Morning: return "아침";
                case TimeOfDay.Day: return "낮";
                case TimeOfDay.Evening: return "저녁";
                case TimeOfDay.Night: return "야간";
                default: return "?";
            }
        }

        void RefreshSlots()
        {
            double now = _session.Clock.ResourceSeconds;
            foreach (SlotWidget widget in _slotWidgets)
            {
                GardenSlot slot = _session.Garden.Slots[widget.Index];
                if (slot.IsEmpty)
                {
                    widget.Background.color = PlaceholderPalette.EmptySoil;
                    widget.Emoji.text = "➕";
                    widget.Label.text = "빈 밭";
                    continue;
                }

                _plantsById.TryGetValue(slot.PlantId, out PlantData plant);
                Color elementColor = plant != null
                    ? PlaceholderPalette.ForComposition(plant.composition)
                    : PlaceholderPalette.Neutral;
                string plantEmoji = plant != null ? plant.displayEmoji : "❓";

                double progress = slot.GetProgress(now, plant != null ? plant.growthSeconds : 0.0);
                int percent = (int)(progress * 100.0);
                switch (GrowthStageUtility.FromProgress(progress))
                {
                    case GrowthStage.Sprout:
                        widget.Background.color = Color.Lerp(PlaceholderPalette.EmptySoil, elementColor, 0.35f);
                        widget.Emoji.text = "🌱";
                        widget.Label.text = $"새싹 {percent}%";
                        break;
                    case GrowthStage.Growing:
                        widget.Background.color = Color.Lerp(PlaceholderPalette.EmptySoil, elementColor, 0.65f);
                        widget.Emoji.text = plantEmoji;
                        widget.Label.text = $"성장 {percent}%";
                        break;
                    default:
                        widget.Background.color = elementColor;
                        widget.Emoji.text = plantEmoji;
                        widget.Label.text = "수확!";
                        break;
                }
            }
        }

        void RefreshInventory()
        {
            if (_inventoryLabel == null)
                return;

            var builder = new StringBuilder();
            foreach (KeyValuePair<string, int> entry in _session.Inventory.Entries)
            {
                _plantsById.TryGetValue(entry.Key, out PlantData plant);
                string name = plant != null ? $"{plant.displayEmoji} {plant.displayName}" : entry.Key;
                builder.AppendLine($"{name} ×{entry.Value}");
            }
            _inventoryLabel.text = builder.Length > 0 ? builder.ToString() : "(비어 있음)";
        }

        // ---- 종자 선택 패널 ----

        void OpenSeedPanel(int slotIndex)
        {
            _pendingSlotIndex = slotIndex;
            _seedPanel.SetActive(true);
        }

        void CloseSeedPanel()
        {
            _pendingSlotIndex = -1;
            _seedPanel.SetActive(false);
        }

        // ---- UI 생성 (플레이스홀더) ----

        void BuildUi()
        {
            _font = LoadPlaceholderFont();

            var canvasGo = new GameObject("GardenCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            EnsureEventSystem();

            _clockLabel = CreateText(canvasGo.transform, "ClockLabel", "", 22, TextAnchor.MiddleLeft);
            SetRect(_clockLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -28f), new Vector2(480f, 32f), new Vector2(0f, 1f));

            BuildSlotGrid(canvasGo.transform);
            BuildInventoryPanel(canvasGo.transform);
            BuildSeedPanel(canvasGo.transform);
        }

        void BuildSlotGrid(Transform parent)
        {
            var gridGo = new GameObject("SlotGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(parent, false);
            var rect = (RectTransform)gridGo.transform;
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-180f, 0f), new Vector2(324f, 324f), new Vector2(0.5f, 0.5f));

            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150f, 150f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < _session.Garden.SlotCount; i++)
            {
                int slotIndex = i;
                var slotGo = new GameObject($"Slot{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                slotGo.transform.SetParent(gridGo.transform, false);
                var background = slotGo.GetComponent<Image>();
                background.color = PlaceholderPalette.EmptySoil;
                slotGo.GetComponent<Button>().onClick.AddListener(() => OnSlotClicked(slotIndex));

                Text emoji = CreateText(slotGo.transform, "Emoji", "", 48, TextAnchor.MiddleCenter);
                SetRectStretch(emoji.rectTransform, new Vector2(0f, 0.28f), Vector2.one);

                Text label = CreateText(slotGo.transform, "Label", "", 18, TextAnchor.MiddleCenter);
                SetRectStretch(label.rectTransform, Vector2.zero, new Vector2(1f, 0.28f));

                _slotWidgets.Add(new SlotWidget { Index = slotIndex, Background = background, Emoji = emoji, Label = label });
            }
        }

        void BuildInventoryPanel(Transform parent)
        {
            var panelGo = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            panelGo.GetComponent<Image>().color = PlaceholderPalette.PanelBackground;
            SetRect((RectTransform)panelGo.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f, 0f), new Vector2(300f, 420f), new Vector2(1f, 0.5f));

            Text title = CreateText(panelGo.transform, "Title", "🧺 수확물", 22, TextAnchor.MiddleCenter);
            SetRectStretch(title.rectTransform, new Vector2(0f, 0.88f), Vector2.one);

            _inventoryLabel = CreateText(panelGo.transform, "Items", "(비어 있음)", 18, TextAnchor.UpperLeft);
            SetRectStretch(_inventoryLabel.rectTransform, new Vector2(0.06f, 0.03f), new Vector2(0.97f, 0.86f));
        }

        void BuildSeedPanel(Transform parent)
        {
            _seedPanel = new GameObject("SeedPanel", typeof(RectTransform), typeof(Image));
            _seedPanel.transform.SetParent(parent, false);
            _seedPanel.GetComponent<Image>().color = PlaceholderPalette.PanelBackground;
            SetRect((RectTransform)_seedPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(380f, 440f), new Vector2(0.5f, 0.5f));

            Text title = CreateText(_seedPanel.transform, "Title", "종자 선택", 24, TextAnchor.MiddleCenter);
            SetRectStretch(title.rectTransform, new Vector2(0f, 0.9f), Vector2.one);

            var listGo = new GameObject("SeedList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(_seedPanel.transform, false);
            SetRectStretch((RectTransform)listGo.transform, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.88f));
            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            foreach (PlantData plant in seedOptions)
            {
                if (plant == null)
                    continue;
                PlantData captured = plant;
                Button button = CreateButton(listGo.transform, $"Seed_{plant.id}",
                    $"{plant.displayEmoji} {plant.displayName} ({plant.growthSeconds:0}초)",
                    PlaceholderPalette.ForComposition(plant.composition),
                    () => TryPlantSeed(_pendingSlotIndex, captured));
                var buttonText = button.GetComponentInChildren<Text>();
                buttonText.fontSize = 20;
            }

            Button cancel = CreateButton(_seedPanel.transform, "Cancel", "취소",
                PlaceholderPalette.Neutral, CloseSeedPanel);
            SetRect((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 34f), new Vector2(160f, 40f), new Vector2(0.5f, 0f));

            _seedPanel.SetActive(false);
        }

        Button CreateButton(Transform parent, string name, string labelText, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            Text label = CreateText(go.transform, "Text", labelText, 18, TextAnchor.MiddleCenter);
            SetRectStretch(label.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.text = content;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        static void SetRectStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Font LoadPlaceholderFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 18);
            return font;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
