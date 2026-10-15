using System;
using Game.Module.InGame;
using UnityEngine;

namespace Game.Module.Common.Modes
{
    /// <summary>호스트의 거리 분류 — 탑 층 조건 · 밤 요일 조건이 같이 쓴다(한 곳에서만 정한다).</summary>
    public enum RangeClass
    {
        Melee,
        Mid,
        Long,
    }

    /// <summary>탑 층 규칙. ⚠ 표가 번호로 저장된다 — 새 규칙은 끝에 붙인다.</summary>
    public enum TowerRule
    {
        None,
        BodyRange,
        BodyAffinity,
        EnemyAffinity,
        NoLeave,
        SingleHost,
        OneShot,
        Boss,
    }

    /// <summary>밤 요일 조건 종류.</summary>
    public enum NightFilter
    {
        All,
        Range,
        Affinity,
    }

    [Serializable]
    public sealed class RangeEntry
    {
        [SerializeField] private string _hostKey = string.Empty;
        [SerializeField] private RangeClass _range;

        public string HostKey => _hostKey;
        public RangeClass Range => _range;

        public RangeEntry(string hostKey, RangeClass range)
        {
            _hostKey = hostKey;
            _range = range;
        }
    }

    [Serializable]
    public sealed class ModeReward
    {
        [SerializeField] private int _shards;
        [SerializeField] private int _gold;

        public int Shards => _shards;
        public int Gold => _gold;

        public ModeReward(int shards, int gold)
        {
            _shards = shards;
            _gold = gold;
        }
    }

    [Serializable]
    public sealed class SweepStep
    {
        [SerializeField] private int _fromFloor;
        [SerializeField] private int _shards;
        [SerializeField] private int _gold;

        public int FromFloor => _fromFloor;
        public int Shards => _shards;
        public int Gold => _gold;

        public SweepStep(int fromFloor, int shards, int gold)
        {
            _fromFloor = fromFloor;
            _shards = shards;
            _gold = gold;
        }
    }

    /// <summary>탑 한 층(기획서 AVSR_Content_Tower 3-5).</summary>
    [Serializable]
    public sealed class TowerFloor
    {
        [SerializeField] private int _floor;
        [SerializeField] private string _nameKey = string.Empty;
        [SerializeField] private string _descKey = string.Empty;
        [SerializeField] private TowerRule _rule;
        [SerializeField] private RangeClass _range;
        [SerializeField] private Affinity _affinity;
        [SerializeField] private string _bossKey = string.Empty;
        [SerializeField] private string[] _trashKeys = Array.Empty<string>();
        [SerializeField] private int _trashCount;
        [SerializeField] private string _roomId = string.Empty;

        public int Floor => _floor;
        public string NameKey => _nameKey;
        public string DescKey => _descKey;
        public TowerRule Rule => _rule;
        public RangeClass Range => _range;
        public Affinity Affinity => _affinity;
        public string BossKey => _bossKey;
        public string[] TrashKeys => _trashKeys;
        public int TrashCount => _trashCount;
        public string RoomId => _roomId;
        public bool IsBoss => _rule == TowerRule.Boss;

        public TowerFloor(int floor, string nameKey, string descKey, TowerRule rule, RangeClass range, Affinity affinity,
                          string bossKey, string[] trashKeys, int trashCount, string roomId)
        {
            _floor = floor;
            _nameKey = nameKey;
            _descKey = descKey;
            _rule = rule;
            _range = range;
            _affinity = affinity;
            _bossKey = bossKey ?? string.Empty;
            _trashKeys = trashKeys ?? Array.Empty<string>();
            _trashCount = trashCount;
            _roomId = roomId ?? string.Empty;
        }
    }

    /// <summary>밤 요일 하나(기획서 AVSR_Content_NightMode 3-4).</summary>
    [Serializable]
    public sealed class NightDay
    {
        [SerializeField] private string _dayKey = string.Empty;    // mon … sun
        [SerializeField] private NightFilter _filter;
        [SerializeField] private RangeClass _range;
        [SerializeField] private Affinity _affinity;
        [SerializeField] private string _bossKey = string.Empty;   // 비면 무작위

        public string DayKey => _dayKey;
        public NightFilter Filter => _filter;
        public RangeClass Range => _range;
        public Affinity Affinity => _affinity;
        public string BossKey => _bossKey;

        public NightDay(string dayKey, NightFilter filter, RangeClass range, Affinity affinity, string bossKey)
        {
            _dayKey = dayKey;
            _filter = filter;
            _range = range;
            _affinity = affinity;
            _bossKey = bossKey ?? string.Empty;
        }
    }

