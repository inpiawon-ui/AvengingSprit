using System;
using System.Collections.Generic;
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

        private UIBinder _ui;
        private IPlayerDataService _player;
        private IChestService _chests;
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
            CoreModule.TryGet<IChestService>(out _chests);

            gameObject.AddComponent<BackButtonRouter>();
            global::Game.Module.Common.GameSound.Music("screen.lobby");

            // 호스트 선택 판은 **로비가** 닫아 둔다. 판이 제 `Awake` 에서 스스로 끄면
            // 켜지는 도중이라 그 프레임에 안 먹어, 판 아래 팁 띠가 로비 바닥으로
            // 삐져나왔다(2026-09-16).
            if (_hostSelectPanel != null) _hostSelectPanel.Close();
            if (_chapterSelectPanel != null) _chapterSelectPanel.Close();
            if (_chapterHostPanel != null) _chapterHostPanel.Close();
            if (_chestRewardPopup != null) _chestRewardPopup.Close();

            // 하단 바 — 로비 · 육성이 **같이 쓴다**. 누른 칸이 선택 모습이 되고 위 화면만 바뀐다(기획 2026-09-21)
            //   HOST → 육성 화면 · PLAY → 로비 · SHOP → 준비 중(선택은 그대로)
            if (_growthPanel != null) _growthPanel.Close();
            _ui.OnClick("HostButton", () => SelectTab(Tab.Host));
            // 왼쪽 위 유령 프로필 — 누르면 육성의 유령 탭으로(유령 = 플레이어 자신)
            _ui.OnClick("LobbyProfile", OpenGhostGrowth);
            _ui.OnClick("ChapterButton", () => SelectTab(Tab.Play));
            _ui.OnClick("ShopButton", () => NotifyNotReady("상점"));
            SelectTab(Tab.Play);

            // 게임 모드 — 열린 것은 시나리오뿐이다. 잠긴 두 칸은 이름을 알려 준다
            _ui.OnClick("ModeScenarioCard", PlayScenario);
            _ui.OnClick("ModePlayButton", PlayScenario);
            _ui.OnClick("ModeSurvivalCard", () => NotifyNotReady(Localize.Get("ui.lobby.mode.survival.name")));
            _ui.OnClick("ModeDefenseCard", () => NotifyNotReady(Localize.Get("ui.lobby.mode.defense.name")));

            // 상자 세 칸 — 카드를 누르면 다 됐으면 보상을 받고, 세는 중이면 젬으로 열지 묻는다
            for (int i = 0; i < ChestSlots; i++)
            {
                int slot = i;   // 클로저가 루프 변수를 잡지 않게 복사한다
                var root = _ui.Find($"ChestSlot{i + 1}");
                var button = root != null ? root.GetComponent<UnityEngine.UI.Button>() : null;
                if (button != null) button.onClick.AddListener(() => OnChestAction(slot));
            }

            foreach (var (element, label) in NotReady)
            {
                var captured = label;
                _ui.OnClick(element, () => NotifyNotReady(captured));
            }

            _playLabelDirty = true;
            ApplyChests();
        }

        // ── 보물상자 ─────────────────────────────────────────────
        //
        // 칸마다 세 가지 모습이 있다. 어느 것이 켜지는지가 곧 상태다 —
        //   빈 칸      「空きスロット」 글자만
        //   세는 중    제목 · 상자 · 시간 판(시계 + 남은 시간)   ← 시안 그대로
        //   다 됨      제목 · 상자 · 시간 판(「完了!」)
        //
        // ⚠ 매 프레임 다시 그리지 않는다. 1초에 한 번 `ChestChangedEvent` 가 오고,
        //   세는 동안의 「남은 시간」 글자만 따로 1초마다 고친다.

        private const int ChestSlots = 3;
        private const float ChestTickSeconds = 1f;
        private float _chestTick;

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

                var s = _chests.Get(i);
                bool has = !s.IsEmpty;
                bool ready = s.IsReady;

                SetIn(root, "ChestEmptyText", !has);
                SetIn(root, "ChestTitleText", has);
                SetIn(root, "ChestArt", has);
                SetIn(root, "ChestTimePlate", has);
                SetIn(root, "ChestTimeIcon", has && !ready);
                SetIn(root, "ChestTimeText", has);

                if (!has) continue;

                var art = _ui.Find(root, "ChestArt")?.GetComponent<UnityEngine.UI.Image>();
                if (art != null)
                {
                    var sprite = ChestSpriteOf(s.ChestKey);
                    if (sprite != null && art.sprite != sprite)
                    {
                        art.sprite = sprite;
                        art.color = Color.white;   // 자리표시 색이 남아 있으면 그림이 물든다
                    }
                    // 그림이 없으면 네모가 뜬다 — 차라리 안 보이는 편이 낫다
                    art.enabled = sprite != null;
                    var layout = root.GetComponent<ChestSlotLayout>();
                    if (layout != null) layout.Apply(art.rectTransform, s.ChestKey);
                }

                TextIn(root, "ChestTitleText", Localize.Get($"chest.{s.ChestKey}.name"));
                TextIn(root, "ChestTimeText", ready ? Localize.Get("ui.lobby.chest.ready") : Remain(s.RemainSeconds));
                if (ready) CenterAlone(root, "ChestTimePlate", "ChestTimeText");
                else CenterRow(root, "ChestTimePlate", "ChestTimeIcon", "ChestTimeText", _timeRowGap, _timeRowShift);
            }
        }

        /// <summary>
        /// 남은 시간 — 시안대로 「3時間 12分」 꼴. 1분 미만은 초로 적는다.
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

        /// <summary>
        /// 아이콘 + 글자 한 줄을 판 가운데에 둔다.
        /// 글자 길이가 「1分」 부터 「3時間 12分」 까지 달라서 자리를 고정하면 짧을 때 덩어리가 쏠린다.
        /// ⚠ 가로 정렬만 바꾼다 — 세로는 글자 모양 기준(Geometry)이어야 시안 줄에 앉는다.
        /// </summary>
        private void CenterRow(Transform slot, string plateName, string iconName, string textName, float gap, float shift)
        {
            var plate = _ui.Find(slot, plateName) as RectTransform;
            var icon = _ui.Find(slot, iconName) as RectTransform;
            var text = _ui.Find(slot, textName)?.GetComponent<TMPro.TextMeshProUGUI>();
            if (plate == null || icon == null || text == null) return;

            var textRect = text.rectTransform;
            text.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Left;
            text.ForceMeshUpdate();
            // 글자 칸은 가로로 눌려 있을 수 있다(시안 글꼴 폭 맞춤) — 보이는 폭은 × localScale.x
            float sx = textRect.localScale.x;
            float textW = text.textBounds.size.x * sx;
            float iconW = icon.rect.width;
            float group = iconW + gap + textW;

            // 셋 다 같은 부모 안이라 localPosition 으로 맞춘다 — 앵커가 달라도 같은 자로 잰다
            float center = plate.localPosition.x + (0.5f - plate.pivot.x) * plate.rect.width;
            float left = center + shift - group * 0.5f;

            var ip = icon.localPosition;
            ip.x = left + icon.pivot.x * iconW;
            icon.localPosition = ip;

            // 글자 잉크의 왼쪽 끝을 맞춘다 — textBounds 는 글자 칸 기준점(pivot) 기준이다
            var tp = textRect.localPosition;
            tp.x = left + iconW + gap - text.textBounds.min.x * sx;
            textRect.localPosition = tp;
        }

        /// <summary>시계 없이 글자만 — 「完了!」 는 판 가운데에 선다.</summary>
        private void CenterAlone(Transform slot, string plateName, string textName)
        {
            var plate = _ui.Find(slot, plateName) as RectTransform;
            var text = _ui.Find(slot, textName)?.GetComponent<TMPro.TextMeshProUGUI>();
            if (plate == null || text == null) return;
            var rt = text.rectTransform;
            text.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Left;
            text.ForceMeshUpdate();
            float sx = rt.localScale.x;
            float center = plate.localPosition.x + (0.5f - plate.pivot.x) * plate.rect.width;
            var tp = rt.localPosition;
            tp.x = center - text.textBounds.size.x * sx * 0.5f - text.textBounds.min.x * sx;
            rt.localPosition = tp;
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
            if (t != null && t.text != value) t.text = value;
        }

        private void OnChestAction(int slot)
        {
            if (_chests == null) return;
            var s = _chests.Get(slot);
            if (s.IsEmpty) return;

            if (s.IsReady)
            {
                if (_chests.TryClaim(slot, out var reward))
                {
                    global::Game.Module.Common.GameSound.Cue("run.gold");
                    ShowChestReward(reward);
                }
                return;
            }

            // 시안엔 젬 버튼이 없다 — 카드를 누르면 젬으로 지금 열지 묻는다
            SystemPopup.Show(Localize.Format("ui.lobby.chest.open_confirm", s.GemCost.ToString("N0")),
                             () => OpenNow(slot));
        }

        private void OpenNow(int slot)
        {
            var s = _chests.Get(slot);
            if (s.IsEmpty || s.IsReady) return;
            // 젬이 모자라면 아무 일도 안 일어난 듯 보인다 — 왜 안 열렸는지 알려 준다
            if (!_chests.TryOpenNow(slot))
                NotifyNotReady(Localize.Format("ui.lobby.chest.need_gem", s.GemCost.ToString("N0")));
            else global::Game.Module.Common.GameSound.Cue("run.shop");
        }

        /// <summary>
        /// 연 상자의 보상 목록. 보상은 이미 들어갔다 — 확인을 누르면 창만 닫힌다.
        /// 보상 창이 프리팹에 없으면(빌더 미실행) 알림창으로 대신한다.
        /// </summary>
        private void ShowChestReward(ChestReward reward)
        {
            if (_chestRewardPopup != null) { _chestRewardPopup.Show(reward); return; }

            var sb = new System.Text.StringBuilder();
            sb.Append(Localize.Format("ui.chest.reward.title", Localize.Get($"chest.{reward.ChestKey}.name")));
            sb.Append("\n\n").Append(Localize.Format("ui.chest.reward.gold", reward.Gold.ToString("N0")));
            for (int i = 0; i < reward.ShardHostKeys.Length; i++)
            {
                var host = _player?.GetHost(reward.ShardHostKeys[i]);
                string name = host != null ? host.DisplayName : reward.ShardHostKeys[i];
                string grade = host != null ? host.Grade.ToString() : "?";
                sb.Append('\n').Append(Localize.Format("ui.chest.reward.shard", name, grade, reward.ShardCounts[i]));
            }
            SystemPopup.Show(sb.ToString(), null, Localize.Get("ui.common.ok"), null);
        }

        // ── 하단 바 ──────────────────────────────────────────────

        private enum Tab { Play, Host }

        private static readonly string[] LobbyPageNodes = { "LobbyV4Gap", "LobbyV4Top", "LobbyV4Mid" };

        private void SelectTab(Tab tab)
        {
            bool host = tab == Tab.Host;
            SetNav("HostButton", host);
            SetNav("ChapterButton", !host);
            SetNav("ShopButton", false);
            // 위 화면만 바꾼다 — 육성 화면이 켜지면 로비 판은 끈다(가려져 안 보이는데 그리기만 한다)
            for (int i = 0; i < LobbyPageNodes.Length; i++)
            {
                var t = transform.Find(LobbyPageNodes[i]);
                if (t != null) t.gameObject.SetActive(!host);
            }
            if (_growthPanel == null) return;
            if (host) _growthPanel.Open();
            else _growthPanel.Close();
        }

        private void SetNav(string node, bool selected)
        {
            var view = _ui.Find(node)?.GetComponent<NavTabView>();
            if (view != null) view.SetSelected(selected);
        }

        // ── 게임 모드 ────────────────────────────────────────────

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
            _tokens.Add(bus.Subscribe<ChestChangedEvent>(_ => ApplyChests()));
            // 코드가 채우는 글자(상자 문구 · 플레이 자리)는 LocalizedText 가 없다 — 언어가 바뀌면 다시 채운다
            _tokens.Add(bus.Subscribe<LanguageChangedEvent>(_ => { _playLabelDirty = true; ApplyChests(); }));
            _tokens.Add(bus.Subscribe<HostSelectRequestedEvent>(OnHostSelectRequested));
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
            var p = mid.anchoredPosition;
            p.y = -(top.rect.height + Mathf.Max(0f, free - Mathf.Max(0f, slack)) * 0.5f);
            mid.anchoredPosition = p;
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
            RefreshProfile();
            ApplyChests();
        }

        /// <summary>경험치 채움의 가득 찬 폭 — `LobbyProfileBinder.FillWidth` 와 같아야 한다.</summary>
        private const float ProfileFillWidth = 116f * 226f / 250f;

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
