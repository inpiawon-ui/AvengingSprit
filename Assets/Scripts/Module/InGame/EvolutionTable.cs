using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 진화가 실제로 하는 일.
    ///
    /// 정본은 `AttackType` 을 11가지로 적어 두었지만(CROSSING_SCYTHE, ORBIT_RELEASE …)
    /// 그것은 **연출 이름**이고, 화면에서 갈리는 것은 "어떻게 날아와 어떻게 맞는가" 다.
    /// 액티브 스킬 25종을 거동 12가지로 묶었던 것과 같은 방식으로 5가지로 묶는다.
    /// </summary>
    public enum EvolutionKind
    {
        /// <summary>앞으로 꿰뚫고 지나간다 (낫·서리 길·프리즘 레이저)</summary>
        PiercingLane,
        /// <summary>부채꼴로 터진다 (업화·전면 폭격)</summary>
        ConeBurst,
        /// <summary>사방으로 흩어진다 (수리검 궤도·미사일 포화)</summary>
        Orbit,
        /// <summary>바닥에 남는다 (눈보라·심연 인장)</summary>
        Field,
        /// <summary>대신 쏘아 주는 것을 놓는다 (자동 로켓 포대)</summary>
        Turret,
    }

    /// <summary>
    /// 정본 v2.2 EVOLUTION_RUNTIME + RECIPE_RUNTIME.
    ///
    /// 진화는 카드가 아니다. **카드 두 장을 재료로 삼아 얻는 별개의 공격**이고,
    /// 얻고 나면 재료 카드는 그대로 남는다. 빌드 슬롯(8칸)이 아니라
    /// 진화 슬롯(3칸)을 쓰고, 액티브 스킬와 달리 버튼이 없다 —
    /// 제 쿨다운으로 알아서 나간다(정본 `INDEPENDENT_COOLDOWN`).
    /// </summary>
    [CreateAssetMenu(fileName = "EvolutionTable", menuName = "Game/Evolution Table")]
    public sealed class EvolutionTable : ScriptableObject
    {
        [SerializeField] private int _maxSlots = 3;
        [SerializeField] private EvolutionEntry[] _entries = Array.Empty<EvolutionEntry>();

        public int MaxSlots => Mathf.Max(1, _maxSlots);
        public IReadOnlyList<EvolutionEntry> Entries => _entries;

        public EvolutionEntry Get(string id)
        {
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i] != null && _entries[i].EvolutionId == id) return _entries[i];
            return null;
        }

        /// <summary>
        /// 지금 재료가 다 모인 진화 하나를 찾는다.
        /// 이미 가진 것과 아직 안 열린 것은 건너뛴다.
        /// </summary>
        public EvolutionEntry FindReady(Func<string, bool> ownsCard, ICollection<string> already)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (e == null || !e.Starter) continue;          // 해금은 아웃게임 몫 — 지금은 스타터만
                if (already != null && already.Contains(e.EvolutionId)) continue;
                if (!ownsCard(e.CardA) || !ownsCard(e.CardB)) continue;
                return e;
            }
            return null;
        }

        /// <summary>
        /// 아직 완성되지 않은 진화의 **모자란 재료** 카드 키를 모은다.
        /// 카드 3택1에서 이 카드에 금테를 두르고, 하나는 반드시 끼워 넣는다.
        /// </summary>
        public void CollectMissing(List<string> into, Func<string, bool> ownsCard,
                                   ICollection<string> already)
        {
            into.Clear();
            for (int i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (e == null || !e.Starter) continue;
                if (already != null && already.Contains(e.EvolutionId)) continue;

                bool a = ownsCard(e.CardA), b = ownsCard(e.CardB);
                if (a == b) continue;                            // 둘 다 있거나 둘 다 없다
                var need = a ? e.CardB : e.CardA;                 // 한 장만 더 있으면 완성이다
                if (!into.Contains(need)) into.Add(need);
            }
        }
    }

    [Serializable]
    public sealed class EvolutionEntry
    {
        [SerializeField] private string _evolutionId;
        [SerializeField] private string _nameKr;
        [SerializeField] private string _nameEn;
        [SerializeField] private string _cardA;
        [SerializeField] private string _cardB;
        [SerializeField] private bool _starter;
        [SerializeField] private string _motifHost;

        [SerializeField] private EvolutionKind _kind;
        [SerializeField] private string _canonAttackType;

        [SerializeField] private float _cooldown = 12f;
        [SerializeField] private float _damageCoef = 1f;
        [SerializeField] private int _hitCount = 1;
        [SerializeField] private int _projectileCount = 1;
        [SerializeField] private float _projectileSpeed = 14f;
        [SerializeField] private float _rangeMeters = 9f;
        [SerializeField] private float _radiusMeters;
        [SerializeField] private float _durationSeconds;
        [SerializeField] private int _pierce;
        [SerializeField] private int _bounce;
        [SerializeField] private float _homing;
        [SerializeField] private string _statusType;

        public string EvolutionId => _evolutionId;
        public string NameKr => _nameKr;
        public string NameEn => _nameEn;
        public string CardA => _cardA;
        public string CardB => _cardB;
        public bool Starter => _starter;
        public string MotifHost => _motifHost;
        public EvolutionKind Kind => _kind;
        public string CanonAttackType => _canonAttackType;
        public float Cooldown => Mathf.Max(1f, _cooldown);
        public float DamageCoef => _damageCoef;
        public int HitCount => Mathf.Max(1, _hitCount);
        public int ProjectileCount => Mathf.Max(1, _projectileCount);
        public float ProjectileSpeed => _projectileSpeed;
        public float RangeMeters => _rangeMeters;
        public float RadiusMeters => _radiusMeters;
        public float DurationSeconds => _durationSeconds;
        public int Pierce => _pierce;
        public int Bounce => _bounce;
        public float Homing => _homing;
        public string StatusType => _statusType;
    }
}
