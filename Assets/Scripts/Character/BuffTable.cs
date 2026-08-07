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

        public string BuffKey => _buffKey;
        public string NameKr => _nameKr;
        public string Description => _description;
        public BuffKind Kind => _kind;
        public int Value => _value;
        public bool Stackable => _stackable;
        public string ColorHex => _colorHex;
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

        /// <summary>
        /// 서로 다른 버프 `count` 개를 뽑는다. `taken` 은 이미 획득해 중복 불가인 키다.
        /// 뽑을 것이 모자라면 있는 만큼만 돌려준다.
        /// </summary>
        public void Draw(List<BuffEntry> into, int count, HashSet<string> exclude, System.Random rng)
        {
            into.Clear();
            var pool = new List<BuffEntry>(_entries.Length);
            for (int i = 0; i < _entries.Length; i++)
                if (exclude == null || !exclude.Contains(_entries[i].BuffKey)) pool.Add(_entries[i]);

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int k = rng.Next(pool.Count);
                into.Add(pool[k]);
                pool.RemoveAt(k);
            }
        }
    }
}
