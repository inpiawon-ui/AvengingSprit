using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 기본 공격 투사체. 발사 시점의 대상을 향해 날아가 명중하면 피해를 넘긴다.
    ///
    /// 유도(호밍)하지 않는다 — 발사 방향을 그대로 유지한다. 오토어택 게임에서
    /// 전탄 명중이 보장되면 이동으로 회피할 여지가 사라져 조작이 무의미해진다.
    ///
    /// 풀링해서 재사용한다. 매 발마다 GameObject 를 만들면 교전 중 GC 가 튄다.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private RectTransform _rect;
        private Image _image;

        private Vector2 _dir;
        private float _speed;
        private float _life;
        private int _damage;
        private Unit _target;
        private bool _fromPlayer;

        /// <summary>관통탄은 맞아도 사라지지 않는다. 같은 대상을 두 번 때리지 않도록 기록한다.</summary>
        private readonly List<Unit> _alreadyHit = new();

        public bool IsActive { get; private set; }
        public bool FromPlayer => _fromPlayer;
        public int Damage => _damage;
        public Unit Target => _target;
        public Vector2 Position => _rect.anchoredPosition;

        public bool Pierce { get; private set; }

        /// <summary>
        /// 이 탄만의 폭발 반경(px). 0 이면 공통값을 쓴다.
        ///
        /// 스킬이 던지는 폭탄은 반경이 제각각이다(융단 폭격 1.8 m · 다중 유도 1.0 m).
        /// 공통값 하나로 두면 두 스킬이 화면에서 구별되지 않는다.
        /// </summary>
        public float BlastRadiusOverride { get; private set; }

        public void SetBlastRadius(float px) => BlastRadiusOverride = Mathf.Max(0f, px);

        /// <summary>되받아친 탄에 관통을 준다(슬러거 Lv5).</summary>
        public void GrantPierce() => Pierce = true;

        /// <summary>
        /// 내 탄을 **적 편으로** 돌린다 (가디언 반사선).
        /// <see cref="TurnFriendly"/> 의 반대다 — 그쪽은 적 탄을 내 것으로 만든다.
        /// </summary>
        public void TurnHostile(float damageMul)
        {
            _fromPlayer = false;
            _dir = -_dir;
            _damage = Mathf.Max(1, Mathf.RoundToInt(_damage * damageMul));
            _alreadyHit.Clear();
            _rect.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);
        }

        /// <summary>이 탄이 하나라도 맞혔는가. 연속 명중 카운터가 읽는다.</summary>
        public bool HasHitAnything => _alreadyHit.Count > 0;

        /// <summary>
        /// 이 탄이 플레이어를 **스쳤지만 맞히지는 못했는가** (C023 회피 잔상).
        /// 회피 시스템이 따로 없는 게임이라, "피했다"의 실체는 이것뿐이다 —
        /// 몸에 닿을 뻔한 탄이 그대로 지나가 수명을 다한 순간.
        /// </summary>
        public bool GrazedPlayer { get; set; }

        /// <summary>남은 도탄 횟수. 0 이면 벽에 닿는 순간 사라진다.</summary>
        public int BouncesLeft { get; private set; }

        /// <summary>도탄을 한 번이라도 했는가 (정본 BUF_T05 뱅크 샷의 조건).</summary>
        public bool HasBounced { get; private set; }
        public int SlowPercent { get; private set; }
        public int LifestealPercent { get; private set; }

        public bool HasHit(Unit u) => _alreadyHit.Contains(u);
        public void MarkHit(Unit u) => _alreadyHit.Add(u);

        /// <summary>
        /// 탄 그림을 갈아 끼운다. 탄은 풀에서 돌려 쓰므로 태어날 때 정한 그림을
        /// 계속 쓰면 **직전에 쏜 무기의 탄이 그대로 나간다.** 발사할 때마다 정한다.
        /// </summary>
        /// <summary>
        /// 탄 종류(`bullet`·`laser`…). 맞았을 때 터지는 그림을 고르는 데 쓴다.
        /// 스프라이트 이름에서 되짚으면 아틀라스 이름 규칙에 묶여 깨지기 쉽다.
        /// </summary>
        public string Kind { get; private set; }

        // ── 태어나는 애니메이션 ──────────────────────────────────
        //
        // 원작 탄 그림은 반복 동작이 아니라 **태어나는 모습**이다 —
        // 눈덩이는 작은 것이 커지고, 화염은 피어난다.
        // 그래서 한 번만 돌리고 다 자란 마지막 장에서 멈춘다.
        // 계속 돌리면 커졌다 작아졌다 덜렁거린다.

        /// <summary>장 수와 무관하게 이 시간 안에 다 자란다.</summary>
        private const float SpawnAnimSeconds = 0.2f;

        /// <summary>
        /// 태어날 때의 크기 비율. 그림만으로는 커지는 게 거의 안 보인다 —
        /// 눈덩이는 3장 중 첫 장만 작고(16→24), 총알·레이저는 아예 한 장뿐이다.
        /// 상자 크기를 같이 키워야 "작은 것이 커진다"가 눈에 들어온다.
        /// </summary>
        private const float SpawnStartScale = 0.5f;

        /// <summary>
        /// 다만 **전부** 태어나는 모습은 아니다. 원작 시트의 박쥐 2장(날개 편 것 /
        /// 접은 것)과 표창 2장(회전)은 반복 동작이다 — 한 번만 넘기고 멈추면
        /// 날개를 접은 채 날아가는 박쥐가 된다. 그런 종류는 계속 돌린다.
        /// </summary>
        private const float LoopFrameSeconds = 0.08f;

        private Sprite[] _frames;
        private float _frameSeconds;
        private float _frameTimer;
        private int _frameIndex;
        private bool _loopFrames;

        private float _size;
        private float _spawnTimer;

        // ── 던지는 탄 ────────────────────────────────────────────
        //
        // 수류탄은 곧게 날지 않는다. 기둥을 넘겨 던지고, 사람이 아니라 **땅**을 노린다.
        // 그래서 나는 동안은 아무것도 맞히지 않고 떨어진 자리에서 터진다.
        // 화면에서 포물선으로 보여야 "던졌다"가 읽힌다 — 곧게 가면 느린 총알이다.

        private const float LobSpinPerSecond = 540f;

        // ── C009 유도 보정 ───────────────────────────────────────
        //
        // 원래 이 게임의 탄은 유도하지 않는다(전탄 명중이 보장되면 회피가 무의미해진다).
        // 카드를 골랐을 때만 켠다 — 그 선택의 값이 곧 "빗나가지 않는다" 다.
        private float _homing;

        public void SetHoming(float strength) => _homing = Mathf.Max(0f, strength);

        private void TickHoming(float dt)
        {
            if (_homing <= 0f || _target == null || !_target.IsAlive) return;
            var want = (_target.Position - _rect.anchoredPosition);
            if (want.sqrMagnitude < 0.0001f) return;
            // 초당 최대 이만큼(라디안) 꺾인다. 세기 1.0 이면 반 바퀴 가까이 돈다.
            _dir = Vector2.Lerp(_dir, want.normalized, Mathf.Clamp01(_homing * dt * 6f)).normalized;
            _rect.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);
        }

        private Vector2 _lobFrom, _lobTo;
        private float _lobSeconds, _lobElapsed, _lobHeight;

        public bool IsLob => _lobSeconds > 0f;

        /// <summary>땅에 닿았는가. 부르는 쪽이 이걸 보고 터뜨린다.</summary>
        public bool HasLanded { get; private set; }

        /// <summary>
        /// 곧게 날던 탄을 던지는 탄으로 바꾼다. <see cref="Fire"/> **뒤에** 부른다 —
        /// Fire 가 피해·색·수명을 정하고, 여기서 궤적만 갈아 끼운다.
        /// </summary>
        public void Lob(Vector2 landing, float seconds, float arcHeight)
        {
            _lobFrom = _rect.anchoredPosition;
            _lobTo = landing;
            _lobSeconds = Mathf.Max(0.05f, seconds);
            _lobElapsed = 0f;
            _lobHeight = arcHeight;
            HasLanded = false;
            // 진행 방향으로 눕히지 않는다. 던진 물건은 돌면서 간다.
            _rect.localEulerAngles = Vector3.zero;
        }

        private void TickLob(float dt)
        {
            _lobElapsed += dt;
            float t = Mathf.Clamp01(_lobElapsed / _lobSeconds);
            // 땅 위의 자리는 곧게 간다. 눈에 보이는 높이만 포물선이다.
            var ground = Vector2.Lerp(_lobFrom, _lobTo, t);
            _rect.anchoredPosition = ground + Vector2.up * (4f * _lobHeight * t * (1f - t));
            _rect.localEulerAngles += new Vector3(0f, 0f, LobSpinPerSecond * dt);
            if (t >= 1f) HasLanded = true;
        }

        public void SetSprite(Sprite sprite, string kind = null)
            => SetSprite(sprite == null ? null : new[] { sprite }, kind);

        /// <param name="loop">반복 동작인가(박쥐 날갯짓·표창 회전). false 면 태어나는 모습.</param>
        public void SetSprite(Sprite[] frames, string kind = null, bool loop = false)
        {
            _frames = frames != null && frames.Length > 0 ? frames : null;
            _loopFrames = loop;
            // 반복 동작은 **마지막 장에서 시작한다.** 원작 박쥐 시트는 1번이 편 날개,
            // 2번이 접은 날개다. 1번부터 돌리면 편 채로 태어나 접었다 펴는 것이
            // 반 박자 늦게 읽힌다 — 접은 채 나와 펴면서 날아가야 "퍼덕인다"가 된다.
            // 회전(표창·수류탄)은 어느 장에서 시작하든 같다.
            _frameIndex = loop && _frames != null ? _frames.Length - 1 : 0;
            _frameSeconds = _frames == null ? 0f
                          : loop ? LoopFrameSeconds
                                 : SpawnAnimSeconds / _frames.Length;
            _frameTimer = _frameSeconds;
            if (_frames != null && _image != null) _image.sprite = _frames[_frameIndex];
            Kind = kind;
        }

        /// <summary>한 장짜리거나 (반복이 아닌데) 이미 다 자랐으면 아무것도 하지 않는다.</summary>
        private void TickFrames(float dt)
        {
            if (_frames == null || _image == null || _frames.Length < 2) return;
            if (!_loopFrames && _frameIndex >= _frames.Length - 1) return;   // 마지막 장에서 멈춘다
            _frameTimer -= dt;
            if (_frameTimer > 0f) return;
            _frameTimer += _frameSeconds;
            _frameIndex = _loopFrames ? (_frameIndex + 1) % _frames.Length : _frameIndex + 1;
            _image.sprite = _frames[_frameIndex];
        }

        /// <summary>다 자랄 때까지만 상자를 키운다.</summary>
        private void TickGrow(float dt)
        {
            if (_spawnTimer >= SpawnAnimSeconds) return;
            _spawnTimer += dt;
            float t = Mathf.Clamp01(_spawnTimer / SpawnAnimSeconds);
            float s = _size * Mathf.Lerp(SpawnStartScale, 1f, Mathf.SmoothStep(0f, 1f, t));
            _rect.sizeDelta = new Vector2(s, s);
        }

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

        public void Fire(Vector2 from, Vector2 to, float speed, int damage,
                         bool fromPlayer, Unit target, float size, Color color, float life,
                         bool pierce = false, int slowPercent = 0, int lifestealPercent = 0,
                         float angleOffsetDeg = 0f, int bounces = 0)
        {
            Pierce = pierce;
            BouncesLeft = bounces;
            HasBounced = false;
            SlowPercent = slowPercent;
            LifestealPercent = lifestealPercent;
            _alreadyHit.Clear();
            _rect.anchoredPosition = from;
            _size = size;
            _spawnTimer = 0f;
            _lobSeconds = 0f;      // 풀에서 온 탄이 직전의 포물선을 물려받지 않게
            BlastRadiusOverride = 0f;   // 반경도 마찬가지다 — 쏠 때마다 다시 정한다
            HasLanded = false;
            _homing = 0f;          // 유도도 마찬가지다 — 쏠 때마다 다시 정한다
            float born = size * SpawnStartScale;
            _rect.sizeDelta = new Vector2(born, born);
            var d = to - from;
            _dir = d.sqrMagnitude < 0.0001f ? Vector2.up : d.normalized;
            if (Mathf.Abs(angleOffsetDeg) > 0.01f)
            {
                float r = angleOffsetDeg * Mathf.Deg2Rad;
                float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
                _dir = new Vector2(_dir.x * cos - _dir.y * sin, _dir.x * sin + _dir.y * cos);
            }
            // 탄환 스프라이트가 가로로 길어서, 돌려주지 않으면 위로 쏴도 오른쪽을 본다
            _rect.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);
            _speed = speed;
            _damage = damage;
            _fromPlayer = fromPlayer;
            _target = target;
            _life = life;
            // 원작 그림에는 색을 입히지 않는다. 눈덩이는 하얗고, 불은 주황이다 —
            // 곱셈 색조를 씌우면 눈덩이가 노래진다.
            // 아군·적군 구분용 색조는 그림 없는 기본 탄(Kind 없음)에만 남긴다.
            _image.color = Kind != null ? Color.white : color;
            GrazedPlayer = false;   // 풀에서 돌려 쓰므로 지난 판정을 지운다
            IsActive = true;
            gameObject.SetActive(true);
        }

        public void Despawn()
        {
            IsActive = false;
            _target = null;
            gameObject.SetActive(false);
        }

        /// <summary>이동시킨다. 수명이 다하면 false.</summary>
        public bool Tick(float dt)
        {
            if (IsLob) TickLob(dt);
            else { TickHoming(dt); _rect.anchoredPosition += _dir * _speed * dt; }
            TickFrames(dt);
            TickGrow(dt);
            _life -= dt;
            return _life > 0f;
        }

        /// <summary>
        /// 적 탄을 **내 탄으로 돌린다** (슬러거 그랜드 슬램).
        /// 방향을 뒤집고 주인을 바꾼다 — 지우는 것과 달리 화면의 탄이 그대로 자산이 된다.
        /// </summary>
        public void TurnFriendly(float damageMul)
        {
            _fromPlayer = true;
            _dir = -_dir;
            _damage = Mathf.Max(1, Mathf.RoundToInt(_damage * damageMul));
            _alreadyHit.Clear();
            _rect.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// 벽에 튕긴다. 남은 횟수가 없으면 false — 부르는 쪽이 없앤다.
        /// <paramref name="normal"/> 은 부딪힌 면의 바깥 방향이다.
        ///
        /// 튕긴 뒤 **맞은 목록을 비운다.** 안 그러면 되돌아온 탄이 방금 지나친 적을
        /// 그냥 통과한다 — 도탄의 재미는 왔던 길을 다시 훑는 데 있다.
        /// </summary>
        public bool Bounce(Vector2 normal)
        {
            if (BouncesLeft <= 0) return false;
            BouncesLeft--;
            HasBounced = true;
            _dir = Vector2.Reflect(_dir, normal).normalized;
            _rect.localEulerAngles =
                new Vector3(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);
            _alreadyHit.Clear();
            return true;
        }
    }
}
