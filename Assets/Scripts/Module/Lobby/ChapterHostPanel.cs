using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 챕터 + 호스트를 **한 창에서** 고른다 (기획 2026-09-29 · 시안 사용자 제공 스샷).
    ///
    /// 예전에는 챕터 선택 창(<see cref="ChapterSelectPanel"/>)과 호스트 선택 창
    /// (<see cref="HostSelectPanel"/>)을 차례로 열었다. 두 창을 이 창 하나가 대신한다 —
    /// 로비 PLAY → 이 창 → 게임. 호스트 도감·강화는 그대로 하단 HOST 칸(육성 화면)이 맡는다.
    ///
    /// 창의 규칙 넷:
    ///   1. 맨 왼쪽은 **유령**이다. 몸 없이 들어가는 것도 하나의 선택이라 자리가 같아야 한다.
    ///   2. 목록에는 **가진 몸만** 나온다(<see cref="IPlayerDataService.IsHostOwned"/>).
    ///   3. **랜덤 선택도 하나의 선택**이다 — 고르면 위에서 고른 몸이 풀린다. 반대도 같다.
    ///   4. 잠긴 챕터는 넘겨볼 수는 있어도 들어가지 못한다.
    ///
    /// 노드 이름 = 바인딩 키다. `ChapterHostBuilder` 가 세우며, 이름을 바꾸면 거기도 바꾼다.
    /// </summary>
    public sealed class ChapterHostPanel : MonoBehaviour
    {
        /// <summary>카드에 쓸 그림. 초상(발주본)이 있으면 그것, 없으면 유닛 그림이다.</summary>
        [Serializable]
        public struct HostArt
        {
            public string Key;
            public Sprite Thumb;
            public bool IsPortrait;   // 유닛 그림은 투명 여백이 있어 칸을 키워 쓴다
        }

        /// <summary>챕터마다 붙는 것 — 보스 이름·얼굴과 클리어 상자.</summary>
        [Serializable]
        public struct ChapterArt
        {
            public string BossName;
            public Sprite BossPortrait;
            public Sprite Chest;
            public string ChestLabel;
        }

        [SerializeField] private HostArt[] _hostArts = Array.Empty<HostArt>();
        [SerializeField] private ChapterArt[] _chapterArts = Array.Empty<ChapterArt>();
        [SerializeField] private Sprite[] _chapterArt = Array.Empty<Sprite>();
        [SerializeField] private Sprite _cardFrame, _cardFrameSelected;
        [SerializeField] private Sprite _starOn, _starOff;

        // 상성 시험판(2026-10-02) — 무기 · 파워 · 마법 아이콘(이 순서), 유리 ▲ · 불리 ▼
        [SerializeField] private Sprite[] _kindGems = Array.Empty<Sprite>();
        [SerializeField] private Sprite _matchUp, _matchDown;

        // ⚠ 클리어 보상 · 챕터 설명 · 그림 칸은 **챕터 표**(`GameConfig.ChapterOf`)에서 온다.
        //   예전에는 여기 6칸 배열로 적혀 있었고, 보상 골드는 실제 지급액의 4~14배였다(임시표).
        //   화면에 적힌 값과 받는 값이 달라서는 안 된다 — 같은 표를 읽는다.

        /// <summary>판에서 주운 골드 중 평균적으로 남겨 나오는 몫(상점에서 쓰고 남은 것). 표시 범위의 아래쪽.</summary>
        private const float RunGoldKeepRatio = 0.6f;

        /// <summary>랜덤이 **안 가진 몸**을 빌려줄 확률(%). 그 판에만 쓴다.</summary>
        private const int LegendChancePercent = 5;

        private int ChapterCount => _player != null && _player.IsReady ? _player.ChapterCount : 1;

        private static readonly Color GoldText = new(1f, 0.85f, 0.32f);
        private static readonly Color DimText = new(0.66f, 0.71f, 0.8f);

        private UIBinder _ui;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();
        private readonly List<string> _cardKeys = new();
        private readonly List<Transform> _cards = new();

        private ScrollRect _hostScroll;
        private int _chapter = 1;
        private string _pickedHost;
        private bool _pickedRandom;
        private bool _built;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);

            _ui.OnClick("CHBackButton", Close);
            _ui.OnClick("CHPrevButton", () => Step(-1));
            _ui.OnClick("CHNextButton", () => Step(+1));
            _ui.OnClick("CHRandomCard", PickRandom);
            _ui.OnClick("CHStartButton", OnStart);
            _ui.OnClick("CHHostPrev", () => ScrollCards(-1));
            _ui.OnClick("CHHostNext", () => ScrollCards(+1));
            _hostScroll = _ui.Get<ScrollRect>("CHHostViewport");
            // 창 뒤 어둡게 막 — 로비가 눌리지 않게 막기만 한다
            // ⚠ 여기서 SetActive(false) 를 하지 않는다. 처음 닫아 두는 것은 주인인 LobbyMainUI 다.
        }

        private void OnEnable()
        {
            if (!CoreModule.TryGet<IEventBus>(out var bus)) return;
            _tokens.Add(bus.Subscribe<CurrencyChangedEvent>(_ => RefreshCurrency()));
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui 규약 — 활성화 시 최상단으로
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
            if (_player == null || !_player.IsReady) return;

            Game.Module.InGame.AffinityRule.Bind(_player.Config);
            _chapter = Mathf.Clamp(_player.SelectedChapter, 1, ChapterCount);
            BuildCards();
            _pickedRandom = false;
            _pickedHost = _player.SelectedHostId;
            if (!_cardKeys.Contains(_pickedHost)) _pickedHost = HostEntry.GhostKey;
            RefreshAll();
            ShowPicked();
        }

        public void Close() => gameObject.SetActive(false);

        // ── 카드 ─────────────────────────────────────────────

        /// <summary>
        /// 가진 몸만 카드를 켠다. 빌더가 미리 세워 둔 칸을 **재활용**한다 —
        /// 열 때마다 만들고 지우면 스크롤 위치가 튀고 쓰레기가 쌓인다.
        /// </summary>
        private void BuildCards()
        {
            if (_built) { RefreshCardsOwned(); return; }
            _built = true;

            var content = _ui.Find("CHHostContent");
            if (content == null) return;
            _cards.Clear();
            _cardKeys.Clear();

            var playable = _player.PlayableHosts;
            int slot = 0;
            for (int i = 0; i < playable.Count && slot < content.childCount; i++)
            {
                var e = playable[i];
                if (e == null || !_player.IsHostOwned(e)) continue;

                var card = content.GetChild(slot);
                string key = e.HostKey;   // 클로저가 루프 변수를 잡지 않게 복사한다
                card.gameObject.SetActive(true);
                var button = card.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => PickHost(key));
                }
                _cards.Add(card);
                _cardKeys.Add(key);
                slot++;
            }
            for (int i = slot; i < content.childCount; i++)
                content.GetChild(i).gameObject.SetActive(false);

            // 칸이 모자라면 가진 몸이 다 안 보인다 — 조용히 잘리면 버그로 읽힌다
            int owned = 0;
            for (int i = 0; i < playable.Count; i++)
                if (playable[i] != null && _player.IsHostOwned(playable[i])) owned++;
            if (owned > content.childCount)
                Debug.LogWarning($"[챕터·호스트] 카드 칸 {content.childCount} 개로 보유 {owned} 명을 다 못 그린다");

            LayoutCards();
        }

        private void RefreshCardsOwned()
        {
            // 판이 열린 뒤 몸을 새로 얻었을 수 있다 — 다시 세운다
            _built = false;
            BuildCards();
        }

        /// <summary>가로 목록의 폭을 카드 수에 맞춘다. 안 맞추면 끝까지 밀리지 않는다.</summary>
        private void LayoutCards()
        {
            if (_ui.Find("CHHostContent") is not RectTransform content) return;
            float step = ChapterHostLayout.CardWidth + ChapterHostLayout.CardGap;
            // ⚠ 첫 칸은 **여백 없이 0 에서** 시작한다. 앞에 여백을 주면 줄 전체가 밀려
            //   여섯째 칸이 오른쪽에서 잘린다(시안은 여섯 장이 꽉 찬다).
            float total = Mathf.Max(ChapterHostLayout.ViewWidth,
                                    _cards.Count * step - ChapterHostLayout.CardGap);
            content.sizeDelta = new Vector2(total, content.sizeDelta.y);
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] is not RectTransform r) continue;
                r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(i * step, 0f);
            }
        }

        // ── 고르기 ───────────────────────────────────────────

        private void PickHost(string key)
        {
            _pickedHost = key;
            _pickedRandom = false;      // 규칙 3 — 둘은 같은 자리다. 하나를 고르면 하나가 풀린다
            // ⚠ 저장에도 넣는다. 창 안의 변수만 바꾸면 **게임은 지난번에 고른 몸으로 시작한다**
            //   — 씬을 넘어가는 단일 출처는 `SelectedHostId` 다(2026-09-29 실제로 그랬다).
            _player.SelectHost(key);
            GameSound.Cue("ui.select");
            RefreshHosts();
            RefreshStart();
            ShowPicked();
        }

        private void PickRandom()
        {
            _pickedRandom = true;
            _pickedHost = null;
            GameSound.Cue("ui.select");
            RefreshHosts();
            RefreshStart();
        }

        /// <summary>
        /// 고른 카드를 보이는 자리로 끌어온다.
        ///
        /// 저장된 선택이 목록 뒤쪽이면 창을 열었을 때 **어디가 골라졌는지 안 보인다** —
        /// 값만 바뀌어 있어 고장으로 읽힌다.
        /// </summary>
        private void ShowPicked()
        {
            if (_hostScroll == null || _hostScroll.content == null || _pickedRandom) return;
            int at = _cardKeys.IndexOf(_pickedHost);
            if (at < 0) return;
            float hidden = _hostScroll.content.rect.width - ChapterHostLayout.ViewWidth;
            if (hidden <= 1f) return;
            float step = ChapterHostLayout.CardWidth + ChapterHostLayout.CardGap;
            float left = at * step;
            float now = _hostScroll.horizontalNormalizedPosition * hidden;
            // 이미 보이면 건드리지 않는다 — 누를 때마다 목록이 튀면 멀미가 난다
            if (left >= now && left + ChapterHostLayout.CardWidth <= now + ChapterHostLayout.ViewWidth) return;
            float want = left < now ? left - ChapterHostLayout.CardGap
                                    : left + ChapterHostLayout.CardWidth - ChapterHostLayout.ViewWidth
                                      + ChapterHostLayout.CardGap;
            _hostScroll.horizontalNormalizedPosition = Mathf.Clamp01(want / hidden);
        }

        /// <summary>목록 좌우 화살표 — 한 번에 카드 하나씩. 손가락으로 쓸어도 된다.</summary>
        private void ScrollCards(int dir)
        {
            if (_hostScroll == null || _hostScroll.content == null) return;
            float hidden = _hostScroll.content.rect.width - ChapterHostLayout.ViewWidth;
            if (hidden <= 1f) return;   // 다 보이면 움직일 것이 없다
            float step = (ChapterHostLayout.CardWidth + ChapterHostLayout.CardGap) / hidden;
            _hostScroll.horizontalNormalizedPosition =
                Mathf.Clamp01(_hostScroll.horizontalNormalizedPosition + dir * step);
        }

        private void Step(int delta)
        {
            int next = Mathf.Clamp(_chapter + delta, 1, ChapterCount);
            if (next == _chapter) return;
            _chapter = next;
            GameSound.Cue("ui.tap");
            RefreshChapter();
            RefreshHosts();   // 상성 — 챕터가 바뀌면 어느 몸이 유리한지도 바뀐다
            RefreshStart();
        }

        // ── 상성 (시험판 2026-10-02) ─────────────────────────
        //
        // 판에 들어가기 **전에** 보여야 한다 — 「이 챕터엔 이쪽 적이 많다 → 이 몸이 유리하다」.
        // 전투 화면과 같은 보석 · 같은 화살표를 쓴다.

        private Sprite GemOf(Game.Module.InGame.Affinity kind)
        {
            int i = (int)kind - 1;
            return i >= 0 && i < _kindGems.Length ? _kindGems[i] : null;
        }

        /// <summary>챕터 칸 — 적 구성(무기 · 파워 · 마법 몇 마리씩)과 보스의 쪽.</summary>
        private void RefreshChapterAffinity()
        {
            bool on = Game.Module.InGame.AffinityRule.Enabled;
            var (blade, force, magic) = Game.Module.InGame.AffinityRule.ChapterMix(_chapter);
            SetKindCount(0, Game.Module.InGame.Affinity.Blade, blade, on);
            SetKindCount(1, Game.Module.InGame.Affinity.Force, force, on);
            SetKindCount(2, Game.Module.InGame.Affinity.Magic, magic, on);

            var bossGem = _ui.Get<Image>("CHBossKindGem");
            if (bossGem != null)
            {
                bossGem.sprite = on ? GemOf(Game.Module.InGame.AffinityRule.BossKindOf(_chapter)) : null;
                bossGem.gameObject.SetActive(bossGem.sprite != null);
            }
        }

        private void SetKindCount(int slot, Game.Module.InGame.Affinity kind, int count, bool on)
        {
            var gem = _ui.Get<Image>($"CHKindGem{slot}");
            if (gem != null)
            {
                gem.sprite = on ? GemOf(kind) : null;
                gem.gameObject.SetActive(gem.sprite != null);
                // 한 마리도 안 나오는 쪽은 흐리게 — 자리는 지킨다(없애면 줄이 흔들린다).
                gem.color = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
            _ui.SetText($"CHKindCount{slot}", on ? count.ToString() : string.Empty);
        }

        /// <summary>호스트 카드 — 그 몸의 보석과, 이 챕터에 가장 많은 적에게 유리한가 불리한가.</summary>
        private void RefreshCardAffinity(Transform card, string hostKey)
        {
            bool on = Game.Module.InGame.AffinityRule.Enabled;
            var kind = Game.Module.InGame.AffinityRule.KindOf(hostKey);

            var gem = _ui.Find(card, "CardKindGem")?.GetComponent<Image>();
            if (gem != null)
            {
                gem.sprite = on ? GemOf(kind) : null;
                gem.gameObject.SetActive(gem.sprite != null);
            }

            var arrow = _ui.Find(card, "CardMatchArrow")?.GetComponent<Image>();
            if (arrow != null)
            {
                int outcome = on ? Game.Module.InGame.AffinityRule.Outcome(
                    kind, Game.Module.InGame.AffinityRule.MajorOf(_chapter)) : 0;
                arrow.sprite = outcome > 0 ? _matchUp : outcome < 0 ? _matchDown : null;
                arrow.gameObject.SetActive(arrow.sprite != null);
            }
        }

        // ── 그리기 ───────────────────────────────────────────

        /// <summary>
        /// 배경에서 빼낸 **붙박이 글자**를 지금 언어로 채운다.
        ///
        /// 예전에는 이 글자들이 화면 그림에 박혀 있었다 — 일본 납품이라 번역이 안 됐다(2026-10-01).
        /// 번역이 없으면 `FromTable` 이 여기 적은 한국어 원문을 그대로 돌려준다.
        /// </summary>
        private void RefreshLabels()
        {
            _ui.SetText("CHBossLabel", Localize.FromTable("ui.chapterhost.boss", "BOSS"));
            _ui.SetText("CHRewardLabel", Localize.FromTable("ui.chapterhost.reward", "클리어 보상"));
            _ui.SetText("CHHostTitle", Localize.FromTable("ui.chapterhost.hosttitle", "호스트 선택"));
            _ui.SetText("CHHostNote",
                        Localize.FromTable("ui.chapterhost.ownedonly", "보유한 호스트만 선택 가능합니다."));
            _ui.SetText("CHRandomTitle", Localize.FromTable("ui.chapterhost.random", "랜덤 선택"));
            _ui.SetText("CHRandomDesc1",
                        Localize.FromTable("ui.chapterhost.randomdesc1",
                                           "모든 호스트 중 하나가 랜덤으로 선택됩니다."));
            _ui.SetText("CHRandomDesc2",
                        Localize.FromTable("ui.chapterhost.randomdesc2",
                                           "낮은 확률로 전설 호스트 등장!"));
            _ui.SetText("CHStartLabel", Localize.FromTable("ui.chapterhost.start", "도전하기") + " \u25B6");
        }

        private void RefreshAll()
        {
            RefreshLabels();
            RefreshCurrency();
            RefreshChapter();
            RefreshHosts();
            RefreshStart();
        }

        private void RefreshCurrency()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("CHGoldText", _player.Gold.ToString("N0"));
            _ui.SetText("CHGemText", _player.Gem.ToString("N0"));
        }

        private bool ChapterOpen => _chapter <= (_player != null ? _player.UnlockedChapter : 1);

        private void RefreshChapter()
        {
            var row = _player != null ? _player.ChapterInfo(_chapter) : default;
            // 그림은 빌려 쓰는 칸에서 — 7챕터부터는 앞 챕터의 그림을 쓴다(새 그림이 오면 표만 고친다)
            int i = (row.Art > 0 ? row.Art : _chapter) - 1;
            int bossAt = (row.BossArt > 0 ? row.BossArt : _chapter) - 1;
            int chestAt = (row.ChestArt > 0 ? row.ChestArt : _chapter) - 1;
            // 「CHAPTER」와 번호는 **한 글자칸**이다. 따로 두면 가로로 늘어나는 화면(4:3)에서
            // 글자는 제 폭을 지키고 자리만 벌어져 둘 사이가 뜬다(2026-10-01).
            _ui.SetText("CHChapterLabel",
                        $"{Localize.FromTable("ui.chapterhost.chapter", "CHAPTER")} "
                        + $"<size=122%><color=#5CC7FF>{_chapter:00}</color></size>");
            _ui.SetText("CHChapterNoText", string.Empty);
            _ui.SetText("CHChapterNameText",
                        Localize.FromTable($"stage.{_chapter}.1.name", row.Name ?? string.Empty));
            _ui.SetText("CHChapterDescText",
                        Localize.FromTable($"chapter.{_chapter}.desc", row.Desc ?? string.Empty));

            var art = _ui.Get<Image>("CHChapterArt");
            if (art != null)
            {
                if (_chapterArt != null && i < _chapterArt.Length && _chapterArt[i] != null)
                    art.sprite = _chapterArt[i];
                art.color = ChapterOpen ? Color.white : new Color(0.45f, 0.45f, 0.5f);
                FillBox(art);
            }
            _ui.SetActive("CHLockIcon", !ChapterOpen);

            // 화살표는 끝에서 흐리게 — 없애면 자리가 흔들린다
            SetArrow("CHPrevButton", _chapter > 1);
            SetArrow("CHNextButton", _chapter < ChapterCount);

            // 「BOSS」·「클리어 보상」 같은 고정 글자는 화면 그림에 있다 — 값만 쓴다
            var ca = _chapterArts != null && bossAt >= 0 && bossAt < _chapterArts.Length
                ? _chapterArts[bossAt] : default;
            var chestArt = _chapterArts != null && chestAt >= 0 && chestAt < _chapterArts.Length
                ? _chapterArts[chestAt] : default;
            _ui.SetText("CHBossNameText", ca.BossName ?? string.Empty);
            var boss = _ui.Get<Image>("CHBossPortrait");
            if (boss != null)
            {
                boss.sprite = ca.BossPortrait;
                boss.enabled = boss.sprite != null;
                boss.preserveAspect = true;
            }

            // 받는 골드 = 클리어 골드 + 판에서 주워 남긴 골드. 상점에서 얼마나 쓰느냐로 범위가 생긴다.
            int min = row.ClearGold + Mathf.RoundToInt(row.RunGold * RunGoldKeepRatio);
            int max = row.ClearGold + row.RunGold;
            _ui.SetText("CHRewardGoldText", $"{min:N0} ~ {max:N0}");
            var chest = _ui.Get<Image>("CHRewardChestIcon");
            if (chest != null)
            {
                chest.sprite = chestArt.Chest;
                chest.enabled = chest.sprite != null;
                chest.preserveAspect = true;
            }
            _ui.SetText("CHRewardChestText", chestArt.ChestLabel ?? string.Empty);

            RefreshChapterAffinity();
        }

        /// <summary>
        /// 그림을 **액자에 꽉 채운다**(cover). 넘치는 쪽은 `RectMask2D` 가 자른다.
        ///
        /// `preserveAspect` 는 반대로 **안에 맞춰 넣어서**(contain) 위아래나 좌우에
        /// 빈 띠가 생긴다 — 시안은 그림이 액자 끝까지 차 있는데 게임에서는 그림만
        /// 동동 떠 보였다(2026-10-01 지적).
        /// </summary>
        private static void FillBox(Image img)
        {
            var sprite = img.sprite;
            var box = img.rectTransform.parent as RectTransform;
            if (sprite == null || box == null) return;

            img.preserveAspect = false;
            float bw = box.rect.width, bh = box.rect.height;
            if (bw <= 1f || bh <= 1f) return;

            var s = sprite.rect;
            float scale = Mathf.Max(bw / s.width, bh / s.height);   // 둘 중 **큰 쪽** = 꽉 채우기
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchoredPosition = Vector2.zero;
            img.rectTransform.sizeDelta = new Vector2(s.width * scale, s.height * scale);
        }

        /// <summary>
        /// 화살표는 **화면 그림에 이미 그려져 있다.** 여기 있는 것은 누를 자리(투명)뿐이라
        /// 색을 건드리면 흰 네모가 나타난다(2026-09-29). 눌리는지만 바꾼다.
        /// </summary>
        private void SetArrow(string name, bool on)
        {
            var btn = _ui.Get<Button>(name);
            if (btn != null) btn.interactable = on;
        }

        private void RefreshHosts()
        {
            // 「호스트 선택」·안내 문구는 화면 그림에 있다
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                string key = _cardKeys[i];
                var e = _player.GetHost(key);
                bool sel = !_pickedRandom && key == _pickedHost;

                // 빈 칸은 화면 그림에 있다 — 고른 칸에만 금색 액자를 덮는다
                var sel2 = _ui.Find(card, "CardFrameSel");
                if (sel2 != null) sel2.gameObject.SetActive(sel);
                var check = _ui.Find(card, "CardCheck");
                if (check != null) check.gameObject.SetActive(sel);

                var thumb = _ui.Find(card, "CardThumb")?.GetComponent<Image>();
                if (thumb != null)
                {
                    var art = ArtOf(key);
                    thumb.sprite = art.Thumb;
                    thumb.enabled = thumb.sprite != null;
                    thumb.preserveAspect = true;
                }
                TextIn(card, "CardName", e != null ? e.DisplayName : key);

                int stars = _player.StarsOf(key);
                bool ghost = e != null && e.IsGhost;
                for (int s = 0; s < 5; s++)
                {
                    var img = _ui.Find(card, $"CardStar{s}")?.GetComponent<Image>();
                    if (img == null) continue;
                    img.gameObject.SetActive(!ghost);
                    // 켜진 별·빈 별 둘 다 시안에서 떼어 온 그림이라 색을 건드리지 않는다
                    img.sprite = s < stars ? _starOn : _starOff;
                    img.color = Color.white;
                }

                // 유령은 몸이 아니다 — 전투력 자리에 무엇인지를 적는다
                var power = _ui.Find(card, "CardPowerText")?.GetComponent<TextMeshProUGUI>();
                if (power != null)
                {
                    power.text = ghost
                        ? Localize.FromTable("ui.chapterhost.ghost_power", "몸 없음")
                        : _player.PowerOf(key).ToString("N0");
                    power.color = ghost ? DimText : GoldText;
                }
                var powerIcon = _ui.Find(card, "CardPowerIcon");
                if (powerIcon != null) powerIcon.gameObject.SetActive(!ghost);

                RefreshCardAffinity(card, key);
            }

            // 랜덤 칸의 글자와 그림도 화면 그림에 있다 — 고른 표시만 낸다
            _ui.SetActive("RandomCheck", _pickedRandom);
        }

        private void RefreshStart()
        {
            int cost = DisplayCost;
            _ui.SetText("CHStartCostText", cost.ToString("N0"));
            _ui.SetActive("CHStartCostIcon", cost > 0);
            _ui.SetActive("CHStartCostText", cost > 0);
            // 「도전하기 ▶」는 화면 그림에 있다

            var btn = _ui.Get<Button>("CHStartButton");
            if (btn != null) btn.interactable = ChapterOpen && (_pickedRandom || !string.IsNullOrEmpty(_pickedHost));
        }

        /// <summary>
        /// 버튼에 적는 값.
        ///
        /// ⚠ <see cref="PlayerDataService.EntryCostOf"/> 를 쓰지 않는다 — 지금 테스트용
        ///   무료 플래그가 켜져 있어 0 이 나온다. **적는 값은 진짜 값이고, 무료는 치를 때만**
        ///   적용한다. 그래야 값이 얼마인지 화면에서 확인할 수 있다.
        /// </summary>
        private int DisplayCost
        {
            get
            {
                if (_pickedRandom) return _player.RandomEntryPrice(_chapter);
                return _player.EntryPriceOf(_player.GetHost(_pickedHost), _chapter);
            }
        }

        private HostArt ArtOf(string key)
        {
            for (int i = 0; i < _hostArts.Length; i++)
                if (_hostArts[i].Key == key) return _hostArts[i];
            return default;
        }

        private void TextIn(Transform card, string name, string value)
        {
            var t = _ui.Find(card, name)?.GetComponent<TextMeshProUGUI>();
            if (t != null) t.text = value;
        }

        // ── 들어가기 ─────────────────────────────────────────

        private void OnStart()
        {
            if (_player == null || !_player.IsReady) return;
            if (!ChapterOpen)
            {
                SystemPopup.Show(Localize.Format("ui.chapter.locked_hint", _chapter - 1), null,
                                 Localize.Get("ui.common.ok"), null);
                return;
            }

            string key = _pickedRandom ? RollRandomHost() : _pickedHost;
            var e = _player.GetHost(key);
            if (e == null) return;

            _player.SelectedChapter = _chapter;
            _player.SelectHost(key);        // 랜덤으로 뽑힌 몸도 여기서 확정된다

            // 유령은 몸이 아니라 값을 안 낸다
            if (e.IsGhost)
            {
                _player.StartAsGhost = true;
                GameSound.Cue("ui.play");
                EnterGameAsync().Forget();   // fire-and-forget: 씬 전환 완료를 기다릴 필요가 없다
                return;
            }

            // 값을 낸다. 모자라면 들어가지 않고 얼마가 모자란지 알린다 —
            // 조용히 유령으로 바꿔 넣으면 「왜 내 캐릭터가 아니지」가 된다.
            int cost = DisplayCost;
            if (!PayEntry(e, cost))
            {
                SystemPopup.Show(
                    Localize.Format("ui.hostselect.gold_short", e.DisplayName, e.Grade, _player.Gold, cost),
                    null, Localize.Get("ui.common.ok"), null);
                return;
            }

            _player.StartAsGhost = false;
            GameSound.Cue("ui.play");
            CoreModule.Get<IEventBus>().Publish(new PossessStartRequestedEvent { HostKeyToPossess = key });
            EnterGameAsync().Forget();   // fire-and-forget: 씬 전환 완료를 기다릴 필요가 없다
        }

        /// <summary>랜덤은 제 값(`RandomEntryPrice`)으로 낸다 — 뽑힌 몸의 등급값이 아니다.</summary>
        private bool PayEntry(HostEntry e, int cost)
        {
            if (!_pickedRandom) return _player.PayHostEntry(e);
            // 몸값이 무료인 동안에는 랜덤도 무료다 — 한쪽만 받으면 랜덤이 손해가 된다
            if (cost <= 0 || PlayerDataService.FreeHostsForTest) return true;
            return _player.TrySpendGold(cost);
        }

        /// <summary>
        /// 가진 몸 중에서 하나. 낮은 확률로 **안 가진 몸**을 이 판만 빌려준다 —
        /// 「전설 호스트 등장」이 그것이다. 빌린 몸은 저장에 남지 않는다.
        /// </summary>
        private string RollRandomHost()
        {
            var playable = _player.PlayableHosts;
            var owned = new List<string>();
            var legend = new List<string>();
            for (int i = 0; i < playable.Count; i++)
            {
                var e = playable[i];
                if (e == null || e.IsGhost) continue;   // 유령은 랜덤에서 뺀다 — 몸을 받으러 온 자리다
                if (_player.IsHostOwned(e)) owned.Add(e.HostKey);
                else if (e.Grade == HostGrade.S) legend.Add(e.HostKey);
            }

            if (legend.Count > 0 && UnityEngine.Random.Range(0, 100) < LegendChancePercent)
                return legend[UnityEngine.Random.Range(0, legend.Count)];
            if (owned.Count > 0) return owned[UnityEngine.Random.Range(0, owned.Count)];
            return HostEntry.GhostKey;   // 가진 몸이 하나도 없으면 유령으로 들어간다
        }

        private async UniTaskVoid EnterGameAsync()
        {
            await _player.SaveAsync();   // 고른 것을 저장소에 커밋 — 씬을 넘는 단일 출처
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.InGame,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }
    }

    /// <summary>카드 자리값 — 판과 빌더가 **같은 숫자**를 봐야 목록이 어긋나지 않는다.</summary>
    public static class ChapterHostLayout
    {
        // ⚠ 시안이 **9:16(1080x1920)** 으로 다시 그려졌다(2026-10-01).
        //   2:3 시안을 늘려 쓰던 때와 달리 늘림이 없다 — 배율은 빌더와 **같은 720/1080** 이다.
        //   실측(v6) : 카드 가로 142 · 세로 360 · 사이 12 · 보이는 폭 914 (여섯 장).
        private const float S = 720f / 1080f;

        public const float CardWidth = 142f * S;
        public const float CardHeight = 360f * S;
        public const float CardGap = 12f * S;
        public const float ViewWidth = 914f * S;
        public const float ViewHeight = 360f * S;
        public const int CardSlots = 26;   // 유령 + 호스트 23 + 여유
    }
}
