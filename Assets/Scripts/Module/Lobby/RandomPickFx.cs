using Game.Module.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 로비 「?(랜덤 선택)」 의 유령 불꽃 문장 연출 (PD 2026-10-12, 시안 `_exchange/in/mock_random_cardback.png` ③ 카드 뒷면).
    ///
    /// 층(뒤 → 앞): 푸른 번짐(가산 · 셰이더) → 불꽃 문장(그림 6장 루프) → 문장 섬광 겹(가산) → 반짝 별(가산).
    ///   · 머묾(약) — 번짐이 2초 숨쉬기, 별이 엇갈려 깜빡이고 가끔 하나가 금빛으로 반짝(전설 암시).
    ///   · 고르는 순간(강) — <see cref="Flash"/>: 문장이 하얗게 번쩍이고 번짐이 한 번 부풀며 별이 함께 켜진다.
    /// 빛은 그림에 굽지 않고 여기서 준다(규칙 08_fx). 노드는 `ChapterHostInfoBinder` 가 꽂는다.
    /// </summary>
    public sealed class RandomPickFx : MonoBehaviour
    {
        [SerializeField] private Sprite[] _frames = System.Array.Empty<Sprite>();
        [SerializeField] private Image _emblem;
        [SerializeField] private Image _emblemFlash;   // 같은 그림 · 가산 재질 — 번쩍임 겹
        [SerializeField] private Graphic _glow;   // 둥근 번짐 — UI/AdditiveLight 고리(반지름 0)로 그린다
        [SerializeField] private RawImage[] _stars = System.Array.Empty<RawImage>();
        [SerializeField] private float _frameRate;   // 0 = 고정(기본). 일렁이게 하려면 8 안팎

        /// <summary>번짐 숨쉬기 한 바퀴(초).</summary>
        private const float BreathSeconds = 2f;
        /// <summary>고르는 순간 섬광 길이(초).</summary>
        private const float FlashSeconds = 0.45f;
        /// <summary>금빛 반짝 간격(초) — 「낮은 확률로 전설」을 은은하게.</summary>
        private const float GoldEvery = 2.6f;

        private static readonly Color GlowColor = new(0.35f, 0.70f, 1f, 1f);
        private static readonly Color StarColor = new(0.75f, 0.90f, 1f, 1f);
        private static readonly Color GoldColor = new(1f, 0.85f, 0.35f, 1f);

        private float _time;
        private float _flashLeft;
        private float _goldIn = GoldEvery;
        private int _goldStar = -1;
        private float _goldLeft;
        private Vector2 _glowSize;

        private void Awake()
        {
            if (_glow != null) _glowSize = _glow.rectTransform.sizeDelta;
        }

        private void OnEnable()
        {
            _time = Random.value * BreathSeconds;   // 카드 · 설명 칸 두 개가 같은 박자로 맞물리지 않게
            _flashLeft = 0f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _time += dt;

            // 불꽃 일렁임 — _frameRate 0 이면 첫 장 고정(PD 2026-10-12 「? 카드에서 혼자만 움직이는 연출은 넣지 마」)
            if (_emblem != null && _frames.Length > 0)
            {
                var frame = _frameRate > 0f ? _frames[(int)(_time * _frameRate) % _frames.Length] : _frames[0];
                _emblem.sprite = frame;
                if (_emblemFlash != null) _emblemFlash.sprite = frame;
            }

            // 섬광(강) — 처음 빠르게 차고 천천히 빠진다
            float flash = 0f;
            if (_flashLeft > 0f)
            {
                _flashLeft = Mathf.Max(0f, _flashLeft - dt);
                flash = 1f - Ease.OutCubic(1f - _flashLeft / FlashSeconds);
            }
            if (_emblemFlash != null) _emblemFlash.color = new Color(1f, 1f, 1f, 0.85f * flash);

            // 번짐 숨쉬기(약) + 섬광 때 한 번 부풂
            float breath = Ease.InOutSine(Mathf.PingPong(_time / (BreathSeconds * 0.5f), 1f));
            if (_glow != null)
            {
                float a = Mathf.Lerp(0.35f, 0.65f, breath) + 0.35f * flash;
                _glow.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, Mathf.Clamp01(a));
                _glow.rectTransform.sizeDelta = _glowSize * (1f + 0.06f * breath + 0.25f * flash);
            }

            // 반짝 별 — 엇갈린 깜빡임 + 가끔 금빛 하나
            _goldIn -= dt;
            if (_goldIn <= 0f && _stars.Length > 0)
            {
                _goldIn = GoldEvery;
                _goldStar = Random.Range(0, _stars.Length);
                _goldLeft = 0.6f;
            }
            if (_goldLeft > 0f) _goldLeft = Mathf.Max(0f, _goldLeft - dt);
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = _stars[i];
                if (s == null) continue;
                float tw = Mathf.Max(0f, Mathf.Sin(_time * 3.1f + i * 1.7f));
                float a = 0.15f + 0.75f * tw * tw + flash;
                var c = StarColor;
                if (i == _goldStar && _goldLeft > 0f)
                {
                    float g = Mathf.Sin(_goldLeft / 0.6f * Mathf.PI);
                    c = Color.Lerp(StarColor, GoldColor, g);
                    a = Mathf.Max(a, g);
                }
                s.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
            }
        }

        /// <summary>고르는 순간 — 문장이 번쩍이고 번짐이 부풀며 별이 함께 켜진다.</summary>
        public void Flash() => _flashLeft = FlashSeconds;
    }
}