    /// <summary>
    /// 추가 모드(망령의 탑 · 망령의 밤) 값의 단일 출처 — `TableData/ModeTable`.
    /// 기획서 AVSR_Content_Tower.md 4-1 · AVSR_Content_NightMode.md 4-1. 본편 값(유령 감소 · 경직 · 상성 · 경험치)은 여기 없다 — 본편 자리에서 읽는다.
    /// ⚠ 숫자를 코드에 적지 않는다. 값은 `Tools/Game/모드/모드 표 만들기` 에서 고친다.
    /// </summary>
    public sealed class ModeTable : ScriptableObject
    {
        // ── 공통 ──────────────────────────────────────────
        [SerializeField] private RangeEntry[] _ranges = Array.Empty<RangeEntry>();
        [SerializeField] private int _freeEntriesPerDay = 3;
        [SerializeField] private int _buyEntriesMax = 3;
        [SerializeField] private int[] _buyEntryGem = { 50, 100, 200 };

        // ── 탑 ────────────────────────────────────────────
        [SerializeField] private TowerFloor[] _floors = Array.Empty<TowerFloor>();
        [SerializeField] private int _loopLength = 10;
        [SerializeField] private float _hpPerFloor = 0.1f;
        [SerializeField] private float _hpPerLoop = 0.5f;
        [SerializeField] private float _hpGrowth = 0.15f;
        [SerializeField] private float _bossHpBase = 0.55f;
        [SerializeField] private float _bossHpPerLoop = 0.4f;
        [SerializeField] private float _atkGrowth = 0.06f;
        [SerializeField] private int _bodyCount = 3;
        [SerializeField] private ModeReward _firstClear = new(1, 50);
        [SerializeField] private ModeReward _firstClearBoss = new(3, 150);
        [SerializeField] private SweepStep[] _sweepSteps = Array.Empty<SweepStep>();

        // ── 밤 ────────────────────────────────────────────
        [SerializeField] private NightDay[] _days = Array.Empty<NightDay>();
        [SerializeField] private string _arenaRoomId = string.Empty;
        [SerializeField] private float _trashFirstDelay = 2f;
        [SerializeField] private float _spawnIntervalStart = 3.4f;
        [SerializeField] private float _spawnIntervalSlope = 160f;
        [SerializeField] private float _spawnIntervalMin = 1.6f;
        [SerializeField] private float _spawnCountStep = 75f;
        [SerializeField] private float _trashHpSlope = 300f;
        [SerializeField] private int _enemyCap = 30;
        [SerializeField] private string[] _trashBlade = Array.Empty<string>();
        [SerializeField] private string[] _trashForce = Array.Empty<string>();
        [SerializeField] private string[] _trashMagic = Array.Empty<string>();
        [SerializeField] private float _bigHostFirst = 40f;
        [SerializeField] private float _bigHostInterval = 45f;
        [SerializeField] private float _bigHostScale = 1.35f;
        [SerializeField] private float _bigHostHpBase = 5f;
        [SerializeField] private float _bigHostHpSlope = 90f;
        [SerializeField] private float _bigHostAtkMul = 1.3f;
        [SerializeField] private float _bigHostStaggerMul = 1.5f;
        [SerializeField] private float _waveInterval = 60f;
        [SerializeField] private int _waveHealPercent = 15;
        [SerializeField] private float[] _chiefTimes = { 120f, 210f };
        [SerializeField] private float _chiefHpMul = 1f;
        [SerializeField] private int _chiefHealPercent = 25;
        [SerializeField] private float _bossTime = 300f;
        [SerializeField] private float _bossHpMul = 0.9f;
        [SerializeField] private float _rescueDelay = 1f;
        [SerializeField] private float _rescueCooldown;
        [SerializeField] private int _expMulBigHost = 8;
        [SerializeField] private int _expMulChief = 12;
        [SerializeField] private int _killsPerShard = 50;
        [SerializeField] private int _clearShardBonus = 3;

        // 공통
        public RangeEntry[] Ranges => _ranges;
        public int FreeEntriesPerDay => _freeEntriesPerDay;
        public int BuyEntriesMax => _buyEntriesMax;
        public int[] BuyEntryGem => _buyEntryGem;

        // 탑
        public TowerFloor[] Floors => _floors;
        public int LoopLength => Mathf.Max(1, _loopLength);
        public float HpPerFloor => _hpPerFloor;
        public float HpPerLoop => _hpPerLoop;
        public float HpGrowth => _hpGrowth;
        public float BossHpBase => _bossHpBase;
        public float BossHpPerLoop => _bossHpPerLoop;
        public float AtkGrowth => _atkGrowth;
        public int BodyCount => _bodyCount;
        public ModeReward FirstClear => _firstClear;
        public ModeReward FirstClearBoss => _firstClearBoss;
        public SweepStep[] SweepSteps => _sweepSteps;

