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

        /// <summary>PLAY → 챕터 선택 → 호스트 선택 (기획 2026-09-18).</summary>
        [SerializeField] private ChapterSelectPanel _chapterSelectPanel;

        /// <summary>상자를 연 뒤 받은 것을 카드로 늘어놓는 창.</summary>
        [SerializeField] private ChestRewardPopup _chestRewardPopup;

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
            /// <summary>세는 중일 때 그림. 비면 <see cref="Sprite"/>. 완료 빛살이 박힌 그림(금 상자)은 세는 중에도 «다 됐다» 로 보인다.</summary>
            public Sprite Counting;
        }

        [SerializeField] private ChestArt[] _chestArts = System.Array.Empty<ChestArt>();

        private UIBinder _ui;
        private IPlayerDataService _player;
        private IChestService _chests;
        private readonly List<IDisposable> _tokens = new();

        // 1차 범위 밖 — 버튼은 두되 누르면 준비중 안내만 띄운다.
        // 시즌 패스 · 이벤트는 시안(lobby_hub_v2)에 있어 로비 v3 에서 다시 세웠다(2026-09-18) — 기능은 나중.
        // 설정 버튼은 시안에 없어 로비에서 빠졌다.
        private static readonly (string element, string label)[] NotReady =
        {
            ("MailButton", "우편"), ("SettingsButton", "설정"),
            ("SeasonPassButton", "시즌 패스"), ("EventButton", "이벤트"),
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
        // ⚠ 「눌린 칸」 연출(밝기 곱하기·크기 줄이기)을 다시 넣지 마라. 목업은 세 칸이
        //   같은 크기·같은 밝기이고, 지금 있는 곳은 PLAY 칸의 **금색 액자**로만 드러난다.
        //   흐리게 죽이면 글자·테두리가 안 보여 「프레임이 없다」로 읽힌다(2026-09-16).
        //   금색 액자는 `LobbyArtBinder` 가 고정으로 박는다.

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

            // 호스트 선택 판은 **로비가** 닫아 둔다. 판이 제 `Awake` 에서 스스로 끄면
            // 켜지는 도중이라 그 프레임에 안 먹어, 판 아래 팁 띠가 로비 바닥으로
            // 삐져나왔다(2026-09-16).
            if (_hostSelectPanel != null) _hostSelectPanel.Close();
            if (_chapterSelectPanel != null) _chapterSelectPanel.Close();
            if (_chestRewardPopup != null) _chestRewardPopup.Close();

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
            // 아래 「게임 모드」 는 프리팹의 LocalizedText 가 칠한다(언어가 바뀌면 따라온다)

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

        // 상자 버튼 글자는 상태마다 **칸이 다르다.**
        // 세는 중엔 「젬값 / 즉시 열기」 두 줄, 다 되면 「보상 획득하기」 한 줄이다.
        // ⚠ 한 칸을 위아래로 옮겨 돌려 쓰지 마라 — 한 줄일 때도 두 줄짜리 높이에 갇혀
        //   목업 절반 크기로 찍힌다(2026-09-16). 크기는 레이아웃 표가 정한다.
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

                var s = _chests.Get(i);
                bool has = !s.IsEmpty;
                bool ready = s.IsReady;

                SetIn(root, "ChestEmptyText", !has);
                SetIn(root, "ChestArt", has);
                SetIn(root, "ChestReadyBanner", ready);
                SetIn(root, "ChestReadyText", ready);
                SetIn(root, "ChestTimePlate", has && !ready);
                SetIn(root, "ChestTimeIcon", has && !ready);
                SetIn(root, "ChestTimeText", has && !ready);
                SetIn(root, "ChestActionButton", has);
                SetIn(root, "ChestActionGemIcon", has && !ready);
                SetIn(root, "ChestActionCostText", has && !ready);
                SetIn(root, "ChestActionLabelText", has && !ready);
                SetIn(root, "ChestReadyLabelText", ready);

                if (!has) continue;

                var art = _ui.Find(root, "ChestArt")?.GetComponent<UnityEngine.UI.Image>();
                if (art != null)
                {
                    var sprite = ChestSpriteOf(s.ChestKey, ready);
                    if (sprite != null && art.sprite != sprite)
                    {
                        art.sprite = sprite;
                        art.color = Color.white;   // 자리표시 색이 남아 있으면 그림이 물든다
                    }
                    // 그림이 없으면 네모가 뜬다 — 차라리 안 보이는 편이 낫다
                    art.enabled = sprite != null;
                }

                // 다 된 칸은 금색 버튼으로 바뀐다 — 어느 칸을 눌러야 하는지가 색으로 보인다
                var button = _ui.Find(root, "ChestActionButton")?.GetComponent<UnityEngine.UI.Image>();
                if (button != null)
                {
                    var want = ready ? _chestButtonGold : _chestButtonBlue;
                    if (want != null && button.sprite != want) button.sprite = want;
                }
                // 상태마다 상자 · 버튼 자리가 다르다(로비 v3) — 상자는 그림 제 크기로
                var layout = root.GetComponent<ChestSlotLayout>();
                if (layout != null)
                {
                    var artRt = art != null ? art.rectTransform : null;
                    layout.Apply(artRt, button != null ? button.rectTransform : null, ready);
                }
                TextIn(root, "ChestTimeText", Remain(s.RemainSeconds));
                if (!ready) CenterTimeRow(root);
                TextIn(root, "ChestActionCostText", s.GemCost.ToString("N0"));
                if (!ready) KeepClearOfIcon(root, "ChestActionGemIcon", "ChestActionCostText", GemClearGap);
                TextIn(root, "ChestActionLabelText", Localize.Get("ui.lobby.chest.open_now"));
                TextIn(root, "ChestReadyLabelText", Localize.Get("ui.lobby.chest.claim"));
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

        /// <summary>시계와 남은 시간 사이 간격(캔버스 px).</summary>
        private const float TimeRowGap = 9.95f;   // 시안: 시계 그림 끝 126 → 글자 139 (시안 px) × 720/941
        /// <summary>
        /// 젬 값이 젬을 덮는지 볼 때의 간격. 시안 「1,000」 은 젬에 거의 붙어 있어(간격 6.89) 그 값으로 재면
        /// 글자 칸의 좌우 여백(bearing) 때문에 시안 그대로의 자리도 «덮는다» 로 잡혀 밀렸다 — 여유를 둔다.
        /// </summary>
        private const float GemClearGap = 3.5f;
        /// <summary>「플레이하기 ▶」 — 글자 끝 → ▶ 간격, 덩어리 가운데(버튼 왼쪽 기준). 시안: 글자 390~535 · ▶ 554~575, 버튼 305~.</summary>
        private const float PlayArrowGap = 12.7f;   // 그림 여백(알파 번짐) 만큼 실측으로 줄였다
        private const float PlayGroupCenter = 135.8f;
        /// <summary>시계 + 시간 덩어리는 시안에서 판 가운데보다 2px(시안) 왼쪽에 있다.</summary>
        private const float TimeRowShift = -1.53f;

        /// <summary>
        /// 시계 + 남은 시간을 **한 덩어리로** 시간 판 가운데에 둔다(2026-09-18 지적).
        ///
        /// 글자 길이가 「1分」 부터 「3時間12分」 까지 달라서 자리를 고정하면
        /// 짧을 때 덩어리가 왼쪽으로 쏠린다. 글자 폭을 재서 매번 맞춘다.
        /// </summary>
        private void CenterTimeRow(Transform slot)
            => CenterRow(slot, "ChestTimePlate", "ChestTimeIcon", "ChestTimeText", TimeRowGap, TimeRowShift);

        /// <summary>
        /// 글자 + 아이콘(글자 뒤) 한 덩어리를 부모 안 정해진 가운데에 둔다.
        /// ▶ 는 시안 글꼴의 좁은 삼각형이라 그림으로 붙인다 — 언어마다 글자 길이가 달라도 글자 끝을 따라간다.
        /// </summary>
        private void PlaceTextThenIcon(string parentName, string textName, string iconName, float gap, float centerX)
        {
            var parent = _ui.Find(parentName);
            if (parent == null) return;
            var text = _ui.Find(parent, textName)?.GetComponent<TMPro.TextMeshProUGUI>();
            var icon = _ui.Find(parent, iconName) as RectTransform;
            if (text == null || icon == null) return;

            var rt = text.rectTransform;
            text.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Left;
            text.ForceMeshUpdate();
            float sx = rt.localScale.x;
            float textW = text.textBounds.size.x * sx;
            float left = centerX - (textW + gap + icon.rect.width) * 0.5f;
            // 버튼 안 자식은 왼쪽 위 기준(anchor·pivot 0,1)이라 anchoredPosition 이 곧 부모 왼쪽에서의 거리다
            var p = rt.anchoredPosition;
            p.x = left - text.textBounds.min.x * sx;
            rt.anchoredPosition = p;
            var ip = icon.anchoredPosition;
            ip.x = left + textW + gap;
            icon.anchoredPosition = ip;
        }

        /// <summary>
        /// 젬 값 — 시안은 **젬은 제자리, 숫자만 제 칸 가운데**다(「1,000」 · 「800」 가운데가 같다).
        /// 칸은 빌더가 잡아 두고, 숫자가 길어 젬을 덮을 때만 오른쪽으로 민다.
        /// </summary>
        private void KeepClearOfIcon(Transform slot, string iconName, string textName, float gap)
        {
            var icon = _ui.Find(slot, iconName) as RectTransform;
            var text = _ui.Find(slot, textName)?.GetComponent<TMPro.TextMeshProUGUI>();
            if (icon == null || text == null) return;

            text.margin = Vector4.zero;
            text.ForceMeshUpdate();
            var rt = text.rectTransform;
            float sx = rt.localScale.x;
            float inkLeft = rt.localPosition.x + text.textBounds.min.x * sx;
            float iconRight = icon.localPosition.x + (1f - icon.pivot.x) * icon.rect.width;
            float need = iconRight + gap - inkLeft;
            // 가운데 정렬 글자는 왼쪽 여백의 절반만큼 움직인다
            if (need > 0f) text.margin = new Vector4(2f * need / sx, 0f, 0f, 0f);
        }

        /// <summary>
        /// 아이콘 + 글자 한 줄을 판 가운데에 둔다. 시안 간격: 시계 → 시간 9.95 (캔버스).
        /// ⚠ 가로 정렬만 바꾼다 — 세로는 글자 모양 기준(Geometry)이어야 시안 줄에 앉는다.
        /// </summary>
        private void CenterRow(Transform slot, string plateName, string iconName, string textName, float gap, float shift = 0f)
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
            float textW = Mathf.Min(text.textBounds.size.x, textRect.rect.width) * sx;
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

        private Sprite ChestSpriteOf(string chestKey, bool ready)
        {
            if (_chestArts == null || string.IsNullOrEmpty(chestKey)) return null;
            for (int i = 0; i < _chestArts.Length; i++)
            {
                if (_chestArts[i].Key != chestKey) continue;
                var a = _chestArts[i];
                return !ready && a.Counting != null ? a.Counting : a.Sprite;
            }
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
                if (_chests.TryClaim(slot, out var reward))
                {
                    global::Game.Module.Common.GameSound.Cue("run.gold");
                    ShowChestReward(reward);
                }
                return;
            }

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

        // ── 게임 모드 ────────────────────────────────────────────

        /// <summary>
        /// 옆 카드 · 화살표 — 로비 v3 는 카드가 시안 자리에 **고정**이다(카드 그림이 바탕에 있다).
        /// 옆 모드는 잠겨 있으므로 돌리지 않고 「준비 중」을 알린다.
        /// </summary>
        private void RotateMode(int step)
        {
            var side = Modes[(ScenarioIndex + step + Modes.Length) % Modes.Length];
            NotifyNotReady(side.Name);
        }

        private void PlaySelectedMode()
        {
            var mode = Modes[_modeIndex];
            if (!mode.Unlocked) { NotifyNotReady(mode.Name); return; }
            global::Game.Module.Common.GameSound.Cue("ui.play");   // 원작 코인 투입음
            // 챕터를 먼저 고른다. 판이 없으면(프리팹 미반영) 예전처럼 바로 호스트 선택으로.
            if (_chapterSelectPanel != null) _chapterSelectPanel.Open();
            else OpenHostSelect(true);
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
            // 시안대로 모드 소개 글 — 진행도는 챕터 선택 화면이 보여 준다
            _ui.SetText("ModeCenterSubText", mid.Desc);
            _ui.SetText("ModePlayButtonText", Localize.Get("ui.lobby.play"));
            PlaceTextThenIcon("ModePlayButton", "ModePlayButtonText", "ModePlayButtonArrow", PlayArrowGap, PlayGroupCenter);
            // ⚠ 잠겼다고 그림을 끄지 마라. 가운데 칸이 통째로 시커먼 판이 되어 고장 난 것처럼
            //   보였다(2026-09-16). 목업도 잠긴 칸에 그림을 두고 자물쇠만 얹는다.
            _ui.SetActive("ModeCenterArt", true);
            _ui.SetActive("ModeCenterLockIcon", !mid.Unlocked);
            _ui.SetActive("ModePlayButton", mid.Unlocked);
            // MAIN 딱지는 **주 콘텐츠에만** 붙는다. 아무 칸에나 붙으면 표시가 아니라 장식이 된다.
            _ui.SetActive("ModeMainBadge", _modeIndex == ScenarioIndex);
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
            img.color = Color.white;
            // 그림이 없으면 색 네모가 남는다 — 안 그리는 편이 낫다
            img.enabled = sprite != null;
        }

        // ── 하단 바 ──────────────────────────────────────────────

        private void SelectTab(string tab)
        {
            // 갈 곳이 있는 것만 실제로 간다. 나머지는 아직 없다고 알린다.
            if (tab == "HostButton") OpenHostSelect(false);
            else if (tab == "ShopButton") NotifyNotReady("상점");
        }

        private void OnEnable()
        {
            var bus = CoreModule.Get<IEventBus>();
            _tokens.Add(bus.Subscribe<UserDataReadyEvent>(_ => Refresh()));
            _tokens.Add(bus.Subscribe<CurrencyChangedEvent>(_ => RefreshCurrency()));
            _tokens.Add(bus.Subscribe<ProgressChangedEvent>(_ => RefreshChapter()));
            _tokens.Add(bus.Subscribe<ChestChangedEvent>(_ => ApplyChests()));
            // 코드가 채우는 글자(모드 이름 · 플레이 · 상자 문구)는 LocalizedText 가 없다 — 언어가 바뀌면 다시 채운다
            _tokens.Add(bus.Subscribe<LanguageChangedEvent>(_ => { Refresh(); ApplyChests(); }));
            _tokens.Add(bus.Subscribe<HostSelectRequestedEvent>(OnHostSelectRequested));
            _tokens.Add(bus.Subscribe<ChapterSelectRequestedEvent>(_ =>
            {
                if (_chapterSelectPanel != null) _chapterSelectPanel.Open();
            }));
            Refresh();
            UpdateGapFill();
        }

        // 긴 화면에서 위판 · 아래판 사이에 뜨는 틈은 골목 바닥 연장 그림이 메운다.
        // 연장 그림은 위판과 몇 줄 겹쳐 알파로 녹인다 — 그런데 틈이 거의 없는 9:16 에서 겹치면
        // 시안 위판 끝줄이 바뀐다. 틈이 작으면 겹치지 않는 자리(위판 바로 아래)로 내린다.
        // ⚠ 끄면 안 된다: 시안(941×1672)은 정확히 9:16 이 아니라 9:16 에서도 0.7 px 가 비어 뒤가 비친다.
        private float _gapOverlapY = float.NaN;

        private void OnRectTransformDimensionsChange() => UpdateGapFill();

        private void UpdateGapFill()
        {
            var gap = transform.Find("LobbyV3Gap") as RectTransform;
            var top = transform.Find("LobbyV3Top") as RectTransform;
            var bottom = transform.Find("LobbyV3Bottom") as RectTransform;
            if (gap == null || top == null || bottom == null) return;
            if (float.IsNaN(_gapOverlapY)) _gapOverlapY = gap.anchoredPosition.y;   // 빌더가 둔 겹친 자리
            float free = ((RectTransform)transform).rect.height - top.rect.height - bottom.rect.height;
            var p = gap.anchoredPosition;
            p.y = free > 2f ? _gapOverlapY : -top.rect.height;
            gap.anchoredPosition = p;
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
            // 버튼 글자는 시안대로 늘 「플레이하기 ▶」다 — 이어 할 챕터는 챕터 선택이 고른다(로비 v3)
            ApplyModes();
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












