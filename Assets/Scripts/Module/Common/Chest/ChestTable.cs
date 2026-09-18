using System;
using UnityEngine;

namespace Game.Module.Common.Chest
{
    /// <summary>
    /// 상자 한 등급. 해제 시간 · 젬값 · 보상 범위를 전부 여기서 읽는다.
    ///
    /// 보상은 **골드와 호스트 조각 두 가지뿐**이다(기획 2026-09-18). 조각은 호스트 여러 명에게
    /// 나눠 들어가고, 등급이 높은 상자일수록 여러 명 · 많이 · 높은 등급(S·A) 호스트가 나온다.
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

        /// <summary>조각 총 개수. 뽑힌 호스트들에게 나눠 준다.</summary>
        [SerializeField] private int _shardMin, _shardMax;

        /// <summary>조각을 받는 호스트 수(종류).</summary>
        [SerializeField] private int _hostKindsMin = 1, _hostKindsMax = 1;

        /// <summary>호스트 등급 B · A · S 가 뽑힐 무게. 0 이면 그 등급은 안 나온다.</summary>
        [SerializeField] private int _weightB = 1, _weightA, _weightS;

        public string Key => _key;
        public string NameKey => _nameKey;
        public string Sprite => _sprite;
        public int UnlockSeconds => _unlockSeconds;
        public float GemPerMinute => _gemPerMinute;

        public int GoldMin => _goldMin;
        public int GoldMax => _goldMax;
        public int ShardMin => _shardMin;
        public int ShardMax => _shardMax;
        public int HostKindsMin => _hostKindsMin;
        public int HostKindsMax => _hostKindsMax;
        public int WeightB => _weightB;
        public int WeightA => _weightA;
        public int WeightS => _weightS;

        public ChestEntry() { }

        public ChestEntry(string key, string sprite, int unlockSeconds, float gemPerMinute,
                          (int min, int max) gold, (int min, int max) shard, (int min, int max) hostKinds,
                          (int b, int a, int s) gradeWeight)
        {
            _key = key;
            _nameKey = $"chest.{key}.name";
            _sprite = sprite;
            _unlockSeconds = unlockSeconds;
            _gemPerMinute = gemPerMinute;
            (_goldMin, _goldMax) = gold;
            (_shardMin, _shardMax) = shard;
            (_hostKindsMin, _hostKindsMax) = hostKinds;
            (_weightB, _weightA, _weightS) = gradeWeight;
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
