using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using WizardGarden.Core;
using WizardGarden.Data;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WizardGarden
{
    /// <summary>
    /// 맵 프레젠테이션 (S04b — Rusty's Retirement형 공간 화면). 정원·공방·상점을
    /// 월드 스페이스(SpriteRenderer + TextMesh)에 배치하고, 커서 클릭을 레이캐스트로
    /// 받아 기존 GameSession API로만 라우팅한다. 상태는 매 프레임 폴링 반영 (S02 방침).
    /// 구 GameScreen(탭 UI)은 디버그 화면 — F12 또는 메뉴로 토글.
    /// </summary>
    public sealed class MapScreen : MonoBehaviour
    {
        /// <summary>진열 1회당 인벤토리 → 진열대 이동 수량 상한 (S04 UI 결정 승계).</summary>
        public const int DisplayMoveCount = 10;

        [Tooltip("종자 선택지 (티어1 4종 + 해금 종자 — SO 참조)")]
        public List<PlantData> seedOptions = new List<PlantData>();

        [Tooltip("작업대 1차 가공 레시피 (마른 잎 4종 — MaterialData SO 참조)")]
        public List<MaterialData> recipeOptions = new List<MaterialData>();

        // ---- 맵 구도 상수 (가로형 — 16:9 · ortho size 5 기준, 미니 모드 대비) ----
        const float MapHalfWidth = 8.9f;
        const int GardenColumns = 4;
        const float TileSize = 1.5f;
        const float TileSpacing = 0.25f;
        static readonly Vector2 GardenCenter = new Vector2(-4.4f, -0.4f);
        static readonly Vector2 BenchCenter = new Vector2(3.1f, 1.7f);
        static readonly Vector2 StandRowCenter = new Vector2(4.6f, -2.2f);
        const float StandSpacing = 1.7f;
        static readonly Vector2 CustomerSpot = new Vector2(1.6f, -3.35f); // 상점 앞 통로 (텍스트 겹침 없는 위치)

        GameSession _session;
        Camera _camera;
        MapHud _hud;
        MapPopup _popup;
        GameObject _debugScreenGo;

        readonly Dictionary<string, ItemData> _itemsById = new Dictionary<string, ItemData>();
        readonly Dictionary<string, PlantData> _plantsById = new Dictionary<string, PlantData>();
        readonly Dictionary<string, MaterialData> _materialsById = new Dictionary<string, MaterialData>();
        readonly List<Shop.SaleRecord> _salesBuffer = new List<Shop.SaleRecord>();
        readonly List<MapPopup.Entry> _entriesBuffer = new List<MapPopup.Entry>();

        sealed class TileWidget
        {
            public SpriteRenderer Soil;
            public SpriteRenderer Plant;
            public TextMesh Emoji;
            public TextMesh Label;
        }

        sealed class StationWidget
        {
            public SpriteRenderer Body;
            public TextMesh Emoji;
            public TextMesh Label;
        }

        TileWidget[] _gardenTiles;
        StationWidget _bench;
        StationWidget[] _stands;
        TextMesh _customerCountdown;

        void Start()
        {
            _session = GameClockRunner.Instance != null ? GameClockRunner.Instance.Session : null;
            if (_session == null)
            {
                Debug.LogError("[MapScreen] GameClockRunner 세션 없음 — 화면 비활성");
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

            SetupCamera();
            BuildMap();

            _hud = new GameObject("MapHud").AddComponent<MapHud>();
            _hud.transform.SetParent(transform, false);
            _popup = new GameObject("MapPopup").AddComponent<MapPopup>();
            _popup.transform.SetParent(transform, false);

            GameScreen debugScreen = Object.FindFirstObjectByType<GameScreen>(FindObjectsInactive.Include);
            _debugScreenGo = debugScreen != null ? debugScreen.gameObject : null;

            _session.Inventory.Changed += RefreshInventoryHud;
            _session.Wallet.Changed += RefreshGoldHud;
            RefreshInventoryHud();
            RefreshGoldHud();
        }

        void OnDestroy()
        {
            if (_session == null)
                return;
            _session.Inventory.Changed -= RefreshInventoryHud;
            _session.Wallet.Changed -= RefreshGoldHud;
        }

        void Update()
        {
            if (_session == null)
                return;

            double now = _session.Clock.ResourceSeconds;

            _salesBuffer.Clear();
            _session.Shop.TickCustomers(now, ResolvePrice, _session.Wallet, _salesBuffer);
            for (int i = 0; i < _salesBuffer.Count; i++)
            {
                Shop.SaleRecord sale = _salesBuffer[i];
                Vector3 fxPosition = new Vector3(CustomerSpot.x - i * 0.85f, CustomerSpot.y, 0f);
                MapCustomerFx.Spawn(fxPosition, $"{ItemLabel(sale.ItemId)} ×{sale.Count}  +{sale.Gold}G");
            }

            HandleInput();
            RefreshGardenTiles(now);
            RefreshBench(now);
            RefreshStands();
            RefreshCustomerCountdown(now);
            RefreshClockHud();
        }

        // ---- 입력 (커서 지시형 — 중앙 레이캐스트) ----

        void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
                ToggleDebugScreen();

            if (IsDebugScreenActive || _popup.IsOpen || _camera == null)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 world = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
            HandleWorldClick(world);
#else
            if (Input.GetKeyDown(KeyCode.F12))
                ToggleDebugScreen();

            if (IsDebugScreenActive || _popup.IsOpen || _camera == null)
                return;
            if (!Input.GetMouseButtonDown(0))
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 world = _camera.ScreenToWorldPoint(Input.mousePosition);
            HandleWorldClick(world);
#endif
        }

        /// <summary>월드 좌표 클릭 처리 (마우스·스모크 테스트 공용 경로). 대상이 없으면 false.</summary>
        public bool HandleWorldClick(Vector2 worldPosition)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            if (hit == null)
                return false;

            MapTile tile = hit.GetComponentInParent<MapTile>();
            if (tile == null)
                return false;

            switch (tile.kind)
            {
                case MapTile.Kind.GardenTile:
                    OnGardenTileClicked(tile.index);
                    return true;
                case MapTile.Kind.Bench:
                    OnBenchClicked();
                    return true;
                case MapTile.Kind.ShopSlot:
                    OnShopSlotClicked(tile.index);
                    return true;
                default:
                    return false;
            }
        }

        void OnGardenTileClicked(int index)
        {
            if (index >= _session.Garden.SlotCount)
            {
                OpenExpandPopup();
                return;
            }

            GardenSlot slot = _session.Garden.Slots[index];
            if (slot.IsEmpty)
            {
                OpenSeedPopup(index);
                return;
            }

            double now = _session.Clock.ResourceSeconds;
            if (slot.IsMature(now, GetGrowthSeconds(slot.PlantId)))
                _session.TryHarvestToInventory(index, GetGrowthSeconds(slot.PlantId));
        }

        void OnBenchClicked()
        {
            Workshop workshop = _session.Workshop;
            if (workshop.IsIdle)
            {
                OpenRecipePopup();
                return;
            }

            double now = _session.Clock.ResourceSeconds;
            double processingSeconds = GetProcessingSeconds(workshop.OutputItemId);
            if (workshop.IsComplete(now, processingSeconds))
                workshop.TryCollect(now, processingSeconds, _session.Inventory, out _, out _);
        }

        void OnShopSlotClicked(int index)
        {
            if (!_session.Shop.IsValidIndex(index))
                return;

            if (_session.Shop.Slots[index].IsEmpty)
                OpenDisplayPopup(index);
            else
                _session.Shop.TryTakeBack(index, _session.Inventory);
        }

        // ---- 팝업 (전부 GameSession API 호출로만 동작) ----

        void OpenSeedPopup(int slotIndex)
        {
            _entriesBuffer.Clear();
            foreach (PlantData plant in seedOptions)
            {
                if (plant == null)
                    continue;
                PlantData captured = plant;

                if (IsSeedLocked(plant))
                {
                    _entriesBuffer.Add(new MapPopup.Entry(
                        $"🔒 {plant.displayEmoji} {plant.displayName} — {plant.unlockCost}G 해금",
                        PlaceholderPalette.Neutral,
                        _session.Wallet.CanAfford(plant.unlockCost),
                        () =>
                        {
                            if (_session.TryPurchaseUnlock(captured.id, captured.unlockCost))
                                OpenSeedPopup(slotIndex); // 해금 반영 재구성
                        }));
                    continue;
                }

                _entriesBuffer.Add(new MapPopup.Entry(
                    $"{plant.displayEmoji} {plant.displayName} ({plant.growthSeconds:0}초)",
                    PlaceholderPalette.ForComposition(plant.composition),
                    true,
                    () => _session.TryPlant(slotIndex, captured.id)));
            }

            _popup.Open($"종자 선택 — 밭 {slotIndex + 1}번", _entriesBuffer);
        }

        void OpenExpandPopup()
        {
            if (_session.Garden.SlotCount >= Garden.MaxSlotCount)
                return;

            int cost = _session.NextGardenSlotCost;
            _entriesBuffer.Clear();
            _entriesBuffer.Add(new MapPopup.Entry(
                $"➕ 밭 확장 — {cost}G",
                PlaceholderPalette.Earth,
                _session.Wallet.CanAfford(cost),
                () => _session.TryBuyGardenSlot()));

            _popup.Open($"잠긴 밭 (보유 {_session.Wallet.Gold:N0}G)", _entriesBuffer);
        }

        void OpenRecipePopup()
        {
            _entriesBuffer.Clear();
            foreach (MaterialData material in recipeOptions)
            {
                if (material == null)
                    continue;
                MaterialData captured = material;
                string sourceLabel = material.sourceItem != null
                    ? $"{material.sourceItem.displayEmoji} {material.sourceItem.displayName} ×{material.sourceCount}"
                    : "(원료 없음)";
                bool hasSource = material.sourceItem != null
                    && _session.Inventory.GetCount(material.sourceItem.id) >= material.sourceCount;

                _entriesBuffer.Add(new MapPopup.Entry(
                    $"{material.displayEmoji} {material.displayName} ← {sourceLabel} ({material.processingSeconds:0}초)",
                    PlaceholderPalette.ForComposition(material.composition),
                    hasSource,
                    () => TryStartRecipe(captured)));
            }

            _popup.Open("가공 선택 (1차 — 가치 ×5)", _entriesBuffer);
        }

        void OpenDisplayPopup(int shopSlotIndex)
        {
            _entriesBuffer.Clear();
            foreach (KeyValuePair<string, int> entry in _session.Inventory.Entries)
            {
                int price = ResolvePrice(entry.Key);
                if (price <= 0)
                    continue;
                string capturedId = entry.Key;
                _itemsById.TryGetValue(entry.Key, out ItemData item);
                _entriesBuffer.Add(new MapPopup.Entry(
                    $"{ItemLabel(entry.Key)} ×{entry.Value} (개당 {price}G)",
                    item != null ? PlaceholderPalette.ForComposition(item.composition) : PlaceholderPalette.Neutral,
                    true,
                    () => _session.Shop.Display(shopSlotIndex, capturedId, DisplayMoveCount, _session.Inventory)));
            }

            if (_entriesBuffer.Count == 0)
                _entriesBuffer.Add(new MapPopup.Entry(
                    "(팔 물건 없음 — 정원에서 수확하세요)", PlaceholderPalette.PanelBackground, false, null));

            _popup.Open($"무엇을 진열할까요? (최대 {DisplayMoveCount}개)", _entriesBuffer);
        }

        bool TryStartRecipe(MaterialData material)
        {
            if (material == null || material.sourceItem == null)
                return false;
            return _session.Workshop.TryStart(material.id, 1, material.sourceItem.id, material.sourceCount,
                _session.Inventory, _session.Clock.ResourceSeconds);
        }

        bool IsSeedLocked(PlantData plant)
        {
            return plant != null && plant.unlockCost > 0 && !_session.Unlocks.IsUnlocked(plant.id);
        }

        // ---- 디버그 화면 (구 GameScreen 탭 UI — 강등) ----

        public bool IsDebugScreenActive => _debugScreenGo != null && _debugScreenGo.activeSelf;

        public void ToggleDebugScreen()
        {
            if (_debugScreenGo == null)
            {
                Debug.LogWarning("[MapScreen] 디버그 화면(GameScreen) 없음 — Setup Map Scene (S04b) 실행 확인");
                return;
            }
            _debugScreenGo.SetActive(!_debugScreenGo.activeSelf);
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

            Debug.LogWarning($"[MapScreen] 알 수 없는 식물 id '{plantId}' — 즉시 수확 가능으로 처리");
            return 0.0;
        }

        double GetProcessingSeconds(string materialId)
        {
            if (_materialsById.TryGetValue(materialId, out MaterialData material))
                return material.processingSeconds;

            Debug.LogWarning($"[MapScreen] 알 수 없는 재료 id '{materialId}' — 즉시 완료로 처리");
            return 0.0;
        }

        // ---- 갱신 (매 프레임 현재 상태 폴링) ----

        void RefreshGardenTiles(double now)
        {
            int unlockedCount = _session.Garden.SlotCount;
            int nextCost = unlockedCount < Garden.MaxSlotCount ? _session.NextGardenSlotCost : 0;

            for (int i = 0; i < _gardenTiles.Length; i++)
            {
                TileWidget tile = _gardenTiles[i];

                if (i >= unlockedCount)
                {
                    tile.Soil.color = new Color(0.15f, 0.12f, 0.10f);
                    tile.Plant.enabled = false;
                    tile.Emoji.text = "🔒";
                    tile.Emoji.color = new Color(1f, 1f, 1f, 0.45f);
                    if (i == unlockedCount)
                    {
                        tile.Label.text = $"확장 {nextCost}G";
                        tile.Label.color = Color.white;
                    }
                    else
                    {
                        tile.Label.text = "";
                    }
                    continue;
                }

                tile.Soil.color = PlaceholderPalette.EmptySoil;
                tile.Emoji.color = Color.white;
                tile.Label.color = Color.white;

                GardenSlot slot = _session.Garden.Slots[i];
                if (slot.IsEmpty)
                {
                    tile.Plant.enabled = false;
                    tile.Emoji.text = "";
                    tile.Label.text = "빈 밭";
                    continue;
                }

                _plantsById.TryGetValue(slot.PlantId, out PlantData plant);
                Color elementColor = plant != null
                    ? PlaceholderPalette.ForComposition(plant.composition)
                    : PlaceholderPalette.Neutral;
                string plantEmoji = plant != null ? plant.displayEmoji : "❓";

                double progress = slot.GetProgress(now, plant != null ? plant.growthSeconds : 0.0);
                int percent = (int)(progress * 100.0);
                tile.Plant.enabled = true;

                switch (GrowthStageUtility.FromProgress(progress))
                {
                    case GrowthStage.Sprout:
                        tile.Plant.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                        tile.Plant.color = Color.Lerp(PlaceholderPalette.EmptySoil, elementColor, 0.5f);
                        tile.Emoji.text = "🌱";
                        tile.Label.text = $"{percent}%";
                        break;
                    case GrowthStage.Growing:
                        tile.Plant.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                        tile.Plant.color = Color.Lerp(PlaceholderPalette.EmptySoil, elementColor, 0.8f);
                        tile.Emoji.text = plantEmoji;
                        tile.Label.text = $"{percent}%";
                        break;
                    default:
                        tile.Plant.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
                        tile.Plant.color = elementColor;
                        tile.Emoji.text = plantEmoji;
                        tile.Label.text = "수확!";
                        break;
                }
            }
        }

        void RefreshBench(double now)
        {
            Workshop workshop = _session.Workshop;

            if (workshop.IsIdle)
            {
                _bench.Body.color = new Color(0.42f, 0.30f, 0.19f);
                _bench.Emoji.text = "🛠️";
                _bench.Label.text = "작업대 — 클릭: 가공";
                return;
            }

            _materialsById.TryGetValue(workshop.OutputItemId, out MaterialData material);
            double processingSeconds = material != null ? material.processingSeconds : 0.0;
            Color color = material != null
                ? PlaceholderPalette.ForComposition(material.composition)
                : PlaceholderPalette.Neutral;
            string name = material != null ? material.displayName : workshop.OutputItemId;

            if (workshop.IsComplete(now, processingSeconds))
            {
                _bench.Body.color = color;
                _bench.Emoji.text = material != null ? material.displayEmoji : "❓";
                _bench.Label.text = $"{name} 완료! 클릭: 수령";
            }
            else
            {
                double progress = workshop.GetProgress(now, processingSeconds);
                _bench.Body.color = Color.Lerp(new Color(0.42f, 0.30f, 0.19f), color, 0.5f);
                _bench.Emoji.text = "⚗️";
                _bench.Label.text = $"{name} {(int)(progress * 100.0)}%";
            }
        }

        void RefreshStands()
        {
            for (int i = 0; i < _stands.Length; i++)
            {
                StationWidget stand = _stands[i];
                Shop.DisplaySlot slot = _session.Shop.Slots[i];

                if (slot.IsEmpty)
                {
                    stand.Body.color = new Color(0.40f, 0.32f, 0.23f);
                    stand.Emoji.text = "➕";
                    stand.Label.text = "진열";
                    continue;
                }

                _itemsById.TryGetValue(slot.ItemId, out ItemData item);
                stand.Body.color = item != null
                    ? PlaceholderPalette.ForComposition(item.composition)
                    : PlaceholderPalette.Neutral;
                stand.Emoji.text = item != null ? item.displayEmoji : "❓";
                string name = item != null ? item.displayName : slot.ItemId;
                stand.Label.text = $"{name} ×{slot.Count}\n개당 {ResolvePrice(slot.ItemId)}G";
            }
        }

        void RefreshCustomerCountdown(double now)
        {
            bool anyDisplayed = false;
            foreach (Shop.DisplaySlot slot in _session.Shop.Slots)
            {
                if (!slot.IsEmpty)
                {
                    anyDisplayed = true;
                    break;
                }
            }

            _customerCountdown.text = anyDisplayed
                ? $"🚶 다음 손님 {_session.Shop.SecondsUntilNextCustomer(now):0}초"
                : "🚶 손님 대기 — 진열대가 비었어요";
        }

        void RefreshClockHud()
        {
            GameClock clock = _session.Clock;
            int hour = (int)clock.HourOfDay;
            int minute = (int)((clock.HourOfDay - hour) * 60.0);
            _hud.SetClock($"{clock.DayIndex}일차 {hour:00}:{minute:00} ({TimeOfDayLabel(clock.CurrentTimeOfDay)})");
        }

        void RefreshGoldHud()
        {
            if (_hud != null)
                _hud.SetGold($"💰 {_session.Wallet.Gold:N0}G");
        }

        void RefreshInventoryHud()
        {
            if (_hud == null)
                return;

            var builder = new StringBuilder();
            foreach (KeyValuePair<string, int> entry in _session.Inventory.Entries)
                builder.AppendLine($"{ItemLabel(entry.Key)} ×{entry.Value}");
            _hud.SetInventory(builder.Length > 0 ? builder.ToString() : "(비어 있음)");
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

        // ---- 맵 생성 (플레이스홀더 — 색 사각형 + 이모지/한글 라벨) ----

        void SetupCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
                _camera = Object.FindFirstObjectByType<Camera>();
            if (_camera == null)
                return;

            // 좁은 화면비에서도 가로 구도 전체가 보이게 세로를 늘린다
            float halfWidth = _camera.orthographicSize * _camera.aspect;
            if (halfWidth < MapHalfWidth)
                _camera.orthographicSize = MapHalfWidth / _camera.aspect;
        }

        void BuildMap()
        {
            MapPlaceholderFactory.CreateSquare(transform, "Ground", new Vector2(19f, 10.6f),
                new Color(0.20f, 0.30f, 0.17f), -100);

            BuildGardenZone();
            BuildWorkshopZone();
            BuildShopZone();
            BuildProps();
        }

        void BuildGardenZone()
        {
            int rows = Garden.MaxSlotCount / GardenColumns;
            float gridWidth = GardenColumns * TileSize + (GardenColumns - 1) * TileSpacing;
            float gridHeight = rows * TileSize + (rows - 1) * TileSpacing;

            MapPlaceholderFactory.CreateSquare(transform, "GardenPatch",
                new Vector2(gridWidth + 0.7f, gridHeight + 0.7f), new Color(0.16f, 0.13f, 0.10f), -50,
                new Vector3(GardenCenter.x, GardenCenter.y, 0f));
            MapPlaceholderFactory.CreateText(transform, "GardenLabel", "🌱 정원", 48, 0.12f, Color.white, -40,
                new Vector3(GardenCenter.x, GardenCenter.y + gridHeight * 0.5f + 0.65f, 0f));

            _gardenTiles = new TileWidget[Garden.MaxSlotCount];
            for (int i = 0; i < Garden.MaxSlotCount; i++)
            {
                Vector3 position = GardenTileWorldPosition(i);
                var tileGo = new GameObject($"GardenTile{i}");
                tileGo.transform.SetParent(transform, false);
                tileGo.transform.localPosition = position;

                var marker = tileGo.AddComponent<MapTile>();
                marker.kind = MapTile.Kind.GardenTile;
                marker.index = i;
                var collider = tileGo.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(TileSize, TileSize);

                var widget = new TileWidget
                {
                    Soil = MapPlaceholderFactory.CreateSquare(tileGo.transform, "Soil",
                        new Vector2(TileSize, TileSize), PlaceholderPalette.EmptySoil, 0),
                    Plant = MapPlaceholderFactory.CreateSquare(tileGo.transform, "Plant",
                        Vector2.one, PlaceholderPalette.Neutral, 4, new Vector3(0f, 0.08f, 0f)),
                    Emoji = MapPlaceholderFactory.CreateText(tileGo.transform, "Emoji", "", 64, 0.10f,
                        Color.white, 8, new Vector3(0f, 0.12f, 0f)),
                    Label = MapPlaceholderFactory.CreateText(tileGo.transform, "Label", "", 40, 0.055f,
                        Color.white, 8, new Vector3(0f, -0.52f, 0f))
                };
                widget.Plant.enabled = false;
                _gardenTiles[i] = widget;
            }
        }

        void BuildWorkshopZone()
        {
            MapPlaceholderFactory.CreateSquare(transform, "WorkshopPatch", new Vector2(4.4f, 3.4f),
                new Color(0.24f, 0.23f, 0.26f), -50, new Vector3(BenchCenter.x, BenchCenter.y - 0.1f, 0f));
            MapPlaceholderFactory.CreateText(transform, "WorkshopLabel", "⚗️ 공방", 48, 0.12f, Color.white, -40,
                new Vector3(BenchCenter.x, BenchCenter.y + 1.95f, 0f));

            var benchGo = new GameObject("Bench");
            benchGo.transform.SetParent(transform, false);
            benchGo.transform.localPosition = new Vector3(BenchCenter.x, BenchCenter.y, 0f);
            var marker = benchGo.AddComponent<MapTile>();
            marker.kind = MapTile.Kind.Bench;
            var collider = benchGo.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2.0f, 1.4f);

            _bench = new StationWidget
            {
                Body = MapPlaceholderFactory.CreateSquare(benchGo.transform, "Body", new Vector2(2.0f, 1.4f),
                    new Color(0.42f, 0.30f, 0.19f), 0),
                Emoji = MapPlaceholderFactory.CreateText(benchGo.transform, "Emoji", "🛠️", 64, 0.09f,
                    Color.white, 8, new Vector3(0f, 0.1f, 0f)),
                Label = MapPlaceholderFactory.CreateText(benchGo.transform, "Label", "", 40, 0.06f,
                    Color.white, 8, new Vector3(0f, -1.0f, 0f))
            };
        }

        void BuildShopZone()
        {
            MapPlaceholderFactory.CreateSquare(transform, "ShopPatch", new Vector2(5.6f, 3.4f),
                new Color(0.30f, 0.22f, 0.15f), -50, new Vector3(StandRowCenter.x, StandRowCenter.y - 0.4f, 0f));
            MapPlaceholderFactory.CreateText(transform, "ShopLabel", "🏪 상점", 48, 0.12f, Color.white, -40,
                new Vector3(StandRowCenter.x, StandRowCenter.y + 1.6f, 0f));

            _stands = new StationWidget[Shop.DisplaySlotCount];
            for (int i = 0; i < Shop.DisplaySlotCount; i++)
            {
                Vector3 position = ShopSlotWorldPosition(i);
                var standGo = new GameObject($"Stand{i}");
                standGo.transform.SetParent(transform, false);
                standGo.transform.localPosition = position;
                var marker = standGo.AddComponent<MapTile>();
                marker.kind = MapTile.Kind.ShopSlot;
                marker.index = i;
                var collider = standGo.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1.4f, 1.1f);

                _stands[i] = new StationWidget
                {
                    Body = MapPlaceholderFactory.CreateSquare(standGo.transform, "Body", new Vector2(1.4f, 1.1f),
                        new Color(0.40f, 0.32f, 0.23f), 0),
                    Emoji = MapPlaceholderFactory.CreateText(standGo.transform, "Emoji", "➕", 56, 0.08f,
                        Color.white, 8, new Vector3(0f, 0.08f, 0f)),
                    Label = MapPlaceholderFactory.CreateText(standGo.transform, "Label", "진열", 36, 0.05f,
                        Color.white, 8, new Vector3(0f, -0.88f, 0f))
                };
            }

            _customerCountdown = MapPlaceholderFactory.CreateText(transform, "CustomerCountdown", "", 40, 0.055f,
                new Color(1f, 1f, 1f, 0.9f), 8, new Vector3(StandRowCenter.x, -3.6f, 0f));
        }

        void BuildProps()
        {
            var tree = MapPlaceholderFactory.CreateSquare(transform, "PropTree", new Vector2(1.0f, 1.4f),
                new Color(0.13f, 0.25f, 0.14f), -30, new Vector3(8.0f, -3.6f, 0f));
            MapPlaceholderFactory.CreateText(tree.transform, "Emoji", "🌲", 64, 0.10f, Color.white, -29);

            var flower = MapPlaceholderFactory.CreateSquare(transform, "PropFlower", new Vector2(0.6f, 0.6f),
                new Color(0.24f, 0.34f, 0.19f), -30, new Vector3(0.9f, -3.9f, 0f));
            MapPlaceholderFactory.CreateText(flower.transform, "Emoji", "🌼", 48, 0.09f, Color.white, -29);

            var herb = MapPlaceholderFactory.CreateSquare(transform, "PropHerb", new Vector2(0.6f, 0.6f),
                new Color(0.24f, 0.34f, 0.19f), -30, new Vector3(0.3f, 3.9f, 0f));
            MapPlaceholderFactory.CreateText(herb.transform, "Emoji", "🌿", 48, 0.09f, Color.white, -29);
        }

        // ---- 맵 좌표 (스모크 테스트 공용) ----

        /// <summary>밭 타일 월드 좌표 (0~MaxSlotCount-1, 좌상단부터 행 우선).</summary>
        public Vector3 GardenTileWorldPosition(int index)
        {
            int rows = Garden.MaxSlotCount / GardenColumns;
            float gridWidth = GardenColumns * TileSize + (GardenColumns - 1) * TileSpacing;
            float gridHeight = rows * TileSize + (rows - 1) * TileSpacing;
            int row = index / GardenColumns;
            int col = index % GardenColumns;
            float left = GardenCenter.x - (gridWidth - TileSize) * 0.5f;
            float top = GardenCenter.y + (gridHeight - TileSize) * 0.5f;
            return new Vector3(left + col * (TileSize + TileSpacing), top - row * (TileSize + TileSpacing), 0f);
        }

        /// <summary>작업대 월드 좌표.</summary>
        public Vector3 BenchWorldPosition => new Vector3(BenchCenter.x, BenchCenter.y, 0f);

        /// <summary>진열대 월드 좌표 (0~2).</summary>
        public Vector3 ShopSlotWorldPosition(int index)
        {
            return new Vector3(StandRowCenter.x + (index - 1) * StandSpacing, StandRowCenter.y, 0f);
        }

        /// <summary>모달 팝업 (스모크 테스트가 항목을 실행할 때 사용).</summary>
        public MapPopup Popup => _popup;
    }
}
