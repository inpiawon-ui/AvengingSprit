using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
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

        private UIBinder _ui;
        private BattleDirector _battle;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();

        private RectTransform _dpad;
        private RectTransform _knob;
        private Vector2 _knobHome;
        private Image _ultimateCooldown;
        private Image _possessButtonImage;
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
            SetPossessReady(false);
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
