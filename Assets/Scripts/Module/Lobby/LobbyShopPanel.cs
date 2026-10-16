using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.Chest;
using Game.Module.Common.Shop;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 로비 상점 — 아래 바 SHOP(기획서 `AVSR_Content_Store.md` B-6, 시안 S02 · S03 · S04 · S05 · S07).
    ///   오늘 — 오늘의 상점 여섯 칸(`IDailyShopService`) · 새로고침
    ///   상자 — 은 · 금 · 백금 상자를 젬으로 사서 바로 연다
    ///   재화 — 계약 둘 · 입문 패키지 셋 · 챕터 돌파 · 젬 묶음 여섯 · 골드 셋(`IStoreService`)
    /// 노드는 `LobbyShopBuilder` 가 세운다. 이름 = 바인딩 키.
    /// </summary>
    public sealed class LobbyShopPanel : MonoBehaviour
    {
        [Serializable]
        public struct KeyedSprite
        {
            public string Key;
            public Sprite Sprite;
        }

        [SerializeField] private KeyedSprite[] _faces = Array.Empty<KeyedSprite>();     // 호스트 얼굴(face_{key})
        [SerializeField] private KeyedSprite[] _chests = Array.Empty<KeyedSprite>();    // 상자 키 → 그림
        [SerializeField] private KeyedSprite[] _products = Array.Empty<KeyedSprite>();  // 상품 id → 그림
        [SerializeField] private Sprite _gemIcon, _goldIcon, _ticketIcon, _essenceIcon, _staminaIcon, _goldPile;
        [SerializeField] private Sprite _tab, _tabSelected;
        [SerializeField] private Sprite _starGold, _starSilver;   // 성급 별(StarRank) — 오늘의 상점 조각 칸
        [SerializeField] private Material _silhouette;

        private enum Page { Today, Chest, Currency }

        private static readonly string[] ChestKeys = { "silver", "gold", "platinum" };
        private static readonly string[] ChestProducts = { "chest_silver", "chest_gold", "chest_platinum" };
        private static readonly string[] Starters = { "starter_1", "starter_2", "starter_3" };
        private static readonly string[] Gems = { "gem_60", "gem_320", "gem_680", "gem_2100", "gem_3600", "gem_7500" };
        private static readonly string[] Golds = { "gold_s", "gold_m", "gold_l" };
        private static readonly string[] Cards = { "card_small", "card_big" };
        private static readonly string[] CardKeys = { "soul_small", "soul_big" };

        private static readonly Color GoldColor = new(1f, 0.85f, 0.32f);

        private UIBinder _ui;
        private IDisposable _storeToken, _currencyToken, _dailyToken;
        private Page _page = Page.Currency;
        private float _tick;
        private string _pending;   // 확인 창에 띄운 상품
        private static readonly Vector2 SlotStarCenter = new(0f, -71f);   // 오늘의 상점 칸 별 줄(빌더 자리)
        private float _confirmRowTop = float.NaN, _confirmRowHeight;   // 확인 창 첫 줄 위 끝 · 줄 높이(빌더 값)
        private bool _busy;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("ShopTabToday", () => Show(Page.Today));
            _ui.OnClick("ShopTabChest", () => Show(Page.Chest));
            _ui.OnClick("ShopTabCurrency", () => Show(Page.Currency));

            _ui.OnClick("TodayRefresh", OnRefreshToday);
            for (int i = 0; i < 6; i++)
            {
                int slot = i;
                _ui.OnClick($"DailySlot{i}Buy", () => OnBuyDaily(slot));
            }
            for (int i = 0; i < 3; i++)
            {
                int row = i;
                _ui.OnClick($"ChestRow{i}Buy", () => Ask(ChestProducts[row]));
                _ui.OnClick($"ChestRow{i}Rate", () => ShowRates(ChestKeys[row]));
            }
            for (int i = 0; i < Cards.Length; i++)
            {
                int c = i;
                _ui.OnClick($"Card{i}Buy", () => Ask(Cards[c]));
                _ui.OnClick($"Card{i}Claim", () => ClaimCard(c));
            }
            for (int i = 0; i < Starters.Length; i++) { int s = i; _ui.OnClick($"Starter{i}Buy", () => Ask(Starters[s])); }
            for (int i = 0; i < Gems.Length; i++) { int g = i; _ui.OnClick($"Gem{i}Buy", () => Ask(Gems[g])); }
            for (int i = 0; i < Golds.Length; i++) { int g = i; _ui.OnClick($"Gold{i}Buy", () => Ask(Golds[g])); }
            _ui.OnClick("ChapterPackBuy", () =>
            {
                var pack = _ui.Find("ChapterPack");
                var tag = pack != null ? pack.GetComponent<ShopProductTag>() : null;
                if (tag != null) Ask(tag.Id);
            });
            _ui.OnClick("ShopRestore", () => SystemPopup.Show(
                Localize.FromTable("ui.store.restore_done", "복원할 구매가 없어요."), null, Localize.Get("ui.common.ok"), null));
            _ui.OnClick("ShopLegal", () => SystemPopup.Show(
                Localize.FromTable("ui.store.legal_body", "특정상거래법 · 자금결제법 표기는 출시 전에 채웁니다."),
                null, Localize.Get("ui.common.ok"), null));

            _ui.OnClick("StoreConfirmBuy", OnConfirmBuy);
            _ui.OnClick("StoreConfirmClose", CloseConfirm);
            _ui.OnClick("StoreConfirmX", CloseConfirm);
            _ui.OnClick("StoreRewardOk", () => _ui.SetActive("StoreReward", false));
        }

        private void OnEnable()
        {
            if (CoreModule.TryGet<IEventBus>(out var bus))
            {
                _storeToken = bus.Subscribe<StoreChangedEvent>(_ => Refresh());
                _currencyToken = bus.Subscribe<CurrencyChangedEvent>(_ => RefreshHeader());
                _dailyToken = bus.Subscribe<DailyShopChangedEvent>(_ => RefreshToday());
            }
        }

        private void OnDisable()
        {
            _storeToken?.Dispose();
            _currencyToken?.Dispose();
            _dailyToken?.Dispose();
            _storeToken = _currencyToken = _dailyToken = null;
        }

        private void Update()
        {
            _tick += Time.unscaledDeltaTime;
            if (_tick < 1f) return;
            _tick = 0f;
            if (_page == Page.Today) RefreshTodayTimer();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            var nav = transform.parent != null ? transform.parent.Find("BottomNav") : null;
            if (nav != null && transform.GetSiblingIndex() > nav.GetSiblingIndex()) transform.SetSiblingIndex(nav.GetSiblingIndex());
            _ui.SetActive("StoreConfirm", false);
            _ui.SetActive("StoreReward", false);
            Show(_page);
        }

        public void Close() => gameObject.SetActive(false);

        /// <summary>뒤로가기 — 팝업부터 닫는다. 닫은 것이 없으면 false(창 닫기는 로비가).</summary>
        public bool CloseTopPopup()
        {
            if (_ui.Find("StoreReward") is Transform r && r.gameObject.activeSelf) { r.gameObject.SetActive(false); return true; }
            if (_ui.Find("StoreConfirm") is Transform c && c.gameObject.activeSelf) { CloseConfirm(); return true; }
            return false;
        }

        private void Show(Page page)
        {
            _page = page;
            SetTab("ShopTabToday", page == Page.Today);
            SetTab("ShopTabChest", page == Page.Chest);
            SetTab("ShopTabCurrency", page == Page.Currency);
            _ui.SetActive("ShopPageToday", page == Page.Today);
            _ui.SetActive("ShopPageChest", page == Page.Chest);
            _ui.SetActive("ShopPageCurrency", page == Page.Currency);
            Refresh();
        }

        private void SetTab(string node, bool on)
        {
            var img = _ui.Get<Image>(node);
            if (img != null) img.sprite = on ? _tabSelected : _tab;
            var tab = _ui.Find(node);
            var label = tab != null ? tab.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (label != null) label.color = on ? new Color(1f, 0.85f, 0.32f) : Color.white;  // 고른 탭은 금테 · 어두운 속 — 시안처럼 금색 글자
        }

        private void Refresh()
        {
            RefreshHeader();
            if (_page == Page.Today) RefreshToday();
            else if (_page == Page.Chest) RefreshChest();
            else RefreshCurrency();
        }

        private void RefreshHeader()
        {
            _ui.SetText("ShopTitleText", Localize.FromTable("ui.store.title", "상점"));
            _ui.SetText("TabLabelToday", Localize.FromTable("ui.store.tab.today", "오늘"));
            _ui.SetText("TabLabelChest", Localize.FromTable("ui.store.tab.chest", "상자"));
            _ui.SetText("TabLabelCurrency", Localize.FromTable("ui.store.tab.currency", "재화"));
            if (!CoreModule.TryGet<IPlayerDataService>(out var p) || !p.IsReady) return;
            _ui.SetText("ShopGoldText", p.Gold.ToString("N0"));
            _ui.SetText("ShopGemText", p.Gem.ToString("N0"));
        }

        // ── 오늘 ─────────────────────────────────────────────

        private void RefreshToday()
        {
            if (_page != Page.Today || !CoreModule.TryGet<IDailyShopService>(out var shop)) return;
            CoreModule.TryGet<IPlayerDataService>(out var p);
            _ui.SetText("TodayTitle", Localize.FromTable("ui.store.today.title", "오늘의 상점"));
            int cost = shop.RefreshGemCost;
            _ui.SetText("TodayRefreshLabel", Localize.FromTable("ui.store.today.refresh", "새로고침"));
            _ui.SetText("TodayRefreshCost", cost > 0 ? cost.ToString() : "-");
            int left = 0;
            if (p?.Config != null) left = Math.Max(0, p.Config.ShopRefreshCount - (p.DailyShop?.refreshes ?? 0));
            _ui.SetText("TodayRefreshLeft", string.Format(Localize.FromTable("ui.store.today.left", "오늘 {0}회 남음"), left));
            RefreshTodayTimer();

            for (int i = 0; i < 6; i++)
            {
                var root = _ui.Find($"DailySlot{i}");
                if (root == null) continue;
                if (i >= shop.SlotCount) { root.gameObject.SetActive(false); continue; }
                root.gameObject.SetActive(true);
                var s = shop.Get(i);
                bool free = s.Kind == DailyShopKind.FreeGold;
                In(root, "SlotFrameFree", free);
                In(root, "SlotNew", s.Kind == DailyShopKind.LockedShard && !s.Bought);
                In(root, "SlotLock", s.Kind == DailyShopKind.LockedShard);
                In(root, "SlotSold", s.Bought);
                var host = s.IsShard && p != null ? p.GetHost(s.Key) : null;

                var art = Img(root, "SlotArt");
                if (art != null)
                {
                    art.sprite = free ? _goldPile : s.Kind == DailyShopKind.Chest ? Keyed(_chests, s.Key) : Keyed(_faces, s.Key);
                    art.enabled = art.sprite != null;
                    art.preserveAspect = true;
                    bool locked = s.Kind == DailyShopKind.LockedShard;
                    art.material = locked ? _silhouette : null;
                    art.color = locked ? new Color(0.16f, 0.2f, 0.35f) : Color.white;
                }
                string name = free ? Localize.FromTable("ui.store.today.free_gold", "무료 골드")
                    : s.Kind == DailyShopKind.Chest ? Localize.Get($"chest.{s.Key}.name")
                    : string.Format(Localize.FromTable("ui.store.today.shard", "{0} 조각"), host != null ? host.DisplayName : s.Key);
                Text(root, "SlotName", name);
                Text(root, "SlotCount", free ? s.Count.ToString("N0") : s.IsShard ? $"×{s.Count}" : string.Empty);
                var countNode = Find(root, "SlotCount");
                var count = countNode != null ? countNode.GetComponent<TextMeshProUGUI>() : null;
                if (count != null) count.color = free ? GoldColor : Color.white;

                // 그 몸의 성급(1~10) — 은별 1~4 · 5성마다 큰 금별. 조각 칸이 아니면 별 없음
                int rank = s.IsShard && host != null && p != null ? p.GetMastery(host.HostKey) : 0;
                StarRank.Apply(root, "SlotStar", rank, _starGold, _starSilver, SlotStarCenter, 14f, 2f);

                In(root, "SlotPriceIconGold", !free && !s.PaidWithGem && !s.Bought);
                In(root, "SlotPriceIconGem", !free && s.PaidWithGem && !s.Bought);
                Text(root, "SlotPriceText", s.Bought ? Localize.FromTable("ui.store.sold", "구매 완료")
                    : free ? Localize.FromTable("ui.store.free", "무료")
                    : s.PaidWithGem ? string.Format(Localize.FromTable("ui.store.price_gem", "젬 {0}"), s.Price)
                    : string.Format(Localize.FromTable("ui.store.price_gold", "골드 {0}"), s.Price.ToString("N0")));
                var priceNode = Find(root, "SlotPriceText");
                var priceText = priceNode != null ? priceNode.GetComponent<TextMeshProUGUI>() : null;
                if (priceText != null) priceText.color = s.Bought ? new Color(0.6f, 0.65f, 0.75f) : free ? new Color(0.45f, 1f, 0.45f) : Color.white;
                Text(root, "SlotSoldText", Localize.FromTable("ui.store.sold", "구매 완료"));
            }
        }

        private void RefreshTodayTimer()
        {
            if (!CoreModule.TryGet<IDailyShopService>(out var shop)) return;
            var t = shop.UntilReset;
            _ui.SetText("TodayTimer", string.Format(Localize.FromTable("ui.store.today.timer", "새 상품까지 {0}"),
                $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}"));
        }

        private void OnBuyDaily(int slot)
        {
            if (!CoreModule.TryGet<IDailyShopService>(out var shop)) return;
            if (shop.TryBuy(slot, out var fail))
            {
                GameSound.Cue("ui.select");
                RefreshToday();
                RefreshHeader();
                return;
            }
            if (!string.IsNullOrEmpty(fail)) SystemPopup.Show(Localize.Get(fail), null, Localize.Get("ui.common.ok"), null);
        }

        private void OnRefreshToday()
        {
            if (!CoreModule.TryGet<IDailyShopService>(out var shop)) return;
            if (shop.TryRefresh(out var fail)) { GameSound.Cue("ui.select"); RefreshToday(); return; }
            if (!string.IsNullOrEmpty(fail)) SystemPopup.Show(Localize.Get(fail), null, Localize.Get("ui.common.ok"), null);
        }

        // ── 상자 ─────────────────────────────────────────────

        private static readonly (int min, int max, int kmin, int kmax)[] ChestInfo =
            { (100, 200, 1, 2), (300, 500, 2, 3), (700, 1000, 3, 4) };

        private void RefreshChest()
        {
            CoreModule.TryGet<IStoreService>(out var store);
            for (int i = 0; i < 3; i++)
            {
                var root = _ui.Find($"ChestRow{i}");
                if (root == null) continue;
                var prod = store?.Find(ChestProducts[i]);
                Text(root, "RowTitle", Localize.Get($"chest.{ChestKeys[i]}.name"));
                Text(root, "RowPrice", prod != null ? store.PriceLabel(prod) : "-");
                Text(root, "RowOpenNow", Localize.FromTable("ui.store.chest.open_now", "바로 열림"));
                var (min, max, kmin, kmax) = ChestInfo[i];
                Text(root, "RowGold", string.Format(Localize.FromTable("ui.store.chest.gold", "골드 {0} ~ {1}"), min.ToString("N0"), max.ToString("N0")));
                Text(root, "RowShard", string.Format(Localize.FromTable("ui.store.chest.shard", "몸 {0} ~ {1}명 조각"), kmin, kmax));
                Text(root, "RowRateText", Localize.FromTable("ui.store.chest.rate", "확률 보기") + "  ▶");
            }
            _ui.SetText("ChestNote", Localize.FromTable("ui.store.chest.note", "상자는 사는 즉시 열립니다 · 상자 칸을 쓰지 않음"));
        }

        private void ShowRates(string chestKey)
        {
            // 확률은 상자 표(ChestTable)의 등급 무게 — 표가 바뀌면 같이 바뀐다
            string body = chestKey switch
            {
                "silver" => "B 85% · A 15% · S 0%",
                "gold" => "B 55% · A 38% · S 7%",
                _ => "B 30% · A 45% · S 25%",
            };
            SystemPopup.Show(string.Format(Localize.FromTable("ui.store.chest.rate_body", "{0}\n조각 등급 확률\n{1}"),
                Localize.Get($"chest.{chestKey}.name"), body), null, Localize.Get("ui.common.ok"), null);
        }

        // ── 재화 ─────────────────────────────────────────────

        private void RefreshCurrency()
        {
            if (!CoreModule.TryGet<IStoreService>(out var store) || !store.IsReady) return;
            var list = store.Visible("currency");

            // 계약 둘
            for (int i = 0; i < Cards.Length; i++)
            {
                var root = _ui.Find($"Card{i}");
                if (root == null) continue;
                var prod = store.Find(Cards[i]);
                var st = store.Card(CardKeys[i]);
                Text(root, "CardTitle", Localize.FromTable($"store.{Cards[i]}.name", i == 0 ? "영혼 계약" : "망령 군주의 계약"));
                Text(root, "CardDesc", Localize.FromTable($"store.{Cards[i]}.desc", i == 0 ? "즉시 젬 300 + 30일 매일 90" : "즉시 1,000 + 매일 200 + 편의"));
                In(root, $"Card{i}Buy", !st.Active);
                In(root, "CardDays", st.Active);
                In(root, $"Card{i}Claim", st.Active);
                if (prod != null) Text(root, "CardPrice", store.PriceLabel(prod));
                Text(root, "CardDaysText", string.Format(Localize.FromTable("ui.store.card.days", "남은 {0}일"), st.DaysLeft));
                Text(root, "CardClaimText", st.ClaimedToday ? Localize.FromTable("ui.store.card.claimed", "받음")
                                                            : Localize.FromTable("ui.store.card.claim", "오늘 받기"));
                var claimNode = Find(root, $"Card{i}Claim");
                var claim = claimNode != null ? claimNode.GetComponent<Button>() : null;
                if (claim != null) claim.interactable = st.Active && !st.ClaimedToday;
            }

            // 입문 패키지
            int days = store.StarterDaysLeft;
            bool anyStarter = false;
            for (int i = 0; i < Starters.Length; i++)
            {
                var root = _ui.Find($"Starter{i}");
                if (root == null) continue;
                var prod = Contains(list, Starters[i]);
                root.gameObject.SetActive(prod != null);
                if (prod == null) continue;
                anyStarter = true;
                Text(root, "StarterTitle", Localize.FromTable($"store.{prod.Id}.name", $"입문 {new[] { "I", "II", "III" }[i]}"));
                Text(root, "StarterDesc", Describe(prod.Rewards));
                Text(root, "StarterOnce", Localize.FromTable("ui.store.once", "1회 한정"));
                Text(root, "StarterPrice", store.PriceLabel(prod));
            }
            _ui.SetActive("StarterSection", anyStarter);
            _ui.SetText("StarterHead", Localize.FromTable("ui.store.starter.title", "입문 패키지")
                                       + $"  <color=#FFD94A>· {string.Format(Localize.FromTable("ui.store.days_left", "{0}일 남음"), days)}</color>");

            // 챕터 돌파 — 보이는 것 하나
            StoreProduct chapter = null;
            for (int i = 0; i < list.Count; i++) if (list[i].Id.StartsWith("chapter_")) { chapter = list[i]; break; }
            _ui.SetActive("ChapterPack", chapter != null);
            if (chapter != null)
            {
                var tag = _ui.Find("ChapterPack").GetComponent<ShopProductTag>();
                if (tag != null) tag.Id = chapter.Id;
                int ch = int.TryParse(chapter.Id.Substring(8), out var c) ? c : 1;
                _ui.SetText("ChapterPackTitle", string.Format(Localize.FromTable("ui.store.chapter.title", "챕터 {0} 돌파 기념"), ch));
                _ui.SetText("ChapterPackDesc", Describe(chapter.Rewards));
                _ui.SetText("ChapterPackOnce", Localize.FromTable("ui.store.once", "1회 한정"));
                _ui.SetText("ChapterPackPrice", store.PriceLabel(chapter));
            }

            // 젬 묶음
            _ui.SetText("GemHead", Localize.FromTable("ui.store.gem.title", "젬 묶음"));
            for (int i = 0; i < Gems.Length; i++)
            {
                var root = _ui.Find($"Gem{i}");
                var prod = store.Find(Gems[i]);
                if (root == null || prod == null) continue;
                In(root, "GemFirst", store.IsFirstDouble(prod));
                Text(root, "GemFirstText", Localize.FromTable("ui.store.first_double", "첫 구매 2배"));
                Text(root, "GemAmount", Amount(prod.Rewards, "gem").ToString("N0"));
                Text(root, "GemPrice", store.PriceLabel(prod));
            }

            // 골드
            _ui.SetText("GoldHead", Localize.FromTable("ui.store.gold.title", "골드"));
            _ui.SetText("ShopLegalText", Localize.FromTable("ui.store.legal", "특정상거래법 · 자금결제법 표기"));
            _ui.SetText("ShopRestoreText", Localize.FromTable("ui.store.restore", "구매 복원"));
            for (int i = 0; i < Golds.Length; i++)
            {
                var root = _ui.Find($"Gold{i}");
                var prod = store.Find(Golds[i]);
                if (root == null || prod == null) continue;
                int left = store.LeftCount(prod, out int max);
                string label = string.Format(Localize.FromTable("ui.store.price_gem", "젬 {0}"), store.PriceLabel(prod));
                if (max > 0) label += $" · <color=#FFD94A>{string.Format(Localize.FromTable("ui.store.today_count", "오늘 {0}/{1}"), Math.Max(0, left), max)}</color>";
                Text(root, "GoldLabel", label);
            }
        }

        // ── 구매 확인 · 획득 ───────────────────────────────────

        private void Ask(string productId)
        {
            if (string.IsNullOrEmpty(productId) || _busy || !CoreModule.TryGet<IStoreService>(out var store)) return;
            var prod = store.Find(productId);
            if (prod == null) return;
            if (store.IsSoldOut(prod))
            {
                SystemPopup.Show(Localize.FromTable("ui.store.soldout", "오늘은 다 샀어요."), null, Localize.Get("ui.common.ok"), null);
                return;
            }
            _pending = productId;
            GameSound.Cue("ui.tap");
            _ui.SetActive("StoreConfirm", true);
            _ui.Find("StoreConfirm").SetAsLastSibling();
            _ui.SetText("StoreConfirmTitle", Localize.FromTable("ui.store.confirm.title", "구매 확인"));
            _ui.SetText("StoreConfirmName", Localize.Get(prod.NameKey));
            var art = _ui.Get<Image>("StoreConfirmArt");
            if (art != null)
            {
                art.sprite = Keyed(_products, prod.Id) ?? (prod.Kind == "chest" ? Keyed(_chests, prod.Id.Substring(6)) : null);
                art.enabled = art.sprite != null;
                art.preserveAspect = true;
            }
            var lines = Lines(prod.Rewards);
            // 시안(S05)은 3줄 기준 — 4줄(입문 패키지 Ⅱ · Ⅲ)이면 줄을 좁혀 「1회 한정」 줄과 겹치지 않게 한다
            float step = lines.Count > 3 ? 45f : 57f;
            for (int i = 0; i < 4; i++)
            {
                var row = _ui.Find($"StoreConfirmRow{i}");
                if (row == null) continue;
                bool on = i < lines.Count;
                row.gameObject.SetActive(on);
                if (!on) continue;
                PlaceConfirmRow((RectTransform)row, i, step, lines.Count > 3 ? 40f : 0f);
                var (icon, name, amount) = lines[i];
                var img = Img(row, "RowIcon");
                if (img != null) { img.sprite = icon; img.enabled = icon != null; img.preserveAspect = true; }
                Text(row, "RowName", name);
                Text(row, "RowAmount", amount);
            }
            string limit = prod.Limit == "once" ? Localize.FromTable("ui.store.once", "1회 한정") : string.Empty;
            if (prod.Show.StartsWith("days_since_install"))
                limit += $" · <color=#FFD94A>{string.Format(Localize.FromTable("ui.store.days_left", "{0}일 남음"), store.StarterDaysLeft)}</color>";
            _ui.SetText("StoreConfirmLimit", limit);
            _ui.SetText("StoreConfirmBuyText", string.Format(Localize.FromTable("ui.store.confirm.buy", "{0} 구매"),
                prod.IsIap ? store.PriceLabel(prod) : string.Format(Localize.FromTable("ui.store.price_gem", "젬 {0}"), store.PriceLabel(prod))));
            _ui.SetText("StoreConfirmCloseText", Localize.FromTable("ui.store.confirm.close", "닫기"));
            _ui.SetText("StoreConfirmNote", prod.IsIap ? Localize.FromTable("ui.store.confirm.note", "결제는 스토어 계정으로 진행됩니다") : string.Empty);
        }

        private void PlaceConfirmRow(RectTransform row, int index, float step, float height)
        {
            if (float.IsNaN(_confirmRowTop))
            {
                var first = _ui.Find("StoreConfirmRow0") as RectTransform;
                if (first == null) return;
                _confirmRowHeight = first.sizeDelta.y;
                _confirmRowTop = first.anchoredPosition.y + _confirmRowHeight * 0.5f;
            }
            float h = height > 0f ? height : _confirmRowHeight;
            row.sizeDelta = new Vector2(row.sizeDelta.x, h);
            row.anchoredPosition = new Vector2(row.anchoredPosition.x, _confirmRowTop - index * step - h * 0.5f);
        }

        private void CloseConfirm()
        {
            _pending = null;
            _ui.SetActive("StoreConfirm", false);
        }

        private void OnConfirmBuy()
        {
            if (string.IsNullOrEmpty(_pending) || _busy) return;
            BuyAsync(_pending).Forget();   // fire-and-forget: 결과는 획득 창이 보인다
        }

        private async UniTaskVoid BuyAsync(string id)
        {
            if (!CoreModule.TryGet<IStoreService>(out var store)) return;
            _busy = true;
            try
            {
                var (outcome, result) = await store.BuyAsync(id, this.GetCancellationTokenOnDestroy());
                CloseConfirm();
                if (outcome == PurchaseOutcome.NotEnoughGem)
                {
                    SystemPopup.Show(Localize.FromTable("ui.store.no_gem", "젬이 모자라요. 젬 묶음을 볼까요?"),
                        () => Show(Page.Currency), Localize.FromTable("ui.store.go_gem", "젬 사러 가기"), Localize.Get("ui.common.cancel"));
                    return;
                }
                if (outcome != PurchaseOutcome.Granted || result == null) return;
                GameSound.Cue("run.gold");
                ShowReward(result);
            }
            finally { _busy = false; }
        }

        private void ClaimCard(int i)
        {
            if (!CoreModule.TryGet<IStoreService>(out var store)) return;
            if (store.TryClaimCard(CardKeys[i], out var result)) { GameSound.Cue("run.gold"); ShowReward(result); }
        }

        /// <summary>획득 창 — 산 것 · 연 상자에서 나온 골드 · 조각까지 칸으로(시안 S07).</summary>
        private void ShowReward(RewardResult result)
        {
            var cells = new List<(Sprite icon, string label)>();
            foreach (var l in result.Lines)
            {
                if (l.Kind == RewardKind.Card || l.Kind == RewardKind.PassXp || l.Kind == RewardKind.PassPremium) continue;
                cells.Add((IconOf(l.Kind, l.Key), LabelOf(l.Kind, l.Key, l.Amount)));
            }
            CoreModule.TryGet<IPlayerDataService>(out var p);
            foreach (var c in result.OpenedChests)
            {
                cells.Add((_goldIcon, string.Format(Localize.FromTable("ui.store.reward.gold", "골드 {0}"), c.Gold.ToString("N0"))));
                for (int k = 0; k < c.ShardHostKeys.Length; k++)
                {
                    var host = p?.GetHost(c.ShardHostKeys[k]);
                    cells.Add((Keyed(_faces, c.ShardHostKeys[k]), string.Format(Localize.FromTable("ui.store.reward.shard", "{0} 조각 ×{1}"),
                        host != null ? host.DisplayName : c.ShardHostKeys[k], c.ShardCounts[k])));
                }
            }
            _ui.SetActive("StoreReward", true);
            _ui.Find("StoreReward").SetAsLastSibling();
            _ui.SetText("StoreRewardTitle", Localize.FromTable("ui.store.reward.title", "획득!"));
            _ui.SetText("StoreRewardDone", Localize.FromTable("ui.store.reward.done", "구매 완료"));
            _ui.SetText("StoreRewardOkText", Localize.Get("ui.common.ok"));
            for (int i = 0; i < 6; i++)
            {
                var cell = _ui.Find($"StoreRewardCell{i}");
                if (cell == null) continue;
                bool on = i < cells.Count;
                cell.gameObject.SetActive(on);
                if (!on) continue;
                var img = Img(cell, "CellIcon");
                if (img != null) { img.sprite = cells[i].icon; img.enabled = img.sprite != null; img.preserveAspect = true; }
                Text(cell, "CellLabel", cells[i].label);
            }
        }

        // ── 글 · 그림 ─────────────────────────────────────────

        private List<(Sprite, string, string)> Lines(string rewards)
        {
            var list = new List<(Sprite, string, string)>();
            if (string.IsNullOrEmpty(rewards)) return list;
            foreach (var raw in rewards.Split(';'))
            {
                var part = raw.Trim();
                int c = part.IndexOf(':');
                if (c < 0) continue;
                string kind = part.Substring(0, c), val = part.Substring(c + 1);
                var k = KindOf(kind);
                if (k == null) continue;
                int n = int.TryParse(val, out var v) ? v : 1;
                string key = val;
                if (kind == "chest")
                {
                    int star = val.IndexOf('*');
                    key = star >= 0 ? val.Substring(0, star) : val;
                    n = star >= 0 && int.TryParse(val.Substring(star + 1), out var m) ? m : 1;
                }
                if (kind == "goldrun") n = GoldRun(n);
                string name = kind == "chest" ? Localize.Get($"chest.{key}.name") : NameOf(k.Value);
                list.Add((IconOf(k.Value, key), name, n.ToString("N0")));
            }
            return list;
        }

        private string Describe(string rewards)
        {
            var parts = new List<string>();
            foreach (var (_, name, amount) in Lines(rewards)) parts.Add($"{name} {amount}");
            return string.Join(" · ", parts);
        }

        private static RewardKind? KindOf(string kind) => kind switch
        {
            "gem" => RewardKind.Gem, "gold" => RewardKind.Gold, "goldrun" => RewardKind.Gold, "essence" => RewardKind.Essence,
            "ticket" => RewardKind.Ticket, "stamina" => RewardKind.Stamina, "chest" => RewardKind.Chest,
            _ => null,
        };

        private static string NameOf(RewardKind k) => k switch
        {
            RewardKind.Gem => Localize.FromTable("ui.store.r.gem", "젬"),
            RewardKind.Gold => Localize.FromTable("ui.store.r.gold", "골드"),
            RewardKind.Essence => Localize.FromTable("ui.store.r.essence", "정수"),
            RewardKind.Ticket => Localize.FromTable("ui.store.r.ticket", "소환권"),
            RewardKind.Stamina => Localize.FromTable("ui.store.r.stamina", "행동력"),
            _ => string.Empty,
        };

        private string LabelOf(RewardKind k, string key, int n)
            => k == RewardKind.Chest ? Localize.Get($"chest.{key}.name") : $"{NameOf(k)} {n:N0}";

        private Sprite IconOf(RewardKind k, string key) => k switch
        {
            RewardKind.Gem => _gemIcon,
            RewardKind.Gold => _goldIcon,
            RewardKind.Essence => _essenceIcon,
            RewardKind.Ticket => _ticketIcon,
            RewardKind.Stamina => _staminaIcon,
            RewardKind.Chest => Keyed(_chests, key),
            _ => null,
        };

        private static int GoldRun(int n)
        {
            if (!CoreModule.TryGet<IPlayerDataService>(out var p) || !p.IsReady) return n;
            int ch = Mathf.Clamp(Math.Max(1, p.UnlockedChapter), 1, Math.Max(1, p.ChapterCount));
            return n * Math.Max(1, p.ChapterInfo(ch).RunGold);
        }

        private static int Amount(string rewards, string kind)
        {
            foreach (var part in (rewards ?? string.Empty).Split(';'))
                if (part.StartsWith(kind + ":") && int.TryParse(part.Substring(kind.Length + 1), out var v)) return v;
            return 0;
        }

        private static StoreProduct Contains(IReadOnlyList<StoreProduct> list, string id)
        {
            for (int i = 0; i < list.Count; i++) if (list[i].Id == id) return list[i];
            return null;
        }

        private static Sprite Keyed(KeyedSprite[] list, string key)
        {
            if (list == null || string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < list.Length; i++) if (list[i].Key == key) return list[i].Sprite;
            return null;
        }

        private Transform Find(Transform root, string name) => _ui.Find(root, name);

        private Image Img(Transform root, string name)
        {
            var t = Find(root, name);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private void In(Transform root, string name, bool on)
        {
            var t = Find(root, name);
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }

        private void Text(Transform root, string name, string value)
        {
            var node = Find(root, name);
            var t = node != null ? node.GetComponent<TextMeshProUGUI>() : null;
            if (t != null) t.text = value;
        }
    }
}
