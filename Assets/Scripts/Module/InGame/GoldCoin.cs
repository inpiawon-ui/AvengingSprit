using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// HUD 로 날아가는 동전 한 닢.
    ///
    /// 골드가 들어올 때 숫자만 올리면 **무엇을 얻었는지**가 안 읽힌다. 방을 비운 것도,
    /// 이벤트에서 받은 것도 화면상 똑같이 "숫자가 조금 늘었다" 로 보인다.
    /// 얻은 자리에서 동전이 튀어 HUD 로 빨려 들어가야 원인과 결과가 이어진다.
    ///
    /// 그림은 **HUD 골드 아이콘을 빌려 쓴다.** 새 리소스를 만들지 않는다.
    ///
    /// 흐름은 두 마디다. 한 마디로 곧장 날리면 그냥 선이 지나간 것처럼 보인다.
    ///   튀기 <see cref="BurstSeconds"/>  얻은 자리에서 흩어진다 (감속)
    ///   빨리기 <see cref="HomeSeconds"/> HUD 아이콘으로 모인다 (가속)
    /// </summary>
    public sealed class GoldCoin : MonoBehaviour
    {
        private const float BurstSeconds = 0.26f;
        private const float HomeSeconds  = 0.40f;
        private const float PopSeconds   = 0.08f;   // 0 → 제 크기
        private const float ArriveScale  = 0.55f;   // 빨려 들어가며 작아진다

        private RectTransform _rect;
        private Image _image;

        private Vector2 _from, _scatter, _to;
        private float _delay;
        private float _time;
        private bool _arrived;

        public bool IsActive => gameObject.activeSelf;

        public static GoldCoin Create(Transform parent, Sprite sprite, Vector2 size)
        {
            var go = new GameObject("GoldCoin", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var c = go.AddComponent<GoldCoin>();
            c._rect = (RectTransform)go.transform;
            c._rect.anchorMin = c._rect.anchorMax = c._rect.pivot = new Vector2(0.5f, 0.5f);
            c._rect.sizeDelta = size;
            c._image = go.GetComponent<Image>();
            c._image.sprite = sprite;
            c._image.raycastTarget = false;
            c._image.preserveAspect = true;
            go.SetActive(false);
            return c;
        }

        /// <summary>좌표는 모두 부모 레이어 기준이다. <paramref name="delay"/> 로 한 닢씩 어긋나게 띄운다.</summary>
        public void Play(Vector2 from, Vector2 to, float delay, float scatterRadius)
        {
            _from = from;
            _to = to;
            _delay = delay;
            _time = 0f;
            _arrived = false;

            // 위쪽으로 치우쳐 흩어진다. 고르게 뿌리면 절반이 바닥으로 내려가
            // HUD(화면 위)로 되돌아오는 길이 어색해진다.
            float a = Random.Range(20f, 160f) * Mathf.Deg2Rad;
            float r = scatterRadius * Random.Range(0.55f, 1f);
            _scatter = from + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);

            _rect.anchoredPosition = from;
            _rect.localScale = Vector3.zero;
            gameObject.SetActive(true);
        }

        /// <summary>도착한 **그 프레임에만** true. 부르는 쪽이 이때 숫자를 한 칸 올린다.</summary>
        public bool Tick(float dt)
        {
            if (_arrived || !gameObject.activeSelf) return false;

            _time += dt;
            if (_time < _delay) return false;

            float t = _time - _delay;

            if (t < BurstSeconds)
            {
                float k = t / BurstSeconds;
                _rect.anchoredPosition = Vector2.Lerp(_from, _scatter, 1f - (1f - k) * (1f - k));   // 감속
                _rect.localScale = Vector3.one * Mathf.Min(1f, t / PopSeconds);
                return false;
            }

            float h = (t - BurstSeconds) / HomeSeconds;
            if (h >= 1f)
            {
                _arrived = true;
                gameObject.SetActive(false);
                return true;
            }

            _rect.anchoredPosition = Vector2.Lerp(_scatter, _to, h * h);   // 가속 — 빨려 들어간다
            _rect.localScale = Vector3.one * Mathf.Lerp(1f, ArriveScale, h);
            return false;
        }

        public void Despawn()
        {
            _arrived = true;
            gameObject.SetActive(false);
        }
    }
}
