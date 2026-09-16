using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.Chest;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 로비(HUB). 상단 HUD 를 소유하고, 호스트 선택 패널을 여는 진입점이다.
    ///
    /// 진입 버튼은 3개지만 경로는 2개다 (확정 사항):
    ///   CONTINUE · CHAPTER → 스테이지 진입 모드
    ///   HOST              → 조회·강화 모드
    /// </summary>
    public sealed class LobbyMainUI : MonoBehaviour, IBackTarget
    {
        [SerializeField] private HostSelectPanel _hostSelectPanel;

        /// <summary>
        /// 모드 칸 그림. <see cref="Modes"/> 와 **같은 순서**다 (서바이벌 · 시나리오 · 디펜스).
        /// 회전 목마라 어느 칸에 무엇이 오는지가 바뀌므로 코드가 갈아 끼운다.
        /// </summary>
        [SerializeField] private Sprite[] _modeArts = new Sprite[3];

        /// <summary>상자 칸 버튼 — 세는 중엔 파랑, 다 되면 금색이다.</summary>
        [SerializeField] private Sprite _chestButtonBlue;
        [SerializeField] private Sprite _chestButtonGold;

        /// <summary>
        /// 상자 등급별 그림. 어느 칸에 무슨 등급이 오는지가 매번 달라 코드가 갈아 끼운다.
        ///
        /// ⚠ 표(`ChestTable`)가 등급을 정하고 여기는 그림만 갖는다 — 등급이 늘면
        ///   표에 줄을 더하고 여기에 짝을 더한다.
        /// </summary>
        [System.Serializable]
        private struct ChestArt
        {
            public string Key;
            public Sprite Sprite;
        }

        [SerializeField] private ChestArt[] _chestArts = System.Array.Empty<ChestArt>();

        private UIBinder _ui;
        private IPlayerDataService _player;
        private IChestService _chests;
        private readonly List<IDisposable> _tokens = new();

        // 1차 범위 밖 — 버튼은 두되 누르면 준비중 안내만 띄운다.
        // ⚠ 시즌패스 · 이벤트 · 일일로그인 · 기능탭 · 챕터 카드 · 고스트 위젯은 로비에서
        //   **걷어냈다**(2026-09-15 · 2026-09-16 목업). 프리팹에서도 지웠으므로 목록에도 없다.
        private static readonly (string element, string label)[] NotReady =
        {
            ("MailButton", "우편"), ("SettingsButton", "설정"),
            // 유령 수색(방치)은 화면만 세워 뒀다 — 기능은 나중(기획 2026-09-16)
            ("GhostSearchHelpButton", "유령 수색"), ("GhostSearchClaimButton", "유령 수색"),
        };

        // ── 게임 모드 ────────────────────────────────────────────
        //
        // 세 칸짜리 회전 목마다. **가운데가 곧 고른 것**이고, 좌우 화살표나 옆칸을
        // 누르면 그 모드가 가운데로 온다. 지금 열린 것은 시나리오 하나뿐이라
        // 나머지 둘은 자물쇠가 붙고 플레이 버튼이 사라진다.
        private readonly struct GameMode
        {
            private readonly string _key;
            public readonly bool Unlocked;
            public GameMode(string key, bool unlocked) { _key = key; Unlocked = unlocked; }
            // 글자는 들고 있지 않는다 — 언어를 바꾸면 다음에 칸을 칠할 때 바로 따라온다
            public string Name => Localize.Get($"ui.lobby.mode.{_key}.name");
            public string Desc => Localize.Get($"ui.lobby.mode.{_key}.desc");
        }

        private static readonly GameMode[] Modes =
        {
            new("survival", false),
            new("scenario", true),
            new("defense", false),
        };

        private const int ScenarioIndex = 1;
        private int _modeIndex = ScenarioIndex;

        // ── 하단 바 ──────────────────────────────────────────────
        //
        // HOST · PLAY · SHOP 은 **다른 페이지로 가는 문**이다. 아직 그 페이지가
        // 없으므로, 누른 것이 눌린 채로 보이게만 한다 — 어디에 서 있는지가 보여야
        // "안 눌렸나" 를 다시 누르지 않는다.
        private static readonly string[] Tabs = { "HostButton", "ChapterButton", "ShopButton" };
        private const float TabOnScale = 1.04f;
        private const float TabOffScale = 0.94f;
        private const float TabLift = 10f;     // 켜진 칸만 살짝 올라온다
        private const float TabDim = 0.45f;    // 꺼진 칸 밝기 배수

        /// <summary>칸 하나 — 제자리·제 색을 기억해 둔다. 꺼질 때 곱하고 켜질 때 되돌린다.</summary>
        private sealed class TabView
        {
            public RectTransform Rect;
            public float BaseY;
            public UnityEngine.UI.Graphic[] Graphics;
            public Color[] BaseColors;
        }

        private readonly List<TabView> _tabViews = new();
        private string _activeTab = "ChapterButton";   // 지금 서 있는 곳이 로비(PLAY)다

        private void Awake()
        {
            _ui = new UIBinder(transform);
            // 본문 폰트를 지금 언어 것으로 — 일본어를 한글 폰트로 그리면 한자가 한국식으로 나온다
            Localize.ApplyFonts(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);
            CoreModule.TryGet<IChestService>(out _chests);

            gameObject.AddComponent<BackButtonRouter>();
            global::Game.Module.Common.GameSound.Music("screen.lobby");
            _ui.SetText("VersionText", $"v{Application.version}");

            // 하단 바 — 누르면 그 칸이 켜진 채로 남는다
            _ui.OnClick("HostButton",    () => SelectTab("HostButton"));
            _ui.OnClick("ChapterButton", () => SelectTab("ChapterButton"));
            _ui.OnClick("ShopButton",    () => SelectTab("ShopButton"));

            // 게임 모드 — 가운데가 고른 것, 옆칸·화살표는 돌린다
            _ui.OnClick("ModePlayButton", PlaySelectedMode);
            _ui.OnClick("ModeCardCenter", PlaySelectedMode);
            _ui.OnClick("ModeCardLeft",   () => RotateMode(-1));
            _ui.OnClick("ModeCardRight",  () => RotateMode(+1));
            _ui.OnClick("ModeArrowLeft",  () => RotateMode(-1));
            _ui.OnClick("ModeArrowRight", () => RotateMode(+1));

            // 하단 바 문구는 목업대로 — 가운데는 CHAPTER 가 아니라 PLAY 다
            _ui.SetText("ChapterButtonTitleText", "PLAY");
            _ui.SetText("ChapterButtonSubText", Localize.Get("ui.lobby.game_mode"));

            // 상자 세 칸 — 누르면 시간이 남았으면 젬으로 열고, 다 됐으면 보상을 받는다
            for (int i = 0; i < ChestSlots; i++)
            {
                int slot = i;   // 클로저가 루프 변수를 잡지 않게 복사한다
                var root = _ui.Find($"ChestSlot{i + 1}");
                if (root == null) continue;
                var button = _ui.Find(root, "ChestActionButton")?.GetComponent<UnityEngine.UI.Button>();
                if (button != null) button.onClick.AddListener(() => OnChestAction(slot));
            }

            // 유령 수색 · 상자 「빈 칸」 글자는 프리팹에 고정이다 — `LocalizedText` 가 칠한다
            ApplyModes();
            ApplyTabs();
            ApplyChests();

            foreach (var (element, label) in NotReady)
            {
                var captured = label;
                _ui.OnClick(element, () => NotifyNotReady(captured));
            }
        }

        // ── 보물상자 ─────────────────────────────────────────────
        //
        // 칸마다 세 가지 모습이 있다. 어느 것이 켜지는지가 곧 상태다 —
        //   빈 칸      「빈 칸」 글자만
        //   세는 중    상자 그림 · 남은 시간 · 「젬 n · 즉시 열기」
        //   다 됨      상자 그림 · 「완료!」 띠 · 「보상 획득하기」
        //
        // ⚠ 매 프레임 다시 그리지 않는다. 1초에 한 번 `ChestChangedEvent` 가 오고,
        //   세는 동안의 「남은 시간」 글자만 따로 1초마다 고친다.

        private const int ChestSlots = 3;
        private const float ChestTickSeconds = 1f;
        private float _chestTick;

        /// <summary>
        /// 다 된 칸에서 버튼 글자를 올리는 양. 젬값 줄이 사라진 만큼이다.
        ///
        /// 세는 중엔 「젬값 / 즉시 열기」 두 줄이라 글자가 버튼 아래쪽에 앉는다.
        /// 다 되면 한 줄뿐이라 그대로 두면 버튼 바닥에 붙어 잘려 보인다.
        /// </summary>
        private const float ChestLabelReadyLift = 22f;

        /// <summary>표가 적어 둔 버튼 글자의 제자리. 올렸다 내렸다 하려면 기준이 있어야 한다.</summary>
        private float _chestLabelY;
        private bool _chestLabelYRead;

        private void Update()
        {
            if (_chests == null) return;
            _chestTick += Time.unscaledDeltaTime;
            if (_chestTick < ChestTickSeconds) return;
            _chestTick = 0f;
            ApplyChests();
        }

        private void ApplyChests()
        {
            if (_chests == null && !CoreModule.TryGet<IChestService>(out _chests)) return;

            for (int i = 0; i < ChestSlots; i++)
            {
                var root = _ui.Find($"ChestSlot{i + 1}");
                if (root == null) continue;

                if (!_chestLabelYRead)
                {
                    var first = _ui.Find(root, "ChestActionLabelText") as RectTransform;
                    if (first != null) { _chestLabelY = first.anchoredPosition.y; _chestLabelYRead = true; }
                }

                var s = _chests.Get(i);
                bool has = !s.IsEmpty;
                bool ready = s.IsReady;

                SetIn(root, "ChestEmptyText", !has);
                SetIn(root, "ChestArt", has);
                SetIn(root, "ChestReadyBanner", ready);
                SetIn(root, "ChestReadyText", ready);
                SetIn(root, "ChestTimeIcon", has && !ready);
                SetIn(root, "ChestTimeText", has && !ready);
                SetIn(root, "ChestActionButton", has);
                SetIn(root, "ChestActionGemIcon", has && !ready);
                SetIn(root, "ChestActionCostText", has && !ready);
                SetIn(root, "ChestActionLabelText", has);

                if (!has) continue;

                var art = _ui.Find(root, "ChestArt")?.GetComponent<UnityEngine.UI.Image>();
                if (art != null)
                {
                    var sprite = ChestSpriteOf(s.ChestKey);
                    if (sprite != null && art.sprite != sprite) art.sprite = sprite;
                    // 그림이 없으면 흰 네모가 뜬다 — 차라리 안 보이는 편이 낫다
                    art.enabled = sprite != null;
                }

                // 다 된 칸은 금색 버튼으로 바뀐다 — 어느 칸을 눌러야 하는지가 색으로 보인다
                var button = _ui.Find(root, "ChestActionButton")?.GetComponent<UnityEngine.UI.Image>();
                if (button != null)
                {
                    var want = ready ? _chestButtonGold : _chestButtonBlue;
                    if (want != null && button.sprite != want) button.sprite = want;
                }
                // 금색 버튼 위에 흰 글자는 안 읽힌다
                var label = _ui.Find(root, "ChestActionLabelText")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (label != null)
                {
                    label.color = ready ? new Color(0.16f, 0.11f, 0.02f) : Color.white;
                    // 다 된 칸은 젬값 줄이 사라진다 — 글자를 버튼 한가운데로 올린다
                    var lr = (RectTransform)label.transform;
                    var p = lr.anchoredPosition;
                    p.y = _chestLabelY + (ready ? ChestLabelReadyLift : 0f);
                    lr.anchoredPosition = p;
                }

                TextIn(root, "ChestTimeText", Remain(s.RemainSeconds));
                TextIn(root, "ChestActionCostText", s.GemCost.ToString("N0"));
                TextIn(root, "ChestActionLabelText",
                       Localize.Get(ready ? "ui.lobby.chest.claim" : "ui.lobby.chest.open_now"));
            }
        }

        /// <summary>
        /// 남은 시간 — 목업대로 「3시간 12분」 꼴. 1분 미만은 초로 적는다.
        ///
        /// ⚠ 단위 자리가 언어마다 달라(「3h 12m」) 꼴을 통째로 표에서 읽는다.
        ///   숫자만 갈아 끼우면 일본어·영어에서 말이 안 된다.
        /// </summary>
        private static string Remain(int seconds)
        {
            if (seconds <= 0) return Localize.Format("ui.lobby.chest.time_s", 0);
            int h = seconds / 3600, m = seconds % 3600 / 60;
            if (h > 0) return Localize.Format("ui.lobby.chest.time_h", h, m);
            if (m > 0) return Localize.Format("ui.lobby.chest.time_m", m);
            return Localize.Format("ui.lobby.chest.time_s", seconds);
        }

        private Sprite ChestSpriteOf(string chestKey)
        {
            if (_chestArts == null || string.IsNullOrEmpty(chestKey)) return null;
            for (int i = 0; i < _chestArts.Length; i++)
                if (_chestArts[i].Key == chestKey) return _chestArts[i].Sprite;
            return null;
        }

        private void SetIn(Transform root, string name, bool on)
        {
            var t = _ui.Find(root, name);
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }

        private void TextIn(Transform root, string name, string value)
        {
            var t = _ui.Find(root, name)?.GetComponent<TMPro.TextMeshProUGUI>();
            if (t != null) t.text = value;
        }

        private void OnChestAction(int slot)
        {
            if (_chests == null) return;
            var s = _chests.Get(slot);
            if (s.IsEmpty) return;

            if (s.IsReady)
            {
                if (_chests.TryClaim(slot, out _)) global::Game.Module.Common.GameSound.Cue("run.gold");
                return;
            }

            // 젬이 모자라면 아무 일도 안 일어난 듯 보인다 — 왜 안 열렸는지 알려 준다
            if (!_chests.TryOpenNow(slot))
                NotifyNotReady(Localize.Format("ui.lobby.chest.need_gem", s.GemCost.ToString("N0")));
            else global::Game.Module.Common.GameSound.Cue("run.shop");
        }

        // ── 게임 모드 ────────────────────────────────────────────

        private void RotateMode(int step)
        {
            _modeIndex = (_modeIndex + step + Modes.Length) % Modes.Length;
            ApplyModes();
        }

        private void PlaySelectedMode()
        {
            var mode = Modes[_modeIndex];
            if (!mode.Unlocked) { NotifyNotReady(mode.Name); return; }
            global::Game.Module.Common.GameSound.Cue("ui.play");   // 원작 코인 투입음
            OpenHostSelect(true);
        }

        /// <summary>세 칸에 모드를 다시 꽂는다. 가운데가 고른 것이다.</summary>
        private void ApplyModes()
        {
            int left = (_modeIndex + Modes.Length - 1) % Modes.Length;
            int right = (_modeIndex + 1) % Modes.Length;

            SetSideCard("ModeCardLeft", left);
            SetSideCard("ModeCardRight", right);

            var mid = Modes[_modeIndex];
            SetArt(_ui.Find("ModeCenterArt"), _modeIndex);
            _ui.SetText("ModeCenterTitleText", mid.Name);
            _ui.SetText("ModeCenterSubText", CenterDescOf(mid));
            // ⚠ 잠겼다고 그림을 끄지 마라. 가운데 칸이 통째로 시커먼 판이 되어 고장 난 것처럼
            //   보였다(2026-09-16). 목업도 잠긴 칸에 그림을 두고 자물쇠만 얹는다.
            _ui.SetActive("ModeCenterArt", true);
            _ui.SetActive("ModeCenterLockIcon", !mid.Unlocked);
            _ui.SetActive("ModePlayButton", mid.Unlocked);
            // MAIN 딱지는 **주 콘텐츠에만** 붙는다. 아무 칸에나 붙으면 표시가 아니라 장식이 된다.
            _ui.SetActive("ModeMainBadge", _modeIndex == ScenarioIndex);
        }

        /// <summary>
        /// 가운데 칸 설명. 시나리오는 지금 어디까지 왔는지가 설명보다 쓸모 있다 —
        /// 챕터 카드를 걷어냈으므로 그 정보가 갈 곳이 여기뿐이다.
        /// </summary>
        private string CenterDescOf(GameMode mode)
        {
            // ⚠ 카드 폭(248)을 넘기면 옆칸 위로 글자가 올라탄다. 챕터 이름까지 넣었더니
            //   실제로 그랬다 — 번호와 진행도만 적는다. 이름은 들어가서 볼 자리가 따로 있다.
            if (!mode.Unlocked || _player == null || !_player.IsReady) return mode.Desc;
            return $"CH {_player.CurrentChapter:00}   ·   {_player.ReachedStage} / 30";
        }

        private void SetSideCard(string card, int modeIndex)
        {
            var root = _ui.Find(card);
            if (root == null) return;
            var mode = Modes[modeIndex];
            var title = _ui.Find(root, "ModeTitleText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (title != null) title.text = mode.Name;
            var sub = _ui.Find(root, "ModeSubText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (sub != null) sub.text = mode.Desc;
            var lockIcon = _ui.Find(root, "ModeLockIcon");
            if (lockIcon != null) lockIcon.gameObject.SetActive(!mode.Unlocked);
            SetArt(_ui.Find(root, "ModeCardArt"), modeIndex);
        }

        /// <summary>그 칸에 그 모드의 그림을 끼운다. 그림이 없으면 칸을 비워 둔다.</summary>
        private void SetArt(Transform target, int modeIndex)
        {
            var img = target != null ? target.GetComponent<UnityEngine.UI.Image>() : null;
            if (img == null) return;
            var sprite = _modeArts != null && modeIndex >= 0 && modeIndex < _modeArts.Length
                ? _modeArts[modeIndex] : null;
            img.sprite = sprite;
            img.color = sprite != null ? Color.white : new Color(0.16f, 0.22f, 0.36f, 1f);
        }

        // ── 하단 바 ──────────────────────────────────────────────

        private void SelectTab(string tab)
        {
            _activeTab = tab;
            ApplyTabs();

            // 갈 곳이 있는 것만 실제로 간다. 나머지는 눌린 티만 남기고 안내한다.
            if (tab == "HostButton") OpenHostSelect(false);
            else if (tab == "ShopButton") NotifyNotReady("상점");
        }

        /// <summary>프리팹이 칠해 둔 색이 원본이다. 한 번만 담아 두고 그 뒤로는 곱하기만 한다.</summary>
        private void CacheTabs()
        {
            _tabViews.Clear();
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (_ui.Find(Tabs[i]) is not RectTransform rt) continue;
                var graphics = rt.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                var colors = new Color[graphics.Length];
                for (int g = 0; g < graphics.Length; g++) colors[g] = graphics[g].color;
                _tabViews.Add(new TabView
                {
                    Rect = rt,
                    BaseY = rt.anchoredPosition.y,
                    Graphics = graphics,
                    BaseColors = colors,
                });
            }
        }

        private void ApplyTabs()
        {
            if (_tabViews.Count != Tabs.Length) CacheTabs();

            for (int i = 0; i < _tabViews.Count; i++)
            {
                var view = _tabViews[i];
                if (view.Rect == null) continue;
                bool on = view.Rect.name == _activeTab;

                view.Rect.localScale = Vector3.one * (on ? TabOnScale : TabOffScale);
                var pos = view.Rect.anchoredPosition;
                pos.y = view.BaseY + (on ? TabLift : 0f);
                view.Rect.anchoredPosition = pos;

                // 액자만 어둡게 하면 그림이 그대로라 티가 안 난다 — 칸 전체를 함께 낮춘다.
                for (int g = 0; g < view.Graphics.Length; g++)
                {
                    var target = view.BaseColors[g];
                    if (!on) target = new Color(target.r * TabDim, target.g * TabDim, target.b * TabDim, target.a);
                    view.Graphics[g].color = target;
                }
            }
        }

        private void OnEnable()
        {
            var bus = CoreModule.Get<IEventBus>();
            _tokens.Add(bus.Subscribe<UserDataReadyEvent>(_ => Refresh()));
            _tokens.Add(bus.Subscribe<CurrencyChangedEvent>(_ => RefreshCurrency()));
            _tokens.Add(bus.Subscribe<ProgressChangedEvent>(_ => RefreshChapter()));
            _tokens.Add(bus.Subscribe<ChestChangedEvent>(_ => ApplyChests()));
            _tokens.Add(bus.Subscribe<HostSelectRequestedEvent>(OnHostSelectRequested));
            Refresh();
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        private void Refresh()
        {
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
            if (_player == null || !_player.IsReady) return;
            RefreshCurrency();
            RefreshChapter();
            ApplyChests();
        }

        private void RefreshCurrency()
        {
            if (_player == null || !_player.IsReady) return;
            // 스태미나·고스트 Lv 위젯은 2026-09-16 목업에서 걷어냈다 — 칠할 칸이 없다
            _ui.SetText("GoldText", _player.Gold.ToString("N0"));
            _ui.SetText("GemText", _player.Gem.ToString("N0"));
        }

        private void RefreshChapter()
        {
            if (_player == null || !_player.IsReady) return;
            // 신규 유저에게 '이어서 하기'는 성립하지 않는다 — 상태별 라벨 전환
            bool started = _player.ReachedStage > 1 || _player.ClearedChapter > 0;
            _ui.SetText("ModePlayButtonText",
                        (started ? Localize.Get("ui.lobby.continue") : Localize.Get("ui.lobby.play")) + "  ▶");
            ApplyModes();   // 가운데 칸이 진행도를 적는다
        }

        private void OpenHostSelect(bool isChapterStart)
        {
            CoreModule.Get<IEventBus>()
                .Publish(new HostSelectRequestedEvent { IsChapterStart = isChapterStart });
        }

        private void OnHostSelectRequested(HostSelectRequestedEvent e)
        {
            if (_hostSelectPanel == null)
            {
                Debug.LogError("[LobbyMainUI] HostSelectPanel 이 연결되지 않았습니다.");
                return;
            }
            _hostSelectPanel.Open(e.IsChapterStart);
        }

        /// <summary>로비에서 열리는 것은 호스트 선택 패널뿐이다. 열려 있으면 그것부터 닫는다.</summary>
        public bool OnBackPressed()
        {
            if (_hostSelectPanel != null && _hostSelectPanel.IsOpen)
            {
                _hostSelectPanel.Close();
                return true;
            }
            return false;
        }

        private void NotifyNotReady(string label)
        {
            CoreModule.Get<IEventBus>()
                .Publish(new NotImplementedFeatureEvent { FeatureLabel = label });
            Debug.Log($"[Lobby] 준비 중입니다 — {label}");
        }
    }
}
