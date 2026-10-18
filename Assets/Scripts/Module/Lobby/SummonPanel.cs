using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
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
    /// 소환(조각 뽑기) — 아래 바 SUMMON(기획서 B-11, 시안 `mock_summon_a` · S12~S15, 연출 `guide_summon_fx.md`).
    /// 연출: 불꽃 기둥(그 판 최고 등급 색 — 하늘 B · 보라 A · 금 대박, 속이지 않음) → 결과 칸 차례로 뒤집기.
    /// 0.4초 뒤부터 화면을 누르면 건너뛴다. 결과는 **이미 지급 · 저장된 뒤** 보여 준다(`IGachaService.PullAsync`).
    /// 노드는 `SummonBuilder` 가 세운다.
    /// </summary>
    public sealed class SummonPanel : MonoBehaviour
    {
        [SerializeField] private LobbyShopPanel.KeyedSprite[] _faces = Array.Empty<LobbyShopPanel.KeyedSprite>();
        [SerializeField] private LobbyShopPanel.KeyedSprite[] _portraits = Array.Empty<LobbyShopPanel.KeyedSprite>();
        [SerializeField] private Sprite _frontB, _frontA, _frontJackpot, _back;
        [SerializeField] private Sprite _gemIcon, _ticketIcon;   // 뽑기 기록 「사용」 칸
        [SerializeField] private Sprite[] _pillarCyan = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _pillarPurple = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _pillarGold = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _jackpot = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _flip = Array.Empty<Sprite>();

        /// <summary>
        /// UI v5(PD 2026-10-17 시안 mock_uiv5_summon) — 픽업 없이 기본 소환. 단추는 1회 · 10회 둘이고,
        /// 오늘 무료가 남아 있으면 1회 단추가 무료로 뽑는다(「오늘 무료 1회」 꼬리표). 등급 확률은 판에 늘 보인다.
        /// 빌더 `UiV5Screens.RunSummon` 이 켠다.
        /// </summary>
        [SerializeField] private bool _v5;
        [SerializeField] private Sprite[] _gradeBadges = Array.Empty<Sprite>();   // B · A · S = R · SR · SSR

        private const float FrameSeconds = 0.12f;
        private const float SkipAfter = 0.4f;
        private const int Cells = 10;

        private static readonly Color GradeB = new(0.55f, 0.88f, 1f);
        private static readonly Color GradeA = new(0.86f, 0.6f, 1f);
        private static readonly Color Gold = new(1f, 0.85f, 0.32f);

        private UIBinder _ui;
        private IDisposable _storeToken, _currencyToken;
        private float _tick;
        private bool _busy;
        private bool _skip;
        private float _fxStart;
        private Vector2[] _resultButtons;   // 10회 배치일 때 다시 · 확인 버튼 자리 · 크기(빌더 값)
        private int _lastCount = 1;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("GachaFreeButton", () => Pull(1, GachaPay.Free));
            _ui.OnClick("GachaPull1Button", () =>
            {
                bool free = _v5 && CoreModule.TryGet<IGachaService>(out var gs) && gs.FreeAvailable;
                Pull(1, free ? GachaPay.Free : PayFor(1));
            });
            _ui.OnClick("GachaPull10Button", () => Pull(10, PayFor(10)));
            _ui.OnClick("GachaRateButton", ShowRates);
            _ui.OnClick("GachaHistoryButton", ShowHistory);
            _ui.OnClick("ResultCloseButton", CloseResult);
            _ui.OnClick("ResultAgainButton", () => { CloseResult(); Pull(_lastCount, PayFor(_lastCount)); });
            _ui.OnClick("SummonFxSkip", () => { if (Time.unscaledTime - _fxStart >= SkipAfter) _skip = true; });
            _ui.OnClick("GachaInfoClose", CloseInfo);
            _ui.OnClick("GachaRatesX", CloseInfo);
            _ui.OnClick("GachaHistoryClose", CloseInfo);
            _ui.OnClick("GachaHistoryX", CloseInfo);
        }

        private void OnEnable()
        {
            if (!CoreModule.TryGet<IEventBus>(out var bus)) return;
            _storeToken = bus.Subscribe<StoreChangedEvent>(_ => Refresh());
            _currencyToken = bus.Subscribe<CurrencyChangedEvent>(_ => Refresh());
        }

        private void OnDisable()
        {
            _storeToken?.Dispose();
            _currencyToken?.Dispose();
            _storeToken = _currencyToken = null;
        }

        private void Update()
        {
            _tick += Time.unscaledDeltaTime;
            if (_tick < 1f) return;
            _tick = 0f;
            RefreshTime();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            var nav = transform.parent != null ? transform.parent.Find("BottomNav") : null;
            if (nav != null && transform.GetSiblingIndex() > nav.GetSiblingIndex()) transform.SetSiblingIndex(nav.GetSiblingIndex());
            _ui.SetActive("GachaResultPopup", false);
            _ui.SetActive("GachaInfoPopup", false);
            _ui.SetActive("SummonFx", false);
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        public bool CloseTopPopup()
        {
            if (_busy) { _skip = true; return true; }
            if (_ui.Find("GachaInfoPopup") is Transform i && i.gameObject.activeSelf) { i.gameObject.SetActive(false); return true; }
            if (_ui.Find("GachaResultPopup") is Transform r && r.gameObject.activeSelf) { CloseResult(); return true; }
            return false;
        }

        // ── 그리기 ───────────────────────────────────────────

        private void Refresh()
        {
            _ui.SetText("SummonTitleText", Localize.FromTable("ui.summon.title", "소환"));
            if (!CoreModule.TryGet<IGachaService>(out var g) || !g.IsReady) return;
            CoreModule.TryGet<IPlayerDataService>(out var p);

            var pick = p?.GetHost(g.PickupHost);
            _ui.SetText("PickupLabelText", Localize.FromTable("ui.summon.pickup", "이번 주 픽업:"));
            _ui.SetText("PickupNameText", pick != null ? pick.DisplayName : g.PickupHost);
            _ui.SetText("PickupNoteText", string.Format(Localize.FromTable("ui.summon.pickup_note", "A급이 나오면 {0}% 확률로 픽업 호스트"), g.PickupSharePct));
            var art = _ui.Get<Image>("PickupArt");
            if (art != null)
            {
                art.sprite = Keyed(_portraits, g.PickupHost) ?? Keyed(_faces, g.PickupHost);
                art.enabled = art.sprite != null;
                art.preserveAspect = true;
            }
            RefreshTime();

            int tickets = p?.Tickets ?? 0;
            _ui.SetText("GachaTicketText", string.Format(Localize.FromTable("ui.summon.tickets", "보유 소환권 <color=#FFD94A>{0}</color>"), tickets));

            bool free = g.FreeAvailable;
            _ui.SetActive("GachaFreeButton", true);
            var freeBtn = _ui.Get<Button>("GachaFreeButton");
            if (freeBtn != null) freeBtn.interactable = free;
            _ui.SetText("FreeTagText", Localize.FromTable("ui.summon.today", "오늘"));
            _ui.SetText("FreeTitleText", free ? Localize.FromTable("ui.summon.free", "무료 1회") : Localize.FromTable("ui.summon.free_used", "내일 다시"));
            // FromTable 은 한국어면 코드의 원문을 쓴다 — v5 는 언어팩 글자(「1회 소환」)를 쓴다
            _ui.SetText("Pull1TitleText", _v5 ? Localize.Get("ui.summon.pull1") : Localize.FromTable("ui.summon.pull1", "1회"));
            _ui.SetText("Pull10TitleText", _v5 ? Localize.Get("ui.summon.pull10") : Localize.FromTable("ui.summon.pull10", "10회"));
            _ui.SetText("Pull10NoteText", Localize.FromTable("ui.summon.pull10_note", "A급 1회 보장"));
            _ui.SetText("Pull1PriceText", Price(g, 1));
            _ui.SetText("Pull10PriceText", Price(g, 10));

            _ui.SetText("GachaPityText", string.Format(Localize.FromTable("ui.summon.pity", "대박까지 <color=#FFD94A>{0} / {1}</color>"), g.PityCount, g.PityMax));
            var fill = _ui.Get<Image>("GachaPityFill");
            if (fill != null) fill.fillAmount = g.PityMax > 0 ? Mathf.Clamp01((float)g.PityCount / g.PityMax) : 0f;
            _ui.SetText("GachaRateText", Localize.FromTable("ui.summon.rates", "확률 보기"));
            if (_v5) RefreshV5(g, free);
            _ui.SetText("GachaHistoryText", Localize.FromTable("ui.summon.history", "뽑기 기록"));
        }

        /// <summary>v5 — 등급 확률 판(R · SR · SSR) · 무료 꼬리표. 표에 없는 등급 칸은 숨긴다.</summary>
        private void RefreshV5(IGachaService g, bool free)
        {
            _ui.SetActive("GachaFreeButton", false);
            _ui.SetActive("SummonV5FreeRibbon", free);
            var grades = g.GradeRates();
            for (int i = 0; i < 3; i++)
            {
                int at = -1;
                string key = i == 0 ? "B" : i == 1 ? "A" : "S";
                for (int k = 0; k < grades.Count; k++) if (grades[k].key == key) at = k;
                _ui.SetActive($"SummonV5Badge{i}", at >= 0);
                _ui.SetActive($"SummonV5Rate{i}", at >= 0);
                if (at >= 0) _ui.SetText($"SummonV5Rate{i}", $"{grades[at].pct:0.#}%");
                var badge = _ui.Get<Image>($"SummonV5Badge{i}");
                if (badge != null && i < _gradeBadges.Length) { badge.sprite = _gradeBadges[i]; badge.preserveAspect = true; }
            }
        }

        private void RefreshTime()
        {
            if (!CoreModule.TryGet<IGachaService>(out var g)) return;
            var t = g.PickupLeft;
            _ui.SetText("PickupTimeText", string.Format(Localize.FromTable("ui.summon.left", "남은 <color=#FFD94A>{0}</color>일 <color=#FFD94A>{1}</color>시간"),
                t.Days, t.Hours));
        }

        private static string Price(IGachaService g, int n)
            => string.Format(Localize.FromTable("ui.summon.price", "젬 {0} / 소환권 {1}"), g.CostGem(n).ToString("N0"), g.CostTicket(n));

        private static GachaPay PayFor(int count)
        {
            // 소환권이 있으면 소환권부터 — 젬은 아낀다
            if (CoreModule.TryGet<IGachaService>(out var g) && CoreModule.TryGet<IPlayerDataService>(out var p)
                && p.Tickets >= g.CostTicket(count)) return GachaPay.Ticket;
            return GachaPay.Gem;
        }

        // ── 뽑기 ─────────────────────────────────────────────

        private void Pull(int count, GachaPay pay)
        {
            if (_busy) return;
            PullAsync(count, pay).Forget();   // fire-and-forget: 결과 창이 보인다
        }

        private async UniTaskVoid PullAsync(int count, GachaPay pay)
        {
            if (!CoreModule.TryGet<IGachaService>(out var g)) return;
            _busy = true;
            try
            {
                var (pulls, fail) = await g.PullAsync(count, pay);
                if (pulls == null)
                {
                    if (fail == "ui.summon.fail.gem")
                        SystemPopup.Show(Localize.FromTable(fail, "젬이 모자라요."), null, Localize.Get("ui.common.ok"), null);
                    else if (!string.IsNullOrEmpty(fail))
                        SystemPopup.Show(Localize.Get(fail), null, Localize.Get("ui.common.ok"), null);
                    return;
                }
                _lastCount = count;
                await PlayFxAsync(pulls, this.GetCancellationTokenOnDestroy());
                ShowResult(pulls, count);
            }
            finally
            {
                _busy = false;
                _ui.SetActive("SummonFx", false);
            }
        }

        /// <summary>불꽃 기둥 — 그 판 최고 등급 색. 대박이 있으면 금 빛살까지. 누르면 건너뛴다.</summary>
        private async UniTask PlayFxAsync(IReadOnlyList<GachaPull> pulls, CancellationToken ct)
        {
            bool jackpot = false, anyA = false;
            CoreModule.TryGet<IPlayerDataService>(out var p);
            foreach (var x in pulls)
            {
                jackpot |= x.Jackpot;
                var h = p?.GetHost(x.HostKey);
                anyA |= h != null && h.Grade != HostGrade.B;
            }
            var frames = jackpot ? _pillarGold : anyA ? _pillarPurple : _pillarCyan;
            _skip = false;
            _fxStart = Time.unscaledTime;
            _ui.SetActive("SummonFx", true);
            _ui.Find("SummonFx").SetAsLastSibling();
            GameSound.Cue("ui.play");
            var img = _ui.Get<Image>("SummonFxPillar");
            await Frames(img, frames, ct);
            if (jackpot && !_skip) await Frames(img, _jackpot, ct);
        }

        private async UniTask Frames(Image img, Sprite[] frames, CancellationToken ct)
        {
            if (img == null || frames == null) return;
            img.enabled = true;
            for (int i = 0; i < frames.Length && !_skip; i++)
            {
                img.sprite = frames[i];
                await UniTask.Delay(TimeSpan.FromSeconds(FrameSeconds), ignoreTimeScale: true, cancellationToken: ct);
            }
            img.enabled = false;
        }

        // ── 결과 ─────────────────────────────────────────────

        private void ShowResult(IReadOnlyList<GachaPull> pulls, int count)
        {
            CoreModule.TryGet<IPlayerDataService>(out var p);
            CoreModule.TryGet<IGachaService>(out var g);
            _ui.SetActive("GachaResultPopup", true);
            _ui.Find("GachaResultPopup").SetAsLastSibling();
            _ui.SetText("ResultTitleText", Localize.FromTable("ui.summon.result", "소환 결과"));
            _ui.SetText("ResultCountText", string.Format(Localize.FromTable("ui.summon.result_count", "{0}회 소환"), count));

            // 1회는 가운데 큰 카드 한 장(S12), 10회는 5 × 2 칸(S13)
            bool single = pulls.Count == 1;
            _ui.SetActive("ResultSingle", single);
            foreach (var n in TenOnly) _ui.SetActive(n, !single);
            PlaceResultButtons(single);
            if (single) FillSingle(pulls[0], p);

            int jackpots = 0, aCount = 0, news = 0, essence = 0;
            for (int i = 0; i < Cells; i++)
            {
                var cell = _ui.Find($"ResultCell{i}");
                if (cell == null) continue;
                bool on = i < pulls.Count;
                cell.gameObject.SetActive(on && !single);
                if (!on) continue;
                var x = pulls[i];
                var host = p?.GetHost(x.HostKey);
                var grade = host != null ? host.Grade : HostGrade.B;
                if (x.Jackpot) jackpots++;
                if (grade != HostGrade.B) aCount++;
                if (x.IsNew) news++;
                essence += x.ToEssence;

                var frame = Img(cell, "CellFrame");
                if (frame != null) frame.sprite = x.Jackpot ? _frontJackpot : grade == HostGrade.B ? _frontB : _frontA;
                var face = Img(cell, "CellFace");
                if (face != null)
                {
                    face.sprite = Keyed(_faces, x.HostKey);
                    face.enabled = face.sprite != null;
                    face.preserveAspect = true;
                }
                var gt = Txt(cell, "CellGradeText");
                if (gt != null)
                {
                    gt.text = string.Format(Localize.FromTable("ui.summon.grade", "{0}급"), grade);
                    gt.color = grade == HostGrade.B ? GradeB : GradeA;
                }
                var name = Txt(cell, "CellNameText");
                if (name != null) name.text = host != null ? host.DisplayName : x.HostKey;
                var amt = Txt(cell, "CellAmountText");
                if (amt != null)
                {
                    amt.text = x.ToEssence > 0
                        ? string.Format(Localize.FromTable("ui.summon.essence", "정수 +{0}"), x.ToEssence)
                        : $"×{x.Shards}";
                    amt.color = x.Jackpot ? Gold : Color.white;
                }
                Act(cell, "CellJackpotMark", x.Jackpot);
                Act(cell, "CellPickupMark", x.Pickup);
                Act(cell, "CellNewMark", x.IsNew);
                Act(cell, "CellShardIcon", x.ToEssence == 0);
                var jt = Txt(cell, "CellJackpotText");
                if (jt != null) jt.text = Localize.FromTable("ui.summon.jackpot", "대박");
            }

            var sb = new StringBuilder();
            if (jackpots > 0) sb.Append($"<color=#FFD94A>{Localize.FromTable("ui.summon.jackpot", "대박")} {jackpots}</color>  ·  ");
            sb.Append($"<color=#E09BFF>{string.Format(Localize.FromTable("ui.summon.grade", "{0}급"), "A")} {aCount}</color>");
            if (news > 0) sb.Append($"  ·  <color=#8FE7FF>{Localize.FromTable("ui.summon.new_body", "새 몸")} {news}</color>");
            if (essence > 0) sb.Append($"  ·  <color=#8FE7FF>{string.Format(Localize.FromTable("ui.summon.essence", "정수 +{0}"), essence)}</color>");
            _ui.SetText("ResultSummaryText", sb.ToString());
            _ui.SetText("ResultAgainTitle", string.Format(Localize.FromTable("ui.summon.again", "{0}회 더"), count));
            _ui.SetText("ResultAgainPrice", g != null ? (PayFor(count) == GachaPay.Ticket
                ? string.Format(Localize.FromTable("ui.summon.price_ticket", "소환권 {0}"), g.CostTicket(count))
                : g.CostGem(count).ToString("N0")) : string.Empty);
            _ui.SetText("ResultCloseText", Localize.Get("ui.common.ok"));
            GameSound.Cue(jackpots > 0 ? "run.card" : "run.gold");
            Refresh();
        }

        private void CloseResult() => _ui.SetActive("GachaResultPopup", false);

        private static readonly string[] TenOnly = { "ResultGhost", "ResultTitleText", "ResultCountPlate", "ResultSummary" };

        private void FillSingle(GachaPull x, IPlayerDataService p)
        {
            var host = p?.GetHost(x.HostKey);
            var grade = host != null ? host.Grade : HostGrade.B;
            var frame = _ui.Get<Image>("SingleFrame");
            if (frame != null) frame.sprite = x.Jackpot ? _frontJackpot : grade == HostGrade.B ? _frontB : _frontA;
            var face = _ui.Get<Image>("SingleFace");
            if (face != null)
            {
                face.sprite = Keyed(_faces, x.HostKey) ?? Keyed(_portraits, x.HostKey);   // 시안은 상반신(얼굴) 그림
                face.enabled = face.sprite != null;
                face.preserveAspect = true;
            }
            var gt = _ui.Get<TextMeshProUGUI>("SingleGradeText");
            if (gt != null)
            {
                gt.text = grade.ToString();
                gt.color = grade == HostGrade.B ? GradeB : GradeA;
            }
            _ui.SetText("SingleNameText", host != null ? host.DisplayName : x.HostKey);
            _ui.SetText("SingleAmountText", x.ToEssence > 0
                ? string.Format(Localize.FromTable("ui.summon.essence", "정수 +{0}"), x.ToEssence)
                : string.Format(Localize.FromTable("ui.summon.shards", "조각 × {0}"), x.Shards));
            _ui.SetActive("SingleShardIcon", x.ToEssence == 0);

            int have = p != null ? p.GetShards(x.HostKey) : 0;
            int cost = p != null ? p.MasteryCost(x.HostKey) : 0;
            bool maxed = cost <= 0 && p != null && p.StarsOf(x.HostKey) >= 5;
            // 비용이 0 인데 별이 덜 찼으면 아직 육성 줄에 없는 몸(처음 얻음) — 모은 조각만 보인다
            string progress = cost > 0
                ? string.Format(Localize.FromTable("ui.summon.next_star",
                    "지금 조각 <color=#FFD94A>{0}</color> / {1}  →  다음 성급까지 <color=#FFD94A>{2}</color>"),
                    have, cost, Mathf.Max(0, cost - have))
                : maxed ? Localize.FromTable("ui.summon.star_max", "최대 성급")
                : string.Format(Localize.FromTable("ui.summon.shards_now", "지금 조각 <color=#FFD94A>{0}</color>"), have);
            _ui.SetText("SingleProgressText", progress);
            var fill = _ui.Get<Image>("SingleProgressFill");
            if (fill != null) fill.fillAmount = cost > 0 ? Mathf.Clamp01(have / (float)cost) : maxed ? 1f : 0f;
        }

        /// <summary>1회면 버튼을 시안 S12 자리(빌더의 빈 칸 SingleAgainSlot · SingleCloseSlot)로, 10회면 원래 자리로.</summary>
        private void PlaceResultButtons(bool single)
        {
            if (_ui.Find("ResultAgainButton") is not RectTransform again
                || _ui.Find("ResultCloseButton") is not RectTransform close) return;
            _resultButtons ??= new[] { again.anchoredPosition, again.sizeDelta, close.anchoredPosition, close.sizeDelta };
            if (single && _ui.Find("SingleAgainSlot") is RectTransform a && _ui.Find("SingleCloseSlot") is RectTransform c)
            {
                again.anchoredPosition = a.anchoredPosition;
                again.sizeDelta = a.sizeDelta;
                close.anchoredPosition = c.anchoredPosition;
                close.sizeDelta = c.sizeDelta;
                return;
            }
            again.anchoredPosition = _resultButtons[0];
            again.sizeDelta = _resultButtons[1];
            close.anchoredPosition = _resultButtons[2];
            close.sizeDelta = _resultButtons[3];
        }

        // ── 확률 · 기록(법으로 늘 보이게) ─────────────────────

        private void ShowRates()
        {
            if (!CoreModule.TryGet<IGachaService>(out var g) || !g.IsReady) return;
            CoreModule.TryGet<IPlayerDataService>(out var p);
            OpenInfo(rates: true);
            _ui.SetText("RatesTitle", Localize.FromTable("ui.summon.rates_title", "소환 확률"));
            _ui.SetText("RatesSubTitle", Localize.FromTable("ui.summon.rates", "확률 보기"));
            _ui.SetText("RateHeadGradeText", Localize.FromTable("ui.summon.rate_grade", "등급 확률"));
            _ui.SetText("RateHeadHostText", Localize.FromTable("ui.summon.rate_host", "호스트별 확률"));
            _ui.SetText("RateHeadAmountText", Localize.FromTable("ui.summon.rate_amount", "조각 수량 확률"));

            // 등급 — B · A 순서(표 순서를 따른다)
            var grades = g.GradeRates();
            for (int i = 0; i < 2; i++)
            {
                string node = i == 0 ? "RateRowGradeB" : "RateRowGradeA";
                bool on = i < grades.Count;
                _ui.SetActive(node, on);
                if (!on) continue;
                var (k, pct) = grades[i];
                var c = k == "A" ? GradeA : Color.white;
                RateLine(node, string.Format(Localize.FromTable("ui.summon.grade", "{0}급"), k), $"{pct:0.00}%", c);
            }

            // 호스트 — 같은 등급은 한 줄(「B급 호스트 11명 · 각 6.82%」), 픽업은 따로 금 테 줄
            int bCount = 0, aCount = 0;
            float bEach = 0f, aEach = 0f, pickPct = 0f;
            string pickName = string.Empty;
            foreach (var r in g.Rates())
            {
                if (r.Pickup)
                {
                    pickPct = r.Percent;
                    var h = p?.GetHost(r.HostKey);
                    pickName = h != null ? h.DisplayName : r.HostKey;
                }
                else if (r.Grade == HostGrade.B) { bCount++; bEach = r.Percent; }
                else { aCount++; aEach = r.Percent; }
            }
            RateLine("RateRowHostB", string.Format(Localize.FromTable("ui.summon.rate_hosts", "{0}급 호스트 {1}명"), "B", bCount),
                string.Format(Localize.FromTable("ui.summon.rate_each", "각 {0}"), $"{bEach:0.00}%"), Color.white);
            _ui.SetActive("RateRowPickup", pickName.Length > 0);
            RateLine("RateRowPickup", string.Format(Localize.FromTable("ui.summon.rate_pickup", "픽업 {0}"), pickName),
                $"{pickPct:0.00}%", new Color(1f, 0.55f, 1f), Gold);
            RateLine("RateRowHostA", string.Format(Localize.FromTable("ui.summon.rate_hosts", "{0}급 호스트 {1}명"), "A", aCount),
                string.Format(Localize.FromTable("ui.summon.rate_each", "각 {0}"), $"{aEach:0.00}%"), GradeA);

            var amounts = g.AmountRates();
            for (int i = 0; i < 3; i++)
            {
                string node = $"RateRowAmount{i}";
                bool on = i < amounts.Count;
                _ui.SetActive(node, on);
                if (!on) continue;
                var (n, pct) = amounts[i];
                RateLine(node, string.Format(Localize.FromTable("ui.summon.rate_shards", "조각 ×{0}"), n), $"{pct:0.00}%", Color.white);
            }

            _ui.SetText("RatesGuaranteeText", Localize.FromTable("ui.summon.rate_guarantee",
                "10회 소환 시 <color=#FFD94A>A급 이상 1명</color> 보장"));
            _ui.SetText("RatesPityText", string.Format(Localize.FromTable("ui.summon.rate_pity",
                "천장 <color=#FFD94A>{0}</color> / {1}"), g.PityCount, g.PityMax));
            _ui.SetText("RatesPityNote", Localize.FromTable("ui.summon.rate_pity_note", "천장 횟수는 이월되지 않습니다."));
            _ui.SetText("GachaInfoCloseText", Localize.Get("ui.common.ok"));
        }

        private void RateLine(string node, string label, string value, Color labelColor, Color? valueColor = null)
        {
            var row = _ui.Find(node);
            if (row == null) return;
            var l = Txt(row, "RowLabel");
            if (l != null) { l.text = label; l.color = labelColor; }
            var v = Txt(row, "RowValue");
            if (v != null) { v.text = value; v.color = valueColor ?? labelColor; }
        }

        private readonly List<Transform> _historyRows = new();

        private void ShowHistory()
        {
            if (!CoreModule.TryGet<IGachaService>(out var g)) return;
            CoreModule.TryGet<IPlayerDataService>(out var p);
            OpenInfo(rates: false);
            _ui.SetText("HistoryTitle", Localize.FromTable("ui.summon.history", "뽑기 기록"));
            _ui.SetText("HistorySubText", Localize.FromTable("ui.summon.history_recent", "최근 100회"));
            _ui.SetText("HistoryHeadDate", Localize.FromTable("ui.summon.history_date", "날짜"));
            _ui.SetText("HistoryHeadHost", Localize.FromTable("ui.summon.history_host", "호스트"));
            _ui.SetText("HistoryHeadShard", Localize.FromTable("ui.summon.history_shard", "조각"));
            _ui.SetText("HistoryHeadPay", Localize.FromTable("ui.summon.history_pay", "사용"));
            _ui.SetText("GachaHistoryCloseText", Localize.FromTable("ui.store.confirm.close", "닫기"));

            var list = g.History();
            _ui.SetActive("HistoryEmptyText", list.Count == 0);
            _ui.SetText("HistoryEmptyText", Localize.FromTable("ui.summon.history_empty", "아직 뽑은 기록이 없어요."));
            var content = _ui.Find("HistoryContent") as RectTransform;
            var template = _ui.Find("HistoryRowTemplate");
            if (content == null || template == null) return;
            // 견본을 필요한 만큼 복제해 둔다(한 번 만든 줄은 다시 쓴다)
            while (_historyRows.Count < list.Count)
            {
                var row = Instantiate(template.gameObject, content, false).transform;
                row.name = $"HistoryRow{_historyRows.Count}";
                _historyRows.Add(row);
            }
            float step = template is RectTransform tr ? tr.sizeDelta.y + 4f : 60f;
            for (int i = 0; i < _historyRows.Count; i++)
            {
                var row = _historyRows[i];
                bool on = i < list.Count;
                row.gameObject.SetActive(on);
                if (!on) continue;
                ((RectTransform)row).anchoredPosition = new Vector2(0f, -2f - i * step);
                FillHistoryRow(row, list[i], p);
            }
            content.sizeDelta = new Vector2(content.sizeDelta.x, list.Count * step + 4f);
            var scroll = _ui.Get<ScrollRect>("HistoryScroll");
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>「yyyyMMdd,hostKey,shards[,MMddHHmm,pay,jackpot]」 — 앞 셋만 있는 옛 기록은 날짜만 보인다.</summary>
        private void FillHistoryRow(Transform row, string line, IPlayerDataService p)
        {
            var c = line.Split(',');
            if (c.Length < 3) return;
            var h = p?.GetHost(c[1]);
            string date = c.Length > 3 && c[3].Length == 8
                ? $"{c[3].Substring(0, 2)}.{c[3].Substring(2, 2)}\n{c[3].Substring(4, 2)}:{c[3].Substring(6, 2)}"
                : c[0].Length == 8 ? $"{c[0].Substring(4, 2)}.{c[0].Substring(6, 2)}" : c[0];
            bool jackpot = c.Length > 5 && c[5] == "1";
            var t = Txt(row, "RowDate");
            if (t != null) t.text = date;
            var face = Img(row, "RowFace");
            if (face != null)
            {
                face.sprite = Keyed(_faces, c[1]);
                face.enabled = face.sprite != null;
                face.preserveAspect = true;
            }
            var name = Txt(row, "RowName");
            if (name != null) { name.text = h != null ? h.DisplayName : c[1]; name.color = jackpot ? Gold : Color.white; }
            var shard = Txt(row, "RowShard");
            if (shard != null) { shard.text = $"×{c[2]}"; shard.color = jackpot ? Gold : Color.white; }
            Act(row, "RowJackpot", jackpot);
            Act(row, "RowPlate", !jackpot);

            string pay = c.Length > 4 ? c[4] : string.Empty;
            var icon = Img(row, "RowPayIcon");
            var payText = Txt(row, "RowPay");
            Sprite payIcon = pay == nameof(GachaPay.Gem) ? _gemIcon : pay == nameof(GachaPay.Ticket) ? _ticketIcon : null;
            if (icon != null) { icon.sprite = payIcon; icon.enabled = payIcon != null; icon.preserveAspect = true; }
            if (payText != null)
            {
                payText.text = pay == nameof(GachaPay.Gem) ? Localize.FromTable("ui.store.r.gem", "젬")
                    : pay == nameof(GachaPay.Ticket) ? Localize.FromTable("ui.store.r.ticket", "소환권")
                    : pay == nameof(GachaPay.Free) ? Localize.FromTable("ui.summon.history_free", "무료") : string.Empty;
                // 아이콘이 없으면(무료) 글자를 사용 칸 가운데로
                var r = payText.rectTransform;
                r.pivot = new Vector2(payIcon != null ? 0f : 0.5f, 0.5f);
                r.anchoredPosition = new Vector2(payIcon != null ? 224f : 236f, 0f);
                payText.alignment = payIcon != null ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            }
        }

        private void OpenInfo(bool rates)
        {
            _ui.SetActive("GachaInfoPopup", true);
            _ui.Find("GachaInfoPopup").SetAsLastSibling();
            _ui.SetActive("RatesGroup", rates);
            _ui.SetActive("HistoryGroup", !rates);
        }

        private void CloseInfo() => _ui.SetActive("GachaInfoPopup", false);

        // ── 도우미 ───────────────────────────────────────────

        private static Sprite Keyed(LobbyShopPanel.KeyedSprite[] list, string key)
        {
            if (list == null || string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < list.Length; i++) if (list[i].Key == key) return list[i].Sprite;
            return null;
        }

        private Image Img(Transform root, string name)
        {
            var t = _ui.Find(root, name);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private TextMeshProUGUI Txt(Transform root, string name)
        {
            var t = _ui.Find(root, name);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        private void Act(Transform root, string name, bool on)
        {
            var t = _ui.Find(root, name);
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }
    }
}
