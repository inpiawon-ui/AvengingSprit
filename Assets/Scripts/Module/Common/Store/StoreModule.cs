using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Module.Common.Chest;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.Module.Common.Store
{
    public enum PurchaseOutcome { Granted, NotEnoughGem, SoldOut, Cancelled, Failed }

    public enum RewardKind { Gold, Gem, Essence, Ticket, Stamina, Shard, Chest, PassXp, PassPremium, Card, Profile }

    public readonly struct RewardLine
    {
        public readonly RewardKind Kind;
        public readonly string Key;
        public readonly int Amount;

        public RewardLine(RewardKind kind, string key, int amount) { Kind = kind; Key = key; Amount = amount; }
    }

    /// <summary>지급 결과 — 화면에 보여 줄 줄 · 그 자리에서 연 상자.</summary>
    public sealed class RewardResult
    {
        public readonly List<RewardLine> Lines = new();
        public readonly List<ChestReward> OpenedChests = new();
    }

    /// <summary>계약 상태 — 남은 날 · 오늘 받았나.</summary>
    public readonly struct CardState
    {
        public readonly bool Active;
        public readonly int DaysLeft;
        public readonly bool ClaimedToday;

        public CardState(bool active, int daysLeft, bool claimedToday) { Active = active; DaysLeft = daysLeft; ClaimedToday = claimedToday; }
    }

    /// <summary>
    /// 로비 상점(기획서 `AVSR_Content_Store.md` B-1). 상품 · 보이기 조건 · 구매 제한 · 젬 구매 · 결제 상품 지급 · 계약.
    /// 결제는 지금 **가짜 스토어**(즉시 성공) — 실제 스토어(Unity IAP)는 B-10 6단계에서 붙인다. 값은 표의 한국 기준값.
    /// </summary>
    public interface IStoreService
    {
        bool IsReady { get; }

        /// <summary>그 탭에 보일 상품(보이기 조건 · 1회 한정 소진을 거른 것). 표 순서.</summary>
        IReadOnlyList<StoreProduct> Visible(string tab);

        StoreProduct Find(string id);

        /// <summary>결제: 「₩1,200」 / 젬: 「60」.</summary>
        string PriceLabel(StoreProduct p);

        /// <summary>오늘 · 이번 주 남은 횟수. 제한이 없으면 -1.</summary>
        int LeftCount(StoreProduct p, out int max);

        bool IsSoldOut(StoreProduct p);

        /// <summary>젬 묶음 첫 구매 2배가 남았나.</summary>
        bool IsFirstDouble(StoreProduct p);

        UniTask<(PurchaseOutcome outcome, RewardResult result)> BuyAsync(string productId, CancellationToken ct = default);

        CardState Card(string cardKey);
        bool TryClaimCard(string cardKey, out RewardResult result);

        /// <summary>입문 패키지 남은 날(첫 실행부터 7일). 끝났으면 0.</summary>
        int StarterDaysLeft { get; }

        /// <summary>보상 문자열을 지급한다 — 패스 · 미션 · 계약이 같은 길을 쓴다.</summary>
        RewardResult Grant(string rewards);
    }

    [Module(Layer = ModuleLayer.Game)]
    public sealed class StoreModule : IModule
    {
        private StoreService _service;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _service = new StoreService();
            CoreModule.Register<IStoreService>(_service);
            IsInitialized = true;
        }

        public void Initialize()
        {
            _service.Bind(CoreModule.Get<IEventBus>());
            _service.LoadAsync().Forget();   // fire-and-forget: 표가 오기 전엔 상점이 빈 칸으로 그린다
        }

        public void Dispose()
        {
            CoreModule.Unregister<IStoreService>();
            _service = null;
            IsInitialized = false;
        }
    }

    internal sealed class StoreService : IStoreService
    {
        private const string TableAddress = "TableData/StoreTable";
        private const int StarterDays = 7;
        private const int CardMaxDays = 180;

        private StoreTable _table;
        private IEventBus _bus;
        private readonly List<StoreProduct> _visible = new();

        public bool IsReady => _table != null && Player != null;

        private static IPlayerDataService Player
            => CoreModule.TryGet<IPlayerDataService>(out var p) && p.IsReady ? p : null;

        private static IResetClock Clock => CoreModule.TryGet<IResetClock>(out var c) ? c : null;
        private static int Today => Clock?.DayKey ?? 0;
        private static int Week => Clock?.WeekKey ?? 0;

        public void Bind(IEventBus bus) => _bus = bus;

        public async UniTask LoadAsync()
        {
            try
            {
                _table = await CoreModule.Get<IResourceManager>().LoadAsync<StoreTable>(TableAddress);
            }
            catch (Exception e)
            {
                Debug.LogError($"[상점] 표를 못 읽었다: {e.Message}");
            }
            Changed();
        }

        public StoreProduct Find(string id) => _table != null ? _table.Find(id) : null;

        // ── 보이기 · 제한 ────────────────────────────────────────

        public IReadOnlyList<StoreProduct> Visible(string tab)
        {
            _visible.Clear();
            var p = Player;
            if (_table == null || p == null) return _visible;
            EnsureInstall(p);
            var all = _table.Products;
            for (int i = 0; i < all.Length; i++)
            {
                var s = all[i];
                if (s.Tab != tab || !Shown(s, p)) continue;
                if (s.Limit == "once" && CountOf(p.Store, s.Id) > 0) continue;   // 1회 한정 — 사면 내린다
                _visible.Add(s);
            }
            return _visible;
        }

        private bool Shown(StoreProduct s, IPlayerDataService p)
        {
            var show = s.Show ?? "always";
            if (show == "always" || show.Length == 0) return true;
            if (show.StartsWith("days_since_install<=")) return StarterDaysLeft > 0;
            if (show.StartsWith("chapter_cleared>=") && int.TryParse(show.Substring(17), out var ch))
                return p.ClearedChapter == ch;   // 그 챕터를 막 깬 동안만 — 다음 챕터를 깨면 다음 기념으로 바뀐다
            return false;   // season_active 등 — 패스 화면 몫
        }

        public int StarterDaysLeft
        {
            get
            {
                var p = Player;
                if (p == null) return 0;
                EnsureInstall(p);
                int passed = DaysBetween(p.Store.installDay, Today);
                return Math.Max(0, StarterDays - passed);
            }
        }

        private static void EnsureInstall(IPlayerDataService p)
        {
            if (p.Store.installDay == 0 && Today != 0) p.Store.installDay = Today;
        }

        public int LeftCount(StoreProduct s, out int max)
        {
            max = -1;
            var p = Player;
            if (p == null || string.IsNullOrEmpty(s.Limit)) return -1;
            var d = p.Store;
            int i = IndexOf(d, s.Id);
            if (s.Limit.StartsWith("daily:") && int.TryParse(s.Limit.Substring(6), out max))
                return max - (i >= 0 && d.lastDay[i] == Today ? d.dayCounts[i] : 0);
            if (s.Limit.StartsWith("weekly:") && int.TryParse(s.Limit.Substring(7), out max))
                return max - (i >= 0 && d.lastWeek[i] == Week ? d.weekCounts[i] : 0);
            if (s.Limit == "once") { max = 1; return CountOf(d, s.Id) > 0 ? 0 : 1; }
            return -1;
        }

        public bool IsSoldOut(StoreProduct s) => LeftCount(s, out _) == 0;

        public bool IsFirstDouble(StoreProduct s)
            => s.Badge == "first_double" && Player != null && Array.IndexOf(Player.Store.firstDoubleUsed, s.Id) < 0;

        public string PriceLabel(StoreProduct s)
        {
            if (s.IsIap) return $"₩{(_table != null ? _table.Krw(s.Price) : 0):N0}";
            return s.GemPrice.ToString("N0");
        }

        // ── 구매 ────────────────────────────────────────────────

        public async UniTask<(PurchaseOutcome, RewardResult)> BuyAsync(string productId, CancellationToken ct = default)
        {
            var p = Player;
            var s = Find(productId);
            if (p == null || s == null) return (PurchaseOutcome.Failed, null);
            if (IsSoldOut(s)) return (PurchaseOutcome.SoldOut, null);

            if (s.IsIap)
            {
                // 가짜 스토어 — 결제창 대신 잠깐 기다렸다 성공. ⚠ 지급 전에 꺼져도 남게 먼저 적고, 지급 뒤 지운다
                Pending(p.Store, s.Id, true);
                await p.SaveAsync();
                await UniTask.Delay(TimeSpan.FromSeconds(0.3), ignoreTimeScale: true, cancellationToken: ct);
            }
            else if (s.GemPrice > 0 && !p.TrySpendGem(s.GemPrice))
            {
                return (PurchaseOutcome.NotEnoughGem, null);
            }

            var result = Deliver(p, s);
            if (s.IsIap) Pending(p.Store, s.Id, false);
            await p.SaveAsync();
            Changed();
            return (PurchaseOutcome.Granted, result);
        }

        /// <summary>산 것을 지급하고 기록한다 — 결제 성공 뒤에만 부른다.</summary>
        private RewardResult Deliver(IPlayerDataService p, StoreProduct s)
        {
            var rewards = s.Rewards ?? string.Empty;
            // 첫 구매 2배 — 젬 묶음 첫 구매는 젬을 두 배로(기획서 B-2)
            if (IsFirstDouble(s))
            {
                rewards = DoubleGem(rewards);
                var used = p.Store.firstDoubleUsed;
                Array.Resize(ref used, used.Length + 1);
                used[^1] = s.Id;
                p.Store.firstDoubleUsed = used;
            }
            Count(p.Store, s.Id);
            return Grant(rewards);
        }

        private static string DoubleGem(string rewards)
        {
            var parts = rewards.Split(';');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].StartsWith("gem:") && int.TryParse(parts[i].Substring(4), out var g)) parts[i] = $"gem:{g * 2}";
            return string.Join(";", parts);
        }

        // ── 계약 ────────────────────────────────────────────────

        public CardState Card(string key)
        {
            var p = Player;
            if (p == null) return default;
            var d = p.Store;
            int i = Array.IndexOf(d.cardKeys, key);
            if (i < 0 || d.cardEndDay[i] < Today) return new CardState(false, 0, false);
            return new CardState(true, DaysBetween(Today, d.cardEndDay[i]) + 1, d.cardLastClaimDay[i] == Today);
        }

        public bool TryClaimCard(string key, out RewardResult result)
        {
            result = null;
            var p = Player;
            var card = _table?.Card(key);
            if (p == null || card == null) return false;
            var st = Card(key);
            if (!st.Active || st.ClaimedToday) return false;
            int i = Array.IndexOf(p.Store.cardKeys, key);
            p.Store.cardLastClaimDay[i] = Today;
            int stamina = PerkValue(card.Perks, "daily_stamina");
            result = Grant($"gem:{card.DailyGem}" + (stamina > 0 ? $";stamina:{stamina}" : string.Empty));
            p.SaveAsync().Forget();   // fire-and-forget: 받은 것은 이미 들어갔다 — 화면을 기다리게 할 이유가 없다
            Changed();
            return true;
        }

        /// <summary>계약을 산다 — 즉시 젬, 오늘부터 days 일. 기간 중이면 뒤로 이어 붙인다(최대 180일).</summary>
        private void StartCard(IPlayerDataService p, string key, RewardResult result)
        {
            var card = _table?.Card(key);
            if (card == null) return;
            var d = p.Store;
            int i = Array.IndexOf(d.cardKeys, key);
            if (i < 0)
            {
                i = d.cardKeys.Length;
                Array.Resize(ref d.cardKeys, i + 1);
                Array.Resize(ref d.cardEndDay, i + 1);
                Array.Resize(ref d.cardLastClaimDay, i + 1);
                d.cardKeys[i] = key;
            }
            int from = d.cardEndDay[i] >= Today ? AddDays(d.cardEndDay[i], 1) : Today;
            int end = AddDays(from, card.Days - 1);
            int cap = AddDays(Today, CardMaxDays - 1);
            d.cardEndDay[i] = Math.Min(end, cap);
            p.AddCurrency(0, card.InstantGem);
            result.Lines.Add(new RewardLine(RewardKind.Gem, null, card.InstantGem));
            result.Lines.Add(new RewardLine(RewardKind.Card, key, card.Days));
        }

        private static int PerkValue(string perks, string name)
        {
            if (string.IsNullOrEmpty(perks)) return 0;
            foreach (var part in perks.Split(';'))
            {
                var kv = part.Split(':');
                if (kv.Length == 2 && kv[0].Trim() == name && int.TryParse(kv[1].Trim().TrimStart('+'), out var v)) return v;
            }
            return 0;
        }

        // ── 보상 지급 ───────────────────────────────────────────

        public RewardResult Grant(string rewards)
        {
            var result = new RewardResult();
            var p = Player;
            if (p == null || string.IsNullOrEmpty(rewards)) return result;
            foreach (var raw in rewards.Split(';'))
            {
                var part = raw.Trim();
                if (part.Length == 0) continue;
                int c = part.IndexOf(':');
                string kind = c < 0 ? part : part.Substring(0, c);
                string val = c < 0 ? string.Empty : part.Substring(c + 1);
                int n = int.TryParse(val, out var v) ? v : 0;
                switch (kind)
                {
                    case "gem": p.AddCurrency(0, n); result.Lines.Add(new RewardLine(RewardKind.Gem, null, n)); break;
                    case "gold": p.AddCurrency(n, 0); result.Lines.Add(new RewardLine(RewardKind.Gold, null, n)); break;
                    case "goldrun":
                        int g = n * RunGold(p);
                        p.AddCurrency(g, 0);
                        result.Lines.Add(new RewardLine(RewardKind.Gold, null, g));
                        break;
                    case "essence": p.AddEssence(n); result.Lines.Add(new RewardLine(RewardKind.Essence, null, n)); break;
                    case "ticket": p.AddTickets(n); result.Lines.Add(new RewardLine(RewardKind.Ticket, null, n)); break;
                    case "stamina":
                        p.SetStamina(p.Stamina + n);
                        result.Lines.Add(new RewardLine(RewardKind.Stamina, null, n));
                        break;
                    case "chest":
                        string key = val;
                        int times = 1;
                        int star = val.IndexOf('*');
                        if (star >= 0) { key = val.Substring(0, star); int.TryParse(val.Substring(star + 1), out times); }
                        if (CoreModule.TryGet<IChestService>(out var chests))
                            for (int t = 0; t < Math.Max(1, times); t++)
                                if (chests.TryOpenImmediate(key, out var opened)) result.OpenedChests.Add(opened);
                        result.Lines.Add(new RewardLine(RewardKind.Chest, key, Math.Max(1, times)));
                        break;
                    case "card": StartCard(p, val, result); break;
                    case "passxp":
                        if (CoreModule.TryGet<ISeasonService>(out var s1)) s1.AddXp(n);
                        result.Lines.Add(new RewardLine(RewardKind.PassXp, null, n));
                        break;
                    case "passpremium":
                        if (CoreModule.TryGet<ISeasonService>(out var s2)) s2.GrantPremium();
                        result.Lines.Add(new RewardLine(RewardKind.PassPremium, null, 1));
                        break;
                    case "profile":
                        if (CoreModule.TryGet<ISeasonService>(out var s3)) s3.GrantProfile(val);
                        result.Lines.Add(new RewardLine(RewardKind.Profile, val, 1));
                        break;
                    default: Debug.LogWarning($"[상점] 모르는 보상: {part}"); break;
                }
            }
            return result;
        }

        /// <summary>판 골드 한 판분 — 열린 가장 높은 챕터의 판 골드(챕터 표).</summary>
        private static int RunGold(IPlayerDataService p)
        {
            int ch = Mathf.Clamp(Math.Max(1, p.UnlockedChapter), 1, Math.Max(1, p.ChapterCount));
            return Math.Max(1, p.ChapterInfo(ch).RunGold);
        }

        // ── 기록 ────────────────────────────────────────────────

        private static int IndexOf(StoreSaveData d, string id) => Array.IndexOf(d.ids, id);

        private static int CountOf(StoreSaveData d, string id)
        {
            int i = IndexOf(d, id);
            return i >= 0 ? d.counts[i] : 0;
        }

        private static void Count(StoreSaveData d, string id)
        {
            int i = IndexOf(d, id);
            if (i < 0)
            {
                i = d.ids.Length;
                Array.Resize(ref d.ids, i + 1);
                Array.Resize(ref d.counts, i + 1);
                Array.Resize(ref d.lastDay, i + 1);
                Array.Resize(ref d.lastWeek, i + 1);
                Array.Resize(ref d.dayCounts, i + 1);
                Array.Resize(ref d.weekCounts, i + 1);
                d.ids[i] = id;
            }
            d.counts[i]++;
            if (d.lastDay[i] != Today) { d.lastDay[i] = Today; d.dayCounts[i] = 0; }
            if (d.lastWeek[i] != Week) { d.lastWeek[i] = Week; d.weekCounts[i] = 0; }
            d.dayCounts[i]++;
            d.weekCounts[i]++;
        }

        private static void Pending(StoreSaveData d, string id, bool add)
        {
            var list = new List<string>(d.pending);
            if (add) list.Add(id); else list.Remove(id);
            d.pending = list.ToArray();
        }

        private void Changed() => _bus?.Publish(new StoreChangedEvent());

        // ── 날짜(yyyyMMdd) ──────────────────────────────────────

        private static DateTime ToDate(int key)
            => key <= 0 ? DateTime.MinValue : new DateTime(key / 10000, key / 100 % 100, key % 100);

        private static int ToKey(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;

        private static int AddDays(int key, int days) => ToKey(ToDate(key).AddDays(days));

        private static int DaysBetween(int from, int to)
            => from <= 0 || to <= 0 ? 0 : (int)(ToDate(to) - ToDate(from)).TotalDays;
    }
}
