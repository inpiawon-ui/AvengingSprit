using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 대시 잔상 한 장.
    ///
    /// 몸 스프라이트를 그대로 복제해 단색으로 눕히고, 알파를 **계단으로** 낮춘다.
    ///
    /// ⚠ 그라데이션으로 부드럽게 흐르게 하지 않는다 (확정 #16 · Point 필터,
    ///   JobClasses §4 · 파티클·셰이더 안 씀). 부드러운 잔상은 도트 화면에서
    ///   혼자 매끈해 보여서 오히려 튄다. 3단계로 뚝뚝 끊어야 원작 감성에 붙는다.
    ///
    /// 그림이 필요 없다 — 몸 그림을 빌려 쓴다.
    /// </summary>
    public sealed class Afterimage : MonoBehaviour
    {
        /// <summary>잔상 색. 청록 한 가지로 눕힌다.</summary>
        public static readonly Color Tint = new(0.498f, 0.831f, 0.910f, 1f);

        private RectTransform _rect;
        private Image _image;
        private float _life;
        private float _lifeMax;
        private float _alpha;

        public static Afterimage Create(Transform parent)
        {
            var go = new GameObject("Afterimage", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<Afterimage>();
            a._rect = go.GetComponent<RectTransform>();
            a._image = go.GetComponent<Image>();
            a._image.raycastTarget = false;
            go.SetActive(false);
            return a;
        }

        /// <summary>
        /// 한 장을 띄운다. <paramref name="alpha"/> 는 계단값(0.6 / 0.35 / 0.15)이고,
        /// 사는 동안 그 값을 **유지하다가 끝에 사라진다** — 서서히 옅어지지 않는다.
        /// </summary>
        public void Play(Sprite sprite, Vector2 at, Vector2 size, bool flipX,
                         float alpha, float seconds)
        {
            if (sprite == null) { gameObject.SetActive(false); return; }
            _image.sprite = sprite;
            _image.color = new Color(Tint.r, Tint.g, Tint.b, alpha);
            _rect.sizeDelta = size;
            _rect.anchoredPosition = at;
            _rect.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);
            _alpha = alpha;
            _lifeMax = Mathf.Max(0.01f, seconds);
            _life = _lifeMax;
            gameObject.SetActive(true);
        }

        public bool IsPlaying => _life > 0f;

        public void Tick(float dt)
        {
            if (_life <= 0f) return;
            _life -= dt;
            if (_life > 0f) return;
            _life = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>남은 알파. 마지막 구간에서만 한 계단 더 떨군다.</summary>
        public void Refresh()
        {
            if (_life <= 0f) return;
            float t = _life / _lifeMax;
            float a = t > 0.5f ? _alpha : _alpha * 0.5f;   // 계단 두 칸 — 흐르지 않는다
            _image.color = new Color(Tint.r, Tint.g, Tint.b, a);
        }
    }
}
