using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WizardGarden
{
    /// <summary>
    /// 맵과 분리된 HUD 레이어 (S04b, uGUI — 미니 모드 대비). 시계·골드·창고·조작 힌트만 표시.
    /// 값은 MapScreen이 밀어 넣는다 (HUD는 표시 전용).
    /// </summary>
    public sealed class MapHud : MonoBehaviour
    {
        Font _font;
        Text _clockLabel;
        Text _goldLabel;
        Text _inventoryLabel;

        void Awake()
        {
            _font = MapPlaceholderFactory.PlaceholderFont;
            BuildUi();
            EnsureEventSystem();
        }

        public void SetClock(string text)
        {
            if (_clockLabel != null)
                _clockLabel.text = text;
        }

        public void SetGold(string text)
        {
            if (_goldLabel != null)
                _goldLabel.text = text;
        }

        public void SetInventory(string text)
        {
            if (_inventoryLabel != null)
                _inventoryLabel.text = text;
        }

        // ---- UI 생성 (플레이스홀더) ----

        void BuildUi()
        {
            var canvasGo = new GameObject("HudCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            // 상단 바 (반투명 — 맵과 분리된 레이어임을 시각화)
            var topBar = CreatePanel(canvasGo.transform, "TopBar");
            SetRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(0f, 36f),
                new Vector2(0.5f, 1f));
            topBar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.75f);

            _clockLabel = CreateText(topBar.transform, "ClockLabel", "", 20, TextAnchor.MiddleLeft);
            SetRect((RectTransform)_clockLabel.transform, new Vector2(0f, 0f), new Vector2(0.6f, 1f),
                new Vector2(16f, 0f), Vector2.zero, new Vector2(0f, 0.5f));

            _goldLabel = CreateText(topBar.transform, "GoldLabel", "💰 0G", 22, TextAnchor.MiddleRight);
            SetRect((RectTransform)_goldLabel.transform, new Vector2(0.6f, 0f), new Vector2(1f, 1f),
                new Vector2(-16f, 0f), Vector2.zero, new Vector2(1f, 0.5f));

            // 창고 패널 (우상단 — 맵의 여백 위)
            var warehouse = CreatePanel(canvasGo.transform, "WarehousePanel");
            SetRect(warehouse, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -48f),
                new Vector2(268f, 300f), new Vector2(1f, 1f));
            warehouse.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.72f);

            Text warehouseTitle = CreateText(warehouse.transform, "Title", "🧺 창고", 19, TextAnchor.MiddleCenter);
            SetRectStretch(warehouseTitle.rectTransform, new Vector2(0f, 0.88f), Vector2.one);

            _inventoryLabel = CreateText(warehouse.transform, "Items", "(비어 있음)", 15, TextAnchor.UpperLeft);
            SetRectStretch(_inventoryLabel.rectTransform, new Vector2(0.07f, 0.03f), new Vector2(0.96f, 0.86f));

            // 조작 힌트 (하단)
            Text hint = CreateText(canvasGo.transform, "Hint",
                "빈 밭=심기 · 자란 식물=수확 · 작업대=가공 · 진열대=진열/회수 · 잠긴 밭=확장  |  F12 디버그 화면",
                15, TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f),
                new Vector2(1100f, 24f), new Vector2(0.5f, 0f));
            hint.color = new Color(1f, 1f, 1f, 0.85f);
        }

        RectTransform CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
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
