using System;
using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.Common.Chest;
using Game.Module.Common.Modes;
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
    /// 화면은 시안(lobby_hub_v3_jp) 픽셀 그대로다 — `LobbyV4Builder` 가 세운다.
    ///   PLAY(시나리오 배너 · 플레이 버튼) → 챕터 선택 → 호스트 선택
    ///   HOST                              → 조회 · 강화 모드
    /// </summary>
    public sealed class LobbyMainUI : MonoBehaviour, IBackTarget
    {
        [SerializeField] private HostSelectPanel _hostSelectPanel;

        /// <summary>PLAY → 챕터 선택 → 호스트 선택 (기획 2026-09-18). ⚠ 아래 통합 창으로 대체됐다.</summary>
        [SerializeField] private ChapterSelectPanel _chapterSelectPanel;

        /// <summary>
        /// 챕터와 호스트를 **한 창에서** 고른다 (기획 2026-09-29).
        /// 이 판이 있으면 PLAY 는 여기로 간다 — 위 두 판(챕터 선택 · 호스트 선택)을 대신한다.
        /// </summary>
        [SerializeField] private ChapterHostPanel _chapterHostPanel;

        /// <summary>상자를 연 뒤 받은 것을 카드로 늘어놓는 창.</summary>
        [SerializeField] private ChestRewardPopup _chestRewardPopup;

        /// <summary>육성 화면 — 하단 바 HOST 칸(기획 2026-09-21).</summary>
        [SerializeField] private GrowthPanel _growthPanel;

        /// <summary>추가 모드 입장 패널 — 디펜스 칸 → 망령의 탑 · 서바이벌 칸 → 망령의 밤(PD 2026-10-15).</summary>
        [SerializeField] private TowerEntryPanel _towerEntryPanel;
        [SerializeField] private NightEntryPanel _nightEntryPanel;

        /// <summary>로비 상점 — 하단 바 SHOP 칸(PD 2026-10-15, 시안 S02~S07). 노드는 `LobbyShopBuilder` 가 세운다.</summary>
        [SerializeField] private LobbyShopPanel _shopPanel;

        /// <summary>소환(조각 뽑기) — 하단 바 SUMMON 칸(PD 2026-10-15 A안). 노드는 `SummonBuilder` 가 세운다.</summary>
        [SerializeField] private SummonPanel _summonPanel;

        /// <summary>시즌 패스 — 위 막대 시즌 배지로 연다(시안 S16 · S17). 노드는 `SeasonPassBuilder` 가 세운다.</summary>
        [SerializeField] private SeasonPassPanel _passPanel;

        /// <summary>프로필 꾸미기 — 왼쪽 위 프로필을 누르면 뜬다(시안 `mock_store_profile`). 노드는 `ProfileBuilder`.</summary>
        [SerializeField] private ProfilePopup _profilePopup;

        /// <summary>
        /// 상자 등급별 그림. 어느 칸에 무슨 등급이 오는지가 매번 달라 코드가 갈아 끼운다.
        ///
        /// ⚠ 표(`ChestTable`)가 등급을 정하고 여기는 그림만 갖는다 — 등급이 늘면
        ///   표에 줄을 더하고 여기에 짝을 더한다.
        /// </summary>
        [Serializable]
        private struct ChestArt
        {
            public string Key;
            public Sprite Sprite;
        }

        [SerializeField] private ChestArt[] _chestArts = Array.Empty<ChestArt>();

        /// <summary>시계와 남은 시간 사이 간격 · 덩어리가 판 가운데에서 비낀 양(캔버스 px). 빌더가 시안에서 잰 값으로 덮는다.</summary>
        [SerializeField] private float _timeRowGap = 22f;
        [SerializeField] private float _timeRowShift;
        /// <summary>「プレイ ▶」 — 글자 끝 → ▶ 간격, 덩어리 가운데(버튼 왼쪽 기준). 빌더가 시안에서 잰 값으로 덮는다.</summary>
        [SerializeField] private float _playArrowGap = 9f;
        [SerializeField] private float _playGroupCenter = 92f;
        /// <summary>아래 바 위 쪽 표시 점 — 고른 칸(HOST · PLAY · SHOP)의 점에 불이 들어온다(PD 2026-10-06).</summary>
        [SerializeField] private Sprite _navDotOn, _navDotOff;

        private UIBinder _ui;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();

        // 1차 범위 밖 — 버튼은 두되 누르면 준비중 안내만 띄운다.
        // ⚠ 유령 관찰 · 시즌 패스 · 이벤트는 임시 화면이라 로비에서 걷어냈다(PD 2026-10-06). 판 그림도 배경에서 지웠다.
        private static readonly (string element, string label)[] NotReady =
        {
            ("MailButton", "우편"), ("SettingsButton", "설정"),
            ("GoldPlusButton", "상점"), ("GemPlusButton", "상점"),
        };

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Game.Module.Common.UI.SystemPopup.Preload();   // 공통 알림 그림을 미리 받는다 — 첫 알림이 색 상자로 뜨지 않게
            // 본문 폰트를 지금 언어 것으로 — 일본어를 한글 폰트로 그리면 한자가 한국식으로 나온다
            Localize.ApplyFonts(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);

            gameObject.AddComponent<BackButtonRouter>();
            global::Game.Module.Common.GameSound.Music("screen.lobby");

            // 호스트 선택 판은 **로비가** 닫아 둔다. 판이 제 `Awake` 에서 스스로 끄면
            // 켜지는 도중이라 그 프레임에 안 먹어, 판 아래 팁 띠가 로비 바닥으로
            // 삐져나왔다(2026-09-16).
            if (_hostSelectPanel != null) _hostSelectPanel.Close();
            if (_chapterSelectPanel != null) _chapterSelectPanel.Close();
            if (_chapterHostPanel != null) _chapterHostPanel.Close();
            if (_chestRewardPopup != null) _chestRewardPopup.Close();
            if (_towerEntryPanel != null) _towerEntryPanel.Close();
            if (_nightEntryPanel != null) _nightEntryPanel.Close();

            // 하단 바 — 로비 · 육성이 **같이 쓴다**. 누른 칸이 선택 모습이 되고 위 화면만 바뀐다(기획 2026-09-21)
            //   HOST → 육성 화면 · PLAY → 로비 · SHOP → 준비 중(선택은 그대로)
            if (_growthPanel != null) _growthPanel.Close();
            _ui.OnClick("HostButton", () => SelectTab(Tab.Host));
            // 왼쪽 위 유령 프로필 — 누르면 육성의 유령 탭으로(유령 = 플레이어 자신)
            // 꾸미기 창이 있으면 그것(시안 「프로필은 로비 왼쪽 위에서 변경」), 없으면 예전처럼 유령 육성
            if (_profilePopup != null) _profilePopup.Close();
            _ui.OnClick("LobbyProfile", () =>
            {
                if (_profilePopup != null) _profilePopup.Open();
                else OpenGhostGrowth();
            });
            _ui.OnClick("ChapterButton", () => SelectTab(Tab.Play));
            // 상점 창이 없는(세우기 전) 프리팹이면 예전처럼 준비 중 안내
            if (_shopPanel != null) _shopPanel.Close();
            _ui.OnClick("ShopButton", () =>
            {
                if (_shopPanel != null) SelectTab(Tab.Shop);
                else NotifyNotReady("상점");
            });
            // 소환은 상점과 칸을 나눴다(PD 2026-10-15 A안). 소환 화면은 다음 단계 — 지금은 준비 중 안내
            if (_summonPanel != null) _summonPanel.Close();
            if (_passPanel != null) _passPanel.Close();
            _ui.OnClick("PassBadge", OpenPass);
            // 홈 v5 진열 칸의 PASS 단추(PD 2026-10-17 — 패스는 칸을 차지하지 않고 떠 있는 단추로)
            _ui.OnClick("HomePassButton", OpenPass);
            _ui.OnClick("SummonButton", () =>
            {
                if (_summonPanel != null) SelectTab(Tab.Summon);
                else NotifyNotReady("소환");
            });
            // 위 막대 행동력 「+」 — 젬으로 채우기(기획서 B-12)
            _ui.OnClick("StaminaPlusButton", OpenStaminaRefill);
            SelectTab(Tab.Play);

            // 게임 모드 — 열린 것은 시나리오뿐이다. 잠긴 두 칸은 이름을 알려 준다
            _ui.OnClick("ModeScenarioCard", PlayScenario);
            _ui.OnClick("ModePlayButton", PlayScenario);
            // 서바이벌 칸 = 망령의 밤 · 디펜스 칸 = 망령의 탑(PD 2026-10-15). 판이 아직 프리팹에 없으면 예전 안내로
            _ui.OnClick("ModeSurvivalCard", OpenNight);
            _ui.OnClick("ModeDefenseCard", OpenTower);
            // 홈 v5 — 시나리오 · 탑 · 밤 큰 카드(PD 2026-10-17 「모드 각각 눌러서 들어간다」)
            _ui.OnClick("HomeScenarioCard", PlayScenario);
            _ui.OnClick("HomeContinueButton", PlayScenario);
            _ui.OnClick("HomeTowerCard", OpenTower);
            _ui.OnClick("HomeNightCard", OpenNight);
            // 미션 · 업적은 화면이 아직 없다 — 자리만 먼저(PD 「나중에 미션이나 업적 들어갈 수도」)
            _ui.OnClick("HomeMissionButton", () => NotifyNotReady(Localize.Get("ui.home.mission")));
            _ui.OnClick("HomeAchieveButton", () => NotifyNotReady(Localize.Get("ui.home.achieve")));

            // 보물상자 칸은 없앴다(PD 2026-10-17 「상자 기능 제거 — 인게임 보상에서도 안 준다, 추후 상점 판매만」).
            // 칸 노드 · 그림 필드는 새 로비 UI(v3)를 지을 때 빌더와 함께 걷어 낸다
            _ui.SetActive("ChestBand", false);

            foreach (var (element, label) in NotReady)
            {
                var captured = label;
                _ui.OnClick(element, () => NotifyNotReady(captured));
            }

            _playLabelDirty = true;
        }

        // 1초 시계 — 행동력 회복 글자만 1초에 한 번 고친다(매 프레임 계산하지 않는다)
        private const float TickSeconds = 1f;
        private float _tick;

        // 「プレイ」 글자는 `LocalizedText` 가 채운다 — 채우기 전에 재면 빌더 기본 글자(한국어) 폭으로 ▶ 가
        // 멀리 떨어진다(2026-09-21). 글자가 바뀐 **다음 프레임**에 잰다.
        private bool _playLabelDirty;

        private void Update()
        {
            if (_playLabelDirty)
            {
                _playLabelDirty = false;
                PlacePlayLabel();
            }
            _tick += Time.unscaledDeltaTime;
            if (_tick < TickSeconds) return;
            _tick = 0f;
            RefreshStamina();   // 회복 시계 글자 — 1초에 한 번(매 프레임 계산하지 않는다)
        }

        /// <summary>
        /// 「プレイ ▶」 — 글자 + ▶ 한 덩어리를 버튼 안 시안 가운데에 둔다.
        /// ▶ 는 시안 글꼴의 좁은 삼각형이라 그림으로 붙인다 — 언어마다 글자 길이가 달라도 글자 끝을 따라간다.
        /// </summary>
        private void PlacePlayLabel()
        {
            var parent = _ui.Find("ModePlayButton");
            if (parent == null) return;
            var text = _ui.Find(parent, "ModePlayButtonText")?.GetComponent<TMPro.TextMeshProUGUI>();
            var icon = _ui.Find(parent, "ModePlayButtonArrow") as RectTransform;
            if (text == null || icon == null) return;

            var rt = text.rectTransform;
            text.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Left;
            text.ForceMeshUpdate();
            float sx = rt.localScale.x;
            float textW = text.textBounds.size.x * sx;
            float left = _playGroupCenter - (textW + _playArrowGap + icon.rect.width) * 0.5f;
            // 버튼 안 자식은 왼쪽 위 기준(anchor·pivot 0,1)이라 anchoredPosition 이 곧 부모 왼쪽에서의 거리다
            var p = rt.anchoredPosition;
            p.x = left - text.textBounds.min.x * sx;
            rt.anchoredPosition = p;
            var ip = icon.anchoredPosition;
            ip.x = left + textW + _playArrowGap;
            icon.anchoredPosition = ip;
        }

        // ── 하단 바 ──────────────────────────────────────────────

        private enum Tab { Play, Host, Summon, Shop }

        private static readonly string[] LobbyPageNodes = { "LobbyV4Gap", "LobbyV4Top", "LobbyV4Mid" };
        /// <summary>홈 v5 가 있으면 예전 판(V4)은 꺼 둔 채 이것만 켜고 끈다.</summary>
        private static readonly string[] HomeV5Nodes = { "HomeV5" };

        private void SelectTab(Tab tab)
        {
            bool host = tab == Tab.Host;
            bool shop = tab == Tab.Shop;
            SetNav("HostButton", host);
            SetNav("ChapterButton", tab == Tab.Play);
            bool summon = tab == Tab.Summon;
            SetNav("SummonButton", summon);
            SetNav("ShopButton", shop);
            SetDots(host ? 0 : summon ? 2 : shop ? 3 : 1);
            if (_summonPanel != null)
            {
                if (summon) _summonPanel.Open();
                else _summonPanel.Close();
            }
            if (_passPanel != null) _passPanel.Close();   // 아래 바를 누르면 패스 창은 닫고 그 화면으로
            if (_shopPanel != null)
            {
                if (shop) _shopPanel.Open();
                else _shopPanel.Close();
            }
            // 아래 바는 챕터 창 위에 있어 창이 열린 채로 눌린다 — HOST 든 PLAY 든 누르면 창을 닫고 그 화면으로
            if (_chapterHostPanel != null && _chapterHostPanel.IsOpen) _chapterHostPanel.Close();
            // 위 화면만 바꾼다 — 육성 화면이 켜지면 로비 판은 끈다(가려져 안 보이는데 그리기만 한다)
            var pages = transform.Find("HomeV5") != null ? HomeV5Nodes : LobbyPageNodes;
            for (int i = 0; i < pages.Length; i++)
            {
                var t = transform.Find(pages[i]);
                if (t != null) t.gameObject.SetActive(!host);
            }
            if (_growthPanel == null) return;
            if (host) _growthPanel.Open();
            else _growthPanel.Close();
        }

        /// <summary>상점 위에 뜬 팝업 · 상점을 닫는다. 닫은 것이 없으면 false.</summary>
        private bool BackFromShop()
        {
            if (_profilePopup != null && _profilePopup.IsOpen)
            {
                _profilePopup.Close();
                return true;
            }
            if (_passPanel != null && _passPanel.IsOpen)
            {
                _passPanel.Close();
                return true;
            }
            if (_summonPanel != null && _summonPanel.IsOpen)
            {
                if (!_summonPanel.CloseTopPopup()) SelectTab(Tab.Play);
                return true;
            }
            if (_shopPanel == null || !_shopPanel.IsOpen) return false;
            if (!_shopPanel.CloseTopPopup()) SelectTab(Tab.Play);   // 상점에서 뒤로 = 로비(PLAY 칸)
            return true;
        }

        private void SetDots(int lit)
        {
            for (int i = 0; i < 4; i++)   // 4칸(S22e) — 넷째 점이 없는 옛 프리팹이면 건너뛴다
            {
                var dot = _ui.Find("NavDot" + i)?.GetComponent<UnityEngine.UI.Image>();
                if (dot != null) dot.sprite = i == lit ? _navDotOn : _navDotOff;
            }
        }

        private void SetNav(string node, bool selected)
        {
            var view = _ui.Find(node)?.GetComponent<NavTabView>();
            if (view != null) view.SetSelected(selected);
        }

        // ── 게임 모드 ────────────────────────────────────────────

        private void OpenPass()
        {
            if (_passPanel == null) { NotifyNotReady("시즌 패스"); return; }
            if (_chapterHostPanel != null && _chapterHostPanel.IsOpen) _chapterHostPanel.Close();
            _passPanel.Open();
        }

        private void OpenTower()
        {
            if (_towerEntryPanel != null) { GameSound.Cue("ui.play"); _towerEntryPanel.Open(); }
            else NotifyNotReady(Localize.Get("ui.lobby.mode.defense.name"));
        }

        private void OpenNight()
        {
            if (_nightEntryPanel != null) { GameSound.Cue("ui.play"); _nightEntryPanel.Open(); }
            else NotifyNotReady(Localize.Get("ui.lobby.mode.survival.name"));
        }

        private void PlayScenario()
        {
            global::Game.Module.Common.GameSound.Cue("ui.play");   // 원작 코인 투입음
            // 챕터와 몸을 한 창에서 고른다(기획 2026-09-29).
            // 판이 아직 프리팹에 없으면 예전 길로 떨어진다 — 창이 안 뜨는 것보다 낫다.
            if (_chapterHostPanel != null) _chapterHostPanel.Open();
            else if (_chapterSelectPanel != null) _chapterSelectPanel.Open();
            else OpenHostSelect(true);
        }

        private void OnEnable()
        {
            var bus = CoreModule.Get<IEventBus>();
            _tokens.Add(bus.Subscribe<UserDataReadyEvent>(_ => Refresh()));
            _tokens.Add(bus.Subscribe<CurrencyChangedEvent>(_ => RefreshCurrency()));
            _tokens.Add(bus.Subscribe<GhostProgressChangedEvent>(_ => RefreshProfile()));
            // 코드가 채우는 글자(상자 문구 · 플레이 자리)는 LocalizedText 가 없다 — 언어가 바뀌면 다시 채운다
            _tokens.Add(bus.Subscribe<LanguageChangedEvent>(_ => _playLabelDirty = true));
            _tokens.Add(bus.Subscribe<HostSelectRequestedEvent>(OnHostSelectRequested));
            _tokens.Add(bus.Subscribe<ModeProgressChangedEvent>(_ => RefreshModeCards()));
            _tokens.Add(bus.Subscribe<ChapterSelectRequestedEvent>(_ =>
            {
                if (_chapterSelectPanel != null) _chapterSelectPanel.Open();
            }));
            Refresh();
            LayoutBands();
        }

        // 판 셋 — 위판은 화면 위, 하단 바 판은 바닥에 붙고, 가운데판(상자 · 게임 모드)은 남는 공간의
        // **한가운데**에 선다. 전에는 가운데판이 하단 바에 붙어 있어 20:9 에서 UI 가 아래로 몰리고
        // 가운데에 빈 바닥만 크게 남았다(2026-09-21 지적). 두 틈은 판 뒤에 깐 이어 그린 그림이 메운다.
        private const string TopNode = "LobbyV4Top", MidNode = "LobbyV4Mid", NavNode = "BottomNav";
        /// <summary>기준 해상도 세로(constants.md 3절) — 9:16 캔버스 높이.</summary>
        private const float ReferenceHeight = 1280f;

        private void OnRectTransformDimensionsChange() => LayoutBands();

        private void LayoutBands()
        {
            var top = transform.Find(TopNode) as RectTransform;
            var mid = transform.Find(MidNode) as RectTransform;
            var nav = transform.Find(NavNode) as RectTransform;
            if (top == null || mid == null || nav == null) return;
            float free = ((RectTransform)transform).rect.height - top.rect.height - mid.rect.height - nav.rect.height;
            // 하단 바가 시안보다 작아(2026-09-21) 9:16 에서도 가운데판 아래에 틈이 남는다. 그 몫(slack)은
            // 가운데판 아래(도시)에 두어 9:16 에서 가운데판이 시안 자리에 있게 하고, 그보다 더 남는 것만 위아래로 나눈다.
            float slack = ReferenceHeight - top.rect.height - mid.rect.height - nav.rect.height;
            // 위 · 아래 고정 시안(2026-10-06)은 위 막대 밑으로 판을 내렸다 — 위판 +44, 가운데판 +46 · 세로 0.9637(실측).
            top.anchoredPosition = new Vector2(top.anchoredPosition.x, -FixedTopShift);
            mid.localScale = new Vector3(1f, FixedMidScaleY, 1f);
            if (transform.Find(GapNode) is RectTransform gap)
                gap.anchoredPosition = new Vector2(gap.anchoredPosition.x, -(top.rect.height + FixedTopShift));
            var p = mid.anchoredPosition;
            p.y = -(top.rect.height + FixedMidShift + Mathf.Max(0f, free - Mathf.Max(0f, slack)) * 0.5f);
            mid.anchoredPosition = p;
        }

        private const string GapNode = "LobbyV4Gap";
        private const float FixedTopShift = 44f, FixedMidShift = 46f, FixedMidScaleY = 0.9637f;

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
            RefreshProfile();
            RefreshModeCards();
        }

        /// <summary>모드 칸 아래 글자 · 빨간 점 — 「원거리의 날 · 입장 3/3」 · 「이번 주 12층 · 입장 2/3」(시안 L).</summary>
        private void RefreshModeCards()
        {
            if (!CoreModule.TryGet<IModeProgress>(out var modes)) return;
            modes.Refresh();
            int free = modes.Table != null ? modes.Table.FreeEntriesPerDay : 3;
            string[] days = { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };
            _ui.SetText("ModeSurvivalTitleText", Localize.Get("ui.night.title"));
            _ui.SetText("ModeSurvivalSubText", Localize.Format("ui.lobby.mode.night.sub",
                Localize.Get($"ui.night.day_rule.{days[modes.TodayIndex]}"), modes.NightEntries, free));
            _ui.SetText("ModeDefenseTitleText", Localize.Get("ui.tower.title"));
            _ui.SetText("ModeDefenseSubText", Localize.Format("ui.lobby.mode.tower.sub",
                modes.TowerWeekCleared, modes.TowerEntries, free));
            _ui.SetActive("ModeSurvivalBadge", modes.NightEntries > 0);
            _ui.SetActive("ModeDefenseBadge", modes.TowerEntries > 0 || (!modes.TowerSweptToday && modes.SweepNow != null));
        }

        /// <summary>경험치 채움의 가득 찬 폭 — `FixedBarsBinder.ProfileFillWidth` 와 같아야 한다.</summary>
        private const float ProfileFillWidth = 117f;

        /// <summary>
        /// 왼쪽 위 유령 프로필(A안, PD 확정 2026-10-06) — 유령 레벨 · 경험치.
        /// 유령은 유저 레벨이다. 경험치는 챕터를 깨면 오른다. 최대 레벨이면 「MAX」.
        /// </summary>
        private void RefreshProfile()
        {
            if (_player == null || !_player.IsReady) return;
            int lv = _player.GhostLevel;
            bool max = lv >= _player.GhostLevelMax;
            // 픽셀 글꼴의 띄어쓰기는 한 글자만큼 넓어 「LV.   1」 로 벌어졌다 — 좁은 칸을 준다
            _ui.SetText("ProfileLevelText", $"LV.<space=0.25em>{lv}");
            _ui.SetText("ProfileExpText", max ? "MAX" : $"{_player.GhostExp:N0} / {_player.GhostExpMax:N0}");
            float ratio = max ? 1f : _player.GhostExpMax > 0 ? (float)_player.GhostExp / _player.GhostExpMax : 0f;
            _ui.SetFill("ProfileExpFill", ratio, ProfileFillWidth);
        }

        private void OpenGhostGrowth()
        {
            SelectTab(Tab.Host);
            if (_growthPanel != null) _growthPanel.ShowGhost();
        }

        private void RefreshCurrency()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("GoldText", _player.Gold.ToString("N0"));
            _ui.SetText("GemText", _player.Gem.ToString("N0"));
            RefreshStamina();
        }

        // 넘쳐 있으면(구매 · 보상으로 최대 초과) 금색 — 그동안 자연 회복은 멈춘다
        private static readonly Color StaminaOverColor = new(1f, 0.85f, 0.32f);

        /// <summary>「18/20」 · 다음 회복 「09:12」(가득이면 숨김). 노드가 없는(꽂기 전) 프리팹이면 아무 일 없다.</summary>
        private void RefreshStamina()
        {
            if (_player == null || !_player.IsReady) return;
            if (!CoreModule.TryGet<Game.Module.Common.Store.IStaminaService>(out var st)) return;
            var text = _ui.Get<TMPro.TextMeshProUGUI>("StaminaText");
            if (text == null) return;
            st.Sync();
            int cur = st.Current, max = st.Max;
            text.text = $"{cur}/{max}";
            text.color = cur > max ? StaminaOverColor : Color.white;
            var until = st.UntilNext;
            bool counting = until > TimeSpan.Zero;
            // 시계가 없으면(가득 · 넘침) 「20/20」 한 줄을 칸 세로 가운데로, 있으면 위 줄로
            var tr = text.rectTransform;
            tr.anchoredPosition = new Vector2(tr.anchoredPosition.x, counting ? -22f : -33f);
            _ui.SetActive("StaminaTimerText", counting);
            if (counting) _ui.SetText("StaminaTimerText", $"{(int)until.TotalMinutes:00}:{until.Seconds:00}");
        }

        private void OpenStaminaRefill()
        {
            if (!CoreModule.TryGet<Game.Module.Common.Store.IStaminaService>(out var st)) return;
            StaminaPopup.Show(st, _player, false, RefreshStamina);
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

        /// <summary>로비에서 열리는 창을 위에서부터 닫는다.</summary>
        public bool OnBackPressed()
        {
            if (BackFromShop()) return true;
            if (_towerEntryPanel != null && _towerEntryPanel.IsOpen)
            {
                _towerEntryPanel.Close();
                return true;
            }
            if (_nightEntryPanel != null && _nightEntryPanel.IsOpen)
            {
                _nightEntryPanel.Close();
                return true;
            }
            if (_growthPanel != null && _growthPanel.IsOpen)
            {
                SelectTab(Tab.Play);   // 육성 화면에서 뒤로 = 로비(PLAY 칸)
                return true;
            }
            if (_chestRewardPopup != null && _chestRewardPopup.IsOpen)
            {
                _chestRewardPopup.Close();
                return true;
            }
            if (_hostSelectPanel != null && _hostSelectPanel.IsOpen)
            {
                _hostSelectPanel.Back();   // 판 시작 길이면 챕터 선택으로 돌아간다
                return true;
            }
            if (_chapterSelectPanel != null && _chapterSelectPanel.IsOpen)
            {
                _chapterSelectPanel.Close();
                return true;
            }
            if (_chapterHostPanel != null && _chapterHostPanel.IsOpen)
            {
                _chapterHostPanel.Close();
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
