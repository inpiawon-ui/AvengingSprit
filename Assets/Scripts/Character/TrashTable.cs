using System;
using UnityEngine;

namespace Game.Character
{
    /// <summary>잡몹이 방에서 맡는 자리 — 3단계 방 틀의 「자리」와 짝이다.</summary>
    public enum TrashRole
    {
        Melee,
        Ranged,
        Flying,
        Turret,
        Support,
    }

    /// <summary>원소판 — 같은 몸에 탄 · 자국의 성질만 바뀐다. 쪽(파워 · 무기 · 마법)은 몸을 따른다.</summary>
    public enum TrashElement
    {
        None,
        Fire,
        Ice,
        Poison,
    }

    /// <summary>
    /// 잡몹 한 종이 하는 짓. **챕터 단계(`chapters.tsv` 의 `pattern`)를 대신한다**(2단계 PD 2026-10-22).
    ///
    /// 예전에는 「3챕터부터 해골이 죽을 때 4발」처럼 행동이 챕터 번호 문턱에 묶여 있어
    /// 한 방에 보통 해골과 터지는 해골을 섞을 수 없었고, 단계가 7에서 끝나 7~10챕터가 같이 싸웠다.
    /// 이제 행동은 몬스터 한 종의 성질이고, 어느 챕터 · 어느 방에서든 섞어 쓴다.
    ///
    /// ⚠ 값(비트 자리)을 바꾸지 않는다 — 표 에셋에 숫자로 구워진다. 새 성질은 늘 뒤에 더한다.
    /// </summary>
    [Flags]
    public enum TrashTrait : long
    {
        None = 0,

        // ── 지금 있던 단계 행동 ──
        Dive          = 1L << 0,    // 박쥐 · 멧돼지 — 띠 예고 → 돌진
        Weave         = 1L << 1,    // 박쥐 — 지그재그로 다가옴
        GunnerBurst   = 1L << 2,    // 고철 전갈 — 3연발
        Burst4        = 1L << 3,    // 연발이 4발
        Snipe         = 1L << 4,    // 고철 전갈 — 가끔 모아 쏘는 저격
        WardenBurst   = 1L << 5,    // 순찰기 — 부채꼴 대신 연발
        Spiral        = 1L << 6,    // 순찰기 — 회오리 유도탄
        Ambush        = 1L << 7,    // 해골 — 땅에 숨었다 솟음
        DeathBurst4   = 1L << 8,    // 해골 — 죽을 때 4발
        DeathBurst8   = 1L << 9,    // 해골 — 죽을 때 8발
        Ring          = 1L << 10,   // 해골 — 쫓으며 고리
        Slam          = 1L << 11,   // 돌 고릴라 — 착지 충격파
        GorillaMix    = 1L << 12,   // 돌 고릴라 — 도약 · 돌진 번갈아
        ChargeFirst   = 1L << 13,   // 돌 고릴라 — 돌진이 먼저
        SplitBats     = 1L << 14,   // 돌 고릴라 — 죽으면 박쥐 둘
        BounceShot    = 1L << 15,   // 코일 — 벽에 튕기는 탄
        SplitShot     = 1L << 16,   // 코일 — 갈라지는 탄
        Mortar        = 1L << 17,   // 코일 — 내 자리에 박격
        CrossAlt      = 1L << 18,   // 십자 포탑 — 4 · 8방향 번갈아
        CrossStream   = 1L << 19,   // 십자 포탑 — 돌며 연사
        BoarChain     = 1L << 20,   // 멧돼지 — 연속 돌진

        // ── 2단계 새 Lv2 ──
        MoleDouble    = 1L << 24,   // 두더지 — 두 번 연달아 솟음(두 번째는 5발)
        MantisDouble  = 1L << 25,   // 사마귀 — 저격 2연발
        ArmadilloRoll = 1L << 26,   // 아르마딜로 — 말아서 구르기
        MushroomPool  = 1L << 27,   // 폭탄 버섯 — 터진 자리에 독 장판
        MummyDouble   = 1L << 28,   // 미라 — 고리 두 겹
        SpiderPool    = 1L << 29,   // 알 거미 — 독 방울이 웅덩이로
        BatBounce     = 1L << 30,   // 박쥐 — 대각선 튕김 이동

