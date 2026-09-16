using System;
using UnityEngine;

namespace Game.Module.Common.Chest
{
    /// <summary>
    /// 상자 한 등급. 해제 시간 · 젬값 · 보상 범위를 전부 여기서 읽는다.
    ///
    /// ⚠ 숫자를 코드에 적지 않는다. 밸런스는 이 표만 고쳐서 돌린다(TBD-BAL).
    /// </summary>
    [Serializable]
    public sealed class ChestEntry
    {
        [SerializeField] private string _key = string.Empty;
        [SerializeField] private string _nameKey = string.Empty;   // 문자열 표 키
        [SerializeField] private string _sprite = string.Empty;    // 아틀라스 스프라이트 이름
        [SerializeField] private int _unlockSeconds = 3600;

        /// <summary>즉시 열기 젬값 = 남은 분 × 이 값 (올림, 최소 1).</summary>
        [SerializeField] private float _gemPerMinute = 3f;

        [SerializeField] private int _goldMin, _goldMax;
        [SerializeField] private int _coreMin, _coreMax;
        [SerializeField] private int _memoryMin, _memoryMax;
        [SerializeField] private int _gemMin, _gemMax;
        /// <summary>호스트 파편 개수. 어느 호스트 것인지는 열 때 가진 몸 중에서 고른다.</summary>
        [SerializeField] private int _shardMin, _shardMax;

        public string Key => _key;
        public string NameKey => _nameKey;
        public string Sprite => _sprite;
        public int UnlockSeconds => _unlockSeconds;
        public float GemPerMinute => _gemPerMinute;

        public int GoldMin => _goldMin;
        public int GoldMax => _goldMax;
        public int CoreMin => _coreMin;
        public int CoreMax => _coreMax;
        public int MemoryMin => _memoryMin;
        public int MemoryMax => _memoryMax;
        public int GemMin => _gemMin;
        public int GemMax => _gemMax;
        public int ShardMin => _shardMin;
        public int ShardMax => _shardMax;

        public ChestEntry() { }

        public ChestEntry(string key, string sprite, int unlockSeconds, float gemPerMinute,
                          (int min, int max) gold, (int min, int max) core,
                          (int min, int max) memory, (int min, int max) gem,
                          (int min, int max) shard)
        {
            _key = key;
            _nameKey = $"chest.{key}.name";
            _sprite = sprite;
            _unlockSeconds = unlockSeconds;
            _gemPerMinute = gemPerMinute;
            (_goldMin, _goldMax) = gold;
            (_coreMin, _coreMax) = core;
            (_memoryMin, _memoryMax) = memory;
            (_gemMin, _gemMax) = gem;
            (_shardMin, _shardMax) = shard;
        }
    }

    /// <summary>상자 표. 주소 `TableData/ChestTable`.</summary>
    [CreateAssetMenu(fileName = "ChestTable", menuName = "AVSR/Chest Table")]
    public sealed class ChestTable : ScriptableObject
    {
        [SerializeField] private ChestEntry[] _entries = Array.Empty<ChestEntry>();

        public ChestEntry[] Entries => _entries;

        public ChestEntry Find(string key)
        {
            if (string.IsNullOrEmpty(key) || _entries == null) return null;
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i] != null && _entries[i].Key == key) return _entries[i];
            return null;
        }

        public void Fill(ChestEntry[] entries) => _entries = entries;
    }
}
