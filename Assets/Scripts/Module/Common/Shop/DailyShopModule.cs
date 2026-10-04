using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common.Chest;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using UnityEngine;

namespace Game.Module.Common.Shop
{
    /// <summary>일일 상점 칸의 종류. **순서가 저장값이다** — 바꾸지 말고 뒤에 더한다.</summary>
    public enum DailyShopKind
    {
        /// <summary>무료 선물(골드). 하루 한 번.</summary>
        FreeGold,
        /// <summary>가진 몸의 조각(골드).</summary>
        OwnedShard,
        /// <summary>아직 없는 몸의 조각(골드) — 해금으로 가는 길.</summary>
        LockedShard,
        /// <summary>아무 몸의 조각(골드).</summary>
        AnyShard,
        /// <summary>상자(젬).</summary>
        Chest,
    }

    /// <summary>진열 한 칸. 화면은 이것만 보고 그린다.</summary>
    public readonly struct DailyShopSlot
    {
        public readonly DailyShopKind Kind;
        /// <summary>조각 칸이면 호스트 키, 상자 칸이면 상자 키, 무료 선물이면 빈 값.</summary>
        public readonly string Key;
        /// <summary>조각 수 · 골드 양 · 상자 1.</summary>
        public readonly int Count;
        /// <summary>값. 무료면 0.</summary>
        public readonly int Price;
        public readonly bool PaidWithGem;
        public readonly bool Bought;

        public DailyShopSlot(DailyShopKind kind, string key, int count, int price, bool gem, bool bought)
        {
            Kind = kind; Key = key; Count = count; Price = price; PaidWithGem = gem; Bought = bought;
        }

        public bool IsShard => Kind == DailyShopKind.OwnedShard || Kind == DailyShopKind.LockedShard
                            || Kind == DailyShopKind.AnyShard;
    }

    /// <summary>
    /// 일일 상점 — 하루에 한 번 진열이 바뀐다(기기 날짜 기준). 기획서 `Projects/AVSR/AVSR_Content_Economy.md`.
    /// </summary>
    public interface IDailyShopService
    {
        int SlotCount { get; }

        /// <summary>그 칸. 날이 바뀌었으면 먼저 다시 짠다.</summary>
        DailyShopSlot Get(int slot);

        /// <summary>산다. 실패하면 이유를 문자열 키로 돌려준다(`ui.shop.daily.fail.*`).</summary>
        bool TryBuy(int slot, out string failKey);

        /// <summary>다음 새로고침의 젬 값. 오늘 더 못 하면 0.</summary>
        int RefreshGemCost { get; }

        /// <summary>젬을 내고 진열을 다시 짠다. 산 칸도 다시 열린다.</summary>
        bool TryRefresh(out string failKey);

        /// <summary>다음 날까지 남은 시간.</summary>
        TimeSpan UntilReset { get; }
    }

    [Module(Layer = ModuleLayer.Game)]
    public sealed class DailyShopModule : IModule
    {
        private DailyShopService _service;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _service = new DailyShopService();
            CoreModule.Register<IDailyShopService>(_service);
            IsInitialized = true;
        }

        public void Initialize() => _service.Bind(CoreModule.Get<IEventBus>());

