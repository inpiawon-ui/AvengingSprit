using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common.Store;
using Game.Module.Events;
using Game.Module.InGame;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.Module.Common.Modes
{
    /// <summary>
    /// 망령의 탑 · 망령의 밤 진행 규칙 — 하루 · 주 넘김, 하루 입장 3번(이기든 지든 −1), 젬 추가 입장,
    /// 탑 첫 클리어 · 소탕, 밤 기록 · 처치 보상. 값은 `ModeTable`, 저장은 `UserData.modes`(v8).
    /// 기획서 AVSR_Content_Tower.md 3-1 · 4-3 · 4-4 / AVSR_Content_NightMode.md 3-2 · 4-3 · 4-4.
    /// </summary>
    public interface IModeProgress
    {
        ModeTable Table { get; }

        // ── 탑 ──
        int TowerWeekCleared { get; }
        int TowerLastWeekBest { get; }
        int TowerNextFloor { get; }
        int TowerEntries { get; }
        int TowerBoughtToday { get; }
        bool TowerSweptToday { get; }
        /// <summary>오늘 젬으로 한 번 더 살 때의 값. 상한이면 −1.</summary>
        int TowerBuyCost { get; }
        bool TryBuyTowerEntry();
        /// <summary>도전 버튼 — 입장 1번을 쓴다(지금 쓰고 저장한다 · 이기든 지든 돌려주지 않는다).</summary>
        bool TryUseTowerEntry();
        /// <summary>층을 깼다 — 이번 주 진행 · 첫 클리어 보상. 주가 넘어간 판이면 넣지 않는다.</summary>
        void RecordTowerClear(int floor, int startWeekKey, IReadOnlyList<string> fitHosts, List<string> outKeys, List<int> outCounts, out int gold);
        SweepStep SweepNow { get; }
        bool TrySweep(List<string> outKeys, List<int> outCounts, out int gold);

        // ── 밤 ──
        int NightEntries { get; }
        int NightBoughtToday { get; }
        int NightBuyCost { get; }
        bool TryBuyNightEntry();
        bool TryUseNightEntry();
        /// <summary>오늘 요일(0 = 월 … 6 = 일, 하루 경계 기준).</summary>
        int TodayIndex { get; }
        int NightBest(int dayIndex);
        /// <summary>밤 판 끝 — 기록 · 처치 보상. 돌려주는 값: 이전 최고.</summary>
        int RecordNight(int dayIndex, int kills, float seconds, bool cleared, IReadOnlyList<string> dayHosts, List<string> outKeys, List<int> outCounts);

        /// <summary>조건에 맞는 호스트(숨긴 몸 뺌).</summary>
        List<string> HostsWhere(Func<HostEntry, bool> match);
        bool IsTrained(string hostKey);

        /// <summary>하루 · 주 경계를 넘었으면 채운다. 화면이 열릴 때 부른다.</summary>
        void Refresh();
    }

    [Module(Layer = ModuleLayer.Game)]
    public sealed class ModeProgressModule : IModule
    {
        private ModeProgressService _service;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _service = new ModeProgressService();
            CoreModule.Register<IModeProgress>(_service);
            IsInitialized = true;
        }

        public void Initialize()
        {
            _service.LoadAsync().Forget();   // fire-and-forget: 표가 오기 전엔 입장 패널이 기본값으로 그린다
        }

        public void Dispose()
        {
            CoreModule.Unregister<IModeProgress>();
            _service = null;
            IsInitialized = false;
        }
    }

    internal sealed class ModeProgressService : IModeProgress
    {
        private const string TableAddress = "TableData/ModeTable";

        private ModeTable _table;
        private readonly System.Random _rng = new();

        public ModeTable Table => _table;

        private static IPlayerDataService Player => CoreModule.TryGet<IPlayerDataService>(out var p) ? p : null;
        private static ModeProgressData Data => Player?.Modes;

        public async UniTask LoadAsync()
        {
            try
            {
                _table = await CoreModule.Get<IResourceManager>().LoadAsync<ModeTable>(TableAddress);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[모드] 표를 못 읽었다({TableAddress}): {ex.Message}");
            }
            Refresh();
        }

        // ── 하루 · 주 ─────────────────────────────────────

        public void Refresh()
        {
            var d = Data;
            if (d == null || !CoreModule.TryGet<IResetClock>(out var clock)) return;
            int day = clock.DayKey, week = clock.WeekKey;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool changed = false;

            // 시각을 뒤로 돌렸으면 채우지 않는다(서버 전까지 앞으로 돌리는 것은 못 막는다 — TBD-SRV)
            bool rolledBack = d.lastRefillUtc > 0 && now + 60 < d.lastRefillUtc;

            if (d.towerWeekId != week && !rolledBack)
            {
                if (d.towerWeekId != 0) d.towerLastWeekBest = d.towerWeekCleared;
                d.towerWeekCleared = 0;
                d.towerWeekId = week;
                changed = true;
            }
            int free = _table != null ? _table.FreeEntriesPerDay : 3;
            if (d.towerDayId != day && !rolledBack)
            {
                d.towerDayId = day;
                d.towerEntries = free;            // 채운다 — 남은 것을 더하지 않는다
                d.towerBoughtToday = 0;
                d.towerSweptToday = false;
                changed = true;
            }
            if (d.nightDayId != day && !rolledBack)
            {
                d.nightDayId = day;
                d.nightEntries = free;
                d.nightBoughtToday = 0;
                changed = true;
            }
            if (changed)
            {
                d.lastRefillUtc = now;
                SaveAndPublish();
            }
        }

        public int TodayIndex
        {
            get
            {
                if (!CoreModule.TryGet<IResetClock>(out var clock)) return 0;
                int k = clock.DayKey;
                var date = new DateTime(k / 10000, k / 100 % 100, k % 100);
                return ((int)date.DayOfWeek + 6) % 7;   // 월 0 … 일 6
            }
        }

        // ── 탑 ──────────────────────────────────────────

        public int TowerWeekCleared => Data?.towerWeekCleared ?? 0;
        public int TowerLastWeekBest => Data?.towerLastWeekBest ?? 0;
        public int TowerNextFloor => TowerWeekCleared + 1;
        public int TowerEntries => Data?.towerEntries ?? 0;
        public int TowerBoughtToday => Data?.towerBoughtToday ?? 0;
        public bool TowerSweptToday => Data?.towerSweptToday ?? false;
        public int TowerBuyCost => _table != null ? _table.BuyGemAt(TowerBoughtToday) : -1;

        public bool TryBuyTowerEntry()
        {
            var d = Data;
            int cost = TowerBuyCost;
            if (d == null || cost < 0 || Player == null || !Player.TrySpendGem(cost)) return false;
            d.towerBoughtToday++;
            d.towerEntries++;
            SaveAndPublish();
            return true;
        }

        public bool TryUseTowerEntry()
        {
            Refresh();
            var d = Data;
            if (d == null || d.towerEntries <= 0) return false;
            d.towerEntries--;
            SaveAndPublish();
            return true;
        }

        public void RecordTowerClear(int floor, int startWeekKey, IReadOnlyList<string> fitHosts,
                                     List<string> outKeys, List<int> outCounts, out int gold)
        {
            gold = 0;
            outKeys?.Clear();
            outCounts?.Clear();
            var d = Data;
            if (d == null || _table == null) return;
            // 주가 넘어간 뒤 끝난 판의 층은 새 주에 넣지 않는다(기획서 3-9)
            if (startWeekKey != 0 && startWeekKey != d.towerWeekId) return;
            if (floor != d.towerWeekCleared + 1) return;   // 이번 주 처음 깨는 층만

            d.towerWeekCleared = floor;
            d.towerAllTimeBest = Mathf.Max(d.towerAllTimeBest, floor);
            var def = _table.FloorAt(floor);
            var reward = def != null && def.IsBoss ? _table.FirstClearBoss : _table.FirstClear;
            gold = reward.Gold;
            GiveShards(fitHosts, reward.Shards, outKeys, outCounts);
            if (gold > 0) Player.AddCurrency(gold, 0);
            SaveAndPublish();
        }

        public SweepStep SweepNow => _table != null ? _table.SweepFor(TowerWeekCleared) : null;

        public bool TrySweep(List<string> outKeys, List<int> outCounts, out int gold)
        {
            gold = 0;
            Refresh();
            var d = Data;
            var step = SweepNow;
            if (d == null || step == null || d.towerSweptToday) return false;
            d.towerSweptToday = true;
            gold = step.Gold;
            // 소탕 조각은 키운 몸 쪽에 무게 — 모든 플레이 가능한 몸에서
            GiveShards(HostsWhere(null), step.Shards, outKeys, outCounts);
            if (gold > 0) Player.AddCurrency(gold, 0);
            SaveAndPublish();
            return true;
        }

        // ── 밤 ──────────────────────────────────────────

        public int NightEntries => Data?.nightEntries ?? 0;
        public int NightBoughtToday => Data?.nightBoughtToday ?? 0;
        public int NightBuyCost => _table != null ? _table.BuyGemAt(NightBoughtToday) : -1;

        public bool TryBuyNightEntry()
        {
            var d = Data;
            int cost = NightBuyCost;
            if (d == null || cost < 0 || Player == null || !Player.TrySpendGem(cost)) return false;
            d.nightBoughtToday++;
            d.nightEntries++;
            SaveAndPublish();
            return true;
        }

        public bool TryUseNightEntry()
        {
            Refresh();
            var d = Data;
            if (d == null || d.nightEntries <= 0) return false;
            d.nightEntries--;
            SaveAndPublish();
            return true;
        }

        public int NightBest(int dayIndex)
        {
            var d = Data;
            return d == null || d.nightBestKills == null || dayIndex < 0 || dayIndex >= d.nightBestKills.Length
                ? 0 : d.nightBestKills[dayIndex];
        }

        public int RecordNight(int dayIndex, int kills, float seconds, bool cleared, IReadOnlyList<string> dayHosts,
                               List<string> outKeys, List<int> outCounts)
        {
            outKeys?.Clear();
            outCounts?.Clear();
            var d = Data;
            if (d == null) return 0;
            d.Normalize();
            dayIndex = Mathf.Clamp(dayIndex, 0, 6);
            int prev = d.nightBestKills[dayIndex];
            if (kills > prev) d.nightBestKills[dayIndex] = kills;
            d.nightBestTime[dayIndex] = Mathf.Max(d.nightBestTime[dayIndex], Mathf.FloorToInt(seconds));
            if (cleared) d.nightClears++;

            if (_table != null)
            {
                int shards = kills / _table.KillsPerShard + (cleared ? _table.ClearShardBonus : 0);
                GiveShards(dayHosts, shards, outKeys, outCounts);
            }
            SaveAndPublish();
            return prev;
        }

        // ── 공통 ────────────────────────────────────────

        public List<string> HostsWhere(Func<HostEntry, bool> match)
        {
            var list = new List<string>();
            var hosts = Player?.PlayableHosts;
            if (hosts == null) return list;
            for (int i = 0; i < hosts.Count; i++)
            {
                var h = hosts[i];
                if (h == null || h.IsGhost || PlayerDataService.IsHiddenHost(h.HostKey)) continue;
                if (match == null || match(h)) list.Add(h.HostKey);
            }
            return list;
        }

        public bool IsTrained(string hostKey) => Player != null && Player.GetMastery(hostKey) >= 1;

        /// <summary>조각 n 개를 목록 호스트에게 나눠 준다 — 키운 몸 쪽 무게 2배(기획서 4-4).</summary>
        private void GiveShards(IReadOnlyList<string> pool, int n, List<string> outKeys, List<int> outCounts)
        {
            if (pool == null || pool.Count == 0 || n <= 0 || Player == null) return;
            var got = new Dictionary<string, int>();
            int total = 0;
            for (int i = 0; i < pool.Count; i++) total += IsTrained(pool[i]) ? 2 : 1;
            for (int s = 0; s < n; s++)
            {
                int r = _rng.Next(total);
                for (int i = 0; i < pool.Count; i++)
                {
                    r -= IsTrained(pool[i]) ? 2 : 1;
                    if (r >= 0) continue;
                    got[pool[i]] = got.TryGetValue(pool[i], out int c) ? c + 1 : 1;
                    break;
                }
            }
            foreach (var kv in got)
            {
                Player.AddShards(kv.Key, kv.Value);
                outKeys?.Add(kv.Key);
                outCounts?.Add(kv.Value);
            }
        }

        private void SaveAndPublish()
        {
            var p = Player;
            if (p != null) p.SaveAsync().Forget();   // fire-and-forget: 진행은 바로 저장한다 — 판 중 앱이 꺼져도 남게
            if (CoreModule.TryGet<IEventBus>(out var bus))
                bus.Publish(new ModeProgressChangedEvent
                {
                    WeekClearedFloor = TowerWeekCleared,
                    TowerEntriesLeft = TowerEntries,
                    NightEntriesLeft = NightEntries,
                    IsSweptToday = TowerSweptToday,
                });
        }
    }
}
