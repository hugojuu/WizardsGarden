using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGarden
{
    /// <summary>
    /// 포션 도감 창 (S06, uGUI 코드 생성 플레이스홀더 — 맵과 분리된 레이어).
    /// 두 페이지: 포션(미발견 ???, 발견 시 이름·조성·재제조) / 실험 일지(실패 부산물 3종).
    /// 완성도·글로벌 골드 보너스·별빛 조각을 상단에 표시. 목록·행 동작은 MapScreen이 채운다.
    /// </summary>
    public sealed class CodexWindow : MonoBehaviour
    {
        public enum Page { Potions = 0, Journal = 1 }

        /// <summary>도감 한 줄(라벨 + 선택적 동작 버튼 = 재제조).</summary>
        public readonly struct Row
        {
            public readonly string Label;
            public readonly Color Color;
            public readonly bool HasAction;
            public readonly string ActionLabel;
            public readonly bool ActionEnabled;
            public readonly Action OnAction;

            public Row(string label, Color color, bool hasAction, string actionLabel, bool actionEnabled, Action onAction)
            {
                Label = label;
                Color = color;
                HasAction = hasAction;
                ActionLabel = actionLabel;
                ActionEnabled = actionEnabled;
                OnAction = onAction;
            }
        }

        public Action OnClosed;
        public Action OnSelectPotions;
        public Action OnSelectJournal;

        Font _font;
        GameObject _root;
        Text _header;
        Text _tabLabel;
        Transform _list;
        Button _potionTab;
        Button _journalTab;

        public bool IsOpen => _root != null && _root.activeSelf;

        void Awake()
        {
            _font = MapPlaceholderFactory.PlaceholderFont;
            BuildUi();
        }

        public void Open(string header, Page activePage, IReadOnlyList<Row> rows)
        {
            _root.SetActive(true);
            Render(header, activePage, rows);
        }

        public void Render(string header, Page activePage, IReadOnlyList<Row> rows)
        {
            if (_header != null) _header.text = header;
            if (_tabLabel != null)
                _tabLabel.text = activePage == Page.Potions ? "— 포션 —" : "— 실험 일지 —";

            // 활성 탭 강조
            if (_potionTab != null)
                _potionTab.GetComponent<Image>().color = activePage == Page.Potions
                    ? new Color(0.35f, 0.45f, 0.65f) : PlaceholderPalette.Neutral;
            if (_journalTab != null)
                _journalTab.GetComponent<Image>().color = activePage == Page.Journal
                    ? new Color(0.35f, 0.45f, 0.65f) : PlaceholderPalette.Neutral;

            for (int i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);

            if (rows == null || rows.Count == 0)
            {
                CreateEmptyRow("(항목 없음)");
                return;
            }
            for (int i = 0; i < rows.Count; i++)
                CreateRow(rows[i]);
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
            var canvasGo = new GameObject("CodexCanvas",
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
            panelRect.sizeDelta = new Vector2(620f, 620f);
            panelGo.GetComponent<Image>().color = PlaceholderPalette.PanelBackground;

            Text title = CreateText(panelGo.transform, "Title", "📖 포션 도감", 24, TextAnchor.MiddleCenter);
            SetStretch(title.rectTransform, new Vector2(0f, 0.93f), new Vector2(1f, 1f));

            _header = CreateText(panelGo.transform, "Header", "", 16, TextAnchor.MiddleCenter);
            _header.color = new Color(1f, 0.95f, 0.7f);
            SetStretch(_header.rectTransform, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.93f));

            _potionTab = CreateButton(panelGo.transform, "PotionTab", "포션", new Color(0.35f, 0.45f, 0.65f),
                () => OnSelectPotions?.Invoke());
            SetAnchored((RectTransform)_potionTab.transform, new Vector2(0.5f, 1f), new Vector2(-115f, -180f),
                new Vector2(200f, 40f), new Vector2(0.5f, 1f));

            _journalTab = CreateButton(panelGo.transform, "JournalTab", "실험 일지", PlaceholderPalette.Neutral,
                () => OnSelectJournal?.Invoke());
            SetAnchored((RectTransform)_journalTab.transform, new Vector2(0.5f, 1f), new Vector2(115f, -180f),
                new Vector2(200f, 40f), new Vector2(0.5f, 1f));

            _tabLabel = CreateText(panelGo.transform, "TabLabel", "— 포션 —", 14, TextAnchor.MiddleCenter);
            _tabLabel.color = new Color(1f, 1f, 1f, 0.6f);
            SetStretch(_tabLabel.rectTransform, new Vector2(0.03f, 0.70f), new Vector2(0.97f, 0.75f));

            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panelGo.transform, false);
            SetStretch((RectTransform)listGo.transform, new Vector2(0.04f, 0.11f), new Vector2(0.96f, 0.69f));
            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _list = listGo.transform;

            Button close = CreateButton(panelGo.transform, "Close", "닫기", PlaceholderPalette.Neutral, Close);
            SetAnchored((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 22f),
                new Vector2(180f, 40f), new Vector2(0.5f, 0f));

            _root.SetActive(false);
        }

        void CreateRow(Row row)
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
            hl.padding = new RectOffset(12, 8, 4, 4);
            rowGo.GetComponent<LayoutElement>().minHeight = 42f;

            Text label = CreateText(rowGo.transform, "Label", row.Label, 16, TextAnchor.MiddleLeft);
            var labelLe = label.gameObject.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;

            if (row.HasAction)
            {
                Button action = CreateButton(rowGo.transform, "Action", row.ActionLabel,
                    new Color(0.30f, 0.5f, 0.34f), () => row.OnAction?.Invoke());
                action.interactable = row.ActionEnabled;
                var le = action.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 120f;
            }
        }

        void CreateEmptyRow(string text)
        {
            var go = new GameObject("Empty", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(_list, false);
            go.GetComponent<LayoutElement>().minHeight = 42f;
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

            Text text = CreateText(go.transform, "Text", label, 16, TextAnchor.MiddleCenter);
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

        static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
