using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.Shop;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.User;
using GameFramework.Core.Base;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 상점 탭(시안 Screens/shop · shop-items) — 추천(입문 · 챕터 돌파 패키지 · 계약) · 오늘(오늘의 상점 6칸) · 상자 · 재화(젬 · 골드 · 행동력).
    /// 상품 · 값 · 보상은 상점 표(IStoreService) · 오늘의 상점(IDailyShopService) — 화면은 줄을 쌓아 그리기만 한다.
    /// 결제는 지금 가짜 스토어(즉시 성공 — StoreModule). 사기 전에 한 번 묻는다(시안 purchase-confirm).
    /// </summary>
    public sealed class RbShopPage : MonoBehaviour
    {
        [SerializeField] private Sprite _priceGem, _priceIap, _priceSold, _pricePack;
        [SerializeField] private Sprite[] _packArts = Array.Empty<Sprite>();   // 이름으로 찾는다(rb_art_shop_*)
        [SerializeField] private Sprite[] _icons = Array.Empty<Sprite>();      // 이름으로 찾는다(rb_icon_*_96)

        private static readonly string[] Tabs = { "best", "today", "chest", "currency" };
        private const float Pad = 22f, Gap = 12f;
        // 고른 탭은 청록 글자(호스트 도감 거르기 칩과 같다)
        private static readonly Color TabOn = new(0.24f, 0.90f, 1f), TabOff = new(0.70f, 0.74f, 0.84f);
        // 버튼 글자 — 값은 금색 · 패키지(노란 버튼) 위는 짙은 글자 · 구매 · 받기 · 확률 보기는 흰색 · 못 사는 것은 흐린 색
        private static readonly Color PriceText = new(1f, 0.79f, 0.24f), InkText = new(0.16f, 0.11f, 0.03f), DimText = new(0.70f, 0.74f, 0.84f);
        private const float CellH = 312f, ChestCellH = 404f;   // 상자 칸은 「확률 보기」 만큼 높다(시안 shop-items)

        private UIBinder _ui;
        private IPlayerDataService _player;
        private RbLobbyUI _shell;
        private SpriteAtlas _faces;
        private string _tab = "best";
        private RectTransform _content;
        private readonly Dictionary<string, List<Transform>> _pool = new();
        private readonly Dictionary<string, int> _used = new();
        private bool _busy;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _content = _ui.Find("ShpBodyContent") as RectTransform;
            _ui.OnClick("ShpBack", () => _shell.SelectTab("Battle"));
            _ui.OnClick("ShpHelp", () => Toast(Localize.Get("ui.store.legal_body")));
            for (int i = 0; i < Tabs.Length; i++)
            {
                string t = Tabs[i];
                _ui.OnClick($"ShpTab{i}", () => { _tab = t; GameSound.Cue("ui.tap"); Refresh(); });
            }
            foreach (var tpl in new[] { "ShpSectionTpl", "ShpPackTpl", "ShpContractTpl", "ShpCellTpl", "ShpRefreshTpl" }) _ui.SetActive(tpl, false);
        }

        public void Open(IPlayerDataService player, SpriteAtlas faces)
        {
            _player = player;
            _faces = faces;
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>그 탭으로 연다(위 막대 「+」 → 재화).</summary>
        public void ShowTab(string tab)
        {
            _tab = tab;
            Refresh();
        }

        // ── 그리기 — 줄을 위에서부터 쌓는다 ─────────────────────────

        private float _y;

        private void Refresh()
        {
            if (_content == null || _player == null) return;
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (_ui.Get<TextMeshProUGUI>($"ShpTab{i}Text") is TextMeshProUGUI t) t.color = Tabs[i] == _tab ? TabOn : TabOff;
            }
            foreach (var list in _pool.Values) foreach (var t in list) t.gameObject.SetActive(false);
            _used.Clear();
            _y = 12f;
            CoreModule.TryGet<IStoreService>(out var store);
            switch (_tab)
            {
                case "best": DrawBest(store); break;
                case "today": DrawToday(); break;
                case "chest": DrawGrid(store, "chest", null, "ui.rb.shop.chest_title", "ui.rb.shop.chest_sub"); break;
                default:
                    DrawGrid(store, "currency", "gem", "ui.rb.shop.gem_title", null);
                    DrawGrid(store, "currency", "gold", "ui.rb.shop.gold_title", null);
                    DrawGrid(store, "currency", "stamina", "ui.rb.shop.stamina_title", null);
                    break;
            }
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, _y + 30f);
        }

        private Transform Take(string tpl)
        {
            if (!_pool.TryGetValue(tpl, out var list)) _pool[tpl] = list = new List<Transform>();
            _used.TryGetValue(tpl, out int n);
            _used[tpl] = n + 1;
            if (n < list.Count) { list[n].gameObject.SetActive(true); return list[n]; }
            var src = _ui.Find(tpl);
            var c = Instantiate(src.gameObject, _content).transform;
            c.name = tpl.Replace("Tpl", string.Empty) + list.Count;
            c.gameObject.SetActive(true);
            list.Add(c);
            if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.ApplyFonts(c);
            return c;
        }

        private static void At(Transform t, float x, float y)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        private void Section(string titleKey, string subKey)
        {
            var s = Take("ShpSectionTpl");
            At(s, Pad, _y);
            TextIn(s, "ShpSectionTitle", Localize.Get(titleKey));
            TextIn(s, "ShpSectionSub", subKey != null ? Localize.Get(subKey) : string.Empty);
            _y += ((RectTransform)s).sizeDelta.y + 6f;
        }

        // ── 추천 — 패키지(큰 판) · 계약(반 칸 둘) ─────────────────────

        private void DrawBest(IStoreService store)
        {
            if (store == null || !store.IsReady) return;
            var list = store.Visible("currency");
            foreach (var p in list)
            {
                if (p.Kind != "package") continue;
                var card = Take("ShpPackTpl");
                At(card, Pad, _y);
                FillPack(card, store, p);
                _y += ((RectTransform)card).sizeDelta.y + Gap;
            }
            float x = Pad;
            float h = 0f;
            foreach (var p in list)
            {
                if (p.Kind != "card") continue;
                var c = Take("ShpContractTpl");
                At(c, x, _y);
                FillContract(c, store, p);
                h = ((RectTransform)c).sizeDelta.y;
                x += ((RectTransform)c).sizeDelta.x + Gap;
            }
            _y += h + Gap;
        }

        private void FillPack(Transform card, IStoreService store, StoreProduct p)
        {
            TextIn(card, "ShpPackName", Localize.Get(p.NameKey));
            string art = p.Id.StartsWith("starter") ? $"rb_art_shop_{p.Id}" : "rb_art_shop_chapter_clear";
            SetImage(card, "ShpPackArt", Find(_packArts, art));
            int days = p.Id.StartsWith("starter") ? store.StarterDaysLeft : 0;
            TextIn(card, "ShpPackTime", days > 0 ? Localize.Format("ui.store.days_left", days) + " · " + Localize.Get("ui.store.once")
                                                 : Localize.Get("ui.store.once"));
            _ui.Find(card, "ShpPackBadge")?.gameObject.SetActive(p.Badge == "best");
            var lines = Parse(p.Rewards);
            for (int i = 0; i < 4; i++)
            {
                bool on = i < lines.Count;
                _ui.Find(card, $"ShpPackR{i}")?.gameObject.SetActive(on);
                if (!on) continue;
                var r = _ui.Find(card, $"ShpPackR{i}");
                SetImage(r, $"ShpPackR{i}Icon", IconOf(lines[i].kind, lines[i].key));
                TextIn(r, $"ShpPackR{i}Value", lines[i].amount.ToString("N0"));
                TextIn(r, $"ShpPackR{i}Label", LabelOf(lines[i].kind, lines[i].key));
            }
            PriceButton(card, "ShpPackBuy", store, p);
        }

        private void FillContract(Transform c, IStoreService store, StoreProduct p)
        {
            TextIn(c, "ShpContractName", Localize.Get(p.NameKey));
            TextIn(c, "ShpContractDesc", Localize.Get(p.DescKey));
            string key = p.Rewards.StartsWith("card:") ? p.Rewards.Substring(5) : p.Id;
            var state = store.Card(key);
            var b = _ui.Find(c, "ShpContractBuy");
            if (state.Active)
            {
                // 계약 중 — 오늘 몫 받기 / 받음(남은 날)
                SetButton(b, state.ClaimedToday ? _priceSold : _priceIap,
                          state.ClaimedToday ? Localize.Format("ui.store.days_left", state.DaysLeft) : Localize.FromTable("ui.rb.shop.claim", "오늘 받기"),
                          null, !state.ClaimedToday, () => Claim(store, key), state.ClaimedToday ? DimText : Color.white);
            }
            // 시안: 계약은 값 대신 「구매」 — 값은 구매 확인 창에서 보인다
            else SetButton(b, _priceIap, Localize.FromTable("ui.rb.shop.buy", "구매"), null, true, () => Confirm(store, p), Color.white);
        }

        // ── 상자 · 재화 — 3칸 줄 ─────────────────────────────────

        private void DrawGrid(IStoreService store, string tab, string kind, string titleKey, string subKey)
        {
            if (store == null || !store.IsReady) return;
            var items = new List<StoreProduct>();
            foreach (var p in store.Visible(tab)) if (kind == null || p.Kind == kind) items.Add(p);
            if (items.Count == 0) return;
            Section(titleKey, subKey);
            float h = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                var c = Take("ShpCellTpl");
                var rt = (RectTransform)c;
                var p = items[i];
                bool chest = p.Kind == "chest";
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, chest ? ChestCellH : CellH);
                At(c, Pad + (rt.sizeDelta.x + Gap) * (i % 3), _y + (rt.sizeDelta.y + Gap) * (i / 3));
                h = rt.sizeDelta.y;
                var lines = Parse(p.Rewards);
                SetImage(c, "ShpCellIcon", lines.Count > 0 ? IconOf(lines[0].kind, lines[0].key) : null);
                // 재화는 이름 대신 실제 양(「골드 한 줌」 이 얼만지 안 보였다 — PD 2026-10-26). 상자는 이름
                TextIn(c, "ShpCellName", tab == "currency" ? Contents(store, p, false) : Localize.Get(p.NameKey));
                var odds = _ui.Find(c, "ShpCellOdds");
                if (odds != null)
                {
                    odds.gameObject.SetActive(chest);
                    if (chest) SetButton(odds, _priceIap, Localize.FromTable("ui.store.chest.rate", "확률 보기"), null, true, () => ShowOdds(p), Color.white);
                }
                int left = store.LeftCount(p, out int max);
                TextIn(c, "ShpCellSub", store.IsFirstDouble(p) ? $"<color=#FFC93D>{Localize.Get("ui.store.first_double")}</color>"
                                        : left >= 0 ? Localize.Format("ui.store.today_count", max - left, max) : string.Empty);
                PriceButton(c, "ShpCellBuy", store, p);
            }
            _y += (h + Gap) * ((items.Count + 2) / 3) + 8f;
        }

        // ── 오늘 — 오늘의 상점 6칸 + 새로고침 ───────────────────────

        private void DrawToday()
        {
            if (!CoreModule.TryGet<IDailyShopService>(out var daily)) return;
            Section("ui.shop.daily.title", null);
            var left = daily.UntilReset;
            TextIn(_pool["ShpSectionTpl"][_used["ShpSectionTpl"] - 1], "ShpSectionSub",
                   Localize.Format("ui.shop.daily.reset", $"{(int)left.TotalHours:00}:{left.Minutes:00}"));
            float h = 0f;
            for (int i = 0; i < daily.SlotCount; i++)
            {
                var s = daily.Get(i);
                var c = Take("ShpCellTpl");
                var rt = (RectTransform)c;
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, CellH);
                _ui.Find(c, "ShpCellOdds")?.gameObject.SetActive(false);
                At(c, Pad + (rt.sizeDelta.x + Gap) * (i % 3), _y + (rt.sizeDelta.y + Gap) * (i / 3));
                h = rt.sizeDelta.y;
                var host = s.IsShard ? _player.GetHost(s.Key) : null;
                SetImage(c, "ShpCellIcon", s.IsShard ? (_faces != null ? _faces.GetSprite($"face_{s.Key}") : null)
                                          : s.Kind == DailyShopKind.Chest ? Find(_icons, $"rb_icon_chest_{s.Key}_96") : Find(_icons, "rb_icon_currency_gold_96"));
                TextIn(c, "ShpCellName", s.IsShard ? (host != null ? host.DisplayName : s.Key)
                                       : s.Kind == DailyShopKind.Chest ? Localize.Get($"store.chest_{s.Key}.name")
                                       : Localize.Format("ui.store.price_gold", s.Count.ToString("N0")));
                TextIn(c, "ShpCellSub", s.IsShard ? Localize.FromTable("ui.rb.summon.card_shards", "조각 ×{0}").Replace("{0}", s.Count.ToString()) : string.Empty);
                int slot = i;
                var b = _ui.Find(c, "ShpCellBuy");
                if (s.Bought) SetButton(b, _priceSold, Localize.Get("ui.shop.daily.bought"), null, false, null, DimText);
                else if (s.Price <= 0) SetButton(b, _priceIap, Localize.Get("ui.shop.daily.free"), null, true, () => BuyDaily(daily, slot), Color.white);
                else SetButton(b, _priceGem, s.Price.ToString("N0"),
                               Find(_icons, s.PaidWithGem ? "rb_icon_currency_gem_96" : "rb_icon_currency_gold_96"), true, () => BuyDaily(daily, slot));
            }
            _y += (h + Gap) * ((daily.SlotCount + 2) / 3);
            var r = Take("ShpRefreshTpl");
            At(r, Pad, _y);
            int cost = daily.RefreshGemCost;
            SetButton(_ui.Find(r, "ShpRefreshBuy"), cost > 0 ? _priceGem : _priceSold,
                      cost > 0 ? $"{Localize.Get("ui.shop.daily.refresh")}  {cost:N0}" : Localize.Get("ui.shop.daily.refresh"),
                      cost > 0 ? Find(_icons, "rb_icon_currency_gem_96") : null, cost > 0, () => RefreshDaily(daily));
            _y += ((RectTransform)r).sizeDelta.y + Gap;
        }

        // ── 버튼 · 사기 ──────────────────────────────────────────

        private void PriceButton(Transform root, string node, IStoreService store, StoreProduct p)
        {
            var b = _ui.Find(root, node);
            if (store.IsSoldOut(p)) { SetButton(b, _priceSold, Localize.Get("ui.store.sold"), null, false, null, DimText); return; }
            bool gem = p.PriceType == "gem";
            // 패키지만 노란 큰 버튼(시안 shop) — 위 글자는 짙게
            bool pack = p.Kind == "package";
            SetButton(b, pack ? _pricePack : gem ? _priceGem : _priceIap, store.PriceLabel(p), gem ? Find(_icons, "rb_icon_currency_gem_96") : null, true,
                      () => Confirm(store, p), pack ? InkText : PriceText);
        }

        private void SetButton(Transform b, Sprite face, string label, Sprite icon, bool on, UnityEngine.Events.UnityAction act, Color? textColor = null)
        {
            if (b == null) return;
            if (b.GetComponent<Image>() is Image img && face != null) img.sprite = face;
            var btn = b.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.interactable = on;
            if (act != null) btn.onClick.AddListener(act);
            var t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            var ic = b.Find(b.name + "Icon")?.GetComponent<Image>();
            if (ic != null) { ic.sprite = icon; ic.gameObject.SetActive(icon != null); }
            if (t == null) return;
            t.text = label;
            t.color = textColor ?? PriceText;
            // 아이콘이 있으면 「💎 80」 — 묶음을 버튼 가운데로
            var tr = t.rectTransform;
            float tw = t.GetPreferredValues(label).x;
            float iw = icon != null && ic != null ? ((RectTransform)ic.transform).sizeDelta.x + 6f : 0f;
            tr.sizeDelta = new Vector2(tw + 2f, tr.sizeDelta.y);
            tr.anchoredPosition = new Vector2(iw * 0.5f, tr.anchoredPosition.y);
            if (iw > 0f) ((RectTransform)ic.transform).anchoredPosition = new Vector2(-(tw + iw) * 0.5f + (iw - 6f) * 0.5f, 0f);
        }

        private void Confirm(IStoreService store, StoreProduct p)
        {
            if (_busy) return;
            var lines = Parse(p.Rewards);
            string icon = lines.Count > 0 ? RbRewards.IconNameOf(lines[0].kind, lines[0].key) : "rb_icon_currency_gem_96";
            // 큰 줄 = 받는 것(「젬 60 + 첫 구매 보너스 60」), 아래 줄 = 값(「₩1,200」 · 「젬 30」) — PD 2026-10-26
            // 「구매를 누르면 무엇을 받는지 설명해야지, 가격 1200 은 뭐냐 · 결제는 스토어 계정으로 · 보유 젬은 빼라」
            string price = p.PriceType == "gem" ? $"{LabelOf("gem", null)} {store.PriceLabel(p)}" : store.PriceLabel(p);
            _shell.Notice.ShowConfirm(icon, string.Format(Localize.FromTable("ui.rb.notice.buy_name", "{0} 구매"), Localize.Get(p.NameKey)),
                                      Contents(store, p, true), price,
                                      () => BuyAsync(store, p.Id).Forget());   // fire-and-forget: 결과가 오면 보상 창으로
        }

        /// <summary>
        /// 받는 것 한 줄 — 「골드 3,000」 · 「젬 300 · 소환권 10 · 골드 5,000」. 판 골드(goldrun)는 실제 양으로 바꾼다.
        /// bonus 면 첫 구매 2배 몫을 「+ 첫 구매 보너스 60」 으로 붙인다(첫 구매는 젬을 두 배로 준다 — StoreModule.DoubleGem).
        /// 계약(card:키)은 설명 글을 쓴다.
        /// </summary>
        private string Contents(IStoreService store, StoreProduct p, bool bonus)
        {
            if (p.Rewards.StartsWith("card:")) return Localize.Get(p.DescKey);
            var lines = Parse(p.Rewards);
            var sb = new System.Text.StringBuilder();
            int gem = 0;
            foreach (var (kind, key, amount) in lines)
            {
                if (sb.Length > 0) sb.Append(" · ");
                if (kind == "chest") { sb.Append(LabelOf(kind, key)); if (amount > 1) sb.Append(" ×").Append(amount); continue; }
                int n = kind == "goldrun" ? amount * store.GoldRunUnit : amount;
                if (kind == "gem") gem += n;
                sb.Append(LabelOf(kind, key)).Append(' ').Append(n.ToString("N0"));
            }
            if (bonus && gem > 0 && store.IsFirstDouble(p))
                sb.Append(" + ").Append(string.Format(Localize.FromTable("ui.rb.notice.first_bonus", "첫 구매 보너스 {0}"), gem.ToString("N0")));
            return sb.ToString();
        }

        /// <summary>상자 「확률 보기」 — 표의 골드 · 조각 범위와 조각이 나오는 호스트 등급 확률(무게 → %).</summary>
        private void ShowOdds(StoreProduct p)
        {
            var lines = Parse(p.Rewards);
            string key = lines.Count > 0 && lines[0].kind == "chest" ? lines[0].key : null;
            if (key == null || !CoreModule.TryGet<Game.Module.Common.Chest.IChestService>(out var chests)) return;
            var e = chests.Entry(key);
            if (e == null) return;
            float total = Mathf.Max(1, e.WeightB + e.WeightA + e.WeightS);
            string Pct(int w) => (w * 100f / total).ToString("0.#") + "%";
            var body = string.Format(Localize.FromTable("ui.rb.shop.odds_gold", "골드 {0} ~ {1}"), e.GoldMin.ToString("N0"), e.GoldMax.ToString("N0"))
                       + "\n" + string.Format(Localize.FromTable("ui.rb.shop.odds_shard", "호스트 조각 {0} ~ {1}개 ({2} ~ {3}명)"),
                                              e.ShardMin, e.ShardMax, e.HostKindsMin, e.HostKindsMax)
                       + "\n\n<color=#3EE6FF>" + Localize.FromTable("ui.rb.shop.odds_grade", "조각이 나오는 호스트 등급") + "</color>"
                       + $"\nR {Pct(e.WeightB)} · SR {Pct(e.WeightA)} · SSR {Pct(e.WeightS)}";
            _shell.Notice.ShowInfo($"rb_icon_chest_{key}_96", Localize.Get(p.NameKey), body);
        }

        private async UniTaskVoid BuyAsync(IStoreService store, string id)
        {
            _busy = true;
            try
            {
                var (outcome, result) = await store.BuyAsync(id);
                if (this == null) return;
                if (outcome == PurchaseOutcome.NotEnoughGem) { _shell.Notice.ShowGemShort(); return; }
                if (outcome == PurchaseOutcome.SoldOut) { Toast(Localize.Get("ui.store.soldout")); return; }
                if (outcome != PurchaseOutcome.Granted) return;
                GameSound.Cue("run.shop");
                _shell.Notice.ShowPurchase(result);   // 상자면 상자 열기 쪽
                _shell.RefreshHeader();
                Refresh();
            }
            finally { _busy = false; }
        }

        private void Claim(IStoreService store, string key)
        {
            if (!store.TryClaimCard(key, out var result)) return;
            GameSound.Cue("run.gold");
            _shell.Notice.ShowReward(result);
            _shell.RefreshHeader();
            Refresh();
        }

        private void BuyDaily(IDailyShopService daily, int slot)
        {
            if (!daily.TryBuy(slot, out var fail)) { Toast(Localize.Get(fail)); return; }
            GameSound.Cue("run.shop");
            _player.SaveAsync().Forget();   // fire-and-forget: 산 칸은 바로 남긴다
            _shell.RefreshHeader();
            Refresh();
        }

        private void RefreshDaily(IDailyShopService daily)
        {
            if (!daily.TryRefresh(out var fail)) { Toast(Localize.Get(fail)); return; }
            _player.SaveAsync().Forget();   // fire-and-forget: 새 진열을 바로 남긴다
            _shell.RefreshHeader();
            Refresh();
        }


        private static List<(string kind, string key, int amount)> Parse(string rewards) => RbRewards.Parse(rewards);

        private Sprite IconOf(string kind, string key) => RbRewards.IconOf(_icons, kind, key);

        private static string LabelOf(string kind, string key) => RbRewards.LabelOf(kind, key);

        private static Sprite Find(Sprite[] list, string name) => RbRewards.Find(list, name);

        private void SetImage(Transform root, string node, Sprite s)
        {
            if (_ui.Find(root, node)?.GetComponent<Image>() is not Image img) return;
            img.sprite = s;
            img.enabled = s != null;
        }

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
