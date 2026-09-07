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

        /// <summary>체력 바 위에 겹쳐 그리는 쉴드. 체력보다 먼저 깎이므로 위에 얹는다.</summary>
        private Image _shieldBarFill;

        /// <summary>쉴드 채움 그림(`hostshieldfill`). 배틀이 한 번 넣어 준다.</summary>
        private static Sprite s_shieldFillSprite;

        public static void SetShieldFillSprite(Sprite s) => s_shieldFillSprite = s;
        private Image _possessMark;

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

        /// <summary>
        /// 탄이 닿았다고 볼 몸통 반경. 몸이 1.5배로 커졌는데 명중 판정은
        /// 중심에서 34픽셀로 고정이라, 어깨를 지나가는 탄이 그냥 통과했다.
        /// 관통탄이 앞사람만 맞히던 것도 같은 이유다 — 뒷사람이 조금만 비껴 서면
        /// 중심에서 34픽셀 안에 들어오지 않는다.
        /// 그림에는 여백이 있으므로 폭의 절반을 그대로 쓰지 않고 조금 좁게 잡는다.
        /// </summary>
        public float BodyRadius => _rect.sizeDelta.x * 0.42f;

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
        /// <summary>
        /// 이 방에서는 다시 빙의할 수 없는 몸 (기획서 1-4 · 1-7).
        /// 스스로 빠져나온 몸이 여기 해당한다 — 놓아준 몸을 곧바로 다시 타면
        /// 탈출 비용(-15%)이 무의미해지고 한 몸을 무한히 재활용하게 된다.
        /// 방이 끝나면 유닛이 통째로 사라지므로 따로 초기화할 것이 없다.
        /// </summary>
        public bool RepossessBanned { get; private set; }

        /// <summary>정본이 "다음 몸" 으로 찍어 둔 적인가. 스폰할 때 정해진다.</summary>
        public bool IsNextBody { get; private set; }
        public void MarkAsNextBody() => IsNextBody = true;

        // ─────────────────────────────────────────────────────────
        // 숙주와 잡몹
        //
        // 정본 ROOM_SPAWN 은 345 자리 중 46 자리(13%)에만 `POSSESSION_TARGET` 을
        // 찍어 두었다. 나머지는 빼앗을 수 없는 적이다.
        // 예전에는 이 표시를 **머리 위 표식에만** 쓰고 실제 판정은 하지 않아서
        // 방에 있는 적을 아무나 다 뺏을 수 있었다. 몸이 널려 있으니 죽지 않고,
        // 죽지 않으니 빙의가 판단이 아니라 습관이 됐다.
        // ─────────────────────────────────────────────────────────

        /// <summary>빼앗을 수 있는 몸인가. 스폰할 때 정해지고 이후 바뀌지 않는다.</summary>
        public bool IsHostBody { get; private set; }
        public void MarkAsHostBody() => IsHostBody = true;

        /// <summary>엘리트인가. 수가 적은 대신 하나하나가 세고, 떨구는 것도 많다.</summary>
        public bool IsElite { get; private set; }
        public void MarkAsElite() => IsElite = true;

        // ─────────────────────────────────────────────────────────
        // 경직 — 빙의의 조건
        //
        // 예전 조건은 "체력을 25~50% 아래로 깎기" 였다. 그러면 **빼앗기와 죽이기가
        // 같은 행동**이 되고, 거기까지 깎았으면 한 대 더 때려 죽이는 쪽이 이득이라
        // 빼앗을 이유가 사라진다.
        //
        // 경직은 체력과 무관하다. 짧은 시간에 몰아쳐야 차고, 손을 놓으면 식는다.
        // 체력 100% 인 몸도 잘 두들기면 빼앗을 수 있고, 체력 10% 인 몸도 게이지를
        // 못 채우면 못 빼앗는다. 이래야 "잡을까 뺏을까" 가 매번 판단이 된다.
        // ─────────────────────────────────────────────────────────

        /// <summary>경직에 필요한 누적 피해 = 최대 체력의 이 비율.</summary>
        private const float StaggerNeedRatio = 0.35f;

        /// <summary>게이지가 가득 찬 뒤 빙의를 받아 주는 시간(초).</summary>
        private const float StaggerWindowSeconds = 1.5f;

        /// <summary>때리기를 멈추면 초당 이만큼(최대 체력 대비 비율) 식는다.</summary>
        private const float StaggerDecayRatio = 0.12f;

        /// <summary>경직이 풀린 뒤 다시 채울 수 없는 시간(초). 무한 경직을 막는다.</summary>
        private const float StaggerCooldownSeconds = 2.0f;

        private float _stagger;         // 누적 피해(체력 단위)
        private float _staggerWindow;   // 남은 창 시간
        private float _staggerCool;     // 남은 재충전 금지 시간

        /// <summary>지금 경직 중인가 — 이 동안에만 빙의가 들어간다.</summary>
        public bool IsStaggered => _staggerWindow > 0f;

        /// <summary>
        /// 게이지 0~1. 머리 위 표식이 이 값을 그린다.
        /// 몸이 없을 때는 경직이 조건이 아니므로 항상 가득 찬 것으로 보인다 —
        /// 채울 수 없는 게이지를 보여 주면 "왜 안 잡히지" 가 된다.
        /// </summary>
        public float StaggerProgress
            => IsStaggered || !PlayerHasHost
             ? 1f : Mathf.Clamp01(_stagger / Mathf.Max(1f, HpMax * StaggerNeedRatio));

        /// <summary>
        /// 게이지를 식히고 창을 닫는다. 매 프레임 부른다.
        /// </summary>
        public void TickStagger(float dt)
        {
            if (_staggerWindow > 0f)
            {
                _staggerWindow -= dt;
                if (_staggerWindow <= 0f)
                {
                    _staggerWindow = 0f;
                    _stagger = 0f;
                    _staggerCool = StaggerCooldownSeconds;
                }
                return;
            }

            if (_staggerCool > 0f) { _staggerCool = Mathf.Max(0f, _staggerCool - dt); return; }
            if (_stagger > 0f) _stagger = Mathf.Max(0f, _stagger - HpMax * StaggerDecayRatio * dt);
        }

        /// <summary>경직을 강제로 푼다. 몸을 빼앗은 직후에 부른다.</summary>
        public void ClearStagger()
        {
            _stagger = 0f;
            _staggerWindow = 0f;
            _staggerCool = 0f;
        }

        public void BanRepossess() => RepossessBanned = true;

        /// <summary>
        /// 내가 놓아준 몸이 적으로 돌아간다 (기획서 1-4 A).
        /// 체력·상태는 그대로 두고 편만 바꾼다 — 놓아준 순간의 몸 그대로여야
        /// "내가 쓰던 몸이 나를 쫓아온다" 가 성립한다.
        /// </summary>
        public void BecomeEnemy()
        {
            Side = UnitSide.Enemy;
            IsAggro = true;          // 놓아주자마자 나를 공격한다
            HoldPossessed(false);
            SetState(EnemyState.Idle);
            RefreshHpBar();
        }

        /// <summary>
        /// 지금 몸을 입고 있는가. `BattleDirector` 가 매 프레임 채워 넣는다.
        ///
        /// ⚠ 경직을 **몸이 없을 때까지** 요구하면 게임이 잠긴다.
        ///   유령은 공격할 수 없으므로(`BattleDirector` 의 사격 경로가 `_host == null` 에서
        ///   곧바로 돌아온다) 경직을 만들 수단 자체가 없다. 몸을 잃는 순간
        ///   **영원히 아무것도 탈 수 없는 상태**가 된다.
        ///
        ///   그래서 경직은 **갈아타기의 조건**이지 빙의 전체의 조건이 아니다.
        ///     몸이 없다 → 숙주면 즉시 탄다. 15 초 시계 앞에서 망설일 여유가 없다
        ///     몸이 있다 → 경직시켜야 갈아탄다. 이쪽은 구제가 아니라 선택이다
        /// </summary>
        public static bool PlayerHasHost;

        /// <summary>
        /// 지금 이 순간 빼앗을 수 있는가.
        /// **숙주로 찍힌 몸**이어야 하고, 몸을 입고 있다면 **경직 중**이기까지 해야 한다.
        /// </summary>
        public bool IsPossessable
        {
            get
            {
                if (RepossessBanned) return false;
                if (Side != UnitSide.Enemy || IsBoss || !IsAlive || _dying) return false;
                if (!IsHostBody) return false;                       // 잡몹은 못 뺏는다
                if (Profile != null &&
                    Profile.PossessKind == Game.Character.PossessKind.NotPossessable) return false;
                return !PlayerHasHost || IsStaggered;
            }
        }

        /// <summary>
        /// 빼앗을 여지가 있는 몸인가 — 지금은 못 타더라도 두들기면 열린다.
        /// 표식을 띄울지 말지가 이 값으로 갈린다.
        /// </summary>
        public bool HasPossessCondition
            => IsHostBody && Side == UnitSide.Enemy && !IsBoss && IsAlive && !_dying
               && !RepossessBanned
               && (Profile == null ||
                   Profile.PossessKind != Game.Character.PossessKind.NotPossessable);

        /// <summary>
        /// 조건 진행도 0~1. 이제 체력이 아니라 **경직 게이지**다.
        /// 표식의 게이지가 이 값을 그린다 — 얼마나 더 때려야 열리는지가 보여야
        /// "왜 안 잡히지"가 "조금만 더"가 된다.
        /// </summary>
        public float PossessProgress => HasPossessCondition ? StaggerProgress : 0f;

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
            _breathePhase = 0f;
            _breatheX = 1f;
            _breatheY = 1f;
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

                // ⚠ 쉴드 바는 체력 바 **위에 따로** 띄운다. 예전에는 같은 자리에
                //   같은 크기로 덮여 있어서 "쉴드가 생겼다" 가 아니라
                //   "체력 바 색이 변했다" 로 읽혔다.
                //   같은 굵기면 체력 바가 둘로 보이므로 60% 로 얇게 한다 —
                //   얇아야 "체력에 덧붙은 것" 으로 읽힌다.
                var shieldSize = new Vector2(barSize.x, barSize.y * ShieldBarHeightRatio);
                var shieldPos = new Vector2(barPos.x,
                                            barPos.y + barSize.y * 0.5f + shieldSize.y * 0.5f + ShieldBarGap);
                _shieldBarFill = GetOrCreate("ShieldBarFill", shieldSize, shieldPos);
                if (s_shieldFillSprite != null) _shieldBarFill.sprite = s_shieldFillSprite;
                else _shieldBarFill.color = new Color(0.125f, 0.878f, 0.910f, 1f);
                ((RectTransform)_shieldBarFill.transform).pivot = new Vector2(0f, 0.5f);
                ((RectTransform)_shieldBarFill.transform).anchoredPosition =
                    new Vector2(shieldPos.x - shieldSize.x * 0.5f, shieldPos.y);
                _shieldBarFill.gameObject.SetActive(false);

                // 빙의 표식은 **적에게만** 단다. 내 몸에 "뺏을 수 있다" 표시가 뜨면 거짓말이다.
                if (!isBoss && side == UnitSide.Enemy)
                {
                    // 그림이 32px 로 그려져 있다. 그 크기 그대로 써야 링이 흐려지지 않는다.
                    _possessMark = GetOrCreate("PossessMark", new Vector2(MarkSize, MarkSize),
                                               new Vector2(0f, size.y * 0.5f + MarkSize * 0.9f));
                    _possessMark.preserveAspect = true;
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

        /// <summary>
        /// 빙의 표식 상태 (기획서 1-5 A · 아이콘 4종).
        /// 색만으로 갈라 두면 교전 중에 못 읽는다 — 그림도 상태마다 다르다.
        /// </summary>
        public enum PossessMark
        {
            /// <summary>표식 없음</summary>
            None,
            /// <summary>빙의 가능 — 파란 조준 링</summary>
            Ready,
            /// <summary>선택 후보 — 지금 버튼이 노리는 몸. 금색</summary>
            Target,
            /// <summary>재빙의 불가 — 이 방에서 내가 버린 몸. 붉은 X</summary>
            Banned,
            /// <summary>빙의 불가 — 조건이 아직 안 찼다. 회색 자물쇠 + 게이지</summary>
            Locked,
        }

        /// <summary>표식 그림의 원본 크기. 이 값으로 띄워야 링이 또렷하다.</summary>
        private const float MarkSize = 32f;

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

        /// <param name="icon">상태에 맞는 그림. null 이면 색만으로 버틴다(아틀라스 로드 전).</param>
        /// <param name="scale">표식 배율. 고스트일 때 120% 로 키운다 (기획서 1-5 B).</param>
        public void SetPossessMark(PossessMark state, Sprite icon = null,
                                   float progress = 1f, float scale = 1f)
        {
            if (_possessMark == null) return;
            bool on = state != PossessMark.None;
            if (_possessMark.gameObject.activeSelf != on) _possessMark.gameObject.SetActive(on);
            if (!on) return;

            _possessMark.sprite = icon;
            // 그림이 있으면 색을 입히지 않는다 — 아이콘이 이미 상태색을 갖고 있다.
            _possessMark.color = icon != null ? Color.white : FallbackColor(state);
            _possessMark.transform.localScale = Vector3.one * scale;

            if (_possessMeter == null)
            {
                // 원형 게이지로 두면 그림이 없는 지금은 그냥 회색 사각형으로 보인다.
                // 가로 막대는 그림 없이도 게이지로 읽힌다 — 표식 아래에 얇게 깐다.
                var size = ((RectTransform)_possessMark.transform).sizeDelta;
                _possessMeter = GetOrCreate("PossessMeter", new Vector2(size.x * 0.8f, 3f),
                                            new Vector2(0f, -size.y * 0.55f),
                                            _possessMark.transform);
                _possessMeter.type = Image.Type.Filled;
                _possessMeter.fillMethod = Image.FillMethod.Horizontal;
                _possessMeter.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            // 게이지는 잠긴 몸에만 붙는다. "얼마나 더 때려야 열리는가"가 그 표식의 전부다.
            bool showMeter = state == PossessMark.Locked;
            if (_possessMeter.gameObject.activeSelf != showMeter)
                _possessMeter.gameObject.SetActive(showMeter);
            if (showMeter)
            {
                _possessMeter.fillAmount = Mathf.Clamp01(progress);
                _possessMeter.color = new Color(0.72f, 0.58f, 1f, 0.9f);
            }
        }

        /// <summary>그림이 아직 없을 때 쓰는 색. 상태를 못 읽는 것보다는 낫다.</summary>
        private static Color FallbackColor(PossessMark state) => state switch
        {
            PossessMark.Target => new Color(1f, 0.78f, 0.23f, 0.95f),
            PossessMark.Banned => new Color(0.91f, 0.28f, 0.24f, 0.95f),
            PossessMark.Locked => new Color(0.55f, 0.58f, 0.66f, 0.75f),
            _ => new Color(0.37f, 0.78f, 1f, 0.95f),
        };

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
        /// 탄이 나가는 지점. 몸 한가운데에서 나오면 총을 들고 있는 의미가 없다.
        ///
        /// ⚠ 자리는 <see cref="MuzzleTable"/> 이 갖는다 — **캐릭터마다 다르다.**
        ///   예전에는 람보 하나를 재서 23종에 같은 값을 썼는데, 코만도는 총구가
        ///   그 절반 거리에 있어 탄이 몸 옆 허공에서 튀어나왔다.
        ///
        /// 방향 스프라이트가 없는 캐릭터는 몸 중심을 그대로 쓴다 — 어느 손에 무기를
        /// 들었는지 알 수 없어서, 어림한 위치로 밀면 오히려 더 어긋난다.
        /// </summary>
        public Vector2 MuzzlePosition
        {
            get
            {
                if (_facingIndex < 0 || _rect == null) return Position;
                var o = MuzzleTable.Get(Key, _facingIndex);
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

        /// <summary>
        /// 방향 한 벌을 다듬는다. **하나라도 비면 그 벌은 통째로 버린다.**
        ///
        /// ⚠ 이웃 방향으로 메우고 싶어지는 자리다. 한 번 그렇게 해 봤고 되돌렸다 —
        ///   위를 보는 프레임에 옆을 보는 그림이 들어가면 **화면에서 바로 보인다.**
        ///   모자란 것은 코드가 아니라 그림으로 채운다.
        ///
        /// 지금 보스 여섯이 `ne`·`n` 이 없어 이 벌들이 다 버려지고 있다.
        /// 그래서 보스가 정지 그림으로 서 있는데, **그것이 지금 상태의 정직한 모습**이다.
        /// 120장 발주가 나가 있다.
        /// </summary>
        private static Sprite[] Validate(Sprite[] five)
        {
            if (five == null || five.Length != FacingSuffix.Length) return null;
            for (int i = 0; i < five.Length; i++)
                if (five[i] == null) return null;
            return five;
        }

        /// <summary>
        /// 사격 동작을 시작한다. 피격 중이면 무시한다 — 맞은 게 더 급한 정보다.
        /// </summary>
        /// <param name="holdScale">
        /// 두 프레임을 얼마나 길게 끌 것인가. 기본 1 이면 0.07 + 0.10 = 0.17 초다.
        ///
        /// ⚠ 잡몹 기준으로 정한 길이다. **보스한테는 너무 짧다.** 256px 짜리 몸이
        ///   0.17초 만에 지나가면 무엇을 했는지 안 보이고, 바닥에 도형만 뜬 채
        ///   보스는 가만히 서 있는 것처럼 읽힌다 — 실제로 그렇게 보고가 들어왔다.
        /// </param>
        public void PlayAttack(float holdScale = 1f)
        {
            if (_dying) return;
            if (_frame == FrameHit && _frameTimer > 0f) return;
            _frame = FrameAtk1;
            _frameTimer = Atk1Seconds * Mathf.Max(0.1f, holdScale);
            _atkHoldScale = Mathf.Max(0.1f, holdScale);
            Apply();
        }

        /// <summary>지금 재생 중인 공격 동작을 얼마나 끄는가. <see cref="PlayAttack"/> 가 정한다.</summary>
        private float _atkHoldScale = 1f;

        /// <summary>
        /// 피격 동작을 시작한다. 사격 중이어도 끊고 들어간다.
        ///
        /// ⚠⚠ **보스는 피격 자세를 안 잡는다.**
        ///
        ///   이 게임은 오토어택이라 보스는 **초당 여러 번** 맞는다. 한 번 맞을 때마다
        ///   0.12초짜리 피격 자세가 들어오면 보스는 사실상 **내내 피격 자세**다.
        ///   게다가 `PlayAttack` 은 피격 중이면 첫 줄에서 돌아가므로
        ///   (`if (_frame == FrameHit && _frameTimer > 0f) return;`)
        ///   **공격 동작이 아예 시작되지도 않는다.**
        ///
        ///   "보스가 공격 액션을 하나도 안 하고 쳐맞기만 한다" 의 정체가 이것이다.
        ///   맞았다는 것은 붉은 틴트(`_flashTimer`)와 피해 숫자가 이미 말해 준다.
        /// </summary>
        public void PlayHit()
        {
            if (_dying || IsBoss) return;
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
        /// <summary>
        /// 영혼이 들어오는 동안 이 자세로 굳는다. 채널이 끝날 때까지 다른 동작이 덮지 않는다 —
        /// 몸을 빼앗기는 중인데 걷거나 쏘면 무슨 일이 벌어지는지 안 읽힌다.
        ///
        /// 자세 그림은 **방향이 없다.** 원작이 정면 2 장 한 벌로만 그려 두었으므로
        /// 부르는 쪽이 `SetSpriteOverride` 로 그 두 장을 번갈아 넣는다.
        /// </summary>
        public void HoldPossessed(bool on)
        {
            _possessHold = on;
            if (!on) SetSpriteOverride(null);
            _frameTimer = 0f;
        }

        private bool _possessHold;

        public void TickAnim(float dt)
        {
            if (_dying) return;   // 사망은 TickDeath 가 따로 돈다
            if (_possessHold) return;

            // 한 번짜리 동작(공격·피격)이 재생 중이면 그게 우선이다.
            if (_frameTimer > 0f)
            {
                _frameTimer -= dt;
                if (_frameTimer > 0f) return;
                if (_frame == FrameAtk1) { _frame = FrameAtk2; _frameTimer = Atk2Seconds * _atkHoldScale; Apply(); return; }
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
            TickBreathe(dt);
        }

        /// <summary>
        /// 사망 연출을 시작한다. 사망 그림이 없으면 false — 부르는 쪽이
        /// 예전처럼 바로 없애면 된다. 캐릭터를 한 종씩 채워 넣어야 해서
        /// 그림이 없는 종이 깨지면 안 된다.
        /// </summary>
        /// <param name="fadeOnly">
        /// 쓰러지는 그림이 없어도 **사라지는 시간만은 준다.**
        /// 죽는 연출을 밖에서 따로 그리는 경우에 쓴다 — 파이썬은 벽 구멍으로
        /// 미끄러져 들어가는 것이 죽는 연출이라 방향별 die 그림이 아예 필요 없다.
        /// 그림이 없다고 그 자리에서 없애 버리면 그 연출이 한 프레임도 못 돈다.
        /// </param>
        public bool BeginDeath(bool fadeOnly = false)
        {
            if (!fadeOnly && (_frames[FrameDie1] == null || _frames[FrameDie2] == null)) return false;

            _dying = true;
            _deathTimer = 0f;
            _frame = FrameDie1;
            _frameTimer = 0f;
            _moving = false;

            // 죽은 몸은 더 이상 정보가 아니다. 체력바·빙의 표식을 지운다.
            if (_hpBarBg != null) _hpBarBg.gameObject.SetActive(false);
            if (_possessMark != null) _possessMark.gameObject.SetActive(false);

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

        /// <summary>
        /// 머리 위 체력바를 켜고 끈다.
        ///
        /// 벽 보스(파이썬)는 방 꼭대기 벽에 붙어 있어서 이 바가 **평소엔 방 밖으로
        /// 벗어나 안 보이다가, 목을 뻗어 머리가 내려가면 딸려 내려와 목을 가로지른다.**
        /// 보스 체력은 위쪽 게이지가 따로 보여 주므로 이 바는 켤 이유가 없다.
        /// </summary>
        public void SetHpBarVisible(bool on)
        {
            if (_hpBarBg != null) _hpBarBg.gameObject.SetActive(on);
        }

        /// <summary>
        /// 방향·프레임 체계를 통째로 무시하고 이 한 장만 그린다.
        /// 빙의 연출처럼 **방향이 없는 동작**에 쓴다 — null 을 넣으면 원래대로 돌아간다.
        /// </summary>
        public void SetSpriteOverride(Sprite s)
        {
            _override = s;
            if (_body == null) return;
            if (s != null)
            {
                _body.sprite = s;
                // 반전이 걸려 있으면 연출 그림까지 뒤집힌다. ApplyBodyScale 이 풀어 준다.
                ApplyBodyScale();
                return;
            }
            _shownFrame = -1;   // 다음 Apply 가 반드시 다시 그리게 한다

            // ⚠ `Apply` 는 방향 그림 세트가 없으면 **아무것도 하지 않고 돌아간다.**
            //    그러면 연출 그림이 그대로 남는다 — 유령이 몸을 잃고 나올 때
            //    빙의 축소 3번째 장(콩알)인 채로 서 있었다.
            //    되돌릴 곳이 없으면 처음 받은 그림으로 직접 돌려놓는다.
            if (_frames[FrameIdle] != null && _facingIndex >= 0) Apply();
            else if (_baseSprite != null) _body.sprite = _baseSprite;
        }

        private Sprite _override;

        /// <summary>현재 (프레임 × 방향) 을 화면에 반영한다. 바뀐 게 없으면 아무것도 하지 않는다.</summary>
        private void Apply()
        {
            if (_override != null) return;   // 연출 그림이 쥐고 있는 동안은 건드리지 않는다
            if (_body == null || _facingIndex < 0 || _frames[FrameIdle] == null) return;

            // 그 동작의 그림이 없으면 idle 로 대신한다 — 없는 채로 두면 빈 칸이 된다.
            int f = _frames[_frame] != null ? _frame : FrameIdle;
            if (f == _shownFrame && _facingIndex == _shownIndex && _facingFlip == _shownFlip) return;

            _shownFrame = f;
            _shownIndex = _facingIndex;
            _shownFlip = _facingFlip;
            _body.sprite = _frames[f][_facingIndex];
            ApplyBodyScale();
        }

        // ── 숨쉬기 ───────────────────────────────────────────────
        //
        // 정지 그림 한 장으로 서 있으면 **죽은 것처럼 보인다.** 보스는 패턴 쿨
        // 사이에 제자리에 서 있는 시간이 길어서 특히 그렇다(기획 2026-09-03 —
        // "가만히 있으니깐 이상하자나").
        //
        // 그림을 더 받지 않고 **몸을 부풀렸다 줄인다.** 가로로 넓어질 때 세로로
        // 살짝 낮아지는 짝(스쿼시)이라 부피가 도는 것으로 읽히고, 세로 변화가
        // 절반이라 발이 바닥에서 뜨는 것이 거의 안 보인다.

        private const float BreatheSeconds = 1.7f;    // 한 번 들이쉬고 내쉬는 데 걸리는 시간
        private const float BreatheAmount = 0.035f;   // 가로로 최대 얼마나 부푸는가
        private float _breathePhase;
        private float _breatheX = 1f, _breatheY = 1f;

        /// <summary>
        /// 좌우 반전과 숨쉬기를 **한 군데서** 곱해 넣는다.
        /// 둘을 따로 쓰면 나중에 쓴 쪽이 앞의 것을 지운다.
        /// </summary>
        private void ApplyBodyScale()
        {
            if (_body == null) return;
            // 연출 그림(빙의 등)이 쥐고 있을 때는 반전을 풀어 준다 — 방향이 없는 그림이다.
            bool flip = _facingFlip && _override == null;
            _body.transform.localScale =
                new Vector3(flip ? -_breatheX : _breatheX, _breatheY, 1f);
        }

        private void TickBreathe(float dt)
        {
            // 보스만 숨쉰다. 잡몹·호스트는 늘 움직이고 있어서 필요 없다.
            bool idle = IsBoss && !_dying && !_possessHold && _override == null
                     && !_moving && _frame == FrameIdle;

            float x = 1f, y = 1f;
            if (idle)
            {
                _breathePhase += dt;
                float w = Mathf.Sin(_breathePhase * (Mathf.PI * 2f / BreatheSeconds));
                x = 1f + w * BreatheAmount;
                y = 1f - w * BreatheAmount * 0.5f;
            }
            else _breathePhase = 0f;

            if (Mathf.Approximately(x, _breatheX) && Mathf.Approximately(y, _breatheY)) return;
            _breatheX = x;
            _breatheY = y;
            ApplyBodyScale();
        }

        /// <summary>
        /// 바라보는 방향을 정한다. 8방향으로 반올림해 다섯 장 + 반전으로 표현한다.
        /// 방향이 안 바뀌면 아무것도 하지 않는다 — 매 프레임 스프라이트를 갈면 낭비다.
        /// </summary>
        /// <summary>
        /// 지금 바라보는 방향(단위 벡터). 액티브 스킬처럼 "앞쪽" 을 써야 하는 연출이 쓴다.
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
            int dealt = Mathf.Max(1, amount);

            // 쉴드가 먼저 깎인다. 다 막아 내면 체력은 건드리지 않는다 —
            // 그래야 격투가 "맞으면서 들어간" 값을 실제로 돌려받는다.
            if (_shield > 0)
            {
                int absorbed = Mathf.Min(_shield, dealt);
                _shield -= absorbed;
                dealt -= absorbed;
                if (dealt <= 0)
                {
                    RefreshHpBar();
                    BeginHitFlash();
                    PlayHit();
                    return false;
                }
            }

            Hp = Mathf.Max(0, Hp - dealt);
            RefreshHpBar();
            BeginHitFlash();
            PlayHit();          // 틴트와 자세를 같은 자리에서 시작해야 따로 놀지 않는다

            // 숙주만 경직이 쌓인다. 잡몹은 아무리 때려도 빼앗을 몸이 되지 않는다.
            if (IsHostBody && Hp > 0 && _staggerWindow <= 0f && _staggerCool <= 0f)
            {
                _stagger += dealt;
                if (_stagger >= HpMax * StaggerNeedRatio) _staggerWindow = StaggerWindowSeconds;
            }
            return Hp == 0;
        }

        public void Heal(int amount) { Hp = Mathf.Min(HpMax, Hp + amount); RefreshHpBar(); }

        /// <summary>
        /// 대가로 체력을 낸다. **여기서는 죽지 않는다** — 최소 1 은 남는다.
        /// `TakeDamage` 를 쓰면 몸을 잃는 흐름을 타는데, 거래로 몸이 죽는 것은
        /// 이벤트 방의 약속이 아니다.
        /// </summary>
        public void SpendHp(int amount)
        {
            if (amount <= 0) return;
            Hp = Mathf.Max(1, Hp - amount);
            RefreshHpBar();
        }

        private void RefreshHpBar()
        {
            if (_hpBarFill == null) return;
            var rt = (RectTransform)_hpBarFill.transform;
            float full = _hpBarBg.rectTransform.sizeDelta.x;
            rt.sizeDelta = new Vector2(full * ((float)Hp / HpMax), rt.sizeDelta.y);

            if (_shieldBarFill == null) return;
            bool on = _shield > 0 && HpMax > 0;
            _shieldBarFill.gameObject.SetActive(on);
            if (!on) return;
            // 쉴드는 체력과 같은 자에 잰다 — 최대 체력의 몇 할인지가 바로 보여야
            // "한 대는 더 버틴다" 가 읽힌다.
            var srt = (RectTransform)_shieldBarFill.transform;
            srt.sizeDelta = new Vector2(full * Mathf.Min(1f, (float)_shield / HpMax), srt.sizeDelta.y);
        }

        /// <summary>
        /// 공격 쿨다운을 진행시키고, 이번 프레임에 때릴 수 있으면 true.
        /// `intervalMul` 은 런 버프(연사 강화) 배율이다 — 유닛 스탯은 건드리지 않는다.
        /// </summary>
        public bool TickAttack(float dt, float intervalMul = 1f)
        {
            _attackTimer -= dt;
            if (_attackTimer > 0f) return false;
            // 약화가 걸려 있으면 손도 같이 느려진다 — 이동만 깎으면 원거리에겐 무효였다.
            _attackTimer = AttackInterval * Mathf.Max(0.05f, intervalMul) * SlowAttackMul;
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

        // ── 무적 점멸 ─────────────────────────────────────────────
        //
        // 무적은 **화면에 안 보이면 없는 것과 같다.** 빙의 직후 2.5초를 줘도
        // 그걸 모르면 그냥 웅크리고 있게 되고, 그러면 준 만큼 손해가 된다.
        // 켜져 있는 동안 몸이 깜빡여야 "지금은 맞아도 된다" 가 읽힌다.

        /// <summary>한 번 깜빡이는 데 걸리는 시간(초). 켜짐·꺼짐 각각 절반씩.</summary>
        private const float InvulnBlinkSeconds = 0.16f;

        private bool _invulnerable;
        private float _invulnPhase;

        /// <summary>무적 상태를 켜고 끈다. 매 프레임 불러도 된다.</summary>
        public void SetInvulnerable(bool on)
        {
            if (_invulnerable == on) return;
            _invulnerable = on;
            _invulnPhase = 0f;
        }

        /// <summary>
        /// 붉은 점멸이 끝나고 **다음 점멸까지 반드시 쉬는 시간.**
        ///
        /// ⚠ 이게 없으면 연사 앞에서 점멸이 한 번도 안 끊긴다. 실측 — 기관단총으로
        ///   보스를 쏘는 동안 몸 색을 1385번 재 보니 1194번(86%)이 붉은색이었다.
        ///   0.12초짜리 점멸이 0.056초 남았을 때 다음 탄이 다시 채워 넣어서,
        ///   보스가 **제 색으로 보이는 순간이 아예 없었다** — 은색·연두인
        ///   로봇 스네이크가 화면에서는 통째로 갈색이었다
        ///   (기획 2026-09-07 — "보스가 왜 색이 바꼈어 원본으로 해줘").
        ///   점멸은 「맞았다」를 알리는 것이지 몸 색을 갈아 치우는 것이 아니다.
        ///
        /// 점멸 시간의 **두 배**로 쉰다. 연사 앞에서 켜짐:꺼짐이 1:2 가 되어
        /// 제 색이 확실히 이긴다 — 같은 길이(1:1)로 쉬었더니 44% 가 붉은색이라
        /// 여전히 붉게 명멸하는 덩어리로 보였다.
        /// </summary>
        private const float FlashRestSeconds = HitSeconds * 2f;

        /// <summary>0 보다 크면 아직 쉬는 중 — 맞아도 붉게 물들이지 않는다.</summary>
        private float _flashRest;

        /// <summary>맞았다. 쉬는 중이 아니면 붉은 점멸을 켠다.</summary>
        private void BeginHitFlash()
        {
            if (_flashRest > 0f) return;
            _flashTimer = HitSeconds;
            _flashRest = HitSeconds + FlashRestSeconds;
        }

        /// <summary>피격 점멸. 스프라이트를 건드리지 않고 틴트만 흔든다.</summary>
        public void TickFlash(float dt)
        {
            // 쉬는 시계는 **무적·사망과 무관하게** 흐른다. 여기서 빠지면 그 동안
            // 멈춰 있다가 풀리는 순간 다시 붙박이 붉은색이 된다.
            if (_flashRest > 0f) _flashRest = Mathf.Max(0f, _flashRest - dt);

            // 사망 중에는 페이드가 색을 쥐고 있다. 여기서 흰색으로 되돌리면 페이드가 풀린다.
            if (_body == null || _dying) return;

            if (_invulnerable)
            {
                // 예고(보스 패턴)만은 무적 위에 남긴다 — 피할 시간을 알리는 색이라
                // 이쪽이 지워지면 패턴이 사고가 된다.
                if (_telegraph) return;
                _invulnPhase += dt;
                bool lit = Mathf.Repeat(_invulnPhase, InvulnBlinkSeconds) < InvulnBlinkSeconds * 0.5f;
                // 밝게 뜬 반 박자 / 반쯤 비치는 반 박자. 색이 아니라 **투명도**가 흔들려야
                // 상태이상 틴트(화상·빙결)와 겹쳐도 무적이라는 것이 따로 읽힌다.
                _body.color = lit ? new Color(1f, 1f, 1f, 1f)
                                  : new Color(0.75f, 0.92f, 1f, 0.35f);
                return;
            }

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
        // ── 예고 프레임 ──────────────────────────────────────────
        //
        // 색만 바꾸면 24개 패턴이 예고 때 전부 똑같이 보인다. **누가 시작했나**를
        // 알리는 것이 예고 4겹의 첫 겹이고, 그림이 이미 6종 들어와 있다
        // (`unit_{보스}_s_tell`) — 코드가 한 번도 안 불렀을 뿐이다.

        /// <summary>
        /// 예고 자세 — **방향마다 한 장씩**. 없는 방향은 null 이다.
        ///
        /// ⚠ 예전에는 정면(`s_tell`) 한 장을 방향과 무관하게 썼다. 그래서 옆을 보던
        ///   보스가 예고하는 순간 **정면으로 홱 돌았다.** 없는 방향은 아예 안 바꾼다 —
        ///   틀린 방향을 보여 주느니 제 방향 정지 그림이 낫다(색은 그대로 깜빡인다).
        /// </summary>
        private Sprite[] _tellSprites;
        private Sprite _tellRestore;

        /// <summary>예고할 때 갈아 끼울 그림 5장(s·se·e·ne·n). 없는 칸은 null.</summary>
        public void SetTellSprites(Sprite[] five) => _tellSprites = five;

        /// <summary>
        /// 방향 **없이** 프레임으로 도는 예고 자세. 있으면 방향별 정지 그림보다 이긴다.
        ///
        /// 제자리에서 하는 동작(가디언 「똬리」처럼 몸을 마는 것)은 위에서 보면
        /// 어느 쪽을 보든 같은 그림이다. 방향축에 다섯 장을 쓰느니 그 자리에
        /// **프레임**을 넣는 편이 훨씬 잘 읽힌다 — 조여드는 것이 보인다.
        /// </summary>
        private Sprite[] _tellFrames;

        /// <summary>방향 없는 예고 자세 프레임. null 이면 방향별 정지 그림으로 돌아간다.</summary>
        public void SetTellFrames(Sprite[] frames) => _tellFrames = frames;

        /// <summary>
        /// 예고 **색**을 켜고 끈다. 부르는 쪽이 0.08초마다 뒤집어 깜빡임을 만든다.
        ///
        /// ⚠ **그림은 여기서 안 바꾼다.** 예전에는 켤 때마다 몸을 예고 그림으로 갈고
        ///   끌 때마다 되돌렸는데, 0.08초마다 뒤집히므로 예고 1.25초 동안 **15번**
        ///   왔다 갔다 했다 — 뒤를 보고 있다가 정면으로 튀었다가를 반복해서
        ///   "애니메이션이 이상하다" 로 보였다. 자세는 <see cref="SetTellPose"/> 가
        ///   예고 시작·끝에 **한 번씩만** 바꾼다.
        /// </summary>
        public void SetTelegraph(bool on)
        {
            _telegraph = on;
            if (_body == null) return;
            if (on) { _body.color = new Color(1f, 0.86f, 0.35f, 1f); return; }
            if (_flashTimer <= 0f) _body.color = Color.white;
        }

        /// <summary>
        /// 예고 자세. 예고가 도는 **동안 내내** 켜 두고 끝날 때 한 번 되돌린다.
        ///
        /// 예고 그림은 방향이 없는 한 장(`unit_{키}_s_tell`)이라, 켜 두면 예고 동안
        /// 정면을 본다 — 「지금 힘을 모으는 중」으로 읽히므로 그것이 맞다.
        /// 문제는 그 자세와 방향 자세를 **번갈아** 보여 준 것이었다.
        /// </summary>
        /// <param name="progress">
        /// 예고가 얼마나 찼는가(0 → 1). 프레임 자세는 이 값으로 장을 고른다 —
        /// 제 시계를 따로 돌리면 예고가 끝나는 순간과 마지막 장이 어긋난다.
        /// </param>
        public void SetTellPose(bool on, float progress = 0f)
        {
            if (_body == null) return;

            if (on)
            {
                Sprite want = null;

                // 프레임 자세가 있으면 그것이 이긴다. 마지막 장에서 **멈춘다** —
                // 되돌아 풀리면 "조이다 말았다" 가 되어 터질 것 같지 않다.
                if (_tellFrames != null && _tellFrames.Length > 0)
                {
                    int i = Mathf.Clamp(
                        Mathf.FloorToInt(Mathf.Clamp01(progress) * _tellFrames.Length),
                        0, _tellFrames.Length - 1);
                    want = _tellFrames[i];
                }
                // 아니면 지금 보는 방향의 정지 그림. 그것도 없으면 **자세를 안 바꾼다.**
                else if (_tellSprites != null && _facingIndex >= 0
                      && _facingIndex < _tellSprites.Length)
                    want = _tellSprites[_facingIndex];

                if (want == null) return;

                // ⚠ 되돌릴 그림을 **켤 때** 기억한다. 끌 때 정하면 이미 예고 그림이라
                //   예고 그림으로 되돌아가 영영 안 풀린다.
                if (_body.sprite != want)
                {
                    if (_tellRestore == null) _tellRestore = _body.sprite;
                    _body.sprite = want;
                }
                return;
            }
            if (_tellRestore != null) { _body.sprite = _tellRestore; _tellRestore = null; }
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

        // ── C004 갑옷 분쇄 ───────────────────────────────────────
        //
        // 저주와 달리 **때린 쪽이 아니라 맞은 쪽에 쌓인다.** 대상을 바꿔도
        // 그 대상의 겹은 그대로 남아 있어, 오래 붙어 싸운 보스가 뒤로 갈수록 무너진다.
        // 상태이상이 아니라 물리적으로 갑옷이 벗겨진 것이므로 화상·저주와 따로 센다.

        public const int ArmorBreakMaxStack = 5;
        private const float ArmorBreakFadeSeconds = 3f;

        private int _armorBreak;
        private float _armorBreakTimer;

        public int ArmorBreakStack => _armorBreak;

        public void AddArmorBreak()
        {
            _armorBreak = Mathf.Min(ArmorBreakMaxStack, _armorBreak + 1);
            _armorBreakTimer = ArmorBreakFadeSeconds;
        }

        /// <summary>겹당 늘어나는 피해 배수. 겹이 없으면 1 이다.</summary>
        public float ArmorBreakMul(float perStack)
            => _armorBreak > 0 ? 1f + _armorBreak * perStack : 1f;

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
            // 때리기를 멈추면 벗겨 놓은 갑옷이 도로 붙는다 — 안 그러면
            // 한 번 5겹을 쌓아 둔 보스가 방이 끝날 때까지 그대로다.
            if (_armorBreakTimer > 0f)
            {
                _armorBreakTimer -= dt;
                if (_armorBreakTimer <= 0f) _armorBreak = 0;
            }

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

        // ── 쉴드 ─────────────────────────────────────────────
        //
        // 격투 직업이 때릴 때마다 쌓는다. 근접은 사거리를 버리고 들어가는 몸이라
        // 맞는 것이 전제다 — 때린 만큼 돌려받지 못하면 그냥 손해만 보는 직업이 된다.
        private int _shield;

        public int Shield => _shield;

        /// <summary>지금 그리고 있는 몸 그림. 잔상이 이걸 복제한다.</summary>
        public Sprite BodySprite => _body != null ? _body.sprite : null;

        /// <summary>몸 그림의 좌우 반전 여부. 잔상이 같은 쪽을 봐야 한다.</summary>
        public bool BodyFlipX => _body != null && _body.transform.localScale.x < 0f;

        // ── 쉴드 감쇠 ────────────────────────────────────────
        //
        // 쉴드는 **싸우는 동안의 보상**이지 들고 다니는 자원이 아니다.
        // 예전에는 `TakeDamage` 로만 깎여서, 한 번 30% 를 채우면 방을 나가도
        // 그대로 들고 갔다 — 다음 방을 30% 더 두꺼운 몸으로 시작하는 셈이다.
        //
        // ⚠ **단순 시간 감쇠로 만들지 않는다.** 격투 6명의 획득 속도가 공격
        //   간격 때문에 1.5배 차이난다(아마존 5.45%/초 · 구루 3.53%/초).
        //   초당 일정량을 그냥 깎으면 느린 몸은 순증이 거의 0 이라 못 쌓는다 —
        //   같은 직업인데 느린 쪽만 벌을 받는다.
        //   그래서 **마지막 타격 후 경과 시간**을 기준으로 깎는다. 싸우는 동안은
        //   줄지 않으므로 획득 속도 차이가 감쇠에 영향을 주지 않는다.
        // ⚠ 유지·감소 값은 **`GameConfig` 가 갖는다.** 여기 상수로 두었더니
        //   "쉴드가 너무 오래 간다" 를 고치는 데 컴파일이 필요했다.
        //   부팅 때 `SetShieldRule` 로 한 번 받아 둔다 — 스프라이트와 같은 방식이다.
        private static float s_shieldHoldSeconds = 0.6f;
        private static float s_shieldDecayPerSecond = 0.12f;

        /// <summary>쉴드 유지·감소 규칙. 전투가 시작될 때 표에서 받아 넣는다.</summary>
        public static void SetShieldRule(float holdSeconds, float decayPerSecond)
        {
            s_shieldHoldSeconds = Mathf.Max(0f, holdSeconds);
            s_shieldDecayPerSecond = Mathf.Max(0f, decayPerSecond);
        }

        private const float ShieldBarHeightRatio = 0.6f;  // 체력 바 대비 굵기
        private const float ShieldBarGap = 2f;            // 체력 바와의 간격(px)

        private float _shieldIdle;      // 마지막 타격 후 경과 시간
        private float _shieldDecayCarry; // 소수점 이월 — 초당 5% 가 1 미만이어도 언젠가 깎인다

        public void AddShield(int amount, int cap)
        {
            if (amount <= 0 || cap <= 0) return;
            _shield = Mathf.Min(cap, _shield + amount);
            _shieldIdle = 0f;            // 때렸으니 유지 시간이 처음부터 다시 간다
            _shieldDecayCarry = 0f;
            RefreshHpBar();
        }

        /// <summary>
        /// 쉴드를 시간에 따라 깎는다. 마지막 타격 후 <see cref="ShieldHoldSeconds"/> 동안은
        /// 그대로 두고, 그 뒤부터 초당 최대 HP의 일정 비율씩 녹인다.
        /// </summary>
        public void TickShield(float dt)
        {
            if (_shield <= 0 || HpMax <= 0) return;

            _shieldIdle += dt;
            if (_shieldIdle < s_shieldHoldSeconds) return;

            _shieldDecayCarry += HpMax * s_shieldDecayPerSecond * dt;
            int whole = Mathf.FloorToInt(_shieldDecayCarry);
            if (whole <= 0) return;

            _shieldDecayCarry -= whole;
            _shield = Mathf.Max(0, _shield - whole);
            RefreshHpBar();
        }

        public void ClearShield()
        {
            _shield = 0;
            _shieldIdle = 0f;
            _shieldDecayCarry = 0f;
            RefreshHpBar();
        }

        // ── 피해 증폭 (갱스터 표식 · 영매 저주) ──────────────────
        //
        // 두 몸이 같은 층을 쓴다. **곱연산으로 겹치고 상한은 +150%** 다.
        // 합연산이면 둘을 겹칠 이유가 "숫자가 커져서" 뿐이지만, 곱연산이면
        // 표식 위에 저주를 얹는 것이 각각을 두 번 거는 것보다 이득이 된다 —
        // 두 몸을 번갈아 빙의할 이유가 그것이다.
        //
        // ⚠ 상한이 없으면 저주가 전염으로 무한히 번지는 Lv5 이후에
        //   잡몹 하나가 열 겹을 뒤집어쓴다. +150% 에서 자른다.

        private const float AmpMaxMul = 2.5f;      // +150%
        private const int AmpSlots = 4;

        private readonly float[] _ampMul = new float[AmpSlots];
        private readonly float[] _ampTimer = new float[AmpSlots];

        /// <summary>받는 피해 배수. 표식·저주가 겹치면 곱해지고 +150% 에서 멈춘다.</summary>
        public float AmpDamageMul
        {
            get
            {
                float m = 1f;
                for (int i = 0; i < AmpSlots; i++)
                    if (_ampTimer[i] > 0f) m *= _ampMul[i];
                return Mathf.Min(m, AmpMaxMul);
            }
        }

        /// <summary>증폭이 하나라도 걸려 있는가. 머리 위 표식을 띄울지 결정한다.</summary>
        public bool HasAmp
        {
            get
            {
                for (int i = 0; i < AmpSlots; i++) if (_ampTimer[i] > 0f) return true;
                return false;
            }
        }

        /// <summary>이 증폭에 남은 시간. 전이할 때 시간을 승계하려고 읽는다.</summary>
        public float AmpSecondsLeft
        {
            get
            {
                float t = 0f;
                for (int i = 0; i < AmpSlots; i++) if (_ampTimer[i] > t) t = _ampTimer[i];
                return t;
            }
        }

        /// <summary>
        /// 증폭을 건다. `percent` 30 이면 받는 피해 ×1.3.
        ///
        /// 같은 세기가 이미 걸려 있으면 **시간만 새로 고친다.** 칸을 따로 쓰면
        /// 표식을 두 번 건 것만으로 ×1.69 가 되어, 한 대상을 계속 찍는 것이
        /// 여럿에게 퍼뜨리는 것보다 이득이 된다 — 이 스킬의 뜻과 반대다.
        /// </summary>
        public void ApplyAmp(int percent, float seconds)
        {
            if (percent <= 0 || seconds <= 0f) return;
            float mul = 1f + percent * 0.01f;

            for (int i = 0; i < AmpSlots; i++)
                if (_ampTimer[i] > 0f && Mathf.Abs(_ampMul[i] - mul) < 0.001f)
                { _ampTimer[i] = Mathf.Max(_ampTimer[i], seconds); return; }

            int slot = -1;
            float weakest = float.MaxValue;
            for (int i = 0; i < AmpSlots; i++)
            {
                if (_ampTimer[i] <= 0f) { slot = i; break; }
                if (_ampMul[i] < weakest) { weakest = _ampMul[i]; slot = i; }
            }
            // 칸이 다 찼으면 가장 약한 것을 밀어낸다. 새 것이 더 약하면 버린다.
            if (_ampTimer[slot] > 0f && _ampMul[slot] >= mul) return;
            _ampMul[slot] = mul;
            _ampTimer[slot] = seconds;
        }

        public void TickAmp(float dt)
        {
            for (int i = 0; i < AmpSlots; i++)
                if (_ampTimer[i] > 0f) _ampTimer[i] -= dt;
        }

        public void ClearAmp()
        {
            for (int i = 0; i < AmpSlots; i++) { _ampTimer[i] = 0f; _ampMul[i] = 1f; }
        }

        // ── 그림만 띄우기 (VAULT 체공) ───────────────────────────
        //
        // 자리(`Position`)는 그대로 두고 **그림만** 위로 올린다. 자리를 옮기면
        // 거리 판정·정렬·분리 밀기가 전부 공중의 좌표를 보게 되어,
        // 떠 있는 동안 옆 사람이 그 자리를 비켜 준다 — 착지할 곳이 사라진다.
        // 바닥에 남는 그림자가 곧 실제 자리다.

        private float _spriteLift;

        public void SetSpriteLift(float pixels)
        {
            if (Mathf.Approximately(_spriteLift, pixels)) return;
            _spriteLift = pixels;
            if (_body != null)
                _body.rectTransform.anchoredPosition = new Vector2(0f, pixels);
        }

        // ── 숨기 — 벽 뒤 · 구멍 안 · 천장 ────────────────────────
        //
        // 보스 셋은 방에 늘 서 있지 않는다. 파이썬은 벽 뒤에, 로봇 스네이크는
        // 구멍 안에, 슬러지는 천장에 있다. **숨어 있는 동안은 때릴 수 없다.**
        //
        // ⚠ 자리(`Position`)는 그대로 둔다. 화면 밖으로 옮기면 거리 판정·정렬이
        //   전부 그 좌표를 보게 되고, 다시 나올 때 엉뚱한 데서 나온다.
        //   **몸 그림만 끄고 바닥에 그림자를 남긴다** — 그림자가 "저기 있다" 를 말한다.

        private Image _shadow;

        /// <summary>숨어 있는가. 숨어 있으면 조준에서도 빠지고 피해도 안 들어간다.</summary>
        public bool IsHidden { get; private set; }

        /// <summary>
        /// 몸을 숨기거나 드러낸다.
        /// <paramref name="showShadow"/> 가 켜져 있으면 바닥에 그림자를 남긴다 —
        /// 슬러지 천장 구간이 그렇다. 벽·구멍은 아예 안 보이는 것이 맞다.
        /// </summary>
        public void SetHidden(bool hidden, bool showShadow = false)
        {
            IsHidden = hidden;
            if (_body != null) _body.enabled = !hidden;

            if (!hidden || !showShadow)
            {
                if (_shadow != null) _shadow.enabled = false;
                return;
            }

            if (_shadow == null)
            {
                var half = ((RectTransform)transform).sizeDelta;
                // 납작한 타원. 몸보다 작고 바닥에 붙는다.
                _shadow = GetOrCreate("Shadow", new Vector2(half.x * 0.72f, half.y * 0.26f),
                                      new Vector2(0f, -half.y * 0.34f));
                _shadow.color = new Color(0f, 0f, 0f, 0.45f);
                _shadow.transform.SetAsFirstSibling();   // 몸보다 뒤에 그린다
            }
            _shadow.enabled = true;
        }

        // ── 행동 패턴의 제 상태 ──────────────────────────────────
        //
        // 도약·체공·포탑 회전은 **적마다 따로** 흘러야 한다. 감독이 딕셔너리로
        // 들고 있으면 죽거나 빙의로 편이 바뀔 때마다 정리해야 하고, 한 번 빠뜨리면
        // 죽은 몸의 타이머가 계속 돈다. 몸에 붙여 두면 몸과 함께 사라진다.
        //
        // ⚠ 값의 뜻은 **패턴마다 다르다.** 여기서 이름을 뜻으로 짓지 않는 이유다 —
        //   `PatternPhase` 는 HOP 에겐 멈춤/웅크림/도약이고 VAULT 에겐 네 단계다.
        //   뜻은 `BattleDirector.Patterns.cs` 의 각 패턴이 갖는다.

        public int PatternPhase { get; set; }
        public float PatternTimer { get; set; }
        public Vector2 PatternFrom { get; set; }
        public Vector2 PatternTo { get; set; }

        /// <summary>십자 포탑의 발사 축(도). 쏠 때마다 45° 돈다.</summary>
        public float PatternAngle { get; set; }

        public void ResetPattern()
        {
            PatternPhase = 0;
            PatternTimer = 0f;
            PatternFrom = PatternTo = Position;
            PatternAngle = 0f;
            SetSpriteLift(0f);
        }

        // ── 스턴 ─────────────────────────────────────────────
        //
        // 굳어 있는 동안은 다가오지도 때리지도 않는다. 맞은 자세로 세워 두면
        // 새 그림 없이도 "멈췄다" 가 읽힌다.
        private float _stunTimer;

        public bool IsStunned => _stunTimer > 0f;

        public void ApplyStun(float seconds)
        {
            if (seconds <= 0f) return;
            _stunTimer = Mathf.Max(_stunTimer, seconds);
            PlayHit();
        }

        public void TickStun(float dt)
        {
            if (_stunTimer <= 0f) return;
            _stunTimer -= dt;
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

        /// <summary>
        /// 약화가 공격 간격을 늘리는 배율.
        ///
        /// ⚠ 예전에는 약화가 **이동속도만** 깎았다. 그런데 원거리 적은 사거리 안에 들어오면
        ///   거의 움직이지 않는다(`BattleDirector.TickEnemies` — 두 번 쏘고 한 번만 옮긴다).
        ///   그래서 35% 확률로 터져 봐야 **사실상 아무 일도 안 일어났다.**
        ///   느려지는 만큼 손도 느려져야 "약해졌다" 가 된다.
        ///
        /// 최대 약화(-40%)에서 간격 +40%. 설계값은 `BattleDirector.SlowProcPercent` 가 정한다.
        /// </summary>
        public float SlowAttackMul => 1f + _slowPercent / 100f;

        /// <summary>약화가 걸려 있는가. 설녀의 파쇄가 두 배가 되는 조건이다.</summary>
        public bool IsSlowed => _slowPercent > 0;

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
