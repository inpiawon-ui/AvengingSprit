using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 보스 행동 1종. 잡몹의 `AttackKind` 와 별개다 — 잡몹은 한 가지만 반복하지만
    /// 보스는 여러 행동을 쿨다운으로 돌려 쓰고, 체력이 깎이면 쓸 수 있는 행동이 늘어난다.
    /// </summary>
    public enum BossPattern
    {
        /// <summary>플레이어 방향으로 부채꼴 탄막</summary>
        Volley,
        /// <summary>사방 360° 탄막. 붙어 있으면 피하기 어렵다</summary>
        Ring,
        /// <summary>플레이어를 향해 돌진. 접촉 피해</summary>
        Charge,
        /// <summary>잡몹 소환. 보스만 노리다 둘러싸이게 만든다</summary>
        Summon,
        /// <summary>조준 연사. 짧은 간격으로 직선탄</summary>
        AimedBurst,

        // ── 정본이 이름 붙인 패턴 ──────────────────────────────────
        // 여기까지는 "탄을 몇 발 어느 각도로" 였다. 아래 셋은 보스마다 다른
        // **자리 싸움**을 만든다 — 무엇을 피하느냐가 보스마다 달라진다.

        /// <summary>
        /// 번갈아 솟는 레이저 (정본 B01 Alternating Pop-up Laser).
        /// 바닥에 금이 갔다가 그 줄에서 세로 광선이 솟는다. 쓸 때마다 줄이 바뀐다.
        /// </summary>
        PopupLaser,

        /// <summary>
        /// 실드 순환 + 내려찍기 (정본 B02 Crusher + Shield Cycle).
        /// 일정 시간 받는 피해가 줄고, 그동안 플레이어 자리에 그림자를 깔았다가 찍는다.
        /// </summary>
        ShieldCycle,

        /// <summary>
        /// 독구름 (정본 B03 Venom Clouds). 플레이어 자리 주변에 저주 장판을 남긴다.
        /// </summary>
        VenomCloud,
    }

    // ── 공간 기본형 6종 ────────────────────────────────────────────
    //
    // 패턴 24개는 전부 이 여섯의 매개변수 조합이다. 여섯만 만들면 24개가 데이터가 된다.
    //
    // ⚠ 이것을 만든 진짜 이유는 재사용이 아니라 **예고와 판정을 한 벌로 묶는 것**이다.
    //   예고에서 윤곽으로 그리고, 발동에서 그 안을 친다 — 같은 숫자, 같은 함수.
    //   따로 만들면 "분명히 피했는데 맞았다" 가 나오고, 하나로 묶으면
    //   그 버그가 구조적으로 불가능해진다.

    public enum BossShape
    {
        /// <summary>부채꼴 — 각도 · 반경 · 바라보는 방향</summary>
        Arc,
        /// <summary>직선 — 폭 · 길이 · 방향</summary>
        Line,
        /// <summary>줄 — 방을 n등분해 일부 줄만 위험</summary>
        Lane,
        /// <summary>구역 — 안전한 곳이 정해져 있고 그것이 움직인다</summary>
        Zone,
        /// <summary>돌진 — 접촉 피해 + 벽 충돌</summary>
        Dash,
        /// <summary>표식 — 대상을 지정하고 지연 후 그 자리를 친다</summary>
        Mark,
    }

    /// <summary>
    /// 같은 기본형 안의 변주. 그리는 방법이 갈리는 지점이다.
    /// 이름은 `AVSR_Bosses.js` 의 `draw.t` 를 그대로 옮긴 것이다.
    /// </summary>
    public enum BossDraw
    {
        None,
        Arc, Halves, Fan,                    // Arc
        Line, CrossLine, Burst, Sweep, Cable, Homing,   // Line
        Lane,                                // Lane
        Ring, Trail, Cover, Quad, Split, Overload, Island,   // Zone
        Dash, Shed,                          // Dash
        Mark,                                // Mark
    }

    /// <summary>
    /// 화살표가 가리키는 것.
    ///
    /// ⚠ `Close` 와 `Hold` 가 있는 것이 중요하다. 전부 "도망쳐" 면 보스전이 도망 게임이 된다.
    ///   킹핀 재장전과 슬러지 분열 코어에서는 화살표가 **보스 쪽**을 가리킨다.
    /// </summary>
    public enum DodgeHint
    {
        /// <summary>뒤로 — 보스 등 뒤로 돈다</summary>
        Back,
        /// <summary>옆으로 — 좌우 아무 쪽이나</summary>
        Side,
        /// <summary>틈으로 — 표시된 한 곳으로</summary>
        Gap,
        /// <summary>직각으로 — 공격선에 수직으로</summary>
        Perp,
        /// <summary>안전지대로 — 초록 구역으로</summary>
        Zone,
        /// <summary>붙어라 — 지금은 도망칠 때가 아니다</summary>
        Close,
        /// <summary>몸을 갈아타라 — 이 게임에서 여기 한 곳뿐</summary>
        Swap,
        /// <summary>쏘지 마라 — 방향이 아니라 손을 놓는 것</summary>
        Hold,
    }

    /// <summary>보스가 걸치는 상태. 패턴과 달리 켜져 있는 동안 계속 작용한다.</summary>
    public enum BossState
    {
        None,
        /// <summary>정면 피해 감소 · 반사 — 가디언</summary>
        Guard,
        /// <summary>머리 둘 — HP 를 나눠 갖고 따로 움직인다 — 로봇 스네이크</summary>
        Twin,
        /// <summary>분열체 — 코어를 안 부수면 본체가 회복 — 슬러지</summary>
        Split,
    }

    [Serializable]
    public sealed class BossMove
    {
        [SerializeField] private BossPattern _pattern;
        [Tooltip("이 페이즈부터 사용한다 (1=시작, 2=체력 60% 이하, 3=30% 이하)")]
        [SerializeField] private int _fromPhase = 1;
        [SerializeField] private float _cooldown = 4f;
        [SerializeField] private int _shotCount = 5;
        [SerializeField] private float _spreadDegrees = 60f;
        [SerializeField] private float _damageMul = 1f;

        [Header("예고 (정본 TelegraphSec)")]
        [Tooltip("이 패턴만의 예고 시간(초). 0 이면 페이즈 기본값을 쓴다.\n" +
                 "예고 길이는 피할 수 있느냐를 가르는 값이라 패턴마다 다르다 — " +
                 "킹핀 「처형 표식」 1.8초가 24개 중 가장 길다.")]
        [SerializeField] private float _telegraphSeconds;

        [Header("이름표 (처음 보는 패턴에만 뜬다)")]
        [SerializeField] private string _nameKr;
        [SerializeField] private string _nameEn;

        [Header("공간 — 예고로 그리고 그대로 판정한다")]
        [SerializeField] private BossShape _shape;
        [SerializeField] private BossDraw _draw;
        [SerializeField] private DodgeHint _dodge;

        [Tooltip("Arc 의 벌어진 각도(도)")]
        [SerializeField] private float _degrees;
        [Tooltip("Arc·Zone 의 반경(m). Ring 은 바깥 반경")]
        [SerializeField] private float _radiusMeters;
        [Tooltip("Line·Dash 의 폭(m)")]
        [SerializeField] private float _widthMeters;
        [Tooltip("Line 의 길이(m)")]
        [SerializeField] private float _lengthMeters;
        [Tooltip("Ring 의 안쪽 반경(m) — 여기까지 조여든다")]
        [SerializeField] private float _innerRadiusMeters;
        [Tooltip("Ring 의 틈 각도(도)")]
        [SerializeField] private float _gapDegrees;
        [Tooltip("Lane 의 줄 수")]
        [SerializeField] private int _lanes;
        [Tooltip("안전지대 좌표(m, 방 기준). (0,0)이면 없다")]
        [SerializeField] private Vector2 _safeAtMeters;

        public BossPattern Pattern => _pattern;
        public int FromPhase => Mathf.Max(1, _fromPhase);
        public float Cooldown => Mathf.Max(0.4f, _cooldown);
        public int ShotCount => Mathf.Max(1, _shotCount);
        public float SpreadDegrees => _spreadDegrees;
        public float DamageMul => _damageMul <= 0f ? 1f : _damageMul;

        /// <summary>이 패턴의 예고 시간. 0 이면 부르는 쪽이 페이즈 기본값을 쓴다.</summary>
        public float TelegraphSeconds => _telegraphSeconds;
        public bool HasTelegraph => _telegraphSeconds > 0f;

        public string NameKr => _nameKr;
        public string NameEn => _nameEn;

        public BossShape Shape => _shape;
        public BossDraw Draw => _draw;
        public DodgeHint Dodge => _dodge;

        public float Degrees => _degrees;
        public float RadiusMeters => _radiusMeters;
        public float WidthMeters => _widthMeters;
        public float LengthMeters => _lengthMeters;
        public float InnerRadiusMeters => _innerRadiusMeters;
        public float GapDegrees => _gapDegrees;
        public int Lanes => Mathf.Max(0, _lanes);
        public Vector2 SafeAtMeters => _safeAtMeters;
        public bool HasSafeSpot => _safeAtMeters.sqrMagnitude > 0.01f;

        /// <summary>이 패턴이 공간 데이터를 갖고 있는가. 없으면 옛 8패턴으로 돈다.</summary>
        public bool HasShape => _draw != BossDraw.None;

        /// <summary>이름표에 쓸 이름. 없으면 빈 문자열 — 그때는 이름표를 안 띄운다.</summary>
        public string LabelKey => string.IsNullOrEmpty(_nameEn) ? _nameKr : _nameEn;
    }

    [Serializable]
    public sealed class BossEntry
    {
        [SerializeField] private string _bossKey;
        [SerializeField] private int _chapter = 1;
        [Tooltip("이 보스가 서는 방 번호. CH1 006 이면 6.\n" +
                 "⚠ 챕터만으로는 못 가른다 — 챕터마다 보스가 둘이다(MID · FINAL).")]
        [SerializeField] private int _roomNo;
        [SerializeField] private string _nameEn;
        [SerializeField] private string _nameKr;
        [SerializeField] private string _spriteName = "unit_boss";

        [Header("전투")]
        [SerializeField] private float _hpMul = 1f;
        [SerializeField] private float _atkMul = 1f;
        [SerializeField] private float _moveSpeedMul = 1f;
        [Tooltip("페이즈가 오를 때마다 쿨다운에 곱하는 값. 1 미만이면 점점 빨라진다.")]
        [SerializeField] private float _phaseCooldownMul = 0.78f;

        [Tooltip("정본 체력·공격력. 0 이면 배율(HpMul·AtkMul)로 계산한다.")]
        [SerializeField] private int _canonHp;
        [SerializeField] private int _canonAtk;

        [Header("상태 · 취약 창")]
        [SerializeField] private BossState _state;
        [Tooltip("브레이크가 열려 있는 시간(초). 0 이면 취약 창이 없다.")]
        [SerializeField] private float _breakSeconds;
        [Tooltip("무엇을 해야 열리는가 — 화면에 띄우지는 않고 개발용 설명이다.")]
        [SerializeField] private string _breakCause;

        [SerializeField] private BossMove[] _moves = Array.Empty<BossMove>();

        public string BossKey => _bossKey;
        public int Chapter => _chapter;
        public int RoomNo => _roomNo;
        public string NameEn => _nameEn;
        public string NameKr => _nameKr;
        public string SpriteName => string.IsNullOrEmpty(_spriteName) ? "unit_boss" : _spriteName;
        public float HpMul => _hpMul <= 0f ? 1f : _hpMul;
        public float AtkMul => _atkMul <= 0f ? 1f : _atkMul;
        public float MoveSpeedMul => _moveSpeedMul <= 0f ? 1f : _moveSpeedMul;
        public float PhaseCooldownMul => Mathf.Clamp(_phaseCooldownMul, 0.3f, 1f);
        public int CanonHp => _canonHp;
        public int CanonAtk => _canonAtk;
        public bool HasCanonStats => _canonHp > 0 && _canonAtk > 0;
        public BossState State => _state;
        public float BreakSeconds => _breakSeconds;
        public bool HasBreak => _breakSeconds > 0f;
        public string BreakCause => _breakCause;
        public IReadOnlyList<BossMove> Moves => _moves;
    }

    /// <summary>
    /// 챕터 보스 정의. HostTable 과 같은 규약 — 배열 하나, 개별 asset 분리 금지.
    /// 에셋 `Assets/BundleResource/TableData/BossTable.asset` · 주소 `TableData/BossTable`
    /// </summary>
    [CreateAssetMenu(fileName = "BossTable", menuName = "AVSR/Boss Table")]
    public sealed class BossTable : ScriptableObject
    {
        [SerializeField] private BossEntry[] _entries = Array.Empty<BossEntry>();

        public IReadOnlyList<BossEntry> Entries => _entries;

        /// <summary>
        /// **그 방에 서는 보스.** 챕터마다 둘이라(MID · FINAL) 방 번호까지 봐야 한다.
        ///   CH1 006/012 · CH2 008/016 · CH3 010/020
        ///
        /// ⚠ 예전에는 <see cref="ForChapter"/> 하나뿐이라 CH1 006 과 012 에
        ///   **같은 보스가 섰다.** 방 6개인데 보스는 3명이었다.
        /// </summary>
        public BossEntry At(int chapter, int roomNo)
        {
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i].Chapter == chapter && _entries[i].RoomNo == roomNo)
                    return _entries[i];
            return null;
        }

        /// <summary>
        /// 키로 찾는다. 방 데이터가 이미 `BossId` 로 어느 보스인지 말해 주므로
        /// 이쪽이 가장 정확하다 — 방 번호를 세지 않아도 된다.
        /// </summary>
        public BossEntry ByKey(string bossKey)
        {
            if (string.IsNullOrEmpty(bossKey)) return null;
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i].BossKey == bossKey) return _entries[i];
            return null;
        }

        /// <summary>
        /// 그 챕터의 보스 하나. 방 번호를 모르는 자리(그림 미리 올리기 등)에서만 쓴다.
        /// **전투에서 쓰지 마라** — 챕터당 둘이라 어느 쪽인지 정하지 못한다.
        /// </summary>
        public BossEntry ForChapter(int chapter)
        {
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i].Chapter == chapter) return _entries[i];
            return _entries.Length > 0 ? _entries[0] : null;
        }
    }
}
