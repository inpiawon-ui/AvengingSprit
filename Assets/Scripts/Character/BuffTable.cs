using System;
using System.Collections.Generic;
using Game.Module.Common;
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
        /// <summary>액티브 스킬 충전 +n%</summary>
        ActiveSkillCharge,
        /// <summary>탄속 +n%</summary>
        ShotSpeed,

        // ── 정본 BUFF_DB 를 받으면서 생긴 종류 ────────────────────
        /// <summary>호스트 최대 체력 +n% (정본 BUF_U01 Vital Shell)</summary>
        HostMaxHp,
        /// <summary>정지 → 발사 지연 -n/100 초 (정본 BUF_U03 Quick Reset)</summary>
        StopDelay,
        /// <summary>받는 피해 -n% (정본 BUF_A04 Heavy Frame)</summary>
        DamageReduction,
        /// <summary>전술 빙의 직후 무적 +n/100 초 (정본 BUF_A06 Safe Exit)</summary>
        SwitchShield,

        // ── 상태이상이 생기면서 살아난 종류 ────────────────────────
        /// <summary>화상 3단계가 주변으로 1단계 옮는다 (정본 BUF_T02 Burning Circuit)</summary>
        BurnSpread,
        /// <summary>같은 적을 연속으로 때리면 피해 +n% (최대 3단계) (정본 BUF_U04 Focused Soul)</summary>
        FocusedSoul,

        // ── 장판이 생기면서 살아난 종류 ────────────────────────────
        /// <summary>장판 지속시간 +n/10 초 (정본 BUF_A02 Persistent Field)</summary>
        FieldDuration,
        /// <summary>둔화 장판 가장자리가 초당 n 피해 (정본 BUF_T03 Cold Geometry)</summary>
        SlowFieldEdge,
        /// <summary>지뢰가 빙결 룬이 된다 (정본 BUF_S04 Mine Alchemy)</summary>
        FreezeRune,

        // ── 도탄이 생기면서 살아난 종류 ────────────────────────────
        /// <summary>탄이 벽에서 n 번 튕긴다 (정본 BUF_T05 Bank Shot 의 바탕)</summary>
        Ricochet,
        /// <summary>튕긴 뒤의 탄이 피해 n% 로 때린다 (정본 BUF_A03 Return Path)</summary>
        ReturnDamage,


        /// <summary>범위 효과 반경 +n% (정본 BUF_U05 Wide Echo)</summary>
        AoeRadius,
        /// <summary>표식 폭발이 n 명 더 번지고 반경이 커진다 (정본 BUF_T01 Marked Payload)</summary>
        MarkPayload,
        /// <summary>흡혈 초과분이 고스트 체력으로 간다. 방마다 n 회 (정본 BUF_T04 Blood Debt)</summary>
        BloodDebt,
        /// <summary>확산의 마지막 탄이 피해 +n% (정본 BUF_A01 Last Magazine)</summary>
        LastShot,

        // ── 설치물이 생기면서 살아난 종류 ──────────────────────────
        /// <summary>포탑 재조준(발사 간격)이 n% 빨라진다 (정본 BUF_T06 Smart Deployment)</summary>
        DeployRetarget,
        /// <summary>포탑이 불을 물려받는다 (정본 BUF_S01 Fire Firmware)</summary>
        DeployFire,

        // ── 정본 v2.3 카드에서 새로 온 것 ────────────────────────
        /// <summary>C002 정밀 조준 — 유효 타깃이 **하나뿐일 때만** 피해 증가</summary>
        SingleTarget,
        /// <summary>C003 마무리 본능 — 체력이 낮은 적에게 피해 증가</summary>
        Execute,
        /// <summary>C015 보스 압축 — 보스에게 주는 피해 증가</summary>
        BossFocus,
        /// <summary>C017 생명 회수 — 적을 잡을 때마다 회복</summary>
        Regen,
        /// <summary>C024 전투 스텝 — 기본 공격 직후 1.2초 이동 속도 증가</summary>
        CombatStep,
        /// <summary>C025 냉기 각인 — 기본 공격에 빙결 부여</summary>
        FrostImprint,
        /// <summary>C026 화염 각인 — 기본 공격에 화상 부여</summary>
        FlameImprint,
        /// <summary>C027 저주 각인 — 기본 공격에 저주 부여</summary>
        CurseImprint,
        /// <summary>C005 연속 압박 — 같은 적을 연속으로 때릴수록 피해 증가(4타 최대)</summary>
        SustainStack,
        /// <summary>C009 유도 보정 — 탄이 대상을 쫓아간다</summary>
        Homing,
        /// <summary>C032 영혼 복제 — 유효 기본공격 8회마다 직전 공격을 한 번 복제</summary>
        SpectralEcho,
        /// <summary>C013 폭발 메아리 — 폭발이 끝난 자리에 축소된 2차 충격</summary>
        ExplosiveEcho,
        /// <summary>C030 유령 포대 — 유효 기본공격 8회마다 포대 1기를 3초간 소환</summary>
        GhostTurret,
        /// <summary>C004 갑옷 분쇄 — 때린 적이 받는 피해가 쌓여서 늘어난다</summary>
        ArmorBreak,
        /// <summary>C014 연쇄 번짐 — 걸린 상태이상이 옆 적으로 번진다</summary>
        StatusChain,
        /// <summary>C018 위기 방벽 — 체력이 위험해지면 방벽이 한 번 선다</summary>
        CrisisBarrier,
        /// <summary>C023 회피 잔상 — 아슬아슬하게 피하면 잔상이 반격한다</summary>
        Afterimage,
        /// <summary>C031 과충전 회로 — 명중이 전기를 튀긴다</summary>
        Overcharge,

        // ── 새 카드 10종 (2026-09-09) ────────────────────────────
        //
        // ⚠ **반드시 끝에 붙인다.** 표(`BuffTable.asset`)는 이 열거형의 **번호**를
        //   저장하므로, 중간에 하나만 끼워도 그 뒤가 전부 한 칸씩 밀려
        //   카드가 이름과 다른 효과로 돌아간다. 실제로 그렇게 19종이 어긋난 적이 있다.

        /// <summary>성장 가속 — EXP 획득 +n%</summary>
        ExpGain,
        /// <summary>수호 방패 — 내 주위를 도는 방패 n개</summary>
        OrbitShield,
        /// <summary>처형 — 약해진 적을 n% 확률로 즉사시킨다 (보스 제외)</summary>
        Assassinate,
        /// <summary>번개 사슬 — 전기 타격이 옆 적 n명에게 더 튄다</summary>
        ChainLightning,
        /// <summary>찰나의 불사 — 피격 시 2초 무적. 쿨 n초</summary>
        GuardInvuln,
        /// <summary>궁지 — 체력이 절반 아래면 피해 +n%</summary>
        LowHpPower,
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

    /// <summary>
    /// 카드 등급 (정본 v2.3). 뽑힐 확률과 테두리 색을 가른다.
    ///   COMMON 8종 60% · RARE 12종 28% · EPIC 8종 9.5% · LEGENDARY 4종 2.5%
    /// </summary>
    public enum CardRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
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
        [Tooltip("정본 Pool — Universal / Tag / AttackStyle")]
        [SerializeField] private string _pool;
        [Tooltip("이 챕터부터 뽑힌다. 정본 CH1/CH2/CH3 열")]
        [SerializeField] private int _fromChapter = 1;
        [Tooltip("정본 Weight — 뽑힐 가중치")]
        [SerializeField] private float _weight = 1f;
        [Header("정본 v2.3 카드")]
        [Tooltip("정본 CardID (C001 …). 아이콘 이름도 이것을 따른다 — card_c001")]
        [SerializeField] private string _cardId;
        [SerializeField] private CardRarity _rarity = CardRarity.Common;
        [Tooltip("정본 분류 — ATTACK / PROJECTILE / AREA / SURVIVAL / MOBILITY / UTILITY / SPECIAL")]
        [SerializeField] private string _category;
        [Tooltip("레벨 1~5 의 수치. 같은 카드를 다시 고르면 레벨이 오른다(최대 5).")]
        [SerializeField] private int[] _levelValues = Array.Empty<int>();

        [Tooltip("지금 실제로 동작하는가. " +
                 "정본 효과는 산문이라(예: 표식 대상 명중 시 릴레이 탄 1발) 표식·장판·저주 같은 " +
                 "시스템이 있어야 구현된다. 아직 없는 것은 꺼 두고 풀에서 뺀다 — " +
                 "고르면 아무 일도 안 일어나는 카드가 3택1 에 섞이면 선택 자체가 거짓이 된다.")]
        [SerializeField] private bool _implemented = true;

        public string BuffKey => _buffKey;
        public string NameKr => _nameKr;
        public string Description => _description;

        /// <summary>화면에 보이는 이름. 지금 언어로 — 번역이 없으면 원문(<see cref="NameKr"/>).</summary>
        public string DisplayName => Localize.FromTable($"card.{_buffKey}.name", _nameKr);
        /// <summary>화면에 보이는 설명. 지금 언어로 — 번역이 없으면 원문(<see cref="Description"/>).</summary>
        public string DisplayDescription => Localize.FromTable($"card.{_buffKey}.desc", _description);
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

        public string CardId => _cardId;
        public CardRarity Rarity => _rarity;
        public string Category => _category;
        public int MaxLevel => _levelValues != null && _levelValues.Length > 0 ? _levelValues.Length : 1;

        /// <summary>이 레벨에서의 수치. 레벨표가 없으면 예전 단일 값으로 떨어진다.</summary>
        public int ValueAt(int level)
        {
            if (_levelValues == null || _levelValues.Length == 0) return _value;
            return _levelValues[Mathf.Clamp(level, 1, _levelValues.Length) - 1];
        }

        /// <summary>
        /// 등급별 뽑힐 무게.
        ///
        /// 정본은 60/28/9.5/2.5 였지만 그건 **Common 8장** 기준이었다.
        /// 지금 목록은 Common 이 한 장뿐이라 그대로 두면 그 한 장만 계속 나온다.
        /// 카드 종류를 늘릴 때마다 등급별 장수를 보고 다시 봐야 하는 값이다.
        /// </summary>
        public static float WeightOf(CardRarity r) => r switch
        {
            CardRarity.Common    => 45.0f,
            CardRarity.Rare      => 30.0f,
            CardRarity.Epic      => 18.0f,
            CardRarity.Legendary =>  7.0f,
            _                    => 1f,
        };

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
            _draw.Clear();

            for (int i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (exclude != null && exclude.Contains(e.BuffKey)) continue;
                // 아직 동작하지 않는 카드는 뽑지 않는다. 고르면 아무 일도 안 일어나는
                // 카드가 섞이면 3택1 이라는 선택 자체가 거짓이 된다.
                if (!e.Implemented) continue;
                if (e.FromChapter > chapter) continue;
                if (!e.IsActiveFor(host)) continue;
                _draw.Add(e);
            }

            // 등급 가중 추첨 (정본 v2.3 — 60 / 28 / 9.5 / 2.5).
            // 등급 안에서는 균등하다. 가중치를 등급에만 두는 이유는,
            // 카드마다 가중치를 또 주면 "왜 이건 안 나오지" 를 아무도 설명 못 하기 때문이다.
            for (int i = 0; i < count && _draw.Count > 0; i++)
            {
                float total = 0f;
                for (int k = 0; k < _draw.Count; k++) total += BuffEntry.WeightOf(_draw[k].Rarity);

                float roll = (float)rng.NextDouble() * total;
                int pick = _draw.Count - 1;
                for (int k = 0; k < _draw.Count; k++)
                {
                    roll -= BuffEntry.WeightOf(_draw[k].Rarity);
                    if (roll > 0f) continue;
                    pick = k; break;
                }
                into.Add(_draw[pick]);
                _draw.RemoveAt(pick);
            }
        }

        private readonly List<BuffEntry> _draw = new();

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