        // 밤
        public NightDay[] Days => _days;
        public string ArenaRoomId => _arenaRoomId;
        public float TrashFirstDelay => _trashFirstDelay;
        public float SpawnIntervalStart => _spawnIntervalStart;
        public float SpawnIntervalSlope => _spawnIntervalSlope;
        public float SpawnIntervalMin => _spawnIntervalMin;
        public float SpawnCountStep => _spawnCountStep;
        public float TrashHpSlope => _trashHpSlope;
        public int EnemyCap => _enemyCap;
        public float BigHostFirst => _bigHostFirst;
        public float BigHostInterval => _bigHostInterval;
        public float BigHostScale => _bigHostScale;
        public float BigHostHpBase => _bigHostHpBase;
        public float BigHostHpSlope => _bigHostHpSlope;
        public float BigHostAtkMul => _bigHostAtkMul;
        public float BigHostStaggerMul => _bigHostStaggerMul;
        public float WaveInterval => _waveInterval;
        public int WaveHealPercent => _waveHealPercent;
        public float[] ChiefTimes => _chiefTimes;
        public float ChiefHpMul => _chiefHpMul;
        public int ChiefHealPercent => _chiefHealPercent;
        public float BossTime => _bossTime;
        public float BossHpMul => _bossHpMul;
        public float RescueDelay => _rescueDelay;
        public float RescueCooldown => _rescueCooldown;
        public int ExpMulBigHost => _expMulBigHost;
        public int ExpMulChief => _expMulChief;
        public int KillsPerShard => Mathf.Max(1, _killsPerShard);
        public int ClearShardBonus => _clearShardBonus;

        public string[] TrashOf(Affinity affinity) => affinity switch
        {
            Affinity.Blade => _trashBlade,
            Affinity.Force => _trashForce,
            Affinity.Magic => _trashMagic,
            _ => _trashBlade,
        };

        /// <summary>표에 없는 호스트는 중거리로 본다(새 호스트를 표에 안 넣었을 때 어느 요일에도 안 빠지게).</summary>
        public RangeClass RangeOf(string hostKey)
        {
            if (_ranges != null)
                for (int i = 0; i < _ranges.Length; i++)
                    if (_ranges[i] != null && _ranges[i].HostKey == hostKey) return _ranges[i].Range;
            return RangeClass.Mid;
        }

        /// <summary>
        /// 그 층의 정의 — 표에 그 층 줄이 있으면 그 줄, 없으면 한 바퀴(LoopLength) 줄을 되풀이한다.
        /// </summary>
        public TowerFloor FloorAt(int floor)
        {
            if (_floors == null || _floors.Length == 0) return null;
            for (int i = 0; i < _floors.Length; i++)
                if (_floors[i] != null && _floors[i].Floor == floor) return _floors[i];
            int slot = (floor - 1) % LoopLength + 1;
            for (int i = 0; i < _floors.Length; i++)
                if (_floors[i] != null && _floors[i].Floor == slot) return _floors[i];
            return _floors[0];
        }

        /// <summary>잔몹 · 적 호스트 체력 배율 — (1 + (f−1)×0.1 + loop×0.5) × (1 + (f−1)×0.15).</summary>
        public float TowerHpMul(int floor)
        {
            int loop = (floor - 1) / LoopLength;
            return (1f + (floor - 1) * _hpPerFloor + loop * _hpPerLoop) * (1f + (floor - 1) * _hpGrowth);
        }

        public float TowerBossHpMul(int floor)
        {
            int loop = (floor - 1) / LoopLength;
            return (_bossHpBase + loop * _bossHpPerLoop) * (1f + (floor - 1) * _hpGrowth);
        }

        public float TowerAtkMul(int floor) => 1f + (floor - 1) * _atkGrowth;

        /// <summary>소탕 — 깬 층이 FromFloor 이상인 가장 높은 계단. 0층이면 null.</summary>
        public SweepStep SweepFor(int clearedFloor)
        {
            if (clearedFloor <= 0 || _sweepSteps == null) return null;
            SweepStep best = null;
            for (int i = 0; i < _sweepSteps.Length; i++)
                if (_sweepSteps[i] != null && clearedFloor >= _sweepSteps[i].FromFloor
                    && (best == null || _sweepSteps[i].FromFloor > best.FromFloor))
                    best = _sweepSteps[i];
            return best;
        }

        /// <summary>오늘 하루 젬으로 n 번째(0부터) 입장을 살 때의 값. 상한을 넘으면 −1.</summary>
        public int BuyGemAt(int boughtToday)
        {
            if (boughtToday >= _buyEntriesMax || _buyEntryGem == null || _buyEntryGem.Length == 0) return -1;
            return _buyEntryGem[Mathf.Min(boughtToday, _buyEntryGem.Length - 1)];
        }

        public void Fill(RangeEntry[] ranges, TowerFloor[] floors, SweepStep[] sweepSteps, NightDay[] days,
                         string[] trashBlade, string[] trashForce, string[] trashMagic, string arenaRoomId)
        {
            _ranges = ranges;
            _floors = floors;
            _sweepSteps = sweepSteps;
            _days = days;
            _trashBlade = trashBlade;
            _trashForce = trashForce;
            _trashMagic = trashMagic;
            _arenaRoomId = arenaRoomId;
        }
    }
}
