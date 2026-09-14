using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        private const float ExpBarWidth = 84f;   // 목업 실측 — GhostExpBarBg 폭

        // TBD-CH — 챕터 미기획. 목업이 노출한 CH3 값만 실제 문구다.
        private static readonly string[] ChapterNames = { "FACTORY", "HARBOR", "TOWER" };
        private static readonly string[] BossNames = { "MAD DOCTOR", "IRON CLAW", "OVERLORD" };

        [SerializeField] private HostSelectPanel _hostSelectPanel;

        private UIBinder _ui;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();

        // 1차 범위 밖 — 버튼은 두되 누르면 준비중 안내만 띄운다.
        // ⚠ 시즌패스 · 이벤트 · 일일로그인 · 기능탭은 로비에서 **걷어냈다**(2026-09-15 목업).
        //   꺼진 오브젝트에 리스너를 걸면 "왜 안 눌리지" 를 다시 찾게 되므로 목록에서도 뺀다.
        private static readonly (string element, string label)[] NotReady =
        {
            ("MailButton", "우편"), ("SettingsButton", "설정"),
        };

        // ── 게임 모드 ────────────────────────────────────────────
        //
        // 세 칸짜리 회전 목마다. **가운데가 곧 고른 것**이고, 좌우 화살표나 옆칸을
        // 누르면 그 모드가 가운데로 온다. 지금 열린 것은 시나리오 하나뿐이라
        // 나머지 둘은 자물쇠가 붙고 플레이 버튼이 사라진다.
        private readonly struct GameMode
        {
            public readonly string Name, Desc;
            public readonly bool Unlocked;
            public GameMode(string name, string desc, bool unlocked)
            { Name = name; Desc = desc; Unlocked = unlocked; }
        }

        private static readonly GameMode[] Modes =
        {
            new("서바이벌 모드", "끝까지 살아남아라", false),
            new("시나리오 모드", "영혼이 깃든 새로운 이야기", true),
            new("디펜스 모드",   "몰려오는 적을 막아라", false),
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
            CoreModule.TryGet<IPlayerDataService>(out _player);

            gameObject.AddComponent<BackButtonRouter>();
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
            _ui.SetText("ChapterButtonSubText", "게임 모드");

            ApplyModes();
            ApplyTabs();

            foreach (var (element, label) in NotReady)
            {
                var captured = label;
                _ui.OnClick(element, () => NotifyNotReady(captured));
            }
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
            OpenHostSelect(true);
        }

        /// <summary>세 칸에 모드를 다시 꽂는다. 가운데가 고른 것이다.</summary>
        private void ApplyModes()
        {
            int left = (_modeIndex + Modes.Length - 1) % Modes.Length;
            int right = (_modeIndex + 1) % Modes.Length;

            SetSideCard("ModeCardLeft", Modes[left]);
            SetSideCard("ModeCardRight", Modes[right]);

            var mid = Modes[_modeIndex];
            _ui.SetText("ModeCenterTitleText", mid.Name);
            _ui.SetText("ModeCenterSubText", CenterDescOf(mid));
            _ui.SetActive("ModeCenterArt", mid.Unlocked);
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

        private void SetSideCard(string card, GameMode mode)
        {
            var root = _ui.Find(card);
            if (root == null) return;
            var title = _ui.Find(root, "ModeTitleText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (title != null) title.text = mode.Name;
            var sub = _ui.Find(root, "ModeSubText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (sub != null) sub.text = mode.Desc;
            var lockIcon = _ui.Find(root, "ModeLockIcon");
            if (lockIcon != null) lockIcon.gameObject.SetActive(!mode.Unlocked);
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
            _tokens.Add(bus.Subscribe<GhostProgressChangedEvent>(_ => RefreshGhost()));
            _tokens.Add(bus.Subscribe<ProgressChangedEvent>(_ => RefreshChapter()));
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
            RefreshGhost();
            RefreshChapter();
        }

        private void RefreshCurrency()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("StaminaText", $"{_player.Stamina}/{_player.StaminaMax}");
            _ui.SetText("GoldText", _player.Gold.ToString("N0"));
            _ui.SetText("GemText", _player.Gem.ToString("N0"));
        }

        private void RefreshGhost()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("GhostLabelText", "GHOST");
            _ui.SetText("GhostLevelText", $"Lv.{_player.GhostLevel}");
            _ui.SetText("GhostExpText", $"{_player.GhostExp} / {_player.GhostExpMax}");
            float r = _player.GhostExpMax > 0 ? (float)_player.GhostExp / _player.GhostExpMax : 0f;
            _ui.SetFill("GhostExpBarFill", r, ExpBarWidth);
        }

        private void RefreshChapter()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("ChapterNumberText", $"CHAPTER {_player.CurrentChapter:00}");
            _ui.SetText("ProgressText", $"{_player.ReachedStage} / 30");

            // 챕터 콘텐츠 미기획 — 목업(CH3)의 문구를 임시로 쓴다.
            // ChapterTable 이 생기면 이 배열을 걷어내고 데이터에서 읽는다. (TBD-CH)
            int idx = Mathf.Clamp(_player.CurrentChapter - 1, 0, ChapterNames.Length - 1);
            _ui.SetText("ChapterNameText", ChapterNames[idx]);
            _ui.SetText("BossNameText", BossNames[idx]);
            // 신규 유저에게 '이어서 하기'는 성립하지 않는다 — 상태별 라벨 전환
            bool started = _player.ReachedStage > 1 || _player.ClearedChapter > 0;
            _ui.SetText("ContinueButtonText", started ? "CONTINUE" : "START");
            _ui.SetText("ModePlayButtonText", (started ? "이어서 하기" : "플레이하기") + "  ▶");
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