        // ── 2단계 새 모양 ──
        Laser         = 1L << 36,   // 감시 눈 — 예고선 레이저
        Teleport      = 1L << 37,   // 그림자 살쾡이 — 사라졌다 옆에서 벰
        Boomerang     = 1L << 38,   // 톱날 원숭이 — 돌아오는 톱날
        Mimic         = 1L << 39,   // 미믹 상자 — 물건인 척
        ShieldAlly    = 1L << 40,   // 수호 등불 — 곁의 몸에 방패
        GhostHunt     = 1L << 41,   // 혼 사냥개 — 유령일 때 빨라짐
    }

    /// <summary>몬스터 한 종(변종 하나). 키가 방 · 챕터 표에 적히고, 몸(그림 · 행동 코드)은 여럿이 같이 쓴다.</summary>
    [Serializable]
    public sealed class TrashDef
    {
        [SerializeField] private string _key;
        [SerializeField] private string _body;
        [SerializeField] private string _sprite;
        [SerializeField] private string _nameKr;
        [SerializeField] private int _level = 1;
        [SerializeField] private TrashRole _role;
        [SerializeField] private TrashElement _element;
        [SerializeField] private long _traits;
        [SerializeField] private AttackKind _attackKind;
        [SerializeField] private int _hp;
        [SerializeField] private int _atk;
        [SerializeField] private float _moveMps;
        [SerializeField] private float _engageMps;
        [SerializeField] private float _rangeMeters;
        [SerializeField] private float _interval;
        [SerializeField] private float _telegraph;
        [SerializeField] private int _shotCount = 1;
        [SerializeField] private float _spreadDegrees;

        /// <summary>몬스터 키 — `skeleton` · `skeleton_2` · `gorilla_fire` …</summary>
        public string Key => _key;
        /// <summary>몸 키 — 그림 아틀라스(`atlas/unit_{몸}`) · 행동 코드 · 쪽이 이 키를 본다.</summary>
        public string Body => string.IsNullOrEmpty(_body) ? _key : _body;
        /// <summary>그림 키 — 원소판은 제 그림(`gorilla_fire`), 나머지는 몸 그림. 그림이 오기 전에는 임포터가 몸 키를 넣는다.</summary>
        public string Sprite => string.IsNullOrEmpty(_sprite) ? Body : _sprite;
        public string NameKr => _nameKr;
        public int Level => Mathf.Max(1, _level);
        public TrashRole Role => _role;
        public TrashElement Element => _element;
        public TrashTrait Traits => (TrashTrait)_traits;
        public bool Has(TrashTrait t) => ((TrashTrait)_traits & t) != 0;
        /// <summary>쏘는 놈인가 — 방 자리를 역할로 갈아 끼울 때 본다. 비행이어도 쏘면(감시 눈) 쏘는 놈이다.</summary>
        public bool IsRanged => _attackKind != AttackKind.Melee;

        public AttackKind AttackKind => _attackKind;
        public int Hp => _hp;
        public int Atk => _atk;
        public float MoveMps => _moveMps;
        public float EngageMps => _engageMps;
        public float RangeMeters => _rangeMeters;
        public float Interval => _interval;
        public float Telegraph => _telegraph;
        public int ShotCount => Mathf.Max(1, _shotCount);
        public float SpreadDegrees => _spreadDegrees;
    }

    /// <summary>
    /// 몬스터 표 — 잡몹 모든 종. 원본은 `Assets/Scripts/Editor/TrashImporter.cs` 의 배열(메뉴 `Tools/Game/몬스터 표 임포트`).
    /// 에셋 `Assets/BundleResource/TableData/TrashTable.asset` · 주소 `TableData/TrashTable` · 라벨 `label_tabledata`
    /// </summary>
    [CreateAssetMenu(menuName = "Game/TrashTable", fileName = "TrashTable")]
    public sealed class TrashTable : ScriptableObject
    {
        [SerializeField] private TrashDef[] _entries = Array.Empty<TrashDef>();

        public TrashDef[] Entries => _entries;

        public TrashDef Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i] != null && _entries[i].Key == key) return _entries[i];
            return null;
        }
    }
}
