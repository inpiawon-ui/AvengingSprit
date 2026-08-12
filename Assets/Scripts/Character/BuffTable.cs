using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 룸 클리어 보상 버프의 효과 종류.
    /// 런 안에서만 유지되며 스테이지를 나가면 사라진다 (GameComposition 콘텐츠 5 — 런 한정 빌드).
    /// </summary>
    public enum BuffKind
    {
        /// <summary>피해 +n%</summary>
        Attack,
        /// <summary>발사 간격 -n%</summary>
        AttackSpeed,
        /// <summary>사거리 +n%</summary>
        Range,
        /// <summary>이동 속도 +n%</summary>
        MoveSpeed,
        /// <summary>고스트 최대 체력 +n (즉시 회복 포함)</summary>
        GhostHp,
        /// <summary>현재 호스트 체력 n% 회복 (즉발)</summary>
        Heal,
        /// <summary>탄 +n 발 (확산)</summary>
        MultiShot,
        /// <summary>탄이 적을 관통한다</summary>
        Pierce,
        /// <summary>피해의 n% 회복</summary>
        Lifesteal,
        /// <summary>명중 시 둔화 n%</summary>
        Slow,
        /// <summary>얼티밋 충전 +n%</summary>
        UltimateCharge,
        /// <summary>탄속 +n%</summary>
        ShotSpeed,

        // ── 정본 BUFF_DB 를 받으면서 생긴 종류 ────────────────────
        /// <summary>호스트 최대 체력 +n% (정본 BUF_U01 Vital Shell)</summary>
        HostMaxHp,
        /// <summary>정지 → 발사 지연 -n/100 초 (정본 BUF_U03 Quick Reset)</summary>
        StopDelay,
        /// <summary>전술 빙의 비용 -n (정본 BUF_U06 Tactical Mercy)</summary>
        TacticalCost,
        /// <summary>받는 피해 -n% (정본 BUF_A04 Heavy Frame)</summary>
        DamageReduction,
        /// <summary>전술 빙의 직후 무적 +n/100 초 (정본 BUF_A06 Safe Exit)</summary>
        SwitchShield,

        // ── 상태이상이 생기면서 살아난 종류 ────────────────────────
        /// <summary>화상 3단계가 주변으로 1단계 옮는다 (정본 BUF_T02 Burning Circuit)</summary>
        BurnSpread,
        /// <summary>같은 적을 연속으로 때리면 피해 +n% (최대 3단계) (정본 BUF_U04 Focused Soul)</summary>
        FocusedSoul,
    }

    /// <summary>
    /// 버프가 언제 켜지는가 (기획서 A 5-4).
    ///
    /// 이 구분이 있어야 "지금 쓰는 몸에 맞는 버프를 골랐는가"가 판단이 된다.
    /// 범용만 있으면 3택1 이 그냥 좋은 것 고르기가 된다.
    /// </summary>
    public enum BuffScope
    {
        /// <summary>항상 유지된다</summary>
        Common,
        /// <summary>해당 태그의 호스트를 쓸 때만 활성</summary>
        Tag,
        /// <summary>지정한 호스트를 쓸 때만 활성</summary>
        HostOnly,
    }

    /// <summary>호스트 계열 태그. 태그형 버프가 어느 몸에 붙는지를 가른다.</summary>
    public enum BuffTag
    {
        None,
        /// <summary>탄환 — 단발·연사·확산·저격</summary>
        Shot,
        /// <summary>근접</summary>
        Melee,
        /// <summary>마법 — 관통·광역</summary>
        Magic,
        /// <summary>설치물</summary>
        Deploy,
        /// <summary>브레스</summary>
        Breath,
    }

    [Serializable]
    public sealed class BuffEntry
    {
        [SerializeField] private string _buffKey;
        [SerializeField] private string _nameKr;
        [SerializeField] private string _description;
        [SerializeField] private BuffKind _kind;
        [SerializeField] private int _value;
        [Tooltip("여러 번 고를 수 있는가. 관통처럼 켜지면 끝인 효과는 false.")]
        [SerializeField] private bool _stackable = true;
        [Tooltip("카드 강조색 (#RRGGBB)")]
        [SerializeField] private string _colorHex = "#F0B428";

        [Header("적용 범위 (기획서 A 5-4)")]
        [SerializeField] private BuffScope _scope = BuffScope.Common;
        [Tooltip("Scope 가 Tag 일 때만 쓴다")]
        [SerializeField] private BuffTag _tag = BuffTag.None;
        [Tooltip("Scope 가 HostOnly 일 때만 쓴다")]
        [SerializeField] private string _hostKey;

        [Header("정본 대조 (BUFF_DB)")]
        [Tooltip("정본 BuffID(BUF_U01 …). 비어 있으면 우리 쪽에서만 있는 버프다")]
        [SerializeField] private string _canonId;
        [Tooltip("정본 Effect 원문. 구현 여부와 무관하게 그대로 담아 둔다 — " +
                 "나중에 시스템이 붙을 때 무엇을 만들어야 하는지가 여기 적혀 있다")]
        [SerializeField] private string _canonEffect;
        [Tooltip("정본 Pool — Universal / Tag / Synergy / AttackStyle")]
        [SerializeField] private string _pool;
        [Tooltip("이 챕터부터 뽑힌다. 정본 CH1/CH2/CH3 열")]
        [SerializeField] private int _fromChapter = 1;
        [Tooltip("정본 Weight — 뽑힐 가중치")]
        [SerializeField] private float _weight = 1f;
        [Tooltip("지금 실제로 동작하는가. " +
                 "정본 효과는 산문이라(예: 표식 대상 명중 시 릴레이 탄 1발) 표식·장판·저주 같은 " +
                 "시스템이 있어야 구현된다. 아직 없는 것은 꺼 두고 풀에서 뺀다 — " +
                 "고르면 아무 일도 안 일어나는 카드가 3택1 에 섞이면 선택 자체가 거짓이 된다.")]
        [SerializeField] private bool _implemented = true;

        public string BuffKey => _buffKey;
        public string NameKr => _nameKr;
        public string Description => _description;
        public BuffKind Kind => _kind;
        public int Value => _value;
        public bool Stackable => _stackable;
        public string ColorHex => _colorHex;
        public BuffScope Scope => _scope;
        public BuffTag Tag => _tag;
        public string HostKey => _hostKey;
        public string CanonId => _canonId;
        public string CanonEffect => _canonEffect;
        public string Pool => _pool;
        public int FromChapter => Mathf.Max(1, _fromChapter);
        public float Weight => _weight <= 0f ? 1f : _weight;
        public bool Implemented => _implemented;

        /// <summary>지금 이 호스트를 쓰는 동안 켜져 있는가. 호스트가 없으면 범용만 켜진다.</summary>
        public bool IsActiveFor(HostEntry host) => _scope switch
        {
            BuffScope.Common => true,
            BuffScope.Tag => host != null && host.Tag == _tag,
            BuffScope.HostOnly => host != null && host.HostKey == _hostKey,
            _ => true,
        };
    }

    /// <summary>
    /// 버프 정의를 배열 하나로 관리한다. HostTable 과 같은 규약 — 개별 asset 분리 금지.
    /// 에셋 `Assets/BundleResource/TableData/BuffTable.asset` · 주소 `TableData/BuffTable`
    /// </summary>
    [CreateAssetMenu(fileName = "BuffTable", menuName = "AVSR/Buff Table")]
    public sealed class BuffTable : ScriptableObject
    {
        [SerializeField] private BuffEntry[] _entries = Array.Empty<BuffEntry>();

        public IReadOnlyList<BuffEntry> Entries => _entries;

        public BuffEntry Get(string buffKey)
        {
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i].BuffKey == buffKey) return _entries[i];
            return null;
        }

        // 뽑기 비율 (기획서 A 5-5). 범용만 쏟아지면 3택1 이 "좋은 것 고르기"가 되고,
        // 전용만 쏟아지면 지금 몸에 안 맞는 카드만 나와 선택이 무의미해진다.
        private const int CommonPercent = 60;
        private const int TagPercent = 30;      // 나머지 10% 가 호스트 전용

        private readonly List<BuffEntry> _common = new();
        private readonly List<BuffEntry> _tag = new();
        private readonly List<BuffEntry> _hostOnly = new();

        /// <summary>
        /// 서로 다른 버프 `count` 개를 뽑는다. `exclude` 는 이미 획득해 중복 불가인 키다.
        ///
        /// 범용 60 / 태그형 30 / 호스트 전용 10 비율로 분류를 먼저 고르고 그 안에서 뽑는다.
        /// 해당 분류가 비면 다른 분류로 넘어간다 — 뽑을 것이 없어 빈손이 되는 편이 더 나쁘다.
        ///
        /// 태그형·전용은 **지금 쓰는 호스트에 맞는 것만** 후보로 넣는다. 안 맞는 카드는
        /// 고르는 순간 꺼져 있어서, 3택1 이 사실상 2택이 되어 버린다.
        /// </summary>
        public void Draw(List<BuffEntry> into, int count, HashSet<string> exclude,
                         System.Random rng, HostEntry host = null, int chapter = 1)
        {
            into.Clear();
            _common.Clear(); _tag.Clear(); _hostOnly.Clear();

            for (int i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (exclude != null && exclude.Contains(e.BuffKey)) continue;
                // 아직 동작하지 않는 버프는 뽑지 않는다. 고르면 아무 일도 안 일어나는
                // 카드가 섞이면 3택1 이라는 선택 자체가 거짓이 된다.
                if (!e.Implemented) continue;
                // 정본은 챕터마다 열리는 풀이 다르다(BUFF_DB 의 CH1/CH2/CH3 열)
                if (e.FromChapter > chapter) continue;
                switch (e.Scope)
                {
                    case BuffScope.Common: _common.Add(e); break;
                    case BuffScope.Tag: if (e.IsActiveFor(host)) _tag.Add(e); break;
                    case BuffScope.HostOnly: if (e.IsActiveFor(host)) _hostOnly.Add(e); break;
                }
            }

            for (int i = 0; i < count; i++)
            {
                var pool = PickPool(rng);
                if (pool == null) return;                 // 셋 다 비었다
                int k = rng.Next(pool.Count);
                into.Add(pool[k]);
                pool.RemoveAt(k);
            }
        }

        private List<BuffEntry> PickPool(System.Random rng)
        {
            int roll = rng.Next(100);
            var first = roll < CommonPercent ? _common
                      : roll < CommonPercent + TagPercent ? _tag
                      : _hostOnly;
            if (first.Count > 0) return first;

            // 비었으면 남은 쪽에서 채운다
            if (_common.Count > 0) return _common;
            if (_tag.Count > 0) return _tag;
            return _hostOnly.Count > 0 ? _hostOnly : null;
        }
    }
}
