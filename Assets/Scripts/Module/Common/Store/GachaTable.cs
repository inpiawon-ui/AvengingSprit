using System;
using UnityEngine;

namespace Game.Module.Common.Store
{
    /// <summary>
    /// 조각 뽑기 표(`Projects/AVSR/Store/gacha.tsv`) — `Tools/Game/로비 상점 표 임포트 (Store)` 가 같이 굽는다.
    /// 주소 `TableData/GachaTable`. 확률 공개(확률 보기)도 이 표를 그대로 계산해 보인다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Gacha Table", fileName = "GachaTable")]
    public sealed class GachaTable : ScriptableObject
    {
        [Serializable]
        public sealed class Setting
        {
            public string Key;
            public string Value;
        }

        [Serializable]
        public sealed class GradeRate
        {
            public string Key;
            public float Percent;
        }

        [Serializable]
        public sealed class AmountRate
        {
            public int Shards;
            public float Percent;
        }

        [SerializeField] private Setting[] _settings = Array.Empty<Setting>();
        [SerializeField] private GradeRate[] _grades = Array.Empty<GradeRate>();
        [SerializeField] private AmountRate[] _amounts = Array.Empty<AmountRate>();

        public Setting[] Settings => _settings;
        public GradeRate[] Grades => _grades;
        public AmountRate[] Amounts => _amounts;

#if UNITY_EDITOR
        public void Fill(Setting[] settings, GradeRate[] grades, AmountRate[] amounts)
        {
            _settings = settings;
            _grades = grades;
            _amounts = amounts;
        }
#endif
    }
}
