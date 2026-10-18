using System;
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
    /// 출전 방식 셋 · 카드 키트 · 도전하기 버튼(시안 S22e, 기획서 `AVSR_Content_Store.md` B-12).
    ///
    ///   몸 골라  — 행동력 + 골드 출전비(오늘의 몸 3명은 그날 골드 무료)
    ///   ? 랜덤   — 행동력만. 깨면 나온 몸 조각 · 판 골드 보너스
    ///   유령     — 행동력만. 깨면 유령 경험치 보너스
    ///
    /// 보너스 지급은 `PlayerDataService.GrantChapterClearAsync`, 행동력은 `IStaminaService`, 오늘의 몸은 `ITodayHostService`.
    /// </summary>
    public sealed partial class ChapterHostPanel
    {
        // 도전하기 버튼 위 작은 띠 — 오늘의 몸(랜덤 · 유령은 버튼 첫 줄에 붙여 쓴다)
        [SerializeField] private Sprite _ribbonToday;

        private const float StartIconGap = 4f, StartPartGap = 10f;

        // 카드(100 × 168) 별 줄 — 이름(-36) 위 · 얼굴 아래. 큰 금별은 1.6배라 은별을 12 로 둔다
        private static readonly Vector2 CardStarCenter = new(0f, -19f);
        private const float CardStarSize = 12f;

        // 카드 줄 치수 — 출전 방식 화면(S22e)이면 작은 카드
        private float CardW => _hasModes ? ChapterHostLayout.ModeCardWidth : ChapterHostLayout.CardWidth;
        private float CardGapW => _hasModes ? ChapterHostLayout.ModeCardGap : ChapterHostLayout.CardGap;
        private float ViewW => _hasModes ? ChapterHostLayout.ModeViewWidth : ChapterHostLayout.ViewWidth;

        private static bool IsToday(string hostKey)
            => !string.IsNullOrEmpty(hostKey) && CoreModule.TryGet<ITodayHostService>(out var t) && t.IsToday(hostKey);

        private static string F(string key, string korean, params object[] args)
            => string.Format(Localize.FromTable(key, korean), args);

        private static string HoursMinutes(TimeSpan t) => $"{(int)t.TotalHours:00}:{t.Minutes:00}";

        // ── 카드 ─────────────────────────────────────────────

        /// <summary>등급 틀 · 등급 배지 · 별(반 칸) · 오늘의 몸 띠.</summary>
        private void ApplyCardKit(Transform card, string key, HostEntry e)
        {
            int g = e != null ? (int)e.Grade : 0;
            var frame = _ui.Find(card, "CardFrame")?.GetComponent<Image>();
            if (frame != null && g < _gradeFrames.Length && _gradeFrames[g] != null) frame.sprite = _gradeFrames[g];
            var badge = _ui.Find(card, "CardGrade")?.GetComponent<Image>();
            if (badge != null)
            {
                badge.sprite = g < _gradeBadges.Length ? _gradeBadges[g] : null;
                badge.gameObject.SetActive(badge.sprite != null);
            }

            // 성급 = 숙련도(1~10) — 은별 1~4 · 5성마다 큰 금별. 숙련도 0(봉인)은 별 없음 — 출전은 된다
            int m = _player.GetMastery(key);
            if (_starGold != null && _starSilver != null)
                StarRank.Apply(card, "CardStar", m, _starGold, _starSilver, CardStarCenter, CardStarSize, 1f);
            else
            {
                int full = m / 2;
                bool half = m % 2 == 1;
                for (int s = 0; s < 5; s++)
                {
                    var img = _ui.Find(card, $"CardStar{s}")?.GetComponent<Image>();
                    if (img == null) continue;
                    img.sprite = s < full ? _starOn : s == full && half && _starHalf != null ? _starHalf : _starOff;
                }
            }

            // 유리 ▲ · 불리 ▼ 화살표는 시안 S22e 카드에 없다 — 속성 아이콘만
            var arrow = _ui.Find(card, "CardMatchArrow");
            if (arrow != null) arrow.gameObject.SetActive(false);
            var frameImg = _ui.Find(card, "CardFrame")?.GetComponent<Image>();
            if (frameImg != null) frameImg.color = Color.white;

            bool today = IsToday(key);
            var band = _ui.Find(card, "CardToday");
            if (band != null) band.gameObject.SetActive(today);
            if (today) TextIn(card, "CardTodayText", Localize.FromTable("ui.start.today", "오늘의 몸"));
        }

        // 미보유 카드 — 실루엣 · 자물쇠 · 빈 별 · 「???」 (시안 S22e 맨 오른쪽)
        private static readonly Color LockedTint = new(0.16f, 0.17f, 0.24f, 1f);

        private void ApplyLockedCard(Transform card, string key)
        {
            var frame = _ui.Find(card, "CardFrame")?.GetComponent<Image>();
            if (frame != null && _gradeFrames.Length > 0) { frame.sprite = _gradeFrames[0]; frame.color = new Color(0.55f, 0.58f, 0.68f, 1f); }
            foreach (var n in new[] { "CardFrameSel", "CardCheck", "CardGrade", "CardKindGem", "CardMatchArrow",
                                      "CardPowerIcon", "CardPowerText", "CardToday" })
            {
                var t = _ui.Find(card, n);
                if (t != null) t.gameObject.SetActive(false);
            }
            var thumb = _ui.Find(card, "CardThumb")?.GetComponent<Image>();
            if (thumb != null)
            {
                thumb.sprite = ArtOf(key).Thumb;
                thumb.material = _silhouette;
                thumb.color = LockedTint;
                thumb.enabled = thumb.sprite != null;
                thumb.preserveAspect = true;
            }
            var lockIcon = _ui.Find(card, "CardLock");
            if (lockIcon != null) lockIcon.gameObject.SetActive(true);
            for (int s = 0; s < 5; s++)
            {
                var img = _ui.Find(card, $"CardStar{s}")?.GetComponent<Image>();
                if (img == null) continue;
                // 새 별 표기에는 「빈 별」이 없다 — 못 가진 몸은 별을 숨기고 「???」 와 자물쇠로만
                img.gameObject.SetActive(_starGold == null);
                img.sprite = _starOff;
            }
            TextIn(card, "CardName", "???");
        }

        // ── 출전 방식 ─────────────────────────────────────────

        private void RefreshModes()
        {
            if (!_hasModes) return;
            var cfg = _player.Config;
            int shards = cfg != null ? cfg.RandomLuckShards : 3;
            int goldPct = cfg != null ? cfg.RandomLuckGoldPct : 20;
            int expPct = cfg != null ? cfg.GhostStartExpPct : 50;

            _ui.SetText("CHModeTitle", Localize.FromTable("ui.start.title", "출전 방식"));
            bool today = IsToday(_pickedHost);
            SetModeCard("CHModeHost", RunStartMode.Host, Localize.FromTable("ui.start.mode.host", "몸 골라"),
                        today ? Localize.FromTable("ui.start.mode.host_today", "오늘의 몸 · 골드 무료") : string.Empty,
                        string.IsNullOrEmpty(_pickedHost) ? null : ArtOf(_pickedHost).Thumb);
            SetModeCard("CHModeRandom", RunStartMode.Random, Localize.FromTable("ui.start.mode.random", "? 랜덤"),
                        F("ui.start.mode.random_note", "클리어 시\n조각 +{0} · 골드 +{1}%", shards, goldPct), null);
            SetModeCard("CHModeGhost", RunStartMode.Ghost, Localize.FromTable("ui.start.mode.ghost", "유령"),
                        F("ui.start.mode.ghost_note", "클리어 시\n유령 경험치 +{0}%", expPct),
                        ArtOf(HostEntry.GhostKey).Thumb);

            // 랜덤 · 유령이면 카드 줄을 덮는다 — 누를 것이 없다는 것을 보인다(기획서 B-12)
            bool dim = _mode != RunStartMode.Host;
            _ui.SetActive("CHHostDim", dim);
            if (dim)
                _ui.SetText("CHHostDimText", _mode == RunStartMode.Random
                    ? Localize.FromTable("ui.start.dim.random", "랜덤 출전 — 몸은 판이 시작될 때 정해져요")
                    : Localize.FromTable("ui.start.dim.ghost", "유령 출전 — 판 안에서 몸을 빼앗아 싸워요"));

            if (CoreModule.TryGet<ITodayHostService>(out var todayHosts))
                _ui.SetText("CHTodayNote", F("ui.start.today_note", "오늘의 몸 {0}명 무료 · {1} 뒤 바뀜",
                                             todayHosts.Today.Count, HoursMinutes(todayHosts.UntilReset)));

            // 설명 칸 — 랜덤 글(예전 「?」 카드 글)을 출전 방식 말로
            _ui.SetText("CHRandomTitle", Localize.FromTable("ui.start.info.random", "? 랜덤 출전"));
            _ui.SetText("CHRandomDesc1", Localize.FromTable("ui.start.info.random1", "몸은 판이 시작될 때 정해져요."));
            _ui.SetText("CHRandomDesc2", Localize.FromTable("ui.start.note.grown", "빼앗은 몸도 내가 키운 만큼 세요."));
        }

        private void SetModeCard(string node, RunStartMode mode, string title, string note, Sprite art)
        {
            var root = _ui.Find(node);
            if (root == null) return;
            bool on = _mode == mode;
            var sel = _ui.Find(root, "ModeSel");
            if (sel != null) sel.gameObject.SetActive(on);
            var t = _ui.Find(root, "ModeTitle")?.GetComponent<TextMeshProUGUI>();
            if (t != null)
            {
                t.text = title;
                t.color = on ? GoldText : Color.white;
            }
            TextIn(root, "ModeNote", note);
            int perRun = CoreModule.TryGet<IStaminaService>(out var st) ? st.PerRun : 5;
            TextIn(root, "ModeStaminaText", perRun.ToString());
            if (art == null) return;   // 랜덤 그림은 프리팹에 박혀 있다
            var img = _ui.Find(root, "ModeArt")?.GetComponent<Image>();
            if (img == null) return;
            img.sprite = art;
            img.enabled = true;
            img.preserveAspect = true;
        }

        // ── 도전하기 ─────────────────────────────────────────

        /// <summary>「⚡5 + 🪙132」 / 「도전하기 ▶」 두 줄과 위의 작은 띠.</summary>
        private void RefreshStartModes()
        {
            int perRun = CoreModule.TryGet<IStaminaService>(out var st) ? st.PerRun : 5;
            int gold = DisplayCost;
            // 랜덤 · 유령은 버튼 첫 줄에 붙여 쓴다(기획서 B-12 「⚡5 · 행운 보너스」). 띠는 오늘의 몸만
            string tag = _mode == RunStartMode.Random ? Localize.FromTable("ui.start.ribbon.luck", "행운 보너스")
                       : _mode == RunStartMode.Ghost ? Localize.FromTable("ui.start.ribbon.ghost", "유령 출전")
                       : null;
            _ui.SetText("CHStartStaminaText", perRun.ToString());
            _ui.SetText("CHStartCostText", gold > 0 ? gold.ToString("N0") : tag != null ? $"· {tag}" : string.Empty);
            _ui.SetActive("CHStartPlus", gold > 0);
            _ui.SetActive("CHStartCostIcon", gold > 0);
            _ui.SetActive("CHStartCostText", gold > 0 || tag != null);
            _ui.SetText("CHStartLabel", Localize.FromTable("ui.chapterhost.start", "도전하기") + " ▶");

            Sprite ribbon = null;
            string ribbonText = null;
            if (_mode == RunStartMode.Host && IsToday(_pickedHost))
            {
                ribbon = _ribbonToday;
                ribbonText = Localize.FromTable("ui.start.ribbon.today", "오늘의 몸 무료");
            }

            var rib = _ui.Get<Image>("CHStartRibbon");
            if (rib != null)
            {
                rib.sprite = ribbon;
                rib.gameObject.SetActive(ribbon != null);
                if (ribbon != null) _ui.SetText("CHStartRibbonText", ribbonText);
            }

            LayoutStartRow(gold > 0, tag != null);

            // 유령은 스킬 칸이 없다 — 설명(유령 규칙)을 넓게 쓴다
            if (!_v5Info && _ui.Find("CHInfoDesc") is RectTransform desc)   // v5 는 띠 폭을 빌더가 정했다
                desc.sizeDelta = new Vector2(_mode == RunStartMode.Ghost ? 300f : 106f, desc.sizeDelta.y);

            var btn = _ui.Get<Button>("CHStartButton");
            if (btn != null)
                btn.interactable = ChapterOpen && (_mode != RunStartMode.Host || !string.IsNullOrEmpty(_pickedHost));
        }

        /// <summary>
        /// 「⚡ 5 + (동전) 132」 을 한 덩어리로 재서 버튼 가운데에 세운다 — 값 자릿수 · 언어마다 폭이 다르다.
        /// 노드는 전부 버튼 안에 있다(`LobbyS22eBinder`).
        /// </summary>
        /// <summary>비용 덩어리를 세울 버튼 안 가운데 x. 0 = 버튼 가운데(예전). UI v5 는 오른쪽 절반(「도전하기 | ⚡5 + 골드」).</summary>
        [SerializeField] private float _startRowCenter;

        private void LayoutStartRow(bool hasGold, bool hasTag)
        {
            var icon = _ui.Find("CHStartStaminaIcon") as RectTransform;
            var stText = _ui.Get<TextMeshProUGUI>("CHStartStaminaText");
            var plus = _ui.Get<TextMeshProUGUI>("CHStartPlus");
            var coin = _ui.Find("CHStartCostIcon") as RectTransform;
            var cost = _ui.Get<TextMeshProUGUI>("CHStartCostText");
            if (icon == null || stText == null) return;

            float w = icon.rect.width + StartIconGap + Pref(stText);
            if (hasGold && plus != null && coin != null && cost != null)
                w += StartPartGap + Pref(plus) + StartPartGap + coin.rect.width + StartIconGap + Pref(cost);
            else if (hasTag && cost != null)
                w += StartIconGap * 2f + Pref(cost);

            float x = _startRowCenter - w * 0.5f;
            x = PlaceIcon(icon, x) + StartIconGap;
            x = PlaceText(stText, x);
            if (!hasGold && hasTag && cost != null)
            {
                PlaceText(cost, x + StartIconGap * 2f);
                return;
            }
            if (!hasGold || plus == null || coin == null || cost == null) return;
            x = PlaceText(plus, x + StartPartGap) + StartPartGap;
            x = PlaceIcon(coin, x) + StartIconGap;
            PlaceText(cost, x);
        }

        private static float Pref(TextMeshProUGUI t) => t.GetPreferredValues(t.text).x;

        private static float PlaceIcon(RectTransform r, float left)
        {
            r.anchoredPosition = new Vector2(left + r.rect.width * 0.5f, r.anchoredPosition.y);
            return left + r.rect.width;
        }

        private static float PlaceText(TextMeshProUGUI t, float left)
        {
            var r = t.rectTransform;
            float width = Pref(t);
            r.pivot = new Vector2(0f, r.pivot.y);
            r.sizeDelta = new Vector2(Mathf.Max(width + 2f, 4f), r.sizeDelta.y);
            r.anchoredPosition = new Vector2(left, r.anchoredPosition.y);
            t.alignment = TextAlignmentOptions.Left;
            return left + width;
        }

        // ── 들어가기 ─────────────────────────────────────────

        private void StartWithMode()
        {
            CoreModule.TryGet<IStaminaService>(out var stamina);
            if (stamina != null && !stamina.CanEnter)
            {
                ShowStaminaShort(stamina);
                return;
            }

            string key = _mode == RunStartMode.Random ? RollRandomHost()
                       : _mode == RunStartMode.Ghost ? HostEntry.GhostKey
                       : _pickedHost;
            var e = _player.GetHost(key);
            if (e == null) return;

            // 값을 둘 다 확인한 **뒤에** 낸다 — 골드가 모자란데 행동력만 빠지면 안 된다
            int gold = PlayerDataService.FreeHostsForTest ? 0 : DisplayCost;
            if (gold > 0 && _player.Gold < gold)
            {
                SystemPopup.Show(
                    Localize.Format("ui.hostselect.gold_short", e.DisplayName, e.Grade, _player.Gold, gold),
                    null, Localize.Get("ui.common.ok"), null);
                return;
            }
            if (stamina != null && !stamina.TryPayEntry())
            {
                ShowStaminaShort(stamina);
                return;
            }
            if (gold > 0) _player.TrySpendGold(gold);

            _player.SelectedChapter = _chapter;
            _player.SelectHost(key);        // 랜덤으로 뽑힌 몸도 여기서 확정된다
            _player.RunStartMode = _mode;
            _player.RunHostKey = key;
            _player.StartAsGhost = e.IsGhost;
            GameSound.Cue("ui.play");
            if (!e.IsGhost)
                CoreModule.Get<IEventBus>().Publish(new PossessStartRequestedEvent { HostKeyToPossess = key });
            EnterGameAsync().Forget();   // fire-and-forget: 씬 전환 완료를 기다릴 필요가 없다
        }

        private void ShowStaminaShort(IStaminaService st) => StaminaPopup.Show(st, _player, true, RefreshStart);
    }
}
