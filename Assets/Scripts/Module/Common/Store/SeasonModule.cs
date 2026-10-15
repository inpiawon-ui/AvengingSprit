using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.Module.Common.Store
{
    public enum MissionPeriod { Daily, Weekly }

    public readonly struct MissionState
    {
        public readonly SeasonTable.Mission Def;
        public readonly int Progress;
        public readonly bool Claimed;
        public readonly bool Locked;   // 「전부 완료」 — 같은 주기의 다른 미션을 다 받기 전

        public MissionState(SeasonTable.Mission def, int progress, bool claimed, bool locked)
        {
            Def = def; Progress = progress; Claimed = claimed; Locked = locked;
        }

        public bool Done => Progress >= Def.Target;
        public bool Claimable => Done && !Claimed && !Locked;
    }

    /// <summary>
    /// 시즌 패스 · 미션 · 프로필 꾸미기(기획서 `AVSR_Content_Store.md` B-1 · B-5 · B-9).
    /// 표는 `SeasonTable`(pass_s1 · missions · profile.tsv). 판 안 이벤트는 카운터만 올리고 저장은 판 끝 · 받기에서.
    /// </summary>
    public interface ISeasonService
    {
        bool IsReady { get; }

        // ── 패스 ───────────────────────────────────────────
        string SeasonId { get; }
        TimeSpan UntilEnd { get; }
        int Xp { get; }
        int Tier { get; }              // 받을 수 있게 된 단계(0 ~ TierMax)
        int TierMax { get; }
        int XpInTier { get; }
        int XpPerTier { get; }
        bool HasPremium { get; }
        IReadOnlyList<SeasonTable.Tier> Tiers { get; }
        bool IsClaimed(int level, bool premium);
        bool CanClaim(int level, bool premium);
        bool TryClaim(int level, bool premium, out RewardResult result);
        bool TryClaimAll(out RewardResult result);
        bool HasClaimable { get; }
        void AddXp(int xp);
        void GrantPremium();

        // ── 미션 ───────────────────────────────────────────
        IReadOnlyList<MissionState> Missions(MissionPeriod period);
        bool TryClaimMission(string id, out RewardResult result);
        bool HasClaimableMission { get; }
        TimeSpan UntilDayReset { get; }
        TimeSpan UntilWeekReset { get; }

        // ── 프로필 ─────────────────────────────────────────
        IReadOnlyList<SeasonTable.ProfileItem> ProfileItems { get; }
        bool Owns(string key);
        void GrantProfile(string key);
        string Equipped(string type);   // frame · title · face
        bool TryEquip(string type, string key);
    }

    [Module(Layer = ModuleLayer.Game)]
    public sealed class SeasonModule : IModule
    {
        private SeasonService _service;
        private readonly List<IDisposable> _tokens = new();

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _service = new SeasonService();
            CoreModule.Register<ISeasonService>(_service);
            IsInitialized = true;
        }

        public void Initialize()
        {
            var bus = CoreModule.Get<IEventBus>();
            _service.Bind(bus);
            _tokens.Add(bus.Subscribe<UserDataReadyEvent>(_ => _service.Count("login", 1, null)));
            _tokens.Add(bus.Subscribe<RoomEnteredEvent>(e => { if (e.RoomIndex == 0) _service.Count("run_start", 1, null); }));
            _tokens.Add(bus.Subscribe<StageFinishedEvent>(e =>
            {
                if (e.IsCleared) { _service.Count("run_clear", 1, null); _service.Count("kill_boss", 1, null); }
                _service.Save();
            }));
            _tokens.Add(bus.Subscribe<PossessedEvent>(e => _service.Possessed(e.PossessedHostKey)));
            _tokens.Add(bus.Subscribe<EnemyKilledEvent>(e =>
            {
                if (e.WithAdvantage) _service.Count("kill_advantage", 1, null);
            }));
            _tokens.Add(bus.Subscribe<DailyShopChangedEvent>(e => { if (e.BoughtSlot >= 0) _service.Count("daily_shop_buy", 1, null); }));
            _tokens.Add(bus.Subscribe<HostStatUpgradedEvent>(_ => _service.Count("stat_upgrade", 1, null)));
            _tokens.Add(bus.Subscribe<ChestOpenedEvent>(_ => _service.Count("chest_open", 1, null)));
            _service.LoadAsync().Forget();   // fire-and-forget: 표가 오기 전엔 패스 화면이 빈 칸으로 그린다
        }

        public void Dispose()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
            CoreModule.Unregister<ISeasonService>();
            _service = null;
            IsInitialized = false;
        }
    }

    internal sealed class SeasonService : ISeasonService
    {
        private const string TableAddress = "TableData/SeasonTable";

        private SeasonTable _table;
        private IEventBus _bus;
        private readonly List<MissionState> _daily = new();
        private readonly List<MissionState> _weekly = new();

        public void Bind(IEventBus bus) => _bus = bus;

        public async UniTask LoadAsync()
        {
            try
            {
                _table = await CoreModule.Get<IResourceManager>().LoadAsync<SeasonTable>(TableAddress);
            }
            catch (Exception e)
            {
                Debug.LogError($"[시즌] 표를 못 읽었다: {e.Message}");
            }
            Changed();
        }

        private static IPlayerDataService Player => CoreModule.TryGet<IPlayerDataService>(out var p) && p.IsReady ? p : null;
        private static IResetClock Clock => CoreModule.TryGet<IResetClock>(out var c) ? c : null;
        private static int Today => Clock?.DayKey ?? 0;

        public bool IsReady => _table != null && Player != null;
        public string SeasonId => Player?.Config != null ? Player.Config.SeasonId : "s1";

        public TimeSpan UntilEnd
        {
            get
            {
                var cfg = Player?.Config;
                if (cfg == null || Clock == null) return TimeSpan.Zero;
                var start = ToDate(cfg.SeasonStartDay);
                var end = start.AddDays(cfg.SeasonDays);
                var today = ToDate(Today);
                var left = end - today - (TimeSpan.FromDays(1) - Clock.UntilNextDay);
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        // ── 패스 ───────────────────────────────────────────

        private StoreSaveData Data
        {
            get
            {
                var p = Player;
                if (p == null) return null;
                var d = p.Store;
                if (d.passSeason != SeasonId)
                {
                    // 시즌이 바뀌면 비운다 — 받은 테두리 · 칭호는 프로필에 남는다
                    d.passSeason = SeasonId;
                    d.passXp = 0;
                    d.passPremium = false;
                    d.passClaimedFree = 0;
                    d.passClaimedPremium = 0;
                }
                return d;
            }
        }

        public IReadOnlyList<SeasonTable.Tier> Tiers => _table != null ? _table.Tiers : Array.Empty<SeasonTable.Tier>();
        public int TierMax => Tiers.Count;
        public int XpPerTier => Tiers.Count > 0 ? Math.Max(1, Tiers[0].Xp) : 100;
        public int Xp => Data?.passXp ?? 0;
        public int Tier => Math.Min(TierMax, Xp / XpPerTier);
        public int XpInTier => Tier >= TierMax ? XpPerTier : Xp % XpPerTier;
        public bool HasPremium => Data?.passPremium ?? false;

        public bool IsClaimed(int level, bool premium)
        {
            var d = Data;
            if (d == null || level < 1 || level > 62) return false;
            long bit = 1L << level;
            return ((premium ? d.passClaimedPremium : d.passClaimedFree) & bit) != 0;
        }

        public bool CanClaim(int level, bool premium)
            => IsReady && level >= 1 && level <= Tier && (!premium || HasPremium) && !IsClaimed(level, premium);

        public bool HasClaimable
        {
            get
            {
                for (int l = 1; l <= Tier; l++)
                    if (CanClaim(l, false) || CanClaim(l, true)) return true;
                return false;
            }
        }

        public bool TryClaim(int level, bool premium, out RewardResult result)
        {
            result = null;
            if (!CanClaim(level, premium)) return false;
            var tier = Tiers[level - 1];
            var d = Data;
            if (premium) d.passClaimedPremium |= 1L << level;
            else d.passClaimedFree |= 1L << level;
            result = Grant(premium ? tier.Premium : tier.Free);
            Save();
            Changed();
            return true;
        }

        public bool TryClaimAll(out RewardResult result)
        {
            result = new RewardResult();
            bool any = false;
            for (int l = 1; l <= Tier; l++)
                for (int k = 0; k < 2; k++)
                {
                    bool premium = k == 1;
                    if (!CanClaim(l, premium)) continue;
                    var d = Data;
                    if (premium) d.passClaimedPremium |= 1L << l;
                    else d.passClaimedFree |= 1L << l;
                    var r = Grant(premium ? Tiers[l - 1].Premium : Tiers[l - 1].Free);
                    result.Lines.AddRange(r.Lines);
                    result.OpenedChests.AddRange(r.OpenedChests);
                    any = true;
                }
            if (any) { Save(); Changed(); }
            return any;
        }

        public void AddXp(int xp)
        {
            var d = Data;
            if (d == null || xp <= 0) return;
            d.passXp = Math.Min(d.passXp + xp, TierMax * XpPerTier);
            Changed();
        }

        public void GrantPremium()
        {
            var d = Data;
            if (d == null) return;
            d.passPremium = true;
            Changed();
        }

        /// <summary>보상 문자열 — 프로필 키는 여기서, 나머지는 상점 지급 길(`IStoreService.Grant`).</summary>
        private RewardResult Grant(string rewards)
        {
            var result = CoreModule.TryGet<IStoreService>(out var store) ? store.Grant(rewards) : new RewardResult();
            return result;
        }

        // ── 미션 ───────────────────────────────────────────

        private void Roll(StoreSaveData d)
        {
            var clock = Clock;
            if (clock == null || _table == null) return;
            int day = clock.DayKey, week = clock.WeekKey;
            var ms = _table.Missions;
            if (d.missionIds.Length != ms.Length)
            {
                var ids = new string[ms.Length];
                var prog = new int[ms.Length];
                var claimed = new bool[ms.Length];
                for (int i = 0; i < ms.Length; i++)
                {
                    ids[i] = ms[i].Id;
                    int old = Array.IndexOf(d.missionIds, ms[i].Id);
                    if (old >= 0 && old < d.missionProgress.Length) { prog[i] = d.missionProgress[old]; claimed[i] = d.missionClaimed[old]; }
                }
                d.missionIds = ids;
                d.missionProgress = prog;
                d.missionClaimed = claimed;
            }
            bool newDay = d.missionDay != day, newWeek = d.missionWeek != week;
            if (!newDay && !newWeek) return;
            for (int i = 0; i < ms.Length; i++)
            {
                bool daily = ms[i].Period == "daily";
                if (daily ? newDay : newWeek) { d.missionProgress[i] = 0; d.missionClaimed[i] = false; }
            }
            if (newWeek) d.hostsThisWeek = Array.Empty<string>();
            d.missionDay = day;
            d.missionWeek = week;
        }

        internal void Count(string type, int amount, string key)
        {
            var p = Player;
            var d = Data;
            if (p == null || d == null || _table == null) return;
            Roll(d);
            if (type == "login")
            {
                if (d.loginDay == Today) return;   // 그날 첫 접속만
                d.loginDay = Today;
            }
            var ms = _table.Missions;
            bool changed = false;
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i].Type != type || d.missionClaimed[i]) continue;
                if (d.missionProgress[i] >= ms[i].Target) continue;
                d.missionProgress[i] = Math.Min(ms[i].Target, d.missionProgress[i] + amount);
                changed = true;
            }
            if (changed) Changed();
        }

        internal void Possessed(string hostKey)
        {
            if (string.IsNullOrEmpty(hostKey) || hostKey == HostEntry.GhostKey) return;
            Count("possess", 1, hostKey);
            var d = Data;
            if (d == null || Array.IndexOf(d.hostsThisWeek, hostKey) >= 0) return;
            var list = new List<string>(d.hostsThisWeek) { hostKey };
            d.hostsThisWeek = list.ToArray();
            Count("distinct_hosts_possessed", 1, hostKey);
        }

        public IReadOnlyList<MissionState> Missions(MissionPeriod period)
        {
            var list = period == MissionPeriod.Daily ? _daily : _weekly;
            list.Clear();
            var d = Data;
            if (d == null || _table == null) return list;
            Roll(d);
            var ms = _table.Missions;
            string want = period == MissionPeriod.Daily ? "daily" : "weekly";
            string all = period == MissionPeriod.Daily ? "daily_all_done" : "weekly_all_done";
            int others = 0, claimedOthers = 0;
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i].Period != want || ms[i].Type == all) continue;
                others++;
                if (d.missionClaimed[i]) claimedOthers++;
            }
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i].Period != want) continue;
                bool isAll = ms[i].Type == all;
                int progress = isAll ? claimedOthers : d.missionProgress[i];
                list.Add(new MissionState(ms[i], progress, d.missionClaimed[i], isAll && claimedOthers < others));
            }
            return list;
        }

        public bool HasClaimableMission
        {
            get
            {
                foreach (var m in Missions(MissionPeriod.Daily)) if (m.Claimable) return true;
                foreach (var m in Missions(MissionPeriod.Weekly)) if (m.Claimable) return true;
                return false;
            }
        }

        public bool TryClaimMission(string id, out RewardResult result)
        {
            result = null;
            var d = Data;
            if (d == null || _table == null) return false;
            MissionState? found = null;
            foreach (var m in Missions(MissionPeriod.Daily)) if (m.Def.Id == id) found = m;
            foreach (var m in Missions(MissionPeriod.Weekly)) if (m.Def.Id == id) found = m;
            if (found == null || !found.Value.Claimable) return false;
            int i = Array.IndexOf(d.missionIds, id);
            d.missionClaimed[i] = true;
            AddXp(found.Value.Def.PassXp);
            result = Grant(found.Value.Def.Rewards);
            result.Lines.Add(new RewardLine(RewardKind.PassXp, null, found.Value.Def.PassXp));
            Save();
            Changed();
            return true;
        }

        public TimeSpan UntilDayReset => Clock?.UntilNextDay ?? TimeSpan.Zero;
        public TimeSpan UntilWeekReset => Clock?.UntilNextWeek ?? TimeSpan.Zero;

        // ── 프로필 ─────────────────────────────────────────

        public IReadOnlyList<SeasonTable.ProfileItem> ProfileItems
            => _table != null ? _table.Profile : Array.Empty<SeasonTable.ProfileItem>();

        public bool Owns(string key)
        {
            var p = Player;
            if (p == null || string.IsNullOrEmpty(key)) return false;
            foreach (var it in ProfileItems)
            {
                if (it.Key != key) continue;
                if (it.Source == "default") return true;
                if (it.Source.StartsWith("chapter_clear:") && int.TryParse(it.Source.Substring(14), out var ch))
                    return p.ClearedChapter >= ch;
                break;
            }
            return Array.IndexOf(p.Store.profileOwned, key) >= 0;
        }

        public void GrantProfile(string key)
        {
            var p = Player;
            if (p == null || string.IsNullOrEmpty(key) || Array.IndexOf(p.Store.profileOwned, key) >= 0) return;
            var list = new List<string>(p.Store.profileOwned) { key };
            p.Store.profileOwned = list.ToArray();
            Changed();
        }

        public string Equipped(string type)
        {
            var d = Player?.Store;
            if (d == null) return string.Empty;
            string v = type == "frame" ? d.profileFrame : type == "title" ? d.profileTitle : d.profileFace;
            if (!string.IsNullOrEmpty(v)) return v;
            return type == "frame" ? "frame_default" : type == "title" ? "title_none" : HostEntry.GhostKey;
        }

        public bool TryEquip(string type, string key)
        {
            var p = Player;
            if (p == null) return false;
            if (type == "face")
            {
                var h = p.GetHost(key);
                if (key != HostEntry.GhostKey && (h == null || !p.IsHostOwned(h))) return false;
                p.Store.profileFace = key;
            }
            else
            {
                if (!Owns(key)) return false;
                if (type == "frame") p.Store.profileFrame = key;
                else p.Store.profileTitle = key;
            }
            Save();
            Changed();
            return true;
        }

        internal void Save() => Player?.SaveAsync().Forget();   // fire-and-forget: 받은 값은 이미 들어갔다 — 저장만 맡긴다

        private void Changed() => _bus?.Publish(new SeasonChangedEvent());

        private static DateTime ToDate(int key)
            => key <= 0 ? DateTime.UtcNow.Date : new DateTime(key / 10000, key / 100 % 100, key % 100);
    }
}
