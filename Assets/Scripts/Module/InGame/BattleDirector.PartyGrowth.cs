using System.Collections.Generic;
using Game.User;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 파티 육성 연결(PD 2026-10-29 · 정본 `Projects/AVSR/_exchange/plan_party_growth.md`).
    ///
    /// - 스킬 레벨 배율 — 액티브(레벨당 +6%) · 필살기(레벨당 +8% · Lv5 강화형) · 액티브 Lv10 인연형 한 단계 더.
    ///   레벨 자체는 `HostMastery`(= 액티브 스킬 레벨) · `IPlayerDataService.SkillLevel` 이 준다.
    /// - 판 끝 조각 — 이 판에 메인 · 보조로 있던 호스트마다 +1(클리어 +2) · 인연을 처음 켠 짝 둘 다 +2(한 번만).
    /// - 인연 세기 — 두 호스트 성급 중 낮은 쪽으로 1 · 2 · 3단. 인연 동작 쪽이 이 값을 곱한다.
    /// ⚠ `BattleDirector.Party*.cs` 는 읽기만 한다 — 파티 상태(`_host` · `_subs` · `_inyeonKey`)를 여기서 꺼내 쓴다.
    /// 수치는 전부 `GameConfig` 「육성」 칸(임시값).
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 스킬 레벨 배율 ──────────────────────────────────────

        /// <summary>액티브 위력 배율 — 액티브 스킬 레벨 하나당 +6%(Lv1 = 1).</summary>
        private float SkillLvActivePowerMul
            => 1f + (_config != null ? _config.ActivePowerPerLevel : 0.06f) * Mathf.Max(0, HostMastery - 1);

        /// <summary>지금 메인의 필살기 스킬 레벨(1~5). 못 가진 몸은 1.</summary>
        private int UltSkillLevel
            => _player != null && _host != null ? _player.SkillLevel(_host.Key, HostSkillSlot.Ult) : 1;

        /// <summary>필살기가 강화형인가(필살기 Lv5).</summary>
        public bool UltEnhanced => _config != null && UltSkillLevel >= _config.UltEnhancedLevel;

        /// <summary>필살기 위력 배율 — 레벨 하나당 +8% · 강화형이면 한 번 더 곱한다. 등급 배율은 부르는 쪽이 곱한다.</summary>
        private float SkillLvUltPowerMul
            => (1f + (_config != null ? _config.UltPowerPerLevel : 0.08f) * Mathf.Max(0, UltSkillLevel - 1))
               * (UltEnhanced ? _config.UltEnhancedMul : 1f);

        /// <summary>
        /// 액티브 Lv10 인연형 한 단계 더 — 인연이 켜진 메인의 인연형 수치에 곱하는 배율. Lv10 미만이면 1.
        /// 인연 동작 쪽이 쓴다(값만 배율로).
        /// </summary>
        public float ActiveInyeonBoostMul
            => _config != null && _host != null && HostMastery >= _config.ActiveInyeonLevel ? _config.ActiveInyeonMul : 1f;

        // ── 인연 세기 ───────────────────────────────────────────

        /// <summary>두 호스트 성급 중 낮은 쪽 단계 → 인연 세기 1 · 2 · 3단(0~3 · 4~7 · 8~10). 못 가진 몸은 성급 0.</summary>
        public int InyeonTier(string hostA, string hostB)
        {
            if (_player == null || _config == null) return 1;
            int low = Mathf.Min(_player.GetMastery(hostA), _player.GetMastery(hostB));
            return _config.InyeonTierOf(low);
        }

        /// <summary>지금 켜진 인연의 세기(메인과 보조 중 첫 짝). 인연이 꺼져 있으면 0.</summary>
        public int InyeonActiveTier
        {
            get
            {
                if (_inyeonKey == null || _host == null) return 0;
                for (int i = 0; i < _subs.Count; i++)
                    if (_subs[i].U != null && IsMainPartner(_subs[i].U.Key)) return InyeonTier(_host.Key, _subs[i].U.Key);
                return 0;
            }
        }

        // ── 이 판에 파티로 있던 호스트 · 켠 인연 ─────────────────

        private readonly List<string> _partyRunHosts = new();
        // 켠 인연 짝 — 같은 첨자가 한 짝(메인 · 짝). 매 프레임 문자열을 만들지 않게 나란한 목록으로 둔다
        private readonly List<string> _inyeonRunMains = new();
        private readonly List<string> _inyeonRunPartners = new();

        /// <summary>판 공개 창구 — 이 판에 메인 · 보조로 있던 호스트(들어온 순서).</summary>
        public IReadOnlyList<string> PartyRunHosts => _partyRunHosts;

        private void ClearPartyGrowth()
        {
            _partyRunHosts.Clear();
            _inyeonRunMains.Clear();
            _inyeonRunPartners.Clear();
        }

        /// <summary>매 프레임 — 지금 파티를 적어 둔다. 몇 칸 안 되는 목록이라 Contains 로 충분하다(할당 없음).</summary>
        private void NotePartyGrowth()
        {
            if (!PartyOn) return;
            if (_host != null) NoteRunHost(_host.Key);
            for (int i = 0; i < _subs.Count; i++)
            {
                var u = _subs[i].U;
                if (u == null) continue;
                NoteRunHost(u.Key);
                if (_inyeonKey != null && _host != null && IsMainPartner(u.Key)) NoteInyeon(_host.Key, u.Key);
            }
        }

        private void NoteRunHost(string key)
        {
            if (!string.IsNullOrEmpty(key) && !_partyRunHosts.Contains(key)) _partyRunHosts.Add(key);
        }

        private void NoteInyeon(string main, string partner)
        {
            for (int i = 0; i < _inyeonRunMains.Count; i++)
                if (_inyeonRunMains[i] == main && _inyeonRunPartners[i] == partner) return;
            _inyeonRunMains.Add(main);
            _inyeonRunPartners.Add(partner);
        }

        // ── 판 끝 조각 ──────────────────────────────────────────

        /// <summary>
        /// 판 끝에 한 번 — 파티로 있던 호스트마다 조각(진 판 +1 · 깬 판 +2) · 처음 켠 인연 짝 둘 다 +2(저장에 기록, 한 번만).
        /// 결과 창 조각 줄에도 합친다. 저장은 뒤따르는 판 끝 지급(`GrantRunGoldAsync` · `GrantChapterClearAsync`)이 한다.
        /// </summary>
        private void GrantPartyGrowthShards(bool cleared)
        {
            if (_player == null || !_player.IsReady || _config == null) return;
            int each = cleared ? _config.PartyClearShards : _config.PartyRunShards;
            for (int i = 0; i < _partyRunHosts.Count; i++) GrantGrowthShard(_partyRunHosts[i], each);
            int bonus = _config.InyeonFirstShards;
            for (int i = 0; i < _inyeonRunMains.Count; i++)
            {
                if (!_player.TryClaimInyeon(_inyeonRunMains[i], _inyeonRunPartners[i])) continue;
                GrantGrowthShard(_inyeonRunMains[i], bonus);
                GrantGrowthShard(_inyeonRunPartners[i], bonus);
            }
            ClearPartyGrowth();   // 두 번 불려도(포기 뒤 끝) 한 번만 준다
        }

        private void GrantGrowthShard(string key, int n)
        {
            if (n <= 0 || string.IsNullOrEmpty(key) || key == Game.Character.HostEntry.GhostKey) return;
            if (_player.GetHost(key) == null) return;
            // 히든(사신)은 그 판 체험뿐 — 조각을 주면 얻는 길이 생긴다(PD 2026-10-23)
            if (key == PlayerDataService.HiddenTrialHostKey) return;
            _player.AddShards(key, n);
            int at = _runShardKeys.IndexOf(key);
            if (at < 0) { _runShardKeys.Add(key); _runShardCounts.Add(n); }
            else _runShardCounts[at] += n;
        }
    }
}
