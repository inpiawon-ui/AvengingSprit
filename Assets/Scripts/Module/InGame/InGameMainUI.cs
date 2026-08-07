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
using GameFramework.Core.Module.Resource;
using GameFramework.Core.Module.Scene;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 인게임 HUD 와 조작. 전투 상태는 `BattleDirector` 가 들고, 여기서는 표시와 입력만 다룬다.
    ///
    /// D-패드는 목업(`ingame_hd_scene`)의 사각 패드를 따른다 — 미결 항목 #8 결정.
    /// 패드 영역을 드래그하면 중심 대비 방향이 이동 입력이 된다.
    /// </summary>
    public sealed class InGameMainUI : MonoBehaviour, IBackTarget
    {
        private const float GhostBarWidth = 114f;   // 목업 실측 (레이아웃 JSON 과 동일)
        private const float HostBarWidth = 98f;
        private const float BossBarWidth = 260f;
        private const float KnobRadius = 34f;
        private const int BuffCardCount = 3;

        private UIBinder _ui;
        private BattleDirector _battle;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();

        private RectTransform _dpad;
        private RectTransform _knob;
        private Vector2 _knobHome;
        private Image _ultimateCooldown;
        private Image _possessButtonImage;
        private BuffTable _buffTable;
        private bool _finished;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            CoreModule.TryGet(out _player);
            gameObject.AddComponent<BackButtonRouter>();

            _dpad = _ui.Find("DPadBase") as RectTransform;
            _knob = _ui.Find("DPadKnob") as RectTransform;
            if (_knob != null) _knobHome = _knob.anchoredPosition;
            _ultimateCooldown = _ui.Get<Image>("UltimateCooldown");
            if (_ultimateCooldown != null)
            {
                // 아래에서 차오르는 쿨다운 덮개. 스프라이트 배선만으로는 채우기 모드를 줄 수 없다.
                _ultimateCooldown.type = Image.Type.Filled;
                _ultimateCooldown.fillMethod = Image.FillMethod.Vertical;
                _ultimateCooldown.fillOrigin = (int)Image.OriginVertical.Top;
                _ultimateCooldown.raycastTarget = false;
            }
            _possessButtonImage = _ui.Get<Image>("PossessButton");

            HookDPad();
            _ui.OnClick("PossessButton", () => _battle?.TryPossess());
            _ui.OnClick("UltimateButton", () => _battle?.TryUltimate());
            _ui.OnClick("PauseButton", OnPause);

            _ui.SetActive("BossGroup", false);
            _ui.SetActive("BuffChoicePanel", false);
            SetPossessReady(false);

            // 딤 — 뒤 화면을 덮고 조작 입력을 막는다
            var dim = _ui.Get<Image>("BuffChoicePanel");
            if (dim != null)
            {
                dim.color = new Color(0.02f, 0.03f, 0.06f, 0.86f);
                dim.raycastTarget = true;
            }
        }

        private void OnEnable()
        {
            var bus = CoreModule.Get<IEventBus>();
            _tokens.Add(bus.Subscribe<CombatHpChangedEvent>(OnHp));
            _tokens.Add(bus.Subscribe<BossHpChangedEvent>(OnBossHp));
            _tokens.Add(bus.Subscribe<PossessedEvent>(OnPossessed));
            _tokens.Add(bus.Subscribe<HostLostEvent>(_ => OnHostLost()));
            _tokens.Add(bus.Subscribe<RoomEnteredEvent>(OnRoomEntered));
            _tokens.Add(bus.Subscribe<PossessTargetChangedEvent>(e => SetPossessReady(e.HasTarget)));
            _tokens.Add(bus.Subscribe<StageFinishedEvent>(OnStageFinished));
            _tokens.Add(bus.Subscribe<BuffOfferEvent>(OnBuffOffer));
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        private void Start() => BootAsync().Forget();   // fire-and-forget: 씬 진입 후 전투 준비

        private async UniTaskVoid BootAsync()
        {
            var field = _ui.Find("RoomField") as RectTransform;
            var layer = _ui.Find("UnitLayer") as RectTransform;
            if (field == null || layer == null)
            {
                Debug.LogError("[InGame] RoomField / UnitLayer 가 없습니다.");
                return;
            }
            _battle = gameObject.AddComponent<BattleDirector>();
            await _battle.BootAsync(field, layer);

            // 카드 문구는 테이블에서 읽는다 — 버프 정의를 UI 에 복제하지 않기 위함
            try { _buffTable = await CoreModule.Get<IResourceManager>().LoadAsync<BuffTable>("TableData/BuffTable"); }
            catch (Exception e) { Debug.LogError($"[InGame] BuffTable 로드 실패 — {e.Message}"); }

            RefreshCurrency();
        }

        private void Update()
        {
            if (_ultimateCooldown != null && _battle != null)
                _ultimateCooldown.fillAmount = 1f - _battle.UltimateRatio;
        }

        // ── D-패드 ───────────────────────────────────────────────
        private void HookDPad()
        {
            if (_dpad == null) return;
            var img = _dpad.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            var trigger = _dpad.gameObject.GetComponent<EventTrigger>()
                          ?? _dpad.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();
            AddTrigger(trigger, EventTriggerType.PointerDown, OnPadDrag);
            AddTrigger(trigger, EventTriggerType.Drag, OnPadDrag);
            AddTrigger(trigger, EventTriggerType.PointerUp, _ => ReleasePad());
        }

        private static void AddTrigger(EventTrigger t, EventTriggerType type,
                                       Action<PointerEventData> handler)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(d => handler((PointerEventData)d));
            t.triggers.Add(entry);
        }

        private void OnPadDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _dpad, e.position, e.pressEventCamera, out var local)) return;

            var dir = Vector2.ClampMagnitude(local / KnobRadius, 1f);
            if (_knob != null) _knob.anchoredPosition = _knobHome + dir * KnobRadius;
            if (_battle != null) _battle.MoveInput = dir;
        }

        private void ReleasePad()
        {
            if (_knob != null) _knob.anchoredPosition = _knobHome;
            if (_battle != null) _battle.MoveInput = Vector2.zero;
        }

        // ── 표시 ─────────────────────────────────────────────────
        private void OnHp(CombatHpChangedEvent e)
        {
            _ui.SetText("GhostHpText", $"{e.GhostHp}/{e.GhostHpMax}");
            _ui.SetFill("GhostHpBarFill", Ratio(e.GhostHp, e.GhostHpMax), GhostBarWidth);

            _ui.SetActive("HostHpBarBg", e.HasHost);
            _ui.SetActive("HostHpText", e.HasHost);
            if (!e.HasHost) return;
            _ui.SetText("HostHpText", $"{e.HostHp}/{e.HostHpMax}");
            _ui.SetFill("HostHpBarFill", Ratio(e.HostHp, e.HostHpMax), HostBarWidth);
        }

        private void OnBossHp(BossHpChangedEvent e)
        {
            bool show = e.BossHpMax > 0;
            _ui.SetActive("BossGroup", show);
            if (!show) return;
            _ui.SetText("BossHpText", $"{e.BossHp}/{e.BossHpMax}");
            _ui.SetFill("BossHpBarFill", Ratio(e.BossHp, e.BossHpMax), BossBarWidth);
        }

        private void OnPossessed(PossessedEvent e)
        {
            _ui.SetText("HostLabel", "HOST");
            _ui.SetText("HostNameText", e.DisplayName);
            _ui.SetActive("HostPortraitFrame", true);

            // 프레임 크롭에 목업의 얼굴이 함께 들어가 있다. 실제 호스트 초상을 위에 덮는다.
            var portrait = _ui.Get<Image>("HostPortraitImage");
            if (portrait != null && _battle != null)
            {
                portrait.sprite = _battle.UnitSprite(e.PossessedHostKey);
                portrait.preserveAspect = true;
                portrait.gameObject.SetActive(portrait.sprite != null);
            }
            SetPossessReady(false);
        }

        private void OnHostLost()
        {
            _ui.SetText("HostNameText", "— 유령 상태 —");
            _ui.SetActive("HostPortraitImage", false);
        }

        private void OnRoomEntered(RoomEnteredEvent e)
        {
            _ui.SetText("StageText",
                e.IsBossRoom ? "BOSS ROOM" : $"ROOM {e.RoomIndex + 1} / {e.RoomTotal}");
        }

        /// <summary>빙의 가능할 때만 버튼을 밝힌다. 목업의 발광 상태를 알파로 흉내낸다.</summary>
        private void SetPossessReady(bool ready)
        {
            var btn = _ui.Get<Button>("PossessButton");
            if (btn != null) btn.interactable = ready;
            if (_possessButtonImage != null)
                _possessButtonImage.color = ready ? Color.white : new Color(0.45f, 0.45f, 0.5f, 1f);
        }

        private void RefreshCurrency()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("GoldText", _player.Gold.ToString("N0"));
            _ui.SetText("GemText", _player.Gem.ToString("N0"));
        }

        private static float Ratio(int v, int max) => max > 0 ? Mathf.Clamp01((float)v / max) : 0f;

        // ── 룸 클리어 버프 3택1 ──────────────────────────────────
        private void OnBuffOffer(BuffOfferEvent e)
        {
            if (_buffTable == null || e.OfferedKeys == null) return;

            for (int i = 0; i < BuffCardCount; i++)
            {
                bool has = i < e.OfferedKeys.Length;
                _ui.SetActive($"BuffCard{i}", has);
                _ui.SetActive($"BuffCard{i}Name", has);
                _ui.SetActive($"BuffCard{i}Desc", has);
                if (!has) continue;

                var entry = _buffTable.Get(e.OfferedKeys[i]);
                if (entry == null) continue;

                _ui.SetText($"BuffCard{i}Name", entry.NameKr);
                _ui.SetText($"BuffCard{i}Desc", entry.Description);

                // 카드 판때기. 호스트 슬롯 프레임을 붙여봤지만 속이 비어 있어 딤 위에서
                // 보이지 않았다. 단색 패널이 읽기 쉽다 (Image 는 sprite 가 null 이면 단색을 그린다).
                var card = _ui.Get<Image>($"BuffCard{i}");
                if (card != null) card.color = new Color(0.078f, 0.102f, 0.157f, 0.98f);

                // 강조색 막대도 같은 이유로 스프라이트가 필요 없다
                var accent = _ui.Get<Image>($"BuffCard{i}Accent");
                if (accent != null && ColorUtility.TryParseHtmlString(entry.ColorHex, out var c))
                    accent.color = c;

                // 매번 다른 버프가 오므로 이전 리스너를 지우고 새로 건다
                var btn = _ui.Get<Button>($"BuffCard{i}");
                if (btn == null) continue;
                var key = entry.BuffKey;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnBuffPicked(key));
            }
            _ui.SetActive("BuffChoicePanel", true);
            _ui.Find("BuffChoicePanel")?.SetAsLastSibling();
        }

        private void OnBuffPicked(string buffKey)
        {
            _ui.SetActive("BuffChoicePanel", false);
            _battle?.ChooseBuff(buffKey);
        }

        // ── 종료 ─────────────────────────────────────────────────
        private void OnStageFinished(StageFinishedEvent e)
        {
            if (_finished) return;
            _finished = true;
            SystemPopup.Show(
                e.IsCleared
                    ? $"스테이지 클리어!\n골드 +{e.RewardGold}   고스트 EXP +{e.RewardGhostExp}"
                    : "유령이 소멸했습니다.\n로비로 돌아갑니다.",
                onConfirm: () => GoLobbyAsync(e).Forget(),   // fire-and-forget: 씬 전환 대기 불필요
                confirmText: "로비로",
                cancelText: null);
        }

        private void OnPause()
        {
            SystemPopup.Show("스테이지를 포기하고 로비로 돌아갈까요?",
                onConfirm: () => GoLobbyAsync(default).Forget(),  // fire-and-forget: 씬 전환 대기 불필요
                confirmText: "포기", cancelText: "계속");
        }

        private async UniTaskVoid GoLobbyAsync(StageFinishedEvent e)
        {
            if (_player != null && _player.IsReady && e.RewardGold > 0)
                await _player.GrantStageRewardAsync(e.RewardGold, e.RewardGhostExp, e.IsCleared);

            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.Lobby,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }

        /// <summary>인게임 뒤로가기 = 일시정지(포기 확인). 06_ui.md 3순위에 해당한다.</summary>
        public bool OnBackPressed()
        {
            OnPause();
            return true;
        }
    }
}