        public void Dispose()
        {
            CoreModule.Unregister<IDailyShopService>();
            _service = null;
            IsInitialized = false;
        }
    }

    internal sealed class DailyShopService : IDailyShopService
    {
        // 칸 여섯 — 무료 선물 · 가진 몸 둘 · 없는 몸 하나 · 아무 몸 하나 · 상자
        private static readonly DailyShopKind[] Layout =
        {
            DailyShopKind.FreeGold, DailyShopKind.OwnedShard, DailyShopKind.OwnedShard,
            DailyShopKind.LockedShard, DailyShopKind.AnyShard, DailyShopKind.Chest,
        };

        private IEventBus _bus;
        private IPlayerDataService _player;
        private readonly List<HostEntry> _pool = new();
        private readonly HashSet<string> _used = new();

        public int SlotCount => Layout.Length;

        public void Bind(IEventBus bus) => _bus = bus;

        private IPlayerDataService Player
        {
            get
            {
                if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
                return _player;
            }
        }

        private static int Today => int.Parse(DateTime.Now.ToString("yyyyMMdd"));

        public TimeSpan UntilReset => DateTime.Today.AddDays(1) - DateTime.Now;

        public DailyShopSlot Get(int slot)
        {
            var d = Ready();
            if (d == null || slot < 0 || slot >= d.kinds.Length) return default;
            bool gem = d.kinds[slot] == (int)DailyShopKind.Chest;
            return new DailyShopSlot((DailyShopKind)d.kinds[slot], d.keys[slot], d.counts[slot], d.prices[slot],
                                     gem, (d.bought & (1 << slot)) != 0);
        }

        public int RefreshGemCost
        {
            get
            {
                var d = Ready();
                var c = Player?.Config;
                return d == null || c == null ? 0 : c.ShopRefreshGems(d.refreshes);
            }
        }

        public bool TryBuy(int slot, out string failKey)
        {
            failKey = null;
            var p = Player;
            var s = Get(slot);
            if (p == null || !p.IsReady || s.Kind == default && s.Key == null && s.Count == 0)
            { failKey = "ui.shop.daily.fail.unknown"; return false; }
            if (s.Bought) { failKey = "ui.shop.daily.fail.bought"; return false; }

            // 상자는 칸이 없으면 **값을 받기 전에** 막는다 — 젬만 나가고 상자가 안 들어오면 안 된다
            IChestService chests = null;
            if (s.Kind == DailyShopKind.Chest && !HasFreeChestSlot(out chests))
            { failKey = "ui.shop.daily.fail.chest_full"; return false; }

            bool paid = s.Price <= 0 || (s.PaidWithGem ? p.TrySpendGem(s.Price) : p.TrySpendGold(s.Price));
            if (!paid) { failKey = s.PaidWithGem ? "ui.shop.daily.fail.gem" : "ui.shop.daily.fail.gold"; return false; }

            switch (s.Kind)
            {
                case DailyShopKind.FreeGold: p.AddCurrency(s.Count, 0); break;
                case DailyShopKind.Chest: chests.TryGrant(s.Key, out _); break;
                default: p.AddShards(s.Key, s.Count); break;
            }
            p.DailyShop.bought |= 1 << slot;
            p.SaveAsync().Forget();   // fire-and-forget: 산 것은 바로 남긴다
            _bus?.Publish(new DailyShopChangedEvent { BoughtSlot = slot });
            return true;
        }

        public bool TryRefresh(out string failKey)
        {
            failKey = null;
            var p = Player;
            var d = Ready();
            if (p == null || d == null) { failKey = "ui.shop.daily.fail.unknown"; return false; }
            int cost = RefreshGemCost;
            if (cost <= 0) { failKey = "ui.shop.daily.fail.refresh_limit"; return false; }
            if (!p.TrySpendGem(cost)) { failKey = "ui.shop.daily.fail.gem"; return false; }
            d.refreshes++;
            Roll(p, d, d.day, d.refreshes);
            p.SaveAsync().Forget();   // fire-and-forget: 낸 젬과 새 진열을 같이 남긴다
            _bus?.Publish(new DailyShopChangedEvent { BoughtSlot = -1 });
            return true;
        }

        private static bool HasFreeChestSlot(out IChestService chests)
        {
            if (!CoreModule.TryGet<IChestService>(out chests)) return false;
            for (int i = 0; i < chests.SlotCount; i++)
                if (chests.Get(i).IsEmpty) return true;
            return false;
        }

        // ── 진열 짜기 ────────────────────────────────────────────

        /// <summary>오늘 진열을 돌려준다. 날이 바뀌었거나 비어 있으면 새로 짠다.</summary>
        private DailyShopData Ready()
        {
            var p = Player;
            if (p == null || !p.IsReady) return null;
            var d = p.DailyShop;
            int today = Today;
            if (d.day != today || d.kinds == null || d.kinds.Length != Layout.Length)
            {
                d.day = today;
                d.refreshes = 0;
                Roll(p, d, today, 0);
                p.SaveAsync().Forget();   // fire-and-forget: 짠 진열을 남겨야 다시 켜도 같다
                _bus?.Publish(new DailyShopChangedEvent { BoughtSlot = -1 });
            }
            return d;
        }

        private void Roll(IPlayerDataService p, DailyShopData d, int day, int refreshes)
        {
            var c = p.Config;
            int n = Layout.Length;
            d.bought = 0;
            d.kinds = new int[n];
            d.keys = new string[n];
            d.counts = new int[n];
            d.prices = new int[n];
            _used.Clear();

            // 값은 **열린 가장 높은 챕터**의 값 배율을 따른다 — 판에서 버는 골드와 같은 비율로 오른다.
            float mul = 1f;
            if (c != null)
            {
                float m = c.ChapterOf(p.UnlockedChapter).PriceMul;
                if (m > 0f) mul = m;
            }

            var rng = new System.Random(day * 397 + refreshes * 7919);
            for (int i = 0; i < n; i++)
            {
                var kind = Layout[i];
                d.kinds[i] = (int)kind;
                switch (kind)
                {
                    case DailyShopKind.FreeGold:
                        d.keys[i] = string.Empty;
                        d.counts[i] = Mathf.RoundToInt((c != null ? c.ShopFreeGold : 100) * mul);
                        d.prices[i] = 0;
                        break;
                    case DailyShopKind.Chest:
                        d.keys[i] = c != null ? c.ShopChestKey : "silver";
                        d.counts[i] = 1;
                        d.prices[i] = c != null ? c.ShopChestGems : 40;
                        break;
                    default:
                    {
                        var host = PickHost(p, kind, rng);
                        if (host == null)
                        {
                            // 고를 몸이 없으면 무료 선물로 채운다 — 빈 칸을 진열하지 않는다
                            d.kinds[i] = (int)DailyShopKind.FreeGold;
                            d.keys[i] = string.Empty;
                            d.counts[i] = Mathf.RoundToInt((c != null ? c.ShopFreeGold : 100) * mul);
                            d.prices[i] = 0;
                            d.bought |= 1 << i;   // 선물은 첫 칸 하나뿐이다 — 대체 칸은 산 것으로 둔다
                            break;
                        }
                        _used.Add(host.HostKey);
                        int count = c != null ? c.ShopShardCount(host.Grade) : 3;
                        int each = c != null ? c.ShopShardGold(host.Grade) : 80;
                        d.keys[i] = host.HostKey;
                        d.counts[i] = count;
                        d.prices[i] = Mathf.RoundToInt(count * each * mul);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 그 칸에 올릴 몸. 다 키운 몸은 뺀다(사면 정수로 바뀔 뿐이다). 후보가 없으면 더 넓게 찾는다.
        /// </summary>
        private HostEntry PickHost(IPlayerDataService p, DailyShopKind kind, System.Random rng)
        {
            var list = p.PlayableHosts;
            for (int pass = 0; pass < 2; pass++)
            {
                _pool.Clear();
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null || e.IsGhost || _used.Contains(e.HostKey)) continue;
                    int m = p.GetMastery(e.HostKey);
                    if (m >= p.MasteryMax) continue;
                    bool owned = m >= 1;
                    bool fits = pass == 1 || kind == DailyShopKind.AnyShard
                             || (kind == DailyShopKind.OwnedShard && owned)
                             || (kind == DailyShopKind.LockedShard && !owned);
                    if (fits) _pool.Add(e);
                }
                if (_pool.Count > 0) return _pool[rng.Next(_pool.Count)];
            }
            return null;
        }
    }
}
