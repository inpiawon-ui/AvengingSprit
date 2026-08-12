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
        // 게이지 채우기 폭. 레이아웃 JSON 의 `*HpBarBg` 가로와 같아야 한다 —
        // 어긋나면 HP 가 가득 차도 바가 덜 차거나 넘친다.
        // (_layout_ingame.py 의 목업 좌표 × 1.25)
        private const float GhostBarWidth = 170f;   // 136 × 1.25
        private const float HostBarWidth = 147.5f;  // 118 × 1.25
        private const float BossBarWidth = 272.5f;  // 218 × 1.25
        private const float ExpBarWidth = 170f;     // 136 × 1.25
        private const int BuffCardCount = 3;

        /// <summary>노브가 패드 폭의 몇 배까지 움직이는가. 이 거리에서 최대 속도다.</summary>
        private const float KnobTravelRatio = 0.30f;
        /// <summary>이 아래로 밀면 이동으로 치지 않는다 — 미세 흔들림에 사격이 끊기지 않게.</summary>
        private const float MoveDeadzone = 0.18f;
        private const float PadMargin = 12f;
        /// <summary>상단 HUD 높이 (레이아웃 184 × 1.25). 이 영역은 패드가 따라오지 않는다.</summary>
        private const float HudHeight = 230f;

        private UIBinder _ui;
        private BattleDirector _battle;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();

        private RectTransform _dpad;
        private RectTransform _knob;
        private Vector2 _knobHome;
        private Vector2 _dpadHome;

        /// <summary>
        /// 손가락을 처음 댄 지점(부모 로컬). **방향은 여기서부터 잰다.**
        ///
        /// 패드 그림은 화면 밖으로 나가지 않게 잘라내므로, 화면 가장자리를 누르면
        /// 패드 중심이 손가락과 어긋난다. 그 중심에서 방향을 재면 아래쪽(엄지 자리)을
        /// 눌렀을 때 위로 밀어도 아래로 읽힌다 — 손가락 자리에서 재야 맞는다.
        /// </summary>
        private Vector2 _padOrigin;
        private Image _ultimateCooldown;
        private Image _possessButtonImage;
        private Image _possessCooldown;
        private int _possessCost;
        private const float MaintainBarWidth = 147.5f;
        private BuffTable _buffTable;
        private string _bossName = "BOSS";
        private int _bossPhase = 1;
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
            _possessCooldown = _ui.Get<Image>("PossessCooldown");
            if (_possessCooldown != null)
            {
                // 얼티밋과 같은 방식 — 아래에서 차오르는 덮개. 숫자만으로는
                // 얼마나 남았는지 감이 안 온다.
                _possessCooldown.type = Image.Type.Filled;
                _possessCooldown.fillMethod = Image.FillMethod.Vertical;
                _possessCooldown.fillOrigin = (int)Image.OriginVertical.Top;
                _possessCooldown.color = new Color(0.05f, 0.06f, 0.12f, 0.72f);
                _possessCooldown.raycastTarget = false;
                _possessCooldown.fillAmount = 0f;
            }

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
            _tokens.Add(bus.Subscribe<ExitOpenedEvent>(OnExitOpened));
            _tokens.Add(bus.Subscribe<RunExpChangedEvent>(OnExpChanged));
            _tokens.Add(bus.Subscribe<EmergencyHostEvent>(OnEmergencyHost));
            _tokens.Add(bus.Subscribe<PossessTargetChangedEvent>(
                e => SetPossessState(e.HasTarget, e.GhostCost, e.Blocked)));
            _tokens.Add(bus.Subscribe<TacticalCooldownEvent>(OnTacticalCooldown));
            _tokens.Add(bus.Subscribe<MaintainChangedEvent>(OnMaintain));
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

        // ── 플로팅 가상 패드 ─────────────────────────────────────
        // 화면 아무 곳이나 누르면 그 자리로 패드가 따라오고, 손을 떼면 제자리로 돌아간다.
        // 한손 세로 조작에서 엄지가 닿는 위치는 매번 다르다 — 고정 패드는 손을 옮기게 만든다.

        private void HookDPad()
        {
            if (_dpad == null) return;

            // 패드가 하단 `ControlGroup` 안에 있으면 그 좁은 영역 밖으로 못 나간다.
            // 화면 어디로든 따라가야 하므로 루트로 올리고, 조작 버튼보다는 아래에 둔다.
            var control = _ui.Find("ControlGroup");
            int controlIndex = control != null ? control.GetSiblingIndex() : transform.childCount;
            _dpad.SetParent(transform, worldPositionStays: true);
            _dpad.SetSiblingIndex(controlIndex);
            _dpadHome = _dpad.anchoredPosition;

            BuildTouchCatcher();
        }

        /// <summary>
        /// 전체 화면 입력 판. 버튼·팝업보다 아래(먼저 그려지는 쪽)에 두어
        /// 다른 UI 가 먹지 않은 터치만 받는다.
        /// </summary>
        private void BuildTouchCatcher()
        {
            // 필드 바닥·HP 바 같은 장식 이미지가 터치를 먹으면 패드가 반응하지 않는다.
            // 버튼(Selectable)만 남기고 전부 레이캐스트를 끈다.
            foreach (var g in GetComponentsInChildren<Graphic>(true))
                if (g.GetComponent<Selectable>() == null) g.raycastTarget = false;

            var go = new GameObject("TouchCatcher", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetSiblingIndex(0);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, -HudHeight);   // 상단 HUD 는 제외한다

            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);        // 보이지 않지만 터치는 받는다
            img.raycastTarget = true;

            var trigger = go.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerDown, OnPadDown);
            AddTrigger(trigger, EventTriggerType.Drag, OnPadDrag);
            AddTrigger(trigger, EventTriggerType.PointerUp, _ => ReleasePad());

            // 3택1 패널은 딤으로 입력을 막아야 하므로 다시 켠다
            var dim = _ui.Get<Image>("BuffChoicePanel");
            if (dim != null) dim.raycastTarget = true;
        }

        private static void AddTrigger(EventTrigger t, EventTriggerType type,
                                       Action<PointerEventData> handler)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(d => handler((PointerEventData)d));
            t.triggers.Add(entry);
        }

        private void OnPadDown(PointerEventData e)
        {
            MovePadTo(e);
            // 누른 순간에는 아직 민 방향이 없다. 0 이어야 "멈춰야 쏜다"가 유지된다.
            if (_knob != null) _knob.anchoredPosition = _knobHome;
            if (_battle != null) _battle.MoveInput = Vector2.zero;
        }

        /// <summary>패드 중심이 터치 지점에 오도록 옮긴다. 화면 밖으로 나가지 않게 잘라낸다.</summary>
        private void MovePadTo(PointerEventData e)
        {
            var parent = _dpad.parent as RectTransform;
            if (parent == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, e.position, e.pressEventCamera, out var local)) return;

            // 방향의 기준점은 자르기 전의 **손가락 자리**다
            _padOrigin = local;

            var ps = parent.rect.size;
            var pp = parent.pivot;
            // 앵커 (0,1)(부모 좌상단)의 부모 로컬 좌표
            var anchor = new Vector2(-pp.x * ps.x, (1f - pp.y) * ps.y);

            var size = _dpad.rect.size;
            var pivot = _dpad.pivot;

            // ⚠️ 자르기는 **중심** 기준으로 한다. 예전에는 피벗 위치를 좌상단 기준
            //    범위에 밀어 넣어서, 피벗이 가운데인 패드는 반 칸씩 밀린 자리에 놓였다.
            var half = size * 0.5f;
            var c = local;
            c.x = Mathf.Clamp(c.x, -pp.x * ps.x + half.x + PadMargin,
                                   (1f - pp.x) * ps.x - half.x - PadMargin);
            c.y = Mathf.Clamp(c.y, -pp.y * ps.y + half.y + PadMargin,
                                   (1f - pp.y) * ps.y - half.y - PadMargin);

            // 패드 피벗이 어디든 중심이 c 에 오게 한다
            var pivotPos = c + new Vector2((pivot.x - 0.5f) * size.x,
                                           (pivot.y - 0.5f) * size.y);
            _dpad.anchoredPosition = pivotPos - anchor;
        }

        private void OnPadDrag(PointerEventData e)
        {
            var parent = _dpad.parent as RectTransform;
            if (parent == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, e.position, e.pressEventCamera, out var local)) return;

            // 손가락을 처음 댄 자리에서 잰다. 패드 그림은 잘려서 옮겨졌을 수 있으므로
            // 그림의 중심을 기준으로 삼으면 방향이 어긋난다.
            float radius = _dpad.rect.size.x * KnobTravelRatio;
            var dir = Vector2.ClampMagnitude((local - _padOrigin) / radius, 1f);
            if (_knob != null) _knob.anchoredPosition = _knobHome + dir * radius;

            // 데드존 — 미세한 흔들림으로 이동 판정이 서면 사격이 영영 재개되지 않는다
            if (_battle != null)
                _battle.MoveInput = dir.magnitude < MoveDeadzone ? Vector2.zero : dir;
        }

        private void ReleasePad()
        {
            if (_dpad != null) _dpad.anchoredPosition = _dpadHome;
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

            // 이름·페이즈는 바뀔 때만 온다. 매 피격 갱신에 덮어쓰지 않는다.
            if (!string.IsNullOrEmpty(e.BossName)) _bossName = e.BossName;
            if (e.Phase > 0) _bossPhase = e.Phase;
            if (!string.IsNullOrEmpty(e.BossName) || e.Phase > 0)
                // 도트 폰트는 고정폭이라 기존 고딕보다 훨씬 넓다. `PHASE 2` 를 그대로 붙이면
                // 이름과 합쳐 273px 칸을 넘어 화면 밖으로 잘린다. 페이즈는 `P2` 로 줄인다.
                _ui.SetText("BossLabel", _bossPhase > 1 ? $"{_bossName} P{_bossPhase}" : _bossName);
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
            // 얼티밋 버튼도 그 몸의 것으로 바꾼다. 21종이 같은 그림이면
            // 무엇을 들고 있는지가 화면에 안 보인다.
            var ult = _ui.Get<Image>("UltimateIcon");
            if (ult != null && _battle != null)
            {
                ult.sprite = _battle.UltimateIcon(e.PossessedHostKey);
                ult.enabled = ult.sprite != null;
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
            // 방 하나가 곧 스테이지 하나다. 마지막 스테이지가 보스방.
            // 방의 성격을 함께 보여준다 — 정예방에 들어선 걸 모르면 대비할 수 없다.
            string kind = e.Kind switch
            {
                RoomKind.Boss => "BOSS",
                RoomKind.Elite => "ELITE",
                RoomKind.Rest => "REST",
                _ => null,
            };
            // 보스방은 바로 옆에 보스 이름표가 뜬다. `BOSS ·` 를 덧붙이면 같은 말을
            // 두 번 하면서 칸만 넘친다(도트 폰트가 고정폭이라 여유가 없다).
            string stage = $"STAGE {e.RoomIndex + 1}/{e.RoomTotal}";
            _ui.SetText("StageText",
                        kind == null || e.Kind == RoomKind.Boss ? stage : $"{kind} · {stage}");
        }

        private void OnEmergencyHost(EmergencyHostEvent e)
            => _ui.SetText("StageText", $"긴급 빙의 — GHOST -{e.GhostCost}");

        private void OnExitOpened(ExitOpenedEvent e)
            => _ui.SetText("StageText", "출구가 열렸다 — 통과해서 다음 스테이지로");

        private void OnExpChanged(RunExpChangedEvent e)
        {
            _ui.SetText("LevelText", $"Lv.{e.Level}");
            _ui.SetFill("ExpBarFill", Ratio(e.Exp, e.ExpToNext), ExpBarWidth);
        }

        /// <summary>
        /// 빙의 버튼 상태. 세 가지를 구분해야 한다.
        ///   대상 없음   — 꺼짐(회색)
        ///   누를 수 있음 — 켜짐. 값이 있으면 값을 함께 보여준다
        ///   대상은 있는데 못 누름 — 켜지되 눌리지 않고, 이유(쿨다운·HP)를 보여준다
        /// 마지막을 그냥 회색으로 두면 "왜 안 되지"만 남는다.
        /// </summary>
        private void SetPossessState(bool hasTarget, int cost, bool blocked)
        {
            _possessCost = cost;
            bool usable = hasTarget && !blocked;

            var btn = _ui.Get<Button>("PossessButton");
            if (btn != null) btn.interactable = usable;
            if (_possessButtonImage != null)
                _possessButtonImage.color =
                    usable ? Color.white
                    : hasTarget ? new Color(0.85f, 0.55f, 0.55f, 1f)   // 대상은 있는데 값이 모자라다
                    : new Color(0.45f, 0.45f, 0.5f, 1f);

            // 전술 빙의(값이 붙는 교체)일 때만 값을 적는다. 유령 상태의 빙의는 공짜다.
            _ui.SetText("PossessCostText", hasTarget && cost > 0 ? $"-{cost}" : string.Empty);
        }

        private void SetPossessReady(bool ready) => SetPossessState(ready, 0, false);

        /// <summary>
        /// 유지 훅 표시. 무엇이 얼마나 쌓였는지가 보여야 교체할 때 무엇을 버리는지 안다.
        /// 몸이 없으면 감춘다 — 유령 상태에는 쌓을 것이 없다.
        /// </summary>
        private void OnMaintain(MaintainChangedEvent e)
        {
            bool has = !string.IsNullOrEmpty(e.HookName);
            _ui.SetActive("MaintainBarBg", has);
            if (!has) { _ui.SetText("MaintainText", string.Empty); return; }

            // 단계가 다 차면 게이지를 가득 채워 둔다 — 더 쌓을 게 없다는 표시다.
            _ui.SetFill("MaintainBarFill", e.Progress, MaintainBarWidth);
            _ui.SetText("MaintainText",
                        e.Stack > 0 ? $"{e.HookName}  ×{e.Stack}" : e.HookName);
        }

        /// <summary>
        /// 전술 빙의 쿨다운. 남은 초와 차오르는 덮개를 함께 보여준다.
        /// 숫자만으로는 "얼마나 남았나"가 손에 안 잡히고, 덮개만으로는 정확한 값을 모른다.
        /// </summary>
        private void OnTacticalCooldown(TacticalCooldownEvent e)
        {
            _ui.SetText("PossessCooldownText",
                        e.Remain > 0f ? Mathf.CeilToInt(e.Remain).ToString() : string.Empty);
            if (_possessCooldown != null)
                _possessCooldown.fillAmount = e.Total > 0f ? Mathf.Clamp01(e.Remain / e.Total) : 0f;
        }

        private void RefreshCurrency()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("GoldText", _player.Gold.ToString("N0"));
            _ui.SetText("GemText", _player.Gem.ToString("N0"));
        }

        private static float Ratio(int v, int max) => max > 0 ? Mathf.Clamp01((float)v / max) : 0f;

        // ── 레벨업 버프 3택1 ─────────────────────────────────────
        // ⚠ 이 창은 방을 비워서 열리는 게 아니라 **레벨이 올라서** 열린다(기획서 A 5-2).
        //    제목이 `ROOM CLEAR` 로 박혀 있어서, 방에 적이 남았는데 클리어가 떴다는
        //    오해를 샀다. 무엇 때문에 열렸는지는 화면이 말해야 한다.
        private void OnBuffOffer(BuffOfferEvent e)
        {
            if (_buffTable == null || e.OfferedKeys == null) return;

            _ui.SetText("BuffTitleText", "LEVEL UP");
            _ui.SetText("BuffSubText", $"Lv.{e.Level} · 하나를 선택하세요");

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
                await _player.GrantStageRewardAsync(e.RewardGold, e.RewardGhostExp, e.IsCleared,
                                                    e.RewardSpiritCore, e.RewardHostMemory,
                                                    e.RewardGem);

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
