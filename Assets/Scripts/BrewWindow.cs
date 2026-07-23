using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGarden
{
    /// <summary>
    /// 가마솥 자유 투입 조합 창 (S06, uGUI 코드 생성 플레이스홀더 — 맵과 분리된 레이어).
    /// 창고 재료를 골라 담고(+/−) 현재 투입 조성 합을 보며 "제조"를 누른다. 판정·소비·발견은
    /// MapScreen이 하고, 이 창은 표시 + 버튼 콜백만 담당한다(재구성형: Render마다 목록 재생성).
    /// </summary>
    public sealed class BrewWindow : MonoBehaviour
    {
        /// <summary>재료 한 줄(라벨 + 담기/빼기 가능 여부 + 콜백).</summary>
        public readonly struct IngredientRow
        {
            public readonly string Label;
            public readonly Color Color;
            public readonly bool CanAdd;
            public readonly bool CanRemove;
            public readonly Action OnAdd;
            public readonly Action OnRemove;

            public IngredientRow(string label, Color color, bool canAdd, bool canRemove, Action onAdd, Action onRemove)
            {
                Label = label;
                Color = color;
                CanAdd = canAdd;
                CanRemove = canRemove;
                OnAdd = onAdd;
                OnRemove = onRemove;
            }
        }

        /// <summary>제조 버튼 콜백 (MapScreen이 설정).</summary>
        public Action OnBrew;
        /// <summary>비우기 버튼 콜백.</summary>
        public Action OnClear;
        /// <summary>닫기 콜백(닫힘 후).</summary>
        public Action OnClosed;

        Font _font;
        GameObject _root;
        Text _title;
        Transform _list;
        Text _summary;
        Text _result;
        Button _brewButton;

        public bool IsOpen => _root != null && _root.activeSelf;

        void Awake()
        {
            _font = MapPlaceholderFactory.PlaceholderFont;
            BuildUi();
        }

        /// <summary>창 열기 + 초기 렌더.</summary>
        public void Open(string title, IReadOnlyList<IngredientRow> rows, string summary, string result, bool canBrew)
        {
            _root.SetActive(true);
            Render(title, rows, summary, result, canBrew);
        }

        /// <summary>현재 상태로 목록·요약·결과 갱신(투입/판정 후 MapScreen이 호출).</summary>
        public void Render(string title, IReadOnlyList<IngredientRow> rows, string summary, string result, bool canBrew)
        {
            if (_title != null) _title.text = title;
            if (_summary != null) _summary.text = summary;
            if (_result != null) _result.text = result;
            if (_brewButton != null) _brewButton.interactable = canBrew;

            for (int i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);

            if (rows == null || rows.Count == 0)
            {
                CreateRowLabel("(창고에 조합할 재료가 없어요 — 정원에서 수확·가공하세요)");
                return;
            }

            for (int i = 0; i < rows.Count; i++)
                CreateIngredientRow(rows[i]);
        }

        public void Close()
        {
            if (_root != null)
                _root.SetActive(false);
            OnClosed?.Invoke();
        }

        // ---- UI 생성 ----

        void BuildUi()
        {
            var canvasGo = new GameObject("BrewCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dimGo.transform.SetParent(canvasGo.transform, false);
            var dimRect = (RectTransform)dimGo.transform;
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            dimGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            dimGo.GetComponent<Button>().onClick.AddListener(Close);
            _root = dimGo;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(dimGo.transform, false);
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 600f);
            panelGo.GetComponent<Image>().color = PlaceholderPalette.PanelBackground;

            _title = CreateText(panelGo.transform, "Title", "가마솥 — 자유 투입 조합", 22, TextAnchor.MiddleCenter);
            SetStretch(_title.rectTransform, new Vector2(0f, 0.92f), new Vector2(1f, 1f));

            // 재료 목록 (스크롤 없이 세로 배치 — 재료 수가 적은 플레이스홀더)
            var listGo = new GameObject("IngredientList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panelGo.transform, false);
            SetStretch((RectTransform)listGo.transform, new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.90f));
            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _list = listGo.transform;

            _summary = CreateText(panelGo.transform, "Summary", "투입 조성: 🔥0 💧0 🌍0 💨0 (합 0)", 18,
                TextAnchor.MiddleCenter);
            SetStretch(_summary.rectTransform, new Vector2(0.05f, 0.31f), new Vector2(0.95f, 0.39f));

            _result = CreateText(panelGo.transform, "Result", "", 17, TextAnchor.MiddleCenter);
            _result.color = new Color(1f, 0.95f, 0.7f);
            SetStretch(_result.rectTransform, new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.30f));

            // 하단 버튼
            _brewButton = CreateButton(panelGo.transform, "Brew", "제조", new Color(0.35f, 0.45f, 0.65f),
                () => OnBrew?.Invoke());
            SetAnchoredButton((RectTransform)_brewButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 66f),
                new Vector2(200f, 48f));

            Button clear = CreateButton(panelGo.transform, "Clear", "비우기", PlaceholderPalette.Neutral,
                () => OnClear?.Invoke());
            SetAnchoredButton((RectTransform)clear.transform, new Vector2(0.5f, 0f), new Vector2(-150f, 22f),
                new Vector2(140f, 36f));

            Button close = CreateButton(panelGo.transform, "Close", "닫기", PlaceholderPalette.Neutral, Close);
            SetAnchoredButton((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(150f, 22f),
                new Vector2(140f, 36f));

            _root.SetActive(false);
        }

        void CreateIngredientRow(IngredientRow row)
        {
            var rowGo = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            rowGo.transform.SetParent(_list, false);
            rowGo.GetComponent<Image>().color = row.Color;
            var hl = rowGo.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 6f;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandHeight = true;
            hl.padding = new RectOffset(10, 8, 4, 4);
            rowGo.GetComponent<LayoutElement>().minHeight = 44f;

            Text label = CreateText(rowGo.transform, "Label", row.Label, 16, TextAnchor.MiddleLeft);
            var labelLe = label.gameObject.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;

            Button minus = CreateButton(rowGo.transform, "Minus", "−", new Color(0.5f, 0.32f, 0.30f),
                () => row.OnRemove?.Invoke());
            minus.interactable = row.CanRemove;
            var minusLe = minus.gameObject.AddComponent<LayoutElement>();
            minusLe.preferredWidth = 52f;

            Button plus = CreateButton(rowGo.transform, "Plus", "＋", new Color(0.30f, 0.5f, 0.34f),
                () => row.OnAdd?.Invoke());
            plus.interactable = row.CanAdd;
            var plusLe = plus.gameObject.AddComponent<LayoutElement>();
            plusLe.preferredWidth = 52f;
        }

        void CreateRowLabel(string text)
        {
            var go = new GameObject("Empty", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(_list, false);
            go.GetComponent<LayoutElement>().minHeight = 44f;
            Text label = CreateText(go.transform, "Label", text, 15, TextAnchor.MiddleCenter);
            SetStretch(label.rectTransform, Vector2.zero, Vector2.one);
        }

        Button CreateButton(Transform parent, string name, string label, Color color,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            Text text = CreateText(go.transform, "Text", label, 18, TextAnchor.MiddleCenter);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one);
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
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        static void SetStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void SetAnchoredButton(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
