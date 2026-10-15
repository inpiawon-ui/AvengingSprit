using System;
using UnityEngine;

namespace Game.Module.Common.Store
{
    /// <summary>
    /// 시즌 패스 단계 · 미션 · 프로필 꾸미기 표(`Projects/AVSR/Store/pass_s1.tsv` · `missions.tsv` · `profile.tsv`).
    /// `Tools/Game/로비 상점 표 임포트 (Store)` 가 같이 굽는다. 주소 `TableData/SeasonTable`.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Season Table", fileName = "SeasonTable")]
    public sealed class SeasonTable : ScriptableObject
    {
        [Serializable]
        public sealed class Tier
        {
            public int Level;
            public int Xp;
            public string Free;
            public string Premium;
        }

        [Serializable]
        public sealed class Mission
        {
            public string Id;
            public string Period;   // daily · weekly
            public string Type;     // login · run_start · run_clear · possess · distinct_hosts_possessed · kill_advantage · kill_boss · daily_shop_buy · stat_upgrade · chest_open · daily_all_done · weekly_all_done
            public int Target;
            public int PassXp;
            public string Rewards;
        }

        [Serializable]
        public sealed class ProfileItem
        {
            public string Key;
            public string Type;     // frame · title
            public string Art;
            public string Source;   // default · pass_s1 · chapter_clear:N · soon
            public string NameKey;
            public int Order;
        }

        [SerializeField] private Tier[] _tiers = Array.Empty<Tier>();
        [SerializeField] private Mission[] _missions = Array.Empty<Mission>();
        [SerializeField] private ProfileItem[] _profile = Array.Empty<ProfileItem>();

        public Tier[] Tiers => _tiers;
        public Mission[] Missions => _missions;
        public ProfileItem[] Profile => _profile;

#if UNITY_EDITOR
        public void Fill(Tier[] tiers, Mission[] missions, ProfileItem[] profile)
        {
            _tiers = tiers;
            _missions = missions;
            _profile = profile;
        }
#endif
    }
}
