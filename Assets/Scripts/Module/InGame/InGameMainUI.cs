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
using UnityEngine.U2D;
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
        private const float GhostBarWidth = 148f;   // 프리팹 GhostHpBarBg 폭
        private const float HostBarWidth = 118f;    // 프리팹 HostHpBarBg 폭
        private const float BossBarWidth = 640f;    // 프리팹 BossHpBarBg 폭
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
        private Image _skillCooldownFill;
        private Image _possessButtonImage;
        private Image _possessCooldown;
        private int _possessCost;
        private BuffTable _buffTable;
        private string _bossName = "BOSS";
        private int _bossPhase = 1;
        private bool _finished;

        // ── 진입 가림막 ──────────────────────────────────────────
        //
        // 씬이 바뀌는 것과 전투 준비가 끝나는 것은 다른 순간이다. 아틀라스·테이블을
        // 받아 오는 동안 방은 텅 비어 있어서, 그 사이가 흰 화면으로 보였다.
        // 준비가 끝날 때까지 덮어 두고 걷어 낸다.

        private const float CoverFadeSeconds = 0.22f;
        private Image _cover;

        private void MakeCover()
        {
            var go = new GameObject("BootCover", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();          // 무엇보다 위에 덮는다

            _cover = go.AddComponent<Image>();
            _cover.color = new Color(0.02f, 0.03f, 0.06f, 1f);
            _cover.raycastTarget = true;    // 준비 전 조작을 먹지 않게 막는다
        }

        private async UniTask RemoveCoverAsync()
        {
            if (_cover == null) return;
            float t = CoverFadeSeconds;
            while (t > 0f)
            {
                t -= Time.deltaTime;
                var c = _cover.color;
                _cover.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(t / CoverFadeSeconds));
                await UniTask.Yield();
            }
            Destroy(_cover.gameObject);
            _cover = null;
        }

        private void Awake()
        {
            _ui = new UIBinder(transform);
            MakeCover();
            CoreModule.TryGet(out _player);
            gameObject.AddComponent<BackButtonRouter>();

            _dpad = _ui.Find("DPadBase") as RectTransform;
            _knob = _ui.Find("DPadKnob") as RectTransform;
            if (_knob != null) _knobHome = _knob.anchoredPosition;
            _skillCooldownFill = _ui.Get<Image>("SkillCooldown");
            if (_skillCooldownFill != null)
            {
                // 아래에서 차오르는 쿨다운 덮개. 스프라이트 배선만으로는 채우기 모드를 줄 수 없다.
                _skillCooldownFill.type = Image.Type.Filled;
                _skillCooldownFill.fillMethod = Image.FillMethod.Vertical;
                _skillCooldownFill.fillOrigin = (int)Image.OriginVertical.Top;
                _skillCooldownFill.raycastTarget = false;
            }
            _possessButtonImage = _ui.Get<Image>("PossessButton");
            _possessCooldown = _ui.Get<Image>("PossessCooldown");
            if (_possessCooldown != null)
            {
                // 액티브 스킬과 같은 방식 — 아래에서 차오르는 덮개. 숫자만으로는
                // 얼마나 남았는지 감이 안 온다.
                _possessCooldown.type = Image.Type.Filled;
                _possessCooldown.fillMethod = Image.FillMethod.Vertical;
                _possessCooldown.fillOrigin = (int)Image.OriginVertical.Top;
                _possessCooldown.color = new Color(0.05f, 0.06f, 0.12f, 0.72f);
                _possessCooldown.raycastTarget = false;
                _possessCooldown.fillAmount = 0f;
            }

            HookDPad();
            CacheGoldFx();
            _ui.OnClick("PossessButton", () => _battle?.TryPossess());
            _ui.OnClick("SkillButton", () => _battle?.TryActiveSkill());
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

            var eventDim = _ui.Get<Image>("EventPanel");
            if (eventDim != null)
            {
                eventDim.color = new Color(0.02f, 0.03f, 0.06f, 0.86f);
                eventDim.raycastTarget = true;
            }
            var box = _ui.Get<Image>("EventBox");
            if (box != null) box.color = new Color(0.078f, 0.102f, 0.157f, 0.98f);
            var accept = _ui.Get<Image>("EventAcceptButton");
            if (accept != null) accept.color = new Color(0.941f, 0.706f, 0.157f, 1f);
            var decline = _ui.Get<Image>("EventDeclineButton");
            if (decline != null) decline.color = new Color(0.16f, 0.19f, 0.26f, 1f);
            _ui.SetActive("EventPanel", false);

            // 제단 창도 같은 딤을 쓴다. `EventPanel` 을 복제해 만든 창이라 뿌리 이미지가
            // **불투명 흰색**으로 남아 있었다 — 화면 전체가 하얗게 덮여 방도 HUD 도 안 보였다.
            var shrineDim = _ui.Get<Image>("ShrinePanel");
            if (shrineDim != null)
            {
                shrineDim.color = new Color(0.02f, 0.03f, 0.06f, 0.86f);
                shrineDim.raycastTarget = true;
            }
            var shrineBox = _ui.Get<Image>("ShrineBox");
            if (shrineBox != null) shrineBox.color = new Color(0.078f, 0.102f, 0.157f, 0.98f);
            _ui.SetActive("ShrinePanel", false);

            var shopDim = _ui.Get<Image>("ShopPanel");
            if (shopDim != null)
            {
                shopDim.color = new Color(0.02f, 0.03f, 0.06f, 0.86f);
                shopDim.raycastTarget = true;
            }

            // 그림은 아틀라스가 온 뒤에 입힌다(`SkinPopups`). 여기서는 단색만 깔아 둔다 —
            // 아틀라스 로드가 실패해도 창이 보이기는 해야 한다.
            var shopBox = _ui.Get<Image>("ShopBox");
            if (shopBox != null) shopBox.color = new Color(0.078f, 0.102f, 0.157f, 0.98f);
            var leave = _ui.Get<Image>("ShopLeaveButton");
            if (leave != null) leave.color = new Color(0.941f, 0.706f, 0.157f, 1f);
            _ui.SetActive("ShopPanel", false);
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
            _tokens.Add(bus.Subscribe<RepossessLockEvent>(OnRepossessLock));
            _tokens.Add(bus.Subscribe<StageFinishedEvent>(OnStageFinished));
            _tokens.Add(bus.Subscribe<BuffOfferEvent>(OnBuffOffer));
            _tokens.Add(bus.Subscribe<EventOfferEvent>(OnEventOffer));
            _tokens.Add(bus.Subscribe<ShrineOpenedEvent>(OnShrineOpened));
            _tokens.Add(bus.Subscribe<ShrineResolvedEvent>(OnShrineResolved));
            _tokens.Add(bus.Subscribe<EventResolvedEvent>(OnEventResolved));
            _tokens.Add(bus.Subscribe<ShopOpenedEvent>(OnShopOpened));
            _tokens.Add(bus.Subscribe<ShopPurchasedEvent>(
                e => _ui.SetText("ShopResultText", e.ResultLine)));
            _tokens.Add(bus.Subscribe<RunGoldChangedEvent>(OnRunGoldChanged));
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
                await RemoveCoverAsync();   // 실패해도 가림막은 걷는다 — 남으면 화면이 잠긴다
                return;
            }
            _battle = gameObject.AddComponent<BattleDirector>();
            SetStaticLabels();
            // 판은 유령으로 시작한다 — HOST 칸과 스킬 버튼을 먼저 비워 둔다.
            ShowNoHost();
            await _battle.BootAsync(field, layer);

            // 카드 문구는 테이블에서 읽는다 — 버프 정의를 UI 에 복제하지 않기 위함
            try { _cardAtlas = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>("atlas/card"); }
            catch (Exception e) { Debug.LogWarning($"[InGameUI] 카드 아틀라스 로드 실패 — {e.Message}"); }

            // 창 부품(상점·악마·제단 액자·칸·버튼)과 특성 분류 아이콘이 여기 있다.
            try { _uiAtlas = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>("atlas/ingamemainui"); }
            catch (Exception e) { Debug.LogWarning($"[InGameUI] 화면 아틀라스 로드 실패 — {e.Message}"); }

            // 아틀라스가 온 **뒤에** 껍데기를 입힌다. 먼저 부르면 그림이 아직 없어
            // 단색으로 남는다 — 화면이 한 번 초라했다가 안 바뀐다.
            SkinPopups();

            try { _buffTable = await CoreModule.Get<IResourceManager>().LoadAsync<BuffTable>("TableData/BuffTable"); }
            catch (Exception e) { Debug.LogError($"[InGame] BuffTable 로드 실패 — {e.Message}"); }

            RefreshCurrency();
            await RemoveCoverAsync();   // 여기까지 와야 방에 그림이 다 올라와 있다
        }

        private void Update()
        {
            if (_skillCooldownFill != null && _battle != null)
                _skillCooldownFill.fillAmount = 1f - _battle.SkillCooldownRatio;

            TickGoldFx(Time.deltaTime);
        }

        // ── 골드 획득 연출 ───────────────────────────────────
        //
        // 골드가 들어와도 HUD 숫자가 조용히 바뀔 뿐이라, 방을 비운 보상인지
        // 이벤트에서 받은 것인지가 화면에서 구분되지 않았다. 얻은 **자리에서**
        // 동전이 튀어 HUD 로 빨려 들어가고, 도착할 때마다 숫자가 한 칸씩 오른다.
        //
        // 그림은 HUD 골드 아이콘을 그대로 빌려 쓴다 — 새 리소스가 필요 없다.

        private const int   MaxCoins       = 24;     // 폭주 방지 상한
        private const float CoinSize       = 22f;
        private const float CoinScatter    = 46f;    // 튀는 반경
        private const float CoinStagger    = 0.045f; // 한 닢씩 어긋나는 간격
        private const float GoldPopSeconds = 0.18f;

        private RectTransform _coinLayer;
        private readonly List<GoldCoin> _coins = new();
        private RectTransform _goldIcon;
        private RectTransform _goldTextRect;
        private TMPro.TextMeshProUGUI _goldTmp;
        private Color _goldTextBase = Color.white;
        private Sprite _coinSprite;

        private int _goldShown;        // 화면에 적혀 있는 값
        private int _goldTarget;       // 실제 판 골드
        private int _coinsInFlight;
        private float _goldPop;

        private void CacheGoldFx()
        {
            _goldIcon = _ui.Find("GoldIcon") as RectTransform;
            _goldTextRect = _ui.Find("GoldText") as RectTransform;
            _goldTmp = _ui.Get<TMPro.TextMeshProUGUI>("GoldText");
            if (_goldTmp != null) _goldTextBase = _goldTmp.color;
            var icon = _ui.Get<Image>("GoldIcon");
            if (icon != null) _coinSprite = icon.sprite;
        }

        /// <summary>
        /// 동전이 날아다닐 판. **처음 골드가 들어올 때** 만든다 —
        /// <c>Awake</c> 에서 만들면 로딩 덮개보다 위에 놓여 순서가 꼬인다.
        /// </summary>
        private void EnsureCoinLayer()
        {
            if (_coinLayer != null) return;

            var go = new GameObject("GoldCoinLayer", typeof(RectTransform));
            _coinLayer = (RectTransform)go.transform;
            _coinLayer.SetParent(transform, false);
            _coinLayer.anchorMin = Vector2.zero;
            _coinLayer.anchorMax = Vector2.one;
            _coinLayer.offsetMin = Vector2.zero;
            _coinLayer.offsetMax = Vector2.zero;
            _coinLayer.SetAsLastSibling();
        }

        private void OnRunGoldChanged(RunGoldChangedEvent e)
        {
            _goldTarget = e.Gold;

            // 상점에서 나간 골드, 또는 나온 자리를 모르는 골드는 바로 반영한다.
            // 쓴 것까지 동전이 튀면 번 것과 화면에서 같아 보인다.
            if (!e.HasSource || e.Delta <= 0 || _goldIcon == null || _coinSprite == null)
            {
                AbortCoins();
                SetGoldShown(_goldTarget);
                _goldPop = GoldPopSeconds;
                return;
            }

            EnsureCoinLayer();

            // 액수가 클수록 많이 튀되 상한을 둔다.
            // 비례로 두면 챕터 보상에서 수십 닢이 쏟아진다.
            int n = Mathf.Clamp(e.Delta / 5, 3, 10);
            Vector2 from = _coinLayer.InverseTransformPoint(e.SourceWorld);
            Vector2 to   = _coinLayer.InverseTransformPoint(_goldIcon.position);

            int made = 0;
            for (int i = 0; i < n; i++)
            {
                var c = RentCoin();
                if (c == null) break;
                c.Play(from, to, i * CoinStagger, CoinScatter);
                made++;
            }

            if (made == 0) { SetGoldShown(_goldTarget); return; }
            _coinsInFlight += made;
        }

        private GoldCoin RentCoin()
        {
            for (int i = 0; i < _coins.Count; i++)
                if (!_coins[i].IsActive) return _coins[i];

            if (_coins.Count >= MaxCoins) return null;
            var c = GoldCoin.Create(_coinLayer, _coinSprite, new Vector2(CoinSize, CoinSize));
            _coins.Add(c);
            return c;
        }

        private void AbortCoins()
        {
            for (int i = 0; i < _coins.Count; i++) _coins[i].Despawn();
            _coinsInFlight = 0;
        }

        /// <summary>
        /// 한 닢이 도착했다. 남은 닢 수로 나눠 숫자를 조금씩 올린다.
        ///
        /// ⚠ 액수를 미리 나눠 두지 않는다. 앞 무리가 날고 있는 중에 다음 골드가
        ///   들어오면 몫이 어긋나 숫자가 목표를 지나쳤다 돌아온다.
        ///   **남은 닢 수로 그때그때 나누면** 겹쳐도 항상 목표로 수렴한다.
        /// </summary>
        private void OnCoinArrived()
        {
            _coinsInFlight--;
            if (_coinsInFlight <= 0)
            {
                _coinsInFlight = 0;
                SetGoldShown(_goldTarget);
            }
            else
            {
                int step = Mathf.Max(1, (_goldTarget - _goldShown) / (_coinsInFlight + 1));
                SetGoldShown(Mathf.Min(_goldTarget, _goldShown + step));
            }
            _goldPop = GoldPopSeconds;
        }

        private void SetGoldShown(int value)
        {
            _goldShown = value;
            _ui.SetText("GoldText", value.ToString("N0"));
        }

        private void TickGoldFx(float dt)
        {
            for (int i = 0; i < _coins.Count; i++)
                if (_coins[i].Tick(dt)) OnCoinArrived();

            if (_goldPop <= 0f) return;

            _goldPop -= dt;
            float k = Mathf.Clamp01(_goldPop / GoldPopSeconds);   // 1 → 0
            float scale = 1f + 0.35f * k;
            if (_goldTextRect != null) _goldTextRect.localScale = new Vector3(scale, scale, 1f);
            if (_goldIcon != null) _goldIcon.localScale = new Vector3(scale, scale, 1f);
            if (_goldTmp != null) _goldTmp.color = Color.Lerp(_goldTextBase, GoldFlash, k);

            if (_goldPop > 0f) return;

            _goldPop = 0f;
            if (_goldTextRect != null) _goldTextRect.localScale = Vector3.one;
            if (_goldIcon != null) _goldIcon.localScale = Vector3.one;
            if (_goldTmp != null) _goldTmp.color = _goldTextBase;
        }

        /// <summary>들어오는 순간만 금색으로 튄다. 평소 색은 그대로 둔다.</summary>
        private static readonly Color GoldFlash = new(1f, 0.85f, 0.35f, 1f);

        // ── 플로팅 가상 패드 ─────────────────────────────────────
        // 화면 아무 곳이나 누르면 그 자리로 패드가 따라오고, 손을 떼면 제자리로 돌아간다.
        // 한손 세로 조작에서 엄지가 닿는 위치는 매번 다르다 — 고정 패드는 손을 옮기게 만든다.

        private void HookDPad()
        {
            if (_dpad == null) return;

            // 패드가 하단 `ControlGroup` 안에 있으면 그 좁은 영역 밖으로 못 나간다.
            // 화면 어디로든 따라가야 하므로 루트로 올리고, 조작 버튼보다는 아래에 둔다.
            // ⚠ `ControlGroup` **뒤**로 보내면 안 된다. 그 안에 불투명한 격자 바닥
            //   (`ControlGrid`, 720×230)이 깔려 있어서 패드가 통째로 가려진다.
            //   조작 버튼보다는 아래, 격자보다는 위 — 그래서 바로 다음 자리다.
            var control = _ui.Find("ControlGroup");
            int controlIndex = control != null ? control.GetSiblingIndex() + 1 : transform.childCount;
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

            // ⚠ `e.HasHost` 만 보면 안 된다. 전투는 **유령도 몸으로 센다** —
            //   판이 유령으로 시작하면 이 값이 참으로 오고, HP 바가 매 프레임
            //   다시 켜져서 `ShowNoHost` 로 끈 것이 도로 살아난다.
            _ui.SetActive("HostHpBarBg", e.HasHost && _hasRealHost);
            _ui.SetActive("HostHpText", e.HasHost);
            if (!e.HasHost) return;
            _ui.SetText("HostHpText", $"{e.HostHp}/{e.HostHpMax}");
            _ui.SetFill("HostHpBarFill", Ratio(e.HostHp, e.HostHpMax), HostBarWidth);
        }

        private void OnBossHp(BossHpChangedEvent e)
        {
            bool show = e.BossHpMax > 0;
            _ui.SetActive("BossGroup", show);

            // ⚠ **챕터 칸은 끄지 않는다.** 상단 오른쪽은 챕터 정보 자리다.
            //   한때 여기서 껐다가 "왜 보스 HP 로 바뀌었냐" 는 지적을 받았다 —
            //   보스 체력은 챕터 정보를 밀어내는 것이 아니다.
            //   겹치던 것은 코드가 아니라 **프리팹 자리**로 풀었다:
            //   보스 체력은 상단 HUD 아래 제 줄(가로 696)로 내려갔다.

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
            // ⚠ 유령은 몸이 아니다. 판이 유령으로 시작하면서 이 이벤트가 유령 키로
            //   날아오는데, 그대로 그리면 CURRENT HOST 칸에 **유령이 호스트로** 앉는다.
            if (e.PossessedHostKey == Game.Character.HostEntry.GhostKey) { ShowNoHost(); return; }
            _hasRealHost = true;
            _ui.SetActive("CurrentHostPanel", true);

            _ui.SetText("HostLabel", "CURRENT HOST");
            _ui.SetActive("HostPortraitFrame", true);
            _ui.SetText("HostNameEnText", e.DisplayNameEn);
            _ui.SetText("HostNameKrText", e.DisplayNameKr);
            _ui.SetText("HostLevelText", $"LV. {(_player != null ? _player.GhostLevel : 1)}");
            _ui.SetText("HpLabelHost", "HP");
            _ui.SetActive("HostLevelBadge", true);
            _ui.SetActive("HostHpBarBg", true);

            // 프레임 크롭에 목업의 얼굴이 함께 들어가 있다. 실제 호스트 초상을 위에 덮는다.
            var portrait = _ui.Get<Image>("HostPortraitImage");
            if (portrait != null && _battle != null)
            {
                portrait.sprite = _battle.UnitSprite(e.PossessedHostKey);
                portrait.preserveAspect = true;
                portrait.gameObject.SetActive(portrait.sprite != null);
            }
            // 액티브 스킬 버튼도 그 몸의 것으로 바꾼다. 23종이 같은 그림이면
            // 무엇을 들고 있는지가 화면에 안 보인다.
            SetSkillButton(e.PossessedHostKey);
            SetPossessReady(false);
        }

        private void OnHostLost() => ShowNoHost();

        /// <summary>
        /// 몸이 없는 상태의 HUD.
        ///
        /// **HOST 칸을 통째로 끈다.** 프레임 그림(`HostPortraitFrame`)에는 목업 얼굴이
        /// 함께 크롭돼 있어서, 초상만 끄고 프레임을 남기면 **있지도 않은 몸의 얼굴**이
        /// 남는다. 액티브 스킬 버튼도 감춘다 — 몸이 없으면 쓸 스킬이 없다.
        ///
        /// ⚠ 판은 **유령으로 시작한다.** 그래서 `HostLostEvent` 를 기다리면 안 된다 —
        ///   한 번도 몸을 잃은 적이 없으니 그 이벤트가 오지 않고, 프리팹 기본값이
        ///   그대로 화면에 남는다. 부팅에서도 이걸 부른다.
        /// </summary>
        /// <summary>안 바뀌는 문구는 여기서 한 번만 넣는다.</summary>
        private void SetStaticLabels()
        {
            _ui.SetText("PlayerSoulLabel", "PLAYER SOUL");
            _ui.SetText("GhostNameText", "GHOST");
            _ui.SetText("GhostSubText", "육체와 별개로 유지되는 영혼 체력");
            _ui.SetText("HpLabelGhost", "HP");
            _ui.SetText("RunResourceLabel", "RUN RESOURCES");
            _ui.SetText("FreeMoveLabel", "FREE MOVE AREA");
            _ui.SetText("ActionLabel", "ACTION");
            // ⚠ `ULTIMATE` 이 아니라 `ACTIVE` 다 — 기획에서 얼티밋을 액티브 스킬로
            //   정리했고, 로비 카드도 `ACTIVE SKILL` 로 맞춰 놨다.
            _ui.SetText("SkillButtonLabel", "ACTIVE");
            _ui.SetText("PossessButtonLabel", "POSSESS");

            // 두 LV 배지는 **같은 값**이다. 몸은 고스트가 들고 온 레벨로 굴러간다 —
            // 호스트마다 레벨을 따로 쌓지 않는 것이 이 게임의 규칙이다.
            int lv = _player != null ? _player.GhostLevel : 1;
            _ui.SetText("LevelText", $"LV. {lv}");
            _ui.SetText("HostLevelText", $"LV. {lv}");
        }

        /// <summary>
        /// 진짜 몸을 입고 있는가. **유령은 아니다** — 전투는 유령도 몸으로 세지만
        /// 화면에서는 "몸 없음" 으로 그려야 한다.
        /// </summary>
        private bool _hasRealHost;

        private void ShowNoHost()
        {
            _hasRealHost = false;
            _ui.SetActive("CurrentHostPanel", false);
            _ui.SetText("HostLabel", string.Empty);
            _ui.SetText("HostNameEnText", string.Empty);
            _ui.SetText("HostNameKrText", string.Empty);
            _ui.SetText("HostLevelText", string.Empty);
            _ui.SetText("HpLabelHost", string.Empty);
            _ui.SetActive("HostLevelBadge", false);
            _ui.SetActive("HostHpBarBg", false);
            _ui.SetText("HostHpText", string.Empty);
            _ui.SetActive("HostPortraitFrame", false);
            _ui.SetActive("HostPortraitImage", false);
            SetSkillButton(null);
        }

        // ── 칸이 비면 남은 것이 저절로 가운데로 온다 ───────────────
        //
        // 몸이 없으면 HOST 칸과 액티브 스킬 버튼이 함께 사라진다. 그 자리를 빈 채로
        // 두면 화면 한쪽이 뜯겨 나간 것처럼 보인다.
        //
        // ⚠ **좌표를 코드로 계산하지 않는다.** 프리팹의 두 줄이 각각
        //   `HorizontalLayoutGroup`(childAlignment = UpperCenter)을 달고 있어서,
        //   자식을 끄면 남은 것이 스스로 가운데로 온다.
        //
        //     HudRow2    CurrentHostPanel · ChapterGroup
        //     ButtonRow  SkillButton · PossessButton
        //
        //   보스 체력(`BossGroup`)은 이 줄에 없다 — 챕터 칸과 나란히 서면 줄이
        //   넘친다(374 + 326 + 326 > 720). 자리를 따로 잡아야 한다.
        //
        //   예전에는 여기서 부모 폭을 재고 절반을 빼서 직접 옮겼는데, 그러면
        //   **같은 자리를 두 곳에서 정하게 된다** — 프리팹을 다시 잡을 때마다
        //   이 코드가 조용히 어긋난다. 실제로 챕터 칸과 보스 체력이 같은 자리에
        //   겹쳐 있는데도 코드가 한쪽만 옮겨 절반씩 포개져 있었다.
        //   지금은 켜고 끄기만 하면 된다.

        /// <summary>봉인된 스킬 아이콘 색. 끄지 않고 눌러서 "있는데 잠겼다" 로 읽힌다.</summary>
        private static readonly Color SealedSkillTint = new(0.38f, 0.40f, 0.48f, 1f);

        /// <summary>
        /// 액티브 스킬 버튼.
        ///
        /// **유령이면 통째로 감춘다** — 몸이 없으면 쓸 스킬 자체가 없다.
        /// 누를 수 없는 버튼을 띄워 두면 "왜 안 눌리지" 가 된다.
        ///
        /// **봉인(숙련도 0)이면 남겨 두되 자물쇠와 딤을 얹는다** — 감추면
        /// 이 몸에 스킬이 있다는 것조차 모르고, 그러면 풀 이유도 안 생긴다.
        /// </summary>
        private void SetSkillButton(string hostKey)
        {
            bool hasHost = !string.IsNullOrEmpty(hostKey);
            _ui.SetActive("SkillButton", hasHost);
            if (!hasHost) return;

            bool sealedSkill = _battle != null && _battle.IsSkillSealed(hostKey);

            var icon = _ui.Get<Image>("SkillIcon");
            if (icon != null && _battle != null)
            {
                icon.sprite = _battle.ActiveSkillIcon(hostKey);
                icon.enabled = icon.sprite != null;
                icon.color = sealedSkill ? SealedSkillTint : Color.white;
            }
            _ui.SetActive("SkillSealIcon", sealedSkill);

            var btn = _ui.Get<Button>("SkillButton");
            if (btn != null) btn.interactable = !sealedSkill;
        }

        /// <summary>방 진행 바 폭. 프리팹 `RoomProgressBg` 와 같아야 한다.</summary>
        private const float RoomProgressWidth = 302f;

        private void OnRoomEntered(RoomEnteredEvent e)
        {
            CloseRoomPanels();   // 앞 방의 상점·이벤트 창을 걷는다

            // 방 하나가 곧 스테이지 하나다. 마지막 스테이지가 보스방.
            // 방의 성격을 함께 보여준다 — 정예방에 들어선 걸 모르면 대비할 수 없다.
            string kind = e.Kind switch
            {
                RoomKind.Boss => "BOSS",
                RoomKind.Elite => "ELITE",
                RoomKind.Rest => "REST",
                _ => null,
            };
            // CHAPTER 칸 — 챕터 번호 · 무대 이름 · 방 번호.
            // ⚠ 런 통짜 번호(`13/45`)만 찍으면 **챕터가 어디서 갈리는지 안 보인다.**
            //   무대는 챕터 안에서 또 한 번 바뀌므로 이름도 방 번호를 따라간다.
            _ui.SetText("ChapterLabel", $"CHAPTER {e.Chapter}");
            _ui.SetText("ChapterNameText",
                        _battle != null ? _battle.StageNameOf(e.Chapter, e.StageInChapter) : string.Empty);
            _ui.SetText("RoomLabel", "ROOM");
            _ui.SetText("RoomNumberText", e.StageInChapter.ToString("00"));
            _ui.SetText("RoomTotalText", $"/ {e.ChapterTotal}");
            _ui.SetFill("RoomProgressFill",
                        e.ChapterTotal > 0 ? (float)e.StageInChapter / e.ChapterTotal : 0f,
                        RoomProgressWidth);

            // 방의 성격은 알림 줄에 잠깐 띄운다 — 정예방에 들어선 걸 모르면 대비할 수 없다.
            _ui.SetText("StageText", kind != null && e.Kind != RoomKind.Boss ? $"{kind} ROOM" : string.Empty);
        }

        private void OnEmergencyHost(EmergencyHostEvent e)
            => _ui.SetText("StageText", $"긴급 빙의 — GHOST -{e.GhostCost}");

        /// <summary>
        /// 증원 예고. 방을 다 비웠다고 생각한 순간 여섯 기가 소리 없이 나타나면
        /// 기획이 아니라 버그로 읽힌다 — 나오기 전에 한 줄이라도 알린다.
        /// </summary>
        private void OnExitOpened(ExitOpenedEvent e)
            => _ui.SetText("StageText", "출구가 열렸다 — 통과해서 다음 스테이지로");

        /// <summary>
        /// 레벨 표기만 남긴다.
        ///
        /// ⚠ 경험치 바는 걷어냈다. **차오르지만 절대 안 오르는 바**였다 —
        ///   고스트 Lv 은 로비에서 골드로 사는 값이라 판 안에서는 변하지 않는다.
        ///   차오르는 바는 "다 차면 뭔가 된다" 는 약속인데 지킬 것이 없었다.
        /// </summary>
        /// <summary>
        /// ⚠ 이 이벤트로 `LevelText` 를 쓰지 않는다. 그건 **런 안의 버프 레벨**이고,
        ///   화면의 LV 배지는 **고스트 Lv** 다 — 판 안에서 안 바뀌므로 이벤트를
        ///   기다리면 영영 빈칸으로 남는다. 배지는 부팅에서 한 번 채운다.
        /// </summary>
        private void OnExpChanged(RunExpChangedEvent e) { }

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

            // 몸을 놓아줄 때만 값이 붙는다. 유령 상태의 빙의는 공짜다.
            _ui.SetText("PossessCostText", hasTarget && cost > 0 ? $"-{cost}" : string.Empty);
        }

        private void SetPossessReady(bool ready) => SetPossessState(ready, 0, false);

        /// <summary>
        /// 유지 훅 표시. 무엇이 얼마나 쌓였는지가 보여야 교체할 때 무엇을 버리는지 안다.
        /// 몸이 없으면 감춘다 — 유령 상태에는 쌓을 것이 없다.
        /// </summary>
        /// <summary>
        /// 전술 빙의 쿨다운. 남은 초와 차오르는 덮개를 함께 보여준다.
        /// 숫자만으로는 "얼마나 남았나"가 손에 안 잡히고, 덮개만으로는 정확한 값을 모른다.
        /// </summary>
        private void OnRepossessLock(RepossessLockEvent e)
        {
            _ui.SetText("PossessCooldownText",
                        e.Remain > 0f ? Mathf.CeilToInt(e.Remain).ToString() : string.Empty);
            if (_possessCooldown != null)
                _possessCooldown.fillAmount = e.Total > 0f ? Mathf.Clamp01(e.Remain / e.Total) : 0f;
        }

        private void RefreshCurrency()
        {
            // ⚠ <c>GoldText</c> 는 <c>RunResourcePanel</c> 안에 있다 — **판 골드**이지 계정 골드가 아니다.
            //   계정 골드를 적으면 첫 방을 비우는 순간 1,250 → 10 으로 뚝 떨어진다.
            //   이 골드는 이벤트·상점이 쓰는 판 안의 주머니이고, 판마다 0 에서 시작한다.
            _goldTarget = _battle != null ? _battle.RunGold : 0;
            SetGoldShown(_goldTarget);

            if (_player == null || !_player.IsReady) return;
            _ui.SetText("GemText", _player.Gem.ToString("N0"));
        }

        private static float Ratio(int v, int max) => max > 0 ? Mathf.Clamp01((float)v / max) : 0f;

        // ── 레벨업 버프 3택1 ─────────────────────────────────────
        // ⚠ 이 창은 방을 비워서 열리는 게 아니라 **레벨이 올라서** 열린다(기획서 A 5-2).
        //    제목이 `ROOM CLEAR` 로 박혀 있어서, 방에 적이 남았는데 클리어가 떴다는
        //    오해를 샀다. 무엇 때문에 열렸는지는 화면이 말해야 한다.
        // ── 이벤트 방 ────────────────────────────────────────────
        //
        // 레벨업 3택1 과 같은 자리에 뜨지만 성격이 다르다.
        // 3택1 은 "무엇을 받을까", 이건 "무엇을 내줄까" 다.
        // 그래서 대가 줄을 제목 아래가 아니라 **버튼 바로 위**에 둔다 —
        // 누르기 직전에 값이 눈에 들어와야 한다.

        /// <summary>
        /// 방이 바뀌면 앞 방의 창을 닫는다.
        ///
        /// 정상 진행에서는 창을 닫아야 출구가 열리지만, **죽어서 방을 벗어나는 길**이
        /// 따로 있다. 그때 닫지 않으면 다음 방에서 지난 방 상점이 그대로 떠 있다.
        /// 진행 쪽(`BattleDirector.EnterRoom`)에서 상태는 이미 지우고 있고,
        /// 화면에서 걷어 내는 것은 여기 몫이다.
        /// </summary>
        private void CloseRoomPanels()
        {
            // ⚠ `RoomEnteredEvent` 는 방 안내가 **다 끝난 뒤**에 발행된다 —
            //    상점·이벤트 창은 그보다 먼저 열린다. 무조건 닫으면 방금 연 창을
            //    같은 프레임에 도로 닫아 버린다. 그래서 진행 쪽에 물어보고 닫는다.
            if (_battle == null || !_battle.IsShopOpen) _ui.SetActive("ShopPanel", false);
            if (_battle == null || _battle.PendingEvent == null) _ui.SetActive("EventPanel", false);
        }

        private void OnEventOffer(EventOfferEvent e)
        {
            _ui.SetText("EventTitleText", e.Title);
            _ui.SetText("EventBodyText", e.Body);
            // 못 고르는 이유를 **누르기 전에** 적는다. 값이 모자란 것과
            // 몸이 없어 못 받는 것은 다른 이유라 문구도 달라야 한다.
            // ⚠ 줄표(`— ... —`)를 붙이지 않는다. 명판 그림(`eventcostpill`)이
            //   양끝 해골 장식을 이미 갖고 있어 줄표까지 넣으면 두 겹이 된다.
            string costLine =
                !string.IsNullOrEmpty(e.BlockedReason)
                    ? (string.IsNullOrEmpty(e.CostLabel)
                        ? e.BlockedReason
                        : $"{e.CostLabel} · {e.BlockedReason}")
                : string.IsNullOrEmpty(e.CostLabel) ? string.Empty
                : e.CostLabel;
            _ui.SetText("EventCostText", costLine);

            _ui.SetText("EventAcceptText", e.AcceptLabel);
            _ui.SetText("EventDeclineText",
                string.IsNullOrEmpty(e.DeclineLabel) ? "지나간다" : e.DeclineLabel);
            _ui.SetText("EventResultText", string.Empty);

            _ui.SetActive("EventResultText", false);
            _ui.SetActive("EventAcceptButton", true);
            _ui.SetActive("EventDeclineButton", true);
            _ui.SetActive("EventCostText", true);
            _ui.SetActive("EventCostPill", !string.IsNullOrEmpty(costLine));
            _ui.SetActive("EventBodyText", true);

            // 값을 못 치르면 버튼을 잠근다. 눌러 놓고 아무 일도 안 일어나면
            // 고장으로 보인다.
            var acceptBtn = _ui.Get<Button>("EventAcceptButton");
            if (acceptBtn != null)
            {
                acceptBtn.onClick.RemoveAllListeners();
                acceptBtn.interactable = e.CanAfford;
                if (e.CanAfford) acceptBtn.onClick.AddListener(() => Resolve(true));
            }
            // 그림(`eventacceptbutton`)이 붙은 뒤로는 **색을 곱하기만 한다.**
            // 예전처럼 금색을 칠하면 용암 그림이 통째로 노래진다.
            var acceptImg = _ui.Get<Image>("EventAcceptButton");
            if (acceptImg != null)
                acceptImg.color = e.CanAfford
                    ? Color.white
                    : new Color(0.45f, 0.42f, 0.42f, 1f);

            var declineBtn = _ui.Get<Button>("EventDeclineButton");
            if (declineBtn != null)
            {
                declineBtn.onClick.RemoveAllListeners();
                declineBtn.onClick.AddListener(() => Resolve(false));
            }

            _ui.SetActive("EventPanel", true);
            _ui.Find("EventPanel")?.SetAsLastSibling();
        }

        /// <summary>
        /// 회복 제단 — 셋 중 하나. **거절 버튼이 없다.**
        /// 셋 다 공짜라 안 고를 이유가 없고, 안 고르는 길을 두면 그 자리가 통로가 된다.
        /// </summary>
        private const int ShrineChoiceCount = 3;

        private void OnShrineOpened(ShrineOpenedEvent e)
        {
            _ui.SetText("ShrineTitleText", "회복의 제단");
            _ui.SetText("ShrineHintText", "하나만 가져갈 수 있다");
            _ui.SetActive("ShrineHintPill", true);
            _ui.SetText("ShrineResultText", string.Empty);
            _ui.SetActive("ShrineResultText", false);

            for (int i = 0; i < ShrineChoiceCount; i++)
            {
                bool has = e.Titles != null && i < e.Titles.Length;
                _ui.SetActive($"ShrineChoice{i}", has);
                if (!has) continue;
                // 이름 한 줄, 그 아래 작은 글씨로 무엇을 주는지.
                _ui.SetText($"ShrineChoice{i}Text",
                            e.Titles[i] + System.Environment.NewLine
                            + $"<size=70%>{e.Descs[i]}</size>");
                var btn = _ui.Get<Button>($"ShrineChoice{i}");
                if (btn == null) continue;
                int pick = i;                     // 클로저가 마지막 값을 잡지 않게 복사한다
                btn.onClick.RemoveAllListeners();
                btn.interactable = true;
                btn.onClick.AddListener(() => ChooseShrine(pick));
            }
            _ui.SetActive("ShrinePanel", true);
            _ui.Find("ShrinePanel")?.SetAsLastSibling();
        }

        private void ChooseShrine(int index)
        {
            if (_battle == null) return;
            _battle.ChooseShrine(index);
        }

        private void OnShrineResolved(ShrineResolvedEvent e)
        {
            for (int i = 0; i < ShrineChoiceCount; i++) _ui.SetActive($"ShrineChoice{i}", false);
            _ui.SetActive("ShrineResultText", true);
            _ui.SetText("ShrineResultText", e.ResultLine);
            _ui.SetText("ShrineHintText", string.Empty);
            _ui.SetActive("ShrineHintPill", false);   // 글자만 지우면 명판이 빈 채로 남는다
            CloseShrineLater().Forget();   // fire-and-forget: 결과를 읽을 틈만 준다
        }

        private async UniTaskVoid CloseShrineLater()
        {
            await UniTask.Delay(1200);
            _ui.SetActive("ShrinePanel", false);
        }

        private void Resolve(bool accept)
        {
            if (_battle == null) return;
            _battle.ResolveEvent(accept);
        }

        private void OnEventResolved(EventResolvedEvent e)
        {
            // 보상이 3택1 이면 그 창이 바로 위에 뜬다. 이벤트 창을 남겨 두면
            // 카드를 고르고 나서 "계속"을 한 번 더 눌러야 한다 — 같은 자리에서
            // 두 번 확인시키지 않는다.
            var offer = _ui.Find("BuffChoicePanel");
            if (offer != null && offer.gameObject.activeSelf)
            {
                _ui.SetActive("EventPanel", false);
                return;
            }

            // 창을 바로 닫지 않는다. 무엇을 얻었는지 한 줄 보여 주고 닫는다 —
            // 즉시 닫으면 값을 치른 결과가 화면에 남지 않는다.
            _ui.SetActive("EventAcceptButton", false);
            _ui.SetActive("EventCostText", false);
            _ui.SetActive("EventCostPill", false);
            _ui.SetText("EventResultText", e.ResultLine);
            _ui.SetActive("EventResultText", true);

            _ui.SetText("EventDeclineText", "계속");
            var btn = _ui.Get<Button>("EventDeclineButton");
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => _ui.SetActive("EventPanel", false));
            }
        }

        // ── 상점 ─────────────────────────────────────────────────
        //
        // 진열대는 매번 다시 그린다. 하나를 사면 남은 골드와 한도가 바뀌어
        // **다른 칸을 살 수 있는지도 함께 바뀌기** 때문이다. 산 칸만 고치면
        // 옆 칸이 아직 살 수 있는 것처럼 남는다.

        /// <summary>
        /// 진열 칸 수. 카드 3 · 몸 1 · 소모품 1 · 회복 1 = 여섯이다(기획 2026-09-08 §6).
        /// 액자 목록 구간이 348px 라 한 칸 52px · 간격 58px 로 딱 여섯 줄이 들어간다.
        /// </summary>
        private const int ShopSlots = 6;

        private void OnShopOpened(ShopOpenedEvent e)
        {
            _ui.SetText("ShopGoldText", $"보유 골드 {e.Gold:N0}");
            _ui.SetText("ShopLimitText", e.LimitLine);

            for (int i = 0; i < ShopSlots; i++)
            {
                bool has = e.Names != null && i < e.Names.Length;
                _ui.SetActive($"ShopItem{i}", has);
                if (!has) continue;

                _ui.SetText($"ShopItem{i}Name", e.Names[i]);
                _ui.SetText($"ShopItem{i}Desc", e.Descs[i]);
                _ui.SetText($"ShopItem{i}Price", $"{e.Prices[i]:N0} G");

                bool can = e.CanBuy[i];
                var img = _ui.Get<Image>($"ShopItem{i}");
                if (img != null)
                {
                    // 못 사는 줄은 **회색 그림**으로 갈아 끼운다. 색만 어둡게 하면
                    // 그림의 금색 테두리가 그대로 남아 살 수 있는 것처럼 보인다.
                    var slotArt = UiArt(can ? "shopitemslot" : "shopitemslot_off");
                    if (slotArt != null) { img.sprite = slotArt; img.color = Color.white; }
                    else img.color = can ? new Color(0.16f, 0.19f, 0.26f, 1f)
                                         : new Color(0.10f, 0.11f, 0.14f, 1f);
                }

                // 분류 아이콘 — 없으면 칸을 비운다(빈 네모를 세우지 않는다)
                var icon = _ui.Get<Image>($"ShopItem{i}Icon");
                if (icon != null)
                {
                    var ic = e.Icons != null && i < e.Icons.Length ? UiArt(e.Icons[i]) : null;
                    icon.sprite = ic;
                    icon.enabled = ic != null;
                    if (ic != null) icon.color = can ? Color.white : new Color(0.45f, 0.45f, 0.5f, 1f);
                }
                // 값을 못 치르는 칸은 값도 흐리게 — 무엇이 모자란지가 값에 있다
                var price = _ui.Get<TMPro.TMP_Text>($"ShopItem{i}Price");
                if (price != null)
                    price.color = can ? new Color(0.941f, 0.753f, 0.220f)
                                      : new Color(0.42f, 0.38f, 0.28f);

                var btn = _ui.Get<Button>($"ShopItem{i}");
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                btn.interactable = can;
                int slot = i;
                if (can) btn.onClick.AddListener(() => { if (_battle != null) _battle.BuyShopItem(slot); });
            }

            var leaveBtn = _ui.Get<Button>("ShopLeaveButton");
            if (leaveBtn != null)
            {
                leaveBtn.onClick.RemoveAllListeners();
                leaveBtn.onClick.AddListener(() =>
                {
                    _ui.SetActive("ShopPanel", false);
                    if (_battle != null) _battle.CloseShop();
                });
            }

            _ui.SetActive("ShopPanel", true);
            _ui.Find("ShopPanel")?.SetAsLastSibling();
        }

        /// <summary>
        /// 레벨업 3택1. 목업(26차 워크오더)의 구조를 그대로 따른다.
        ///
        ///   칩(등급 또는 레벨 변화) → 아이콘 → 이름 → 효과
        ///
        /// ⚠ 판때기·칩 배경은 **리소스다.** 여기서 색으로 지어내지 않는다.
        ///   `cardpanel_{등급}` / `cardchip_*` 이 오면 스프라이트만 꽂으면 된다.
        ///   아직 안 왔으므로 판때기는 단색으로만 깔아 두고(글자가 읽혀야 하므로),
        ///   테두리 색 같은 **디자인 요소는 만들지 않는다.**
        /// </summary>
        private void OnBuffOffer(BuffOfferEvent e)
        {
            if (_buffTable == null || e.OfferedKeys == null) return;

            // 제목은 그림(`leveluptitle`)으로 간다. 게임 서체로는 시안의 두께와
            // 광택이 안 나온다. 그림이 아직 없으면 글자가 대신 선다 —
            // 둘을 같이 띄우면 겹쳐 보인다.
            var titleArt = _ui.Get<Image>("BuffTitleArt");
            var titleSprite = _cardAtlas != null ? _cardAtlas.GetSprite("leveluptitle") : null;
            if (titleArt != null)
            {
                titleArt.sprite = titleSprite;
                titleArt.enabled = titleSprite != null;
                titleArt.color = Color.white;
            }
            _ui.SetText("BuffTitleText", titleSprite != null ? string.Empty : "LEVEL UP!");
            _ui.SetText("BuffSubText", "카드를 선택하세요");

            for (int i = 0; i < BuffCardCount; i++)
            {
                bool has = i < e.OfferedKeys.Length;
                _ui.SetActive($"BuffCard{i}", has);
                if (!has) continue;

                var entry = _buffTable.Get(e.OfferedKeys[i]);
                if (entry == null) { _ui.SetActive($"BuffCard{i}", false); continue; }

                int lv = _battle != null ? _battle.CardLevel(entry.BuffKey) : 0;

                // 칩 — 가진 카드면 레벨 변화, 새 카드면 등급. 목업 그대로.
                // ⚠ 화살표는 `→` 만 쓴다. 픽셀 폰트에는 화살표가 아예 없고
                //   한글 폴백(NotoSansKR)에 `→` 하나만 있다. `▸`·`►` 는 □ 로 나온다.
                _ui.SetText($"BuffCard{i}ChipText",
                    lv > 0 ? $"Lv.{lv} → Lv.{Mathf.Min(lv + 1, entry.MaxLevel)}"
                           : entry.Rarity.ToString().ToUpperInvariant());

                _ui.SetText($"BuffCard{i}Name", entry.NameKr);
                _ui.SetText($"BuffCard{i}Desc", entry.Description);

                SetCardArt(i, entry, lv > 0);

                // 매번 다른 카드가 오므로 이전 리스너를 지우고 새로 건다
                var btn = _ui.Get<Button>($"BuffCard{i}");
                if (btn == null) continue;
                var key = entry.BuffKey;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnBuffPicked(key));
            }

            _ui.SetActive("BuffChoicePanel", true);
            _ui.Find("BuffChoicePanel")?.SetAsLastSibling();
        }

        /// <summary>
        /// 카드 한 장에 그림을 꽂는다.
        ///
        /// 판때기·칩·아이콘테두리는 전부 **아틀라스에서 이름으로 꺼낸다.**
        /// 아직 안 온 것은 꽂지 않는다 — 대신 그리지 않는다.
        /// </summary>
        private void SetCardArt(int i, BuffEntry entry, bool owned)
        {
            string rarity = entry.Rarity.ToString().ToLowerInvariant();

            // ① 판때기 — 등급별 액자
            var panel = _ui.Get<Image>($"BuffCard{i}");
            var panelArt = _cardAtlas != null ? _cardAtlas.GetSprite($"cardpanel_{rarity}") : null;
            if (panel != null)
            {
                panel.sprite = panelArt;
                panel.type = panelArt != null ? Image.Type.Sliced : Image.Type.Simple;
                // 판때기가 아직 없으면 글자가 읽히게 속만 깔아 둔다(테두리는 만들지 않는다)
                panel.color = panelArt != null ? Color.white
                                               : new Color(0.078f, 0.102f, 0.157f, 0.98f);
            }

            // ② 칩
            var chip = _ui.Get<Image>($"BuffCard{i}Chip");
            var chipArt = _cardAtlas != null
                ? _cardAtlas.GetSprite(owned ? "cardchip_level" : "cardchip_rarity") : null;
            if (chip != null)
            {
                chip.sprite = chipArt;
                chip.enabled = chipArt != null;
            }
            var chipText = _ui.Get<TMPro.TMP_Text>($"BuffCard{i}ChipText");
            if (chipText != null)
                // ⚠ 명판 그림은 **어두운 돌**이다(실측 밝기 25/255). 예전에는 그림이
                //   있으면 글자를 거의 검정으로 칠했는데, 그 위에서는 아예 안 보였다.
                //   이미 가진 카드(레벨 표시)는 금빛으로 갈라 한눈에 구분되게 한다.
                chipText.color = owned ? new Color(0.94f, 0.71f, 0.16f)
                                       : new Color(0.88f, 0.92f, 0.98f);

            // ③ 아이콘 + 아이콘 테두리 (이미 있는 리소스)
            var icon = _ui.Get<Image>($"BuffCard{i}Icon");
            if (icon == null) return;
            var art = _cardAtlas != null && !string.IsNullOrEmpty(entry.CardId)
                ? _cardAtlas.GetSprite($"card_{entry.CardId.ToLowerInvariant()}") : null;
            icon.sprite = art;
            icon.enabled = art != null;
            icon.color = Color.white;

            var frameArt = _cardAtlas != null ? _cardAtlas.GetSprite($"card_frame_{rarity}") : null;
            var frame = GetOrMakeCardImage((RectTransform)icon.transform.parent, "IconFrame");
            if (frame != null)
            {
                frame.sprite = frameArt;
                frame.enabled = frameArt != null;
                frame.color = Color.white;
                frame.transform.SetSiblingIndex(0);   // 아이콘 뒤에
                FitAroundIcon((RectTransform)frame.transform, (RectTransform)icon.transform);
            }
        }

        /// <summary>테두리가 아이콘보다 이만큼 크다. 사방으로 반씩 나눠 커진다.</summary>
        private const float IconFrameGrow = 16f;

        /// <summary>
        /// 테두리를 아이콘에 정확히 겹쳐 놓는다.
        ///
        /// ⚠ 예전에는 아이콘의 `anchoredPosition` 을 그대로 복사했는데,
        ///   아이콘은 좌상단 앵커(0,1)이고 테두리는 가운데 앵커(0.5,1)로 만들어져
        ///   같은 숫자가 **다른 곳을 가리켰다.** 아이콘 x=48 이 테두리에서는
        ///   "카드 중앙에서 오른쪽으로 48" 이 되어 테두리만 옆으로 밀려 있었다.
        ///   앵커·피벗까지 아이콘 것을 그대로 가져와 좌표계를 맞춘다.
        /// </summary>
        private static void FitAroundIcon(RectTransform frame, RectTransform icon)
        {
            frame.anchorMin = icon.anchorMin;
            frame.anchorMax = icon.anchorMax;
            frame.pivot = icon.pivot;
            frame.sizeDelta = icon.sizeDelta + new Vector2(IconFrameGrow, IconFrameGrow);

            // 피벗이 가운데가 아니면 커진 만큼 한쪽으로만 자란다. 그 절반을 되민다.
            frame.anchoredPosition = icon.anchoredPosition
                - new Vector2((0.5f - icon.pivot.x) * IconFrameGrow,
                              (0.5f - icon.pivot.y) * IconFrameGrow);
        }

        // ── 카드 그림 ────────────────────────────────────────────
        //
        // 아이콘 테두리만 런타임에 만들어 붙인다 — 아이콘 **뒤**에 깔려야 하는데
        // 표에서 형제 순서를 지정할 방법이 없기 때문이다. 나머지(판때기·칩·아이콘)는
        // 전부 표가 만든 자리를 그대로 쓴다.
        private SpriteAtlas _cardAtlas;
        private SpriteAtlas _uiAtlas;

        /// <summary>
        /// 화면 아틀라스에서 한 장. 없으면 null — 부르는 쪽이 단색으로 버틴다.
        ///
        /// `unit:` 으로 시작하면 **유닛 아틀라스**에서 꺼낸다. 상점이 파는 몸의 초상이
        /// 그렇다 — UI 아틀라스에는 그 얼굴이 없어서 칸이 비어 있었다.
        /// </summary>
        private Sprite UiArt(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.StartsWith("unit:"))
                return _battle != null ? _battle.UnitSprite(key.Substring(5)) : null;
            return _uiAtlas != null ? _uiAtlas.GetSprite(key) : null;
        }

        /// <summary>
        /// 방에서 열리는 창 셋(상점·악마·제단)에 그림을 입힌다.
        /// **아틀라스가 온 뒤에** 한 번만 부른다.
        ///
        /// 세 창이 같은 화면에 번갈아 뜨므로 한 자리에서 같이 입힌다 —
        /// 따로 두면 한 창만 그림이 빠져도 눈에 안 띈다.
        ///
        /// ⚠ 그림을 물릴 때 `color` 를 흰색으로 되돌린다. 단색 시절의 색이 남아 있으면
        ///   그림에 그 색이 곱해져 칙칙해진다 — 액자가 회색으로 뜬다.
        /// </summary>
        private void SkinPopups()
        {
            // 상점 「유령 노점」
            Skin("ShopBox", "shopframe");
            Skin("ShopLeaveButton", "shopleavebutton");
            for (int i = 0; i < ShopSlots; i++) Skin($"ShopItem{i}", "shopitemslot");

            // 악마 「봉인된 궤짝」
            Skin("EventBox", "eventframe");
            Skin("EventCostPill", "eventcostpill");
            Skin("EventAcceptButton", "eventacceptbutton");
            Skin("EventDeclineButton", "eventdeclinebutton");

            // 천사 「회복의 제단」
            Skin("ShrineBox", "shrineframe");
            Skin("ShrineHintPill", "shrinehintpill");
            for (int i = 0; i < ShrineChoiceCount; i++) Skin($"ShrineChoice{i}", "shrinechoiceslot");

            void Skin(string node, string art)
            {
                var img = _ui.Get<Image>(node);
                var sp = UiArt(art);
                // 그림이 없으면 **손대지 않는다.** 명판은 꺼진 채로 시작하므로
                // 여기서 켜지 않으면 빈 네모가 서지 않는다.
                if (img == null || sp == null) return;
                img.sprite = sp;
                img.color = Color.white;
                img.type = Image.Type.Simple;
                img.enabled = true;
            }
        }

        /// <summary>자리는 잡지 않는다 — 부르는 쪽이 아이콘에 맞춰 놓는다.</summary>
        private Image GetOrMakeCardImage(RectTransform card, string name)
        {
            var found = card.Find(name) as RectTransform;
            if (found != null) return found.GetComponent<Image>();

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(card, false);
            go.GetComponent<Image>().raycastTarget = false;   // 카드 버튼이 눌려야 한다
            return go.GetComponent<Image>();
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
