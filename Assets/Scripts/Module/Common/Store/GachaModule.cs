using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using UnityEngine;

namespace Game.Module.Common.Store
{
    public enum GachaPay { Free, Ticket, Gem }

    /// <summary>한 번 뽑은 것 — 몸 하나 + 조각 수. 다 키운 몸이면 조각 대신 정수(ToEssence).</summary>
    public readonly struct GachaPull
    {
        public readonly string HostKey;
        public readonly int Shards;
        public readonly bool Jackpot;
        public readonly bool Pickup;
        public readonly bool IsNew;
        public readonly int ToEssence;

        public GachaPull(string hostKey, int shards, bool jackpot, bool pickup, bool isNew, int toEssence)
        {
            HostKey = hostKey; Shards = shards; Jackpot = jackpot; Pickup = pickup; IsNew = isNew; ToEssence = toEssence;
        }
    }

    public readonly struct GachaRateLine
    {
        public readonly string HostKey;
        public readonly HostGrade Grade;
        public readonly float Percent;
        public readonly bool Pickup;

        public GachaRateLine(string hostKey, HostGrade grade, float percent, bool pickup)
        {
            HostKey = hostKey; Grade = grade; Percent = percent; Pickup = pickup;
        }
    }

    /// <summary>
    /// 조각 뽑기(기획서 `AVSR_Content_Store.md` B-11). 값 · 확률은 전부 `Projects/AVSR/Store/gacha.tsv`(GachaTable).
    /// 결과는 **값을 받고 · 지급하고 · 저장한 뒤** 돌려준다 — 연출 도중 꺼져도 결과는 이미 들어가 있다.
    /// </summary>
    public interface IGachaService
    {
        bool IsReady { get; }
        string PickupHost { get; }
        int PickupSharePct { get; }
        TimeSpan PickupLeft { get; }
        bool FreeAvailable { get; }
        int PityCount { get; }
        int PityMax { get; }
        int CostGem(int count);
        int CostTicket(int count);

        /// <summary>count 1 · 10. 값을 못 내면 null(이유는 failKey).</summary>
        UniTask<(IReadOnlyList<GachaPull> pulls, string failKey)> PullAsync(int count, GachaPay pay);

        /// <summary>확률 보기 — 표에서 계산한 몸별 %(합 100).</summary>
        IReadOnlyList<GachaRateLine> Rates();

        /// <summary>등급 · 조각 수 확률(%).</summary>
        IReadOnlyList<(string key, float pct)> GradeRates();
        IReadOnlyList<(int shards, float pct)> AmountRates();

        /// <summary>최근 기록(새것부터) — 「yyyyMMdd,hostKey,shards,MMddHHmm,pay,jackpot」(옛 기록은 앞 셋만).</summary>
        IReadOnlyList<string> History();
    }

    [Module(Layer = ModuleLayer.Game)]
    public sealed class GachaModule : IModule
    {
        private GachaService _service;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _service = new GachaService();
            CoreModule.Register<IGachaService>(_service);
            IsInitialized = true;
        }

        public void Initialize()
        {
            _service.Bind(CoreModule.Get<IEventBus>());
            _service.LoadAsync().Forget();   // fire-and-forget: 표가 오기 전엔 소환 화면이 값을 비워 그린다
        }

