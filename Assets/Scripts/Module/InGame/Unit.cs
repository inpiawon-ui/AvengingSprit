using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>전투 필드 위의 개체 한 기. 고스트·호스트·적·보스가 모두 이 한 종류다.</summary>
    /// <summary>
    /// 적 행동 상태 (기획서 A 4-1 적 상태 흐름).
    ///   Idle → Detect → Approach → Attack → Cooldown → (Hit) → Dead
    /// Hit 는 흐름의 단계가 아니라 어느 상태에서든 끼어드는 반응이다.
    /// </summary>
    public enum EnemyState
    {
        /// <summary>순찰·대기. 아직 플레이어를 못 봤다</summary>
        Idle,
        /// <summary>플레이어를 감지했다</summary>
        Detect,
        /// <summary>사거리 안으로 접근 중</summary>
        Approach,
        /// <summary>공격 중</summary>
        Attack,
        /// <summary>공격 후 대기</summary>
        Cooldown,
        /// <summary>피격 반응</summary>
        Hit,
        /// <summary>사망 처리</summary>
        Dead,
    }

    public enum UnitSide
    {
        Player,
        Enemy,
    }

    /// <summary>
    /// 인게임 유닛. UI 캔버스 위에 Image 로 그린다(전투 필드가 `RoomField` 하위 RectTransform).
    ///
    /// 별도 물리를 쓰지 않는다. 방 기반 오토어택은 위치·거리 계산만으로 충분하고,
    /// Rigidbody 를 걸면 UI 좌표계와 물리 좌표계가 섞여 디버깅이 어려워진다.
    /// </summary>
    public sealed class Unit : MonoBehaviour
    {
        private RectTransform _rect;
        private Image _body;
        private Image _hpBarBg;
        private Image _hpBarFill;
        private Image _possessMark;
        private Image _fireRing;

        private float _attackTimer;
        private float _flashTimer;
        private int _slowPercent;
        private float _slowTimer;
        private bool _telegraph;

        public UnitSide Side { get; private set; }
        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public int Hp { get; private set; }
        public int HpMax { get; private set; }
        public int Atk { get; private set; }
        public float MoveSpeed { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackInterval { get; private set; }
        public bool IsBoss { get; private set; }
        public bool IsAlive => Hp > 0;

        public Vector2 Position
        {
            get => _rect.anchoredPosition;
            set => _rect.anchoredPosition = value;
        }

        /// <summary>
        /// 지금 이 몸을 빼앗을 수 있는가 (정본 POSSESSION_MATRIX).
        ///
        /// 보스는 언제나 불가다. 나머지는 프로필의 방식을 따른다 —
        /// 즉시면 바로, 조건부면 체력을 임계 아래로 깎아 놔야 열린다.
        /// 프로필이 없는 적(임시 스프라이트 등)은 즉시로 본다. 못 뺏는 적이
        /// 조용히 늘어나면 방이 통째로 막힌다.
        /// </summary>
        public bool IsPossessable
        {
            get
            {
                if (Side != UnitSide.Enemy || IsBoss || !IsAlive || _dying) return false;
                if (Profile == null) return true;
                return Profile.PossessKind switch
                {
                    Game.Character.PossessKind.NotPossessable => false,
                    Game.Character.PossessKind.Condition => HpPercent <= Profile.PossessHpPercent,
                    _ => true,
                };
            }
        }

        /// <summary>빙의 조건이 걸려 있는 적인가. 표식을 어떻게 그릴지가 갈린다.</summary>
        public bool HasPossessCondition =>
            Profile != null && Profile.PossessKind == Game.Character.PossessKind.Condition;

        /// <summary>
        /// 조건 진행도 0~1. 체력이 임계에 닿으면 1이다.
        /// 표식의 게이지가 이 값을 그린다 — 얼마나 더 때려야 열리는지가 보여야
        /// "왜 안 잡히지"가 "조금만 더"가 된다.
        /// </summary>
        public float PossessProgress
        {
            get
            {
                if (!HasPossessCondition) return 1f;
                int gate = Profile.PossessHpPercent;
                if (gate >= 100) return 1f;
                // 100% → 0, 임계 → 1
                return Mathf.Clamp01((100f - HpPercent) / (100f - gate));
            }
        }

        private float HpPercent => HpMax > 0 ? Hp * 100f / HpMax : 0f;

        /// <summary>공격 방식 데이터. 보스는 null (기본 단발).</summary>
        public Game.Character.HostEntry Profile { get; private set; }

        /// <summary>
        /// 플레이어를 인지했는가. false 면 제자리에서 기다린다.
        /// 한 번 켜지면 꺼지지 않는다 — 사거리 밖으로 나갔다고 잊어버리면
        /// 적이 왔다 갔다 하며 어그로가 끊긴 것처럼 보인다.
        /// </summary>
        public bool IsAggro { get; set; }

        /// <summary>
        /// 적 행동 상태 (기획서 A 4-1). 지금은 표시·디버그용이지만, 상태를 이름으로
        /// 들고 있어야 나중에 AI 타입별로 분기를 넣을 자리가 생긴다.
        /// 살아 있는 일반 적은 언제든 빙의 대상이 되므로(`IsPossessable`),
        /// 기획서의 POSSESSABLE 은 별도 상태가 아니라 이 플래그로 표현한다.
        /// </summary>
        public EnemyState State { get; private set; } = EnemyState.Idle;

        /// <summary>빙의 우선순위. 높을수록 먼저 잡힌다 (기획서 A 4-3).</summary>
        public int PossessPriority { get; set; }

        /// <summary>이 적에게 빙의할 수 있는 거리. 0 이면 전역 기본값을 쓴다.</summary>
        public float PossessRange { get; set; }

        public void SetState(EnemyState s)
        {
            if (Side != UnitSide.Enemy) return;
            State = s;
        }

        /// <summary>최대 체력 대비 비율로 현재 체력을 정한다. 빙의 시작 체력(70%)에 쓴다.</summary>
        public void SetHpPercent(int percent)
            => Hp = Mathf.Clamp(Mathf.RoundToInt(HpMax * percent / 100f), 1, HpMax);

        public void Setup(UnitSide side, string key, string displayName, Sprite sprite,
                          int hp, int atk, float moveSpeed, float attackRange,
                          float attackInterval, Vector2 size, bool isBoss = false,
                          Game.Character.HostEntry profile = null)
        {
            Profile = profile;
            _rect = (RectTransform)transform;
            Side = side;
            Key = key;
            DisplayName = displayName;
            HpMax = Mathf.Max(1, hp);
            Hp = HpMax;
            Atk = atk;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            AttackInterval = attackInterval;
            IsBoss = isBoss;

            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = size;

            _body = GetOrCreate("Body", size, Vector2.zero);
            _body.sprite = sprite;
            _baseSprite = sprite;
            for (int i = 0; i < _frames.Length; i++) _frames[i] = null;
            _facingIndex = -1;
            _facingFlip = false;
            _frame = FrameIdle;
            _frameTimer = 0f;
            _shownFrame = -1;
            _shownIndex = -1;
            _moving = false;
            _walkPhase = 0f;
            _dying = false;
            _deathTimer = 0f;
            _body.transform.localScale = Vector3.one;
            _body.preserveAspect = true;
            _body.raycastTarget = false;

            // 플레이어도 머리 위에 단다. 상단 HUD 에도 있지만 교전 중에는 시선이
            // 캐릭터에 있어서, 위를 봐야 남은 체력을 아는 것은 늦다.
            {
                var barSize = new Vector2(size.x * 0.7f, 5f);
                var barPos = new Vector2(0f, size.y * 0.5f + 6f);
                _hpBarBg = GetOrCreate("HpBarBg", barSize, barPos);
                _hpBarBg.color = new Color(0.06f, 0.07f, 0.10f, 0.9f);
                _hpBarFill = GetOrCreate("HpBarFill", barSize, barPos, _hpBarBg.transform);
                // 편을 색으로 가른다 — 붉은 바가 둘이면 누구 체력인지 헷갈린다
                _hpBarFill.color = side == UnitSide.Enemy
                    ? new Color(0.85f, 0.20f, 0.16f, 1f)
                    : new Color(0.35f, 0.85f, 0.40f, 1f);
                ((RectTransform)_hpBarFill.transform).pivot = new Vector2(0f, 0.5f);
                ((RectTransform)_hpBarFill.transform).anchoredPosition = new Vector2(-barSize.x * 0.5f, 0f);

                // 빙의 표식은 **적에게만** 단다. 내 몸에 "뺏을 수 있다" 표시가 뜨면 거짓말이다.
                if (!isBoss && side == UnitSide.Enemy)
                {
                    _possessMark = GetOrCreate("PossessMark", new Vector2(18f, 18f),
                                               new Vector2(0f, size.y * 0.5f + 20f));
                    _possessMark.color = new Color(0.55f, 0.80f, 1f, 0.95f);
                    _possessMark.gameObject.SetActive(false);
                }
            }
            RefreshHpBar();
        }

        private Image GetOrCreate(string name, Vector2 size, Vector2 pos, Transform parent = null)
        {
            var p = parent != null ? parent : transform;
            var t = p.Find(name);
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(p, false);
                t = go.transform;
            }
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = t.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        /// <summary>빙의 표식 상태 (정본 POSSESSION_MATRIX 의 UI 3단).</summary>
        public enum PossessMark
        {
            /// <summary>표식 없음</summary>
            None,
            /// <summary>조건이 안 찼다 — 회색. 게이지가 얼마나 남았는지 보여준다</summary>
            Progress,
            /// <summary>지금 뺏을 수 있다 — 보라</summary>
            Ready,
        }

        private Image _possessMeter;

        /// <summary>
        /// 빙의 표식을 그린다. 조건부 적은 **잠긴 상태도 보여야** 한다 —
        /// 아무 표시가 없으면 "왜 안 잡히지"로 끝나고, 게이지가 보이면 "조금만 더"가 된다.
        /// </summary>
        // ── 표식 (정본 시너지 S01) ────────────────────────────
        // 갱스터가 때린 적에 남는다. **몸을 갈아타도 사라지지 않는다** —
        // 그게 시너지의 전부다. 이전 몸이 만든 것을 다음 몸이 물려받는다.
        private float _markTimer;
        private Image _markView;

        public bool IsMarked => _markTimer > 0f;

        public void SetMark(float seconds)
        {
            _markTimer = Mathf.Max(_markTimer, seconds);
            ShowMark(true);
        }

        public void ClearMark()
        {
            _markTimer = 0f;
            ShowMark(false);
        }

        public void TickMark(float dt)
        {
            if (_markTimer <= 0f) return;
            _markTimer -= dt;
            if (_markTimer <= 0f) ShowMark(false);
        }

        private void ShowMark(bool on)
        {
            if (_markView == null)
            {
                if (!on) return;
                var size = _rect.sizeDelta;
                _markView = GetOrCreate("Mark", new Vector2(15f, 15f),
                                        new Vector2(size.x * 0.28f, size.y * 0.42f));
                _markView.color = new Color(1f, 0.35f, 0.30f, 0.95f);
                // 스프라이트가 없으면 Image 는 정사각형을 그린다. 그대로 두면 표식이
                // 아니라 그리다 만 흰(붉은) 네모로 보인다. 45° 돌려 마름모로 읽히게 한다.
                _markView.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            if (_markView.gameObject.activeSelf != on) _markView.gameObject.SetActive(on);
        }

        public void SetPossessMark(PossessMark state, float progress = 1f)
        {
            if (_possessMark == null) return;
            bool on = state != PossessMark.None;
            if (_possessMark.gameObject.activeSelf != on) _possessMark.gameObject.SetActive(on);
            if (!on) return;

            _possessMark.color = state == PossessMark.Ready
                ? new Color(0.62f, 0.45f, 1f, 0.95f)      // 보라 — 지금 누르면 된다
                : new Color(0.55f, 0.58f, 0.66f, 0.75f);  // 회색 — 아직 잠겼다

            if (_possessMeter == null)
            {
                // 원형 게이지로 두면 그림이 없는 지금은 그냥 회색 사각형으로 보인다.
                // 가로 막대는 그림 없이도 게이지로 읽힌다 — 표식 아래에 얇게 깐다.
                var size = ((RectTransform)_possessMark.transform).sizeDelta;
                _possessMeter = GetOrCreate("PossessMeter", new Vector2(size.x * 1.4f, 3f),
                                            new Vector2(0f, -size.y * 0.65f),
                                            _possessMark.transform);
                _possessMeter.type = Image.Type.Filled;
                _possessMeter.fillMethod = Image.FillMethod.Horizontal;
                _possessMeter.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            bool showMeter = state == PossessMark.Progress;
            if (_possessMeter.gameObject.activeSelf != showMeter)
                _possessMeter.gameObject.SetActive(showMeter);
            if (showMeter)
            {
                _possessMeter.fillAmount = Mathf.Clamp01(progress);
                _possessMeter.color = new Color(0.72f, 0.58f, 1f, 0.9f);
            }
        }

        /// <summary>
        /// 사격 중 표시. 궁수의 전설은 "멈춰야 쏜다"가 규칙이라 지금 쏘는 중인지가
        /// 한눈에 보여야 한다. 발밑 링을 켜서 알린다.
        /// </summary>
        public void SetFiring(bool on)
        {
            if (_fireRing == null)
            {
                if (!on) return;
                _fireRing = GetOrCreate("FireRing", new Vector2(_rect.sizeDelta.x * 0.9f, 14f),
                                        new Vector2(0f, -_rect.sizeDelta.y * 0.45f));
                _fireRing.color = new Color(1f, 0.72f, 0.24f, 0.55f);
                _fireRing.transform.SetAsFirstSibling();
            }
            if (_fireRing.gameObject.activeSelf != on) _fireRing.gameObject.SetActive(on);
        }

        public void SetSprite(Sprite s)
        {
            if (_body != null) _body.sprite = s;
            _baseSprite = s;
        }

        // ── 8방향 바라보기 ───────────────────────────────────────
        // 그리는 것은 다섯 방향(↓ ↘ → ↗ ↑)뿐이고, 왼쪽 절반은 좌우 반전으로 만든다.
        // 캐릭터가 좌우 대칭이라 반전이 자연스럽고, 제작량이 96장 → 60장으로 준다.
        //
        // 방향 스프라이트가 없으면 원래 그림을 그대로 쓴다. 12종을 한 번에 만들지
        // 않고 한 종씩 넣어 볼 수 있어야 해서, 없는 쪽이 깨지면 안 된다.

        /// <summary>바라보기 스프라이트 5장. 순서는 s / se / e / ne / n.</summary>
        public static readonly string[] FacingSuffix = { "s", "se", "e", "ne", "n" };

        /// <summary>프레임 종류. 값이 곧 `_frames` 배열 인덱스다.</summary>
        public const int FrameIdle = 0;
        public const int FrameAtk1 = 1;
        public const int FrameAtk2 = 2;
        public const int FrameHit = 3;
        public const int FrameWalk1 = 4;
        public const int FrameWalk2 = 5;
        public const int FrameDie1 = 6;
        public const int FrameDie2 = 7;

        /// <summary>파일명 접미. idle 은 접미가 없어 null 이다.</summary>
        public static readonly string[] FrameSuffix =
            { null, "atk1", "atk2", "hit", "walk1", "walk2", "die1", "die2" };

        // 연출 길이. 합(0.17초)이 어떤 호스트의 공격 간격보다도 짧아야 한다 —
        // 길면 다음 발사가 이전 동작을 자르고 들어와 반동이 안 보인다.
        private const float Atk1Seconds = 0.07f;
        private const float Atk2Seconds = 0.10f;
        private const float HitSeconds = 0.12f;   // 붉은 점멸과 같은 길이. 색과 자세가 따로 놀면 어색하다

        // 걸음 한 짝의 길이. 두 장이 번갈아 도므로 한 걸음 주기는 이 값의 2배다.
        private const float WalkFrameSeconds = 0.14f;

        // 사망. die2 는 페이드가 끝날 때까지 머무르므로 따로 길이를 두지 않는다.
        private const float Die1Seconds = 0.16f;
        private const float DeathFadeSeconds = 0.50f;

        private readonly Sprite[][] _frames = new Sprite[FrameSuffix.Length][];
        private Sprite _baseSprite;
        private int _facingIndex = -1;
        private bool _facingFlip;

        private int _frame = FrameIdle;
        private float _frameTimer;
        private int _shownFrame = -1, _shownIndex = -1;
        private bool _shownFlip;

        private bool _moving;        // 이번 프레임에 움직였나 — 걷기 재생 조건
        private float _walkPhase;
        private bool _dying;
        private float _deathTimer;

        /// <summary>사망 연출이 도는 중. 이 동안에는 표적·충돌에서 빠져 있어야 한다.</summary>
        public bool IsDying => _dying;

        public bool HasFacing => _frames[FrameIdle] != null;

        /// <summary>
        /// 방향별 총구 위치. 몸 중심 기준이고, 캔버스 크기로 나눠 둬서 표시 크기가
        /// 달라도 따라간다. 순서는 `FacingSuffix` 와 같다.
        ///
        /// 조준 방향으로 일정 거리 미는 방식은 안 된다 — 방향마다 총구가 다른 데 있다.
        /// `s` 는 총이 화면 앞쪽으로 단축돼 몸 한가운데에 가깝고, `n` 은 총열이 등 뒤로
        /// 가려져 오른쪽 어깨 옆에서 나온다. 람보 `atk1` 의 화염 중심을 실측한 값이다.
        /// </summary>
        private static readonly Vector2[] MuzzleOffset =
        {
            new(0.00f,  0.03f),   // s  ↓ 몸 중앙
            new(0.47f, -0.11f),   // se ↘
            new(0.43f, -0.02f),   // e  →
            new(0.46f,  0.26f),   // ne ↗ 총을 들어 올려 높다
            new(0.18f,  0.20f),   // n  ↑ 오른쪽 어깨 옆
        };

        /// <summary>
        /// 탄이 나가는 지점. 몸 한가운데에서 나오면 총을 들고 있는 의미가 없다.
        /// 방향 스프라이트가 없는 캐릭터는 몸 중심을 그대로 쓴다 — 어느 손에 무기를
        /// 들었는지 알 수 없어서, 어림한 위치로 밀면 오히려 더 어긋난다.
        /// </summary>
        public Vector2 MuzzlePosition
        {
            get
            {
                if (_facingIndex < 0 || _rect == null) return Position;
                var o = MuzzleOffset[_facingIndex];
                var size = _rect.sizeDelta;
                return Position + new Vector2((_facingFlip ? -o.x : o.x) * size.x, o.y * size.y);
            }
        }

        /// <summary>
        /// 방향 스프라이트를 넘겨준다. 한 벌(5장) 중 하나라도 비면 그 벌은 통째로 버린다 —
        /// 섞이면 방향마다 다른 그림이 나와서 더 이상하다.
        /// 공격·피격 벌이 없으면 그 동작에서도 idle 을 쓴다. 캐릭터를 한 종씩
        /// 채워 넣을 수 있어야 해서, 없는 쪽이 깨지면 안 된다.
        /// </summary>
        public void SetFacingSprites(Sprite[][] sets)
        {
            for (int f = 0; f < _frames.Length; f++)
                _frames[f] = sets != null && f < sets.Length ? Validate(sets[f]) : null;
            _shownFrame = -1;   // 다음 Apply 에서 반드시 다시 그리게 한다
        }

        private static Sprite[] Validate(Sprite[] five)
        {
            if (five == null || five.Length != FacingSuffix.Length) return null;
            for (int i = 0; i < five.Length; i++)
                if (five[i] == null) return null;
            return five;
        }

        /// <summary>사격 동작을 시작한다. 피격 중이면 무시한다 — 맞은 게 더 급한 정보다.</summary>
        public void PlayAttack()
        {
            if (_dying) return;
            if (_frame == FrameHit && _frameTimer > 0f) return;
            _frame = FrameAtk1;
            _frameTimer = Atk1Seconds;
            Apply();
        }

        /// <summary>피격 동작을 시작한다. 사격 중이어도 끊고 들어간다.</summary>
        public void PlayHit()
        {
            if (_dying) return;
            _frame = FrameHit;
            _frameTimer = HitSeconds;
            Apply();
        }

        /// <summary>
        /// 이번 프레임에 움직였는지 알려준다. 걷기는 시간이 아니라 **실제 이동**에
        /// 매여야 한다 — 멈춰 서서 다리만 젓는 그림이 나오면 안 된다.
        /// </summary>
        public void SetMoving(bool moving)
        {
            if (!moving) _walkPhase = 0f;   // 멈추면 처음 걸음부터 다시 시작
            _moving = moving;
        }

        /// <summary>
        /// 동작을 진행시킨다.
        /// 공격·피격은 한 번 재생하고 끝나며, 그 뒤에는 이동 중이면 걷기가,
        /// 아니면 idle 이 깔린다.
        /// </summary>
        public void TickAnim(float dt)
        {
            if (_dying) return;   // 사망은 TickDeath 가 따로 돈다

            // 한 번짜리 동작(공격·피격)이 재생 중이면 그게 우선이다.
            if (_frameTimer > 0f)
            {
                _frameTimer -= dt;
                if (_frameTimer > 0f) return;
                if (_frame == FrameAtk1) { _frame = FrameAtk2; _frameTimer = Atk2Seconds; Apply(); return; }
                _frameTimer = 0f;
            }

            if (_moving)
            {
                _walkPhase += dt;
                // 두 장을 번갈아 돌린다. 나머지 연산이라 위상이 커져도 안전하다.
                bool second = (int)(_walkPhase / WalkFrameSeconds) % 2 == 1;
                _frame = second ? FrameWalk2 : FrameWalk1;
            }
            else
            {
                _frame = FrameIdle;
            }
            Apply();
        }

        /// <summary>
        /// 사망 연출을 시작한다. 사망 그림이 없으면 false — 부르는 쪽이
        /// 예전처럼 바로 없애면 된다. 캐릭터를 한 종씩 채워 넣어야 해서
        /// 그림이 없는 종이 깨지면 안 된다.
        /// </summary>
        public bool BeginDeath()
        {
            if (_frames[FrameDie1] == null || _frames[FrameDie2] == null) return false;

            _dying = true;
            _deathTimer = 0f;
            _frame = FrameDie1;
            _frameTimer = 0f;
            _moving = false;

            // 죽은 몸은 더 이상 정보가 아니다. 체력바·빙의 표식·사격 링을 지운다.
            if (_hpBarBg != null) _hpBarBg.gameObject.SetActive(false);
            if (_possessMark != null) _possessMark.gameObject.SetActive(false);
            if (_fireRing != null) _fireRing.gameObject.SetActive(false);

            Apply();
            return true;
        }

        /// <summary>사망 연출을 진행시킨다. 다 끝났으면 true — 그때 없앤다.</summary>
        public bool TickDeath(float dt)
        {
            if (!_dying) return true;
            _deathTimer += dt;

            if (_deathTimer >= Die1Seconds && _frame == FrameDie1)
            {
                _frame = FrameDie2;
                Apply();
            }

            // die2 로 넘어간 뒤부터 서서히 사라진다.
            if (_body != null && _deathTimer > Die1Seconds)
            {
                float t = Mathf.Clamp01((_deathTimer - Die1Seconds) / DeathFadeSeconds);
                var c = _body.color;
                _body.color = new Color(c.r, c.g, c.b, 1f - t);
            }
            return _deathTimer >= Die1Seconds + DeathFadeSeconds;
        }

        /// <summary>현재 (프레임 × 방향) 을 화면에 반영한다. 바뀐 게 없으면 아무것도 하지 않는다.</summary>
        private void Apply()
        {
            if (_body == null || _facingIndex < 0 || _frames[FrameIdle] == null) return;

            // 그 동작의 그림이 없으면 idle 로 대신한다 — 없는 채로 두면 빈 칸이 된다.
            int f = _frames[_frame] != null ? _frame : FrameIdle;
            if (f == _shownFrame && _facingIndex == _shownIndex && _facingFlip == _shownFlip) return;

            _shownFrame = f;
            _shownIndex = _facingIndex;
            _shownFlip = _facingFlip;
            _body.sprite = _frames[f][_facingIndex];
            // 좌우 반전은 스케일로 준다. 부호만 바꾸므로 픽셀 정렬이 깨지지 않는다.
            var s = _body.transform.localScale;
            _body.transform.localScale =
                new Vector3(_facingFlip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, s.z);
        }

        /// <summary>
        /// 바라보는 방향을 정한다. 8방향으로 반올림해 다섯 장 + 반전으로 표현한다.
        /// 방향이 안 바뀌면 아무것도 하지 않는다 — 매 프레임 스프라이트를 갈면 낭비다.
        /// </summary>
        /// <summary>
        /// 지금 바라보는 방향(단위 벡터). 얼티밋처럼 "앞쪽" 을 써야 하는 연출이 쓴다.
        /// 방향 스프라이트가 8칸이라 정확한 각도가 아니라 **보이는 대로의 방향**이다 —
        /// 그림과 어긋나면 등 뒤로 불을 뿜는 그림이 된다.
        /// </summary>
        public Vector2 Facing { get; private set; } = Vector2.down;

        public void SetFacing(Vector2 dir)
        {
            if (_frames[FrameIdle] == null || dir.sqrMagnitude < 0.0001f) return;
            Facing = dir.normalized;

            // 화면 좌표계라 위쪽이 +y 다. 오른쪽(→)을 0 도로 두고 8칸으로 나눈다.
            float deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            int oct = Mathf.RoundToInt(deg / 45f);
            if (oct < 0) oct += 8;              // 0=→ 1=↗ 2=↑ 3=↖ 4=← 5=↙ 6=↓ 7=↘

            int index;
            bool flip;
            switch (oct)
            {
                case 0: index = 2; flip = false; break;   // →  e
                case 1: index = 3; flip = false; break;   // ↗  ne
                case 2: index = 4; flip = false; break;   // ↑  n
                case 3: index = 3; flip = true;  break;   // ↖  ne 반전
                case 4: index = 2; flip = true;  break;   // ←  e  반전
                case 5: index = 1; flip = true;  break;   // ↙  se 반전
                case 6: index = 0; flip = false; break;   // ↓  s
                default: index = 1; flip = false; break;  // ↘  se
            }
            _facingIndex = index;
            _facingFlip = flip;
            Apply();
        }

        /// <summary>피해를 적용한다. 사망했으면 true.</summary>
        public bool TakeDamage(int amount)
        {
            if (!IsAlive) return false;
            Hp = Mathf.Max(0, Hp - Mathf.Max(1, amount));
            RefreshHpBar();
            _flashTimer = HitSeconds;
            PlayHit();          // 틴트와 자세를 같은 자리에서 시작해야 따로 놀지 않는다
            return Hp == 0;
        }

        public void Heal(int amount) { Hp = Mathf.Min(HpMax, Hp + amount); RefreshHpBar(); }

        private void RefreshHpBar()
        {
            if (_hpBarFill == null) return;
            var rt = (RectTransform)_hpBarFill.transform;
            float full = _hpBarBg.rectTransform.sizeDelta.x;
            rt.sizeDelta = new Vector2(full * ((float)Hp / HpMax), rt.sizeDelta.y);
        }

        /// <summary>
        /// 공격 쿨다운을 진행시키고, 이번 프레임에 때릴 수 있으면 true.
        /// `intervalMul` 은 런 버프(연사 강화) 배율이다 — 유닛 스탯은 건드리지 않는다.
        /// </summary>
        public bool TickAttack(float dt, float intervalMul = 1f)
        {
            _attackTimer -= dt;
            if (_attackTimer > 0f) return false;
            _attackTimer = AttackInterval * Mathf.Max(0.05f, intervalMul);
            return true;
        }

        // ── 공격 예고 ────────────────────────────────────────────
        //
        // 정본은 적마다 예고 시간을 준다(0.3~0.9초). 이게 없으면 사거리에 들어선
        // 순간 맞아서 **피할 방법이 없다.** 근접으로 파고들 틈이 생기지 않는다.

        private float _windupTimer;

        /// <summary>지금 자세를 잡는 중인가. 이 동안은 안 쏘고, 노란 틴트로 보인다.</summary>
        public bool IsWindingUp => _windupTimer > 0f;

        public void BeginWindup(float seconds)
        {
            _windupTimer = Mathf.Max(0.01f, seconds);
            SetTelegraph(true);
        }

        /// <summary>예고를 진행시킨다. 이번 프레임에 끝났으면 true — 그때 때린다.</summary>
        public bool TickWindup(float dt)
        {
            if (_windupTimer <= 0f) return false;
            _windupTimer -= dt;
            if (_windupTimer > 0f) return false;
            _windupTimer = 0f;
            SetTelegraph(false);
            return true;
        }

        /// <summary>예고를 중간에 접는다 — 대상이 사라지거나 사거리 밖으로 나갔을 때.</summary>
        public void CancelWindup()
        {
            if (_windupTimer <= 0f) return;
            _windupTimer = 0f;
            SetTelegraph(false);
        }

        /// <summary>피격 점멸. 스프라이트를 건드리지 않고 틴트만 흔든다.</summary>
        public void TickFlash(float dt)
        {
            // 사망 중에는 페이드가 색을 쥐고 있다. 여기서 흰색으로 되돌리면 페이드가 풀린다.
            if (_body == null || _dying) return;
            if (_flashTimer <= 0f)
            {
                // 예고 중에는 예고색이 이긴다. 그 다음이 상태이상, 없으면 흰색.
                if (_telegraph) return;
                _body.color = TryStatusTint(out var tint) ? tint : Color.white;
                return;
            }
            _flashTimer -= dt;
            if (_flashTimer > 0f) { _body.color = new Color(1f, 0.45f, 0.45f, 1f); return; }
            _body.color = TryStatusTint(out var back) ? back : Color.white;
        }

        /// <summary>
        /// 보스 패턴 예고. 피할 시간을 주지 않으면 패턴이 아니라 사고가 된다.
        /// 피격 점멸과 같은 틴트를 쓰므로 켜져 있는 동안은 점멸이 덮어쓰지 않는다.
        /// </summary>
        public void SetTelegraph(bool on)
        {
            _telegraph = on;
            if (_body == null) return;
            if (on) _body.color = new Color(1f, 0.86f, 0.35f, 1f);
            else if (_flashTimer <= 0f) _body.color = Color.white;
        }

        // ── 상태이상 ──────────────────────────────────────────────
        //
        // 정본 버프 24종 중 17종이 이것이 없어서 꺼져 있었다. 세 가지만 있으면
        // 그중 상당수가 살아난다 — 화상(지속 피해) · 빙결(둔화·정지) · 저주(받는 피해 증가).
        //
        // 지속 피해를 유닛이 스스로 깎지 않는다. 죽음 처리·보상·피해 숫자가 전부
        // BattleDirector 에 있어서, 여기서 깎으면 죽어도 아무 일도 안 일어난다.
        // 이 클래스는 **얼마를 깎아야 하는지만 알려주고** 실제 피해는 부르는 쪽이 준다.

        public const int StatusMaxStack = 3;

        private float _burnTimer, _burnAccum;
        private int _burnStack;
        private float _curseTimer;
        private int _curseStack;
        private float _freezeTimer;

        public int BurnStack => _burnStack;
        public int CurseStack => _curseStack;
        public bool IsFrozen => _freezeTimer > 0f;

        /// <summary>저주 배수 — 받는 피해가 단계마다 늘어난다.</summary>
        public float CurseDamageMul => 1f + _curseStack * CursePerStack;

        private const float CursePerStack = 0.15f;
        private const float BurnDamagePerStackPerSecond = 6f;
        private const int FreezeSlowPercent = 55;

        public void ApplyBurn(float seconds)
        {
            _burnStack = Mathf.Min(StatusMaxStack, _burnStack + 1);
            _burnTimer = Mathf.Max(_burnTimer, seconds);
        }

        public void ApplyCurse(float seconds)
        {
            _curseStack = Mathf.Min(StatusMaxStack, _curseStack + 1);
            _curseTimer = Mathf.Max(_curseTimer, seconds);
        }

        /// <summary>빙결. 둔화를 겸하므로 기존 둔화 경로를 함께 쓴다.</summary>
        public void ApplyFreeze(float seconds)
        {
            _freezeTimer = Mathf.Max(_freezeTimer, seconds);
            ApplySlow(FreezeSlowPercent, seconds);
        }

        /// <summary>
        /// 상태이상 시간을 흘린다. 이번 프레임에 줘야 할 화상 피해를 돌려준다(없으면 0).
        /// 소수 피해가 사라지지 않도록 누적해 두었다가 1 이상이 될 때만 떨어뜨린다 —
        /// 매 프레임 1씩 주면 화상이 초당 60 이 된다.
        /// </summary>
        public int TickStatus(float dt)
        {
            if (_curseTimer > 0f)
            {
                _curseTimer -= dt;
                if (_curseTimer <= 0f) _curseStack = 0;
            }
            if (_freezeTimer > 0f) _freezeTimer -= dt;

            if (_burnTimer <= 0f) return 0;
            _burnTimer -= dt;
            if (_burnTimer <= 0f) { _burnStack = 0; _burnAccum = 0f; return 0; }

            _burnAccum += BurnDamagePerStackPerSecond * _burnStack * dt;
            if (_burnAccum < 1f) return 0;
            int give = Mathf.FloorToInt(_burnAccum);
            _burnAccum -= give;
            return give;
        }

        public void ClearStatus()
        {
            _burnTimer = _curseTimer = _freezeTimer = _burnAccum = 0f;
            _burnStack = _curseStack = 0;
        }

        /// <summary>
        /// 상태이상 색. 피격 점멸과 빙의 표시가 이미 몸 색을 쓰므로 그 둘이 없을 때만 칠한다.
        /// 색이 겹치면 무엇이 걸렸는지도, 맞았는지도 안 보인다.
        /// </summary>
        public bool TryStatusTint(out Color color)
        {
            if (_burnStack > 0) { color = new Color(1f, 0.55f, 0.25f, 1f); return true; }
            if (_freezeTimer > 0f) { color = new Color(0.55f, 0.85f, 1f, 1f); return true; }
            if (_curseStack > 0) { color = new Color(0.72f, 0.45f, 1f, 1f); return true; }
            color = Color.white;
            return false;
        }

        // ── 원거리 재배치 ─────────────────────────────────────────
        //
        // 두 번 쏘고 한 번 옮긴다. 가만히 서서 계속 쏘면 붙박인 과녁이 되고,
        // 계속 쫓아오면 붙어 버려 사거리의 의미가 없다.

        private int _shotsSinceMove;

        public bool IsRepositioning { get; private set; }
        public Vector2 RepositionTarget { get; private set; }

        /// <summary>한 발 쐈다고 세고, 옮길 차례면 true.</summary>
        public bool CountShotAndNeedsMove(int shotsPerMove)
        {
            _shotsSinceMove++;
            if (_shotsSinceMove < shotsPerMove) return false;
            _shotsSinceMove = 0;
            return true;
        }

        public void BeginReposition(Vector2 target)
        {
            IsRepositioning = true;
            RepositionTarget = target;
        }

        public void EndReposition() => IsRepositioning = false;

        /// <summary>둔화 부여(설녀). 더 강한 둔화가 걸려 있으면 유지한다.</summary>
        public void ApplySlow(int percent, float seconds)
        {
            if (percent <= 0) return;
            if (percent >= _slowPercent) _slowPercent = Mathf.Clamp(percent, 0, 90);
            _slowTimer = Mathf.Max(_slowTimer, seconds);
        }

        public void TickSlow(float dt)
        {
            if (_slowTimer <= 0f) return;
            _slowTimer -= dt;
            if (_slowTimer <= 0f) _slowPercent = 0;
        }

        private float CurrentSpeed => MoveSpeed * (1f - _slowPercent / 100f);

        /// <summary>이번 프레임에 움직일 거리. 지형지물을 타고 미끄러지려면 부르는 쪽이 필요하다.</summary>
        public Vector2 StepToward(Vector2 target, float dt)
        {
            var d = target - Position;
            float len = d.magnitude;
            if (len < 0.001f) return Vector2.zero;
            return d / len * CurrentSpeed * dt;
        }

        public void MoveToward(Vector2 target, float dt) => Position += StepToward(target, dt);
    }
}
