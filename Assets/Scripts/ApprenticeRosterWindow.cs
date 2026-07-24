using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGarden
{
    /// <summary>
    /// 견습생 관리 창 (S09, uGUI 코드 생성 플레이스홀더 — 맵과 분리된 레이어). 보유 견습생을 나열하고
    /// 구역 슬롯에 배치/해제, 각성 버튼을 제공한다. 판정·상태 변경은 MapScreen이 하고 이 창은 표시+콜백만.
    /// 유닛 클릭 시에도 이 창을 열어 "상태 보기"를 겸한다.
    /// </summary>
    public sealed class ApprenticeRosterWindow : MonoBehaviour
    {
        /// <summary>견습생 한 줄 (라벨 + 배치/해제 버튼 + 각성 버튼).</summary>
        public readonly struct Row
        {
            public readonly string Label;
            public readonly Color Color;
            public readonly string ActionLabel;   // "배치"/"해제"
            public readonly bool ActionEnabled;
            public readonly Action OnAction;
            public readonly bool ShowAwaken;
            public readonly bool AwakenEnabled;
            public readonly Action OnAwaken;

            public Row(string label, Color color, string actionLabel, bool actionEnabled, Action onAction,
                bool showAwaken, bool awakenEnabled, Action onAwaken)
            {
                Label = label;
                Color = color;
                ActionLabel = actionLabel;
                ActionEnabled = actionEnabled;
                OnAction = onAction;
                ShowAwaken = showAwaken;
                AwakenEnabled = awakenEnabled;
                OnAwaken = onAwaken;
            }
        }

        public Action OnClosed;

        Font _font;
        GameObject _root;
        Text _title;
        Text _header;
        Transform _list;

        public bool IsOpen => _root != null && _root.activeSelf;

        void Awake()
        {
            _font = MapPlaceholderFactory.PlaceholderFont;
            BuildUi();
        }

        public void Open(string header, IReadOnlyList<Row> rows)
        {
            _root.SetActive(true);
            Render(header, rows);
        }

        public void Render(string header, IReadOnlyList<Row> rows)
        {
            if (_header != null) _header.text = header;

            for (int i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);

            if (rows == null || rows.Count == 0)
            {
                CreateRowLabel("(보유한 견습생이 없어요)");
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
            var canvasGo = new GameObject("ApprenticeCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 112;
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
            panelRect.sizeDelta = new Vector2(640f, 640f);
            panelGo.GetComponent<Image>().color = PlaceholderPalette.PanelBackground;

            _title = CreateText(panelGo.transform, "Title", "견습생 관리 — 배치/해제", 22, TextAnchor.MiddleCenter);
            SetStretch(_title.rectTransform, new Vector2(0f, 0.93f), new Vector2(1f, 1f));

            _header = CreateText(panelGo.transform, "Header", "", 16, TextAnchor.MiddleCenter);
            _header.color = new Color(0.85f, 0.9f, 1f);
            SetStretch(_header.rectTransform, new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.93f));

            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panelGo.transform, false);
            SetStretch((RectTransform)listGo.transform, new Vector2(0.04f, 0.11f), new Vector2(0.96f, 0.85f));
            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _list = listGo.transform;

            Button close = CreateButton(panelGo.transform, "Close", "닫기", PlaceholderPalette.Neutral, Close);
            SetAnchoredButton((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 22f),
                new Vector2(160f, 40f));

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
            hl.padding = new RectOffset(10, 8, 4, 4);
            rowGo.GetComponent<LayoutElement>().minHeight = 52f;

            Text label = CreateText(rowGo.transform, "Label", row.Label, 15, TextAnchor.MiddleLeft);
            var labelLe = label.gameObject.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;

            if (row.ShowAwaken)
            {
                Button awaken = CreateButton(rowGo.transform, "Awaken", "각성",
                    new Color(0.62f, 0.5f, 0.22f), () => row.OnAwaken?.Invoke());
                awaken.interactable = row.AwakenEnabled;
                awaken.gameObject.AddComponent<LayoutElement>().preferredWidth = 78f;
            }

            Button action = CreateButton(rowGo.transform, "Action", row.ActionLabel,
                row.ActionLabel == "해제" ? new Color(0.5f, 0.32f, 0.30f) : new Color(0.30f, 0.5f, 0.34f),
                () => row.OnAction?.Invoke());
            action.interactable = row.ActionEnabled;
            action.gameObject.AddComponent<LayoutElement>().preferredWidth = 96f;
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