        public void Dispose()
        {
            CoreModule.Unregister<IGachaService>();
            _service = null;
            IsInitialized = false;
        }
    }

    internal sealed class GachaService : IGachaService
    {
        private const string TableAddress = "TableData/GachaTable";
        private const int HistoryMax = 100;

        private GachaTable _table;
        private IEventBus _bus;
        private readonly System.Random _rng = new(Environment.TickCount);
        private readonly List<GachaRateLine> _rates = new();
        private readonly List<string> _hist = new();

        public void Bind(IEventBus bus) => _bus = bus;

        public async UniTask LoadAsync()
        {
            try
            {
                _table = await CoreModule.Get<GameFramework.Core.Module.Resource.IResourceManager>()
                    .LoadAsync<GachaTable>(TableAddress);
            }
            catch (Exception e)
            {
                Debug.LogError($"[소환] 표를 못 읽었다: {e.Message}");
            }
            _bus?.Publish(new StoreChangedEvent());
        }

        private static IPlayerDataService Player => CoreModule.TryGet<IPlayerDataService>(out var p) && p.IsReady ? p : null;
        private static int Today => CoreModule.TryGet<IResetClock>(out var c) ? c.DayKey : 0;

        public bool IsReady => _table != null && Player != null;
        public string PickupHost => S("pickup_host", string.Empty);
        public int PickupSharePct => I("pickup_share", 50);
        public TimeSpan PickupLeft => CoreModule.TryGet<IResetClock>(out var c) ? c.UntilNextWeek : TimeSpan.Zero;
        public int PityMax => I("pity_count", 50);
        public int PityCount => Player?.Store.gachaPity ?? 0;

        public bool FreeAvailable
        {
            get
            {
                var p = Player;
                return p != null && I("free_per_day", 1) > 0 && p.Store.gachaFreeDay != Today;
            }
        }

        public int CostGem(int count) => count >= 10 ? I("cost_gem_10", 540) : I("cost_gem_1", 60);
        public int CostTicket(int count) => count >= 10 ? I("cost_ticket_10", 10) : I("cost_ticket_1", 1);

        public async UniTask<(IReadOnlyList<GachaPull>, string)> PullAsync(int count, GachaPay pay)
        {
            var p = Player;
            if (p == null || _table == null) return (null, "ui.summon.fail.notready");
            count = count >= 10 ? 10 : 1;
            switch (pay)
            {
                case GachaPay.Free:
                    if (!FreeAvailable || count != 1) return (null, "ui.summon.fail.free");
                    p.Store.gachaFreeDay = Today;
                    break;
                case GachaPay.Ticket:
                    if (!p.TrySpendTickets(CostTicket(count))) return (null, "ui.summon.fail.ticket");
                    break;
                default:
                    if (!p.TrySpendGem(CostGem(count))) return (null, "ui.summon.fail.gem");
                    break;
            }

            var pool = Pool(p);
            var list = new List<GachaPull>(count);
            bool hadA = false;
            for (int i = 0; i < count; i++)
            {
                bool forceA = count == 10 && i == count - 1 && !hadA && Grade("A") > 0;
                var pull = Roll(p, pool, forceA);
                if (Grade(p.GetHost(pull.HostKey)) == HostGrade.A) hadA = true;
                list.Add(pull);
            }
            for (int i = 0; i < list.Count; i++) Remember(p, list[i], pay);
            await p.SaveAsync();
            _bus?.Publish(new StoreChangedEvent());
            return (list, null);
        }

        private GachaPull Roll(IPlayerDataService p, List<HostEntry> pool, bool forceA)
        {
            // 등급 → 그 등급 안에서 같은 확률. A 면 픽업 몫
            string pickup = PickupHost;
            HostGrade grade = forceA ? HostGrade.A : (_rng.Next(0, 100) < Grade("B") ? HostGrade.B : HostGrade.A);
            HostEntry host = null;
            bool isPickup = false;
            if (grade == HostGrade.A && !string.IsNullOrEmpty(pickup) && _rng.Next(0, 100) < PickupSharePct)
            {
                host = p.GetHost(pickup);
                isPickup = host != null;
            }
            if (host == null)
            {
                var same = pool.FindAll(h => h.Grade == grade && h.HostKey != pickup);
                if (same.Count == 0) same = pool;
                host = same[_rng.Next(0, same.Count)];
            }

            // 조각 수 — 천장이면 대박 확정
            var d = p.Store;
            int shards;
            bool jackpot;
            if (d.gachaPity + 1 >= PityMax) { shards = JackpotAmount(); jackpot = true; }
            else
            {
                shards = RollAmount();
                jackpot = shards >= JackpotAmount();
            }
            d.gachaPity = jackpot ? 0 : d.gachaPity + 1;
            d.gachaTotal++;

            bool isNew = !p.IsHostOwned(host);
            // 다 키운 몸의 조각은 정수로 바뀐다(AddShards 의 규칙) — 결과에 보이려고 바뀌기 전 값을 잰다
            int before = p.Essence;
            p.AddShards(host.HostKey, shards);
            int toEssence = Math.Max(0, p.Essence - before);
            return new GachaPull(host.HostKey, shards, jackpot, isPickup, isNew, toEssence);
        }

        private void Remember(IPlayerDataService p, GachaPull pull, GachaPay pay)
        {
            var d = p.Store;
            // 시각은 기기 시계(화면에 「10.15 21:34」) — 하루 경계는 Today(리셋 시계)가 맡는다
            string at = DateTime.Now.ToString("MMddHHmm");
            var list = new List<string>(d.gachaHist) { $"{Today},{pull.HostKey},{pull.Shards},{at},{pay},{(pull.Jackpot ? 1 : 0)}" };
            if (list.Count > HistoryMax) list.RemoveRange(0, list.Count - HistoryMax);
            d.gachaHist = list.ToArray();
        }

        private static List<HostEntry> Pool(IPlayerDataService p)
        {
            var pool = new List<HostEntry>();
            var all = p.PlayableHosts;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e == null || e.IsGhost || PlayerDataService.IsHiddenHost(e.HostKey) || e.Grade == HostGrade.S) continue;
                pool.Add(e);
            }
            return pool;
        }

        private int RollAmount()
        {
            int r = _rng.Next(0, 100), acc = 0;
            var amounts = _table.Amounts;
            for (int i = 0; i < amounts.Length; i++)
            {
                acc += Mathf.RoundToInt(amounts[i].Percent);
                if (r < acc) return amounts[i].Shards;
            }
            return amounts.Length > 0 ? amounts[0].Shards : 2;
        }

        private int JackpotAmount()
        {
            int max = 0;
            foreach (var a in _table.Amounts) max = Math.Max(max, a.Shards);
            return max;
        }

        private int Grade(string key)
        {
            foreach (var g in _table.Grades) if (g.Key == key) return Mathf.RoundToInt(g.Percent);
            return 0;
        }

        private static HostGrade Grade(HostEntry e) => e != null ? e.Grade : HostGrade.B;

        public IReadOnlyList<GachaRateLine> Rates()
        {
            _rates.Clear();
            var p = Player;
            if (p == null || _table == null) return _rates;
            var pool = Pool(p);
            string pickup = PickupHost;
            int nb = pool.FindAll(h => h.Grade == HostGrade.B).Count;
            int na = pool.FindAll(h => h.Grade == HostGrade.A && h.HostKey != pickup).Count;
            bool hasPickup = pool.Exists(h => h.HostKey == pickup);
            float a = Grade("A"), b = Grade("B");
            float pickPct = hasPickup ? a * PickupSharePct / 100f : 0f;
            foreach (var h in pool)
            {
                float pct = h.Grade == HostGrade.B ? (nb > 0 ? b / nb : 0f)
                          : h.HostKey == pickup ? pickPct
                          : na > 0 ? (a - pickPct) / na : 0f;
                _rates.Add(new GachaRateLine(h.HostKey, h.Grade, pct, h.HostKey == pickup));
            }
            _rates.Sort((x, y) => y.Percent.CompareTo(x.Percent));
            return _rates;
        }

        public IReadOnlyList<(string, float)> GradeRates()
        {
            var list = new List<(string, float)>();
            if (_table != null) foreach (var g in _table.Grades) list.Add((g.Key, g.Percent));
            return list;
        }

        public IReadOnlyList<(int, float)> AmountRates()
        {
            var list = new List<(int, float)>();
            if (_table != null) foreach (var a in _table.Amounts) list.Add((a.Shards, a.Percent));
            return list;
        }

        public IReadOnlyList<string> History()
        {
            _hist.Clear();
            var p = Player;
            if (p == null) return _hist;
            var h = p.Store.gachaHist;
            for (int i = h.Length - 1; i >= 0; i--) _hist.Add(h[i]);
            return _hist;
        }

        private string S(string key, string fallback)
        {
            if (_table == null) return fallback;
            foreach (var s in _table.Settings) if (s.Key == key) return s.Value;
            return fallback;
        }

        private int I(string key, int fallback)
            => int.TryParse(S(key, null), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }
}
