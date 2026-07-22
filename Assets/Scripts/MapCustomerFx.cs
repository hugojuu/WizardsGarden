using UnityEngine;

namespace WizardGarden
{
    /// <summary>
    /// 손님 연출 (S04b — 연출만, 유닛 AI는 S09). 판매 발생 시 상점 앞에
    /// 손님 표시가 나타나 잠시 머물다 떠오르며 사라진다.
    /// </summary>
    public sealed class MapCustomerFx : MonoBehaviour
    {
        const float Duration = 2.6f;
        const float FadeInSeconds = 0.25f;
        const float FadeOutSeconds = 0.8f;
        const float RiseSpeed = 0.12f;

        float _age;
        SpriteRenderer _body;
        TextMesh _emoji;
        TextMesh _label;

        public static MapCustomerFx Spawn(Vector3 position, string message)
        {
            var go = new GameObject("CustomerFx");
            go.transform.position = position;
            var fx = go.AddComponent<MapCustomerFx>();
            fx.Build(message);
            return fx;
        }

        void Build(string message)
        {
            _body = MapPlaceholderFactory.CreateSquare(transform, "Body", new Vector2(0.55f, 0.75f),
                new Color(0.82f, 0.66f, 0.42f), 20);
            _emoji = MapPlaceholderFactory.CreateText(transform, "Emoji", "🧑", 64, 0.09f, Color.white, 21,
                new Vector3(0f, 0.12f, 0f));
            _label = MapPlaceholderFactory.CreateText(transform, "Label", message, 40, 0.055f, Color.white, 21,
                new Vector3(0f, 0.85f, 0f));
        }

        void Update()
        {
            _age += Time.deltaTime;
            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

            float alpha = 1f;
            if (_age < FadeInSeconds)
                alpha = _age / FadeInSeconds;
            else if (_age > Duration - FadeOutSeconds)
                alpha = Mathf.Clamp01((Duration - _age) / FadeOutSeconds);

            SetAlpha(alpha);
            if (_age >= Duration)
                Destroy(gameObject);
        }

        void SetAlpha(float alpha)
        {
            Color bodyColor = _body.color;
            bodyColor.a = alpha;
            _body.color = bodyColor;

            Color textColor = _emoji.color;
            textColor.a = alpha;
            _emoji.color = textColor;
            _label.color = textColor;
        }
    }
}
