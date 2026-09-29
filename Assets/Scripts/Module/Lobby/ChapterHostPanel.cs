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

        /// <summary>
        /// 클리어 보상 골드 — ⚠ **임시표다 (2026-09-29).** 경제 밸런스가 정해지면 표로 옮긴다.
        /// 스샷의 CH2 값(3,000~5,000)에 맞추고 앞뒤를 이어 붙였다.
        /// </summary>
        private static readonly (int min, int max)[] ClearGold =
        {
            (1000, 1800), (3000, 5000), (5000, 8000), (8000, 12000), (12000, 18000), (18000, 26000),
        };

        /// <summary>챕터 설명 두 줄 — 번역표에 없으면 이 한국어가 나온다.</summary>
        private static readonly string[] ChapterDesc =
        {
            "도시가 버린 것들이 쌓이는 곳\n무언가가 그 아래에서 깨어났다.",
            "도시 외곽에 위치한 수수께끼의 군사 시설\n더 깊은 곳에서 누군가가 움직이고 있다.",
            "네온이 꺼지지 않는 밤의 거리\n골목마다 다른 얼굴이 기다린다.",
            "빗물이 고인 공중기지 옥상\n발밑이 무너지는 소리가 난다.",
            "잠기지 않은 연구소의 문\n실패한 것들이 아직 숨을 쉰다.",
            "불길이 멈추지 않는 정유소\n모든 것의 주인이 여기 있다.",
        };

        /// <summary>랜덤 선택의 값. 제일 싸다 — 고를 이유를 값으로 만든다(기획 2026-09-29).</summary>
        public const int RandomCost = 300;

        /// <summary>랜덤이 **안 가진 몸**을 빌려줄 확률(%). 그 판에만 쓴다.</summary>
        private const int LegendChancePercent = 5;

        private const int ChapterCount = PlayerDataService.ChapterCount;

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
            RefreshStart();
        }

        // ── 그리기 ───────────────────────────────────────────

        private void RefreshAll()
        {
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
            int i = _chapter - 1;
            // 「CHAPTER」 글자는 화면 그림에 이미 있다 — 번호만 쓴다
            _ui.SetText("CHChapterNoText", $"{_chapter:00}");
            _ui.SetText("CHChapterNameText", Localize.Get($"stage.{_chapter}.1.name"));
            _ui.SetText("CHChapterDescText",
                        Localize.FromTable($"chapter.{_chapter}.desc",
                                           i < ChapterDesc.Length ? ChapterDesc[i] : string.Empty));

            var art = _ui.Get<Image>("CHChapterArt");
            if (art != null)
            {
                if (_chapterArt != null && i < _chapterArt.Length && _chapterArt[i] != null)
                    art.sprite = _chapterArt[i];
                art.color = ChapterOpen ? Color.white : new Color(0.45f, 0.45f, 0.5f);
            }
            _ui.SetActive("CHLockIcon", !ChapterOpen);

            // 화살표는 끝에서 흐리게 — 없애면 자리가 흔들린다
            SetArrow("CHPrevButton", _chapter > 1);
            SetArrow("CHNextButton", _chapter < ChapterCount);

            // 「BOSS」·「클리어 보상」 같은 고정 글자는 화면 그림에 있다 — 값만 쓴다
            var ca = _chapterArts != null && i < _chapterArts.Length ? _chapterArts[i] : default;
            _ui.SetText("CHBossNameText", ca.BossName ?? string.Empty);
            var boss = _ui.Get<Image>("CHBossPortrait");
            if (boss != null)
            {
                boss.sprite = ca.BossPortrait;
                boss.enabled = boss.sprite != null;
                boss.preserveAspect = true;
            }

            var (min, max) = i < ClearGold.Length ? ClearGold[i] : (0, 0);
            _ui.SetText("CHRewardGoldText", $"{min:N0} ~ {max:N0}");
            var chest = _ui.Get<Image>("CHRewardChestIcon");
            if (chest != null)
            {
                chest.sprite = ca.Chest;
                chest.enabled = chest.sprite != null;
                chest.preserveAspect = true;
            }
            _ui.SetText("CHRewardChestText", ca.ChestLabel ?? string.Empty);
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
                if (_pickedRandom) return RandomCost;
                var e = _player.GetHost(_pickedHost);
                return e == null || e.IsGhost ? 0 : PlayerDataService.BaseHostEntryCost(e.Grade);
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

        /// <summary>랜덤은 제 값(<see cref="RandomCost"/>)으로 낸다 — 뽑힌 몸의 등급값이 아니다.</summary>
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
        // ⚠ 시안(1024 폭) 값을 그대로 줄인 것이다. 배율은 빌더와 **같은 0.703125** 다.
        //   시안 카드 = 가로 142.67 · 세로 245 · 사이 8 · 보이는 폭 896 (여섯 장).
        private const float S = 720f / 1024f;

        public const float CardWidth = 140f * S;
        public const float CardHeight = 258f * S;
        public const float CardGap = 12.4f * S;
        public const float ViewWidth = 901f * S;
        public const float ViewHeight = 258f * S;
        public const int CardSlots = 26;   // 유령 + 호스트 23 + 여유
    }
}
