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
        [SerializeField] private Sprite _priceGem, _priceIap, _priceSold;
        [SerializeField] private Sprite[] _packArts = Array.Empty<Sprite>();   // 이름으로 찾는다(rb_art_shop_*)
        [SerializeField] private Sprite[] _icons = Array.Empty<Sprite>();      // 이름으로 찾는다(rb_icon_*_96)

        private static readonly string[] Tabs = { "best", "today", "chest", "currency" };
        private const float Pad = 22f, Gap = 12f;
        private static readonly Color TabOn = new(1f, 1f, 1f), TabOff = new(0.70f, 0.74f, 0.84f);

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
                _ui.SetActive($"ShpTab{i}On", Tabs[i] == _tab);
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
                          null, !state.ClaimedToday, () => Claim(store, key));
            }
            else PriceButton(c, "ShpContractBuy", store, p);
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
                At(c, Pad + (rt.sizeDelta.x + Gap) * (i % 3), _y + (rt.sizeDelta.y + Gap) * (i / 3));
                h = rt.sizeDelta.y;
                var p = items[i];
                var lines = Parse(p.Rewards);
                SetImage(c, "ShpCellIcon", lines.Count > 0 ? IconOf(lines[0].kind, lines[0].key) : null);
                TextIn(c, "ShpCellName", Localize.Get(p.NameKey));
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
                if (s.Bought) SetButton(b, _priceSold, Localize.Get("ui.shop.daily.bought"), null, false, null);
                else if (s.Price <= 0) SetButton(b, _priceIap, Localize.Get("ui.shop.daily.free"), null, true, () => BuyDaily(daily, slot));
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
            if (store.IsSoldOut(p)) { SetButton(b, _priceSold, Localize.Get("ui.store.sold"), null, false, null); return; }
            bool gem = p.PriceType == "gem";
            SetButton(b, gem ? _priceGem : _priceIap, store.PriceLabel(p), gem ? Find(_icons, "rb_icon_currency_gem_96") : null, true,
                      () => Confirm(store, p));
        }

        private void SetButton(Transform b, Sprite face, string label, Sprite icon, bool on, UnityEngine.Events.UnityAction act)
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
            string note = p.PriceType == "gem"
                ? string.Format(Localize.FromTable("ui.rb.notice.gem_have", "보유 젬 {0:N0}"), _player.Gem)
                : Localize.FromTable("ui.rb.notice.store_pay", "결제는 스토어 계정으로 진행됩니다");
            _shell.Notice.ShowConfirm(icon, string.Format(Localize.FromTable("ui.rb.notice.buy_name", "{0} 구매"), Localize.Get(p.NameKey)), store.PriceLabel(p), note,
                                      () => BuyAsync(store, p.Id).Forget());   // fire-and-forget: 결과가 오면 보상 창으로
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
                _shell.Notice.ShowReward(result);
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
