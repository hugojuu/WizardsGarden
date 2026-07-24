using System;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGarden
{
    /// <summary>
    /// 복귀 요약 패널 (S08, uGUI 코드 생성 — 맵과 분리된 레이어). 게임을 켰을 때
    /// "그동안의 변화"(벌어들인 골드·수확량·정지 항목·8시간 캡 안내)를 모달로 보여준다.
    /// 정산·문구 구성은 MapScreen이 하고, 이 창은 표시 + 닫기 콜백만 담당한다.
    /// </summary>
    public sealed class OfflineSummaryWindow : MonoBehaviour
    {
        /// <summary>닫힘 콜백(닫은 뒤).</summary>
        public Action OnClosed;

        Font _font;
        GameObject _root;
        Text _title;
        Text _body;

        public bool IsOpen => _root != null && _root.activeSelf;

        void Awake()
        {
            _font = MapPlaceholderFactory.PlaceholderFont;
            BuildUi();
        }

        /// <summary>창 열기 + 문구 렌더.</summary>
        public void Open(string title, string body)
        {
            if (_title != null) _title.text = title;
            if (_body != null) _body.text = body;
            _root.SetActive(true);
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
            var canvasGo = new GameObject("OfflineSummaryCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120; // 팝업·조합/도감 창보다 위 — 복귀 즉시 안내
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
            dimGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            dimGo.GetComponent<Button>().onClick.AddListener(Close);
            _root = dimGo;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(dimGo.transform, false);
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 440f);
            panelGo.GetComponent<Image>().color = PlaceholderPalette.PanelBackground;

            _title = CreateText(panelGo.transform, "Title", "다녀오셨군요! ✨", 24, TextAnchor.MiddleCenter);
            SetStretch(_title.rectTransform, new Vector2(0f, 0.86f), new Vector2(1f, 1f));
            _title.color = new Color(1f, 0.95f, 0.7f);

            _body = CreateText(panelGo.transform, "Body", "", 18, TextAnchor.UpperLeft);
            SetStretch(_body.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.84f));
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;

            Button confirm = CreateButton(panelGo.transform, "Confirm", "확인", new Color(0.35f, 0.45f, 0.65f), Close);
            var confirmRect = (RectTransform)confirm.transform;
            confirmRect.anchorMin = new Vector2(0.5f, 0f);
            confirmRect.anchorMax = new Vector2(0.5f, 0f);
            confirmRect.pivot = new Vector2(0.5f, 0f);
            confirmRect.anchoredPosition = new Vector2(0f, 24f);
            confirmRect.sizeDelta = new Vector2(200f, 48f);

            _root.SetActive(false);
        }

        Button CreateButton(Transform parent, string name, string label, Color color, UnityEngine.Events.UnityAction onClick)
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
    }
}
