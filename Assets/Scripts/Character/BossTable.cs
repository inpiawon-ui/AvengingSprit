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
    /// <summary>
    /// 패턴 하나하나의 이름. **24개 패턴에 24개 값**이 일대일로 붙는다.
    ///
    /// ⚠ 예전에는 `Arc`·`Halves`·`Cable` 처럼 **모양만 가리키는** 20개를 24패턴이
    ///   나눠 썼다. 그래서 `CheckBreak` 이 `case BossDraw.Dash` 하나로 크러셔 돌진과
    ///   가디언 머리 물기를 같이 잡았고, 도형을 만들 때도 어느 보스 것인지 알 수 없었다.
    ///   **패턴마다 제 이름을 갖는다.** 값 하나가 곧 패턴 하나다.
    ///
    /// 이름은 `_exchange/out/42_jobs/AVSR_Bosses.js` 의 `en` 을 그대로 옮긴 것이다.
    /// </summary>
    public enum BossDraw
    {
        None,

        // ── B01 크러셔 — 쓰레기를 씹는 기계 ─────────────────────
        Crush,           // 압착 — 아치형 입이 앞을 내려찍는다
        WreckingBall,    // 쇠사슬 파괴구 — 원 궤도. 안쪽이 안전하다
        Conveyor,        // 컨베이어 가동 — 바닥 세 줄 중 둘이 흐른다
        ShieldUp,        // 방패 전개 — 정면을 막고 압착을 두 번
        RamCharge,       // 돌진 — 나에게 붉은 줄을 긋고 그 줄을 타고 밀고 들어온다

        // ── B02 가디언 — 마디를 하나씩 끊어라 ───────────────────
        SegmentThrust,   // 마디 돌진 — 길이가 남은 마디 수를 따른다
        SegmentLaunch,   // 마디 사출 — 마디 둘을 떼어 굴린다
        CoilWall,        // 똬리 — 원형 벽. 틈으로 들어가면 머리가 있다
        HeadBite,        // 머리 물기 — 마디가 적을수록 빠르다

        // ── B04 파이썬 — 벽에서 나온다 ──────────────────────────
        WallBurst,       // 벽 돌파 — 나올 자리는 벽의 금으로만 안다
        VenomCloud,      // 독구름 — 퍼진다. 다 퍼지면 방 절반이다
        BodyCross,       // 몸통 가로지르기 — 벽에서 벽으로 한 줄
        TripleBurst,     // 세 갈래 돌파 — 안 겹치는 자리가 하나뿐

        // ── B03 킹핀 — 하늘에 떠 있다 ───────────────────────────
        MissileSalvo,    // 미사일 일제 — 착탄 원 다섯
        ExecutionLock,   // 처형 조준 — 몸을 갈아타야 벗는다
        StrafingRun,     // 저공 활강 — 이때만 근접이 닿는다
        BoosterDrop,     // 부스터 강하 — 그림자 예고 후 내려찍는다

        // ── B05 로봇 스네이크 — 구멍에서 나온다 ─────────────────
        HatchOpen,       // 구멍 개방 — 덮개가 열리는 것이 곧 예고다
        RailLaser,       // 레이저 — 조준선이 먼저 그려진다
        DebrisFall,      // 천장 파편 — 그림자 다섯
        FullEmergence,   // 일제 출현 — 안 솟은 구멍 하나가 안전지대

        // ── B06 슬러지 — 위에서 떨어진다 ────────────────────────
        Emerge,          // 솟아오름 — 바닥이 부풀어 예고한다
        Spit,            // 뱉기 — 웅덩이가 남아 다음을 못 피하게 한다
        CeilingCling,    // 천장 붙기 — 몸이 사라지고 그림자만 남는다
        CeilingSpread,   // 천장 확산 — 깨끗한 자리가 옮겨 다닌다
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
    /// <summary>
    /// 보스가 **방과 맺는 관계**. 넷은 그냥 서서 싸우지 않는다.
    ///
    /// ⚠ 예전 값(`Guard`·`Twin`·`Split`)은 원작 그림을 안 보고 지은 것이라 지웠다 —
    ///   가디언은 방패병이 아니라 **지네**고, 로봇 스네이크는 머리 둘이 아니라
    ///   **바닥 구멍 여섯**이며, 슬러지는 분열하는 게 아니라 **천장에 붙는다.**
    /// </summary>
    public enum BossState
    {
        /// <summary>방 안에 서서 싸운다 — 크러셔 · 킹핀</summary>
        None,
        /// <summary>마디 여덟. 마디를 3 이하로 끊기 전까지 머리가 무적이다 — 가디언</summary>
        Segments,
        /// <summary>방 안에 없다. 벽 뒤에 있다가 뚫고 나온다 — 파이썬</summary>
        Walls,
        /// <summary>본체가 없다. 바닥 구멍 여섯에서 번갈아 솟는다 — 로봇 스네이크</summary>
        Holes,
        /// <summary>천장에 붙는다. 붙은 동안은 그림자만 보인다 — 슬러지</summary>
        Ceiling,
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
