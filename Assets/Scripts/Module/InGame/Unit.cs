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

        /// <summary>호스트가 될 수 있는 적인가. 보스는 빙의 대상이 아니다.</summary>
        public bool IsPossessable => Side == UnitSide.Enemy && !IsBoss && IsAlive;

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
            _body.transform.localScale = Vector3.one;
            _body.preserveAspect = true;
            _body.raycastTarget = false;

            // 적만 머리 위 체력바를 단다. 플레이어 체력은 상단 HUD 가 담당한다.
            if (side == UnitSide.Enemy)
            {
                var barSize = new Vector2(size.x * 0.7f, 5f);
                var barPos = new Vector2(0f, size.y * 0.5f + 6f);
                _hpBarBg = GetOrCreate("HpBarBg", barSize, barPos);
                _hpBarBg.color = new Color(0.06f, 0.07f, 0.10f, 0.9f);
                _hpBarFill = GetOrCreate("HpBarFill", barSize, barPos, _hpBarBg.transform);
                _hpBarFill.color = new Color(0.85f, 0.20f, 0.16f, 1f);
                ((RectTransform)_hpBarFill.transform).pivot = new Vector2(0f, 0.5f);
                ((RectTransform)_hpBarFill.transform).anchoredPosition = new Vector2(-barSize.x * 0.5f, 0f);

                if (!isBoss)
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

        public void SetPossessMark(bool on)
        {
            if (_possessMark != null) _possessMark.gameObject.SetActive(on);
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

        /// <summary>파일명 접미. idle 은 접미가 없어 null 이다.</summary>
        public static readonly string[] FrameSuffix = { null, "atk1", "atk2", "hit" };

        // 연출 길이. 합(0.17초)이 어떤 호스트의 공격 간격보다도 짧아야 한다 —
        // 길면 다음 발사가 이전 동작을 자르고 들어와 반동이 안 보인다.
        private const float Atk1Seconds = 0.07f;
        private const float Atk2Seconds = 0.10f;
        private const float HitSeconds = 0.12f;   // 붉은 점멸과 같은 길이. 색과 자세가 따로 놀면 어색하다

        private readonly Sprite[][] _frames = new Sprite[FrameSuffix.Length][];
        private Sprite _baseSprite;
        private int _facingIndex = -1;
        private bool _facingFlip;

        private int _frame = FrameIdle;
        private float _frameTimer;
        private int _shownFrame = -1, _shownIndex = -1;
        private bool _shownFlip;

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
        public void SetFacingSprites(Sprite[] idle, Sprite[] atk1 = null,
                                     Sprite[] atk2 = null, Sprite[] hit = null)
        {
            _frames[FrameIdle] = Validate(idle);
            _frames[FrameAtk1] = Validate(atk1);
            _frames[FrameAtk2] = Validate(atk2);
            _frames[FrameHit] = Validate(hit);
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
            if (_frame == FrameHit && _frameTimer > 0f) return;
            _frame = FrameAtk1;
            _frameTimer = Atk1Seconds;
            Apply();
        }

        /// <summary>피격 동작을 시작한다. 사격 중이어도 끊고 들어간다.</summary>
        public void PlayHit()
        {
            _frame = FrameHit;
            _frameTimer = HitSeconds;
            Apply();
        }

        /// <summary>동작을 진행시킨다. atk1 → atk2 → idle 순으로 되돌아간다.</summary>
        public void TickAnim(float dt)
        {
            if (_frame == FrameIdle) return;
            _frameTimer -= dt;
            if (_frameTimer > 0f) return;

            if (_frame == FrameAtk1) { _frame = FrameAtk2; _frameTimer = Atk2Seconds; }
            else { _frame = FrameIdle; _frameTimer = 0f; }
            Apply();
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
        public void SetFacing(Vector2 dir)
        {
            if (_frames[FrameIdle] == null || dir.sqrMagnitude < 0.0001f) return;

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

        /// <summary>피격 점멸. 스프라이트를 건드리지 않고 틴트만 흔든다.</summary>
        public void TickFlash(float dt)
        {
            if (_body == null) return;
            if (_flashTimer <= 0f)
            {
                if (!_telegraph) _body.color = Color.white;
                return;
            }
            _flashTimer -= dt;
            _body.color = _flashTimer > 0f ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
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

        public void MoveToward(Vector2 target, float dt)
        {
            var d = target - Position;
            float len = d.magnitude;
            if (len < 0.001f) return;
            Position += d / len * CurrentSpeed * dt;
        }
    }
}
