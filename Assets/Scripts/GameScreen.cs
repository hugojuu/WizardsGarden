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
    /// 3구역(정원/공방/상점) 통합 화면 어댑터 (S04 — S03 GardenScreen 재구성).
    /// 표시·입력만 담당, 로직은 전부 Core(GameSession 이하). UI는 코드로 생성하는
    /// 플레이스홀더(색 사각형 + 이모지 + 한글 라벨) — 정식 아트·UI는 후반 교체.
    /// </summary>
    public sealed class GameScreen : MonoBehaviour
    {
        public enum Zone { Garden = 0, Workshop = 1, Shop = 2 }

        /// <summary>진열 버튼 1회당 인벤토리 → 진열대 이동 수량 상한 (UI 결정).</summary>
        public const int DisplayMoveCount = 10;

        [Tooltip("종자 선택지 (티어1 4종 + 해금 종자 — SO 참조, 표시는 데이터 필드 참조로만)")]
        public List<PlantData> seedOptions = new List<PlantData>();

        [Tooltip("작업대 1차 가공 레시피 (마른 잎 4종 — MaterialData SO 참조)")]
        public List<MaterialData> recipeOptions = new List<MaterialData>();

        GameSession _session;
        Font _font;

        Text _clockLabel;
        Text _goldLabel;
        Text _inventoryLabel;

        GameObject _gardenPanel;
        GameObject _workshopPanel;
        GameObject _shopPanel;
        readonly List<Button> _tabButtons = new List<Button>();
        Zone _activeZone = Zone.Garden;

        GameObject _slotGridGo;
        Button _expandButton;
        Text _expandLabel;
        GameObject _seedPanel;
        int _pendingSlotIndex = -1;

        Image _benchBackground;
        Text _benchEmoji;
        Text _benchLabel;
        readonly List<(Button button, MaterialData material)> _recipeButtons = new List<(Button, MaterialData)>();

        Text _customerLabel;
        Text _saleLogLabel;
        GameObject _displayPanel;
        int _pendingDisplaySlot = -1;
        readonly List<SlotWidget> _shopSlotWidgets = new List<SlotWidget>();
        readonly Queue<string> _saleLog = new Queue<string>();
        readonly List<Shop.SaleRecord> _salesBuffer = new List<Shop.SaleRecord>();

        readonly Dictionary<string, ItemData> _itemsById = new Dictionary<string, ItemData>();
        readonly Dictionary<string, PlantData> _plantsById = new Dictionary<string, PlantData>();
        readonly Dictionary<string, MaterialData> _materialsById = new Dictionary<string, MaterialData>();
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
                Debug.LogError("[GameScreen] GameClockRunner 세션 없음 — 화면 비활성");
                enabled = false;
                return;
            }

            foreach (PlantData plant in seedOptions)
            {
                if (plant == null || string.IsNullOrEmpty(plant.id))
                    continue;
                _plantsById[plant.id] = plant;
                _itemsById[plant.id] = plant;
            }
            foreach (MaterialData material in recipeOptions)
            {
                if (material == null || string.IsNullOrEmpty(material.id))
                    continue;
                _materialsById[material.id] = material;
                _itemsById[material.id] = material;
            }

            BuildUi();
            _session.Inventory.Changed += RefreshInventory;
            _session.Wallet.Changed += RefreshGold;
            RefreshInventory();
            RefreshGold();
            ShowZone(Zone.Garden);
        }

        void OnDestroy()
        {
            if (_session == null)
                return;
            _session.Inventory.Changed -= RefreshInventory;
            _session.Wallet.Changed -= RefreshGold;
        }

        void Update()
        {
            if (_session == null)
                return;

            double now = _session.Clock.ResourceSeconds;

            _salesBuffer.Clear();
            _session.Shop.TickCustomers(now, ResolvePrice, _session.Wallet, _salesBuffer);
            foreach (Shop.SaleRecord sale in _salesBuffer)
                AppendSaleLog(sale);

            RefreshClockLabel();
            RefreshGardenSlots(now);
            RefreshExpandButton();
            RefreshWorkshop(now);
            RefreshShop(now);
        }

        // ---- 조회 (SO 데이터) ----

        int ResolvePrice(string itemId)
        {
            return _itemsById.TryGetValue(itemId, out ItemData item) ? item.baseValue : 0;
        }

        string ItemLabel(string itemId)
        {
            return _itemsById.TryGetValue(itemId, out ItemData item)
                ? $"{item.displayEmoji} {item.displayName}"
                : itemId;
        }

        double GetGrowthSeconds(string plantId)
        {
            if (_plantsById.TryGetValue(plantId, out PlantData plant))
                return plant.growthSeconds;

            Debug.LogWarning($"[GameScreen] 알 수 없는 식물 id '{plantId}' — 즉시 수확 가능으로 처리");
            return 0.0;
        }

        double GetProcessingSeconds(string materialId)
        {
            if (_materialsById.TryGetValue(materialId, out MaterialData material))
                return material.processingSeconds;

            Debug.LogWarning($"[GameScreen] 알 수 없는 재료 id '{materialId}' — 즉시 완료로 처리");
            return 0.0;
        }

        // ---- 입력 (버튼·스모크 테스트 공용 공개 API) ----

        public void ShowZone(Zone zone)
        {
            _activeZone = zone;
            _gardenPanel.SetActive(zone == Zone.Garden);
            _workshopPanel.SetActive(zone == Zone.Workshop);
            _shopPanel.SetActive(zone == Zone.Shop);
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                _tabButtons[i].GetComponent<Image>().color = i == (int)zone
                    ? new Color(0.36f, 0.36f, 0.46f)
                    : new Color(0.17f, 0.17f, 0.21f);
            }
            CloseSeedPanel();
            CloseDisplayPanel();
        }

        public void OnGardenSlotClicked(int slotIndex)
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

        /// <summary>종자 심기 (성공 시 종자 패널 닫힘). 해금 안 된 종자는 실패.</summary>
        public bool TryPlantSeed(int slotIndex, PlantData plant)
        {
            if (plant == null || IsSeedLocked(plant))
                return false;
            if (!_session.TryPlant(slotIndex, plant.id))
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

        /// <summary>골드로 밭 슬롯 확장.</summary>
        public bool TryBuyGardenSlot()
        {
            return _session.TryBuyGardenSlot();
        }

        /// <summary>골드로 종자 해금 (성공 시 종자 패널이 열려 있으면 갱신).</summary>
        public bool TryUnlockSeed(PlantData plant)
        {
            if (plant == null || !_session.TryPurchaseUnlock(plant.id, plant.unlockCost))
                return false;

            if (_seedPanel != null && _seedPanel.activeSelf)
                OpenSeedPanel(_pendingSlotIndex); // 잠금 해제 반영 재구성
            return true;
        }

        public bool IsSeedLocked(PlantData plant)
        {
            return plant != null && plant.unlockCost > 0 && !_session.Unlocks.IsUnlocked(plant.id);
        }

        /// <summary>작업대 가공 시작 — 원료는 인벤토리에서 소비.</summary>
        public bool TryStartRecipe(MaterialData material)
        {
            if (material == null || material.sourceItem == null)
                return false;
            return _session.Workshop.TryStart(material.id, 1, material.sourceItem.id, material.sourceCount,
                _session.Inventory, _session.Clock.ResourceSeconds);
        }

        /// <summary>작업대 산출물 수령 (완료 상태일 때만).</summary>
        public bool TryCollectBench()
        {
            Workshop workshop = _session.Workshop;
            if (workshop.IsIdle)
                return false;
            return workshop.TryCollect(_session.Clock.ResourceSeconds, GetProcessingSeconds(workshop.OutputItemId),
                _session.Inventory, out _, out _);
        }

        public void OnShopSlotClicked(int slotIndex)
        {
            if (!_session.Shop.IsValidIndex(slotIndex))
                return;

            if (_session.Shop.Slots[slotIndex].IsEmpty)
                OpenDisplayPanel(slotIndex);
            else
                _session.Shop.TryTakeBack(slotIndex, _session.Inventory);
        }

        /// <summary>인벤토리 아이템을 진열대로 이동 (1회 최대 DisplayMoveCount개).</summary>
        public bool TryDisplayItem(int slotIndex, string itemId)
        {
            int moved = _session.Shop.Display(slotIndex, itemId, DisplayMoveCount, _session.Inventory);
            if (moved <= 0)
                return false;

            CloseDisplayPanel();
            return true;
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

        void RefreshGold()
        {
            if (_goldLabel != null)
                _goldLabel.text = $"💰 {_session.Wallet.Gold:N0}G";
        }

        void RefreshGardenSlots(double now)
        {
            if (_slotWidgets.Count != _session.Garden.SlotCount)
                RebuildGardenGrid();

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

        void RefreshExpandButton()
        {
            if (_expandButton == null)
                return;

            if (_session.Garden.SlotCount >= Garden.MaxSlotCount)
            {
                _expandLabel.text = "밭 확장 (최대)";
                _expandButton.interactable = false;
                return;
            }

            int cost = _session.NextGardenSlotCost;
            _expandLabel.text = $"➕ 밭 확장 — {cost}G";
            _expandButton.interactable = _session.Wallet.CanAfford(cost);
        }

        void RefreshWorkshop(double now)
        {
            Workshop workshop = _session.Workshop;

            if (workshop.IsIdle)
            {
                _benchBackground.color = PlaceholderPalette.PanelBackground;
                _benchEmoji.text = "🛠️";
                _benchLabel.text = "작업대 비어 있음\n아래에서 가공을 선택";
            }
            else
            {
                _materialsById.TryGetValue(workshop.OutputItemId, out MaterialData material);
                double processingSeconds = material != null ? material.processingSeconds : 0.0;
                double progress = workshop.GetProgress(now, processingSeconds);
                Color color = material != null
                    ? PlaceholderPalette.ForComposition(material.composition)
                    : PlaceholderPalette.Neutral;
                string emoji = material != null ? material.displayEmoji : "❓";
                string name = material != null ? material.displayName : workshop.OutputItemId;

                if (workshop.IsComplete(now, processingSeconds))
                {
                    _benchBackground.color = color;
                    _benchEmoji.text = emoji;
                    _benchLabel.text = $"{name} 완료!\n클릭해서 수령";
                }
                else
                {
                    _benchBackground.color = Color.Lerp(PlaceholderPalette.EmptySoil, color, 0.5f);
                    _benchEmoji.text = "⚗️";
                    _benchLabel.text = $"{name} 가공 중… {(int)(progress * 100.0)}%";
                }
            }

            foreach ((Button button, MaterialData material) in _recipeButtons)
            {
                bool hasSource = material.sourceItem != null
                    && _session.Inventory.GetCount(material.sourceItem.id) >= material.sourceCount;
                button.interactable = workshop.IsIdle && hasSource;
            }
        }

        void RefreshShop(double now)
        {
            _customerLabel.text = $"🚶 다음 손님까지 {_session.Shop.SecondsUntilNextCustomer(now):0}초";

            foreach (SlotWidget widget in _shopSlotWidgets)
            {
                Shop.DisplaySlot slot = _session.Shop.Slots[widget.Index];
                if (slot.IsEmpty)
                {
                    widget.Background.color = PlaceholderPalette.PanelBackground;
                    widget.Emoji.text = "➕";
                    widget.Label.text = "빈 진열대";
                    continue;
                }

                _itemsById.TryGetValue(slot.ItemId, out ItemData item);
                widget.Background.color = item != null
                    ? PlaceholderPalette.ForComposition(item.composition)
                    : PlaceholderPalette.Neutral;
                widget.Emoji.text = item != null ? item.displayEmoji : "❓";
                string name = item != null ? item.displayName : slot.ItemId;
                widget.Label.text = $"{name}\n×{slot.Count} · 개당 {ResolvePrice(slot.ItemId)}G";
            }
        }

        void RefreshInventory()
        {
            if (_inventoryLabel == null)
                return;

            var builder = new StringBuilder();
            foreach (KeyValuePair<string, int> entry in _session.Inventory.Entries)
                builder.AppendLine($"{ItemLabel(entry.Key)} ×{entry.Value}");
            _inventoryLabel.text = builder.Length > 0 ? builder.ToString() : "(비어 있음)";
        }

        void AppendSaleLog(Shop.SaleRecord sale)
        {
            _saleLog.Enqueue($"🔔 {ItemLabel(sale.ItemId)} ×{sale.Count} → +{sale.Gold}G");
            while (_saleLog.Count > 5)
                _saleLog.Dequeue();
            _saleLogLabel.text = string.Join("\n", _saleLog);
        }

        // ---- 종자 선택 패널 (열 때마다 해금 상태 반영 재구성) ----

        void OpenSeedPanel(int slotIndex)
        {
            _pendingSlotIndex = slotIndex;
            RebuildSeedPanelContent();
            _seedPanel.SetActive(true);
        }

        void CloseSeedPanel()
        {
            _pendingSlotIndex = -1;
            if (_seedPanel != null)
                _seedPanel.SetActive(false);
        }

        void RebuildSeedPanelContent()
        {
            Transform list = _seedPanel.transform.Find("SeedList");
            for (int i = list.childCount - 1; i >= 0; i--)
                Destroy(list.GetChild(i).gameObject);

            foreach (PlantData plant in seedOptions)
            {
                if (plant == null)
                    continue;
                PlantData captured = plant;

                if (IsSeedLocked(plant))
                {
                    Button lockButton = CreateButton(list, $"Unlock_{plant.id}",
                        $"🔒 {plant.displayEmoji} {plant.displayName} — {plant.unlockCost}G 해금",
                        PlaceholderPalette.Neutral,
                        () => TryUnlockSeed(captured));
                    lockButton.interactable = _session.Wallet.CanAfford(plant.unlockCost);
                    lockButton.GetComponentInChildren<Text>().fontSize = 18;
                    continue;
                }

                Button button = CreateButton(list, $"Seed_{plant.id}",
                    $"{plant.displayEmoji} {plant.displayName} ({plant.growthSeconds:0}초)",
                    PlaceholderPalette.ForComposition(plant.composition),
                    () => TryPlantSeed(_pendingSlotIndex, captured));
                button.GetComponentInChildren<Text>().fontSize = 20;
            }
        }

        // ---- 진열 선택 패널 (열 때마다 인벤토리 반영 재구성) ----

        void OpenDisplayPanel(int shopSlotIndex)
        {
            _pendingDisplaySlot = shopSlotIndex;
            RebuildDisplayPanelContent();
            _displayPanel.SetActive(true);
        }

        void CloseDisplayPanel()
        {
            _pendingDisplaySlot = -1;
            if (_displayPanel != null)
                _displayPanel.SetActive(false);
        }

        void RebuildDisplayPanelContent()
        {
            Transform list = _displayPanel.transform.Find("ItemList");
            for (int i = list.childCount - 1; i >= 0; i--)
                Destroy(list.GetChild(i).gameObject);

            bool any = false;
            foreach (KeyValuePair<string, int> entry in _session.Inventory.Entries)
            {
                int price = ResolvePrice(entry.Key);
                if (price <= 0)
                    continue;
                any = true;
                string capturedId = entry.Key;
                _itemsById.TryGetValue(entry.Key, out ItemData item);
                Button button = CreateButton(list, $"Display_{entry.Key}",
                    $"{ItemLabel(entry.Key)} ×{entry.Value} (개당 {price}G)",
                    item != null ? PlaceholderPalette.ForComposition(item.composition) : PlaceholderPalette.Neutral,
                    () => TryDisplayItem(_pendingDisplaySlot, capturedId));
                button.GetComponentInChildren<Text>().fontSize = 18;
            }

            if (!any)
                CreateText(list, "EmptyHint", "(팔 수 있는 물건이 없음 — 정원에서 수확하세요)", 16, TextAnchor.MiddleCenter);
        }

        // ---- UI 생성 (플레이스홀더) ----

        void BuildUi()
        {
            _font = LoadPlaceholderFont();

            var canvasGo = new GameObject("GameCanvas",
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

            _goldLabel = CreateText(canvasGo.transform, "GoldLabel", "💰 0G", 24, TextAnchor.MiddleRight);
            SetRect(_goldLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -28f), new Vector2(320f, 32f), new Vector2(1f, 1f));

            BuildTabBar(canvasGo.transform);
            BuildInventoryPanel(canvasGo.transform);

            _gardenPanel = CreateZonePanel(canvasGo.transform, "GardenPanel");
            BuildGardenPanel(_gardenPanel.transform);

            _workshopPanel = CreateZonePanel(canvasGo.transform, "WorkshopPanel");
            BuildWorkshopPanel(_workshopPanel.transform);

            _shopPanel = CreateZonePanel(canvasGo.transform, "ShopPanel");
            BuildShopPanel(_shopPanel.transform);

            BuildSeedPanel(canvasGo.transform);
            BuildDisplayPanel(canvasGo.transform);
        }

        GameObject CreateZonePanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRectStretch((RectTransform)go.transform, Vector2.zero, Vector2.one);
            return go;
        }

        void BuildTabBar(Transform parent)
        {
            var barGo = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            barGo.transform.SetParent(parent, false);
            SetRect((RectTransform)barGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-160f, -72f), new Vector2(480f, 40f), new Vector2(0.5f, 1f));
            var layout = barGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            string[] labels = { "🌱 정원", "⚗️ 공방", "🏪 상점" };
            for (int i = 0; i < labels.Length; i++)
            {
                Zone zone = (Zone)i;
                Button tab = CreateButton(barGo.transform, $"Tab_{zone}", labels[i],
                    new Color(0.17f, 0.17f, 0.21f), () => ShowZone(zone));
                tab.GetComponentInChildren<Text>().fontSize = 20;
                _tabButtons.Add(tab);
            }
        }

        void BuildGardenPanel(Transform parent)
        {
            _slotGridGo = new GameObject("SlotGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            _slotGridGo.transform.SetParent(parent, false);
            SetRect((RectTransform)_slotGridGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-160f, -128f), new Vector2(366f, 488f), new Vector2(0.5f, 1f));

            var grid = _slotGridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(110f, 110f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            RebuildGardenGrid();

            _expandButton = CreateButton(parent, "ExpandButton", "➕ 밭 확장",
                PlaceholderPalette.Earth, () => TryBuyGardenSlot());
            SetRect((RectTransform)_expandButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-160f, 22f), new Vector2(280f, 44f), new Vector2(0.5f, 0f));
            _expandLabel = _expandButton.GetComponentInChildren<Text>();
            _expandLabel.fontSize = 20;
        }

        void RebuildGardenGrid()
        {
            for (int i = _slotGridGo.transform.childCount - 1; i >= 0; i--)
                Destroy(_slotGridGo.transform.GetChild(i).gameObject);
            _slotWidgets.Clear();

            for (int i = 0; i < _session.Garden.SlotCount; i++)
            {
                int slotIndex = i;
                var slotGo = new GameObject($"Slot{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                slotGo.transform.SetParent(_slotGridGo.transform, false);
                var background = slotGo.GetComponent<Image>();
                background.color = PlaceholderPalette.EmptySoil;
                slotGo.GetComponent<Button>().onClick.AddListener(() => OnGardenSlotClicked(slotIndex));

                Text emoji = CreateText(slotGo.transform, "Emoji", "", 36, TextAnchor.MiddleCenter);
                SetRectStretch(emoji.rectTransform, new Vector2(0f, 0.28f), Vector2.one);

                Text label = CreateText(slotGo.transform, "Label", "", 15, TextAnchor.MiddleCenter);
                SetRectStretch(label.rectTransform, Vector2.zero, new Vector2(1f, 0.28f));

                _slotWidgets.Add(new SlotWidget { Index = slotIndex, Background = background, Emoji = emoji, Label = label });
            }
        }

        void BuildWorkshopPanel(Transform parent)
        {
            var benchGo = new GameObject("Bench", typeof(RectTransform), typeof(Image), typeof(Button));
            benchGo.transform.SetParent(parent, false);
            SetRect((RectTransform)benchGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-160f, -140f), new Vector2(360f, 170f), new Vector2(0.5f, 1f));
            _benchBackground = benchGo.GetComponent<Image>();
            _benchBackground.color = PlaceholderPalette.PanelBackground;
            benchGo.GetComponent<Button>().onClick.AddListener(() => TryCollectBench());

            _benchEmoji = CreateText(benchGo.transform, "Emoji", "🛠️", 44, TextAnchor.MiddleCenter);
            SetRectStretch(_benchEmoji.rectTransform, new Vector2(0f, 0.4f), Vector2.one);

            _benchLabel = CreateText(benchGo.transform, "Label", "", 18, TextAnchor.MiddleCenter);
            SetRectStretch(_benchLabel.rectTransform, Vector2.zero, new Vector2(1f, 0.4f));

            Text recipeTitle = CreateText(parent, "RecipeTitle", "1차 가공 (원료 → 재료, 가치 ×5)", 18, TextAnchor.MiddleCenter);
            SetRect(recipeTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-160f, -330f), new Vector2(420f, 26f), new Vector2(0.5f, 1f));

            var listGo = new GameObject("RecipeList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(parent, false);
            SetRect((RectTransform)listGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-160f, -362f), new Vector2(420f, 232f), new Vector2(0.5f, 1f));
            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            foreach (MaterialData material in recipeOptions)
            {
                if (material == null)
                    continue;
                MaterialData captured = material;
                string sourceLabel = material.sourceItem != null
                    ? $"{material.sourceItem.displayEmoji} {material.sourceItem.displayName} ×{material.sourceCount}"
                    : "(원료 없음)";
                Button button = CreateButton(listGo.transform, $"Recipe_{material.id}",
                    $"{material.displayEmoji} {material.displayName} ← {sourceLabel} ({material.processingSeconds:0}초)",
                    PlaceholderPalette.ForComposition(material.composition),
                    () => TryStartRecipe(captured));
                button.GetComponentInChildren<Text>().fontSize = 17;
                _recipeButtons.Add((button, material));
            }
        }

        void BuildShopPanel(Transform parent)
        {
            _customerLabel = CreateText(parent, "CustomerLabel", "", 20, TextAnchor.MiddleCenter);
            SetRect(_customerLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-160f, -140f), new Vector2(420f, 30f), new Vector2(0.5f, 1f));

            var rowGo = new GameObject("DisplayRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent, false);
            SetRect((RectTransform)rowGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-160f, -184f), new Vector2(430f, 140f), new Vector2(0.5f, 1f));
            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (int i = 0; i < Shop.DisplaySlotCount; i++)
            {
                int slotIndex = i;
                var slotGo = new GameObject($"Display{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                slotGo.transform.SetParent(rowGo.transform, false);
                var background = slotGo.GetComponent<Image>();
                background.color = PlaceholderPalette.PanelBackground;
                slotGo.GetComponent<Button>().onClick.AddListener(() => OnShopSlotClicked(slotIndex));

                Text emoji = CreateText(slotGo.transform, "Emoji", "➕", 34, TextAnchor.MiddleCenter);
                SetRectStretch(emoji.rectTransform, new Vector2(0f, 0.38f), Vector2.one);

                Text label = CreateText(slotGo.transform, "Label", "빈 진열대", 14, TextAnchor.MiddleCenter);
                SetRectStretch(label.rectTransform, Vector2.zero, new Vector2(1f, 0.38f));

                _shopSlotWidgets.Add(new SlotWidget { Index = slotIndex, Background = background, Emoji = emoji, Label = label });
            }

            Text hint = CreateText(parent, "Hint", "빈 칸 클릭 = 진열 · 찬 칸 클릭 = 회수 (손님 1명 = 한 종류 최대 5개 구매)",
                15, TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-160f, -334f), new Vector2(560f, 24f), new Vector2(0.5f, 1f));

            _saleLogLabel = CreateText(parent, "SaleLog", "", 17, TextAnchor.UpperCenter);
            SetRect(_saleLogLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-160f, -368f), new Vector2(480f, 180f), new Vector2(0.5f, 1f));
        }

        void BuildInventoryPanel(Transform parent)
        {
            var panelGo = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            panelGo.GetComponent<Image>().color = PlaceholderPalette.PanelBackground;
            SetRect((RectTransform)panelGo.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f, -20f), new Vector2(300f, 480f), new Vector2(1f, 0.5f));

            Text title = CreateText(panelGo.transform, "Title", "🧺 창고", 22, TextAnchor.MiddleCenter);
            SetRectStretch(title.rectTransform, new Vector2(0f, 0.9f), Vector2.one);

            _inventoryLabel = CreateText(panelGo.transform, "Items", "(비어 있음)", 17, TextAnchor.UpperLeft);
            SetRectStretch(_inventoryLabel.rectTransform, new Vector2(0.06f, 0.03f), new Vector2(0.97f, 0.88f));
        }

        void BuildSeedPanel(Transform parent)
        {
            _seedPanel = BuildModalPanel(parent, "SeedPanel", "종자 선택", "SeedList", CloseSeedPanel);
        }

        void BuildDisplayPanel(Transform parent)
        {
            _displayPanel = BuildModalPanel(parent, "DisplayPanel", "무엇을 진열할까요? (최대 10개)", "ItemList", CloseDisplayPanel);
        }

        GameObject BuildModalPanel(Transform parent, string name, string titleText, string listName,
            UnityEngine.Events.UnityAction onCancel)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = PlaceholderPalette.PanelBackground;
            SetRect((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 460f), new Vector2(0.5f, 0.5f));

            Text title = CreateText(panel.transform, "Title", titleText, 22, TextAnchor.MiddleCenter);
            SetRectStretch(title.rectTransform, new Vector2(0f, 0.9f), Vector2.one);

            var listGo = new GameObject(listName, typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panel.transform, false);
            SetRectStretch((RectTransform)listGo.transform, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.88f));
            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            Button cancel = CreateButton(panel.transform, "Cancel", "취소", PlaceholderPalette.Neutral, onCancel);
            SetRect((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 30f), new Vector2(160f, 40f), new Vector2(0.5f, 0f));

            panel.SetActive(false);
            return panel;
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
