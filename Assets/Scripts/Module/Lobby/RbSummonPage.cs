using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
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
    /// RE:BORN 소환 탭(시안 Screens/summon · summon-result-1/10/jackpot · summon-rates · summon-records).
    /// 뽑기는 IGachaService — 값을 받고 · 지급하고 · 저장한 뒤 결과가 온다. 이 화면은 보여 주기만 한다.
    /// 등급은 R · SR · SSR 로 적는다(PD 2026-10-23 — 시안의 A·B·C급 표기는 쓰지 않는다).
    /// </summary>
    public sealed class RbSummonPage : MonoBehaviour
    {
        [SerializeField] private Sprite[] _cardFrames = Array.Empty<Sprite>();   // HostGrade 순서(R · SR · SSR)
        [SerializeField] private Sprite[] _rarityTags = Array.Empty<Sprite>();
        [SerializeField] private Sprite _iconGem, _iconTicket;

        private const int ResultSlots = 10;

        private UIBinder _ui;
        private IPlayerDataService _player;
        private RbLobbyUI _shell;
        private SpriteAtlas _faces;
        private bool _busy;
        private int _lastCount = 10;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _ui.OnClick("SumBack", () => _shell.SelectTab("Battle"));
            _ui.OnClick("SumHelp", () => _shell.ShowGuide("summon"));   // 시안 guide
            _ui.OnClick("SumRatesButton", () => ShowSub("SumRates"));
            _ui.OnClick("SumRecordsButton", () => ShowSub("SumRecords"));
            _ui.OnClick("SumRatesBack", () => ShowSub(null));
            _ui.OnClick("SumRecordsBack", () => ShowSub(null));
            _ui.OnClick("SumPull1", () => Pull(1));
            _ui.OnClick("SumPull10", () => Pull(10));
            _ui.OnClick("SumResultOk", () => ShowSub(null));
            _ui.OnClick("SumResultAgain", () => Pull(_lastCount));
            _ui.SetActive("SumCardTemplate", false);
        }

        public void Open(IPlayerDataService player, SpriteAtlas faces)
        {
            _player = player;
            _faces = faces;
            gameObject.SetActive(true);
            ShowSub(null);
        }

        /// <summary>안의 쪽(결과 · 확률 · 기록) — null 이면 메인.</summary>
        private void ShowSub(string sub)
        {
            foreach (var s in new[] { "SumResult", "SumRates", "SumRecords" }) _ui.SetActive(s, s == sub);
            _ui.SetActive("SumMain", sub == null);
            if (sub == null) Refresh();
            else if (sub == "SumRates") FillRates();
            else if (sub == "SumRecords") FillRecords();
        }

        public bool Back()
        {
            if (_ui == null) return false;
            foreach (var s in new[] { "SumResult", "SumRates", "SumRecords" })
                if (_ui.Find(s) is Transform t && t.gameObject.activeSelf) { ShowSub(null); return true; }
            return false;
        }

        // ── 메인 ────────────────────────────────────────────

        private void Refresh()
        {
            if (!CoreModule.TryGet<IGachaService>(out var g) || !g.IsReady || _player == null) return;
            var pick = _player.GetHost(g.PickupHost);
            // 픽업 몸이 아직 몸 목록에 없으면(히든 · 미보유 표 밖) 이름만 표에서 꺼낸다
            _ui.SetText("SumPickupHost", pick != null ? $"{HostGradeLabel.Of(pick.Grade)} {pick.DisplayName}"
                                       : string.IsNullOrEmpty(g.PickupHost) ? string.Empty : Localize.FromTable($"host.{g.PickupHost}.name", g.PickupHost));
            _ui.SetText("SumPickupRule", string.Format(Localize.FromTable("ui.rb.summon.pickup_rule", "SSR 이 나오면 <color=#FFC93D>{0}%</color> 확률로 픽업 호스트"), g.PickupSharePct));
            var left = g.PickupLeft;
            _ui.SetText("SumPickupLeft", string.Format(Localize.FromTable("ui.rb.summon.left", "남은 {0}일 {1}시간"), Math.Max(0, left.Days), Math.Max(0, left.Hours)));
            _ui.SetText("SumPity", string.Format(Localize.FromTable("ui.rb.summon.pity", "대박까지 <color=#FFFFFF>{0}</color> /{1}"), g.PityCount, g.PityMax));
            if (_ui.Find("SumPityFill") is RectTransform fill)
                fill.anchorMax = new Vector2(Mathf.Clamp01(g.PityMax > 0 ? (float)g.PityCount / g.PityMax : 0f), 1f);
            _ui.SetText("SumGem", _player.Gem.ToString("N0"));
            _ui.SetText("SumTickets", string.Format(Localize.FromTable("ui.rb.summon.tickets", "보유 소환권 <color=#FFFFFF>{0}</color>"), _player.Tickets));

            var pay1 = PayFor(g, 1);
            _ui.SetText("SumPull1Text", pay1 == GachaPay.Free ? Localize.FromTable("ui.rb.summon.free", "오늘 무료 1회")
                                       : string.Format(Localize.FromTable("ui.rb.summon.pull", "{0}회 소환"), 1) + "  " + Price(g, 1, pay1));
            var pay10 = PayFor(g, 10);
            _ui.SetText("SumPull10Text", string.Format(Localize.FromTable("ui.rb.summon.pull", "{0}회 소환"), 10) + "  " + Price(g, 10, pay10));
        }

        private GachaPay PayFor(IGachaService g, int count)
        {
            if (count == 1 && g.FreeAvailable) return GachaPay.Free;
            return _player != null && _player.Tickets >= g.CostTicket(count) ? GachaPay.Ticket : GachaPay.Gem;
        }

        /// <summary>「🎫1」 / 「💎3,000」 — 글꼴에 그림 글자가 없어 소환권 · 젬은 낱말로(값만 숫자).</summary>
        private string Price(IGachaService g, int count, GachaPay pay) => pay == GachaPay.Ticket
            ? string.Format(Localize.FromTable("ui.rb.summon.price_ticket", "소환권 {0}"), g.CostTicket(count))
            : string.Format(Localize.FromTable("ui.rb.summon.price_gem", "젬 {0:N0}"), g.CostGem(count));

        // ── 뽑기 ────────────────────────────────────────────

        private void Pull(int count)
        {
            if (_busy || !CoreModule.TryGet<IGachaService>(out var g) || !g.IsReady) return;
            _lastCount = count;
            PullAsync(g, count, PayFor(g, count)).Forget();   // fire-and-forget: 결과가 오면 결과 쪽을 연다
        }

        private async UniTaskVoid PullAsync(IGachaService g, int count, GachaPay pay)
        {
            _busy = true;
            try
            {
                var (pulls, failKey) = await g.PullAsync(count, pay);
                if (this == null) return;
                // 젬이 모자라면 공용 「젬이 모자라요」 창(시안 summon-poor · gem-short) — 젬 사러 가기 → 상점 재화 탭
                if (pulls == null && failKey == "ui.summon.fail.gem") { _shell.Notice.ShowGemShort(); return; }
                if (pulls == null) { Toast(Localize.Get(failKey)); return; }
                GameSound.Cue("run.card");
                ShowResult(pulls, count);
                _shell.RefreshHeader();
            }
            finally { _busy = false; }
        }

        private readonly List<Transform> _cards = new();

        private void ShowResult(IReadOnlyList<GachaPull> pulls, int count)
        {
            ShowSub("SumResult");
            bool jackpot = false;
            int essence = 0;
            for (int i = 0; i < pulls.Count; i++) { jackpot |= pulls[i].Jackpot; essence += pulls[i].ToEssence; }
            _ui.SetText("SumResultCount", string.Format(Localize.FromTable("ui.rb.summon.count", "{0}회 소환"), count));
            _ui.SetText("SumResultTitle", jackpot ? Localize.FromTable("ui.rb.summon.jackpot", "대박!") : Localize.FromTable("ui.rb.summon.result", "소환 결과"));
            _ui.SetActive("SumResultEssence", essence > 0);
            _ui.SetText("SumResultEssenceText", string.Format(Localize.FromTable("ui.rb.summon.essence", "+{0} 영혼 정수"), essence));
            if (CoreModule.TryGet<IGachaService>(out var g))
                _ui.SetText("SumResultAgainText", string.Format(Localize.FromTable("ui.rb.summon.again", "{0}회 더"), count) + "  " + Price(g, count, PayFor(g, count) == GachaPay.Free ? GachaPay.Gem : PayFor(g, count)));

            var tpl = _ui.Find("SumCardTemplate");
            if (tpl == null) return;
            while (_cards.Count < ResultSlots)
            {
                var c = Instantiate(tpl.gameObject, tpl.parent).transform;
                c.name = $"SumCard{_cards.Count}";
                _cards.Add(c);
                if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.ApplyFonts(c);
            }
            bool single = pulls.Count == 1;
            var tr = (RectTransform)tpl;
            for (int i = 0; i < _cards.Count; i++)
            {
                bool on = i < pulls.Count;
                _cards[i].gameObject.SetActive(on);
                if (!on) continue;
                var rt = (RectTransform)_cards[i];
                // 1회는 가운데 큰 카드 한 장, 10회는 5 × 2
                rt.localScale = Vector3.one * (single ? 2f : 1f);
                rt.anchoredPosition = single ? new Vector2(0f, -150f)
                                             : new Vector2((i % 5 - 2) * (tr.sizeDelta.x + 8f), -(i / 5) * (tr.sizeDelta.y + 10f));
                FillCard(_cards[i], pulls[i]);
            }
        }

        private void FillCard(Transform card, GachaPull p)
        {
            var e = _player?.GetHost(p.HostKey);
            int g = e != null ? (int)e.Grade : 0;
            if (card.GetComponent<Image>() is Image frame && g < _cardFrames.Length) frame.sprite = _cardFrames[g];
            if (_ui.Find(card, "SumCardRarity")?.GetComponent<Image>() is Image tag && g < _rarityTags.Length) tag.sprite = _rarityTags[g];
            _ui.Find(card, "SumCardNew")?.gameObject.SetActive(p.IsNew);
            _ui.Find(card, "SumCardJackpot")?.gameObject.SetActive(p.Jackpot);
            if (_ui.Find(card, "SumCardFace")?.GetComponent<Image>() is Image face)
            {
                face.sprite = _faces != null ? _faces.GetSprite($"face_{p.HostKey}") : null;
                face.enabled = face.sprite != null;
            }
            TextIn(card, "SumCardName", e != null ? e.DisplayName : p.HostKey);
            TextIn(card, "SumCardShards", p.ToEssence > 0 ? string.Format(Localize.FromTable("ui.rb.summon.card_essence", "정수 ×{0}"), p.ToEssence)
                                                          : string.Format(Localize.FromTable("ui.rb.summon.card_shards", "조각 ×{0}"), p.Shards));
        }

        // ── 확률 · 기록 ───────────────────────────────────────

        private void FillRates()
        {
            if (!CoreModule.TryGet<IGachaService>(out var g) || !g.IsReady) return;
            var sb = new StringBuilder();
            foreach (var (key, pct) in g.GradeRates())
                sb.Append(HostGradeLabel.Of(key)).Append("\t<color=#4AE08A>").Append(pct.ToString("0.00")).AppendLine("%</color>");
            _ui.SetText("SumRatesGrades", sb.ToString());
            sb.Clear();
            foreach (var (shards, pct) in g.AmountRates())
                sb.Append(string.Format(Localize.FromTable("ui.rb.summon.card_shards", "조각 ×{0}"), shards))
                  .Append("\t<color=#4AE08A>").Append(pct.ToString("0.00")).AppendLine("%</color>");
            _ui.SetText("SumRatesAmounts", sb.ToString());
            sb.Clear();
            var lines = g.Rates();
            for (int i = 0; i < lines.Count; i++)
            {
                var e = _player?.GetHost(lines[i].HostKey);
                sb.Append(HostGradeLabel.Of(lines[i].Grade)).Append("  ").Append(e != null ? e.DisplayName : lines[i].HostKey)
                  .Append(lines[i].Pickup ? " ★" : string.Empty).Append("\t").Append(lines[i].Percent.ToString("0.00")).AppendLine("%");
            }
            _ui.SetText("SumRatesHosts", sb.ToString());
        }

        private void FillRecords()
        {
            if (!CoreModule.TryGet<IGachaService>(out var g)) return;
            var list = g.History();
            var sb = new StringBuilder();
            for (int i = 0; i < list.Count && i < 100; i++)
            {
                var c = list[i].Split(',');
                if (c.Length < 3) continue;
                var e = _player?.GetHost(c[1]);
                string when = c.Length > 3 && c[3].Length == 8 ? $"{c[3].Substring(0, 2)}/{c[3].Substring(2, 2)} {c[3].Substring(4, 2)}:{c[3].Substring(6, 2)}" : c[0];
                sb.Append(when).Append("  ").Append(e != null ? $"{HostGradeLabel.Of(e.Grade)} {e.DisplayName}" : c[1])
                  .Append("  ×").Append(c[2]).Append(c.Length > 5 && c[5] == "1" ? "  <color=#FFC93D>" + Localize.FromTable("ui.rb.summon.jackpot_tag", "대박") + "</color>" : string.Empty).AppendLine();
            }
            _ui.SetText("SumRecordsList", sb.Length > 0 ? sb.ToString() : Localize.FromTable("ui.rb.summon.no_records", "아직 뽑은 기록이 없어요."));
        }

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
