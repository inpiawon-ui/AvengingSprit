using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>장판이 안에 있는 대상에게 무엇을 하는가.</summary>
    public enum FieldEffect
    {
        /// <summary>초당 피해만 준다</summary>
        Damage,
        /// <summary>둔화</summary>
        Slow,
        /// <summary>화상</summary>
        Burn,
        /// <summary>빙결</summary>
        Freeze,
        /// <summary>저주</summary>
        Curse,
    }

    /// <summary>
    /// 바닥에 깔리는 지속 영역.
    ///
    /// 정본이 요구하는 것 중 이것 하나가 여러 갈래를 막고 있었다 —
    /// 버프 4종(차가운 기하·지속하는 장판·지뢰 연금술·저주 화염)과
    /// 보스 패턴(독구름·서리 룬)이 전부 "바닥에 깔린 영역"을 전제한다.
    ///
    /// 탄과 같은 풀 방식이다. 장판은 한 방에 여럿이 겹칠 수 있어
    /// 매번 GameObject 를 만들면 교전 중에 GC 가 튄다.
    ///
    /// ⚠ 피해를 스스로 주지 않는다. 죽음 처리·보상·피해 숫자가 전부
    ///    BattleDirector 에 있으므로 **누가 안에 있는지만 알려준다.**
    ///    상태이상(`Unit.TickStatus`)에서 같은 이유로 같은 선택을 했다.
    /// </summary>
    public sealed class Field : MonoBehaviour
    {
        /// <summary>효과가 떨어지는 간격. 매 프레임 주면 초당 60번이 된다.</summary>
        private const float TickInterval = 0.5f;
        private const float FadeSeconds = 0.4f;   // 사라지기 전 마지막 구간

        private RectTransform _rect;
        private Image _image;
        private float _life;
        private float _lifeMax;
        private float _tickTimer;
        private Color _base;

        public Vector2 Center { get; private set; }
        public float Radius { get; private set; }
        public FieldEffect Effect { get; private set; }
        public int DamagePerTick { get; private set; }
        public bool FromPlayer { get; private set; }
        public bool IsActive => _life > 0f;

        public void Init(Sprite sprite)
        {
            _rect = (RectTransform)transform;
            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _image = GetComponent<Image>();
            if (_image == null) _image = gameObject.AddComponent<Image>();
            _image.sprite = sprite;
            _image.raycastTarget = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// ⚠ 그림을 깔 때마다 정한다. 장판도 풀에서 돌려 쓰므로 태어날 때 정하면
        ///    직전 효과의 그림이 그대로 남는다. 없으면 감춘다.
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            _frames = null;
            _image.sprite = sprite;
            _image.enabled = sprite != null;
        }

        // ── 돌아가는 장판 ──────────────────────────────────────
        //
        // 장판은 6초를 사는데 그림은 한 장이라 **깔린 순간부터 멈춰 있었다.**
        // 불바다가 타는 것이 아니라 불 그림을 바닥에 붙여 놓은 것으로 보였다
        // (기획 2026-09-15). 여러 장을 받으면 사는 내내 돌린다.

        private const float FrameSeconds = 0.16f;
        private Sprite[] _frames;
        private float _frameTimer;
        private int _frame;

        /// <summary>여러 장으로 돌린다. 장판이 사는 내내 처음부터 끝까지 반복한다.</summary>
        public void SetFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) { SetSprite(null); return; }
            _frames = frames;
            _frame = 0;
            _frameTimer = FrameSeconds;
            _image.sprite = frames[0];
            _image.enabled = true;
        }

        public void Spawn(Vector2 at, float radius, float seconds, FieldEffect effect,
                          int damagePerTick, bool fromPlayer, Color color)
        {
            Center = at;
            Radius = radius;
            Effect = effect;
            DamagePerTick = damagePerTick;
            FromPlayer = fromPlayer;
            _life = _lifeMax = seconds;
            _tickTimer = 0f;                 // 깔리는 순간 한 번 터진다
            _base = color;

            _rect.anchoredPosition = new Vector2(Mathf.Round(at.x), Mathf.Round(at.y));
            _rect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            _image.color = color;
            gameObject.SetActive(true);
        }

        /// <summary>시간을 흘린다. 이번 프레임에 효과를 떨어뜨릴 차례면 true.</summary>
        public bool Tick(float dt)
        {
            if (_life <= 0f) return false;
            _life -= dt;
            if (_life <= 0f) { Despawn(); return false; }

            if (_frames != null)
            {
                _frameTimer -= dt;
                if (_frameTimer <= 0f)
                {
                    _frameTimer += FrameSeconds;
                    _frame = (_frame + 1) % _frames.Length;
                    if (_frames[_frame] != null) _image.sprite = _frames[_frame];
                }
            }

            // 끝나기 전에 흐려진다 — 갑자기 사라지면 언제 안전해졌는지 알 수 없다
            var c = _base;
            if (_life < FadeSeconds) c.a *= _life / FadeSeconds;
            _image.color = c;

            _tickTimer -= dt;
            if (_tickTimer > 0f) return false;
            _tickTimer = TickInterval;
            return true;
        }

        public bool Contains(Vector2 p) => (p - Center).sqrMagnitude <= Radius * Radius;

        /// <summary>남은 시간을 늘린다 (정본 BUF_A02 지속하는 장판).</summary>
        public void Extend(float seconds)
        {
            _life += seconds;
            _lifeMax += seconds;
        }

        public void Despawn()
        {
            _life = 0f;
            gameObject.SetActive(false);
        }
    }
}
