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

        [Tooltip("발견 가능한 포션 (S06 authoring분 — 조합 매칭·판매·도감 분모)")]
        public List<PotionData> potionOptions = new List<PotionData>();

        [Tooltip("실패 부산물 3종 (탁한 포션/수상한 침전물/희뿌연 안개병 — 실험 일지)")]
        public List<PotionData> byproductOptions = new List<PotionData>();

        [Tooltip("맵 아트 (A02 — 비어 있으면 색 사각형 플레이스홀더로 동작)")]
        public MapArtSet art = new MapArtSet();

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
        static readonly Vector2 CauldronCenter = new Vector2(0.1f, 1.6f);  // 상단 중앙 빈 공간 (정원·공방 사이)
        static readonly Vector2 CodexCenter = new Vector2(0.1f, 3.55f);

        GameSession _session;
        Camera _camera;
        MapHud _hud;
        MapPopup _popup;
        GameObject _debugScreenGo;

        // S06 조합·도감
        Codex _codex;
        BrewMatcher _matcher;
        BrewStation _brewStation;
        BrewWindow _brewWindow;
        CodexWindow _codexWindow;
        OfflineSummaryWindow _offlineSummary;
        CodexWindow.Page _codexPage = CodexWindow.Page.Potions;
        string _brewResultText = "";

        readonly Dictionary<string, ItemData> _itemsById = new Dictionary<string, ItemData>();
        readonly Dictionary<string, PlantData> _plantsById = new Dictionary<string, PlantData>();
        readonly Dictionary<string, MaterialData> _materialsById = new Dictionary<string, MaterialData>();
        readonly List<Shop.SaleRecord> _salesBuffer = new List<Shop.SaleRecord>();
        readonly List<MapPopup.Entry> _entriesBuffer = new List<MapPopup.Entry>();

        // S06 조합 상태·버퍼
        readonly Dictionary<string, int> _brewSelection = new Dictionary<string, int>(System.StringComparer.Ordinal);
        readonly HashSet<string> _potionIdSet = new HashSet<string>(System.StringComparer.Ordinal);
        readonly Dictionary<Core.Element, string> _elementUnitIngredient = new Dictionary<Core.Element, string>();
        readonly List<PotionData> _potionCatalog = new List<PotionData>();
        readonly List<PotionData> _byproductCatalog = new List<PotionData>();
        readonly List<BrewInputItem> _brewInputBuffer = new List<BrewInputItem>();
        readonly List<BrewWindow.IngredientRow> _brewRowBuffer = new List<BrewWindow.IngredientRow>();
        readonly List<CodexWindow.Row> _codexRowBuffer = new List<CodexWindow.Row>();

        sealed class TileWidget
        {
            public SpriteRenderer Soil;
            public SpriteRenderer Plant;
            public TextMesh Emoji;
            public TextMesh Label;
            public bool SoilIsArt;

            /// <summary>흙 칸 색 — 아트 스프라이트면 색을 덮지 않고 명암만 준다.</summary>
            public void SetSoilColor(Color placeholderColor, float artBrightness)
            {
                Soil.color = SoilIsArt
                    ? new Color(artBrightness, artBrightness, artBrightness)
                    : placeholderColor;
            }
        }

        sealed class StationWidget
        {
            public SpriteRenderer Body;
            public TextMesh Emoji;
            public TextMesh Label;
            public bool BodyIsArt;

            /// <summary>시설 색 — 아트 스프라이트면 원소색으로 물들이지 않는다(상태는 라벨이 전달).</summary>
            public void SetBodyColor(Color placeholderColor)
            {
                Body.color = BodyIsArt ? Color.white : placeholderColor;
            }
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

            RegisterPotionCatalog();
            BuildElementUnitMap();
            _matcher = new BrewMatcher(
                BrewRecipeFactory.ToRecipes(_potionCatalog),
                BrewRecipeFactory.BuildByproducts(
                    FindByproduct(BrewRecipeFactory.MurkyId),
                    FindByproduct(BrewRecipeFactory.SedimentId),
                    FindByproduct(BrewRecipeFactory.MistId)));
            _brewStation = new BrewStation(_matcher, _codex);

            SetupCamera();
            BuildMap();

            _hud = new GameObject("MapHud").AddComponent<MapHud>();
            _hud.transform.SetParent(transform, false);
            _popup = new GameObject("MapPopup").AddComponent<MapPopup>();
            _popup.transform.SetParent(transform, false);

            _brewWindow = new GameObject("BrewWindow").AddComponent<BrewWindow>();
            _brewWindow.transform.SetParent(transform, false);
            _brewWindow.OnBrew = () => BrewExecute();
            _brewWindow.OnClear = BrewClear;

            _codexWindow = new GameObject("CodexWindow").AddComponent<CodexWindow>();
            _codexWindow.transform.SetParent(transform, false);
            _codexWindow.OnSelectPotions = () => SetCodexPage(CodexWindow.Page.Potions);
            _codexWindow.OnSelectJournal = () => SetCodexPage(CodexWindow.Page.Journal);

            _offlineSummary = new GameObject("OfflineSummaryWindow").AddComponent<OfflineSummaryWindow>();
            _offlineSummary.transform.SetParent(transform, false);

            GameScreen debugScreen = Object.FindFirstObjectByType<GameScreen>(FindObjectsInactive.Include);
            _debugScreenGo = debugScreen != null ? debugScreen.gameObject : null;

            _session.Inventory.Changed += RefreshInventoryHud;
            _session.Wallet.Changed += RefreshGoldHud;
            _codex.Changed += RefreshCodexHud;
            RefreshInventoryHud();
            RefreshGoldHud();
            RefreshCodexHud();

            RunOfflineSettlement();
        }

        void OnDestroy()
        {
            if (_session == null)
                return;
            _session.Inventory.Changed -= RefreshInventoryHud;
            _session.Wallet.Changed -= RefreshGoldHud;
            if (_codex != null)
                _codex.Changed -= RefreshCodexHud;
        }

        void RegisterPotionCatalog()
        {
            _codex = _session.Codex;
            var byproductIds = new HashSet<string>(System.StringComparer.Ordinal)
            {
                BrewRecipeFactory.MurkyId, BrewRecipeFactory.SedimentId, BrewRecipeFactory.MistId
            };

            foreach (PotionData potion in potionOptions)
            {
                if (potion == null || string.IsNullOrEmpty(potion.id) || byproductIds.Contains(potion.id))
                    continue;
                _potionCatalog.Add(potion);
                _itemsById[potion.id] = potion;
                _potionIdSet.Add(potion.id);
                _codex.RegisterPotion(potion.id);
            }

            foreach (PotionData byproduct in byproductOptions)
            {
                if (byproduct == null || string.IsNullOrEmpty(byproduct.id))
                    continue;
                _byproductCatalog.Add(byproduct);
                _itemsById[byproduct.id] = byproduct;
                _potionIdSet.Add(byproduct.id);
                _codex.RegisterByproduct(byproduct.id);
            }
        }

        // 원소 → 단위(합 1) 재료 id — 재제조 시 조성으로부터 투입 재료를 역산한다.
        // 가공 재료(마른 잎) 우선, 없으면 단일 원소 종자로 대체.
        void BuildElementUnitMap()
        {
            foreach (MaterialData material in recipeOptions)
                TryMapUnit(material);
            foreach (PlantData plant in seedOptions)
                TryMapUnit(plant);
        }

        void TryMapUnit(ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.id))
                return;
            Core.ElementComposition c = item.composition;
            if (c.Total != 1)
                return;
            for (int e = 0; e < Core.ElementComposition.SlotCount; e++)
            {
                var element = (Core.Element)e;
                if (c[element] == 1 && !_elementUnitIngredient.ContainsKey(element))
                    _elementUnitIngredient[element] = item.id;
            }
        }

        PotionData FindByproduct(string id)
        {
            foreach (PotionData byproduct in _byproductCatalog)
                if (byproduct != null && byproduct.id == id)
                    return byproduct;
            return null;
        }

        // ---- S08 오프라인 정산 (복귀 시 자원 시간 정산 + 요약 패널) ----

        /// <summary>
        /// 세이브 복원으로 쌓인 오프라인 자원초를 정산하고(캡 8h·효율 60%·임시 자동화),
        /// 변화가 있으면 복귀 요약 패널을 띄운다. 정산은 PendingOfflineSeconds를 소비 후 비우므로 1회만 실행됨.
        /// (스모크 테스트 공용 진입점 — 결과 요약을 반환.)
        /// </summary>
        public Core.OfflineSettlementResult RunOfflineSettlement()
        {
            if (_session == null)
                return null;
            double raw = _session.PendingOfflineSeconds;
            if (raw <= 0.0)
                return null;

            // ★ 임시 자동화 상수 — S09 견습생 시스템이 FixedOfflineAutomation을 교체한다.
            var automation = new Core.FixedOfflineAutomation();
            var settlement = new Core.OfflineSettlement(automation);
            Core.OfflineSettlementResult result = settlement.Settle(
                _session.Clock, _session.Garden, _session.Inventory, _session.Shop, _session.Wallet,
                raw, GetGrowthSeconds, ResolvePrice, ApplyCodexGoldBonus);
            _session.ClearPendingOfflineSeconds();

            if (result.HasActivity && _offlineSummary != null)
                _offlineSummary.Open("다녀오셨군요! ✨", BuildOfflineSummaryText(result));
            return result;
        }

        static string BuildOfflineSummaryText(Core.OfflineSettlementResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{FormatDuration(r.RawOfflineSeconds)} 동안 정원을 비우셨네요.");
            if (r.WasCapped)
                sb.AppendLine("⏸️ 오프라인은 최대 8시간까지만 정산됩니다.");
            sb.AppendLine("");
            sb.AppendLine($"💰 +{r.GoldEarned:N0}G 를 벌었습니다");
            if (r.TotalHarvested > 0)
                sb.AppendLine($"🌾 {r.TotalHarvested:N0}개 수확 — 보관함에 {r.HarvestedToStorage:N0}개 쌓였어요");
            sb.AppendLine("");
            sb.AppendLine("⏸️ 계절·날씨·VIP·모험은 그대로예요 (여전히 봄 — 사건 시간은 정지)");
            return sb.ToString();
        }

        // 자원 시간 경과 안내용 표기 (현실 시간 기준 raw 경과).
        static string FormatDuration(double seconds)
        {
            if (seconds < 60.0)
                return $"{(int)seconds}초";
            int totalMinutes = (int)(seconds / 60.0);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            if (hours <= 0)
                return $"{minutes}분";
            return minutes > 0 ? $"{hours}시간 {minutes}분" : $"{hours}시간";
        }

        void Update()
        {
            if (_session == null)
                return;

            double now = _session.Clock.ResourceSeconds;

            _salesBuffer.Clear();
            _session.Shop.TickCustomers(now, ResolvePrice, _session.Wallet, _salesBuffer, ApplyCodexGoldBonus);
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

            if (IsDebugScreenActive || AnyModalOpen || _camera == null)
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

            if (IsDebugScreenActive || AnyModalOpen || _camera == null)
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
                case MapTile.Kind.Cauldron:
                    OpenBrewWindow();
                    return true;
                case MapTile.Kind.Codex:
                    OpenCodexWindow();
                    return true;
                default:
                    return false;
            }
        }

        bool AnyModalOpen =>
            (_popup != null && _popup.IsOpen)
            || (_brewWindow != null && _brewWindow.IsOpen)
            || (_codexWindow != null && _codexWindow.IsOpen)
            || (_offlineSummary != null && _offlineSummary.IsOpen);

        long ApplyCodexGoldBonus(long gold) => _codex != null ? _codex.ApplyGoldBonus(gold) : gold;

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

        // ---- S06 조합 (가마솥 자유 투입 → 발견) ----

        /// <summary>가마솥 조합 창 열기 (스모크 테스트 공용 진입점).</summary>
        public void OpenBrewWindow()
        {
            _brewSelection.Clear();
            _brewResultText = "창고 재료를 담아 원소 조성을 맞춰보세요.";
            RefreshBrewWindow(open: true);
        }

        /// <summary>재료 1개 투입(보유량 한도 내).</summary>
        public void BrewAdd(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return;
            _brewSelection.TryGetValue(itemId, out int selected);
            if (selected < _session.Inventory.GetCount(itemId))
                _brewSelection[itemId] = selected + 1;
            RefreshBrewWindow();
        }

        /// <summary>재료 1개 회수.</summary>
        public void BrewRemove(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !_brewSelection.TryGetValue(itemId, out int selected))
                return;
            if (selected <= 1)
                _brewSelection.Remove(itemId);
            else
                _brewSelection[itemId] = selected - 1;
            RefreshBrewWindow();
        }

        /// <summary>투입 비우기.</summary>
        public void BrewClear()
        {
            _brewSelection.Clear();
            _brewResultText = "투입을 비웠어요.";
            RefreshBrewWindow();
        }

        /// <summary>제조 실행 — 판정·소비·발견 반영 (스모크 테스트 공용). 성공/부산물 여부 반환.</summary>
        public BrewAttemptResult BrewExecute()
        {
            BuildBrewInputs(_brewSelection);
            BrewAttemptResult result = _brewStation.Attempt(_brewInputBuffer, BuildBrewContext(), _session.Inventory);
            ApplyBrewResult(result);
            RefreshBrewWindow();
            return result;
        }

        void ApplyBrewResult(BrewAttemptResult result)
        {
            switch (result.Status)
            {
                case BrewApplyStatus.Discovered:
                    _session.AddStarlight(result.StarlightAwarded);
                    _brewResultText = $"✨ 새로운 포션 발견! {ItemLabel(result.ProducedItemId)}  (+별빛 조각 {result.StarlightAwarded})";
                    MapCustomerFx.Spawn(new Vector3(CauldronCenter.x, CauldronCenter.y + 0.9f, 0f),
                        $"✨ 발견! {ItemName(result.ProducedItemId)}");
                    _brewSelection.Clear();
                    break;
                case BrewApplyStatus.AlreadyKnown:
                    _brewResultText = $"{ItemLabel(result.ProducedItemId)} 제조 완료 (이미 발견된 레시피).";
                    _brewSelection.Clear();
                    break;
                case BrewApplyStatus.Byproduct:
                    _brewResultText = $"실패… {ItemLabel(result.ProducedItemId)} 획득 — 실험 일지에 기록"
                        + (result.NewlyDiscovered ? "  ✨ 실험 일지 새 항목!" : "");
                    _brewSelection.Clear();
                    break;
                case BrewApplyStatus.MissingIngredient:
                    _brewResultText = result.Result.Hint;   // "핵심 재료가 빠진 것 같다" (재료 미소비)
                    break;
                case BrewApplyStatus.ConditionNotMet:
                    _brewResultText = result.Result.Hint;   // 조건 불충족 (재료 미소비)
                    break;
                default:
                    _brewResultText = "투입이 비었거나 재료가 부족해요.";
                    break;
            }
            RefreshCodexHud();
        }

        void BuildBrewInputs(IReadOnlyDictionary<string, int> selection)
        {
            _brewInputBuffer.Clear();
            foreach (KeyValuePair<string, int> pair in selection)
            {
                if (pair.Value <= 0 || !_itemsById.TryGetValue(pair.Key, out ItemData item))
                    continue;
                _brewInputBuffer.Add(new BrewInputItem(pair.Key, item.composition, item.baseValue, pair.Value));
            }
        }

        Core.IBrewContext BuildBrewContext()
        {
            // TimeOfDay만 실값 — 날씨/계절은 S11 전까지 맑음/봄 고정 (S05 인계 방침).
            return new Core.BrewContext(_session.Clock.CurrentTimeOfDay, Core.Weather.Clear, Core.Season.Spring);
        }

        void RefreshBrewWindow(bool open = false)
        {
            if (_brewWindow == null)
                return;

            _brewRowBuffer.Clear();
            foreach (KeyValuePair<string, int> entry in _session.Inventory.Entries)
            {
                string id = entry.Key;
                if (_potionIdSet.Contains(id) || !_itemsById.TryGetValue(id, out ItemData item))
                    continue;   // 포션·부산물은 재투입 불가 — 재료(식물·가공재료)만

                int owned = entry.Value;
                _brewSelection.TryGetValue(id, out int selected);
                string label = $"{item.displayEmoji} {item.displayName}  [{CompositionShort(item.composition)}]  보유 {owned} · 투입 {selected}";
                string capturedId = id;
                _brewRowBuffer.Add(new BrewWindow.IngredientRow(
                    label,
                    PlaceholderPalette.ForComposition(item.composition),
                    selected < owned,
                    selected > 0,
                    () => BrewAdd(capturedId),
                    () => BrewRemove(capturedId)));
            }

            string summary = BrewSummary();
            bool canBrew = BrewSelectionTotal() > 0;
            const string title = "가마솥 — 자유 투입 조합";
            if (open)
                _brewWindow.Open(title, _brewRowBuffer, summary, _brewResultText, canBrew);
            else
                _brewWindow.Render(title, _brewRowBuffer, summary, _brewResultText, canBrew);
        }

        string BrewSummary()
        {
            var total = new Core.ElementComposition();
            foreach (KeyValuePair<string, int> pair in _brewSelection)
            {
                if (pair.Value <= 0 || !_itemsById.TryGetValue(pair.Key, out ItemData item))
                    continue;
                for (int i = 0; i < pair.Value; i++)
                    total += item.composition;
            }
            // 이모지 글리프가 없는 환경 대비 한글 병기 (플레이스홀더 규약 — 라벨이 의미 전달).
            return $"투입 조성 — 🔥불 {total.fire} · 💧물 {total.water} · 🌍대지 {total.earth} · 💨바람 {total.wind}  (합 {total.Total})";
        }

        int BrewSelectionTotal()
        {
            int sum = 0;
            foreach (KeyValuePair<string, int> pair in _brewSelection)
                if (pair.Value > 0)
                    sum += pair.Value;
            return sum;
        }

        static string CompositionShort(Core.ElementComposition c)
        {
            // 이모지 + 한글 병기 (플레이스홀더 규약 — 글리프 없는 환경에서도 라벨이 의미 전달).
            var parts = new List<string>(5);
            if (c.fire > 0) parts.Add($"🔥불{c.fire}");
            if (c.water > 0) parts.Add($"💧물{c.water}");
            if (c.earth > 0) parts.Add($"🌍대지{c.earth}");
            if (c.wind > 0) parts.Add($"💨바람{c.wind}");
            if (c.star > 0) parts.Add($"⭐별{c.star}");
            return parts.Count > 0 ? string.Join(" ", parts) : "—";
        }

        string ItemName(string itemId)
        {
            return _itemsById.TryGetValue(itemId, out ItemData item) ? item.displayName : itemId;
        }

        // ---- S06 도감 ----

        /// <summary>도감 창 열기 (스모크 테스트 공용 진입점).</summary>
        public void OpenCodexWindow()
        {
            _codexPage = CodexWindow.Page.Potions;
            RefreshCodexWindow(open: true);
        }

        void SetCodexPage(CodexWindow.Page page)
        {
            _codexPage = page;
            RefreshCodexWindow();
        }

        void RefreshCodexWindow(bool open = false)
        {
            if (_codexWindow == null)
                return;

            _codexRowBuffer.Clear();
            if (_codexPage == CodexWindow.Page.Potions)
                BuildCodexPotionRows();
            else
                BuildCodexJournalRows();

            if (open)
                _codexWindow.Open(CodexHeader(), _codexPage, _codexRowBuffer);
            else
                _codexWindow.Render(CodexHeader(), _codexPage, _codexRowBuffer);
        }

        void BuildCodexPotionRows()
        {
            foreach (PotionData potion in _potionCatalog)
            {
                if (_codex.IsDiscovered(potion.id))
                {
                    string label = $"{potion.displayEmoji} {potion.displayName}  [{CompositionShort(potion.composition)}]  {potion.baseValue}G";
                    string capturedId = potion.id;
                    bool canRebrew = CanRebrew(potion);
                    _codexRowBuffer.Add(new CodexWindow.Row(
                        label, PlaceholderPalette.ForComposition(potion.composition),
                        true, "재제조", canRebrew, () => RebrewRecipe(capturedId)));
                }
                else
                {
                    _codexRowBuffer.Add(new CodexWindow.Row(
                        "❔ ??? (미발견 포션)", new Color(0.16f, 0.16f, 0.19f), false, null, false, null));
                }
            }
        }

        void BuildCodexJournalRows()
        {
            foreach (PotionData byproduct in _byproductCatalog)
            {
                if (_codex.IsDiscovered(byproduct.id))
                {
                    string label = $"{byproduct.displayEmoji} {byproduct.displayName}  {byproduct.baseValue}G";
                    _codexRowBuffer.Add(new CodexWindow.Row(
                        label, PlaceholderPalette.ForComposition(byproduct.composition), false, null, false, null));
                }
                else
                {
                    _codexRowBuffer.Add(new CodexWindow.Row(
                        "❔ ??? (미기록 실패작)", new Color(0.16f, 0.16f, 0.19f), false, null, false, null));
                }
            }
        }

        string CodexHeader()
        {
            int discovered = _codex.DiscoveredCount;
            int total = _codex.TotalEntries;
            int percent = total > 0 ? Mathf.RoundToInt((float)_codex.CompletionRatio * 100f) : 0;
            int bonusPercent = Mathf.RoundToInt((float)_codex.GoldBonusFraction * 100f);
            return $"발견 {discovered}/{total} ({percent}%)  ·  글로벌 골드 +{bonusPercent}%  ·  ✨ 별빛 조각 {_session.StarlightShards}";
        }

        /// <summary>발견 레시피 원클릭 재제조 — 조성에서 재료를 역산해 자동 투입.</summary>
        public bool RebrewRecipe(string potionId)
        {
            PotionData potion = null;
            foreach (PotionData p in _potionCatalog)
                if (p != null && p.id == potionId) { potion = p; break; }
            if (potion == null)
                return false;

            if (!TryBuildRecipeSelection(potion, out Dictionary<string, int> need))
            {
                MapCustomerFx.Spawn(new Vector3(CodexCenter.x, CodexCenter.y + 0.8f, 0f), "재료가 부족해요");
                return false;
            }

            BuildBrewInputs(need);
            BrewAttemptResult result = _brewStation.Attempt(_brewInputBuffer, BuildBrewContext(), _session.Inventory);
            if (result.IsPotionSuccess)
                MapCustomerFx.Spawn(new Vector3(CodexCenter.x, CodexCenter.y + 0.8f, 0f),
                    $"재제조: {ItemName(potionId)} +1");
            else
                MapCustomerFx.Spawn(new Vector3(CodexCenter.x, CodexCenter.y + 0.8f, 0f), "재료가 부족해요");

            RefreshCodexWindow();
            return result.IsPotionSuccess;
        }

        bool CanRebrew(PotionData potion)
        {
            if (!TryBuildRecipeSelection(potion, out Dictionary<string, int> need))
                return false;
            foreach (KeyValuePair<string, int> pair in need)
                if (_session.Inventory.GetCount(pair.Key) < pair.Value)
                    return false;
            return true;
        }

        // 조성 + 지정 재료를 실제 인벤토리 재료 id로 역산. 지정 재료의 조성만큼 목표에서 먼저 차감한 뒤
        // 남은 조성을 원소당 단위 재료로 채운다(이중 계산 방지 — 예: 용의 숨결 🔥4💨2 = 용의 입김초 🔥2💨1 ×2).
        // 매핑 불가한 원소가 남으면 false.
        bool TryBuildRecipeSelection(PotionData potion, out Dictionary<string, int> need)
        {
            need = new Dictionary<string, int>(System.StringComparer.Ordinal);
            Core.ElementComposition remaining = potion.composition;

            if (potion.requiredIngredients != null)
            {
                foreach (IngredientRequirement req in potion.requiredIngredients)
                {
                    if (req == null || req.item == null || string.IsNullOrEmpty(req.item.id))
                        return false;
                    need.TryGetValue(req.item.id, out int have);
                    need[req.item.id] = have + req.count;
                    for (int k = 0; k < req.count; k++)
                        remaining = SubtractComposition(remaining, req.item.composition);
                }
            }

            for (int e = 0; e < Core.ElementComposition.SlotCount; e++)
            {
                int amount = remaining[(Core.Element)e];
                if (amount <= 0)
                    continue;   // 0 이하 = 지정 재료가 이미 이 원소를 채움
                if (!_elementUnitIngredient.TryGetValue((Core.Element)e, out string unitId))
                    return false;   // 이 원소의 단위 재료가 없음
                need.TryGetValue(unitId, out int have);
                need[unitId] = have + amount;
            }
            return need.Count > 0;
        }

        static Core.ElementComposition SubtractComposition(Core.ElementComposition a, Core.ElementComposition b)
        {
            return new Core.ElementComposition(
                a.fire - b.fire, a.water - b.water, a.earth - b.earth, a.wind - b.wind, a.star - b.star);
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
                string sourceLabel = RecipeSourceLabel(material);
                bool hasSource = HasAllInputs(material);

                _entriesBuffer.Add(new MapPopup.Entry(
                    $"[{material.processingStage}차] {material.displayEmoji} {material.displayName} ← {sourceLabel} ({material.processingSeconds:0}초)",
                    PlaceholderPalette.ForComposition(material.composition),
                    hasSource,
                    () => TryStartRecipe(captured)));
            }

            _popup.Open("가공 선택 (1~3차 — 마른 잎/가루 → 정수 → 별빛·모래·수정)", _entriesBuffer);
        }

        // 가공 원료 라벨 (주 원료 + 추가 원료). 다중 입력이면 " + " 로 이어 붙임.
        string RecipeSourceLabel(MaterialData material)
        {
            if (material.sourceItem == null)
                return "(원료 없음)";
            var builder = new StringBuilder();
            builder.Append($"{material.sourceItem.displayEmoji} {material.sourceItem.displayName} ×{material.sourceCount}");
            if (material.extraInputs != null)
            {
                foreach (IngredientRequirement extra in material.extraInputs)
                {
                    if (extra == null || extra.item == null)
                        continue;
                    builder.Append($" + {extra.item.displayEmoji} {extra.item.displayName} ×{extra.count}");
                }
            }
            return builder.ToString();
        }

        // 주 원료 + 추가 원료를 전부 보유하고 있는가 (다중 입력 가공 시작 가능 여부).
        bool HasAllInputs(MaterialData material)
        {
            if (material.sourceItem == null
                || _session.Inventory.GetCount(material.sourceItem.id) < material.sourceCount)
                return false;
            if (material.extraInputs != null)
            {
                foreach (IngredientRequirement extra in material.extraInputs)
                {
                    if (extra == null || extra.item == null || string.IsNullOrEmpty(extra.item.id))
                        return false;
                    if (_session.Inventory.GetCount(extra.item.id) < extra.count)
                        return false;
                }
            }
            return true;
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
            Workshop workshop = _session.Workshop;
            if (!workshop.IsIdle || !HasAllInputs(material))
                return false;

            // 추가 원료(2·3차 다중 입력)를 먼저 소비 — 주 원료는 Workshop.TryStart이 소비.
            bool hasExtras = material.extraInputs != null && material.extraInputs.Count > 0;
            if (hasExtras)
                foreach (IngredientRequirement extra in material.extraInputs)
                    _session.Inventory.TryRemove(extra.item.id, extra.count);

            bool started = workshop.TryStart(material.id, 1, material.sourceItem.id, material.sourceCount,
                _session.Inventory, _session.Clock.ResourceSeconds);

            if (!started && hasExtras)   // 롤백 — 주 원료 소비 실패 시 추가 원료 복구
                foreach (IngredientRequirement extra in material.extraInputs)
                    _session.Inventory.Add(extra.item.id, extra.count);
            return started;
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
                    tile.SetSoilColor(new Color(0.15f, 0.12f, 0.10f), 0.42f);
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

                tile.SetSoilColor(PlaceholderPalette.EmptySoil, 1f);
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
                _bench.SetBodyColor(new Color(0.42f, 0.30f, 0.19f));
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
                _bench.SetBodyColor(color);
                _bench.Emoji.text = material != null ? material.displayEmoji : "❓";
                _bench.Label.text = $"{name} 완료! 클릭: 수령";
            }
            else
            {
                double progress = workshop.GetProgress(now, processingSeconds);
                _bench.SetBodyColor(Color.Lerp(new Color(0.42f, 0.30f, 0.19f), color, 0.5f));
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
                    stand.SetBodyColor(new Color(0.40f, 0.32f, 0.23f));
                    stand.Emoji.text = "➕";
                    stand.Label.text = "진열";
                    continue;
                }

                _itemsById.TryGetValue(slot.ItemId, out ItemData item);
                stand.SetBodyColor(item != null
                    ? PlaceholderPalette.ForComposition(item.composition)
                    : PlaceholderPalette.Neutral);
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

        void RefreshCodexHud()
        {
            if (_hud == null || _codex == null)
                return;
            int percent = _codex.TotalEntries > 0 ? Mathf.RoundToInt((float)_codex.CompletionRatio * 100f) : 0;
            int bonusPercent = Mathf.RoundToInt((float)_codex.GoldBonusFraction * 100f);
            _hud.SetCodex($"📖 {_codex.DiscoveredCount}/{_codex.TotalEntries} ({percent}%) · 골드+{bonusPercent}% · ✨{_session.StarlightShards}");
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
            if (art.HasGround)
                MapPlaceholderFactory.CreateSprite(transform, "Ground", art.ground, -100);
            else
                MapPlaceholderFactory.CreateSquare(transform, "Ground", new Vector2(19f, 10.6f),
                    new Color(0.20f, 0.30f, 0.17f), -100);

            BuildGardenZone();
            BuildWorkshopZone();
            BuildBreweryZone();
            BuildShopZone();
            BuildProps();
        }

        // 구역 바닥 패치 — 지면 아트가 있으면 이미 구워져 있으므로 색 사각형을 생략한다.
        void BuildZonePatch(string name, Vector2 size, Color color, Vector3 position)
        {
            if (art.HasGround)
                return;
            MapPlaceholderFactory.CreateSquare(transform, name, size, color, -50, position);
        }

        // 시설 몸체 — 아트가 있으면 스프라이트, 없으면 색 사각형(플레이스홀더).
        SpriteRenderer BuildStationBody(Transform parent, Sprite sprite, Vector2 placeholderSize,
            Color placeholderColor, out bool isArt)
        {
            isArt = sprite != null;
            return isArt
                ? MapPlaceholderFactory.CreateSprite(parent, "Body", sprite, 0)
                : MapPlaceholderFactory.CreateSquare(parent, "Body", placeholderSize, placeholderColor, 0);
        }

        void BuildBreweryZone()
        {
            // 상단 중앙 조합 구역 패치 (정원·공방 사이 빈 공간)
            BuildZonePatch("BreweryPatch", new Vector2(3.4f, 3.9f), new Color(0.20f, 0.16f, 0.24f),
                new Vector3(CauldronCenter.x, 2.35f, 0f));

            // 가마솥 — 조합 창 진입
            var cauldronGo = new GameObject("Cauldron");
            cauldronGo.transform.SetParent(transform, false);
            cauldronGo.transform.localPosition = new Vector3(CauldronCenter.x, CauldronCenter.y, 0f);
            var cauldronMarker = cauldronGo.AddComponent<MapTile>();
            cauldronMarker.kind = MapTile.Kind.Cauldron;
            var cauldronCollider = cauldronGo.AddComponent<BoxCollider2D>();
            cauldronCollider.size = new Vector2(2.0f, 1.5f);
            BuildStationBody(cauldronGo.transform, art.cauldron, new Vector2(2.0f, 1.5f),
                new Color(0.34f, 0.24f, 0.42f), out bool cauldronIsArt);
            if (!cauldronIsArt)
                MapPlaceholderFactory.CreateText(cauldronGo.transform, "Emoji", "🍯", 64, 0.1f, Color.white, 8,
                    new Vector3(0f, 0.12f, 0f));
            MapPlaceholderFactory.CreateText(cauldronGo.transform, "Label", "가마솥 — 클릭: 조합", 40, 0.055f,
                Color.white, 8, new Vector3(0f, -1.02f, 0f));

            // 도감 책 — 도감 창 진입
            var codexGo = new GameObject("CodexBook");
            codexGo.transform.SetParent(transform, false);
            codexGo.transform.localPosition = new Vector3(CodexCenter.x, CodexCenter.y, 0f);
            var codexMarker = codexGo.AddComponent<MapTile>();
            codexMarker.kind = MapTile.Kind.Codex;
            var codexCollider = codexGo.AddComponent<BoxCollider2D>();
            codexCollider.size = new Vector2(1.4f, 1.0f);
            BuildStationBody(codexGo.transform, art.codexBook, new Vector2(1.4f, 1.0f),
                new Color(0.22f, 0.30f, 0.44f), out bool codexIsArt);
            if (!codexIsArt)
                MapPlaceholderFactory.CreateText(codexGo.transform, "Emoji", "📖", 52, 0.08f, Color.white, 8,
                    new Vector3(0f, 0.08f, 0f));
            MapPlaceholderFactory.CreateText(codexGo.transform, "Label", "도감", 36, 0.05f, Color.white, 8,
                new Vector3(0f, -0.72f, 0f));
        }

        void BuildGardenZone()
        {
            int rows = Garden.MaxSlotCount / GardenColumns;
            float gridWidth = GardenColumns * TileSize + (GardenColumns - 1) * TileSpacing;
            float gridHeight = rows * TileSize + (rows - 1) * TileSpacing;

            BuildZonePatch("GardenPatch", new Vector2(gridWidth + 0.7f, gridHeight + 0.7f),
                new Color(0.16f, 0.13f, 0.10f), new Vector3(GardenCenter.x, GardenCenter.y, 0f));
            MapPlaceholderFactory.CreateText(transform, "GardenLabel", "🌱 정원", 48, 0.12f, Color.white, -40,
                new Vector3(GardenCenter.x, GardenCenter.y + gridHeight * 0.5f + 0.65f, 0f));
            BuildGardenFence(gridWidth, gridHeight);

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

                bool soilIsArt = art.gardenPlot != null;
                var widget = new TileWidget
                {
                    SoilIsArt = soilIsArt,
                    Soil = soilIsArt
                        ? MapPlaceholderFactory.CreateSprite(tileGo.transform, "Soil", art.gardenPlot, 0)
                        : MapPlaceholderFactory.CreateSquare(tileGo.transform, "Soil",
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

        // 정원 울타리 — 밭 그리드 위·아래를 따라 조각을 반복 배치 (아트가 있을 때만).
        void BuildGardenFence(float gridWidth, float gridHeight)
        {
            if (art.fence == null)
                return;

            const float step = 0.72f;
            float halfWidth = gridWidth * 0.5f + 0.25f;
            float top = GardenCenter.y + gridHeight * 0.5f + 0.35f;
            float bottom = GardenCenter.y - gridHeight * 0.5f - 0.35f;
            int count = Mathf.FloorToInt(halfWidth * 2f / step) + 1;
            float startX = GardenCenter.x - halfWidth;

            for (int i = 0; i < count; i++)
            {
                float x = startX + i * step;
                MapPlaceholderFactory.CreateSprite(transform, $"Fence_T{i}", art.fence, -30,
                    new Vector3(x, top, 0f));
                MapPlaceholderFactory.CreateSprite(transform, $"Fence_B{i}", art.fence, -30,
                    new Vector3(x, bottom, 0f));
            }

            if (art.waterBucket != null)
                MapPlaceholderFactory.CreateSprite(transform, "PropWaterBucket", art.waterBucket, -30,
                    new Vector3(-0.5f, -2.6f, 0f));
        }

        void BuildWorkshopZone()
        {
            BuildZonePatch("WorkshopPatch", new Vector2(4.4f, 3.4f), new Color(0.24f, 0.23f, 0.26f),
                new Vector3(BenchCenter.x, BenchCenter.y - 0.1f, 0f));
            MapPlaceholderFactory.CreateText(transform, "WorkshopLabel", "⚗️ 공방", 48, 0.12f, Color.white, -40,
                new Vector3(BenchCenter.x, BenchCenter.y + 1.95f, 0f));

            var benchGo = new GameObject("Bench");
            benchGo.transform.SetParent(transform, false);
            benchGo.transform.localPosition = new Vector3(BenchCenter.x, BenchCenter.y, 0f);
            var marker = benchGo.AddComponent<MapTile>();
            marker.kind = MapTile.Kind.Bench;
            var collider = benchGo.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2.0f, 1.4f);

            SpriteRenderer body = BuildStationBody(benchGo.transform, art.workbench, new Vector2(2.0f, 1.4f),
                new Color(0.42f, 0.30f, 0.19f), out bool benchIsArt);
            _bench = new StationWidget
            {
                Body = body,
                BodyIsArt = benchIsArt,
                Emoji = MapPlaceholderFactory.CreateText(benchGo.transform, "Emoji", "🛠️", 64, 0.09f,
                    Color.white, 8, new Vector3(0f, benchIsArt ? 1.15f : 0.1f, 0f)),
                Label = MapPlaceholderFactory.CreateText(benchGo.transform, "Label", "", 40, 0.06f,
                    Color.white, 8, new Vector3(0f, -1.0f, 0f))
            };
        }

        void BuildShopZone()
        {
            BuildZonePatch("ShopPatch", new Vector2(5.6f, 3.4f), new Color(0.30f, 0.22f, 0.15f),
                new Vector3(StandRowCenter.x, StandRowCenter.y - 0.4f, 0f));
            MapPlaceholderFactory.CreateText(transform, "ShopLabel", "🏪 상점", 48, 0.12f, Color.white, -40,
                new Vector3(StandRowCenter.x, StandRowCenter.y + 1.6f, 0f));

            if (art.shopStall != null)
                MapPlaceholderFactory.CreateSprite(transform, "PropShopStall", art.shopStall, -30,
                    new Vector3(7.7f, -0.55f, 0f));
            if (art.shopSign != null)
                MapPlaceholderFactory.CreateSprite(transform, "PropShopSign", art.shopSign, -30,
                    new Vector3(0.9f, -2.0f, 0f));

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

                SpriteRenderer body = BuildStationBody(standGo.transform, art.shopShelf, new Vector2(1.4f, 1.1f),
                    new Color(0.40f, 0.32f, 0.23f), out bool standIsArt);
                _stands[i] = new StationWidget
                {
                    Body = body,
                    BodyIsArt = standIsArt,
                    Emoji = MapPlaceholderFactory.CreateText(standGo.transform, "Emoji", "➕", 56, 0.08f,
                        Color.white, 8, new Vector3(0f, standIsArt ? 0.92f : 0.08f, 0f)),
                    Label = MapPlaceholderFactory.CreateText(standGo.transform, "Label", "진열", 36, 0.05f,
                        Color.white, 8, new Vector3(0f, -0.88f, 0f))
                };
            }

            _customerCountdown = MapPlaceholderFactory.CreateText(transform, "CustomerCountdown", "", 40, 0.055f,
                new Color(1f, 1f, 1f, 0.9f), 8, new Vector3(StandRowCenter.x, -3.6f, 0f));
        }

        // 여백 장식 — 숲속의 작은 마녀풍 아늑함. 아트가 없으면 기존 색 사각형 플레이스홀더.
        void BuildProps()
        {
            BuildProp("PropTree", art.tree, new Vector3(7.4f, 3.3f, 0f), "🌲",
                new Vector2(1.0f, 1.4f), new Color(0.13f, 0.25f, 0.14f), 64);
            BuildProp("PropFlower", art.flowers, new Vector3(-1.3f, -4.6f, 0f), "🌼",
                new Vector2(0.6f, 0.6f), new Color(0.24f, 0.34f, 0.19f), 48);
            BuildProp("PropRock", art.rock, new Vector3(-8.3f, -1.0f, 0f), "🌿",
                new Vector2(0.6f, 0.6f), new Color(0.24f, 0.34f, 0.19f), 48);
        }

        void BuildProp(string name, Sprite sprite, Vector3 position, string placeholderEmoji,
            Vector2 placeholderSize, Color placeholderColor, int placeholderFontSize)
        {
            if (sprite != null)
            {
                MapPlaceholderFactory.CreateSprite(transform, name, sprite, -30, position);
                return;
            }
            var box = MapPlaceholderFactory.CreateSquare(transform, name, placeholderSize, placeholderColor,
                -30, position);
            MapPlaceholderFactory.CreateText(box.transform, "Emoji", placeholderEmoji, placeholderFontSize,
                0.10f, Color.white, -29);
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

        /// <summary>가마솥 월드 좌표 (스모크 테스트).</summary>
        public Vector3 CauldronWorldPosition => new Vector3(CauldronCenter.x, CauldronCenter.y, 0f);

        /// <summary>도감 책 월드 좌표 (스모크 테스트).</summary>
        public Vector3 CodexWorldPosition => new Vector3(CodexCenter.x, CodexCenter.y, 0f);

        /// <summary>모달 팝업 (스모크 테스트가 항목을 실행할 때 사용).</summary>
        public MapPopup Popup => _popup;

        /// <summary>조합 창 (스모크 테스트).</summary>
        public BrewWindow BrewWindow => _brewWindow;

        /// <summary>도감 창 (스모크 테스트).</summary>
        public CodexWindow CodexWindow => _codexWindow;

        /// <summary>복귀 요약 패널 (스모크 테스트).</summary>
        public OfflineSummaryWindow OfflineSummary => _offlineSummary;
    }
}
