using UnityEngine;

namespace WizardGarden
{
    /// <summary>
    /// 맵 플레이스홀더 생성 유틸 (S04b) — 흰 사각형 스프라이트 + 월드 TextMesh.
    /// 색은 SpriteRenderer.color / TextMesh.color로, 정식 아트 교체 시 이 지점만 바뀐다.
    /// </summary>
    public static class MapPlaceholderFactory
    {
        static Sprite _square;
        static Font _font;

        /// <summary>1×1 월드 유닛 흰 사각형 (플레이 세션마다 지연 재생성).</summary>
        public static Sprite SquareSprite
        {
            get
            {
                if (_square == null)
                {
                    var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var pixels = new Color32[16];
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color32(255, 255, 255, 255);
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    texture.filterMode = FilterMode.Point;
                    _square = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                    _square.name = "PlaceholderSquare";
                }
                return _square;
            }
        }

        /// <summary>플레이스홀더 폰트 (한글 글리프 있는 레거시 런타임 폰트 — S03 결정 승계).</summary>
        public static Font PlaceholderFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null)
                        _font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 18);
                }
                return _font;
            }
        }

        /// <summary>색 사각형 SpriteRenderer 생성 (size = 월드 유닛).</summary>
        public static SpriteRenderer CreateSquare(Transform parent, string name, Vector2 size, Color color,
            int sortingOrder, Vector3 localPosition = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SquareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        /// <summary>월드 TextMesh 생성 (이모지 + 한글 라벨 플레이스홀더 규약).</summary>
        public static TextMesh CreateText(Transform parent, string name, string text, int fontSize,
            float characterSize, Color color, int sortingOrder, Vector3 localPosition = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var textMesh = go.AddComponent<TextMesh>();
            textMesh.font = PlaceholderFont;
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.text = text;
            textMesh.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = PlaceholderFont.material;
            renderer.sortingOrder = sortingOrder;
            return textMesh;
        }
    }
}
