using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGarden
{
    /// <summary>
    /// 맵 화면용 모달 목록 팝업 (S04b, uGUI 코드 생성 플레이스홀더).
    /// 종자 선택·가공 선택·진열 선택·확장 확인 공용 — 열 때마다 내용을 재구성한다.
    /// 항목 동작은 닫힌 뒤 실행 (동작이 팝업을 다시 열 수 있음 — 해금 → 목록 갱신).
    /// </summary>
    public sealed class MapPopup : MonoBehaviour
    {
        public readonly struct Entry
        {
            public readonly string Label;
            public readonly Color Color;
            public readonly bool Interactable;
            public readonly Action Action;

            public Entry(string label, Color color, bool interactable, Action action)
            {
                Label = label;
                Color = color;
                Interactable = interactable;
                Action = action;
            }
        }

        Font _font;
        GameObject _root;
        Text _title;
        Transform _list;
        readonly List<Entry> _entries = new List<Entry>();

        public bool IsOpen => _root != null && _root.activeSelf;

        public int EntryCount => _entries.Count;

        public string GetEntryLabel(int index)
        {
            return index >= 0 && index < _entries.Count ? _entries[index].Label : null;
        }

        void Awake()
        {
            _font = MapPlaceholderFactory.PlaceholderFont;
            BuildUi();
        }

        /// <summary>팝업 열기 — 목록 내용을 다시 만든다.</summary>
        public void Open(string title, List<Entry> entries)
        {
            _title.text = title;
            _entries.Clear();
            _entries.AddRange(entries);

            for (int i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);

            for (int i = 0; i < _entries.Count; i++)
            {
                int index = i;
                Entry entry = _entries[i];
                Button button = CreateButton(_list, $"Entry{i}", entry.Label, entry.Color, () => InvokeEntry(index));
                button.interactable = entry.Interactable;
            }

            _root.SetActive(true);
        }

        public void Close()
        {
            _entries.Clear();
            if (_root != null)
                _root.SetActive(false);
        }

        /// <summary>항목 실행 (버튼 클릭 경로 — 스모크 테스트도 이 경로 사용). 닫은 뒤 동작 실행.</summary>
        public bool InvokeEntry(int index)
        {
            if (!IsOpen || index < 0 || index >= _entries.Count)
                return false;

            Entry entry = _entries[index];
            if (!entry.Interactable)
                return false;

            Close();
            entry.Action?.Invoke();
            return true;
        }

        // ---- UI 생성 (플레이스홀더) ----

        void BuildUi()
        {
            var canvasGo = new GameObject("PopupCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            // 딤 배경 — 클릭하면 닫힘 (맵 오클릭 방지 겸용)
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
            panelRect.sizeDelta = new Vector2(460f, 480f);
            panelGo.GetComponent<Image>().color = PlaceholderPalette.PanelBackground;

            _title = CreateText(panelGo.transform, "Title", "", 22, TextAnchor.MiddleCenter);
            var titleRect = _title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.9f);
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var listGo = new GameObject("EntryList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panelGo.transform, false);
            var listRect = (RectTransform)listGo.transform;
            listRect.anchorMin = new Vector2(0.05f, 0.15f);
            listRect.anchorMax = new Vector2(0.95f, 0.88f);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = Vector2.zero;
            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            _list = listGo.transform;

            Button cancel = CreateButton(panelGo.transform, "Cancel", "취소", PlaceholderPalette.Neutral, Close);
            var cancelRect = (RectTransform)cancel.transform;
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.pivot = new Vector2(0.5f, 0f);
            cancelRect.anchoredPosition = new Vector2(0f, 22f);
            cancelRect.sizeDelta = new Vector2(160f, 40f);

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
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
    }
}
