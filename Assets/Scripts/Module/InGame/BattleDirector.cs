using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Loading;
using GameFramework.Core.Module.Resource;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Localize = Game.Module.Common.Localize;
using GameSound = Game.Module.Common.GameSound;

namespace Game.Module.InGame
{
    /// <summary>
    /// 전투 진행 전체를 맡는다 — 룸 생성, 유닛 AI, 빙의, 액티브 스킬, 종료 판정.
    ///
    /// 게임 흐름(GameComposition 4절)
    ///   룸 입장 → 오토어택 교전 → [호스트 사망] 고스트 복귀 → 재빙의
    ///   → 전멸 → 룸 클리어 → 다음 룸 → (마지막 룸) 보스 → 스테이지 클리어
    ///
    /// 상태는 전부 이 클래스가 들고, 화면 표시는 이벤트로만 흘려보낸다.
    /// UI 가 이 클래스를 직접 참조하지 않아야 룸 로직을 UI 없이 테스트할 수 있다.
    /// </summary>
    public sealed partial class BattleDirector : MonoBehaviour
    {
        private const string AtlasAddress = "atlas/ingamemainui";

        /// <summary>
        /// 캐릭터 아틀라스 주소 접두사. 캐릭터 한 종이 아틀라스 하나다
        /// (`Assets/BaseResource/Unit/{key}/` ↔ `atlas/unit_{key}`).
        ///
        /// 화면 아틀라스에 섞지 않는 이유: 한 방에 실제로 나오는 캐릭터는 몇 종뿐인데
        /// 통짜 아틀라스는 12종을 전부 메모리에 올린다. 방향 5장에 공격 프레임까지
        /// 붙으면 한 종이 20장이 되어 감당이 안 된다.
        /// </summary>
        private const string UnitAtlasPrefix = "atlas/unit_";

        private RectTransform _field;
        private RectTransform _unitLayer;
        private GameConfig _config;
        private IPlayerDataService _player;
        private SpriteAtlas _atlas;                                        // HUD·탄·바닥
        private ILoadingManager _loading;                                  // 씬 전환 가림막
        private readonly Dictionary<string, SpriteAtlas> _unitAtlas = new();  // 캐릭터 키 → 아틀라스
        private IEventBus _bus;

        private Unit _ghost;
        private Unit _host;                       // 빙의 중이 아니면 null
        private readonly List<Unit> _enemies = new();
        private readonly List<Unit> _dead = new();   // 정리용 재사용 버퍼 (hot path 할당 금지)

        /// <summary>
        /// 사망 연출이 도는 몸. `_enemies` 에서는 이미 빠져 있어 표적도 충돌도 되지 않고,
        /// 그림만 남아 쓰러지다 사라진다. 연출이 끝나면 여기서 빼고 없앤다.
        /// </summary>
        private readonly List<Unit> _dying = new();

        /// <summary>피해 수치. 탄과 같은 풀 방식 — 타격마다 만들면 교전 중 GC 가 튄다.</summary>
        private readonly List<DamageText> _damageTexts = new();
        private RectTransform _textLayer;

        /// <summary>탄이 터지는 그림. 탄과 같은 풀 방식이다.</summary>
        private const int MaxImpacts = 24;
        private readonly List<Impact> _impacts = new();

        private const int MaxShots = 64;
        private readonly List<Projectile> _shots = new();
        private RectTransform _shotLayer;
        private RectTransform _fieldLayer;

        private int _roomIndex = -1;
        private int _ghostHp;
        /// <summary>유령 자연 감소의 소수점 이월. 프레임마다 반올림하면 3/초가 안 맞는다.</summary>
        private float _drainCarry;
        private float _invuln;
        private float _ghostProtect;
        /// <summary>문 하나. 갈림길 방은 둘이고 어느 쪽으로 나가느냐가 곧 선택이다.</summary>
        private sealed class ExitGate
        {
            public RectTransform View;
            public Image Img;
            public string NextRoomId;
            public GameObject Label;
        }

        private readonly List<ExitGate> _exits = new();

        // ── 문은 처음부터 서 있다 ────────────────────────────────
        //
        // 예전에는 방을 다 정리해야 문이 **허공에서 생겼다.** 그래서 처음 하는 사람은
        // "이 방을 어떻게 끝내는지" 자체를 몰랐다. 목표가 안 보이는 방을 헤매게 된다.
        //
        // 이제 들어서는 순간부터 닫힌 문이 보이고, 방을 비우면 열리는 것을 보여 준다.
        //   진입   저기로 나가는구나
        //   전투   아직 아니구나
        //   클리어 지금이구나

        /// <summary>문이 열려 있는가. 닫혀 있으면 닿아도 안 넘어간다.</summary>
        private bool _exitOpen;

        /// <summary>여는 연출이 시작되고 지난 시간. 음수면 아직 안 열렸다.</summary>
        private float _exitOpenTime = -1f;

        /// <summary>닫힘 → open1 → open2 → 열림. 네 장을 이 시간에 걸쳐 넘긴다.</summary>
        private const float ExitOpenSeconds = 0.5f;
        private float _skillCooldown;

        /// <summary>
        /// 액티브 스킬은 **때려서** 찬다 (2026-09-09). 예전에는 시간이 알아서 채웠다.
        ///
        /// 시간제는 싸우든 안 싸우든 똑같이 차서, 스킬이 「내가 번 것」이 아니라
        /// 「기다리면 오는 것」이었다. 가만히 서 있다가 꽉 차면 쓰는 것이 최적이 된다.
        /// 때려서 채우면 붙어서 싸우는 쪽이 이득이 된다.
        ///
        /// 한 대에 <see cref="SkillHitsToFull"/> 분의 1 씩. 몸마다 쿨이 다르므로
        /// 초가 아니라 **비율**로 채운다 — 쿨 긴 몸이 20대보다 더 맞아야 하면
        /// 「이 몸은 스킬을 못 쓴다」가 되어 버린다.
        /// </summary>
        private const int SkillHitsToFull = 30;   // 기획 2026-09-15 — 20대 → 1.5배 더 때려야 찬다

        private void ChargeSkillOnHit()
        {
            float full = SkillCooldownOf(_host?.Profile);
            if (full <= 0f) return;
            _skillCooldown = Mathf.Min(
                _skillCooldown + full / SkillHitsToFull * _buffs.ActiveSkillChargeMul, full);
        }
        private bool _running;
        private Unit _possessTarget;
        private bool _hadPossessTarget;
        private bool _hadPossessBlocked;
        private int _hadPossessCost = -1;

        // ── 유지 훅 ───────────────────────────────────────────────
        // 정본이 "fun-critical" 로 못박은 장치다. 몸을 오래 탈수록 그 몸에서만
        // 쌓이는 것이 생기고, 갈아타면 사라진다.
        //
        // 이게 없으면 전술 빙의는 **비용만 있고 잃는 게 없는** 선택이 된다.
        // 값을 내는 쪽만 있으면 "안 바꾸는" 것이 언제나 정답이라, 물음 자체가 성립하지 않는다.
        //
        // 정본은 훅의 **이름**만 준다(표식 릴레이·콤보 미터…). 실제 효과는 그 몸의
        // 시그니처를 구현해야 나오므로, 지금은 공통 규칙 하나로 대신한다 —
        // 명중이 쌓이면 단계가 오르고 단계마다 피해가 는다. 모양은 같다.
        private float _repossessLock;
        private int _repossessLockShown = -1;

        private float _stopTimer;

        private const float ChargeSeconds = 0.9f;
        private const float ChargeSpeedMul = 5.5f;

        /// <summary>크러셔 돌진은 두 배로 빠르다(기획 2026-09-02).</summary>
        private const float RamSpeedBoost = 2f;

        /// <summary>
        /// 지금 돌진의 속도 배수. **자가 하나여야 한다** —
        /// 달린 거리(`TickBoss`)와 달릴 시간(`ApplyMoveEffect`)이 같은 값을 봐야
        /// 그려 둔 줄 끝에 정확히 선다. 두 곳에 따로 적으면 줄과 몸이 어긋난다.
        /// </summary>
        private float _chargeSpeedMul = ChargeSpeedMul;

        /// <summary>이번 돌진이 나를 **뚫고 지나가는가**. 아니면 닿는 자리에서 멈춘다.</summary>
        private bool _chargePierce;

        /// <summary>이번 돌진에서 이미 때렸는가. 뚫고 가는 동안 매 프레임 때리면 안 된다.</summary>
        private bool _chargeHitDone;

        /// <summary>닿는 순간 터뜨릴 것. 예고 시점에 정해 둔다 — 그때는 도형이 없다.</summary>
        private string _chargeFx = "slam";
        private float _chargeFxSize = 144f;
        private const int MaxRoomUnits = 14;
        private const float SummonRadius = 200f;

        /// <summary>적 배치 띠 — 필드 높이 대비. 아래쪽은 플레이어 시작 위치를 위해 비운다.</summary>
        private const float EnemyBandTop = 0.06f;
        private const float EnemyBandBottom = 0.44f;
        /// <summary>플레이어 시작 높이. 적 띠 끝과 탐지 거리보다 멀어야 첫 프레임에 안 달려든다.</summary>
        private const float PlayerStartY = 0.88f;

        // ── 방 규격 · 세로 따라가는 카메라 ────────────────────────
        // 정본의 방은 **8.4 × 14 m** 이고 34방 전부 카메라 모드가 `VERTICAL_FOLLOW` 다
        // (보스방만 16 m). 가로는 화면에 다 들어가고 세로가 화면보다 길다.
        //
        // 이걸 한 화면에 눌러 담으면 세로 거리가 0.67배로 찌그러져 사거리·회피 간격이
        // 전부 달라진다. 정본 좌표를 쓰는 의미가 사라지므로, 방을 실제 크기로 두고
        // 카메라가 따라간다.
        //
        // `RoomField` 가 보이는 창(뷰포트)이고 `UnitLayer` 가 방 전체다.
        // 창은 그대로 두고 방을 세로로 밀어 카메라를 흉내낸다.
        // 정본 v3.3 의 방은 폭 24~32 m 다. 세로 화면에 안 들어가서 1/4 로 줄였고,
        // **전 방 같은 폭**으로 통일했다 — 방마다 폭이 다르면 픽셀/미터가 달라져
        // 캐릭터 크기가 방을 넘을 때마다 변한다.
        /// <summary>
        /// 방 폭(미터). 8 → 15 → **10**. 15 로 넓혔더니 방이 화면(720px)보다 넓어져
        /// 가로 스크롤이 생겼고, 오른쪽에 뭐가 있는지 보려면 찾아다녀야 했다.
        /// 세로 스크롤만 남긴다 — **방 폭 = 화면 폭**이라 가로는 한눈에 다 들어온다.
        ///
        /// 왜 하필 10 인가. 화면에 보이는 유닛 수는 미터값과 **무관하다**
        /// (유닛 144px · 화면 720px → 항상 5 기 폭). 미터값이 정하는 것은
        /// 정본의 미터 수치가 픽셀로 얼마나 크게 보이느냐 하나뿐이다.
        ///
        ///     10 m → 72 px/m → 유닛 한 기 = 2.0 m(사람 크기) · 최대 사거리 8.5 m = 화면의 85%
        ///      8 m → 90 px/m → 유닛 한 기 = 1.6 m 이지만 8.5 m 사거리가 화면을 넘는다
        ///     15 m → 48 px/m → 유닛 한 기 = 3.0 m — 근접 사거리(2.8 m)보다 몸이 커진다
        /// </summary>
        private const float RoomMeterWidth = 10f;

        /// <summary>
        /// 화면에 보이는 폭. **방 폭과 같게 유지한다** — 다르게 두면 가로 스크롤이 살아난다.
        /// `WantScroll` 의 x 는 `Min(0, viewW - roomW)` 로 잠기므로 둘이 같으면 항상 0 이다.
        /// </summary>
        private const float ViewMeterWidth = RoomMeterWidth;
        // 방 세로도 **전 방 고정**이다(`RoomImporterV33.RoomHeight` 와 같은 값).
        // 화면에 보이는 높이가 약 11 m 라, 13 m 면 조금만 올라가도 방이 한눈에 들어온다.
        // 예전에는 14~28 m 였고 그러면 방 하나가 두세 화면이라 뭐가 있는지 모르고 올라갔다.
        /// <summary>
        /// 보스가 서는 자리 — 방 높이의 몇 배만큼 위에서 내려온 지점인가.
        ///
        /// ⚠ 예전 값 0.14 는 방 꼭대기였다. 크러셔의 패턴은 반경 1.2~2.5 m 인데
        ///   나(0.88)와 9.6 m 떨어져 있어 **바닥 도형이 나한테 닿을 수가 없었다.**
        ///   화면에서는 "보스가 위에서 혼자 뭘 한다" 로 보인다.
        ///   위에서 1/3(= 바닥에서 2/3 높이)이면 나와 7.1 m 다.
        /// </summary>
        private const float BossStandY = 1f / 3f;

        /// <summary>보스 그림 배율. 256 캔버스가 방 폭의 1/3 이라 조금 줄인다.</summary>
        private const float BossScale = 0.9f;

        /// <summary>
        /// 쫓아오는 보스가 멈춰 서는 거리(m).
        ///
        /// ⚠ 4 m 였다가 3 m 로 내렸다. 거리 조건의 기준값(4 m)에 맞췄더니
        ///   **조건만 맞고 도형은 안 닿았다** — 가디언 「똬리」의 위험한 띠가
        ///   2.6~3.5 m 라, 4 m 에 선 나는 바깥 0.5 m 밖에 서 있다.
        ///   패턴은 뜨는데 절대 안 맞으니 화면에서는 "저건 왜 쓰는 거냐" 가 된다.
        ///   **조건이 맞는 거리가 아니라 도형이 닿는 거리**까지 와야 한다.
        /// </summary>
        private const float ChaseStopMeters = 3f;

        // ⚠ 2026-09-11 에 13 m → **16 m** (기획 — 배치를 더 넓게). 60방 데이터(`RoomTable._height`)도 16 이고,
        //   바닥 그림도 720 × 1152 로 이어 그려 다시 받았다. 여기 값은 정본 방이 없을 때의 기본값이다.
        private const float RoomMeterHeight = 16f;
        private const float BossRoomMeterHeight = 16f;

        /// <summary>
        /// 벽 보스(파이썬) 아레나 높이. **13 m 그대로** 둔다 — 카메라를 세우는 방이라(`CameraLocked`)
        /// 16:9 화면(창 1050)에 방 전체가 들어와야 하고, 바닥 그림도 720 × 936 그대로 쓴다.
        /// </summary>
        private const float WallArenaMeterHeight = 13f;

        // ⚠ 카메라는 **즉시** 따라간다. 보간을 넣지 않는다.
        //
        //   예전에는 지수 보간(시정수 125 ms)으로 부드럽게 붙였는데, 그러면 움직일 때마다
        //   화면이 한 박자 늦게 따라오고 멈추면 뒤늦게 밀려와 **흔들리는 것처럼** 보인다.
        //   조이스틱으로 직접 모는 게임에서 이 지연은 부드러움이 아니라 무게추가 된다.
        //
        //   "즉시 붙이면 걸음마다 튄다"고 걱정해 넣었던 것인데, 실제 원인은 보간이 아니라
        //   `ApplyScroll` 의 픽셀 반올림이다. 그쪽은 도트를 또렷하게 두기 위해 필요하고
        //   1 px 이라 눈에 띄지 않는다.

        private RectTransform _floor;
        private Image _floorImage;
        private Sprite _defaultFloor;
        private float _pxPerMeter = 1f;
        private Vector2 _roomSize;      // 픽셀
        /// <summary>창을 방 위에서 얼마나 밀어 놓았는가. x 는 가로, y 는 세로다.</summary>
        private Vector2 _scroll;
        /// <summary>밀어내기 속도 — 이동 속도 대비. 너무 크면 서로 튕겨 나간다.</summary>
        private const float SeparationSpeedRatio = 0.55f;

        private BossTable _bossTable;
        private readonly BossBrain _brain = new();

        /// <summary>이 방에 서는 보스. 바닥·방 크기·무대가 모두 이 하나를 본다.</summary>
        private BossEntry _roomBoss;
        private Unit _boss;
        private float _telegraphPulse;
        private float _chargeDamageMul = 1f;

        private BuffTable _buffTable;
        private readonly RunBuffs _buffs = new();
        private readonly List<BuffEntry> _offer = new();
        /// <summary>슬롯이 찼을 때 쓰는 제외 목록. 매번 새로 만들지 않으려고 들고 있는다.</summary>
        private readonly System.Random _rng = new();
        private bool _awaitingBuff;

        /// <summary>런 레벨. 적을 잡아 EXP 를 모으고, 차면 버프 3택1 이 열린다 (기획서 A 5-2).</summary>
        private int _level = 1;
        private int _exp;

        /// <summary>빙의할 대상이 하나도 없는 상태가 이어진 시간 (기획서 A 8-3).</summary>
        private float _emergencyWait;
        private RoomKind _roomKind = RoomKind.Normal;

        /// <summary>이 런에 쌓인 버프. 스테이지를 나가면 사라진다.</summary>
        public RunBuffs Buffs => _buffs;
        public bool IsAwaitingBuff => _awaitingBuff;

        /// <summary>이 카드의 지금 레벨. 안 가졌으면 0. (UI 가 "Lv.2 → Lv.3" 를 그린다)</summary>
        public int CardLevel(string cardKey) => _buffs.LevelOf(cardKey);

        public Vector2 MoveInput { get; set; }

        /// <summary>지금 사격 중인가. 멈춰서 사거리 안에 적이 있을 때만 true (궁수의 전설 규칙).</summary>
        public bool IsFiring { get; private set; }
        public float SkillCooldownRatio => _config == null ? 0f
            : Mathf.Clamp01(_skillCooldown / SkillCooldownOf(_host?.Profile));

        /// <summary>
        /// 지금 탄 몸의 액티브 스킬 쿨(초).
        ///
        /// **우선순위를 정하는 자리는 여기 하나뿐이다** — 호스트 표에 값이 있으면 그것,
        /// 없으면 `GameConfig` 기본값. 사거리·간격에서 `_canonHost*` 가 조용히 먼저 먹어
        /// 배율 칸이 통째로 죽어 있던 일을 되풀이하지 않는다(`AVSR_JobClasses.md` §6).
        /// </summary>
        private float SkillCooldownOf(HostEntry e)
        {
            float fallback = _config != null ? _config.ActiveSkillCooldownSeconds : 14f;
            if (fallback <= 0f) fallback = 14f;
            return e == null ? fallback : e.ActiveSkillCooldown(fallback);
        }
        public bool CanPossess => _host == null && _possessTarget != null;
        public bool IsRunning => _running;

        public async UniTask BootAsync(RectTransform field, RectTransform unitLayer)
        {
            _field = field;
            _unitLayer = unitLayer;

            // 방이 창보다 크므로 잘라 내야 한다. 없으면 화면 밖 적이 상단 HUD 위에 그려진다.
            if (_field.GetComponent<RectMask2D>() == null) _field.gameObject.AddComponent<RectMask2D>();

            // 바닥도 방의 일부다. 바닥만 제자리에 두면 카메라가 움직이는 것이 아니라
            // **물건들이 미끄러지는 것**으로 보인다 — 기준이 없으면 이동을 읽을 수 없다.
            var floorT = _field.Find("RoomFloor") as RectTransform;
            if (floorT != null)
            {
                _floor = floorT;
                _floorImage = _floor.GetComponent<Image>();
                // 세로로 길어진 방을 늘려 채우면 바닥 무늬가 뭉개진다. 타일로 반복한다.
                if (_floorImage != null)
                {
                    _floorImage.type = Image.Type.Tiled;
                    _defaultFloor = _floorImage.sprite;
                }
            }
            // 화면 폭이 8 m 를 담는다. 방이 15 m 라 나머지는 카메라가 따라가며 보여 준다.
            _pxPerMeter = _field.rect.width / ViewMeterWidth;
            // ⚠ **방 크기를 정하기 전에** 필드 윗변 자리(상단 HUD 몫)를 떠 둔다.
            //   `SetRoomSize` 가 곧바로 창 높이를 맞추는데 그때 이 값이 있어야 한다.
            CaptureFieldTop();
            SetRoomSize(RoomMeterHeight);
            _bus = CoreModule.Get<IEventBus>();
            CoreModule.TryGet(out _player);
            // 가림막. 없으면(모듈 미등록) 그냥 예전처럼 조용히 로드한다.
            CoreModule.TryGet(out _loading);

            var res = CoreModule.Get<IResourceManager>();
            await LoadPanelAtlasAsync(res);
            try { _atlas = await res.LoadAsync<SpriteAtlas>(AtlasAddress); }
            catch (Exception e) { Debug.LogError($"[Battle] 아틀라스 로드 실패 — {e.Message}"); }
            CachePossessMarkSprites();
            try { _config = await res.LoadAsync<GameConfig>("TableData/GameConfig"); }
            catch (Exception e) { Debug.LogError($"[Battle] GameConfig 로드 실패 — {e.Message}"); }
            if (_config == null) return;

            // ⚠ **`_config` 을 읽은 뒤에** 넣는다. `CachePossessMarkSprites` 안에 두었더니
            //   그 함수가 표보다 먼저 도는 자리라 부팅 때마다 NullReference 로 터졌다.
            Unit.SetShieldRule(_config.ShieldHoldSeconds, _config.ShieldDecayPerSecond);
            Unit.SetAttackSpeed(_config.AttackSpeedMul);
            EnsureSandboxTag();   // Sandbox — 지울 때 이 줄도 함께

            // ⚠ 여섯 표를 **한꺼번에** 띄운다. 순서대로 await 하면 로드 시간이 그대로 더해진다 —
            //   서로 기다릴 이유가 없는 것들이다(`GameConfig` 만 앞에서 먼저 확인한다).
            await UniTask.WhenAll(
                LoadTableAsync<RoomTable>(res, "TableData/RoomTable",
                    t => _rooms = t, "RoomTable 없음 — 절차적 생성으로 간다", warnOnly: true),
                LoadTableAsync<BossTable>(res, "TableData/BossTable",
                    t => _bossTable = t, "BossTable 로드 실패"),
                LoadTableAsync<BuffTable>(res, "TableData/BuffTable",
                    t => _buffTable = t, "BuffTable 로드 실패"),
                LoadTableAsync<EventTable>(res, "TableData/EventTable",
                    t => _eventTable = t, "EventTable 없음 — 이벤트 방은 그냥 지나간다", warnOnly: true),
                LoadTableAsync<ShopTable>(res, "TableData/ShopTable",
                    t => _shopTable = t, "ShopTable 없음 — 상점 방은 그냥 지나간다", warnOnly: true));
            // 스폰은 동기 코드다. 테이블이 다 올라온 뒤에 이 런이 쓸 캐릭터를 먼저 올린다.
            await LoadUnitAtlasesAsync(res, RunUnitKeys());
            _buffs.Clear();   // 버프는 런 한정 — 스테이지 진입마다 초기화한다
            _eventsUsed.Clear();
            _runGold = 0;     // 판 골드도 런 한정이다
            _possessReachMul = 0f;
            _shopDiscount = 0;
            _bossShieldBreak = false;

            // 장판은 바닥에 깔린다 — 유닛보다 **아래**다. 위에 그리면 캐릭터가
            // 장판에 잠겨 어디 서 있는지 안 보인다.
            var fieldGo = new GameObject("FieldLayer", typeof(RectTransform));
            fieldGo.transform.SetParent(_unitLayer.parent, false);
            _fieldLayer = (RectTransform)fieldGo.transform;
            _fieldLayer.anchorMin = _unitLayer.anchorMin;
            _fieldLayer.anchorMax = _unitLayer.anchorMax;
            _fieldLayer.pivot = _unitLayer.pivot;
            _fieldLayer.anchoredPosition = _unitLayer.anchoredPosition;
            _fieldLayer.sizeDelta = _unitLayer.sizeDelta;
            _fieldLayer.SetSiblingIndex(_unitLayer.GetSiblingIndex());

            // 탄은 유닛보다 위에 그린다 — 유닛 뒤로 숨으면 피격 판단이 안 보인다
            var shotGo = new GameObject("ShotLayer", typeof(RectTransform));
            shotGo.transform.SetParent(_unitLayer.parent, false);
            _shotLayer = (RectTransform)shotGo.transform;
            _shotLayer.anchorMin = _unitLayer.anchorMin;
            _shotLayer.anchorMax = _unitLayer.anchorMax;
            _shotLayer.pivot = _unitLayer.pivot;
            _shotLayer.anchoredPosition = _unitLayer.anchoredPosition;
            _shotLayer.sizeDelta = _unitLayer.sizeDelta;

            // 피해 수치는 탄보다 위에 그린다 — 탄에 가리면 읽을 수 없다.
            var textGo = new GameObject("DamageTextLayer", typeof(RectTransform));
            textGo.transform.SetParent(_unitLayer.parent, false);
            _textLayer = (RectTransform)textGo.transform;
            _textLayer.anchorMin = _unitLayer.anchorMin;
            _textLayer.anchorMax = _unitLayer.anchorMax;
            _textLayer.pivot = _unitLayer.pivot;
            _textLayer.anchoredPosition = _unitLayer.anchoredPosition;
            _textLayer.sizeDelta = _unitLayer.sizeDelta;

            SpawnGhost();
            EnterStartHost();

            // ⚠ **한 판은 언제나 1챕터 1스테이지에서 시작한다**(기획 2026-09-08).
            //
            //   예전에는 저장된 진행도가 가리키는 챕터의 첫 방에서 시작했다.
            //   그런데 챕터를 올리는 자리가 아래 `EnterRoom` 하나뿐이라(방을
            //   밟으면 올라간다) 한 번 올라간 진행도는 안 내려온다 — 시험 삼아
            //   3챕터 방을 열어 본 것만으로 그 뒤 모든 판이 3챕터에서 시작했다.
            //
            //   진행도를 여기서 되돌린다. **격파 기록(`ClearedChapter`)은 안 건드린다** —
            //   호스트 해금은 그것을 먼저 보므로 깨서 얻은 몸은 잠기지 않는다.
            //   챕터를 쓰는 자리가 열다섯 곳인데(적 성장·보스 선택·아틀라스 …)
            //   전부 이 값을 보므로, 여기 하나만 맞춰 두면 나머지가 따라온다.
            _runChapter = 1;
            _runStage = 1;
            _maxHpDebt = 0;   // 계약은 판 한정이다
            ClearCardRuntime();
            ClearShopExtras();

            _canonRoomId = FirstCanonRoom;
            EnterRoom(0);

            // 첫 방까지 다 세운 **뒤에** 걷는다. 씬이 올라온 순간 걷으면
            // 빈 바닥이 잠깐 보였다가 캐릭터가 나타난다.
            //
            // 걷힐 때까지 **기다렸다가** 전투를 연다. 가림막은 막대를 100%까지 채우느라
            // 잠깐 더 떠 있는데, 그동안 `_running` 이 켜져 있으면 적이 가림막 뒤에서
            // 달려들고 Ghost HP 도 깎인다 — 보지도 못한 전투가 먼저 시작된다.
            // ⚠ 방을 **세우는 것**과 방이 **그려지는 것**은 다르다.
            //   `EnterRoom` 은 오브젝트를 만들 뿐이고, 실제 그림은 다음 렌더에서 나온다.
            //   그 전에 가림막을 걷으면 검은 화면이 한 박자 보인다.
            //   두 프레임을 넘겨 레이아웃·캔버스가 한 번 돌게 한 뒤에 걷는다.
            //   그리고 **바닥 그림이 실제로 걸릴 때까지** 기다린다. 바닥은 따로 읽어 오므로
            //   이걸 안 기다리면 방이 반만 그려진 화면이 잠깐 드러난다.
            //   못 읽는 바닥(아직 안 온 그림)에 매달려 멎지 않도록 2 초에서 끊는다.
            await UniTask.WhenAny(
                UniTask.WaitUntil(() => _floorPending == 0,
                                  cancellationToken: this.GetCancellationTokenOnDestroy()),
                UniTask.Delay(2000, cancellationToken: this.GetCancellationTokenOnDestroy()));

            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());

            if (_loading != null)
            {
                await _loading.HideAsync();
                // ⚠ `HideAsync` 는 **곧바로 돌아올 수 있다.** 프레임워크가 씬이 올라온 순간 이미 걷기 시작해서,
                //   두 번째 호출은 「이미 걷는 중」이라 기다리지 않는다 — 로딩창이 떠 있는데 판이 돌았다
                //   (기획 2026-09-15). 실제로 사라질 때까지 기다린다.
                await UniTask.WaitUntil(() => !_loading.IsVisible,
                                        cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            _bootDone = true;
        }

        private bool _bootDone;

        /// <summary>
        /// 판을 연다. **화면의 덮개까지 다 걷힌 뒤에** `InGameMainUI` 가 부른다.
        /// 여기서 여는 이유: 전투 준비 뒤에도 카드 · 화면 아틀라스를 읽느라 검은 덮개가 더 떠 있는데,
        /// 그동안 판이 돌면 적이 덮개 뒤에서 달려들고 Ghost HP 가 깎인다.
        /// </summary>
        public void BeginBattle()
        {
            if (_bootDone) _running = true;
        }

        // ─────────────────────────────────────────────────────────
        private void SpawnGhost()
        {
            _ghostHp = GhostHpMax;
            _repossessLock = 0f;      // 런은 언제나 교체 가능한 상태로 시작한다
            _eliteRoomsCleared = 0;
            _repossessLockShown = -1;
            _ghost = NewUnit("Ghost");
            _ghost.Setup(UnitSide.Player, "ghost", "GHOST", UnitGet("ghost"),
                         GhostHpMax, 0, _config.GhostMoveSpeed, 0f, 1f,
                         UnitBox(72f, 90f));
            _ghost.Position = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * PlayerStartY);
            PublishHp();
        }

        /// <summary>
        /// 로비에서 고른 호스트를 입고 시작한다.
        /// 캐릭터를 골라 놓고 유령으로 떨어지면 그 선택이 화면에 나타나지 않는다.
        ///
        /// 빼앗은 몸(빙의 70%)과 달리 **체력은 가득** 채운다 — 훔친 몸이 아니라
        /// 데려온 몸이다. 몸을 잃으면 그때부터 유령이 되고, 기존 흐름(빙의·긴급 투입)이
        /// 그대로 이어진다.
        ///
        /// 고른 호스트가 없으면(데이터 미준비 등) 아무것도 하지 않는다 —
        /// 유령으로 시작하던 예전 흐름 그대로다.
        /// </summary>
        private void EnterStartHost()
        {
            var entry = PickPlayerHost();
            if (entry == null)
            {
                // 조용히 유령으로 시작하면 "왜 내 캐릭터가 아니지"의 원인을 못 찾는다.
                Debug.LogWarning("[Battle] 고른 호스트를 못 찾아 유령으로 시작한다 — "
                                 + $"유저데이터 준비={_player != null && _player.IsReady}");
                return;
            }
            EnterHost(entry, entry.HostKey, entry.DisplayName, _ghost.Position, 100);
        }

        // ── 정본 방 ───────────────────────────────────────────────
        // 절차적 생성과 나란히 둔다. `_canonRoomId` 가 가리키는 방이 테이블에 있으면
        // 그 방을 쓰고, 없으면 예전 방식으로 만든다. 34방을 한 번에 갈아 끼우면
        // 어디서 깨졌는지 알 수 없어서, 한 방씩 옮겨 붙인다.
        /// <summary>
        /// 이 챕터의 첫 방. 정본 v3.3 은 챕터마다 `ROOM_CH{n}_001` 로 시작한다.
        /// (예전 34방 체계는 `CH1_N01` 이었다 — ID 가 통째로 바뀌었다.)
        /// </summary>
        /// <summary>
        /// 이번 판이 시작하는 방.
        ///
        /// ⚠ 한때 챕터를 **1~3 으로 잘랐다.** 방이 3챕터뿐이던 시절의 값인데,
        ///   6챕터가 되면서 CH4~6 을 고른 플레이어가 CH3_001 로 떨어졌다 —
        ///   **챕터 4·5·6 의 보스에 도달할 방법이 아예 없었다.**
        /// </summary>
        /// <summary>
        /// 판이 시작하는 방. **언제나 1챕터 1스테이지다**(기획 2026-09-08).
        ///
        /// 진행도를 따라 시작 방을 옮기면, 한 번 올라간 진행도가 안 내려와서
        /// 그 뒤로 앞 챕터를 다시는 못 본다. 시작은 고정하고, 챕터는 방을
        /// 밟아 나가며 오른다(`EnterRoom`).
        /// </summary>
        private string FirstCanonRoom => "ROOM_CH1_001";

        /// <summary>
        /// 이 판이 지금 몇 챕터를 걷고 있는가. **저장에 안 쓴다.**
        ///
        /// 한 판은 1챕터에서 시작해 60방을 이어서 간다(기획 2026-09-08).
        /// 걷는 동안의 챕터는 판이 끝나면 버릴 값이라 저장에 손대지 않는다 —
        /// 예전에는 방을 밟을 때마다 `SetProgress` 로 저장에 써 넣었고,
        /// 그래서 3챕터 방을 한 번 열어 본 것만으로 그 뒤 모든 판이
        /// 3챕터에서 시작했다. 올라가는 길만 있고 내려오는 길이 없었다.
        ///
        /// 판이 끝날 때 **한 번만** 저장에 옮긴다(<see cref="Finish"/>).
        /// </summary>
        private int _runChapter = 1;

        /// <summary>이 판에서 밟은 방 수. <see cref="_runChapter"/> 와 같이 저장에 안 쓴다.</summary>
        private int _runStage = 1;

        /// <summary>챕터 수. 정본 6챕터.</summary>
        private const int ChapterCount = 6;

        private RoomTable _rooms;
        private EventTable _eventTable;
        private ShopTable _shopTable;
        private RoomEntry _canonRoom;
        private string _canonRoomId;
        /// <summary>이번 런에서 비운 정예 방 수. 정본 R_ELITE 가 여기에 붙는다.</summary>
        private int _eliteRoomsCleared;

        private readonly HashSet<string> _missingActors = new();

        /// <summary>정본 좌표(미터, 좌하단 기준) → 우리 좌표(픽셀, 좌상단 기준 · 아래가 음수).</summary>
        private Vector2 ToPixels(Vector2 meters)
            => new(meters.x * _pxPerMeter, -(_roomSize.y - meters.y * _pxPerMeter));

        /// <summary>
        /// 정본 v3.3 방 타입 → 우리 방 종류.
        /// SHOP·EVENT 는 아직 화면이 없다. 적이 없는 방이라는 점은 REST 와 같으므로
        /// 그 자리를 빌린다 — 그래야 지나갈 수 있다. 화면이 생기면 갈라낸다.
        /// </summary>
        /// <summary>
        /// 004 가 회복 제단으로 갈리는 체력 문턱. 고스트 최대 체력의 몫이다.
        ///
        /// ⚠ 004 **바로 다음이 중간 보스**이고 그 대장은 빙의가 안 된다.
        ///   체력이 바닥인 채로 대가형 이벤트만 뜨면 그 판은 거기서 끝난다 —
        ///   고를 것이 남아 있어야 선택이지, 죽는 길 하나뿐이면 그건 벽이다.
        ///
        /// ⚠ 한때 「그 위는 회복/이벤트 반반 무작위」로 뒀다가 되돌렸다(2026-09-08 확정).
        ///   무작위로 두면 **회복이 두 번 연달아 나오는 판**이 생기고, 그러면 이벤트
        ///   25종을 돌리려고 004 를 이벤트 방으로 만든 뜻이 흐려진다.
        ///   대가형(악마 계약)은 별도 방이 아니라 **이벤트 풀 안**에 들어간다.
        /// </summary>
        private const float RestHpRatio = 0.30f;

        /// <summary>
        /// 004 방이 무엇으로 열리는가 (기획 2026-09-15).
        ///
        ///   호스트 체력 ≤ 30%  →  천사의 제단 (`RoomKind.Rest`)
        ///   그 외               →  천사 · 악마 **반반**
        ///
        /// ⚠ 예전에는 **고스트 HP** 만 봤다. 몸에 타 있는 동안 고스트 HP 는 거의 가득이라
        ///   천사의 제단이 사실상 안 나왔다. 이제 몸의 체력을 본다 — 몸이 없으면 고스트 HP 로 잰다.
        /// </summary>
        private RoomKind KindOfCanon(RoomEntry room)
        {
            if (room.IsBoss) return RoomKind.Boss;
            var kind = (room.Type ?? string.Empty).ToUpperInvariant() switch
            {
                "BOSS"  => RoomKind.Boss,
                "ELITE" => RoomKind.Elite,
                "EVENT" => RoomKind.Event,
                "SHOP" => RoomKind.Shop,
                "REST" => RoomKind.Rest,
                _       => RoomKind.Normal,
            };
            if (kind == RoomKind.Event)
            {
                float ratio = _host != null && _host.HpMax > 0 ? (float)_host.Hp / _host.HpMax
                            : GhostHpMax > 0 ? (float)_ghostHp / GhostHpMax : 1f;
                if (ratio <= RestHpRatio || UnityEngine.Random.value < 0.5f) return RoomKind.Rest;
            }
            return kind;
        }

        /// <summary>
        /// 정본 EnemyID(E001 …) 로 프로필을 찾는다.
        /// 아직 그림이 없는 배우는 대역을 세운다 — null 로 두면 **보이지 않는 적**이 되어
        /// 방이 클리어되지 않는다. 무엇이 대역인지는 한 번만 알린다.
        /// </summary>
        private HostEntry ActorProfile(string actorId, IReadOnlyList<HostEntry> hosts)
        {
            if (string.IsNullOrEmpty(actorId)) return hosts.Count > 0 ? hosts[0] : null;

            // 정본 v3.3 스폰은 **우리 슬러그**를 들고 있다(임포터가 EN_xx 를 풀어 넣었다).
            // 예전 34방 데이터는 정본 EnemyID(E001…) 였으므로 둘 다 본다.
            for (int i = 0; i < hosts.Count; i++)
                if (hosts[i].HostKey == actorId) return hosts[i];
            for (int i = 0; i < hosts.Count; i++)
                if (hosts[i].EnemyId == actorId) return hosts[i];

            // ⚠ 잡몹은 `HostTable` 에 없다 — `CreateTrash` 로 코드가 만든다.
            //   그러니 여기서 못 찾는 것이 **정상**이고, 경고할 일이 아니다.
            //   예전에는 그냥 경고를 뱉어서 "해골·박쥐·폐품사수의 그림이 아직 없다" 는
            //   줄이 매 판 찍혔고, 실제로는 셋 다 그림이 멀쩡히 붙어 있는데도
            //   그 로그만 보고 리소스를 다시 발주할 뻔했다.
            if (TrashByKey(actorId, 1) != null || TrashByKey(actorId, 2) != null
                || TrashByKey(actorId, 3) != null)
                return hosts.Count > 0 ? hosts[0] : null;

            // 숨긴 몸(아마존 정예 — 기획 2026-09-15)은 목록에서 빠져 있다.
            // 방 데이터가 그 자리를 찍었으면 아마존으로 세운다 — 비워 두면 방 구성이 달라진다.
            if (PlayerDataService.IsHiddenHost(actorId))
            {
                for (int i = 0; i < hosts.Count; i++)
                    if (hosts[i].HostKey == "amazon") return hosts[i];
                return hosts.Count > 0 ? hosts[0] : null;
            }

            if (_missingActors.Add(actorId))
                Debug.LogWarning($"[Battle] {actorId} 의 그림이 아직 없다 — 대역으로 세운다");
            return hosts.Count > 0 ? hosts[0] : null;
        }

        /// <summary>
        /// 방에 설 적을 **한꺼번에** 세운다. 들어서는 순간 전부 서 있다.
        ///
        /// ⚠ 증원(웨이브)은 없다. 두 번 만들었다가 두 번 다 지웠다 —
        ///   방을 비운 순간 허공에서 적이 나오면, 빙의할 몸이 방금 다 죽은 상태라
        ///   유령으로 새 무리를 맞게 되어 대응할 방법이 없다.
        /// </summary>
        private void SpawnRoom(RoomEntry room)
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return;

            var spawns = room.Spawns;

            // ── 이 웨이브에서 어느 자리가 몸인가 ─────────────────────
            //
            // 정본은 두 방향으로 극단이다. 33 개 방 중 15 개는 숙주를 **한 자리도**
            // 안 찍었고, 반대로 `ROOM_CH1_001` 은 네 자리가 **전부** 숙주다.
            // 그 방이 곧 "다 엘리트라 의미가 없는" 화면이다 — 전원이 빼앗을 수 있는
            // 몸이면 빼앗는 것이 선택이 아니라 기본값이 된다.
            //
            // 그래서 아래 두 값 사이로 조인다. 부족하면 승격하고, 넘치면 잡몹으로 돌린다.
            _hostSlots.Clear();
            // ⚠ 중간 보스 방은 **부하가 전부 몸**이다.
            //   정본: "대장은 못 뺏고 부하는 뺏을 수 있으니, 부하를 빼앗아 대장을 치는
            //   전투가 된다." 여기서 한 자리라도 잡몹으로 돌리면 그 구조가 무너진다.
            if (room.IsMidBoss)
                for (int i = 0; i < spawns.Count; i++) _hostSlots.Add(i);
            else
                for (int i = 0; i < spawns.Count; i++)
                    if (spawns[i].IsPossessionTarget) _hostSlots.Add(i);

            if (_hostSlots.Count == 0)
            {
                // 승격 — 정본이 몸을 안 남긴 방. `PossessPriority` 가 높은 자리를 고른다.
                int bestPri = int.MinValue, at = -1;
                for (int i = 0; i < spawns.Count; i++)
                {
                    var cand = ActorProfile(spawns[i].ActorId, hosts);
                    if (cand == null ||
                        cand.PossessKind == Game.Character.PossessKind.NotPossessable) continue;
                    if (cand.PossessPriority > bestPri) { bestPri = cand.PossessPriority; at = i; }
                }
                if (at >= 0) _hostSlots.Add(at);
            }
            else while (!room.IsMidBoss && _hostSlots.Count > MaxHostsPerWave)
            {
                // 강등 — 우선순위가 가장 낮은 자리부터 잡몹으로 돌린다.
                int worst = 0, worstPri = int.MaxValue;
                for (int k = 0; k < _hostSlots.Count; k++)
                {
                    var cand = ActorProfile(spawns[_hostSlots[k]].ActorId, hosts);
                    int pri = cand != null ? cand.PossessPriority : int.MinValue;
                    if (pri < worstPri) { worstPri = pri; worst = k; }
                }
                _hostSlots.RemoveAt(worst);
            }

            // 잡몹 회전을 방마다 다른 자리에서 시작한다.
            //
            // 늘 0 에서 시작하면 목록의 **뒤쪽이 작은 방에서 영영 안 나온다** —
            // 폐품 사수가 목록 셋째라, 잡몹이 둘뿐인 방에서는 한 번도 안 섰다.
            // 첫 방만은 0 으로 고정한다. 근접 둘로 기본을 먼저 가르치는 자리다.
            int roomNo = RoomNumberOf(_canonRoom != null ? _canonRoom.RoomId : null);
            int trashSeq = roomNo <= 1 ? 0 : roomNo;
            int spawned = 0;
            for (int i = 0; i < spawns.Count; i++)
            {
                var s = spawns[i];

                // 테스트 모드 — 방마다 **한 기만** 세운다. 배치·진행을 끝까지 훑을 때 쓴다.
                if (OneEnemyPerRoom && spawned >= 1) continue;
                spawned++;

                // 엘리트는 **배정표가 찍는다.** 정본 별도 ID(EL01…)를 보던 시절과 다르다 —
                // 이제 자리도 배우도 레이아웃이 정하므로 ID 로는 구별할 수 없다.
                bool elite = s.Elite;

                // 이 자리가 몸인가, 잡몹인가.
                // 엘리트는 그 자체가 관문이라 잡몹으로 바꾸지 않는다.
                bool isHost = elite || _hostSlots.Contains(i);
                int chapterNo = _runChapter;
                // 방 데이터가 이 자리의 잡몹을 지정했으면 그대로 세운다(손으로 짠 레이아웃).
                // 안 지정했으면 예전대로 목록을 돌려 쓴다 — 절차 생성 방이 그렇다.
                var e = isHost ? ActorProfile(s.ActorId, hosts)
                               : (TrashForSlot(s.ActorId, chapterNo, trashSeq++)
                                  ?? TrashAt(trashSeq++, chapterNo));
                if (e == null) continue;

                var u = NewUnit(isHost ? $"Enemy_{s.ActorId}_{s.SpawnId}"
                                       : $"Trash_{e.HostKey}_{s.SpawnId}");
                NoteMetHost(e);   // 상점이 파는 목록은 이 판에서 만난 몸뿐이다
                u.Setup(UnitSide.Enemy, e.HostKey, e.DisplayName, TrashSprite(e),
                        // 정본 엘리트(EL##)는 자기 행에 이미 센 체력이 적혀 있다.
                        // 거기에 배율까지 곱하면 두 번 세진다 — 정본이 있으면 배율은 안 쓴다.
                        Mathf.RoundToInt(EnemyHpOf(e) * (elite && !e.HasCanon ? _config.EliteHpMul : 1f)
                                         * SpawnHpMul(elite)),
                        Mathf.RoundToInt(EnemyAtkOf(e) * (elite && !e.HasCanon ? _config.EliteAtkMul : 1f)
                                         * SpawnAtkMul(elite)),
                        EnemySpeedOf(e),
                        EnemyRangeOf(e),
                        EnemyIntervalOf(e, elite) / EnemyHandSpeedMul,
                        // 캔버스가 한 등급 큰 것들(128×128)은 상자도 커야 한다.
                        // 잡몹 상자(84)에 넣으면 캔버스 여백까지 줄어 오히려 작아 보인다.
                        // 엘리트뿐 아니라 **집행자**도 128 캔버스다.
                        elite || e.HostKey == TrashEnforcerKey
                            ? UnitBox(128f, 128f) : UnitBox(84f, 78f),
                        isBoss: false, profile: e);
                u.Position = ToPixels(s.At);
                ClearOfCover(u);
                // 정본이 `POSSESSION_TARGET` 으로 찍은 자리(와 승격한 한 자리)만
                // 빼앗을 수 있는 몸이다. 나머지는 잡몹 — 두들겨도 경직이 쌓이지 않는다.
                if (isHost && !elite) { u.MarkAsHostBody(); u.MarkAsNextBody(); }
                if (elite) u.MarkAsElite();
                u.PossessPriority = e.PossessPriority;
                u.PossessRange = 0f;
                u.SetState(EnemyState.Idle);
                // 풀에서 돌려 쓰므로 앞 방의 도약 단계가 남아 있을 수 있다.
                u.ResetPattern();
                ApplyFacingSprites(u, e.SpriteKey);
                _enemies.Add(u);
            }
            EnsureHostBodies();
            SpawnMidBoss(room, hosts);

            // 방마다 무엇이 섰는지 한 줄로 남긴다. "적용이 안 된 것 같다" 는 말을
            // 추측으로 되받지 않으려면, 화면 대신 콘솔이 답하게 해야 한다.
            int nHost = 0, nMelee = 0, nRanged = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var u = _enemies[i];
                if (u == null || !u.IsAlive) continue;
                if (u.IsHostBody) nHost++;
                else if (IsRangedTrashKey(u.Key)) nRanged++;
                else nMelee++;
            }
            Debug.Log($"[방] {room.RoomId} — 숙주 {nHost} · 근접 {nMelee} · 원거리 {nRanged}");
        }

        /// <summary>
        /// 방에 빼앗을 수 있는 몸이 한 기도 없으면 하나를 승격한다.
        ///
        /// 정본 `ROOM_SPAWN` 은 33 개 방 중 **15 개에 `POSSESSION_TARGET` 을 찍지 않았다.**
        /// 그 방에 몸 없이 들어가면 아무것도 할 수 없이 죽는다 — 대응할 방법이 없는
        /// 죽음은 난이도가 아니라 고장이다. 그래서 최소 1 기는 보장한다.
        ///
        /// 고르는 기준은 정본의 `PossessPriority` 다. 정본이 "이 방에서는 이놈"이라고
        /// 말해 두지 않았을 뿐, 어떤 놈이 몸으로 쓸 만한지는 이미 적어 두었다.
        /// </summary>
        private void EnsureHostBodies()
        {
            int have = 0, best = -1;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsBoss) continue;
                if (e.Side != UnitSide.Enemy) continue;
                if (e.IsHostBody) { have++; continue; }
                if (e.Profile != null &&
                    e.Profile.PossessKind == Game.Character.PossessKind.NotPossessable) continue;
                if (e.PossessPriority > bestScore) { bestScore = e.PossessPriority; best = i; }
            }
            if (have == 0 && best >= 0) { _enemies[best].MarkAsHostBody(); _enemies[best].MarkAsNextBody(); }
        }

        /// <summary>
        /// 다음 웨이브를 부른다. 앞 웨이브를 다 비우면 잠깐 뜸을 들인 뒤 나온다 —
        /// 비우자마자 곧바로 쏟아지면 방을 정리했다는 감각이 사라진다.
        /// 아직 남은 웨이브가 있으면 true(= 방이 아직 안 끝났다).
        /// </summary>
        // ── 지형지물 ──────────────────────────────────────────────
        // 이게 없으면 `Pillar`·`Hazard Lane` 같은 방 이름이 이름값을 못 한다.
        // 방마다 다른 점이 적 배치뿐이게 되어 34방이 다 같은 방으로 느껴진다.

        private sealed class Obstacle
        {
            // ⚠ 판정이 **둘**이다. 하나로 쓰면 둘 중 하나가 반드시 틀린다.
            //
            //   Bounds     — **몸이 부딪히는** 상자. 그림보다 작을 수 있다.
            //                블록은 세로 0.7 칸이라 한 칸짜리 통로를 걸어 지나갈 수 있다.
            //   ShotBounds — **탄이 막히는** 상자. 언제나 그림 발자국 그대로다.
            //                여기까지 줄이면 눈에는 벽인데 탄이 통과해 버린다.
            public Rect Bounds;          // 픽셀. 중심이 아니라 좌상단 기준(우리 좌표계)
            public Rect ShotBounds;      // 그림 발자국 전체 — 탄 차폐·스폰 밀어내기용
            public bool BlocksMove;
            public bool BlocksShot;
            public bool BlocksEnemyShot;

            // ── 움직이거나 상태가 바뀌는 것들 ──────────────────
            public string Kind;          // CRATE · TIMED_SPIKE · ROTATING_BLADE · SWING_HAMMER
            public Image Img;            // 매 프레임 그림을 갈아 끼운다
            public Sprite[] Frames;      // 애니메이션 장면
            public float Phase;          // 방마다·물건마다 어긋나게 시작한다
            public Rect Home;            // 움직이는 것의 원래 자리(왕복·회전의 기준)
            public int Hp;               // 부술 수 있는 것만 0 보다 크다
            public RectTransform View2;  // 축·사슬처럼 따라다니는 두 번째 그림
            public bool IsHazard;
            /// <summary>지금 아픈 상태인가. 가시가 내려가 있는 동안은 false 다.</summary>
            public bool HazardOn = true;
            public int Damage;
            public float Tick;
            public GameObject View;
        }

        private readonly List<Obstacle> _obstacles = new();
        private readonly Dictionary<Unit, float> _hazardTimer = new();

        /// <summary>
        /// 그림이 발자국보다 위로 더 솟는 높이(픽셀).
        ///
        /// 쿼터뷰라 기둥은 바닥에 찍힌 넓이보다 위로 훨씬 높다. 그림을 충돌 사각형에
        /// 딱 맞추면 기둥이 납작한 타일이 되어 "가릴 수 있는 것"으로 안 보인다.
        /// 그림 캔버스는 `발자국 + 솟음` 이고, 캔버스 아래쪽이 발자국과 맞물린다.
        ///
        /// 해저드는 0이다. 불길이 사각형 밖으로 나가면 어디까지가 아픈 자리인지 흐려진다.
        /// </summary>
        private static readonly Dictionary<string, float> ObstacleRise = new()
        {
            // ⚠ 아래 넷은 **시안을 재서** 나온 값이다(`_exchange/out/30_tile/tile_concept.png`).
            //   시안 바닥 타일 32.6 px 를 자로 삼아 오브젝트 높이를 재고 우리 72 px 로 환산했다.
            { "PILLAR",         94f },   // 부서진 석조 기둥 — 그림 72×166
            { "DIVIDER",        70f },
            { "RICOCHET_WALL",  90f },
            { "BARRICADE",      30f },   // 파이프 난간 — 그림 216×102
            { "LOW_COVER",      30f },   // 파이프 다발 — 그림 216×102
            { "HAZARD",          0f },
            // 격자 블록. 발자국 1×1 m(72×72 px) 위로 한 뼘 솟는다 —
            // 납작하면 바닥 무늬로 보이고, 너무 높으면 뒤가 안 보인다.
            { "BLOCK",          30f },   // 72×72 발자국 + 윗면 30 = 그림 72×102
            // 새로 들어온 넷. 바닥에 눕는 것(가시)은 0, 서 있는 것은 캔버스에서 뺀 값이다.
            { "CRATE",          60f },   // 철제 상자 — 그림 144×132 · 발자국 2×1 칸
            // 도형을 둘 늘렸다. 맵마다 **쓰는 도형 조합**이 달라야 실루엣이 달라진다 —
            // 같은 네모를 색만 바꾸면 아무리 다른 물건이어도 같아 보인다.
            { "BULK",           94f },   // 큰 덩어리 — 그림 144×238 · 발자국 2×2 칸
            { "RAIL",           30f },   // 세로로 긴 것 — 그림 72×174 · 발자국 1×2 칸
            { "TIMED_SPIKE",     0f },   // 바닥 배수구 — 그림 144×144 · 바닥에 눕는다
            { "ROTATING_BLADE",  0f },   // 바닥을 스치듯 돈다
            { "SWING_HAMMER",   54f },
        };

        /// <summary>
        /// 판정 상자 / 그림 발자국 비율. 없으면 1 — 그림 그대로 막는다.
        ///
        /// 격자 블록만 줄인다. 블록 사이 **한 칸짜리 통로**를 지나가야 하는데
        /// 발자국을 꽉 채워 막으면 몸이 안 들어간다.
        ///
        /// ── 값을 어떻게 정했나 ──────────────────────────────────
        /// 두 값을 동시에 만족해야 한다. 발판 반크기를 `h`, 배율을 `s` 라 하면
        ///
        ///   통로 폭   = 144 − 2(36s + h)     ← 클수록 지나가기 쉽다
        ///   벽 안전여유 = 36s + h − 36        ← 0 이하면 **벽을 통과**한다
        ///
        /// 둘이 정확히 반대로 움직이므로 통로는 72 px 를 절대 못 넘는다.
        /// 가로·세로 발판이 다르므로(25.2 · 15.46) 배율도 달라야 같은 통로 폭이 나온다.
        ///
        ///   가로 0.50 → 통로 57.6 px · 여유 7.2 px
        ///   세로 0.70 → 통로 62.7 px · 여유 4.7 px
        ///
        /// ⚠ 가로를 안 줄이면(1.0) 나란히 선 블록 사이 통로가 **21.6 px** 밖에 안 된다.
        ///   눈에는 한 칸이 뻥 뚫려 보이는데 몸이 안 들어간다 — 실제로 그렇게 막혔었다.
        /// </summary>
        private static readonly Dictionary<string, Vector2> ObstacleFootScale = new()
        {
            // 지금은 비어 있다 — `ObstacleViewScale` 로 물건 자체를 줄였으므로
            // 판정을 또 줄일 이유가 없다. **보이는 것이 곧 막는 것**이다.
            // 그림은 그대로 두고 판정만 줄여야 하는 물건이 생기면 여기에 적는다.
        };

        /// <summary>
        /// 장애물을 칸보다 작게 그린다. **모든 종류에 걸린다.**
        ///
        /// 0.7 — 한 칸(72 px)짜리가 **50.4 px** 로 줄고 칸마다 21.6 px 여백이 남는다.
        /// 판정만 줄였을 때는 화면이 그대로라 통로가 눈에 안 보였다. 물건이 줄어야
        /// "여기가 지나갈 수 있는 자리"가 그림으로 읽힌다.
        ///
        /// 발자국·솟음·판정이 **한꺼번에** 이 배율을 먹는다(`SpawnObstacles`) —
        /// 보이는 것이 곧 막는 것이다. 셋 중 하나만 줄이면 또 어긋난다.
        ///
        /// 원본 그림을 70 %로 줄여 그리므로 도트가 조금 물러진다.
        /// 통로가 보이는 값이 그 대가보다 크다고 판단했다.
        /// </summary>
        private const float ObstacleViewScale = 0.7f;

        private static readonly Dictionary<string, Color> ObstacleColor = new()
        {
            { "PILLAR",        new Color(0.34f, 0.31f, 0.42f, 1f) },
            { "BARRICADE",     new Color(0.40f, 0.33f, 0.28f, 1f) },
            { "LOW_COVER",     new Color(0.30f, 0.34f, 0.40f, 1f) },
            { "DIVIDER",       new Color(0.28f, 0.27f, 0.36f, 1f) },
            { "RICOCHET_WALL", new Color(0.44f, 0.44f, 0.52f, 1f) },
            { "HAZARD",        new Color(0.86f, 0.34f, 0.18f, 0.45f) },
            { "BLOCK",         new Color(0.78f, 0.85f, 0.86f, 1f) },
            // 그림이 없을 때 쓰는 자리표시자 색. 파일이 들어오면 자동으로 그림이 이긴다.
            { "CRATE",          new Color(0.52f, 0.38f, 0.22f, 1f) },
            { "BULK",           new Color(0.44f, 0.40f, 0.34f, 1f) },
            { "RAIL",           new Color(0.38f, 0.40f, 0.46f, 1f) },
            { "TIMED_SPIKE",    new Color(0.72f, 0.66f, 0.30f, 0.75f) },
            { "ROTATING_BLADE", new Color(0.78f, 0.78f, 0.84f, 0.95f) },
            { "SWING_HAMMER",   new Color(0.62f, 0.60f, 0.66f, 0.95f) },
        };

#if UNITY_EDITOR
        // ── 판정 상자 보기 (F1) ───────────────────────────────────
        //
        // 초록 = 몸이 부딪히는 상자 · 노랑 = 탄이 막히는 상자 · 빨강 = 캐릭터 발판.
        // 셋이 다르다는 것을 눈으로 봐야 "왜 안 지나가는지"를 두 번 묻지 않게 된다.

        private bool _showHitBoxes;
        private readonly List<Image> _hitBoxViews = new();

        private Image MakeHitBox(Color c)
        {
            var go = new GameObject("HitBox", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.color = c; img.raycastTarget = false;
            go.transform.SetAsLastSibling();
            return img;
        }

        private void RebuildHitBoxView()
        {
            for (int i = 0; i < _hitBoxViews.Count; i++)
                if (_hitBoxViews[i] != null) Destroy(_hitBoxViews[i].gameObject);
            _hitBoxViews.Clear();
            if (!_showHitBoxes) return;
            for (int i = 0; i < _obstacles.Count; i++)
            {
                _hitBoxViews.Add(MakeHitBox(new Color(1f, 0.85f, 0.1f, 0.22f)));   // 탄
                _hitBoxViews.Add(MakeHitBox(new Color(0.2f, 1f, 0.4f, 0.30f)));    // 몸
            }
            _hitBoxViews.Add(MakeHitBox(new Color(1f, 0.2f, 0.2f, 0.55f)));        // 발판
        }

        private void TickHitBoxView()
        {
            if (_hitBoxViews.Count != _obstacles.Count * 2 + 1) { RebuildHitBoxView(); return; }
            for (int i = 0; i < _obstacles.Count; i++)
            {
                Put(_hitBoxViews[i * 2],     _obstacles[i].ShotBounds);
                Put(_hitBoxViews[i * 2 + 1], _obstacles[i].Bounds);
            }
            var me = Avatar;
            var foot = _hitBoxViews[_hitBoxViews.Count - 1];
            if (me == null) { foot.enabled = false; return; }
            foot.enabled = true;
            var half = FootHalf(me);
            var c = new Vector2(me.Position.x, me.Position.y - FootDrop(me));
            Put(foot, new Rect(c.x - half.x, c.y - half.y, half.x * 2f, half.y * 2f));

            static void Put(Image img, Rect r)
            {
                if (img == null) return;
                var rt = img.rectTransform;
                rt.sizeDelta = new Vector2(r.width, r.height);
                rt.anchoredPosition = r.center;
            }
        }
#endif


        /// <summary>기본형이 없는 도형을 빌려 올 무대 순서. 밝고 중립적인 것부터.</summary>
        private static readonly string[] FallbackEnvs =
        {
            "holding", "rooftop", "refinery", "missile", "street", "junkyard",
        };

        /// <summary>지금 방의 무대 이름(`junkyard` 등). 연구소는 빈 문자열이다.</summary>
        private string _floorEnv = string.Empty;

        /// <summary>
        /// 지형지물 그림자. `_obstacles` 와 따로 드는 이유는 `SortDepth` 에 넣지 않기
        /// 위해서다 — 그림자는 늘 맨 뒤에 깔린 채 정렬에서 빠져 있어야 안 깜빡인다.
        /// ⚠ 따로 들었으므로 **여기서 같이 지워야 한다.** 안 지우면 앞 방 그림자가
        ///   새 방 바닥에 그대로 남는다(실제로 그랬다).
        /// </summary>
        private readonly List<GameObject> _obstacleShadows = new();

        private void ClearObstacles()
        {
            for (int i = 0; i < _obstacles.Count; i++)
                if (_obstacles[i].View != null) Destroy(_obstacles[i].View);
            _obstacles.Clear();

            for (int i = 0; i < _obstacleShadows.Count; i++)
                if (_obstacleShadows[i] != null) Destroy(_obstacleShadows[i]);
            _obstacleShadows.Clear();
            _hazardTimer.Clear();
        }

        /// <summary>
        /// 보스방에서 치우는 지형지물.
        ///
        /// 상자는 **일반 방에서 엄폐물**이지만 보스방에서는 방해만 된다 —
        /// 예고 도형이 상자에 가려 어디가 위험한지 안 보이고,
        /// 「뒤로 돌아라」·「붙어라」 같은 지시를 상자가 막아 못 지키게 만든다.
        /// 기둥·바리케이드처럼 **읽히는 큰 것**은 남긴다. 그건 지형이다.
        /// </summary>
        private static bool ClearedInBossRoom(string kind) => kind == "CRATE";

        /// <summary>그림자 크기 — 발자국 대비. 살짝 넓게 깔려야 바닥에 앉은 것으로 보인다.</summary>
        private const float ObstacleShadowScale = 1.12f;

        /// <summary>그림자 높이 — 쿼터뷰라 납작해야 바닥에 누운 것으로 읽힌다.</summary>
        private const float ObstacleShadowFlatten = 0.45f;

        private static readonly Color ObstacleShadowColor = new(0f, 0f, 0f, 0.42f);

        /// <summary>
        /// 지형지물 발밑에 타원 그림자를 깐다.
        ///
        /// 새 그림 없이 있는 것(`fx_shadow` 가 없으면 흰 사각형에 색만)으로 만든다 —
        /// 이건 「무엇이 물건인가」를 가르는 표시지 장식이 아니다.
        ///
        /// ⚠ **캐릭터보다 뒤에 깔린다.** `SortDepth` 는 `_obstacles` 와 유닛만 줄을 세우므로,
        ///   그림자는 거기 넣지 않고 만들 때 맨 뒤로 보낸다. 매 프레임 다시 정렬되지 않아
        ///   깜빡이지도 않는다.
        /// </summary>
        private void SpawnObstacleShadow(string kind, Vector2 center, Vector2 size)
        {
            var go = new GameObject($"Shadow_{kind}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size.x * ObstacleShadowScale,
                                       size.y * ObstacleShadowScale * ObstacleShadowFlatten);
            // 발자국 아래쪽에 앉힌다 — 물건이 서 있는 자리가 곧 그림자 자리다.
            rt.anchoredPosition = new Vector2(center.x, center.y - size.y * 0.5f
                                              + rt.sizeDelta.y * 0.5f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.sprite = GetSprite("fx_shadow") ?? GetSprite("obj_shadow");
            img.color = ObstacleShadowColor;
            go.transform.SetAsFirstSibling();   // 바닥 바로 위, 모든 것보다 뒤
            _obstacleShadows.Add(go);
        }

        private void SpawnObstacles(RoomEntry room)
        {
            bool bossRoom = _roomKind == RoomKind.Boss;
            var list = room.Objects;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                if (bossRoom && ClearedInBossRoom(o.Kind)) continue;
                var center = ToPixels(o.At);

                // ⚠ **물건 자체를 줄인다.** 판정만 줄이면 눈에는 아무 변화가 없다 —
                //   그림은 칸을 꽉 채운 채 몸만 몰래 지나가서, 왜 지나가는지·왜 안 지나가는지
                //   화면만 봐서는 알 수가 없다. 칸에 여백이 보여야 통로가 통로로 읽힌다.
                const float vs = ObstacleViewScale;
                var size = o.Size * _pxPerMeter * vs;

                // 판정은 그림 그대로다. **보이는 것이 곧 막는 것**이어야 한다.
                var hit = ObstacleFootScale.TryGetValue(o.Kind ?? "", out var fs)
                    ? new Vector2(size.x * fs.x, size.y * fs.y) : size;
                var rect = new Rect(center.x - hit.x * 0.5f, center.y - hit.y * 0.5f,
                                    hit.x, hit.y);
                var shotRect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f,
                                        size.x, size.y);

                // 그림은 발자국보다 위로 솟는다. 아래쪽을 발자국에 맞물려 놓아야
                // 발밑이 어긋나지 않는다. 물건을 줄였으면 솟음도 같은 비율로 줄인다 —
                // 안 줄이면 납작한 발자국 위에 예전 높이가 얹혀 비율이 무너진다.
                float rise = (ObstacleRise.TryGetValue(o.Kind ?? "", out var r) ? r : 0f) * vs;
                var viewSize = new Vector2(size.x, size.y + rise);
                var viewCenter = new Vector2(center.x, center.y + rise * 0.5f);

                // ⚠ 정수 픽셀에 앉힌다. 72 px/m 로 떨어지는 값이라도 부동소수 나눗셈에서
                //   0.0001 이 남고, 그 반 픽셀 때문에 블록을 옆으로 이어 붙였을 때
                //   경계에 실금이 생기거나 한 줄이 두 번 그려진다.
                viewSize = new Vector2(Mathf.Round(viewSize.x), Mathf.Round(viewSize.y));
                viewCenter = new Vector2(Mathf.Round(viewCenter.x * 2f) * 0.5f,
                                         Mathf.Round(viewCenter.y * 2f) * 0.5f);

                // ⚠ **발밑에 그림자를 깐다** (2026-09-10).
                //   바닥 그림에도 물건이 그려져 있어서, 우리가 세운 지형지물과
                //   배경 무늬가 화면에서 구분이 안 됐다 — 그려진 것을 엄폐물로 알고
                //   숨으려다 맞고, 진짜 엄폐물은 못 알아봤다.
                //   그림자는 「이건 바닥에서 솟아 있다」를 한눈에 말해 준다.
                //   눕는 것(도랑·가시판)에는 안 붙인다 — 솟은 것이 아니다.
                if (rise > 0f) SpawnObstacleShadow(o.Kind, center, size);

                var go = new GameObject($"Obj_{o.Kind}_{o.ObjectId}",
                                        typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_unitLayer, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = viewSize;
                rt.anchoredPosition = viewCenter;

                var img = go.GetComponent<Image>();
                // 같은 종류라도 크기가 제각각이다. 한 장으로 돌려쓰면 늘어나 뭉개지므로
                // **화면에 놓일 크기와 이름이 같은 그림**을 먼저 찾는다(`obj_pillar_234x348`).
                // 없으면 방향별(RICOCHET_WALL 의 v/h), 그것도 없으면 종류 기본 한 장.
                string kind = (o.Kind ?? "").ToLowerInvariant();
                string art = SpritePrefixOf(o.Kind);
                Sprite sprite;
                if (o.Kind == "BLOCK")
                {
                    // 격자 블록은 세 장이 거의 같고 잔무늬만 다르다. 한 장만 쓰면
                    // 벽이 인쇄물처럼 보이므로 **자리로** 골라 섞는다 —
                    // 자리가 같으면 늘 같은 그림이라 방을 다시 들어와도 안 바뀐다.
                    int pick = Mathf.Abs(Mathf.RoundToInt(o.At.x * 7f + o.At.y * 13f)) % BlockVariants + 1;
                    sprite = EnvSprite($"obj_block_{pick}") ?? EnvSprite("obj_block_1");
                }
                else
                {
                    sprite = EnvSprite($"{art}_{Mathf.RoundToInt(viewSize.x)}x{Mathf.RoundToInt(viewSize.y)}")
                             ?? EnvSprite($"{art}_{(size.y >= size.x ? "v" : "h")}")
                             ?? EnvSprite($"{art}_1")     // 여러 장짜리는 첫 장을 기본으로
                             ?? EnvSprite(art);
                }
                img.sprite = sprite;
                img.color = sprite != null
                    ? Color.white
                    : ObstacleColor.TryGetValue(o.Kind ?? "", out var c)   // 그림 오기 전 자리표시자
                        ? c : new Color(0.33f, 0.32f, 0.40f, 1f);
                img.raycastTarget = false;

                var ob = new Obstacle
                {
                    Bounds = rect, ShotBounds = shotRect,
                    BlocksMove = o.BlocksMove, BlocksShot = o.BlocksShot,
                    BlocksEnemyShot = o.BlocksEnemyShot,
                    IsHazard = o.IsHazard, Damage = o.HazardDamage, Tick = o.HazardTick,
                    View = go, Kind = o.Kind, Img = img, Home = rect,
                    // 같은 방의 같은 종류가 한 박자로 움직이면 기계처럼 보인다.
                    // 자리로 위상을 어긋내면 방 전체가 살아 있는 것처럼 읽힌다.
                    Phase = Mathf.Repeat((center.x * 0.013f + center.y * 0.021f), 1f),
                };
                // 도랑은 **바닥에 파인 것**이다. 다른 소품·캐릭터보다 아래로 내린다 —
                // 위에 있으면 도랑이 사람을 덮어 어디 서 있는지 안 보인다.
                if (o.Kind == "CHANNEL_H" || o.Kind == "CHANNEL_V") go.transform.SetAsFirstSibling();
                SetupMoving(ob, kind);
                _obstacles.Add(ob);
            }
        }

        // ── 움직이는 지형지물 ────────────────────────────────────
        //
        // 정본이 30개 방에 `TIMED_HAZARD`·`ROTATING_HAZARD` 를 적어 두었는데
        // 이름만 저장하고 거동은 없었다. 배치가 전부 정적이면 한 번 파악한 방은
        // 두 번째부터 아무 판단도 필요 없어진다. 시간 축은 여기서 생긴다.

        private const float SpikeCycle = 2.0f;     // 들어감 → 솟음 → 들어감 한 바퀴
        private const float BladeTurn = 2.6f;      // 톱날 한 바퀴
        private const float BladeRadius = 2.2f;    // 축에서 날까지(미터)
        private const float HammerCycle = 3.0f;    // 추가 왕복 한 바퀴
        private const float HammerTravel = 3.4f;   // 왕복 폭(미터)
        private const int CrateHp = 40;

        /// <summary>
        /// 종류 이름 → 그림 파일 접두사.
        ///
        /// 종류 이름은 거동을 말하고(`ROTATING_BLADE` = 도는 것), 파일 이름은 물건을
        /// 말한다(`obj_blade` = 톱날). 둘이 꼭 같을 필요는 없어서 여기서 이어 준다.
        /// </summary>
        /// <summary>격자 블록 그림 장수. 잔무늬만 다른 세 장을 자리로 골라 섞는다.</summary>
        private const int BlockVariants = 3;

        private static string SpritePrefixOf(string kind) => kind switch
        {
            "ROTATING_BLADE" => "obj_blade",
            "SWING_HAMMER"   => "obj_hammer",
            // 도랑은 가로·세로 두 장뿐이다. 접두사를 하나로 두면 바로 아래
            // 크기 판정이 `_h`·`_v` 를 알아서 골라 준다 — 종류를 둘로 나눈 이유다.
            "CHANNEL_H" or "CHANNEL_V" => "obj_channel",
            _                => "obj_" + (kind ?? string.Empty).ToLowerInvariant(),
        };

        /// <summary>종류마다 필요한 준비물을 챙긴다. 정적인 것은 그냥 지나간다.</summary>
        private void SetupMoving(Obstacle ob, string kind)
        {
            string art = SpritePrefixOf(ob.Kind);
            switch (ob.Kind)
            {
                case "CRATE":
                    ob.Hp = CrateHp;
                    ob.Frames = Frames3(art);
                    break;

                case "TIMED_SPIKE":
                    ob.Frames = Frames3(art);
                    // 가시는 **솟아 있을 때만** 아프다. 판정은 매 프레임 켜고 끈다.
                    ob.IsHazard = true;
                    if (ob.Damage <= 0) ob.Damage = 8;
                    if (ob.Tick <= 0f) ob.Tick = 0.6f;
                    break;

                case "ROTATING_BLADE":
                {
                    ob.Frames = new[]
                    {
                        GetSprite($"{art}_1"), GetSprite($"{art}_2"),
                        GetSprite($"{art}_3"), GetSprite($"{art}_4"),
                    };
                    ob.IsHazard = true;
                    if (ob.Damage <= 0) ob.Damage = 10;
                    if (ob.Tick <= 0f) ob.Tick = 0.5f;
                    // 축은 제자리에 박혀 있고 날만 돈다. 축이 없으면 무엇을 중심으로
                    // 도는지 안 보여서 날이 허공에 떠 있는 것처럼 읽힌다.
                    ob.View2 = MakeExtra("Axis", $"{art}_axis", ob.Home.center, 54f);
                    break;
                }

                case "SWING_HAMMER":
                    ob.Frames = new[] { GetSprite(art) };
                    ob.IsHazard = true;
                    if (ob.Damage <= 0) ob.Damage = 12;
                    if (ob.Tick <= 0f) ob.Tick = 0.7f;
                    break;
            }
        }

        private Sprite[] Frames3(string prefix)
            => new[] { GetSprite($"{prefix}_1"), GetSprite($"{prefix}_2"), GetSprite($"{prefix}_3") };

        /// <summary>축·사슬처럼 본체를 따라다니는 보조 그림 한 장.</summary>
        private RectTransform MakeExtra(string name, string sprite, Vector2 at, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = at;
            var img = go.GetComponent<Image>();
            img.sprite = GetSprite(sprite);
            img.enabled = img.sprite != null;
            img.raycastTarget = false;
            rt.SetAsFirstSibling();          // 날 뒤에 깔린다
            return rt;
        }

        /// <summary>
        /// 엄폐물 안에 선 몸을 밖으로 밀어낸다. **세우는 순간에만** 쓴다.
        ///
        /// ⚠ 정본 스폰 자리 345개 중 121개가 엄폐물 사각형 안이다. 정본은 방 한가운데
        ///   부근에 적을 세우는데(x≈4 m), 엄폐물 자리는 정본에 없어서 내가 만들었고
        ///   그것도 한가운데에 놓았기 때문이다. 겹치면 이렇게 된다:
        ///
        ///     · 적이 기둥 속에 서 있어 **기둥 위에 체력바만 뜬다** — 건물에 HP 가 달린 것처럼 보인다
        ///     · 내 탄이 기둥의 `BlocksShot` 에 먼저 먹혀 **적에게 닿지 않는다**
        ///     · 그래서 죽지 않고, `_enemies` 가 비지 않아 **방이 영영 클리어되지 않는다**
        ///
        ///   실제로 ROOM_CH1_002(스테이지 2/12)가 이 경우다 — 적 3기가 전부 기둥 속이었다.
        ///
        /// `ResolveObstacles` 로는 모자란다. 그쪽은 **발밑**만 빼내므로 몸 중심이 사각형
        /// 모서리에 걸린 채 남고, 탄 판정은 중심을 향하므로 여전히 먹힌다.
        /// 여기서는 중심까지 확실히 빼내고 여유를 조금 더 둔다.
        /// </summary>
        private void ClearOfCover(Unit u)
        {
            if (u == null || _obstacles.Count == 0) return;
            const float Margin = 8f;   // 모서리에 딱 붙으면 반올림에 따라 다시 먹힌다

            var p = u.Position;
            for (int pass = 0; pass < 4; pass++)   // 밀어낸 자리가 또 다른 엄폐물일 수 있다
            {
                bool moved = false;
                for (int i = 0; i < _obstacles.Count; i++)
                {
                    var o = _obstacles[i];
                    if (!o.BlocksMove && !o.BlocksShot) continue;   // 해저드는 밟아도 된다
                    // 여기서는 **그림 전체**로 본다. 몸이 지나갈 길(`Bounds`)만 피하면
                    // 적이 블록 그림 속에 반쯤 파묻힌 채 서서 탄이 안 닿는다.
                    if (!o.ShotBounds.Contains(p)) continue;

                    float dx = p.x - o.ShotBounds.center.x;
                    float dy = p.y - o.ShotBounds.center.y;
                    float ox = o.ShotBounds.width * 0.5f - Mathf.Abs(dx) + Margin;
                    float oy = o.ShotBounds.height * 0.5f - Mathf.Abs(dy) + Margin;

                    // 얕게 걸린 축으로 뺀다. 정확히 중심이면 부호가 0 이라 방향을 정해 준다.
                    if (ox <= oy) p.x += (dx >= 0f ? 1f : -1f) * ox;
                    else p.y += (dy >= 0f ? 1f : -1f) * oy;
                    moved = true;
                }
                if (!moved) break;
            }

            u.Position = p;
            ClampToField(u);
        }

        /// <summary>
        /// 발밑 판정 상자의 **반크기**. 몸 전체로 보면 머리가 기둥에 걸려 못 지나간다.
        ///
        /// 가로는 그림의 **절반**이다. 블록 한 칸(72 px) 틈을 지나가야 하는데
        /// 예전 값(0.55 → 그림의 55 %, 55 px)은 틈에 겨우 들어가 벽을 스치듯 비벼야 했다.
        /// 절반(50 px)이면 좌우로 11 px 씩 남아 그냥 걸어서 통과한다.
        ///
        /// 세로는 그림의 32 %다. 이보다 얇게 잡으면 세로로 쌓인 블록 사이
        /// 0.3 칸(21.6 px) 틈에 몸이 끼어 **벽을 통과**한다(`ObstacleFootScale` 주석 참조).
        /// </summary>
        private static Vector2 FootHalf(Unit u)
        {
            var h = ((RectTransform)u.transform).sizeDelta * 0.5f;
            return new Vector2(Mathf.Max(h.x * 0.5f,  MinFootHalfX),
                               Mathf.Max(h.y * 0.32f, MinFootHalfY));
        }

        /// <summary>
        /// 발판 반크기의 **하한**. `ObstacleViewScale` 과 짝을 이룬다 —
        /// 블록 배율이 `s` 일 때 `36s + 발판반크기 > 36` 이어야
        /// 나란히 붙은 블록 사이로 **벽을 통과**하지 않는다.
        ///
        ///   배율 0.7 → 발판 반크기가 10.8 px 를 넘어야 한다
        ///
        /// 그림이 작은 유닛은 비율로만 잡으면 이 선에 가까워진다.
        /// 배율을 건드릴 때 이 두 값도 같이 본다.
        /// </summary>
        private const float MinFootHalfX = 21f;
        private const float MinFootHalfY = 14f;

        /// <summary>
        /// 그림 가운데에서 발판 가운데까지의 거리.
        ///
        /// ⚠ 예전에는 발판을 `pos.y - 발판반높이` 에 놓았다. 그러면 상자가 **가슴께**에 걸린다 —
        ///   깊이 정렬은 그림 밑변을 발밑으로 보는데 충돌만 48 px 위를 보고 있었다.
        ///   눈으로 발끝을 틈에 맞춰도 가슴이 먼저 막혀 "보이는데 안 들어가는" 상태가 됐다.
        ///   이제 둘 다 **그림 밑변**을 기준으로 한다.
        /// </summary>
        private static float FootDrop(Unit u)
            => ((RectTransform)u.transform).sizeDelta.y * 0.5f - FootHalf(u).y;

        private bool BlockedAt(Vector2 pos, Vector2 half, float drop)
        {
            var foot = new Vector2(pos.x, pos.y - drop);
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!o.BlocksMove) continue;
                if (Mathf.Abs(foot.x - o.Bounds.center.x) < o.Bounds.width * 0.5f + half.x &&
                    Mathf.Abs(foot.y - o.Bounds.center.y) < o.Bounds.height * 0.5f + half.y)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 막힌 것을 타고 미끄러지며 움직인다.
        ///
        /// 먼저 밀어 넣고 빠져나오게 하면 입력과 밀어내기가 매 프레임 싸워서
        /// 벽에 붙었을 때 캐릭터가 떨린다. 아예 **들어가지 않게** 하는 편이 낫다.
        /// 대각선이 막히면 x 만, 그것도 막히면 y 만 시도한다 — 벽을 따라 흐른다.
        /// </summary>
        private Vector2 SlideMove(Unit u, Vector2 from, Vector2 delta)
        {
            if (_obstacles.Count == 0) return from + delta;
            // 구루 패시브 — 걸어서 지나간다. 탄은 그대로 막힌다(이동만이다).
            if (u == Avatar && HostIgnoresObstacles) return from + delta;
            var half = FootHalf(u);
            float drop = FootDrop(u);

            var p = from + delta;
            if (!BlockedAt(p, half, drop)) return p;

            var px = new Vector2(from.x + delta.x, from.y);
            if (!BlockedAt(px, half, drop)) return px;

            var py = new Vector2(from.x, from.y + delta.y);
            if (!BlockedAt(py, half, drop)) return py;

            return from;
        }

        /// <summary>
        /// 이미 막힌 것 안에 있으면 밀어낸다. 스폰이나 순간이동으로 갇힌 경우의 구제책이다.
        /// 평소 이동은 `SlideMove` 가 애초에 들어가지 않게 막는다.
        /// 가장 얕게 겹친 축으로 빼야 모서리에서 반대편으로 튀지 않는다.
        /// </summary>
        private void ResolveObstacles(Unit u)
        {
            if (_obstacles.Count == 0 || u == null) return;
            var half = FootHalf(u);
            float drop = FootDrop(u);
            var p = u.Position;
            if (!BlockedAt(p, half, drop)) return;

            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!o.BlocksMove) continue;

                var foot = new Vector2(p.x, p.y - drop);
                float dx = foot.x - o.Bounds.center.x;
                float dy = foot.y - o.Bounds.center.y;
                float ox = o.Bounds.width * 0.5f + half.x - Mathf.Abs(dx);
                float oy = o.Bounds.height * 0.5f + half.y - Mathf.Abs(dy);
                if (ox <= 0f || oy <= 0f) continue;

                // 정확히 중심에 겹치면 부호가 0 이라 방향을 못 정한다. 아래로 밀어낸다.
                if (ox < oy) p.x += (dx >= 0f ? 1f : -1f) * ox;
                else p.y += (dy >= 0f ? 1f : -1f) * oy;
            }
            u.Position = p;
        }

        // 앞뒤 정렬 — 발밑이 아래인 것이 위에 그려진다.
        // 쿼터뷰라 기둥이 항상 뒤에 깔리면 기둥 앞에 선 캐릭터까지 기둥에 가려진다.
        // 매 프레임 새 리스트를 만들면 hot path 할당이 되므로 버퍼를 재사용한다.
        private readonly List<Transform> _depthT = new();
        private readonly List<float> _depthY = new();

        /// <summary>
        /// 캐릭터 그림 아래에 비어 있는 띠. 96 px 캔버스에서 발끝 아래로 8 px 이 늘 남는다
        /// (아마존·유령·해골·박쥐 전부 같다). 캔버스 밑변을 발밑으로 쓰면 그만큼
        /// **실제보다 앞에 있는 것으로** 줄을 서서, 지형지물과 앞뒤가 한 뼘씩 어긋난다.
        /// </summary>
        private const float UnitFootPadRatio = 8f / 96f;

        /// <summary>
        /// 줄 세우기에 한 기를 넣는다.
        ///
        /// ⚠ **보스는 언제나 맨 뒤다.** 발밑으로 줄을 세우면 덩치(256px)가 커서
        ///   플레이어보다 앞에 서는 순간이 생기고, 그러면 내 몸이 보스 그림에
        ///   통째로 파묻혀 **어디 서 있는지 안 보인다.** 보스는 배경에 가까운
        ///   큰 장치라 뒤에 두는 편이 늘 읽힌다.
        /// </summary>
        private void AddDepth(Unit u)
        {
            if (u == null || !u.gameObject.activeSelf) return;
            if (u.IsBoss) { _depthT.Add(u.transform); _depthY.Add(float.MaxValue); return; }
            var size = ((RectTransform)u.transform).sizeDelta;
            _depthT.Add(u.transform);
            _depthY.Add(u.Position.y - size.y * 0.5f + size.y * UnitFootPadRatio);
        }

        private void SortDepth()
        {
            // ⚠ **지형이 없어도 정렬한다.** 예전에는 `_obstacles.Count == 0` 이면 그냥
            //   빠졌는데, 지형지물을 방마다 2~3개로 줄이면서 **물건이 0개인 방이 18곳**
            //   생겼다. 그 방에서는 정렬이 통째로 안 돌아, 나중에 세운 출구 계단이
            //   맨 앞에 그려져 **캐릭터를 덮었다**. 출구·보스 순서도 여기서 정해진다.
            _depthT.Clear();
            _depthY.Clear();
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o.View == null) continue;
                _depthT.Add(o.View.transform);
                // ⚠ **바닥에 눕는 것은 언제나 맨 뒤다.** 가시판·불길처럼 솟음이 0 인 물건은
                //   바닥에 그린 무늬지 서 있는 물건이 아니다. 발자국 아래 변으로 줄을 세우면
                //   그 위에 선 캐릭터가 판보다 뒤로 밀려 **판에 파묻힌다.**
                float oRise = ObstacleRise.TryGetValue(o.Kind ?? "", out var orv) ? orv : 0f;
                _depthY.Add(oRise <= 0f ? float.MaxValue : o.ShotBounds.yMin);
            }
            AddDepth(_ghost);
            AddDepth(_host);
            for (int i = 0; i < _enemies.Count; i++) AddDepth(_enemies[i]);
            for (int i = 0; i < _dying.Count; i++) AddDepth(_dying[i]);
            // 문은 정렬에서 빼면 순서가 매 프레임 밀려 깜빡인다. 늘 맨 뒤에 둔다 —
            // 방 위쪽 끝에 있어 무엇을 가릴 일이 없다.
            for (int i = 0; i < _exits.Count; i++)
                if (_exits[i].View != null)
                {
                    _depthT.Add(_exits[i].View);
                    _depthY.Add(float.MaxValue);
                }

            // 삽입 정렬 — 항목이 스무 개 남짓이고 프레임마다 거의 정렬돼 있다.
            for (int i = 1; i < _depthT.Count; i++)
            {
                var t = _depthT[i]; float y = _depthY[i];
                int j = i - 1;
                while (j >= 0 && _depthY[j] < y)   // 큰 값(위쪽)이 앞으로
                {
                    _depthT[j + 1] = _depthT[j]; _depthY[j + 1] = _depthY[j]; j--;
                }
                _depthT[j + 1] = t; _depthY[j + 1] = y;
            }
            for (int i = 0; i < _depthT.Count; i++)
                if (_depthT[i].GetSiblingIndex() != i) _depthT[i].SetSiblingIndex(i);
        }

        /// <summary>해저드 위에 서 있으면 주기적으로 깎인다.</summary>
        /// <summary>
        /// 움직이거나 상태가 바뀌는 지형지물을 한 프레임 진행시킨다.
        ///
        /// 판정 사각형(`Bounds`)까지 함께 옮긴다 — 그림만 움직이고 판정이 제자리에 있으면
        /// 보이는 것과 맞는 것이 어긋나서 "왜 맞았는지 모르겠다" 가 된다.
        /// </summary>
        private void TickMovingObstacles(float dt)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o.Kind == null || o.View == null) continue;
                var rt = (RectTransform)o.View.transform;

                switch (o.Kind)
                {
                    case "TIMED_SPIKE":
                    {
                        // 0 → 1 → 0 을 오간다. 솟은 동안(0.55 이상)만 아프다.
                        float t = Mathf.Repeat(Time.time / SpikeCycle + o.Phase, 1f);
                        float up = t < 0.5f ? t * 2f : (1f - t) * 2f;
                        SetFrame(o, up < 0.2f ? 0 : up < 0.75f ? 1 : 2);
                        // ⚠ `Damage` 를 끄고 켜면 안 된다. 내려간 동안 0 으로 덮어쓴 뒤
                        //   올라올 때 `Max(1, 0)` 이 되어 **한 번 내려갔다 오면 피해가 1 로 굳는다.**
                        //   원래 값은 그대로 두고 **켜짐 여부만** 따로 든다.
                        o.HazardOn = up >= 0.55f;
                        break;
                    }

                    case "ROTATING_BLADE":
                    {
                        // 축을 중심으로 돈다. 그림은 22.5도씩 네 장이라 90도가 한 바퀴다.
                        float t = Mathf.Repeat(Time.time / BladeTurn + o.Phase, 1f);
                        float ang = t * Mathf.PI * 2f;
                        float rad = BladeRadius * _pxPerMeter;
                        var c = o.Home.center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                        MoveObstacle(o, rt, c);
                        SetFrame(o, Mathf.FloorToInt(Mathf.Repeat(t * 16f, 4f)));
                        break;
                    }

                    case "SWING_HAMMER":
                    {
                        // 두 점을 오간다. 끝에서 잠깐 멎어야 "돌아온다" 가 읽힌다 —
                        // 등속으로 왕복하면 언제 방향이 바뀌는지 안 보인다.
                        float t = Mathf.Repeat(Time.time / HammerCycle + o.Phase, 1f);
                        float e = Mathf.SmoothStep(0f, 1f, t < 0.5f ? t * 2f : (1f - t) * 2f);
                        float span = HammerTravel * _pxPerMeter;
                        var c = o.Home.center + new Vector2(0f, -span * 0.5f + span * e);
                        MoveObstacle(o, rt, c);
                        break;
                    }

                    case "CRATE":
                        // 남은 체력에 따라 금이 간다. 부서지는 것은 맞을 때 처리한다.
                        SetFrame(o, o.Hp > CrateHp / 2 ? 0 : 1);
                        break;
                }
            }
        }

        /// <summary>판정과 그림을 함께 옮긴다. 축·사슬 같은 보조 그림도 따라간다.</summary>
        private void MoveObstacle(Obstacle o, RectTransform rt, Vector2 center)
        {
            o.Bounds = new Rect(center.x - o.Home.width * 0.5f,
                                center.y - o.Home.height * 0.5f,
                                o.Home.width, o.Home.height);
            // ⚠ 탄 상자도 같이 옮긴다. 하나만 옮기면 그림은 도는데 탄은 제자리를 막는다.
            o.ShotBounds = new Rect(center.x - o.ShotBounds.width * 0.5f,
                                    center.y - o.ShotBounds.height * 0.5f,
                                    o.ShotBounds.width, o.ShotBounds.height);
            float rise = ObstacleRise.TryGetValue(o.Kind ?? "", out var r) ? r : 0f;
            rt.anchoredPosition = new Vector2(center.x, center.y + rise * 0.5f);
        }

        private static void SetFrame(Obstacle o, int index)
        {
            if (o.Img == null || o.Frames == null || o.Frames.Length == 0) return;
            var sp = o.Frames[Mathf.Clamp(index, 0, o.Frames.Length - 1)];
            if (sp == null || o.Img.sprite == sp) return;
            o.Img.sprite = sp;
            o.Img.color = Color.white;
        }

        /// <summary>
        /// 부술 수 있는 것에 탄이 맞았다. 부서졌으면 true —
        /// 그 자리에서 길이 열린다. 막다른 곳을 내가 뚫어 만드는 것이 이 물건의 값어치다.
        /// </summary>
        private bool DamageCrate(Vector2 at, int damage)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (o.Kind != "CRATE" || o.Hp <= 0 || !o.ShotBounds.Contains(at)) continue;
                o.Hp -= Mathf.Max(1, damage);
                ShowDamage(at, damage, toEnemy: true);
                if (o.Hp > 0) return true;

                SetFrame(o, 2);
                if (o.View != null) Destroy(o.View);
                if (o.View2 != null) Destroy(o.View2.gameObject);
                _obstacles.RemoveAt(i);
                return true;
            }
            return false;
        }

        private void TickHazards(float dt)
        {
            if (_obstacles.Count == 0) return;
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!o.IsHazard || !o.HazardOn || o.Damage <= 0) continue;
                // ⚠ **적은 안 아프다.** 가시·톱니·용암은 플레이어에게만 판정한다(기획 2026-09-08).
                //
                //   적까지 아프면 방이 알아서 정리된다 — 가시밭에 몰아넣고 기다리는 것이
                //   최적 수가 되어 「피해서 지나간다」라는 문제가 통째로 사라진다.
                //   게다가 적은 지형을 보고 걷지 않으므로(추격만 한다) 제 발로 밟다가
                //   죽어 나가, 배치가 난이도가 아니라 서비스가 된다.
                Burn(o, Avatar, dt);
            }
        }

        private void Burn(Obstacle o, Unit u, float dt)
        {
            if (u == null || !u.IsAlive || u.IsDying) return;
            // 밟았는지는 **이동 판정과 같은 발밑**으로 본다. 여기만 다른 값을 쓰면
            // 눈에는 판 위에 서 있는데 안 아프거나, 비켰는데 계속 아프다.
            var foot = new Vector2(u.Position.x, u.Position.y - FootDrop(u));
            if (!o.ShotBounds.Contains(foot))
            {
                _hazardTimer.Remove(u);
                return;
            }

            _hazardTimer.TryGetValue(u, out float t);
            t -= dt;
            if (t > 0f) { _hazardTimer[u] = t; return; }

            // 처음 밟는 순간 바로 한 번 아프게 한다. 그래야 밟았다는 것을 안다.
            _hazardTimer[u] = o.Tick;
            if (u == _host || u == _ghost) DamagePlayer(o.Damage);
            else { u.TakeDamage(o.Damage); ShowDamage(u.Position, o.Damage, true); }
        }

        /// <summary>
        /// 방 크기를 정한다. 가로는 늘 화면 폭(8.4 m)이고 세로만 방마다 다르다.
        /// `UnitLayer` 를 방 크기로 키우고 위쪽에 붙인다 — 좌표계가 위에서 아래로
        /// 음수인 채 그대로 유지되도록.
        /// </summary>
        private void SetRoomSize(float meterHeight)
        {
            _roomSize = new Vector2(RoomMeterWidth * _pxPerMeter, meterHeight * _pxPerMeter);
            _unitLayer.anchorMin = _unitLayer.anchorMax = new Vector2(0f, 1f);
            _unitLayer.pivot = new Vector2(0f, 1f);
            _unitLayer.sizeDelta = _roomSize;
            _scroll = Vector2.zero;
            // 방 크기가 바뀌면 탄·숫자 레이어도 같은 크기·같은 자리여야 한다
            if (_shotLayer != null) _shotLayer.sizeDelta = _roomSize;
            if (_textLayer != null) _textLayer.sizeDelta = _roomSize;
            if (_floor != null)
            {
                _floor.anchorMin = _floor.anchorMax = new Vector2(0f, 1f);
                _floor.pivot = new Vector2(0f, 1f);
                _floor.sizeDelta = _roomSize;
            }
            LayoutPythonStage();
            // 방이 바뀌면 창도 다시 잰다 — 보스방(16 m)은 더 보여 줄 수 있다.
            FitFieldHeight();
            ApplyScroll();
        }

        /// <summary>
        /// 세로 카메라. 창은 고정이고 방을 민다.
        /// 플레이어를 창 한가운데 두되 방의 위아래 끝을 넘어가지 않는다 —
        /// 넘어가면 방 밖의 빈 공간이 보인다.
        /// </summary>
        private void TickCamera(float dt)
        {
            var a = Avatar;
            if (a == null || _unitLayer == null) return;

            TickZoom(dt);              // 줌을 먼저 — 카메라 범위가 줌에 따라 달라진다
            _scroll = WantScroll(a);   // 지연 없음 — 위 주석 참조
            ApplyScroll();
        }

        // ── 카메라 줌 ─────────────────────────────────────────────
        //
        // 화면 비율마다 기본 줌(`GameConfig.CameraZoomFor` — 9:16 폰 · 4:3 태블릿)이 있고,
        // 빙의하는 동안은 그 위에 `PossessZoom` 만큼 더 당긴다. 빙의는 이 게임의 이름값이 걸린
        // 동작이라 아무 변화 없이 캐릭터만 바뀌면 그냥 조작 하나로 읽힌다.
        //
        // ⚠ 2026-09-11 전에는 **창(`_field`)을 통째로** 키웠다. 창에 마스크가 걸려 있어 마스크까지
        //   같이 커졌고, 줌 중심이 캐릭터가 아니라 창 가운데였다. 이제 창은 그대로 두고
        //   방 레이어들만 캐릭터 자리(`ViewFocus`)를 중심으로 키운다 — `ApplyScroll` · `Place`.

        private const float ZoomSpeed = 9f;

        private float _zoom = 1f;

        /// <summary>지금 가야 할 줌. 벽 보스는 카메라를 세우므로 당기지 않는다 — 벽이 화면 밖으로 나간다.</summary>
        private float WantZoom()
        {
            if (_config == null || _field == null || CameraLocked) return 1f;
            // 캔버스 값(부모 크기)으로 비율을 잰다. `Screen` 은 해상도가 바뀌는 프레임에 캔버스와 어긋난다.
            var parent = _field.parent as RectTransform;
            float aspect = parent != null && parent.rect.height > 0f
                ? parent.rect.width / parent.rect.height : 9f / 16f;
            float zoom = _config.CameraZoomFor(aspect);
            return IsChanneling ? zoom * _config.PossessZoom : zoom;
        }

        private void TickZoom(float dt)
        {
            float want = WantZoom();
            _zoom = Mathf.Lerp(_zoom, want, 1f - Mathf.Exp(-ZoomSpeed * dt));
            if (Mathf.Abs(_zoom - want) < 0.0005f) _zoom = want;
        }

        /// <summary>
        /// 줌의 중심 — 창 안에서 캐릭터가 서는 자리(창 왼쪽 위 기준, 아래로 음수).
        /// 캐릭터는 줌이 바뀌어도 이 자리에 그대로 있고 둘레만 커진다.
        /// </summary>
        private Vector2 ViewFocus => new(_field.rect.width * 0.5f, -_field.rect.height * CameraAnchor);

        /// <summary>
        /// 방 좌표 → 창 좌표(창 왼쪽 위 기준, 아래로 음수). 스크롤과 줌을 함께 먹인다.
        /// 창에서 무엇이 어디 보이는지 따지는 곳(화면 밖 판정 · 빙의 화살표)은 전부 이것을 쓴다.
        /// </summary>
        private Vector2 RoomToView(Vector2 room)
        {
            var f = ViewFocus;
            return f + _zoom * (room + _scroll - f);
        }

        /// <summary>
        /// 스크롤을 세 레이어에 함께 먹인다.
        ///
        /// 탄·피해 수치는 유닛보다 위에 그리려고 **형제 레이어**로 뽑아 놨다.
        /// 그래서 유닛 레이어만 밀면 탄과 숫자가 그 자리에 남아 캐릭터와 따로 논다 —
        /// 방이 화면보다 길어진 뒤로 최대 400px 까지 어긋났다.
        /// </summary>
        private void ApplyScroll()
        {
            // 정수로 맞춰 놓지 않으면 픽셀 그림이 매 프레임 미세하게 흔들린다.
            // 화면 흔들림(`_shakeOffset`)은 여기에 얹는다 — 모든 레이어가 한 값을 쓰므로
            // 여기 한 곳만 더하면 바닥·유닛·탄·글자가 통째로 같이 흔들린다.
            var at = _scroll + _shakeOffset;
            Place(_unitLayer, at);
            Place(_shotLayer, at);
            Place(_textLayer, at);
            Place(_fieldLayer, at);
            Place(_pyStage, at);
            Place(_floor, at);
            // 방 밖 그림도 방과 한 몸으로 움직인다 — 바닥은 방 아랫변 밑, 구름은 방 윗변 위.
            Place(_apron, new Vector2(at.x, at.y - _roomSize.y));
            Place(_cloud, new Vector2(at.x, at.y - RoomCloudDrop));
        }

        /// <summary>
        /// 방 레이어 하나를, 줌이 없을 때 있어야 할 자리(`at`)에서 **캐릭터 자리를 중심으로**
        /// 줌만큼 당겨 놓는다. 레이어는 전부 창 왼쪽 위에 앵커가 걸려 있어 한 식으로 된다
        /// (피벗은 달라도 된다 — 구름은 아래 피벗이다).
        /// </summary>
        private void Place(RectTransform layer, Vector2 at)
        {
            if (layer == null) return;
            var f = ViewFocus;
            var p = f + _zoom * (at - f);
            // 정수로 맞춰 놓지 않으면 픽셀 그림이 매 프레임 미세하게 흔들린다.
            layer.anchoredPosition = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
            layer.localScale = new Vector3(_zoom, _zoom, 1f);
        }

        /// <summary>
        /// 캐릭터를 창의 이 높이(위에서부터 비율)에 둔다 — 가운데보다 살짝 위.
        /// 아래쪽 조작 버튼과 그것을 쥔 엄지에서 멀어진다(기획 2026-09-11, 궁수의 전설 방식).
        /// </summary>
        private const float CameraAnchor = 0.45f;

        /// <summary>
        /// 벽 보스 아레나는 카메라를 세운다. 벽이 방 꼭대기 2 m 를 가로지르는데
        /// 카메라가 캐릭터를 따라 내려가면 벽이 화면 밖으로 나가 머리가 허공에서 튀어나온다.
        /// </summary>
        private bool CameraLocked => _roomBoss != null && _roomBoss.State == BossState.Walls;

        /// <summary>
        /// 카메라 자리. 캐릭터를 창 `CameraAnchor` 높이에 둔다.
        ///
        /// ⚠ 세로는 **방 밖까지** 민다 — 방이 창보다 짧아도 따라간다. 방 위 바깥은 구름,
        ///   아래 바깥은 방 밖 바닥이 가리므로 **그 그림이 닿는 데까지만** 민다
        ///   (`RoomCloudReach` · `RoomApronSize.y`). 더 밀면 그림 너머 빈칸이 보인다.
        /// </summary>
        private Vector2 WantScroll(Unit a)
        {
            float viewW = _field.rect.width, viewH = _field.rect.height;
            // 줌이 걸리면 창에 담기는 방은 그만큼 좁다. 캐릭터 자리(`ViewFocus`)를 중심으로 당기므로
            // 창의 네 변은 줌 없는 좌표로 f + (변 − f) ÷ 줌 자리다. 줌 1 이면 예전 범위와 같다.
            var f = ViewFocus;
            float z = Mathf.Max(_zoom, 0.01f);
            float left = f.x - f.x / z, right = f.x + (viewW - f.x) / z;
            float top = f.y - f.y / z, bottom = f.y - (viewH + f.y) / z;

            // 가로는 **음수 방향**으로 민다. 방 좌표는 오른쪽이 +x 인데 창을 왼쪽으로
            // 밀어야 오른쪽이 보인다. 세로와 부호가 반대라 헷갈리기 쉬운 자리다.
            // 줌 1 이면 창 폭 = 방 폭이라 늘 0 — 당겼을 때만 좌우로 따라간다.
            float xLo = right - _roomSize.x;
            float x = Mathf.Clamp(f.x - a.Position.x, Mathf.Min(xLo, left), left);
            if (CameraLocked) return new Vector2(x, 0f);

            float lo = top - RoomCloudReach;                        // 방 윗변이 이만큼 내려온다
            float hi = _roomSize.y + bottom + RoomApronSize.y;      // 방 아랫변이 이만큼 올라간다
            float y = Mathf.Clamp(f.y - a.Position.y, lo, Mathf.Max(lo, hi));
            return new Vector2(x, y);
        }

        /// <summary>
        /// 방에 들어선 순간의 화면. 흘러가면 안 된다 —
        /// `SetRoomSize` 가 0으로 돌려놓은 뒤 부드럽게 따라가면 방마다 화면이
        /// 위에서 아래로 주르륵 미끄러진다("갑자기 내려갔다 올라오는" 것의 정체).
        /// </summary>
        private void SnapCamera()
        {
            var a = Avatar;
            if (a == null || _unitLayer == null) return;
            // 방에 들어선 순간은 줌도 제자리 — 방마다 스르륵 당겨지는 연출이 생기면 안 된다.
            _zoom = WantZoom();
            _scroll = WantScroll(a);
            ApplyScroll();
        }

        /// <summary>
        /// 지금 창에 보이는가. 방이 화면보다 길어져 생긴 판정이다.
        /// 가장자리에서 깜빡이지 않도록 한 칸 여유를 둔다.
        /// </summary>
        private bool IsOnScreen(Unit u)
        {
            if (u == null) return false;
            const float Margin = 60f;
            var v = RoomToView(u.Position);        // 창 기준 좌표(0 이 위, 아래로 음수 · 0 이 왼쪽)
            return v.y <= Margin && v.y >= -_field.rect.height - Margin
                && v.x >= -Margin && v.x <= _field.rect.width + Margin;
        }

        private Unit NewUnit(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_unitLayer, false);
            return go.AddComponent<Unit>();
        }

        private Sprite GetSprite(string n) => _atlas != null ? _atlas.GetSprite(n) : null;

        // 빙의 표식 그림 4종 (기획서 1-5 A). `SpriteAtlas.GetSprite` 는 부를 때마다
        // 새 Sprite 를 만들어 준다 — 매 프레임 부르면 그대로 쌓인다. 한 번만 받아 둔다.
        private Sprite _markReady, _markTarget, _markBanned, _markLocked, _markArrow;
        private Sprite _markNextBody1, _markNextBody2;

        /// <summary>다음 몸 표식은 두 장을 번갈아 보여 준다. 가만히 있으면 눈에 안 들어온다.</summary>
        private const float NextBodyBlink = 0.4f;

        private void CachePossessMarkSprites()
        {
            Unit.SetShieldFillSprite(GetSprite("hostshieldfill"));

            _markReady = GetSprite("possessmark_ready");
            _markTarget = GetSprite("possessmark_target");
            _markBanned = GetSprite("possessmark_banned");
            _markLocked = GetSprite("possessmark_locked");
            _markArrow = GetSprite("possessmark_arrow");
            _markNextBody1 = GetSprite("possessmark_nextbody_1");
            _markNextBody2 = GetSprite("possessmark_nextbody_2");

            // 문 4장도 여기서 한 번만 받는다. 여는 연출이 매 프레임 그림을 바꾸므로
            // 그때마다 GetSprite 를 부르면 0.5 초 동안 새 Sprite 가 30개 쌓인다.
            _exitClosed = GetSprite(ExitClosedKey);
            _exitOpen1 = GetSprite(ExitOpen1Key);
            _exitOpen2 = GetSprite(ExitOpen2Key);
            _exitOpened = GetSprite(ExitOpenKey);
        }

        private Sprite _exitClosed, _exitOpen1, _exitOpen2, _exitOpened;

        /// <summary>스프라이트 이름 → 캐릭터 아틀라스 키. `unit_boss` → `boss`.</summary>
        private static string UnitKeyOf(string spriteName)
            => spriteName != null && spriteName.StartsWith("unit_") ? spriteName.Substring(5) : spriteName;

        /// <summary>캐릭터 스프라이트 조회. 아틀라스가 안 올라와 있으면 null 이다.</summary>
        /// <summary>
        /// 잡몹 그림. 방향 5장짜리 전용 아틀라스가 먼저고, 없으면 공용 아틀라스를 본다.
        ///
        /// 십자 포탑이 그렇다 — 그림(`obj_turret`)이 무대 소품으로 이미 `ingamemainui`
        /// 아틀라스에 들어 있다. 안 움직이고 방향도 없으니 방향 5장을 새로 그릴 이유가 없다.
        /// </summary>
        private Sprite TrashSprite(HostEntry e)
            => e == null ? null : UnitGet(e.SpriteKey) ?? GetSprite(e.SpriteKey);

        private Sprite UnitGet(string key, string suffix = null)
        {
            if (key == null || !_unitAtlas.TryGetValue(key, out var atlas) || atlas == null) return null;
            if (suffix != null) return atlas.GetSprite($"unit_{key}_{suffix}");

            // 방향 없는 기본 그림은 방향 5장이 붙기 전 한 프레임 동안만 쓰인다.
            // 없으면 정면(s)으로 대신한다 — 이것 때문에 통째로 안 보이면 손해가 크다.
            return atlas.GetSprite($"unit_{key}") ?? atlas.GetSprite($"unit_{key}_s");
        }

        /// <summary>
        /// 이 런에서 쓸 캐릭터 아틀라스를 미리 올린다.
        /// 스폰은 동기 코드라 이 시점에 다 올라와 있어야 한다 — 늦으면 그림 없이 스폰된다.
        /// </summary>
        /// <summary>한 번에 띄우는 아틀라스 수. 너무 크게 잡으면 진행 표시가 뭉텅이로 뛴다.</summary>
        private const int AtlasBatch = 5;

        /// <summary>
        /// 표 하나를 띄운다. 실패해도 판을 멈추지 않는다 —
        /// 없는 표는 그 기능만 빠지고 나머지는 돈다.
        /// </summary>
        private static async UniTask LoadTableAsync<T>(IResourceManager res, string address,
                                                       Action<T> assign, string failMessage,
                                                       bool warnOnly = false) where T : UnityEngine.Object
        {
            try { assign(await res.LoadAsync<T>(address)); }
            catch (Exception e)
            {
                if (warnOnly) Debug.LogWarning($"[Battle] {failMessage}. {e.Message}");
                else Debug.LogError($"[Battle] {failMessage} — {e.Message}");
            }
        }

        private async UniTask LoadUnitAtlasesAsync(IResourceManager res, List<string> keys)
        {
            // ⚠ **한 장씩 기다리면 안 된다.** 캐릭터 하나가 아틀라스 하나라
            //   스무 장 가까이 되는데, 순서대로 await 하면 그 시간이 전부 더해진다
            //   (한 장 0.2초면 스무 장에 4초). 진입 시 검은 화면의 정체가 이것이었다.
            //   다섯 장씩 한꺼번에 띄우고 그 묶음만 기다린다.
            var batch = new List<UniTask>(AtlasBatch);
            for (int i = 0; i < keys.Count; i += AtlasBatch)
            {
                batch.Clear();
                int end = Mathf.Min(i + AtlasBatch, keys.Count);
                for (int j = i; j < end; j++)
                {
                    var key = keys[j];
                    if (string.IsNullOrEmpty(key) || _unitAtlas.ContainsKey(key)) continue;
                    batch.Add(LoadOneUnitAtlasAsync(res, key));
                }
                if (batch.Count > 0) await UniTask.WhenAll(batch);
                ReportLoading(end, keys.Count);
            }
            ReportLoading(keys.Count, keys.Count);
        }

        private async UniTask LoadOneUnitAtlasAsync(IResourceManager res, string key)
        {
            try { _unitAtlas[key] = await res.LoadAsync<SpriteAtlas>(UnitAtlasPrefix + key); }
            catch (Exception e)
            {
                // 한 종이 없다고 런을 멈추지 않는다 — 그 캐릭터만 그림 없이 나온다.
                Debug.LogError($"[Battle] 캐릭터 아틀라스 로드 실패 unit_{key} — {e.Message}");
            }
        }

        /// <summary>
        /// 가림막에 캐릭터 로드 진행을 알린다. 씬 로드가 앞 60%를 쓰므로 뒤 40%가 우리 몫이다
        /// (`Game.Module.Common.LoadingFlowModule.SceneLoadShare`).
        /// </summary>
        private void ReportLoading(int done, int total)
        {
            if (_loading == null || total <= 0) return;
            float t = Mathf.Clamp01((float)done / total);
            _loading.SetProgress(Game.Module.Common.LoadingFlowModule.SceneLoadShare
                                 + (1f - Game.Module.Common.LoadingFlowModule.SceneLoadShare) * t);
            // 몇 장째인지는 막대와 퍼센트가 말한다. 가운데 한 줄은 가림막이 알아서 굴린다.
        }

        /// <summary>
        /// 방 <paramref name="index"/> 의 <paramref name="i"/> 번째 적이 쓸 호스트.
        /// 미리 올릴 아틀라스를 고를 때와 실제로 스폰할 때가 반드시 같아야 하므로
        /// 뽑는 식을 한 곳에만 둔다 — 갈라지면 그림 없는 적이 나온다.
        /// </summary>
        private static HostEntry EnemyAt(IReadOnlyList<HostEntry> hosts, int index, int i)
            => hosts[(i * 5 + index * 3 + 1) % hosts.Count];

        // ─────────────────────────────────────────────────────────
        // 잡몹
        //
        // 정본 `ROOM_SPAWN` 은 자리마다 역할을 적어 두었다 —
        // `POSSESSION_TARGET`(46) 은 빼앗을 몸, `STANDARD`·`REINFORCEMENT`(299) 는 그냥 적이다.
        // 그런데 정본 `ENEMY_RUNTIME` 의 적 23종은 **전부 호스트 23종과 1:1** 이라,
        // 역할이 뭐든 화면에는 사람 모양 엘리트만 나왔다. 뺏을 수 있는 놈과 없는 놈이
        // 똑같이 생겨서 구별이 안 되고, 그래서 "다 엘리트라 의미가 없는" 화면이 됐다.
        //
        // 이제 `POSSESSION_TARGET` 만 원래 배우로 세우고, 나머지는 잡몹으로 바꾼다.
        // 사람 = 빼앗을 수 있다 / 뼈·짐승 = 없다. 실루엣만으로 갈린다.
        // ─────────────────────────────────────────────────────────

        private const string TrashSkeletonKey = "skeleton";
        private const string TrashBatKey = "bat";

        /// <summary>세 기 중 한 기를 박쥐로. 나머지는 해골이다.</summary>
        private const int BatEveryNth = 3;

        private static HostEntry s_skeleton;
        private static HostEntry s_bat;

        /// <summary>
        /// 해골 — 벽. 느리고 약하지만 길을 막는다.
        /// 유령이 됐을 때 이 벽을 뚫고 다음 숙주까지 가야 한다.
        /// </summary>
        private static HostEntry Skeleton => s_skeleton ??= HostEntry.CreateTrash(
            TrashSkeletonKey, "해골", AttackKind.Melee,
            hp: 28, atk: 6, moveMps: 1.2f, engageMps: 2.4f,
            rangeMeters: 1.4f, interval: 1.4f, telegraph: 0.45f);

        /// <summary>
        /// 박쥐 — 추격. 몸이 있을 땐 성가신 정도지만,
        /// 유령이 되면 도주선을 끝까지 따라붙는 진짜 위협이다.
        /// </summary>
        private static HostEntry Bat => s_bat ??= HostEntry.CreateTrash(
            TrashBatKey, "박쥐", AttackKind.Melee,
            hp: 18, atk: 4, moveMps: 2.6f, engageMps: 4.6f,
            rangeMeters: 1.2f, interval: 0.9f, telegraph: 0.25f);

        private const string TrashEnforcerKey = "actor_enforcer";
        private static HostEntry s_enforcer;

        /// <summary>
        /// 집행자 — 무겁다. 느리지만 한 대가 아프고 잘 안 죽는다.
        ///
        /// 그림 40장이 **이미 프로젝트에 들어와 있었는데 아무 데서도 안 쓰고 있었다.**
        /// 캔버스가 128×128 로 해골·박쥐(96)보다 한 등급 크다 — 줄이지 않고 그대로 쓴다.
        /// 덩치가 곧 "저건 밀고 들어오는 놈" 이라는 신호가 된다.
        /// </summary>
        private static HostEntry Enforcer => s_enforcer ??= HostEntry.CreateTrash(
            TrashEnforcerKey, "집행자", AttackKind.Melee,
            hp: 52, atk: 9, moveMps: 1.0f, engageMps: 2.0f,
            rangeMeters: 1.6f, interval: 1.7f, telegraph: 0.6f);

        private const string TrashGunnerKey = "scrapgunner";
        private static HostEntry s_gunner;

        /// <summary>
        /// 폐품 사수 — **첫 원거리 잡몹**이다.
        ///
        /// 그 전까지 잡몹은 해골·박쥐 둘뿐이었고 **둘 다 근접**이라, 방에 들어가면
        /// 언제나 "달려오는 것을 상대하는" 한 가지 문제만 나왔다. 쏘는 놈이 섞이면
        /// 붙는 것과 피하는 것을 동시에 해야 한다 — 엄폐물이 그제서야 값을 가진다.
        ///
        /// 사거리 5.2 m 는 **숙주 원거리(7.0~8.5 m)보다 짧다.** 잡몹이 숙주보다 멀리
        /// 쏘면 몸을 빼앗을 이유가 줄어든다. 예비동작 0.75초로 길게 잡아 피할 틈을 남긴다.
        /// </summary>
        private static HostEntry Scrapgunner => s_gunner ??= HostEntry.CreateTrash(
            TrashGunnerKey, "폐품 사수", AttackKind.Single,
            hp: 24, atk: 7, moveMps: 1.4f, engageMps: 2.2f,
            rangeMeters: 5.2f, interval: 1.9f, telegraph: 0.75f);

        private const string TrashWardenKey = "roadwarden";
        private const string TrashCoilKey = "coilwalker";
        private static HostEntry s_warden, s_coil;

        /// <summary>
        /// 순찰기 — CH2 원거리. 미사일 기지와 밤거리를 같이 도는 경광등 로봇.
        ///
        /// 폐품 사수보다 **무겁고 느리지만 한 발이 세다.** 챕터가 깊어질수록
        /// 쏘는 놈이 강해지되, 예비동작은 그대로 길게 둔다 —
        /// 원거리가 늘어난 만큼 피할 틈까지 줄면 화면이 탄으로 덮인다.
        /// </summary>
        private static HostEntry Roadwarden => s_warden ??= HostEntry.CreateTrash(
            TrashWardenKey, "순찰기", AttackKind.Single,
            hp: 34, atk: 9, moveMps: 1.1f, engageMps: 1.9f,
            rangeMeters: 5.6f, interval: 1.8f, telegraph: 0.75f,
            // 자리를 지키고 각도로 덮는다(FAN). 사거리 5.6 m 에 40° 면 부채꼴 끝 폭이
            // 약 3.8 m — 방 폭의 3분의 1이다. 정면이면 2발, 옆으로 비키면 1발.
            shotCount: 3, spreadDegrees: 40f);

        /// <summary>
        /// 코일 보행기 — CH3 원거리. 옥상과 정유소의 삼각다리 자동기계.
        ///
        /// 셋 중 가장 **멀리서 자주** 쏜다. 대신 제일 물러서 붙으면 금방 부서진다 —
        /// 3챕터쯤이면 "쏘는 놈부터 끊는다" 가 몸에 배어 있어야 한다.
        /// </summary>
        private static HostEntry Coilwalker => s_coil ??= HostEntry.CreateTrash(
            TrashCoilKey, "코일 보행기", AttackKind.Single,
            hp: 26, atk: 10, moveMps: 1.6f, engageMps: 2.4f,
            rangeMeters: 6.2f, interval: 2.4f, telegraph: 0.7f);


        /// <summary>
        /// 한 웨이브에 세우는 몸의 최대 수. 넘치는 자리는 잡몹으로 돌린다.
        /// 전원이 빼앗을 수 있는 몸이면 빼앗는 것이 선택이 아니라 기본값이 된다.
        /// </summary>
        private const int MaxHostsPerWave = 2;

        /// <summary>
        /// 방마다 적을 **한 기만** 세운다. 배치·진행을 빠르게 훑어보기 위한 테스트 스위치다.
        ///
        /// 에디터 메뉴 `Tools/Game/테스트 — 방당 몹 1기` 로 켜고 끈다.
        /// 빌드에는 없다(`UNITY_EDITOR` 밖에서는 언제나 false) — 켜 둔 채 나가는 사고를 막는다.
        /// </summary>
        /// <summary>
        /// CH1 열두 방 안에서 **여섯 테마를 차례로** 보여 준다.
        /// 배경만 확인하려고 48방을 끝까지 깨지 않아도 되게 하는 스위치다.
        ///
        /// 에디터 메뉴 `Tools/Game/테스트 — 1챕터에서 테마 6종 다 보기` 로 켜고 끈다.
        /// 빌드에는 없다.
        /// </summary>
        public static bool CycleThemesInChapter1
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.CycleThemesCh1", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.CycleThemesCh1", value);
#else
            get => false;
            set { }
#endif
        }

        public static bool OneEnemyPerRoom
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.OneEnemyPerRoom", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.OneEnemyPerRoom", value);
#else
            get => false;
            set { }
#endif
        }

        /// <summary>
        /// **방 1~6 에 보스를 하나씩 세운다.** 여섯 보스를 한 판에서 차례로 보기 위한
        /// 테스트 스위치다 — 정상 진행으로는 여섯째 보스까지 48방을 깨야 한다.
        ///
        ///   001 로봇스네이크 · 002 크러셔 · 003 파이썬 · 004 슬러지 · 005 가디언 · 006 킹핀
        ///
        ///   ⚠ 이것이 **원작 순서**다(사용자 원작 스샷 대조 2026-09-07).
        ///     킹핀이 최종 보스다. 표 순서 = 챕터 순서 = 여기 번호다.
        ///
        /// 방 데이터(`RoomTable`)는 건드리지 않는다. `BossTable` 이 체력·공격력·패턴을
        /// 다 들고 있으므로 그것만 읽어 세운다 — 껐다 켜는 것으로 원래대로 돌아간다.
        ///
        /// 에디터 메뉴 `Tools/Game/테스트 — 방 1~6 에 보스 하나씩` 로 켜고 끈다.
        /// 빌드에는 없다(`UNITY_EDITOR` 밖에서는 언제나 false).
        /// </summary>
        public static bool BossPerRoomTest
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.BossPerRoomTest", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.BossPerRoomTest", value);
#else
            get => false;
            set { }
#endif
        }

        /// <summary>
        /// 보스 패턴 쿨다운에 곱하는 값. 1 이면 정본 그대로다.
        ///
        /// 정본 쿨은 8~20초라 **한 판에 네 패턴을 다 보기가 어렵다.**
        /// 첫 보스는 그 전에 죽어서 두 개만 보고 끝난다 — 확인이 안 된다.
        /// 이 스위치를 켜면 절반으로 줄어 한 판에 다 나온다.
        ///
        /// ⚠ **정본 값을 고치는 것이 아니다.** `BossDefTable` 의 숫자는 그대로 있고
        ///   여기서만 곱한다. 끄면 즉시 정본 속도로 돌아온다. 빌드에는 없다.
        /// 에디터 메뉴 `Tools/Game/테스트 — 보스 쿨 절반` 으로 켜고 끈다.
        /// </summary>
        public static bool BossHalfCooldown
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.BossHalfCooldown", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.BossHalfCooldown", value);
#else
            get => false;
            set { }
#endif
        }

        /// <summary>보스 두뇌가 쿨다운에 곱하는 값. 스위치가 꺼져 있으면 1 이다.</summary>
        public static float BossCooldownMul => BossHalfCooldown ? 0.5f : 1f;

        /// <summary>
        /// **연출 확인용 — 네 패턴이 일정 간격으로 무작위로 하나씩 나간다.**
        ///
        /// 패턴별 쿨(8~20초)·거리 조건·묶음을 전부 무시하고, 한 개의 시계만 굴려
        /// 그때그때 하나를 뽑는다. 「스킬 버튼」(`BossIdleOnly`)은 무엇이 나올지
        /// 내가 고르는 것이라 **조건대로 도는 흐름**은 확인할 수 없었다 —
        /// 이 스위치는 보스가 스스로 돌되 네 개를 골고루 보여 준다.
        ///
        /// ⚠ 정본 값을 고치는 것이 아니다. 끄면 즉시 표대로 돌아온다.
        /// 에디터 메뉴 `Tools/Game/테스트 — 보스 스킬 무작위로 3초마다` 로 켜고 끈다.
        /// </summary>
        public static bool BossRandomEvery
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.BossRandomEvery", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.BossRandomEvery", value);
#else
            get => false;
            set { }
#endif
        }

        /// <summary>
        /// 위 스위치가 켜졌을 때 패턴 사이 간격(초).
        ///
        /// ⚠ 이름에 숫자를 박지 않는다. 2초로 시작했다가 **너무 잦아서** 3초로
        ///   올렸는데, 이름이 `...Every2s` 였으면 그 순간부터 거짓말이 된다.
        /// </summary>
        public const float BossRandomEverySeconds = 3f;

        /// <summary>
        /// **벽 보스의 머리를 계속 내놓는다.**
        ///
        /// 파이썬은 나왔다(0.4) → 때리고(1.0) → 들어갔다(0.4) → 미끄러진다(0.8) 를
        /// 반복한다. 한 바퀴 2.6초 중 **패턴이 나갈 수 있는 것은 1초뿐**이라,
        /// 시험 간격을 줄여도 실제 간격은 2.6~5초로 벌어진다.
        /// 연출만 빠르게 훑어볼 때 이 스위치를 켜면 머리가 안 들어가고
        /// 시계 그대로 나간다.
        ///
        /// ⚠ 이 리듬은 이 보스의 정체다. **확인용이지 기본값이 아니다.**
        /// 에디터 메뉴 `Tools/Game/테스트 — 벽 보스 머리 계속 내놓기` 로 켜고 끈다.
        /// </summary>
        public static bool BossWallStayOut
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.BossWallStayOut", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.BossWallStayOut", value);
#else
            get => false;
            set { }
#endif
        }

        /// <summary>
        /// 보스 체력 배수. 1 이면 표 그대로다.
        ///
        /// 페이즈 2·3 패턴은 체력이 60%·30% 아래로 내려가야 나온다. 그런데 가디언은
        /// 머리가 열린 뒤 피해가 2배라 **실질 체력이 1700** 이고, 네 패턴을 보기 전에
        /// 끝난다 — 실제로 「똬리」와 「머리 물기」를 못 보고 죽었다는 보고가 왔다.
        ///
        /// ⚠ **표 값을 고치는 것이 아니다.** `BossDefTable` 의 숫자는 그대로 있고
        ///   여기서만 곱한다. 끄면 즉시 정본 체력으로 돌아온다. 빌드에는 없다.
        /// 에디터 메뉴 `Tools/Game/테스트 — 보스 체력 10배` 로 켜고 끈다.
        /// </summary>
        public static bool BossDoubleHp
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.BossDoubleHp", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.BossDoubleHp", value);
#else
            get => false;
            set { }
#endif
        }

        /// <summary>보스 체력에 곱하는 값. 스위치가 꺼져 있으면 1 이다.</summary>
        public static float BossHpMul => BossDoubleHp ? 10f : 1f;

        /// <summary>
        /// 보스가 **스스로는 아무것도 안 한다.** 스킬을 손으로 눌러 하나씩 볼 때 쓴다.
        ///
        /// 쿨다운이 돌면 확인하려는 패턴 위에 다른 패턴이 겹쳐서, 무엇을 보고 있는지
        /// 알 수 없다. 켜 두면 쫓아오지도, 때리지도, 패턴을 고르지도 않는다 —
        /// 시험 버튼(`BattleDirector.TestGui.cs`)으로 부른 것만 돈다.
        ///
        /// ⚠ 켜 둔 채로 잊으면 **보스가 영영 아무것도 안 한다.** 확인이 끝나면 끈다.
        /// 에디터 메뉴 `Tools/Game/테스트 — 보스 가만히 (버튼으로만)` 로 켜고 끈다.
        /// </summary>
        public static bool BossIdleOnly
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.BossIdleOnly", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.BossIdleOnly", value);
#else
            get => false;
            set { }
#endif
        }

        /// <summary>
        /// 한 보스만 계속 세운다. 0 이면 방 순서대로 여섯을 돌린다.
        ///
        /// 순서대로만 되면 **여섯째 보스를 보려고 다섯 방을 깨야 한다.**
        /// 하나를 오래 들여다보려면 그 하나가 계속 나와야 한다.
        /// </summary>
        public static int BossPickIndex
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetInt("AVSR.BossPickIndex", 0);
            set => UnityEditor.EditorPrefs.SetInt("AVSR.BossPickIndex", value);
#else
            get => 0;
            set { }
#endif
        }

        /// <summary>테스트 모드에서 이 방이 세울 보스. 아니면 null.</summary>
        /// <summary>이 방의 보스를 정한다. 테스트로 지정한 것이 있으면 그것이 이긴다.</summary>
        private BossEntry ResolveBossDef(BossEntry forced)
        {
            if (forced != null) return forced;
            if (_bossTable == null) return null;
            string key = _canonRoom != null && _canonRoom.IsBoss ? BossSlug(_canonRoom.BossId) : null;
            int chapter = _runChapter;
            return _bossTable.ByKey(key) ?? _bossTable.ForChapter(chapter);
        }

        private BossEntry TestBossFor(int index)
        {
            if (!BossPerRoomTest || _bossTable == null) return null;
            var all = _bossTable.Entries;

            // 하나를 골라 뒀으면 **어느 방이든** 그 보스다.
            int pick = BossPickIndex;
            if (pick >= 1 && pick <= all.Count) return all[pick - 1];

            return index >= 0 && index < all.Count ? all[index] : null;
        }

        /// <summary>이번 웨이브에서 몸이 될 스폰 인덱스. 매번 재사용한다(hot path 는 아니지만 습관).</summary>
        private readonly List<int> _hostSlots = new();

        /// <summary>이 런이 건드릴 수 있는 캐릭터 키를 모은다.</summary>
        private List<string> RunUnitKeys()
        {
            int chapter = _runChapter;

            // 잡몹은 챕터마다 짝이 다르다(`TrashKeysFor`). **여섯 챕터 것을 다 올린다.**
            //
            // ⚠ 예전에는 현재 챕터 것만 올렸다. 그런데 판은 언제나 CH1 에서 시작해
            //   CH6 까지 **한 판으로 이어 간다** — 아틀라스를 올리는 자리는 판 시작
            //   한 곳뿐이라, CH2 에 들어서면 순찰기·집행자가 **흰 사각형**으로 섰다
            //   (2026-09-08 확인). 아래 보스 목록이 이미 같은 이유로 챕터를 안 가른다.
            //   잡몹은 여섯 종뿐이고 챕터마다 셋씩 겹치므로 실제로 세 장이 더 붙는다.
            // 골렘은 잡몹도 호스트도 아니다 — 영매가 **불러내는** 몸이라 방 어디에도 안 서 있다.
            // 여기 안 넣으면 아틀라스가 안 올라오고, 그림이 없으면 소환 자체가 조용히 취소된다.
            var keys = new List<string> { "ghost", GolemKey };
            // ⚠ 전용 아틀라스가 있는 것만 올린다. 십자 포탑은 공용 아틀라스를 쓰므로
            //   여기 넣으면 `obj_turret` 이라는 없는 아틀라스를 부르다 실패가 쌓인다.
            for (int c = 1; c <= ChapterCount; c++)
                foreach (var k in TrashKeysFor(c))
                    if (k != TrashCrossKey && !keys.Contains(k)) keys.Add(k);
            var bossDef = _bossTable != null ? _bossTable.ForChapter(chapter) : null;
            keys.Add(UnitKeyOf(bossDef != null ? bossDef.SpriteName : "unit_boss"));
            // 그림이 하나도 안 걸렸을 때를 위한 마지막 대비책
            keys.Add("boss");

            // 이 챕터에서 만날 보스는 **둘**이다(중간·최종). 방마다 다른 몸이므로
            // 챕터 대표 하나만 올리면 나머지 하나가 흰 사각형으로 선다.
            // 아직 안 온 보스는 빌려 쓸 몸까지 함께 올린다.
            if (_rooms != null)
                for (int i = 0; i < _rooms.Rooms.Count; i++)
                {
                    var r = _rooms.Rooms[i];
                    // ⚠ 챕터로 거르지 않는다. 한 런이 1→2→3 을 이어서 가므로
                    //   현재 챕터의 보스만 올리면 다음 챕터 보스가 흰 사각형으로 선다.
                    if (r == null || !r.IsBoss) continue;
                    // `BossStand` 가 곧 "아직 그림이 없는 보스" 목록이다 —
                    // 제 이름과 다른 몸을 돌려주면 그 보스는 아직 그림이 없다는 뜻이다.
                    // 없는 아틀라스를 미리 부르면 로드 실패가 콘솔에 쌓인다.
                    var slug = BossSlug(r.BossId);
                    var key = BossStand(slug);
                    if (!keys.Contains(key)) keys.Add(key);
                }

            // 빙의로 몸을 갈아타도 로비에서 고른 호스트는 긴급 투입으로 나올 수 있다.
            var emergency = PickPlayerHost();
            if (emergency != null) keys.Add(emergency.SpriteKey);

            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return keys;

            // 정본 경로가 있으면 그 길에 실제로 나오는 배우만 미리 받는다.
            // 전부 받으면 쓰지도 않을 아틀라스가 딸려 온다.
            if (_rooms != null && _rooms.Get(FirstCanonRoom) != null)
            {
                var id = FirstCanonRoom;
                int guard = 0;
                while (!string.IsNullOrEmpty(id) && guard++ < 64)
                {
                    var room = _rooms.Get(id);
                    if (room == null) break;
                    for (int i = 0; i < room.Spawns.Count; i++)
                    {
                        var e = ActorProfile(room.Spawns[i].ActorId, hosts);
                        if (e != null && !keys.Contains(e.SpriteKey)) keys.Add(e.SpriteKey);
                    }
                    id = room.Exits.Count > 0 ? room.Exits[0].NextRoomId : null;
                }
                return keys;
            }

            // 방 종류(일반·정예)에 따라 마릿수가 달라지므로 둘 중 많은 쪽까지 훑는다.
            for (int index = 0; index < _config.StagesPerChapter; index++)
            {
                int count = SandboxCount(Mathf.Max(_config.EliteEnemyCount, _config.EnemiesPerRoom(index)));
                for (int i = 0; i < count; i++)
                {
                    var key = EnemyAt(hosts, index, i).SpriteKey;
                    if (!keys.Contains(key)) keys.Add(key);
                }
            }
            return keys;
        }

        /// <summary>
        /// 방향 스프라이트 5장을 찾아 붙인다. 하나라도 없으면 붙이지 않는다 —
        /// 없는 방향만 원래 그림으로 나오면 캐릭터가 방향마다 바뀌어 보인다.
        /// 12종을 한 번에 만들지 않고 한 종씩 넣어 볼 수 있어야 해서 이렇게 둔다.
        /// </summary>
        /// <summary>
        /// 걷기 그림을 쓰지 않는 종. 지금은 비어 있다.
        ///
        /// `medium` 이 한때 여기 있었다 — 로브 종이라 걷기를 "로브가 벌어지는 것" 으로
        /// 그려서 실루엣이 통째로 바뀌었다. 재작업분(큐 38)이 실루엣을 그대로 두고
        /// 아랫단만 물결치게 고쳐 와서 뺐다.
        ///
        /// 같은 사고가 또 나면 여기에 넣고 재작업을 걸면 된다 — 그림 하나 때문에
        /// 그 캐릭터를 통째로 못 쓰게 두지 않기 위한 자리다.
        /// </summary>
        private static readonly HashSet<string> NoWalkFrames = new();

        private void ApplyFacingSprites(Unit u, string key)
        {
            var sets = new Sprite[Unit.FrameSuffix.Length][];
            for (int f = 0; f < sets.Length; f++)
                sets[f] = FrameSet(key, Unit.FrameSuffix[f]);
            if (sets[Unit.FrameIdle] == null) return;   // 방향 그림이 없는 종은 지금 그림 그대로 둔다

            if (NoWalkFrames.Contains(key))
                sets[Unit.FrameWalk1] = sets[Unit.FrameWalk2] = null;

            u.SetFacingSprites(sets);
        }

        /// <summary>한 동작의 방향 5장. 하나라도 없으면 null — 반쪽짜리는 안 쓴다.</summary>
        private Sprite[] FrameSet(string key, string frame)
        {
            var set = new Sprite[Unit.FacingSuffix.Length];
            for (int i = 0; i < set.Length; i++)
            {
                var suffix = frame == null
                    ? Unit.FacingSuffix[i]
                    : $"{Unit.FacingSuffix[i]}_{frame}";
                set[i] = UnitGet(key, suffix);
                if (set[i] == null) return null;
            }
            return set;
        }

        /// <summary>HUD 초상용. 아틀라스를 들고 있는 쪽이 하나뿐이라 여기서 내준다.</summary>
        public Sprite UnitSprite(string hostKey) => UnitGet(hostKey);

        /// <summary>
        /// 그 몸의 액티브 스킬 아이콘. 인게임 버튼이 21종 다 같은 그림을 쓰고 있어서
        /// 어떤 액티브 스킬을 들고 있는지가 화면에 안 보였다.
        ///
        /// 아이콘은 호스트 선택 화면 아틀라스에 있다. 인게임에서 쓰려면 그 아틀라스를
        /// 함께 올려야 하므로 여기서 늦게 한 번만 불러온다.
        /// </summary>
        /// <summary>이 챕터·방의 무대 이름. 값은 `GameConfig` 가 갖는다.</summary>
        public string StageNameOf(int chapter, int room)
            => _config != null ? _config.StageNameOf(chapter, room) : string.Empty;

        public Sprite ActiveSkillIcon(string hostKey)
            => _panelAtlas != null && hostKey != null
                ? _panelAtlas.GetSprite($"ultimateicon_{hostKey}") : null;

        private SpriteAtlas _panelAtlas;

        private async UniTask LoadPanelAtlasAsync(IResourceManager res)
        {
            if (_panelAtlas != null) return;
            try { _panelAtlas = await res.LoadAsync<SpriteAtlas>("atlas/hostselectpanel"); }
            catch (Exception e)
            {
                // 없어도 게임은 돈다 — 버튼이 기본 그림으로 남을 뿐이다
                Debug.LogWarning($"[Battle] 액티브 스킬 아이콘 아틀라스 로드 실패 — {e.Message}");
            }
        }

        /// <summary>
        /// 이 스테이지가 어떤 방인가 (기획서 A 06 ROOM TYPE).
        ///
        /// 마지막은 항상 보스다. 그 앞은 한 챕터 안에서 같은 방만 반복되지 않게
        /// 스테이지 번호로 갈라 준다 — 지금은 3스테이지라 경우의 수가 적다.
        /// 챕터가 길어지면 방 구성표를 데이터로 빼야 한다.
        /// </summary>
        /// <summary>
        /// 이 챕터의 방 수. 정본 경로를 따라가면 CH1 은 10방이다 —
        /// `StagesPerChapter`(3) 는 절차적 생성 시절의 값이라 진행 표시가 어긋난다.
        /// </summary>
        /// <summary>
        /// 이 챕터가 몇 방짜리인가.
        ///
        /// ⚠ 갈림길 때문에 **고정값이 아니다.** 어느 문으로 갔느냐에 따라
        ///   CH2 는 13~15방, CH3 는 16~18방으로 달라진다. 그래서 처음 방부터 세지 않고
        ///   **지금 서 있는 방에서 앞을 센 뒤 지나온 수를 더한다.**
        ///   문을 고를 때마다 총계가 갱신되므로 "12/15" 가 거짓말이 되지 않는다.
        ///   앞을 볼 때는 첫 번째 문을 따라간다 — 아직 안 고른 갈림길은 알 수 없다.
        /// </summary>
        private int RoomTotal
        {
            get
            {
                if (_rooms == null) return _config.StagesPerChapter;

                int passed = _canonRoom != null ? Mathf.Max(0, _roomIndex) : 0;
                var id = _canonRoom != null ? _canonRoom.RoomId : FirstCanonRoom;

                int ahead = 0;
                while (!string.IsNullOrEmpty(id) && ahead < 64)
                {
                    var r = _rooms.Get(id);
                    if (r == null) break;
                    ahead++;
                    id = r.Exits.Count > 0 ? r.Exits[0].NextRoomId : null;
                }
                int n = passed + ahead;
                return n > 0 ? n : _config.StagesPerChapter;
            }
        }

        /// <summary>정본 경로의 끝(보스를 잡은 방)인가.</summary>
        private bool IsLastRoom =>
            _canonRoom != null ? _canonRoom.IsChapterEnd
                               : _roomIndex >= _config.StagesPerChapter - 1;

        private RoomKind KindOf(int index)
        {
            int last = _config.StagesPerChapter - 1;
            if (index >= last) return RoomKind.Boss;
            if (index == 0) return RoomKind.Normal;        // 첫 방은 늘 평범하게 연다
            // 보스 직전은 정예로 조인다. 다만 Ghost HP 가 바닥이면 쉬어 가게 한다.
            if (index == last - 1)
                return _ghostHp <= GhostHpMax / 3 ? RoomKind.Rest : RoomKind.Elite;
            return RoomKind.Normal;
        }

        /// <summary>
        /// 보스 공격력 배수. 1챕터 보스(로봇 스네이크)만 2배(기획 2026-09-15) — 너무 약했다.
        /// 모든 보스 피해가 `boss.Atk` 를 읽으므로 여기 한 곳에서 곱한다.
        /// </summary>
        private static float BossAtkMulOf(string bossKey) => bossKey == "robot_snakes" ? 2f : 1f;

        private void EnterRoom(int index)
        {
            _roomIndex = index;
            DespawnExit();
            _exitOpen = false;
            _exitOpenTime = -1f;
            ClearFields();   // 안 지우면 새 방 바닥에 지난 방 장판이 남는다
            ClearRoomProp(); // 지난 방 제단·가판이 새 방 한복판에 남지 않게
            ClearGoldPiles();   // 못 걷고 나간 골드가 다음 방 바닥에 남지 않게
            _bloodDebtUsed = 0;   // 피의 부채는 방마다 다시 센다
            ClearDeployables();   // 포탑도 방을 따라오지 않는다
            ClearAlly();          // 동료도 마찬가지 — 산 방에서만 같이 싸운다
            // 불러낸 것들도 방을 넘어가지 않는다 — **골렘만 예외**다. 죽을 때까지 따라온다(기획 2026-09-15).
            ClearSummons(keepGolem: true);
            ClearJuice();         // ⚠ 늦춘 시간을 되돌린다. 안 하면 느려진 채로 굳는다
            ClearExitArrows();    // 안내 화살표도 방을 따라오지 않는다
            _echoBlasts.Clear();  // 방을 넘긴 뒤 지난 방 좌표에서 터지면 안 된다
            _rangedKnockAt.Clear();   // 지난 방 몹의 밀림 시각을 들고 가지 않는다
            _barrier = 0;
            _barrierUsedThisRoom = false;   // 위기 방벽은 방마다 한 번 (C018)
            _fightReward = null;            // 지난 방 매복 삯을 들고 넘어가지 않는다
            // 이벤트·상점은 창을 닫아야 출구가 열리므로 정상 진행에서는 이미 비어 있다.
            // 그래도 여기서 지운다 — 죽어서 방을 벗어나는 길이 따로 있고,
            // 남아 있으면 다음 방에서 지난 방 창이 살아 있는 것처럼 보인다.
            _event = null;
            _eventDone = false;
            _shopOpen = false;
            _shopOffers.Clear();
            // 정본 방이 있으면 그것이 이긴다. 없으면 예전 절차적 생성으로 돌아간다 —
            // 34방을 한 번에 갈아 끼우지 않고 한 방씩 옮겨 붙이기 위해서다.
            _canonRoom = _rooms != null ? _rooms.Get(_canonRoomId) : null;
            _roomKind = _canonRoom != null ? KindOfCanon(_canonRoom) : KindOf(index);
            // 테스트 — 방 1~6 을 통째로 보스방으로 돌린다.
            var testBoss = TestBossFor(index);
            if (testBoss != null) _roomKind = RoomKind.Boss;
            bool isBoss = _roomKind == RoomKind.Boss;

            // ⚠ 이 방의 보스를 **여기서 한 번만** 정한다. 바닥·방 크기·무대가 모두 이것을 본다.
            //   따로따로 구하면 어긋난다 — 실제로 테스트로 끼워 넣은 보스방에
            //   그 방 원래 바닥(거리)이 깔려 파이썬이 길바닥에서 싸웠다.
            _roomBoss = isBoss ? ResolveBossDef(testBoss) : null;

            // ⚠ 반드시 `_canonRoom` 을 정한 **뒤에** 부른다. 앞에서 부르면 바닥이
            //   지난 방의 템플릿으로 정해진다 — 방마다 한 칸씩 밀린 그림이 깔린다.
            ApplyRoomFloor();

            // 정본은 보스방만 세로가 16 m 다. 방마다 높이가 달라질 수 있어 여기서 정한다.
            //
            // ⚠ **벽 보스만 화면 한 판짜리 아레나**다. 벽은 방 꼭대기 2 m 를 가로지르는데
            //   방이 13 m 면 카메라가 플레이어를 따라 내려가 벽이 화면 밖으로 나간다 —
            //   벽이 안 보이면 머리가 허공에서 튀어나오는 것으로 보인다.
            //   화면 높이를 그대로 쓰면 세로 스크롤이 0 이라 벽이 늘 붙어 있다.
            //
            // ⚠ 2026-09-11 에 필드가 화면 끝까지 늘었다(카메라 따라가기). 창 높이를 그대로 쓰면
            //   방이 바닥 그림(936)보다 길어져 그 밑이 빈다 — 방 높이에서 자르고, 카메라는 세운다
            //   (`CameraLocked`). 창이 방보다 짧은 화면이면 예전처럼 창 높이.
            SetRoomSize(_roomBoss != null && _roomBoss.State == BossState.Walls
                        ? Mathf.Min(_field.rect.height, WallArenaMeterHeight * _pxPerMeter) / _pxPerMeter
                      : _canonRoom != null ? _canonRoom.Height
                      : isBoss ? BossRoomMeterHeight : RoomMeterHeight);

            // 무대는 방 크기가 정해진 **뒤에** 깐다 — 벽 폭이 방 폭이다.
            ApplyPythonStage(_roomBoss);

            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null) Destroy(_enemies[i].gameObject);
            _enemies.Clear();

            // 이전 방에서 쓰러지던 몸은 여기서 끊는다. 안 그러면 새 방 바닥에
            // 앞 방 시체가 남아 페이드된다.
            for (int i = 0; i < _dying.Count; i++)
                if (_dying[i] != null) Destroy(_dying[i].gameObject);
            _dying.Clear();

            for (int i = 0; i < _damageTexts.Count; i++) _damageTexts[i].Despawn();
            for (int i = 0; i < _impacts.Count; i++) _impacts[i].gameObject.SetActive(false);

            // 빙의가 도는 중에 방이 바뀌면 빼앗기던 몸이 그대로 남는다.
            // 적 목록에서는 이미 빠져 있어서 아무도 치워 주지 않는다.
            if (_channelBody != null) { Destroy(_channelBody.gameObject); _channelBody = null; }
            _channel = 0f;
            _channelEntry = null;
            if (_ghost != null)
            {
                _ghost.transform.localScale = Vector3.one;
                _ghost.SetSpriteOverride(null);
            }

            _boss = null;
            ClearObstacles();
            if (_canonRoom != null) SpawnObstacles(_canonRoom);

            // 이전 룸의 탄이 다음 룸까지 날아가 첫 적을 때리는 일을 막는다
            for (int i = 0; i < _shots.Count; i++) _shots[i].Despawn();

            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0)
            {
                Debug.LogError("[Battle] 호스트 테이블이 비어 있어 적을 만들 수 없다.");
                return;
            }

            if (isBoss)
            {
                int chapter = _runChapter;

                // 정본 보스가 있으면 이름·체력·공격력·이동속도를 그대로 쓴다.
                // 우리 BossTable 은 배율표라 절대값이 없다 — 정본 쪽이 단일 출처다.
                // 테스트 모드면 방 데이터를 무시하고 표에 적힌 보스를 그대로 세운다.
                var forced = testBoss;
                bool canon = forced == null && _canonRoom != null && _canonRoom.IsBoss;

                // ⚠ 그림도 행동 목록도 **방이 든 보스**로 고른다.
                //   예전에는 그림만 방이 고르고 행동은 `ForChapter(chapter)` 가 골랐다 —
                //   챕터당 엔트리가 하나뿐이라 **CH1 006 과 012 가 같은 패턴으로 싸웠다.**
                //   보이는 몸과 하는 짓이 갈라져 있었던 것이다.
                //   아직 그림이 안 온 보스는 형제 몸을 빌린다(`BossStand`).
                string bossKey = forced != null ? forced.BossKey
                               : canon ? BossSlug(_canonRoom.BossId) : null;
                var def = _roomBoss;
                if (string.IsNullOrEmpty(bossKey)) bossKey = def?.BossKey ?? "boss";
                string bossName = canon ? _canonRoom.BossName : def?.NameEn ?? "BOSS";
                var bossArt = UnitGet(bossKey)
                           ?? UnitGet(BossStand(bossKey))
                           ?? UnitGet(UnitKeyOf(def?.SpriteName ?? "unit_boss"))
                           ?? UnitGet("boss");

                var boss = NewUnit("Boss");
                boss.Setup(UnitSide.Enemy,
                           bossKey,
                           canon ? _canonRoom.BossName : def?.DisplayName ?? "BOSS",
                           bossArt,
                           Mathf.RoundToInt(BossHpMul * (
                               canon ? _canonRoom.BossHp
                                 : def != null && def.HasCanonStats ? def.CanonHp
                                 : Mathf.RoundToInt(_config.BossHp(chapter) * (def?.HpMul ?? 1f)))),
                           Mathf.RoundToInt(BossAtkMulOf(bossKey) * (
                               canon ? _canonRoom.BossAtk
                                 : def != null && def.HasCanonStats ? def.CanonAtk
                                 : Mathf.RoundToInt(_config.BossAtk * (def?.AtkMul ?? 1f)))),
                           canon ? _canonRoom.BossMoveSpeed * _pxPerMeter
                                 : _config.BossMoveSpeed * (def?.MoveSpeedMul ?? 1f),
                           _config.BossAttackRange, _config.BossAttackInterval,
                           // 보스 그림은 256×256 캔버스다(닿는 선 y=232). 160 상자에 넣으면
                           // 캔버스 여백까지 함께 줄어 보스가 잡몹보다 작아진다.
                           // 캔버스 크기를 그대로 쓴다 — 방 폭 720 의 약 1/3 이다.
                           UnitBox(256f * BossScale, 256f * BossScale), isBoss: true);
                ApplyFacingSprites(boss, UnitGet(bossKey) != null ? bossKey : BossStand(bossKey));
                // 예고 프레임. 6종 다 들어와 있고 없으면 조용히 색만 바뀐다.
                // 예고 자세를 **방향마다** 넘긴다. 없는 방향은 null 이고, 그 방향에서는
                // 자세를 안 바꾼다 — 정면 한 장을 옆·뒤에도 쓰면 예고할 때마다 홱 돈다.
                {
                    var stand = UnitGet(bossKey) != null ? bossKey : BossStand(bossKey);
                    var tells = new Sprite[Unit.FacingSuffix.Length];
                    for (int i = 0; i < tells.Length; i++)
                        tells[i] = UnitGet(stand, $"{Unit.FacingSuffix[i]}_tell");
                    boss.SetTellSprites(tells);
                }
                // ⚠ 정본의 `BossAt` 도 안 쓴다. 정본 방은 세로 16 m 를 전제로 적힌 좌표라
                //   13 m 방에 그대로 넣으면 보스가 천장에 붙는다. 자리는 한 곳에서 정한다.
                boss.Position = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * BossStandY);
                _enemies.Add(boss);
                _boss = boss;
                _brain.Setup(def);
                if (canon)
                {
                    // 문턱과 예고 시간은 보스마다 다르다 — 정본 값을 그대로 넣는다
                    var tel = new List<float>();
                    for (int i = 0; i < _canonRoom.BossPhases.Count; i++)
                        tel.Add(_canonRoom.BossPhases[i].TelegraphSeconds);
                    _brain.SetCanonPhases(_canonRoom.BossPhaseGates, tel);
                    // 보스마다 제 공격 4가지 (정본 BOSS_ATTACK_RUNTIME)
                    _brain.SetCanonMoves(_canonRoom.BossMoves);
                }
                _bus.Publish(new BossHpChangedEvent
                {
                    BossHp = boss.Hp, BossHpMax = boss.HpMax,
                    BossName = bossName, Phase = 1,
                });
            }
            else if (_roomKind == RoomKind.Event)
            {
                // 악마의 제단 — 적이 없다. 대신 값을 묻는다.
                // ⚠ 들어서자마자 묻지 않는다. 그러면 방이 화면 한 장으로 끝나
                //   「지나가는 복도」가 된다 — 상점·회복과 같은 규칙이다.
                //   제단에 다가서면 `TickRoomProp` 이 `OfferEvent` 를 부른다.
                SpawnDevilAltar();
                SpawnExit();
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }
            else if (_roomKind == RoomKind.Shop)
            {
                // 상점 — 모아 둔 골드를 쓰는 자리. 가판에 다가서면 열린다.
                // ⚠ 들어서자마자 열지 않는다. 그러면 방이 화면 한 장으로 끝나
                //   「지나가는 복도」가 된다(`TickRoomProp` 주석 참조).
                SpawnShopStall();
                SpawnExit();
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }
            else if (_roomKind == RoomKind.Rest)
            {
                // 회복 방 — 적이 없다. 가운데 제단에 다가서면 몸과 유령이 함께 찬다.
                // 유령 상태의 시계가 계속 도는 게임이라, 쉬어 가는 방이 곧 보상이다.
                // 돌려주는 양은 `UseHealShrine` 에 있다(정본 REST_MASTER).
                SpawnHealShrine();
                SpawnExit();
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }
            else if (_canonRoom != null)
            {
                SpawnRoom(_canonRoom);
                // 상점에서 사 둔 것은 **적이 설 때까지 기다렸다가** 터진다
                // (`TickConsumable` 주석 참조). 여기서 따로 켤 것이 없다.
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }
            else
            {
                bool elite = _roomKind == RoomKind.Elite;
                SpawnProcedural(SandboxCount(elite ? _config.EliteEnemyCount : _config.EnemiesPerRoom(index)),
                                elite, index);
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
            }

            SandboxTopUp(index);   // Sandbox — 지울 때 이 줄도 함께

            // 방 골드를 이 방 적 머릿수로 나눈다. 적을 **세운 뒤에** 해야 머릿수를 안다.
            PrepareGoldDrops();
            ResetEnemySkills();
            ClearDanger();
            ClearBossState();
            // ⚠ 방을 넘어가도 꺼야 한다. 몸을 바꿀 때만 끄면 **지연 폭발이 앞 방 좌표에서**
            //   터지고, 표식 전이가 새 방까지 이어진다.
            ClearSkillState();

            // 챕터를 넘었으면 진행도를 올린다. 한 런이 1→2→3 을 이어서 가므로
            // 여기서 안 올리면 3챕터를 걸어도 계속 1챕터로 기록된다 —
            // 호스트 해금 조건이 이 값을 본다.
            if (_canonRoom != null && _canonRoom.Chapter > _runChapter)
                _runChapter = _canonRoom.Chapter;

            // 나갈 문을 **닫힌 채로** 미리 세운다. 위 분기에서 이미 문을 연 방
            // (회복·상점처럼 들어서자마자 볼일이 끝나는 방)은 건드리지 않는다.
            if (!_exitOpen) PlaceClosedExit();

            // 정본 방은 들어서는 자리가 정해져 있다(layout.playerSpawns).
            // 방마다 입구 위치가 달라 여기서 옮겨 놓지 않으면 벽 속에서 시작한다.
            var avatar = Avatar;
            if (avatar != null && _canonRoom != null)
            {
                avatar.Position = ToPixels(_canonRoom.PlayerSpawn);
                ClearOfCover(avatar);   // 입구 자리도 엄폐물과 겹칠 수 있다
            }
            // 따라온 골렘을 내 곁으로 옮긴다. 안 옮기면 지난 방 좌표에 선다.
            if (avatar != null) RegroupSummons(avatar.Position);
            // ⚠ **방에 들어선 직후 잠깐은 안 맞는다** (2026-09-10).
            //   입구에서 적을 4.5 m 밀어냈지만, 원거리 적은 그보다 멀리서도 쏜다.
            //   화면이 새 방으로 바뀌는 순간에 이미 날아오던 탄이 닿으면
            //   「들어서자마자 맞았다」가 된다 — 무엇이 있었는지 보기도 전이다.
            //   한 호흡만 준다. 길게 주면 그냥 걸어 들어가 때리는 방이 된다.
            _invuln = Mathf.Max(_invuln, RoomEntryInvulnSeconds);

            SnapCamera();

            int ch = _canonRoom != null ? Mathf.Clamp(_canonRoom.Chapter, 1, ChapterCount) : _runChapter;
            _bus.Publish(new RoomEnteredEvent
            {
                RoomIndex = index, RoomTotal = RoomTotal,
                Chapter = ch,
                StageInChapter = _canonRoom != null ? RoomNumberOf(_canonRoom.RoomId) : index + 1,
                ChapterTotal = ChapterRoomCount(ch),
                IsBossRoom = isBoss, Kind = _roomKind,
            });
        }

        /// <summary>
        /// 정본 스폰표가 없는 자리에 적을 세운다. 방 인덱스를 섞어 넣어
        /// 방마다 조합이 달라진다. 정본 방이 없던 시절의 생성 경로였는데,
        /// 지금은 **이벤트 방의 매복**도 이 길을 쓴다 — 매복에는 스폰표가 없다.
        /// </summary>
        private void SpawnProcedural(int count, bool elite, int seed)
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var e = EnemyAt(hosts, seed, i);
                var u = NewUnit($"{(elite ? "Elite" : "Enemy")}_{e.HostKey}_{i}");
                // 적도 호스트다 — 같은 공격 방식을 쓴다. 방마다 교전 양상이 달라진다.
                // 정예는 수가 적은 대신 하나하나가 세다 — 빙의 대상이 귀해진다.
                NoteMetHost(e);
                u.Setup(UnitSide.Enemy, e.HostKey, e.DisplayName, UnitGet(e.SpriteKey),
                        Mathf.RoundToInt(EnemyHpOf(e) * (elite ? _config.EliteHpMul : 1f) * SpawnHpMul(elite)),
                        Mathf.RoundToInt(EnemyAtkOf(e) * (elite ? _config.EliteAtkMul : 1f) * SpawnAtkMul(elite)),
                        EnemySpeedOf(e),
                        EnemyRangeOf(e),
                        EnemyIntervalOf(e, elite) / EnemyHandSpeedMul,
                        UnitBox(84f, 78f), isBoss: false, profile: e);
                u.Position = SpawnSlot(i, count);
                ClearOfCover(u);
                // 기획서 A 4-3 — 빙의 우선순위·사거리는 적마다 다를 수 있다.
                // 사거리 0 은 "전역 기본값을 쓴다"는 뜻이다.
                u.PossessPriority = e.PossessPriority;
                u.PossessRange = 0f;
                u.SetState(EnemyState.Idle);
                // 절차 생성 방에는 정본 `POSSESSION_TARGET` 이 없다.
                // 네 기에 한 기꼴로 숙주를 둔다 — 정본 비율(13%)보다 후하지만,
                // 절차 방은 정본 방과 달리 구제 신호가 기댈 배치 정보가 없다.
                if (i % 4 == 0) { u.MarkAsHostBody(); u.MarkAsNextBody(); }
                ApplyFacingSprites(u, e.SpriteKey);
                _enemies.Add(u);
            }
            EnsureHostBodies();
        }

        /// <summary>필드 상단 절반에 고르게 흩어 놓는다. 플레이어 시작 위치와 겹치지 않게 한다.</summary>
        /// <summary>
        /// 방 위쪽에 넓게 흩어 배치한다.
        ///
        /// 좁게 모아두면 첫 프레임부터 한 덩어리로 보이고, 전부 같은 지점을 향해 움직여
        /// 끝까지 뭉쳐 다닌다. 가로는 거의 꽉 채우고 세로도 벌린 뒤 행마다 어긋나게 민다.
        /// 플레이어 시작 위치와는 탐지 거리보다 멀게 띄운다 — 들어가야 반응하게 하기 위함.
        /// </summary>
        private Vector2 SpawnSlot(int i, int count)
        {
            float w = _roomSize.x, h = _roomSize.y;
            int cols = Mathf.Min(3, Mathf.Max(1, count));
            int rows = Mathf.CeilToInt(count / (float)cols);

            int row = i / cols;
            int colInRow = i - row * cols;
            int inRow = Mathf.Min(cols, count - row * cols);

            float fx = inRow <= 1 ? 0.5f : (float)colInRow / (inRow - 1);
            float fy = rows <= 1 ? 0.35f : (float)row / (rows - 1);

            // 격자로 딱 맞으면 대형처럼 보인다. 행마다 반 칸씩 어긋나게 민다.
            float stagger = row % 2 == 0 ? 0.07f : -0.07f;
            float x = w * Mathf.Lerp(0.10f, 0.90f, Mathf.Clamp01(fx + stagger));
            float y = -h * Mathf.Lerp(EnemyBandTop, EnemyBandBottom, fy);
            return new Vector2(x, y);
        }

        /// <summary>필드 밖으로 나가지 않게 잘라낸다. 밀림·돌진이 벽을 넘지 않게.</summary>
        /// <summary>
        /// 탄이 엄폐물에 막히는가. **누가 쏜 탄이냐에 따라 다르다.**
        ///
        /// 낮은 엄폐물(낮은 벽·바리케이드)은 적 탄이 넘어간다. 숨어도 맞는다는 뜻이고,
        /// 그 대신 내가 쏠 자리를 찾아 움직이게 만든다. 키 큰 것(기둥)만 양쪽을 다 막는다.
        /// </summary>
        private bool BlockedByCover(Vector2 at, bool fromPlayer)
        {
            // 적 탄은 **지형을 통과한다.**
            //
            // 지형이 촘촘해질수록 엄폐 뒤에 붙어 서서 아무것도 안 하는 것이 최적이 된다 —
            // 적이 못 쏘고 나는 나가서 쏘면 되니까. 그러면 지형이 전술이 아니라 은신처가 된다.
            // 막히는 쪽은 **내 탄만**이다. 그래야 지형이 "어디에 숨을까"가 아니라
            // "어디서 쏠 수 있을까"를 묻는 물건이 된다.
            if (!fromPlayer) return false;
            // 패시브로 엄폐물을 통과하는 몸이 있다(명세 2026-09-14).
            if (ShotIgnoresObstacles) return false;

            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!o.BlocksShot) continue;
                // ⚠ `Bounds` 가 아니라 `ShotBounds` 다. `Bounds` 는 몸이 지나갈 길을 내려고
                //   줄여 놓은 상자라, 그것으로 탄을 막으면 벽 위아래로 탄이 새 나간다.
                if (o.ShotBounds.Contains(at)) return true;
            }
            return false;
        }

        /// <summary>
        /// 방 테두리(미터). 배경 그림의 좌우 벽 띠 폭과 같다 —
        /// 몸이 거기 올라가면 벽을 밟고 선 것처럼 보인다.
        ///
        /// ⚠ 2026-09-11 에 1 m → **24 px(⅓ m)** 로 줄였다. 방 바닥 그림 좌우 벽 띠를 95 px 안팎에서
        ///   24 px 로 다시 받았다(기획 — 「x 축을 다 쓸 수 있게」). 그림을 바꾸고 이 값을 안 바꾸면
        ///   얇아진 벽 안쪽 바닥을 밟지 못한다 — 판정은 그림과 **같은 자** 여야 한다.
        /// ⚠ 방 편집기의 배치 여백(`RoomImporterV33.EdgeMeters`, 1 m)은 **일부러 그대로** 둔다.
        ///   그건 지형·적을 놓지 않는 칸이라 줄이면 다시 임포트할 때 60 방 배치가 통째로 움직인다.
        /// </summary>
        private const float RoomEdgeMeters = 24f / 72f;

        /// <summary>
        /// 방 안에 가둔다.
        ///
        /// ⚠ 예전에는 방 **전체**(0~폭, 0~높이)로 잘랐다. 그런데 배경 그림은 바깥 1 m 가
        ///   벽이고 위쪽은 문 구역이라, 그대로 두면 **벽 위로 걸어 올라가고 문을 지나쳐
        ///   화면 끝까지** 갔다. 레이아웃이 비워 두는 폭과 같은 값으로 조인다.
        ///
        /// 위쪽은 **문 자리에서 멈춘다.** 문을 지나칠 수 있으면 나가는 자리가 어디인지
        /// 흐려지고, 열리기 전에도 그 위에 서 있게 된다.
        /// </summary>
        private void ClampToField(Unit u)
        {
            if (u != null) u.Position = ClampedInField(u, u.Position);
        }

        /// <summary>
        /// 방 안으로 접은 자리. **걸어가는 쪽도 대시도 전부 이 함수를 지난다.**
        ///
        /// ⚠ 예전에는 이동 코드가 `ClampToField` 를 안 부르고 **자기 자리에서 따로**
        ///   방 전체(0~폭, 0~높이)로 잘랐다. 그래서 여기 값을 고쳐도 플레이어는
        ///   그대로 벽 위로 걸어 나갔다 — 판정이 두 곳에 있으면 반드시 한쪽이 낡는다.
        /// </summary>
        private Vector2 ClampedInField(Unit u, Vector2 p)
        {
            if (u == null) return p;
            var half = ((RectTransform)u.transform).sizeDelta * 0.5f;
            float edge = RoomEdgeMeters * _pxPerMeter;

            p.x = Mathf.Clamp(p.x, edge + half.x, _roomSize.x - edge - half.x);
            p.y = Mathf.Clamp(p.y, -_roomSize.y + edge + half.y, TopLimitFor(half.y));
            return p;
        }

        /// <summary>
        /// 위로 갈 수 있는 한계(픽셀, 음수). 문이 서 있으면 그 자리, 없으면 테두리 안쪽.
        /// </summary>
        private float TopLimitFor(float halfY)
        {
            if (_exits.Count > 0 && _exits[0]?.View != null)
                return Mathf.Min(-halfY, _exits[0].View.anchoredPosition.y);
            return -(RoomEdgeMeters * _pxPerMeter) - halfY;
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
#if UNITY_EDITOR
            // F1 — 판정 상자 보기. "눈에는 뚫려 보이는데 안 들어간다"를 말로 주고받으면
            // 매번 계측을 다시 하게 된다. 그냥 보이게 해 두는 편이 빠르다.
            // ⚠ 이 프로젝트는 새 Input System 이다. `UnityEngine.Input` 은 예외를 던진다.
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame)
            { _showHitBoxes = !_showHitBoxes; RebuildHitBoxView(); }
            if (_showHitBoxes) TickHitBoxView();
#endif
            // ⚠ **판정보다 먼저 잰다.** 화면 비율은 게임이 멈춰 있어도 바뀔 수 있다.
            //   아래 이른 반환 뒤에 두었다가, 3택1 창이 떠 있는 동안 태블릿으로 바꾸니
            //   창 높이가 그대로라 필드와 조작바 사이가 검게 비었다.
            TickFieldFit();

            if (!_running || _config == null) return;
            if (_awaitingBuff) return;   // 3택1 선택 대기 — 적이 없는 상태라 멈춰도 안전하다

            // ⚠ 임시 — WASD 이동 (`BattleDirector.Keyboard.cs`). 지울 때 이 줄도 함께.
            TickKeyboardMove();

            float dt = Time.deltaTime;

            // 빙의가 들어가는 중이면 다른 것은 멈추지 않되 조작만 잠근다 —
            // 0.35 초 동안 영혼이 몸으로 빨려 들어가는 것을 보여 준다.
            TickPossessChannel(dt);

            TickGhostState(dt);
            if (!_running) return;       // 자연 감소로 소멸했을 수 있다


            TickShake(dt);
            TickHitStop();

            TickPlayer(dt);
            TickAlly(dt);          // 상점에서 산 동료
            TickSummons(dt);       // 내가 불러낸 것들 — 해골 · 골렘 · 분신
            TickAfterimages(dt);
            TickEnemies(dt);
            TickShots(dt);
            TickFields(dt);
            TickDeploy(dt);
            TickDeployables(dt);
            TickEchoBlasts(dt);
            TickCards(dt);
            if (_overchargeTimer > 0f) _overchargeTimer -= dt;
            if (_afterimageTimer > 0f) _afterimageTimer -= dt;
            CheckCrisisBarrier();   // 위기는 피격뿐 아니라 화상·장판으로도 온다
            TickSkillEffect(dt);
            TickBreak(dt);
            // 보스가 벽 뒤·구멍 안·천장에 있는 동안은 못 때린다. 그 주기를 여기서 돌린다.
            TickMidBoss(dt);       // 부하가 다 죽으면 대장이 3초 굳는다
            TickBeam(dt);          // 쏜 빔이 잠깐 남았다 옅어진다
            TickExecutionLock(dt); // 표식이 예고 내내 나를 쫓아온다
            TickRoomProp();        // 회복 제단·상점 가판에 다가섰는가
            TickLockShot(dt);      // 표식이 사라지면 그 자리로 탄이 날아온다
            TickMelt(dt);          // 천장 확산 — 섬이 옮겨 다니는 8초
            TickKingpinDrop(dt);   // 올라가 있는 시간은 예고 시간과 같다
            TickGlideBreak();      // 활강이 **멈춘 자리**에서 취약 창을 본다
            TickBossPresence(dt);
            TickPythonStage(dt);   // 벽 뒤 몸통은 늘 흐른다
            TickOrbitLinger(dt);   // 파괴구는 때린 뒤에도 잠깐 더 돈다
            TickBossShieldView();  // 방패판은 예고가 아니라 걸려 있는 4초 동안 서 있다
            TickFollowUp(dt);      // 방패 전개가 부른 압착 두 번
            TickFlight(dt);        // 예고 내내 탄이 날아 도형을 채운다
            CleanupDead();
            // CleanupDead 다음에 돈다 — 이번 프레임에 죽은 몸도 바로 쓰러지기 시작한다.
            TickDying(dt);
            for (int i = 0; i < _enemies.Count; i++) _enemies[i]?.TickMark(dt);
            TickMovingObstacles(dt);
            TickHazards(dt);
            SortDepth();          // 이동이 끝난 뒤에 앞뒤를 다시 정한다
            TickDamageTexts(dt);
            TickGoldPiles(dt);
            for (int i = 0; i < _impacts.Count; i++) _impacts[i].Tick(dt);
            TickStatusFx(dt);
            TickHostPassives(dt);
            TickNewSkills(dt);
            // 모든 이동이 끝난 뒤에 화면을 옮긴다. 중간에 옮기면 한 프레임 늦게 따라온다.
            TickCamera(dt);
            // 빙의 조건이 "몸이 있느냐" 로 갈린다. 판정 직전에 채워야 한 프레임도 안 어긋난다.
            Unit.PlayerHasHost = _host != null;
            RefreshPossessTarget();
            TickRescue(dt);
            TickEmergency(dt);
            if (!_running) return;      // 긴급 호스트를 못 써서 졌을 수 있다
            TickExitOpen(dt);
            TickExit();

            // 출구가 이미 열려 있으면 다시 클리어 처리하지 않는다
            // 빙의 중에는 방을 닫지 않는다 — 빼앗기는 몸은 이미 적 목록에서 빠져 있어서
            // 마지막 한 마리를 빼앗는 순간 방이 클리어된 것으로 보인다.
            if (_combatStep > 0f) _combatStep -= dt;
            TickPendingLevelUp(dt);
            // 레벨업 팝업이 대기 중이면 출구를 먼저 열지 않는다 —
            // 고르기도 전에 다음 방으로 갈 수 있으면 성장이 선택이 아니라 사고가 된다.
            // ⚠ 조건은 `_exits.Count == 0` 이 아니라 **`!_exitOpen`** 이다.
            //   문은 이제 방에 들어서는 순간부터 닫힌 채로 서 있으므로 `_exits` 는
            //   처음부터 비어 있지 않다. 예전 조건을 그대로 두면 방이 영영 안 끝난다.
            if (_enemies.Count == 0 && !_exitOpen
                && !_awaitingBuff && !HasPendingLevelUp && !IsChanneling) OnRoomCleared();
        }

        private Unit Avatar => _host != null ? _host : _ghost;

        // ── 확률 효과 ─────────────────────────────────────────
        //
        // 판을 뒤집는 효과(흡혈·스턴·둔화·반사)는 **확률로만** 터진다.
        // 무조건 들어가면 그 몸이 언제나 정답이 되어 빙의할 이유가 사라진다.
        // 실제로 흡혈은 매 타격마다 피해의 35% 가 들어와서 흡혈귀·사신이
        // 사실상 죽지 않았다 — 기댓값을 12.5% 로 낮추고 대신 크게 터뜨린다.
        //
        // 평타의 **모양**(관통·착탄 범위·탄 수)은 확률이 아니다. 그건 효과가
        // 아니라 그 직업의 사거리 개념이라, 터졌다 말았다 하면 조준을 못 한다.
        private const int   LeechChance        = 25;    // %
        private const int   LeechPercent       = 50;    // 발동 시 준 피해의 이만큼 회복
        private const int   SlowChance         = 35;
        private const int   SlowProcPercent    = 40;
        private const float SlowProcSeconds    = 1.5f;
        private const int   ReflectChance      = 40;
        private const int   StunChance         = 12;    // 격투 직업
        private const float StunProcSeconds    = 0.8f;

        /// <summary>백분율 굴림. 방 뽑기와 같은 난수를 쓴다.</summary>
        private bool Roll(int percent) => percent > 0 && _rng.Next(100) < percent;

        // ── 직업 ──────────────────────────────────────────────
        //
        // 표에 직업 칸을 따로 만들지 않는다. 평타 방식과 사거리에서 그대로 나오는데
        // 칸을 더 두면 표와 실제가 어긋날 자리가 하나 더 생긴다.
        //
        //   격투   근접·광역          1.4 ~ 2.2 m   탄이 없다
        //   중거리 확산·단발          4.5 ~ 5.5 m   착탄 범위로 여럿을 친다
        //   관통   관통               7.2 ~ 8.2 m   줄지어 선 것을 뚫는다
        //   원거리 나머지             7.0 ~ 8.5 m   한 명씩 정확히
        // ⚠ 직업은 **셋**이다(확정본 2026-09-14) — 근거리 6 · 중거리 3 · 원거리 14.
        //   「관통」은 직업에서 뺐다. 적을 뚫는 것은 무기의 성질(`AttackKind.Pierce`)이다.
        //   로비(`HostSelectPanel.JobOf`)와 **같은 규칙**이다 — 바뀌면 두 곳을 함께 고친다.
        private enum HostJob { Melee, Mid, Ranged }

        /// <summary>중거리와 원거리를 가르는 선. 근거리 최대 2.2m 와는 두 칸 넘게 벌어져 있다.</summary>
        private const float MidRangeMeters = 6.0f;

        private static HostJob JobOf(HostEntry e)
        {
            if (e == null) return HostJob.Ranged;
            if (e.Kind == AttackKind.Melee || e.Kind == AttackKind.Pulse) return HostJob.Melee;
            return e.CanonHostRange > 0f && e.CanonHostRange <= MidRangeMeters
                 ? HostJob.Mid : HostJob.Ranged;
        }

        /// <summary>
        /// 격투 직업의 상시 규칙 — 때릴 때마다 쉴드, 그리고 확률 스턴.
        ///
        /// 스턴은 **빼앗을 수 없는 적에게만** 건다. 빙의로 열리는 몸까지 굳히면
        /// "굳혀 놓고 갈아탄다" 가 언제나 정답이 되고, 빙의 연출 0.7초 + 무적 1.25초와
        /// 겹쳐서 근접이 아무 위험 없이 몸을 갈아입는다.
        /// </summary>
        /// <summary>
        /// 약화를 걸고 **터진 것을 보여 준다.**
        ///
        /// 확률 효과는 보이지 않으면 안 되는 것과 같다(`AVSR_JobClasses.md` §4).
        /// 흡혈·스턴·쉴드는 그림이 붙었는데 약화만 빠져 있었다 —
        /// 35% 로 터지는데 화면에 아무 일도 안 일어나 "안 되는 것" 으로 보였다.
        ///
        /// `fx_slow_1~3` 이 아직 없으면 `PlayFx` 가 조용히 넘어간다(오류 아님).
        /// 그림이 들어오는 순간 저절로 켜진다.
        /// </summary>
        private void ApplySlowProc(Unit victim)
        {
            if (victim == null) return;
            victim.ApplySlow(SlowProcPercent, SlowProcSeconds);
            SpawnFx("slow", victim.Position, StunFxSize);
        }

        private void MeleeJobProc(Unit victim, HostEntry p)
        {
            if (JobOf(p) != HostJob.Melee) return;

            var me = Avatar;
            if (me != null)
            {
                // 구루 결계는 획득량을 두 배로 만든다.
                int gain = Mathf.Max(1, Mathf.RoundToInt(me.HpMax * _config.ShieldPerHitPercent / 100f))
                         * (_wardSeconds > 0f ? 2 : 1);
                me.AddShield(gain, me.HpMax * _config.ShieldCapPercent / 100);
            }

            // ⚠ `IsPossessable` 로 거르면 안 된다. 그 값은 **지금 이 순간 탈 수 있는가**라
            //    내가 이미 몸을 쓰고 있으면 숙주까지 전부 false 가 된다 — 결국 굳히면
            //    안 될 몸을 굳힌다. 물어야 할 것은 "빼앗을 여지가 있는 몸인가" 쪽이다.
            //    보스는 위쪽 `TickBoss` 가 먼저 가로채므로 굳혀 봐야 무시된다.
            if (victim == null || !victim.IsAlive || victim.IsBoss
                || victim.HasPossessCondition) return;
            if (Roll(StunChance)) victim.ApplyStun(StunProcSeconds);
        }

        /// <summary>무적 중인가. 빙의 직후와 호스트 상실 직후의 보호 시간을 함께 본다.</summary>
        private bool IsInvulnerable => _invuln > 0f || _ghostProtect > 0f;

        /// <summary>
        /// 유령 상태의 시간 규칙 (기획서 A 1-2 · 1-3).
        ///
        /// Ghost HP 는 체력이 아니라 **남은 시간**이다. 유령으로 떠 있는 동안 초당 깎이므로,
        /// "안전한 곳에서 기다린다"가 공짜가 아니게 된다. 호스트가 살아 있으면 멈춘다 —
        /// 몸을 얻은 상태가 곧 시계를 멈춘 상태다.
        ///
        /// 호스트를 잃은 직후에는 보호 시간이 붙는다. 그 순간은 적 한복판이라,
        /// 보호가 없으면 다시 빙의할 틈 없이 연쇄로 죽는다.
        /// </summary>
        private void TickGhostState(float dt)
        {
            if (_invuln > 0f) _invuln = Mathf.Max(0f, _invuln - dt);
            if (_skillInvuln > 0f) _skillInvuln = Mathf.Max(0f, _skillInvuln - dt);

            // 전술 빙의 쿨다운은 몸 안에 있든 밖에 있든 흐른다.
            // 유령일 때 멈추면 죽고 나서 기다리는 것이 이득이 된다.
            if (_repossessLock > 0f)
            {
                _repossessLock = Mathf.Max(0f, _repossessLock - dt);
                // 매 프레임 발행하지 않는다 — 0.1초 눈금이 바뀔 때만. 표시는 그걸로 충분하다.
                if (_repossessLock == 0f ||
                    Mathf.FloorToInt(_repossessLock * 10f) != _repossessLockShown)
                {
                    _repossessLockShown = Mathf.FloorToInt(_repossessLock * 10f);
                    _bus.Publish(new RepossessLockEvent
                    {
                        Remain = _repossessLock, Total = _config.RepossessLockSeconds,
                    });
                }
                if (_repossessLock == 0f) RefreshPossessTarget();
            }

            if (_ghostProtect > 0f)
            {
                _ghostProtect = Mathf.Max(0f, _ghostProtect - dt);
                return;                       // 보호 중에는 자연 감소도 멈춘다
            }
            if (_host != null) return;        // 몸이 있으면 시계가 멈춘다
            if (SandboxKeepsGhost) return;    // Sandbox — 지울 때 이 줄도 함께

            _drainCarry += _config.GhostDrainPerSecond * dt;
            int whole = Mathf.FloorToInt(_drainCarry);
            if (whole <= 0) return;

            _drainCarry -= whole;
            _ghostHp = Mathf.Max(0, _ghostHp - whole);
            PublishHp();
            if (_ghostHp == 0) Finish(false);
        }

        // ─────────────────────────────────────────────────────────
        // 구조 신호
        //
        // 몸이 터졌는데 방에 빼앗을 몸이 없으면 **대응할 방법이 없는 죽음**이 된다.
        // 그건 난이도가 아니라 고장이다. 그래서 유령이 된 지 몇 초 뒤,
        // 방 위쪽에서 숙주 한 기가 걸어 들어온다.
        //
        // 긴급 호스트(아래)와 다르다. 긴급 호스트는 **제자리에서 몸을 준다** —
        // 유령 구간이 즉시 끝나 버려서 쫓기는 맛이 없다. 구조 신호는 목표를 놓아 줄 뿐,
        // 잡몹 사이를 뚫고 가서 경직시켜 빼앗는 것은 플레이어 몫이다.
        // ─────────────────────────────────────────────────────────

        /// <summary>몸을 잃고 이만큼 지나면 숙주가 들어온다(초).</summary>
        private const float RescueDelaySeconds = 3f;

        /// <summary>연달아 불러들이지 못하게 하는 간격(초).</summary>
        private const float RescueCooldownSeconds = 20f;

        private float _rescueWait;
        private float _rescueCool;

        private void TickRescue(float dt)
        {
            if (_rescueCool > 0f) _rescueCool = Mathf.Max(0f, _rescueCool - dt);

            // 몸이 있거나, 빼앗을 몸이 이미 방에 있으면 구조할 이유가 없다.
            if (_host != null || _awaitingBuff || HasFutureHost()) { _rescueWait = 0f; return; }
            // 보스방은 긴급 호스트가 맡는다. 보스와 싸우는 중에 몸이 걸어 들어오면
            // 보스 패턴과 구조 목표가 화면에서 서로를 가린다.
            if (_boss != null) { _rescueWait = 0f; return; }

            _rescueWait += dt;
            if (_rescueWait < RescueDelaySeconds || _rescueCool > 0f) return;
            _rescueWait = 0f;
            _rescueCool = RescueCooldownSeconds;
            SpawnRescueHost();
        }

        /// <summary>
        /// 숙주 한 기를 방 위쪽에 세운다. 플레이어와 출구 사이라야 의미가 있다 —
        /// 뒤쪽에 놓으면 왔던 길을 되돌아가야 해서 쫓기는 방향이 뒤집힌다.
        /// </summary>
        private void SpawnRescueHost()
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return;

            var e = EnemyAt(hosts, _roomIndex, _enemies.Count);
            var u = NewUnit($"RescueHost_{e.HostKey}");
            NoteMetHost(e);   // 상점이 파는 목록은 이 판에서 만난 몸뿐이다
            u.Setup(UnitSide.Enemy, e.HostKey, e.DisplayName, UnitGet(e.SpriteKey),
                    Mathf.RoundToInt(EnemyHpOf(e)), Mathf.RoundToInt(EnemyAtkOf(e)),
                    EnemySpeedOf(e), EnemyRangeOf(e), EnemyIntervalOf(e),
                    UnitBox(84f, 78f), isBoss: false, profile: e);
            u.Position = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * 0.12f);
            ClearOfCover(u);
            u.MarkAsHostBody();
            u.MarkAsNextBody();
            u.PossessPriority = e.PossessPriority;
            u.PossessRange = 0f;
            u.SetState(EnemyState.Idle);
            ApplyFacingSprites(u, e.SpriteKey);
            _enemies.Add(u);
        }

        /// <summary>
        /// 긴급 호스트 (기획서 A 8-3).
        ///
        /// 빙의할 몸이 하나도 없으면 시계만 도는 상태가 된다 — 특히 보스방은 보스가
        /// 빙의 대상이 아니라(A 1-4) 손쓸 방법이 아예 없다. 1초를 기다린 뒤 몸을 하나
        /// 만들어 준다. 대신 값이 비싸다 — Ghost HP 를 추가로 깎고, 시작 체력이 30%다.
        /// **구제책이지 선택지가 아니다.** 값은 Ghost HP 로 내고, 그 피가 다하면 끝이다.
        /// </summary>
        private void TickEmergency(float dt)
        {
            if (_host != null || _awaitingBuff) { _emergencyWait = 0f; return; }
            if (_possessTarget != null || HasFutureHost()) { _emergencyWait = 0f; return; }

            // ⚠ 적이 없는 방에서는 이 구제책이 **사형선고**가 된다.
            //   정본 v3.3 의 48방 중 15방(SHOP·EVENT·REST)은 적이 0기다.
            //   유령으로 그 방에 들어서면 빙의할 몸이 없다는 이유로 1초 뒤 패배한다.
            //   방을 비운 직후도 마찬가지다 — 출구가 열려 있는데 걸어가다 진다.
            //   빙의할 몸이 없는 것이 문제가 되려면 **싸울 상대가 있어야** 한다.
            if (_enemies.Count == 0) { _emergencyWait = 0f; return; }

            _emergencyWait += dt;
            if (_emergencyWait < _config.EmergencyDelaySeconds) return;
            _emergencyWait = 0f;

            // ⚠ 구제에 실패했다고 **그 자리에서 죽이지 않는다.**
            //   유령의 Ghost HP 는 체력이 아니라 남은 시간이다(기획서 A 1-2).
            //   그런데 여기서 바로 지게 해 두면, 보스방처럼 빼앗을 몸이 없는 방에서
            //   몸을 잃는 순간 **체력이 가득해도 즉사**한다. 긴급 호스트를 이미 쓴
            //   방이면 더 확실하게 죽는다 — 실제로 그렇게 나갔다.
            //
            //   구제는 구제일 뿐이고, 지는 조건은 시간이 다하는 것 하나다.
            //   못 구했으면 유령으로 떠 있게 두고 다음 기회를 다시 본다 —
            //   보스가 잡몹을 부르거나, 빼앗을 몸이 다시 생길 수 있다.
            // 피가 있으면 몇 번이든 부활한다.
            //
            // ⚠ 방마다 한 번으로 묶어 두었더니, 보스방처럼 긴 방에서 두 번째로 몸을
            //   잃으면 손쓸 방법이 아예 없었다 — 빙의할 몸도 없고(보스는 대상이 아니다)
            //   구제도 이미 썼으니, 유령으로 떠서 시계가 다 돌기를 기다리는 것 말고는
            //   할 수 있는 것이 없다. 그건 게임이 아니라 대기다.
            //
            //   값은 **Ghost HP 로 낸다.** 낼 수 있는 만큼 내는 것이지 횟수를 세는 것이
            //   아니다. 한 번에 20 이 나가므로 무한히 버티지 못한다 — 그게 상한이다.
            if (_ghostHp <= _config.EmergencyGhostCost) return;

            var entry = PickPlayerHost();
            if (entry == null) return;

            _ghostHp = Mathf.Max(1, _ghostHp - _config.EmergencyGhostCost);

            EnterHost(entry, entry.HostKey, entry.DisplayName, _ghost.Position,
                      _config.EmergencyHostHpPercent);
            _bus.Publish(new EmergencyHostEvent
            {
                HostKey = entry.HostKey, GhostCost = _config.EmergencyGhostCost,
            });
        }

        /// <summary>
        /// **앞으로** 빼앗을 수 있는 몸이 방에 있는가.
        ///
        /// `IsPossessable` 은 "지금 당장 탈 수 있는가" 라서 체력 문턱을 함께 본다 —
        /// 23종 중 17종이 25~50% 아래로 떨어져야 탈 수 있다.
        /// 긴급 호스트의 판단에 그 값을 쓰면 **멀쩡한 적이 가득한 방도 "몸이 없는 방"**
        /// 이 된다. 실제로 그래서 몸을 잃은 지 1초 만에 같은 몸이 되살아났다 —
        /// 사장님이 "죽었는데 살아난다" 고 본 것이 이것이다.
        ///
        /// 여기서 물어야 할 것은 "지금 탈 수 있나" 가 아니라 **"깎으면 탈 수 있나"** 다.
        /// 깎아서 탈 수 있는 몸이 하나라도 있으면 구제할 이유가 없다 — 깎으면 된다.
        /// </summary>
        private bool HasFutureHost()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsBoss) continue;
                if (e.Side != UnitSide.Enemy || e.RepossessBanned) continue;
                if (!e.IsHostBody) continue;      // 잡몹은 아무리 깎아도 몸이 되지 않는다
                if (e.Profile != null &&
                    e.Profile.PossessKind == Game.Character.PossessKind.NotPossessable) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 플레이어가 데려온 몸. 로비에서 고른 호스트를 우선한다.
        /// 런 시작 몸과 긴급 투입 몸이 같은 것을 쓴다 — 고른 캐릭터가 곧 내 캐릭터다.
        /// </summary>
        private HostEntry PickPlayerHost()
        {
            if (_player == null || !_player.IsReady) return null;

            // ⚠ **입었던 몸이 먼저다.** 이 판에서 한 번이라도 몸을 입었으면
            //   그 몸이 구제책이 된다.
            //
            //   아래 `StartAsGhost` 규칙만 있을 때, 유령으로 시작한 판은 **끝까지**
            //   구제를 못 받았다 — 12번째 보스방에서 몸을 잃어도 마찬가지였다.
            //   그 규칙이 말하려던 것은 "**첫 몸**은 직접 빼앗아라" 였지
            //   "이 판 내내 구제 없다" 가 아니다.
            if (_lastHostEntry != null) return _lastHostEntry;

            // 유령으로 골라 들어왔고 아직 한 번도 몸을 안 입었다 —
            // 1번 방에서 직접 빼앗는다. 원작이 그랬다.
            if (_player.StartAsGhost) return null;
            var picked = _player.GetHost(_player.SelectedHostId);
            if (picked != null) return picked;
            var all = _player.AllHosts;
            return all != null && all.Count > 0 ? all[0] : null;
        }

        private void TickPlayer(float dt)
        {
            var me = Avatar;
            if (me == null) return;
            // 점멸을 그리기 **전에** 알려 준다 — 한 프레임 늦으면 켜지는 순간이 씹힌다.
            me.SetInvulnerable(IsInvulnerable);
            // 윤곽은 **스킬이 준 무적**에만 켠다(기획 2026-09-15). 방 입장·빙의 직후·카드 무적까지
            // 켜니 캐릭터가 수시로 허옇게 번쩍여 지저분했다 — 무적이라고 다 그리는 게 아니다.
            me.SetInvulnAura(_skillInvuln > 0f);
            // 몸을 갈아타면 아바타가 바뀐다. 물려받지 못한 쪽에 점멸이 남으면
            // 쓰지도 않는 몸이 계속 깜빡인다.
            if (_ghost != null && _ghost != me) { _ghost.SetInvulnerable(false); _ghost.SetInvulnAura(false); }
            if (_host != null && _host != me) { _host.SetInvulnerable(false); _host.SetInvulnAura(false); }
            me.TickFlash(dt);
            me.TickAnim(dt);
            // 쉴드는 싸우는 동안의 보상이다 — 손을 놓으면 2초 뒤부터 녹는다.
            me.TickShield(dt);

            // 빙의가 들어가는 중에는 조작을 받지 않는다. 안 막으면 조이스틱이
            // 빨려 들어가는 연출과 서로 자리를 다툰다.
            if (IsChanneling) return;

            // 돌진하는 동안도 마찬가지다. 조작을 같이 받으면 걷는 힘과 돌진이
            // 서로 자리를 당겨 경로가 휘고, 3 m 를 갔는지 알 수 없게 된다.
            if (IsDashing) { TickDash(dt, me); return; }

            // 호퍼의 도약도 같다. 뛰는 동안 걸어지면 포물선이 휘어 어디에 떨어질지 모른다.
            if (IsSlamming) { TickSlam(dt); return; }

            // ⚠ **굳은 동안은 조작을 받지 않는다.**
            //   스턴 장치(`Unit.ApplyStun`)는 원래 잡몹에만 걸렸다 — 시계를 굴리는 곳도
            //   입력을 막는 곳도 적 쪽에만 있었다. 「똬리」가 나를 굳히게 되면서
            //   플레이어 쪽에도 같은 것이 필요해졌다.
            //   머리 위 별은 `TickStatusFx` 가 띄운다.
            me.TickStun(dt);
            if (me.IsStunned)
            {
                me.SetMoving(false);
                IsFiring = false;
                _stopTimer = 0f;
                return;
            }

            // ⚠️ 이것이 없으면 **갇힌다.** `SlideMove` 는 막힌 곳에 "들어가지 않게" 막는
            //    방식이라, 어쩌다 안에 들어간 뒤에는 어느 쪽으로도 못 나온다 —
            //    모든 후보 위치가 똑같이 막힌 것으로 판정되어 제자리를 돌려준다.
            //    적과 소환물에는 이 구제책이 걸려 있었는데 플레이어만 빠져 있었다.
            //    빙의 교체·밀림·방 진입 스폰으로 겹치면 그 판이 끝난다.
            ResolveObstacles(me);

            // ⚠️ 궁수의 전설 규칙 — **움직이는 동안에는 쏘지 않는다.**
            //    이동과 공격이 배타적이어야 "자리를 잡을까 딜을 넣을까"의 긴장이 생긴다.
            //    이걸 없애면 조작이 그냥 산책이 된다.
            bool moving = MoveInput.sqrMagnitude > 0.0001f;
            if (!moving) me.SetMoving(false);
            if (moving)
            {
                // 막힌 것을 타고 미끄러진다. 밀어 넣고 빼내면 벽에서 캐릭터가 떨린다.
                var before = me.Position;
                var p = SlideMove(me, me.Position,
                                  MoveInput * (me.MoveSpeed * _buffs.MoveMul * CombatStepMul) * dt);
                // 테두리·문 한계는 `ClampedInField` 한 곳이 갖는다.
                p = ClampedInField(me, p);
                me.Position = p;
                var moved = p - before;

                // 걷는 쪽을 바라본다. 아래 `return` 때문에 이동 중에는 조준 쪽
                // 방향 전환에 도달하지 못하므로, 여기서 돌려 주지 않으면
                // 이동 중에는 방향이 통째로 멈춘다.
                // 이동 중 사격이 되는 호스트는 아래에서 조준 방향이 덮어쓴다 —
                // 겨누는 쪽이 걷는 쪽보다 우선이다.
                // 민 방향이 아니라 **실제로 간 방향**으로 돈다. 벽에 스쳐 미끄러질 때
                // 민 쪽을 보면 벽을 향해 옆걸음질하는 그림이 된다.
                me.SetFacing(moved.sqrMagnitude > 0.0001f ? moved : MoveInput);
                me.SetMoving(true);

                // 기획서 A 3-3 Move Attack — 이동 중 사격은 **예외 호스트에만** 허용한다.
                // 전부 허용하면 멈출 이유가 없어져 위 규칙이 죽는다.
                // 폭력배 난사 1초 동안은 **걸으면서도 쏜다**(기획 2026-09-15) —
                // 쏟아붓는 기술이 걸음 한 번에 끊기면 난사가 아니라 멈춤 버튼이 된다.
                bool moveAttack = (_host != null && _host.Profile != null && _host.Profile.MoveAttack)
                               || _spraySeconds > 0f;
                if (!moveAttack)
                {
                    _stopTimer = 0f;
                    IsFiring = false;
                    if (_host != null) _host.CoolAttack(dt);   // 걷는 동안에도 다음 차례까지 시간은 흐른다
                    return;
                }
            }

            // 멈춘 직후 아주 짧게 준비 시간을 둔다. 없으면 톡톡 끊어 눌러도 손해가 없어
            // 멈춤의 대가가 사라진다.
            _stopTimer += dt;
            if (_stopTimer < Mathf.Max(0.02f, _config.AttackResumeSeconds - _buffs.StopDelayCut))
            { IsFiring = false; if (_host != null) _host.CoolAttack(dt); return; }

            // 고스트는 공격하지 않는다 — 빙의해야 싸울 수 있다(핵심 동사)
            if (_host == null) { IsFiring = false; return; }

            // 설녀 — 얼음 안에서는 손도 멈춘다(명세: 맞지도 때리지도 않는다).
            if (IsSelfFrozen) { IsFiring = false; return; }

            var target = Nearest(_host.Position);

            // 노리는 쪽을 바라본다. 사거리 밖이라 아직 안 쏘더라도 몸은 돌려 둔다 —
            // 조준이 먼저 보이고 사격이 뒤따라야 "겨눈다"는 느낌이 난다.
            if (target != null) _host.SetFacing(target.Position - _host.Position);

            // 근접은 붙어야 때린다 — 적과 같은 규칙이다. 전역 사거리를 900 으로
            // 올려 두어서 그대로 두면 아마존 주먹이 방 건너편까지 닿는다.
            //
            // ⚠ **가장자리까지 잰다.** 여기가 예전에 중심에서 중심까지였다 —
            //   그런데 실제 피해를 주는 `MeleeStrike` 는 이미 `EdgeDistance` 로 바뀌어 있어,
            //   **판정이 두 곳에서 서로 다른 자를 쓰고 있었다.**
            //
            //   보스는 상자가 256px(반경 107.5)이고 아마존 사거리는 101px 이다.
            //   중심으로 재면 101 ≤ 107.5 이라 **보스 중심 안으로 들어가야** 이 문이 열린다.
            //   몸에 딱 붙어 서도 안 열리니, 안쪽 판정이 아무리 관대해도 한 번도 안 불린다 —
            //   "붙어 있는데 딜이 안 들어간다" 가 그것이다.
            bool inRange = target != null &&
                           EdgeDistance(_host, target)
                               <= EffectiveRange(_host) * _buffs.RangeMul;
            IsFiring = inRange;
            if (!inRange) { _host.CoolAttack(dt); return; }
            // 버프는 유닛 스탯을 덮어쓰지 않고 발사 시점에 곱한다 (빙의로 몸이 바뀌어도 유지)
            if (!_host.TickAttack(dt, _buffs.IntervalMul * HasteMul)) return;
            PerformAttack(_host, target, true);
            // C024 전투 스텝 — 쏘고 나면 잠깐 빨라진다. 치고 빠지는 손맛이 여기서 난다.
            if (_buffs.CombatStepBonus > 0f) _combatStep = CombatStepSeconds;
        }

        private void TickEnemies(float dt)
        {
            if (_burnSpreadTimer > 0f) _burnSpreadTimer -= dt;
            TickBait(dt);
            // 닌자 분신 — **도발**. 서 있는 동안 적은 분신을 쫓고 분신을 때린다(명세 2026-09-14).
            // 보스는 빠진다 — 아래에서 `Avatar` 를 따로 넘긴다(바닥에 그려 놓고 치는 패턴이라).
            var me = TauntUnit ?? Avatar;
            if (me == null) return;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                e.TickFlash(dt);
                e.TickAnim(dt);
                e.TickSlow(dt);
                e.TickStun(dt);
                e.TickRoot(dt);
                e.TickAmp(dt);
                e.TickStagger(dt);   // 몰아치지 않으면 식는다

                // 화상 피해는 유닛이 스스로 깎지 않는다 — 죽음 처리·보상·피해 숫자가
                // 전부 여기 있어서, 저쪽에서 깎으면 죽어도 아무 일도 안 일어난다.
                int burn = e.TickStatus(dt);
                if (burn > 0)
                {
                    // 정본 BUF_T02 — 3단계 화상이 주변으로 옮는다.
                    // 화상 틱은 3단계에서 0.06초마다 떨어지므로 그대로 두면 초당 열몇 번
                    // 옮는다. 초에 한 번으로 묶는다.
                    if (_buffs.BurnSpreads && e.BurnStack >= Unit.StatusMaxStack)
                        SpreadBurn(e);

                    // 불 그림은 여기서 피우지 않는다 — `TickStatusFx` 가 몸에 붙여 돌린다.
                    ShowDamage(e.Position, burn, toEnemy: true);
                    if (e.TakeDamage(burn)) { KillEnemy(e); continue; }
                    if (e.IsBoss)
                        _bus.Publish(new BossHpChangedEvent { BossHp = e.Hp, BossHpMax = e.HpMax });
                }

                // ⚠ **몸이 없으면 아무도 유령을 표적으로 잡지 않는다.**
                //
                // 유령은 맞지 않는다(A 1-2 — Ghost HP 는 체력이 아니라 남은 시간이다).
                // 그런데 적이 계속 쫓아와 때리는 시늉을 하면, 화면에서는 맞고 있는 것으로
                // 보이고 마침 시계가 줄어들고 있어서 "맞아서 닳는다" 로 읽힌다.
                // 판정을 막는 것만으로는 부족하다 — 표적으로 잡는 것 자체를 끊는다.
                //
                // 유령 구간의 압박은 **시계**다. 초당 감소와 몸을 잃을 때의 20% 가
                // 그 값을 치른다. 쫓기는 것이 아니라 서두르는 것이 이 구간의 문제다.
                // ⚠ **보스는 이 규칙에서 빠진다.** 아래 「몸이 없으면 멈춘다」보다 먼저 돈다.
                //
                //   보스 패턴은 나를 쫓아와 때리는 것이 아니라 **바닥에 그려 놓고 치는 것**이라,
                //   유령이 그 위에 서 있어도 `DamagePlayer` 가 걸러 낸다 — 멈출 이유가 없다.
                //   멈춰 세워 놨더니 유령으로 보스방에 들어간 순간 보스가 **아무것도 안 하고
                //   서 있었다.** 화면에서는 그냥 고장으로 보인다.
                //
                //   기준점은 `Avatar` 다 — 몸이 있으면 그 몸, 없으면 유령.
                //   보스는 "지금 내가 서 있는 자리" 를 겨눈다.
                if (e.IsBoss) { TickBoss(e, Avatar, dt); continue; }

                // 부하를 다 잃은 대장은 3초 굳는다. 이 방의 유일한 취약 창이라
                // 여기서 계속 때리면 창이 창이 아니게 된다.
                if (e == _midBoss && IsMidBossStunned) { e.SetMoving(false); continue; }

                // ⚠ **몸이 없으면 아무도 유령을 표적으로 잡지 않는다.**
                if (_host == null || IsSelfFrozen)
                {
                    e.IsAggro = false;
                    e.SetState(EnemyState.Idle);
                    continue;
                }

                // 굳어 있는 동안은 다가오지도 때리지도 않는다.
                // 자세도 함께 푼다 — 안 그러면 풀리는 순간 예고 없이 맞는다.
                if (e.IsStunned)
                {
                    e.CancelWindup();
                    e.SetMoving(false);
                    Separate(e, i, dt);
                    continue;
                }

                float d = Vector2.Distance(e.Position, me.Position);

                // 매복은 **깨어나기 전부터** 숨어 있어야 한다.
                //
                // 아래 탐지 관문은 화면에 들어와야 통과한다. 거기서 숨기면
                // 한 프레임 보였다가 사라져 「숨었다」가 아니라 「깜빡였다」가 된다.
                // 땅에서 나온 뒤(단계 2)에는 이 줄이 그냥 지나가고 평소 흐름을 탄다.
                if (e.PatternPhase < 2 && PatternOf(e) == EnemyPattern.Ambush
                    && TickAmbush(e, me, d, dt))
                { Separate(e, i, dt); continue; }

                // 탐지 — 들어오기 전에는 제자리에서 기다린다.
                // 처음부터 전부 달려들면 방이 통째로 한 덩어리가 되어 몰려다닌다.

                if (!e.IsAggro)
                {
                    // 화면 밖에서는 깨어나지 않는다. 정본의 `NO_OFFSCREEN_TELEGRAPH` —
                    // 보이지도 않는 곳에서 예고 없이 날아오는 공격은 피할 방법이 없다.
                    //
                    // 거리는 더 이상 보지 않는다. 화면에 보이면 곧 싸움이다 —
                    // 탐지 거리를 두면 방에 들어가서 한참을 걸어가야 교전이 시작되고,
                    // 그 사이가 그냥 빈 시간이 된다.
                    if (!IsOnScreen(e))
                    {
                        e.SetState(EnemyState.Idle);
                        Separate(e, i, dt);
                        continue;
                    }
                    e.IsAggro = true;
                    e.SetState(EnemyState.Detect);
                }

                // 기획서 A 4-1 — Detect → Approach → Attack → Cooldown.
                // 상태를 이름으로 들고 있어야 AI 타입별 분기를 넣을 자리가 생긴다.
                e.SetFacing(me.Position - e.Position);   // 적도 플레이어를 바라본다

                // ── 행동 패턴 ────────────────────────────────────
                //
                // ⚠ 여기가 예전에는 `IsMelee(e)` 하나로 갈렸다. 근접/원거리 둘뿐이라
                //   새 행동을 넣을 자리가 없었다 — 이제 패턴이 자리를 차지하고,
                //   `true` 를 돌려주면 아래 상태기를 통째로 건너뛴다.
                var pattern = PatternOf(e);

                // 적 호스트의 액티브 스킬이 패턴보다 먼저다. 시전 중에는 안 움직인다.
                if (TickEnemySkill(e, me, dt)) { Separate(e, i, dt); continue; }

                if (pattern == EnemyPattern.Cross)
                { TickCross(e, dt); Separate(e, i, dt); continue; }

                if (pattern == EnemyPattern.Vault)
                { TickVault(e, me, dt); Separate(e, i, dt); continue; }

                if (pattern == EnemyPattern.Hop && TickHop(e, me, d, dt))
                { Separate(e, i, dt); continue; }

                // 챕터가 깊어지며 붙는 세 가지. `false` 를 돌려주면 평소 흐름으로 내려간다 —
                // 매복은 땅에서 나온 뒤, 3연발·회오리는 사거리 밖일 때가 그렇다.
                if (pattern == EnemyPattern.Burst && TickBurst(e, me, d, dt))
                { Separate(e, i, dt); continue; }

                if (pattern == EnemyPattern.Spiral && TickSpiral(e, me, d, dt))
                { Separate(e, i, dt); continue; }

                // 근접과 원거리는 다르게 움직인다.
                //
                //   근접  붙어야 때린다. 사거리가 짧으므로 계속 쫓는다
                //   원거리 **두 번 쏘고 한 번 자리를 옮긴다.** 가만히 서서 계속 쏘면
                //          한 자리에 붙박여 있어 피하기만 하면 되는 과녁이 되고,
                //          계속 쫓아오면 붙어 버려 사거리의 의미가 없다
                float reach = EffectiveRange(e);

                if (d > reach)
                {
                    e.CancelWindup();   // 사거리 밖으로 밀려났으면 자세를 푼다

                    // ⚠ 옆걸음도 함께 접는다.
                    //   옆걸음 목표는 **때리던 그 순간의 내 자리**를 기준으로 잡은 것이다.
                    //   그 사이 내가 달아나면 목표가 낡는다. 그런데 예전에는 여기서
                    //   접지 않아, 쫓아와서 사거리에 들어오는 순간 낡은 자리로 되돌아갔다 —
                    //   그 자리는 이미 내 뒤쪽이라 **쫓다 말고 도망치는 것처럼** 보였고,
                    //   되돌아갔다 다시 쫓기를 반복해 영영 때리지 못했다.
                    //   붙어야 할 때는 붙는 것이 먼저다.
                    e.EndReposition();

                    e.SetState(EnemyState.Approach);
                    e.Position = SlideMove(e, e.Position, e.StepToward(me.Position, dt));
                    e.SetMoving(true);
                }
                // 자세를 잡는 중 — 아직 안 때린다. 이 틈이 피하거나 파고들 시간이다
                else if (e.IsWindingUp)
                {
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    if (e.TickWindup(dt))
                    {
                        PerformAttack(e, me, false);
                        if (pattern == EnemyPattern.Strafe
                            && e.CountShotAndNeedsMove(ShotsBeforeMove))
                            e.BeginReposition(PickRepositionSpot(e, me));
                    }
                }
                else if (e.IsRepositioning)
                {
                    // 자리를 옮기는 중 — 쏘지 않는다. 이 틈이 곧 반격할 틈이다
                    e.SetState(EnemyState.Cooldown);
                    e.Position = SlideMove(e, e.Position, e.StepToward(e.RepositionTarget, dt));
                    e.SetMoving(true);
                    if (Vector2.Distance(e.Position, e.RepositionTarget) < 24f) e.EndReposition();
                }
                else if (e.TickAttack(dt))
                {
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);

                    // 예고도 함께 줄인다 — 「간격 + 예고」가 한 대에 걸리는 시간이라
                    // 간격만 줄이면 체감이 거의 안 바뀐다.
                    float windup = (e.Profile?.CanonTelegraph ?? 0f) * _config.EnemyWindupMul;
                    if (windup > 0f && CanStartAttack(e))
                    {
                        e.BeginWindup(windup);
                    }
                    else if (windup <= 0f)
                    {
                        // 정본에 예고가 없는 배우는 예전처럼 바로 때린다
                        PerformAttack(e, me, false);
                        if (pattern == EnemyPattern.Strafe
                            && e.CountShotAndNeedsMove(ShotsBeforeMove))
                            e.BeginReposition(PickRepositionSpot(e, me));
                    }
                    else
                    {
                        // ⚠ **동시 공격 상한에 걸린 차례를 그냥 버리면 안 된다.**
                        //   `TickAttack` 은 true 를 돌려주는 순간 이미 간격을 다시 채웠다.
                        //   여기서 아무것도 안 하고 빠지면 한 간격을 통째로 날리고,
                        //   상한이 1인 배우(살라만더·집행자)는 앞사람이 자세를 잡고 있는 동안
                        //   차례가 계속 날아가 **영영 안 때린다.**
                        //   실제로 CH1 005 대장과 CH2 006 집행자가 그렇게 서 있었다.
                        //
                        //   자리가 날 때까지 **짧게 다시 본다.** 간격을 새로 채우지 않고
                        //   조금 뒤에 한 번 더 묻는 것이라, 상한이 풀리는 즉시 때린다.
                        e.RetryAttackSoon(AttackRetrySeconds);
                    }
                }
                else
                {
                    e.SetMoving(false);
                    e.SetState(EnemyState.Cooldown);
                }

                Separate(e, i, dt);
            }
        }

        // ── 적 행동 거리 ──────────────────────────────────────────

        /// <summary>
        /// 지금 때리기 시작해도 되는가.
        ///
        /// 정본은 배우마다 **동시에 때릴 수 있는 마릿수**(MaxConcurrent 1~4)를 정해 둔다.
        /// 제한이 없으면 방 안 전원이 같은 순간에 쏴서, 근접으로는 들어갈 틈이 아예 없다.
        /// 순서를 정하지 않고 먼저 준비를 시작한 쪽이 자리를 차지한다 — 나머지는 다음 간격에
        /// 다시 본다. 그래야 사격이 한 덩어리가 아니라 물결처럼 나뉜다.
        ///
        /// 마릿수가 한 자릿수라 매번 세도 된다. 목록을 따로 들고 있으면 죽거나 빙의로
        /// 편이 바뀔 때마다 어긋난다.
        /// </summary>
        private bool CanStartAttack(Unit e)
        {
            int cap = e.Profile?.CanonMaxConcurrent ?? 0;
            if (cap <= 0) return true;

            // ⚠ 예전에는 `o.Key == e.Key` 로 **같은 종끼리만** 셌다.
            //   방에 수류탄병 3 · 아마존 2 · 기관총 2 처럼 섞여 있으면 종마다 따로
            //   상한을 먹어서 사실상 제한이 없었다 — 11기 방에서 전원이 동시에 쏘고 있었다.
            //   상한은 **방 전체 기준**이어야 "지금 나를 노리는 것은 둘" 이 성립한다.
            int busy = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var o = _enemies[i];
                if (o == e || o == null || !o.IsAlive) continue;
                if (o.IsWindingUp && ++busy >= cap) return false;
            }
            return true;
        }

        /// <summary>
        /// 동시 공격 상한에 걸렸을 때 다시 묻기까지의 시간.
        /// 간격(1~3초)보다 훨씬 짧아야 「자리가 나면 곧바로」가 된다.
        /// </summary>
        private const float AttackRetrySeconds = 0.15f;

        /// <summary>화상 걸린 몸에 붙어 도는 불 크기(px).</summary>
        /// <remarks>불이 몸을 둘러야 한다. 잡몹 상자가 100 px 남짓(84 × 1.2)이라 그보다 조금 크게.</remarks>
        private const float BurnFxSize = 112f;

        /// <summary>방에 들어선 직후 안 맞는 시간. 한 호흡만 준다.</summary>
        private const float RoomEntryInvulnSeconds = 1.0f;

        /// <summary>타격·피격 표시 크기(px). 예전 표시가 작아 때린 줄도 몰랐다.</summary>
        private const float HitFxSize = 96f;
        private const float HurtFxSize = 112f;
        private const float CritFxSize = 150f;

        /// <summary>몇 발 쏘고 자리를 옮기는가 (원거리).</summary>
        private const int ShotsBeforeMove = 2;
        private const float RepositionDistance = 190f;

        private static bool IsMelee(Unit e)
        {
            var k = e.Profile?.Kind;
            return k == AttackKind.Melee || k == AttackKind.Pulse;
        }

        /// <summary>
        /// 실제로 때릴 수 있는 거리.
        ///
        /// 정본 사거리는 근접도 1.2~1.8m(103~155px)라 그대로 쓰면 된다. 다만 그림 크기
        /// 때문에 맞붙어도 중심 사이가 90px 이라, 그보다 짧은 값이 들어오면 영영 닿지
        /// 않는다 — 근접에만 바닥을 깔아 준다.
        /// </summary>
        private float EffectiveRange(Unit e)
            => IsMelee(e) ? Mathf.Max(e.AttackRange, _config.MeleeAttackRange) : e.AttackRange;

        // ─────────────────────────────────────────
        // 정본 실수치 해석
        //
        // 정본(CH01_03_RUNTIME_DATA)은 배우마다 HP·공격력·이동속도·사거리·간격을
        // 실제 값으로 준다. 그 값이 있으면 **그대로** 쓴다.
        // 정본에 없는 창작 배우만 예전의 "표시 스탯 × 배율" 공식으로 되돌아간다.
        //
        // 거리·속도는 정본이 미터 단위다. 방 크기에서 얻은 _pxPerMeter 로 환산한다.
        // ─────────────────────────────────────────

        /// <summary>
        /// 화면에 세울 상자 크기. 그림은 그대로 두고 상자만 키운다 —
        /// 기준값(84·96·128·256)은 그림 캔버스에서 온 것이라 여기서 한 번에 곱한다.
        /// </summary>
        private Vector2 UnitBox(float w, float h)
            => new(w * _config.UnitScale, h * _config.UnitScale);

        /// <summary>
        /// 지금 방에서 잡몹에 붙는 배율. 챕터 곡선 × 방 곡선.
        ///
        /// ⚠ **여기 하나에서만 곱한다.** 스폰하는 자리가 여섯 곳인데(일반·정예·
        ///   구조대·소환·이벤트 …) 거기서 각자 곱하면 한 곳을 빠뜨리고,
        ///   그 방만 CH1 세기의 적이 서 있게 된다.
        ///
        /// 정본 스탯(`HasCanon`)에도 곱한다 — 정본 표는 **그 적이 어떤 놈인가**를
        /// 적은 것이지 몇 번째 챕터에서 만나는가를 적은 것이 아니다.
        /// </summary>
        private float EnemyGrowth()      // 공격력용
        {
            int roomNo = _canonRoom != null ? RoomNumberOf(_canonRoom.RoomId) : _roomIndex + 1;
            return _config.EnemyChapterMul(_runChapter) * _config.EnemyRoomMul(roomNo);
        }

        /// <summary>체력용 배율. 챕터별 체력 손질이 더 곱해진다(1챕터는 절반).</summary>
        private float EnemyHpGrowth()
        {
            int roomNo = _canonRoom != null ? RoomNumberOf(_canonRoom.RoomId) : _roomIndex + 1;
            return _config.EnemyChapterHpMul(_runChapter) * _config.EnemyRoomMul(roomNo);
        }

        private int EnemyHpOf(HostEntry e)
            => SandboxHp(Mathf.Max(1, Mathf.RoundToInt(
                   (e.HasCanon ? e.CanonHp : _config.EnemyHp(e.Hp))
                   * EnemyHpGrowth() * _config.EnemyHpMul)));

        private int EnemyAtkOf(HostEntry e)
            => Mathf.Max(1, Mathf.RoundToInt(
                   (e.HasCanon ? e.CanonAtk
                               : _config.EnemyAtk(e.Atk) * e.DamageMul) * EnemyGrowth()));

        // 적은 늘 싸우러 오는 중이다 — 정본의 EngageSpeed 를 쓴다.
        // MoveSpeed(1.0~2.5m/s)는 교전 전의 걸음이라, 그걸 쓰면 내(3.4~5.2)가 뒤로 걷기만 해도
        // 영영 안 잡힌다. 근접 적은 한 번도 닿지 못하고 과녁이 된다.
        private float EnemySpeedOf(HostEntry e)
            => e.HasCanon ? e.CanonEngageSpeed * _pxPerMeter : _config.EnemySpeed(e.Spd);

        private float EnemyRangeOf(HostEntry e)
            => e.HasCanon ? e.CanonRange * _pxPerMeter : _config.EnemyAttackRange * e.RangeMul;

        /// <summary>
        /// 엘리트는 거기서 한 번 더 줄인다. 「엘리트 방은 확실히 다른 방」이 되어야 한다 —
        /// 체력만 두꺼우면 시간만 오래 걸리는 방이지 어려운 방이 아니다.
        /// </summary>
        private const float EliteIntervalMul = 0.5f;

        // ── 몬스터 조정 (기획 2026-09-15) ────────────────────────
        //
        // 방에 **서는 자리**(방 · 무작위 방 · 보스 부하)에서만 곱한다.
        // `EnemyAtkOf` · `EnemyIntervalOf` 에 넣으면 상점 동료 · 중간 보스까지 같이 세진다.
        /// <summary>일반 몬스터 피해 2배. 엘리트 · 보스에는 안 붙는다.</summary>
        private const float NormalEnemyAtkMul = 2f;
        /// <summary>1챕터 엘리트 체력 · 공격력 2배 — 너무 약했다.</summary>
        private const float Ch1EliteStatMul = 2f;
        /// <summary>일반 몬스터 · 엘리트 공격 속도 1.5배(간격 ÷ 1.5).</summary>
        private const float EnemyHandSpeedMul = 1.5f;

        private float SpawnHpMul(bool elite) => elite && _runChapter == 1 ? Ch1EliteStatMul : 1f;
        private float SpawnAtkMul(bool elite)
            => elite ? (_runChapter == 1 ? Ch1EliteStatMul : 1f) : NormalEnemyAtkMul;

        /// <summary>
        /// 적이 얼마나 자주 때리는가. 배율은 `GameConfig` 에 있다 —
        /// 두 번 다시 조정하게 되어 인스펙터에서 돌릴 수 있게 뺐다.
        /// </summary>
        private float EnemyIntervalOf(HostEntry e, bool elite = false)
            => (e.HasCanon ? e.CanonInterval : _config.EnemyAttackInterval * e.IntervalMul)
               * _config.EnemyIntervalMul * (elite ? EliteIntervalMul : 1f);

        // 내가 탄 몸. 정본은 같은 배우라도 **적일 때와 내가 탔을 때 교전값을 따로** 준다
        // (attacks 의 AP_E### / AP_H##). 체력·공격력은 몸 자체의 것이라 적일 때와 같다.

        // ── 고스트 Lv 인계 ────────────────────────────────────
        //
        // **고스트 본체의 능력치는 의미가 없다.** 유령은 싸우지 않는다 —
        // 15초 시계를 들고 다음 몸까지 가는 것이 전부다.
        // 그래서 Lv 는 고스트가 **들고 다니다가 빙의하는 순간 그 몸에 얹는다.**
        //
        // ⚠ 고스트 HP(100)·감소 속도(초당 6.7)에는 **붙이지 않는다.**
        //   그 시계가 이 게임의 심장이라, 레벨로 늘어나면
        //   "몸을 안 갈아타도 버틴다" 가 되어 빙의를 고를 이유가 사라진다.
        //
        // HP 와 ATK 를 다르게 올리는 것도 같은 이유다. 둘 다 2.5배면 실질 전투력이
        // 여섯 배가 된다 — 2.5배 오래 버티면서 2.5배 빨리 죽인다.
        // **레벨은 생존을 사고, 화력은 몸이 판다.**
        // ⚠ 아래 두 배율은 **임시 폴백**이다. 호스트 표(`_levelStats`)에 값이 들어오면
        //   저절로 죽는다. 배율 하나를 전원에게 곱하면 23명이 같은 비율로 커져서
        //   성장해도 몸끼리의 관계가 안 변한다 — 그래서 표가 정답이다.
        private const float GhostHpPerLevel = 0.03f;
        private const float GhostAtkPerLevel = 0.016f;

        /// <summary>지금 고스트 Lv. 저장이 없으면 1 이다. 상한은 `GameConfig` 가 갖는다.</summary>
        private int GhostLevel
            => _player == null ? 1 : Mathf.Clamp(_player.GhostLevel, 1, _player.GhostLevelMax);

        private float GhostHpMul  => 1f + GhostHpPerLevel  * (GhostLevel - 1);
        private float GhostAtkMul => 1f + GhostAtkPerLevel * (GhostLevel - 1);

        /// <summary>
        /// 이 레벨에서 이 몸의 체력.
        ///
        /// **호스트가 가진 레벨 표가 먼저다.** 표가 있으면 그대로 읽고,
        /// 비어 있으면 임시로 배율을 곱한다 — 표 값이 아직 안 정해졌기 때문이다.
        /// 표가 채워지는 순간 배율 경로는 저절로 죽는다.
        /// </summary>
        private int LeveledHp(HostEntry e)
        {
            if (e != null && e.HasLevelStats)
            {
                var st = e.StatAt(GhostLevel);
                if (st.Hp > 0) return st.Hp;
            }
            return Mathf.RoundToInt(HostHpOf(e) * GhostHpMul);
        }

        private int LeveledAtk(HostEntry e)
        {
            if (e != null && e.HasLevelStats)
            {
                var st = e.StatAt(GhostLevel);
                if (st.Atk > 0) return st.Atk;
            }
            return Mathf.Max(1, Mathf.RoundToInt(HostAtkOf(e) * GhostAtkMul));
        }

        // ── 내가 입은 몸의 능력치는 전부 등급에서 나온다 (2026-09-15) ──
        //
        // ⚠ 예전에는 정본 절대값(`CanonHostHp` 등)을 그대로 썼다. 그래서 확정본이 준
        //   0~100 눈금(`e.Hp` · `e.Atk` · `e.Spd` · `e.AtkSpeed`)이 **게임에 하나도 안 걸렸다** —
        //   로비 표시에만 쓰이고 판은 정본으로 돌았다. 등급별 차등을 주려고 받은 값이
        //   차등을 전혀 못 만들고 있었다(기획 2026-09-15).
        //   이제 다섯 자리 전부 등급을 읽는다. 수치 곡선은 `GameConfig` 한 곳이다.

        private int HostHpOf(HostEntry e)
            => e == null ? 100 : _config.HpOfGrade(e.HpGrade);

        // 한 방 피해는 **공격력 등급(DPS) ÷ 공속 등급(횟수) ÷ 한 번에 나가는 탄 수** 다.
        // 공격력 등급만 보고 정하면 공속이 빠른 몸이 총량까지 같이 가져간다.
        //
        // ⚠ `DamageMul`(0.45~1.75)은 **더 이상 곱하지 않는다.** 그 칸은 정본 절대값과
        //   짝이던 보정이라, 등급이 DPS 를 정하는 지금 다시 곱하면 등급이 무의미해진다 —
        //   실제로 폭력배가 DPS 11.7, 화이트 위저드가 45.0 으로 네 배 가까이 벌어졌다.
        //   대신 **탄 수로 나눈다.** 두 발씩 나가는 몸(호퍼SMG · 코만도MG)이
        //   같은 등급으로 두 배를 때리면 안 된다.
        private int HostAtkOf(HostEntry e)
            => e == null ? 10
             : Mathf.Max(1, Mathf.RoundToInt(
                   _config.AtkOfGrade(e.AtkGrade, e.RateGrade) / Mathf.Max(1, e.ShotCount)));

        // 이동속도만은 교전 프로필이 안 맞아도 호스트 값을 쓴다 — 근접이냐 원거리냐와
        // 상관없는 값이라, 적 걸음으로 조종하게 두면 그 몸만 못 쓰게 된다.
        private float HostSpeedOf(HostEntry e)
            => e == null ? 180f : _config.MoveOfGrade(e.SpdGrade) * _pxPerMeter;

        // 호스트 프로필이 없으면 그 배우가 적일 때 쓰던 값을 그대로 쓴다.
        // 예전 공식(전역 900 × 배율)으로 돌아가면 정본 배우들과 격이 어긋난다.
        //
        // ⚠ **표에 적힌 미터가 곧 게임에서의 미터다.** 예전에는 근접이 아닌 몸에
        //   ×2 를 곱했다(2026-09-10 「원거리 사거리 2배로」). 그 탓에 확정본이 적은
        //   드라군 7.0 m 가 판에서는 14.0 m 가 되어, 10×16 m 방을 원거리 열넷이
        //   통째로 덮었다 — 근거리·중거리·원거리를 가른 의미가 사라졌다.
        //   로비 상세 카드는 곱하지 않은 값을 보여 주고 있었으므로 화면과 판정도 어긋났다.
        //   확정본 2026-09-14 기준으로 배율을 걷어낸다. 적이 쓰는 `EnemyRangeOf` 도
        //   곱하지 않으므로, 같은 몸이면 내가 입든 적이 입든 사거리가 같아진다.
        private float HostRangeOf(HostEntry e)
            => e == null ? _config.HostAttackRange
             : e.HasCanonHost ? e.CanonHostRange * _pxPerMeter
             : e.HasCanon     ? e.CanonRange * _pxPerMeter
             : _config.HostAttackRange * e.RangeMul;

        /// <summary>
        /// 탄속. 정본은 배우마다 다르게 준다 — 닌자 탄이 12.5m/s, 잡몹이 8.5m/s 다.
        /// 전부 같은 속도로 날면 "저 탄은 빠르니 지금 피해야 한다" 는 판단이 안 생긴다.
        /// </summary>
        private float ShotSpeedOf(HostEntry e, bool fromPlayer)
        {
            float mps = e == null ? 0f
                      : fromPlayer ? (e.CanonHostShotSpeed > 0f ? e.CanonHostShotSpeed
                                                                : e.CanonShotSpeed)
                      : e.CanonShotSpeed;
            return mps > 0f ? mps * _pxPerMeter
                            : fromPlayer ? _config.ShotSpeedPlayer : _config.ShotSpeedEnemy;
        }

        // 내 손맛만 당긴다 — 적 간격은 정본 그대로 둔다.
        //
        // ⚠ `HostAttackSpeedMul`(정본 간격을 0.3 배로 깎던 손잡이)은 **더 이상 안 쓴다.**
        //   등급이 초당 횟수를 직접 정하므로 그 위에 또 곱하면 자가 둘이 된다.
        //   격투가가 초당 4.2 회를 때리던 원인이 그 이중 곱이었다(기획 2026-09-15).
        //
        // ⚠ **전역 손 속도(`AttackSpeedMul`)를 여기서 미리 되돌린다.** 그 손잡이는
        //   적·보스·소환물의 속도를 한 번에 내리려고 둔 것이고, `Unit.TickAttack` 이
        //   마지막에 나눈다. 호스트는 등급이 **최종** 초당 횟수를 정해야 하므로
        //   (로비에 적힌 숫자가 곧 판에서 때리는 횟수여야 한다) 여기서 곱해 상쇄한다.
        //   상쇄하지 않으면 로비가 2.8 회라고 적고 판은 1.96 회를 때린다 — 실측으로 확인했다.
        private float HostIntervalOf(HostEntry e)
            => e == null ? _config.HostAttackInterval
             : _config.IntervalOfGrade(e.RateGrade) * _config.AttackSpeedMul / HostHandSpeedOf(e);

        // 기획 2026-09-15 — 아마존 공격 속도 2배, 근접 몸 1.5배. **아마존은 2배만** — 둘을 겹치지 않는다.
        // ⚠ 로비에 적힌 초당 횟수는 등급으로만 계산하므로 이 배율이 안 보인다.
        private const float AmazonHandSpeedMul = 2f;
        private const float MeleeHandSpeedMul = 1.5f;

        private static float HostHandSpeedOf(HostEntry e)
            => e.HostKey == "amazon" ? AmazonHandSpeedMul
             : e.Kind == AttackKind.Melee || e.Kind == AttackKind.Pulse ? MeleeHandSpeedMul
             : 1f;

        /// <summary>
        /// 옮겨 갈 자리. 플레이어를 계속 사거리 안에 두되 **옆으로** 돈다 —
        /// 뒤로 물러나면 도망으로 보이고, 앞으로 가면 근접과 다를 바가 없다.
        /// </summary>
        private Vector2 PickRepositionSpot(Unit e, Unit me)
        {
            var toMe = me.Position - e.Position;
            var side = new Vector2(-toMe.y, toMe.x).normalized;
            // 개체마다 좌우를 갈라 놓기만 하면 된다. EntityId 를 int 로 캐스팅하는 것은
            // 이미 폐기 예정이라 해시로 받는다 — 값의 의미는 안 쓰고 홀짝만 본다.
            if (((e.GetEntityId().GetHashCode() + _roomIndex) & 1) == 0) side = -side;

            // 옮겨 갈 자리도 같은 한계를 지킨다. 여기만 방 전체로 두면
            // 적이 테두리 위로 걸어 올라가 벽에 붙어 선다.
            return ClampedInField(e, e.Position + side * RepositionDistance);
        }

        /// <summary>
        /// 서로 겹치지 않게 밀어낸다.
        ///
        /// 전부 같은 목표(플레이어)로 달려가면 사거리가 비슷한 개체끼리 같은 지점에 겹쳐
        /// 한 마리처럼 보인다. 가까운 개체끼리만 반대로 밀어 덩어리를 푼다.
        /// </summary>
        private void Separate(Unit e, int index, float dt)
        {
            float r = _config.EnemySeparation;
            if (r <= 0f) return;

            Vector2 push = Vector2.zero;
            for (int j = 0; j < _enemies.Count; j++)
            {
                if (j == index) continue;
                var o = _enemies[j];
                if (o == null || !o.IsAlive) continue;

                var diff = e.Position - o.Position;
                float dist = diff.magnitude;
                if (dist >= r) continue;
                // 겹쳐 있으면 방향이 없다 — 인덱스로 갈라 서로 반대로 민다
                if (dist < 0.01f) { push += new Vector2((index % 2 == 0) ? 1f : -1f, 0.3f); continue; }
                push += diff / dist * (1f - dist / r);
            }
            if (push.sqrMagnitude < 0.0001f) return;

            e.Position += push.normalized * (e.MoveSpeed * SeparationSpeedRatio) * dt;
            ClampToField(e);
            ResolveObstacles(e);   // 스폰이나 밀림으로 갇힌 경우의 구제책
        }

        // ── 보스 ─────────────────────────────────────────────────

        /// <summary>
        /// 페이즈가 바뀌는 순간에 하는 일.
        ///
        /// 정본이 가장 세게 못박은 것이 "페이즈마다 **행동이** 바뀐다"는 것이다.
        /// 수치만 오르는 것은 페이즈가 아니라고 적혀 있다. 지금 우리가 데이터에서
        /// 그대로 살릴 수 있는 것은 둘이다 —
        ///   · **잡몹 소환**(MinionPool). 정본은 이것을 "교체 창"이라고 부른다.
        ///     보스는 빙의할 수 없으니, 몸을 갈아탈 기회는 이때 부르는 잡몹뿐이다.
        ///   · **예고 시간**. 페이즈마다 다르고, 피할 수 있느냐를 가르는 값이다.
        ///
        /// 이름 붙은 패턴(BurrowTrack·ConveyorReverse 등)은 그림과 함께 와야 해서
        /// 아직 우리 볼리로 흉내낸다. 페이즈마다 탄 수·확산·간격이 갈리게 해 뒀다.
        /// </summary>
        private void EnterBossPhase(Unit boss, int phase)
        {
            // ⚠ 아래 조기 반환보다 **먼저** 부른다. 정본 방 정보가 없는 방
            //   (테스트로 끼워 넣은 보스방)에서도 벽은 무너져야 한다.
            ApplyWallPhase(phase);

            var def = _canonRoom != null ? _canonRoom.BossPhase(phase) : null;
            if (def == null) return;

            // ⚠ **보스방에는 잡몹을 세우지 않는다** (2026-09-09).
            //   예고 도형이 가리고, 누구한테 맞았는지 헷갈린다.
            //   보스방은 보스 하나만 선다 — 몸은 들고 들어가는 것이다.

            _bus.Publish(new BossPhaseEvent
            {
                Phase = phase, Pattern = def.Pattern, MinionCount = 0,
            });
        }

        /// <summary>가려던 자리가 이만큼 넘게 잘리면 벽에 닿은 것으로 본다(제곱 픽셀).</summary>
        private const float ChargeClampEpsilon = 0.25f;

        private void TickBoss(Unit boss, Unit me, float dt)
        {
            // ⚠⚠ **보스도 나를 바라봐야 한다.**
            //
            //   `SetFacing` 이 잡몹 경로에만 있었다. 보스는 그 위에서 `continue` 로
            //   빠져나가므로 **한 번도 불리지 않았다** — `_facingIndex` 가 영영 -1 이고,
            //   `Unit.Apply()` 는 그 값이 음수면 첫 줄에서 돌아간다.
            //   그래서 보스는 40장을 다 갖고도 `unit_{키}_s` 한 장만 쓴다.
            //   공격·피격·걷기 그림이 전부 안 나온다. 실측: 가디언 8벌 5방향이
            //   다 붙어 있는데 `_facingIndex = -1` 이었다.
            //
            //   예전에는 보스가 나를 쫓아다녔고 `MoveToward` 가 방향을 세워 줘서
            //   가려져 있었다. 보스를 제자리에 세운 순간 드러났다.
            if (me != null) boss.SetFacing(me.Position - boss.Position);

            int before = _brain.Phase;
            _brain.UpdatePhase((float)boss.Hp / boss.HpMax);
            if (_brain.Phase != before)
            {
                _bus.Publish(new BossHpChangedEvent
                {
                    BossHp = boss.Hp, BossHpMax = boss.HpMax, Phase = _brain.Phase,
                });
                EnterBossPhase(boss, _brain.Phase);
            }

            // 돌진 중에는 다른 행동을 하지 않는다. 접촉하면 피해를 주고 멈춘다.
            if (_brain.ChargeLeft > 0f)
            {
                // ⚠ 아래에서 `_brain.Tick` 을 부르지 않고 돌아가므로 **여기서** 줄여야 한다.
                _brain.TickCharge(dt);
                // ⚠ 방 안에 붙들어 둔다. 그냥 더하면 보스가 벽을 뚫고 나가 화면 밖에서
                //   패턴을 계속 돌린다 — 무엇에 맞는지 알 수 없게 된다.
                var want = boss.Position + _brain.ChargeDir * (boss.MoveSpeed * _chargeSpeedMul) * dt;
                var got = ClampedInField(boss, want);
                boss.Position = got;

                // ⚠ **벽에 닿으면 거기서 끝이다.** 활강 시간은 그어 둔 띠 길이(방 대각선)로
                //   잡는데 실제로 갈 수 있는 거리는 방 끝까지뿐이다. 그대로 두면
                //   벽에 붙은 채로 남은 시간을 다 흘려보낸다 —
                //   실측: 0.6초 만에 y −743 에 닿고 **1.0초를 더 서 있었다.**
                //   화면에서는 "끝에 딱 서지 않고 미끄러진다" 로 보인다(기획 2026-09-07).
                //
                // ⚠ 간 거리를 **자리 두 개를 빼서** 재면 안 된다. 자리를 다시 읽는 값이
                //   프레임에 따라 한 박자 늦어서, 멀쩡히 가는 중에도 "안 움직였다" 로
                //   읽혀 1픽셀 만에 멈췄다. **가려던 자리가 잘렸는지**를 직접 본다.
                if (_brain.ChargeLeft > 0f
                    && (got - want).sqrMagnitude > ChargeClampEpsilon)
                    _brain.BeginCharge(Vector2.zero, 0f);
                // 유령은 보스 돌진도 통과한다. 여기서 멈춰 세우면 유령을 벽 삼아
                // 보스를 세울 수 있게 되어, 맞지도 않는 몸이 방패가 된다.
                // 보스 **몸이 스치면** 맞는다. 중심까지 54px 을 요구하면 보스가 나를
                // 밟고 지나가도 안 맞는다 — 256px 짜리 몸이 통째로 무해해진다.
                if (_host != null && !_chargeHitDone
                    && EdgeDistance(me, boss) <= _config.ShotHitRadius * 1.6f)
                {
                    _chargeHitDone = true;
                    DamagePlayer(Mathf.Max(1, Mathf.RoundToInt(boss.Atk * _chargeDamageMul)));
                    // 터지는 것은 **닿은 자리**다. 도형 중심에서 터뜨리면 보스가
                    // 출발한 자리에서 폭발이 나 무엇에 맞았는지 알 수 없다.
                    PlayFx(_chargeFx, me.Position, _chargeFxSize, loop: false);

                    // ⚠ **뚫고 가는 돌진은 여기서 안 멈춘다.**
                    //   멈춰 세웠더니 보스가 나에게 닿자마자 그 자리에 서 버려서
                    //   "날아오다 만다" 로 보였다. 그어 둔 줄 끝까지 가야 한 동작이다.
                    if (!_chargePierce) _brain.BeginCharge(Vector2.zero, 0f);
                }
                return;
            }

            TickBossPending(dt);

            // ⚠ 시험 모드 — 스스로는 아무것도 안 한다. 버튼으로 부른 예고만 굴린다.
            //
            // ⚠ **에디터 전용으로 감싼다.** 아래가 부르는 `BossBrain.TickTelegraph`·
            //   `TakeReady` 가 `#if UNITY_EDITOR` 안에 있어서, 가드 없이 두면
            //   에디터는 통과하고 **플레이어 빌드에서만** CS1061 로 터진다.
            //   실제로 APK 빌드가 여기서 멈췄다 (`BossIdleOnly` 는 빌드에서 늘 false 라
            //   죽은 분기인데, 죽었어도 컴파일은 돼야 한다).
#if UNITY_EDITOR
            if (BossIdleOnly)
            {
                if (_brain.IsTelegraphing)
                {
                    _telegraphPulse += dt;
                    boss.SetTellPose(true, _brain.TelegraphProgress);
                    PulseTelegraph(boss);
                    if (_dangerMove != _brain.Pending) BeginDanger(boss, me, _brain.Pending);
                    TickDanger(dt);
                    // 예고가 다 찼는지는 여기서 직접 본다 — `_brain.Tick` 을 안 부르므로
                    _brain.TickTelegraph(dt);
                    return;
                }
                var fired = _brain.TakeReady();
                if (fired != null)
                {
                    boss.SetTelegraph(false);
                    boss.SetTellPose(false);
                    if (!StrikeDanger(boss, me, fired)) ExecuteBossMove(boss, me, fired);
                    return;
                }
                boss.SetMoving(false);
                return;
            }
#endif

            // 두뇌가 거리를 보고 패턴을 고른다 — 붙으면 파괴구, 떨어지면 미사일·압착·돌진.
            // 표적이 없으면 아주 먼 것으로 친다(붙어야 쓰는 패턴이 헛돌지 않게).
            float distM = me != null
                ? Vector2.Distance(boss.Position, me.Position) / _pxPerMeter : 999f;

            // ⚠ **벽 보스는 나와 있는 동안에만 패턴을 고른다.**
            //   벽 뒤에 있는 동안 예고가 뜨면 아무도 없는 자리에 도형이 그려지고,
            //   나오기도 전에 때린다. 이 보스의 리듬 자체가 「나와 있을 때만」이다.
            var move = _brain.Tick(dt, distM, CanWallBossAct);

            // 예고 중에는 제자리에서 번쩍인다. 피할 시간을 주지 않으면 패턴이 아니라 사고다.
            if (_brain.IsTelegraphing)
            {
                _telegraphPulse += dt;
                // 자세는 예고 내내 한 번만 세운다. 색만 깜빡인다.
                boss.SetTellPose(true, _brain.TelegraphProgress);
                PulseTelegraph(boss);
                // 예고가 막 시작된 프레임에 도형을 **한 번** 굳힌다.
                if (_dangerMove != _brain.Pending) BeginDanger(boss, me, _brain.Pending);
                TickDanger(dt);
                return;
            }

            if (move == null)
            {
                boss.SetTelegraph(false);
                boss.SetTellPose(false);
                ClearDanger();

                // ⚠⚠ **보스에게 평타는 없다.**
                //
                //   한때 3.61 m 안에 들어오면 1.6초마다 후려치는 평타가 있었다.
                //   패턴 쿨이 8~20초이던 시절, 그 빈 시간을 메우려고 넣은 것이다.
                //
                //   지금 가디언의 쿨은 2~4초라 빈 시간이 없고, 무엇보다 평타는
                //   **예고도 도형도 없이 그냥 맞는** 유일한 공격이었다.
                //   근접 몸은 1.34 m 에서 때리므로 평타 사거리 안에 늘 들어와 있어,
                //   피할 방법 없이 초당 27씩 맞았다 — 예고와 패턴을 넣은 이유가
                //   "맞고 안 맞고는 피했느냐가 정한다" 인데 그것을 정면으로 깼다.
                //   (기획 2026-09-03 — "근접캐릭터는 이유 없이 그냥 맞아야돼")
                //
                //   보스가 주는 피해는 **바닥에 그린 것뿐**이다.

                // ⚠ **쫓아오느냐는 보스마다 다르다.**
                //
                //   크러셔는 컨베이어에 박힌 압축기라 제자리가 맞다. 그런데 그것을
                //   6종 공통으로 깔았더니 **짧은 패턴이 영영 안 쓰였다** —
                //   원거리 몸은 7.2 m 에서 서서 쏘는데 보스가 안 오면
                //   반경 3.5 m 짜리 「똬리」는 조건이 맞는 순간이 오지 않는다.
                //   거리 조건은 "안 닿으면 건너뛴다" 가 아니라 **"닿을 때까지 간다"** 다.
                //
                //   `ChaseStopMeters` 까지만 간다. 몸이 겹칠 때까지 붙으면 누가 누군지
                //   안 보이고, 그 거리가 곧 `RangeMeters`(가깝다의 기준)와 같아야
                //   붙어서 쓰는 패턴이 실제로 쓰인다.
                var def = _brain != null ? _brain.Entry : null;
                if (def != null && def.Chases && me != null
                    && Vector2.Distance(boss.Position, me.Position) > ChaseStopMeters * _pxPerMeter)
                {
                    // ⚠⚠ **`SetMoving(true)` 이 없으면 걷기 그림이 한 번도 안 나온다.**
                    //   `MoveToward` 는 자리만 옮긴다. 걷기는 시간이 아니라 실제
                    //   이동에 매여 있어서(`Unit.SetMoving`) 이 줄이 빠지면 보스가
                    //   미끄러지듯 정지 그림으로 다가온다 — 보스 6종 walk 시트
                    //   60장이 통째로 죽어 있었다.
                    boss.SetMoving(true);
                    boss.MoveToward(me.Position, dt);
                    return;
                }

                boss.SetMoving(false);
                return;
            }

            boss.SetTelegraph(false);
            boss.SetTellPose(false);   // 때리는 순간에는 제 방향 자세로 돌아온다
            // 도형이 있는 패턴은 **그려 둔 그것**으로 친다. 없는 것만 옛 경로로 간다.
            if (!StrikeDanger(boss, me, move)) ExecuteBossMove(boss, me, move);
        }

        private void PulseTelegraph(Unit boss)
        {
            boss.SetTelegraph(Mathf.Repeat(_telegraphPulse, 0.16f) < 0.08f);
        }

        // ── 정본이 이름 붙인 보스 패턴 셋 ─────────────────────────
        //
        // 지금까지는 셋 다 "부채꼴·돌진·링" 으로 흉내만 냈다. 수치만 다른 같은 패턴은
        // 정본이 못박은 "페이즈마다 행동이 바뀐다" 를 만족하지 못한다.

        private const int LaneCount = 3;
        private const float CrackSeconds = 0.85f;   // 정본 B01 예고 0.85초
        private const float BeamSeconds = 0.7f;
        private const float SlamSeconds = 1.0f;     // 정본 B02 예고 1.0초
        private const float ShieldSeconds = 4.0f;
        private const float VenomSeconds = 5.0f;

        private int _laneParity;                    // 번갈아 — 쓸 때마다 줄이 바뀐다
        private float _pendingTimer;
        private int _pendingDamage;
        private Vector2 _pendingAt;
        private bool _pendingIsBeam;
        private int _pendingLane = -1;

        /// <summary>보스 실드가 남은 시간. 0 보다 크면 **정면에서 온 피해**가 줄어든다.</summary>
        private float _bossShield;

        /// <summary>
        /// 방패에 막혔을 때 남는 피해. 정본 「피해 90% 감소」 그대로 0.10 이다.
        ///
        /// ⚠ 한때 0.45(55% 감소)였다. 정본과 다른 값을 쓸 이유가 없다.
        /// </summary>
        private const float BossShieldDamageMul = 0.10f;

        /// <summary>방패가 막는 각도. 정본 「정면 120°」 — 반각 60° 안이면 막힌다.</summary>
        private const float BossShieldHalfDegrees = 60f;

        /// <summary>
        /// 이 공격이 보스 방패에 막히는가.
        ///
        /// ⚠ **전방향으로 막으면 안 된다.** 정본이 정면 120° 라고 못박았고,
        ///   회피 지시도 「뒤로 — 방패는 앞만 막는다」다. 전방향으로 90% 를 깎으면
        ///   그 지시가 거짓말이 되고, 등 뒤로 도는 플레이가 보상받지 못한다.
        /// </summary>
        private bool BlockedByBossShield(Unit boss, Vector2 from)
        {
            if (_bossShield <= 0f) return false;
            var facing = boss.Facing;
            if (facing.sqrMagnitude < 0.0001f) return true;   // 방향을 모르면 막는 쪽으로
            var toAttacker = from - boss.Position;
            if (toAttacker.sqrMagnitude < 0.0001f) return true;
            return Vector2.Angle(facing, toAttacker) <= BossShieldHalfDegrees;
        }

        /// <summary>
        /// 번갈아 솟는 레이저. 바닥에 **금이 먼저 간다** — 어디서 솟는지 안 보이면
        /// 예고가 아니라 사고다. 금이 사라지는 순간 그 자리에서 광선이 솟는다.
        /// </summary>
        private void PopupLaser(int damage)
        {
            _laneParity = 1 - _laneParity;
            float laneW = _roomSize.x / LaneCount;
            for (int i = _laneParity; i < LaneCount; i += 2)
            {
                float x = laneW * (i + 0.5f);
                SpawnField(new Vector2(x, -_roomSize.y * 0.5f), laneW * 0.42f,
                           CrackSeconds, FieldEffect.Damage, 0, fromPlayer: false);
            }
            _pendingIsBeam = true;
            _pendingLane = _laneParity;
            _pendingDamage = damage;
            _pendingTimer = CrackSeconds;
        }

        /// <summary>
        /// 실드를 두르고 내려찍는다. 실드 동안 피해가 줄어드는 것이 핵심이다 —
        /// "지금은 때릴 때가 아니라 피할 때" 라는 구간을 만든다.
        /// </summary>
        private void ShieldCycle(Unit boss, Unit me, int damage)
        {
            // 정본 EV_CH3_03 보스 대비 — 사 둔 파쇄가 첫 방어막을 그냥 없앤다.
            // 한 번만 쓴다. 없애 놓고도 내려찍기는 그대로 온다 —
            // 산 것은 방어막을 깨는 수단이지 안전이 아니다.
            if (_bossShieldBreak) { _bossShieldBreak = false; _bossShield = 0f; }
            else _bossShield = ShieldSeconds;
            boss.SetTelegraph(true);
            SpawnField(me.Position, 150f, SlamSeconds, FieldEffect.Damage, 0, fromPlayer: false);
            _pendingIsBeam = false;
            _pendingAt = me.Position;
            _pendingDamage = damage;
            _pendingTimer = SlamSeconds;
        }

        /// <summary>독구름. 플레이어가 선 자리를 물들여 그 자리를 못 쓰게 만든다.</summary>
        private void VenomCloud(Unit me, int count)
        {
            int n = Mathf.Clamp(count, 1, 2);
            for (int i = 0; i < n; i++)
            {
                var off = i == 0 ? Vector2.zero
                                 : new Vector2(UnityEngine.Random.Range(-160f, 160f), UnityEngine.Random.Range(-160f, 160f));
                SpawnField(me.Position + off, 150f, VenomSeconds,
                           FieldEffect.Curse, 4, fromPlayer: false);
            }
        }

        private void TickBossPending(float dt)
        {
            if (_bossShield > 0f) _bossShield -= dt;
            if (_pendingTimer <= 0f) return;
            _pendingTimer -= dt;
            if (_pendingTimer > 0f) return;

            if (_pendingIsBeam)
            {
                float laneW = _roomSize.x / LaneCount;
                for (int i = _pendingLane; i < LaneCount; i += 2)
                    SpawnField(new Vector2(laneW * (i + 0.5f), -_roomSize.y * 0.5f),
                               laneW * 0.42f, BeamSeconds, FieldEffect.Damage,
                               _pendingDamage, fromPlayer: false);
            }
            else
            {
                SpawnField(_pendingAt, 150f, 0.35f, FieldEffect.Damage,
                           _pendingDamage, fromPlayer: false);
            }
            _pendingLane = -1;
        }

        private void ExecuteBossMove(Unit boss, Unit me, BossMove m)
        {
            int dmg = Mathf.RoundToInt(boss.Atk * m.DamageMul);
            switch (m.Pattern)
            {
                case BossPattern.Volley:
                    FireFan(boss, me.Position, m.ShotCount, m.SpreadDegrees, dmg);
                    break;

                case BossPattern.Ring:
                    // 사방 360° — 붙어 있으면 피하기 어렵다. 거리를 벌리게 만드는 패턴.
                    FireFan(boss, me.Position, m.ShotCount, 360f - 360f / m.ShotCount, dmg);
                    break;

                case BossPattern.AimedBurst:
                    FireFan(boss, me.Position, m.ShotCount, m.SpreadDegrees, dmg);
                    break;

                case BossPattern.Charge:
                    _chargeDamageMul = m.DamageMul;
                    _chargeSpeedMul = ChargeSpeedMul;
                    _chargePierce = false;
                    _chargeHitDone = false;
                    _brain.BeginCharge(me.Position - boss.Position, ChargeSeconds);
                    break;

                case BossPattern.Summon:
                    SummonMinions(boss, m.ShotCount);
                    break;

                case BossPattern.PopupLaser:  PopupLaser(dmg); break;
                case BossPattern.ShieldCycle: ShieldCycle(boss, me, dmg); break;
                case BossPattern.VenomCloud:  VenomCloud(me, m.ShotCount); break;
            }
        }

        /// <summary>
        /// 부채꼴로 흩뿌린다. 보스 패턴과 플레이어 액티브 스킬이 함께 쓴다.
        ///
        /// ⚠ <paramref name="fromPlayer"/> 를 반드시 넘긴다. 이 값이 틀리면 **내가 쏜 탄이
        ///    나를 때린다** — 부채꼴의 시작점이 곧 내 자리라 16발이 그 자리에서 전부
        ///    나에게 꽂힌다. 실제로 액티브 스킬을 붙이다가 한 번 그렇게 만들었다.
        /// </summary>
        private void FireFan(Unit from, Vector2 at, int count, float spanDeg, int damage,
                             bool fromPlayer = false)
        {
            // 탄은 **화면 끝까지 나가야 한다.** 수명이 짧으면 중간에 사라져
            // 멀찍이 떨어진 곳이 안전지대가 되고, 탄막을 피할 이유가 없어진다.
            float speed = fromPlayer
                ? _config.ShotSpeedPlayer * _buffs.ShotSpeedMul
                : _config.ShotSpeedEnemy;
            float reach = _roomSize.magnitude;
            float life = reach / Mathf.Max(1f, speed) + 0.25f;

            for (int i = 0; i < count; i++)
            {
                float off = count == 1 ? 0f : -spanDeg * 0.5f + spanDeg * i / (count - 1);
                var shot = RentShot();
                if (shot == null) return;
                shot.SetSprite(ShotSpriteOf(from), ShotKindOf(from),
                               LoopsFrames(ShotKindOf(from)));
                shot.Fire(from.Position, at, speed, damage,
                          fromPlayer, null, _config.ShotSize * 1.15f,
                          fromPlayer ? ShotPlayerColor : ShotBossColor,
                          life, angleOffsetDeg: off,
                          // 액티브 스킬 탄에도 버프가 실려야 한다. 여기만 빠져 있어서
                          // 흡혈 카드를 먹고 액티브 스킬을 쓰면 한 방울도 안 돌았다.
                          slowPercent: fromPlayer ? _buffs.SlowPercent : 0,
                          lifestealPercent: fromPlayer ? _buffs.LifestealPercent : 0,
                          pierce: fromPlayer && _buffs.Pierce,
                          bounces: fromPlayer ? _buffs.Bounces : 0);
            }
        }

        /// <summary>보스만 노리다 둘러싸이게 만든다. 방 상한을 넘지 않게 막는다.</summary>
        private void SummonMinions(Unit boss, int count)
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return;
            if (_enemies.Count > MaxRoomUnits) return;

            for (int i = 0; i < count; i++)
            {
                var e = hosts[(_enemies.Count * 3 + i * 7) % hosts.Count];
                var u = NewUnit($"Minion_{e.HostKey}_{_enemies.Count}");
                NoteMetHost(e);   // 상점이 파는 목록은 이 판에서 만난 몸뿐이다
                u.Setup(UnitSide.Enemy, e.HostKey, e.DisplayName, UnitGet(e.SpriteKey),
                        Mathf.Max(1, Mathf.RoundToInt(EnemyHpOf(e) * 0.6f)),
                        Mathf.RoundToInt(EnemyAtkOf(e) * NormalEnemyAtkMul),
                        EnemySpeedOf(e),
                        EnemyRangeOf(e),
                        EnemyIntervalOf(e) / EnemyHandSpeedMul,
                        UnitBox(78f, 72f), isBoss: false, profile: e);

                // 보스(160px)와 겹치지 않게 바깥에 원형으로 흩는다
                float a = (i / (float)count) * Mathf.PI * 2f + _enemies.Count * 0.7f;
                u.Position = boss.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * SummonRadius;
                u.IsAggro = true;   // 불러낸 것들은 기다리지 않는다
                ClampToField(u);
                ResolveObstacles(u);
                _enemies.Add(u);
            }
        }

        // ── 공격 방식 ────────────────────────────────────────────
        // 호스트마다 교전 거리·탄 수·발사 간격이 달라야 "어떤 몸을 뺏었는가"에 의미가 생긴다.
        // 사거리·간격·피해 배율은 Setup 시점에 이미 반영돼 있다(Unit.Atk/AttackRange/AttackInterval).

        private void PerformAttack(Unit attacker, Unit target, bool fromPlayer)
        {
            // 공격 방식과 무관하게 몸은 똑같이 쏘는 동작을 한다.
            // 여기 한 곳에서 켜야 근접·원거리·보스가 따로 놀지 않는다.
            attacker.PlayAttack();

            // 이번 공격에 실리는 패시브 배수를 여기서 **한 번** 잡는다.
            // ⚠ 복제(C032)는 다시 잡지 않는다 — 같은 프레임의 같은 공격이다.
            if (!_echoing) BeginSwing(fromPlayer);
            // 평타 소리는 **내 몸만** 낸다. 적 수십이 쏘는 소리까지 내면 화면이 소리로 덮인다.
            if (fromPlayer && !_echoing) GameSound.HostAttack(attacker.Key);

            // C032 영혼 복제 — 8타째면 이 공격을 한 번 더 낸다.
            // 자세를 다시 잡지 않고 **같은 프레임에** 한 번 더 내보낸다.
            if (fromPlayer && CountEcho())
            {
                _echoing = true;
                PerformAttack(attacker, target, true);
                _echoing = false;
            }

            // C030 유령 포대 — 복제된 공격은 세지 않는다(유효 기본 공격이 아니다)
            if (fromPlayer && !_echoing) TryGhostTurret(attacker);

            var p = attacker.Profile;
            var kind = p?.Kind ?? AttackKind.Single;

            switch (kind)
            {
                case AttackKind.Melee:
                case AttackKind.Pulse:
                {
                    // 구루는 주변을 한꺼번에 치지 않고 **하나만** 친다(기획 2026-09-15).
                    // 추가 발사 카드가 붙으면 탄 수 대신 **맞는 적 수**가 늘어난다.
                    bool single = attacker.Key == "guru";
                    int maxHits = single && fromPlayer ? 1 + _buffs.ExtraShots + SprayExtraShots : 1;
                    MeleeStrike(attacker, target, fromPlayer,
                                hitAll: kind == AttackKind.Pulse && !single, maxHits: maxHits);
                    break;
                }

                default:
                {
                    // 다중 사격 버프는 확산이 아닌 방식에도 탄을 더한다 (플레이어 한정).
                    // 카드 한 장 = 한 발. 확률이 아니라 **항상** 붙는다.
                    int extra = fromPlayer ? _buffs.ExtraShots + SprayExtraShots : 0;
                    // 정본 projectiles 는 확산이 아닌 몸에도 탄 수를 준다 (갱스터 3발).
                    // 확산일 때만 세면 그 값이 버려진다.
                    int shots = p == null ? 1 : fromPlayer ? p.ShotCount : p.EnemyShotCount;
                    // 한 번에 나가는 탄에 상한을 둔다. 수치 하나가 어긋나도 화면이
                    // 탄으로 덮이거나 줄이 방 밖까지 뻗지 않게 하는 마지막 방어선이다.
                    int n = Mathf.Clamp(shots + extra, 1, MaxShotsPerVolley);

                    // ⚠ **여러 발이면 언제나 부채꼴이다.**
                    //
                    // 예전에는 몸이 원래부터 다연발이면(코만도 기관총 2발) 한 줄로
                    // 앞뒤로 세우고, `추가 발사` 카드로 늘어난 것만 부채꼴로 벌렸다.
                    // 그런데 줄로 세운 두 발은 화면에서 **한 줄기로 보인다.** 그러다
                    // 카드를 한 장 먹는 순간 전체가 부채꼴로 바뀌어, 2발이 3발이 되는 것이
                    // 1발이 3발이 된 것처럼 읽혔다 — "왜 두 발이 늘어나냐" 는 말이 그것이다.
                    //
                    // 몇 발을 쏘는 몸인지가 늘 보여야 카드를 먹었을 때의 변화도 정직해진다.
                    bool fan = n > 1;

                    // 벌리는 각도. 확산 몸은 정본 값을 쓰고, 카드로 늘어난 몸은 발당 14°.
                    // 8° 는 탄 상자(104px)가 겹쳐 한 덩어리로 보이고, 24° 를 넘기면
                    // 조준한 적을 양쪽 다 빗나간다. 그 사이 값이다.
                    float span = kind == AttackKind.Spread && p.SpreadDegrees > 0f
                               ? p.SpreadDegrees : 0f;
                    if (fan && n > 1) span = Mathf.Max(span, ExtraFanDegrees * (n - 1));
                    // 난사(폭력배)는 각도를 스스로 정한다 — Lv6~10 에 20° 가 40° 로 벌어진다
                    if (fromPlayer && SprayExtraShots > 0) span = Mathf.Max(span, SprayDegrees);

                    // 정본은 공격력을 **한 번의 공격**에 준다. 탄 수는 따로 적혀 있으므로
                    // 나눠 실어야 탄 수가 그대로 화력 배수가 되지 않는다 — 확산은 맞히기
                    // 쉬운 대신 한 발이 약한 것이 맞다.
                    int split = Mathf.Max(1, shots);

                    // 정본 BUF_A01 마지막 탄창 — 확산의 **마지막 한 발**이 더 아프다.
                    // 마지막 발만 강하면 "다 맞히는 것" 이 아니라 "끝까지 붙어 있는 것" 이 이득이 된다.
                    for (int i = 0; i < n; i++)
                    {
                        float off = !fan || n == 1 ? 0f : -span * 0.5f + span * i / (n - 1);
                        // 부채꼴은 총구 한 점에서 시작하므로 태어나는 순간에는 전부 겹친다.
                        // 조준선과 **직각으로** 조금씩 밀어 두면 나가는 순간부터 갈라져 보인다.
                        float side = !fan || n == 1
                            ? 0f
                            : (i - (n - 1) * 0.5f) * _config.ShotSize * FanSideRatio;
                        FireShot(attacker, target, fromPlayer, off,
                                 lastShot: fromPlayer && i == n - 1, split: split,
                                 sideOffset: side);
                    }
                    break;
                }
            }
        }

        /// <summary>근접·광역은 탄을 쓰지 않고 즉시 판정한다. 대신 타격 위치에 섬광만 남긴다.</summary>
        /// <summary>근접으로 마무리했다. 시너지 계기이자 회복 시점이다.</summary>
        private void OnMeleeFinish(Unit attacker)
        {
            PublishHp();
        }

        // ── 덩치 보정 ────────────────────────────────────────────
        //
        // 거리 판정이 전부 **중심에서 중심까지**였다. 잡몹은 상자가 84px 라 큰 차이가
        // 없지만 보스는 256px 다 — 반경 108px 인데 아마존 근접 사거리가 101px 이라,
        // **보스 몸 안으로 7px 들어가야 때려졌다.** "완전 겹쳐야 맞는다" 가 그 증상이다.
        //
        // 탄은 이미 `e.BodyRadius` 를 보고 가장자리를 재고 있었다. 근접과 보스 패턴만
        // 빠져 있었으므로 같은 방식으로 맞춘다.
        //
        // ⚠ 반경을 통째로 빼지 않는다. 그러면 **모든 근접이 한 뼘씩 길어져** 잡몹전까지
        //   같이 바뀐다. 기준 크기를 넘는 **초과분만** 준다 —
        //   보통 몸끼리는 지금과 똑같고, 덩치 큰 것만 가장자리가 제자리를 찾는다.

        /// <summary>기준 몸 반경(px). 잡몹·호스트 상자(84)의 반경이다.</summary>
        private const float StandardBodyRadius = 84f * 0.42f;

        /// <summary>기준보다 큰 만큼. 큰 몸은 그만큼 가장자리가 멀리 있다.</summary>
        private static float BodyExcess(Unit u)
            => u == null ? 0f : Mathf.Max(0f, u.BodyRadius - StandardBodyRadius);

        /// <summary><paramref name="to"/> 의 **몸 가장자리**까지의 거리.</summary>
        private static float EdgeDistance(Unit from, Unit to)
            => from == null || to == null
             ? float.MaxValue
             : Vector2.Distance(from.Position, to.Position) - BodyExcess(to);

        private void MeleeStrike(Unit attacker, Unit target, bool fromPlayer, bool hitAll, int maxHits = 1)
        {
            var p = attacker.Profile;
            float reach = attacker.AttackRange * (fromPlayer ? _buffs.RangeMul : 1f);

            // 슬러거 "탄환 반사" — 휘두르는 범위 안의 적 탄을 지운다
            if (p != null && p.ReflectsShots && (IsReflectingAll || Roll(ReflectChance)))
            {
                for (int i = 0; i < _shots.Count; i++)
                {
                    var s = _shots[i];
                    if (!s.IsActive || s.FromPlayer == fromPlayer) continue;
                    if (Vector2.Distance(s.Position, attacker.Position) > reach) continue;
                    // 지우지 말고 **되받아친다.** 도탄이 생기기 전에는 지우는 수밖에
                    // 없었지만, 이제 방향을 뒤집으면 진짜 반사가 된다.
                    // 지속 중이면 **되돌려 보내는** 것이 아니라 내 탄으로 만든다 —
                    // 튕기기만 하면 원래 쏜 적이 자기 탄에 안 맞는다.
                    if (fromPlayer) GameSound.Cue("hit.reflect");
                    if (IsReflectingAll && fromPlayer) ReflectShot(s);
                    else if (!s.Bounce((s.Position - attacker.Position).normalized)) s.Despawn();
                }
            }

            if (fromPlayer)
            {
                // 겨눈 적을 **먼저** 친다(첫 바퀴는 겨눈 적만, 둘째 바퀴는 나머지).
                // 목록 순서대로만 치면 한 명만 치는 몸이 옆의 엉뚱한 적을 때린다.
                int hits = 0;
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int i = _enemies.Count - 1; i >= 0; i--)
                    {
                        if (i >= _enemies.Count) continue;   // 앞에서 죽어 목록이 줄었다
                        var e = _enemies[i];
                        if (e == null || !e.IsAlive) continue;
                        if ((pass == 0) != (e == target)) continue;
                        // 몸 가장자리까지 잰다 — 보스처럼 큰 몸은 중심이 멀어도 몸은 코앞이다.
                        if (EdgeDistance(attacker, e) > reach) continue;
                        Burst(e.Position, true);
                        GameSound.Cue("hit.enemy");
                        bool wasAlive = e.IsAlive;
                        HitEnemyWith(e,
                            Mathf.RoundToInt(attacker.Atk * _buffs.AttackMul * EchoMul * SwingMul(fromPlayer)), p);
                        // 정본 S04 흡혈 마무리 — 근접으로 끝냈을 때만 회복이 터진다.
                        // 흡혈을 쌓는 몸과 터뜨리는 몸이 달라 **갈아타야만** 성립한다.
                        if (wasAlive && !e.IsAlive) OnMeleeFinish(attacker);
                        if (!hitAll && ++hits >= maxHits) return;
                    }
                }
                return;
            }

            // 유령에게는 근접도 헛손질이다. 때리는 시늉(`Burst`)까지 지우면 적이
            // 멈춰 선 것처럼 보이므로 **휘두르기는 남기고 명중만 없앤다.**
            Burst(target.Position, false);
            if (_host == null) return;
            DamagePlayer(attacker.Atk);
        }

        /// <summary>피해 없는 시각 효과. 근접 공격이 화면에서 아무 일도 없어 보이는 것을 막는다.</summary>
        private void Burst(Vector2 at, bool fromPlayer)
        {
            var v = RentShot();
            if (v == null) return;
            // 풀에서 꺼낸 탄은 **직전에 쏜 무기의 그림**을 그대로 들고 있다.
            // 근접 타격 표시는 원작 그림이 아니므로 기본 탄으로 되돌리고 색조를 입힌다.
            v.SetSprite(ShotFrames(null));
            v.Fire(at, at + Vector2.up, 0f, 0, fromPlayer, null,
                   _config.ShotSize * 2.2f,
                   fromPlayer ? ShotPlayerColor : ShotEnemyColor, 0.12f);
        }

        // 무기 계열마다 원소가 붙던 표(`StatusOfKind`)는 뺐다.
        // 정본에 그런 규칙이 없다 — 정본은 화상·빙결·저주를 **버프가 켜 줄 때만** 건다
        // (BUF_T02 살라만더·드라군 / BUF_T03 백마법사·영매 / BUF_S05 영매·드라군).
        // 기본 공격이 항상 원소를 묻히면 그 버프 여섯 개가 팔 물건을 잃는다.
        // 상태이상 자체(ApplyBurn·ApplyFreeze·ApplyCurse)는 장판·액티브 스킬이 계속 쓴다.
        // 버프로 다시 잇는 것은 추가 기획이 나온 뒤에 한다.

        // ── 설치물(자동 포탑) ──────────────────────────────────────
        //
        // 정본에서 마지막까지 남아 있던 구멍. 로봇의 정체성("배치 네트워크")이자
        // 버프 2종·시너지 2종이 여기 매여 있었다.

        private const int MaxDeployables = 4;
        private const float DeploySeconds = 8f;
        private const float DeployRange = 420f;
        private const float DeployFireInterval = 0.85f;
        private const float DeployCooldown = 4.5f;
        private const string DeployHostKey = "robot";

        /// <summary>포탑 그림. 무대 소품으로 이미 들어와 있는 삼각대 기관포다.</summary>
        private const string TurretSpriteKey = "obj_turret";

        private readonly List<Deployable> _deployables = new();
        private float _deployCooldown;

        /// <summary>
        /// 로봇의 몸일 때만 포탑이 나간다. 쿨다운으로 저절로 깔린다 —
        /// 버튼을 하나 더 두면 조작이 늘고, 이 게임의 조작은 이동과 사격 둘뿐이다.
        /// </summary>
        /// <summary>
        /// 보스방은 제 바닥을 쓴다. 원작에서도 보스마다 아레나가 따로 있다 —
        /// 같은 던전 바닥 위에서 싸우면 "여기가 그 방" 이라는 느낌이 안 난다.
        /// 전용 바닥이 아직 없으면 기본 바닥으로 돌아간다.
        /// </summary>
        private void ApplyRoomFloor()
        {
            if (_floorImage == null) return;

            // 일반 방 바닥은 **챕터마다 심리스 타일 한 장**이다.
            //
            // 예전에는 방 한 칸을 통째로 그린 큰 그림을 챕터 × 템플릿(18장) 썼다.
            // 그런데 그 그림들은 90° 수직 탑뷰에 어둡기만 하고 격자가 없어서
            // **어디로 가고 있는지가 화면에서 안 읽혔다.** 큰 그림은 방 높이가
            // 바뀔 때마다 늘어나 뭉개지기도 했다.
            //
            // 작은 체크무늬 타일을 반복하면 격자가 살아나 이동감이 생기고,
            // 48방을 3장으로 덮는다. 아틀라스에 들어 있어 따로 물고 있을 필요도 없다.
            int chapter = Mathf.Clamp(_runChapter, 1, 3);


            // ── 지금은 **모든 방이 같은 배경 한 장**을 쓴다 ────────────────
            //
            // 챕터 × 템플릿 18장이 이미 있지만 전부 못 쓴다:
            //   · 크기가 720×1530 (또는 보스 1260) 이다. 방은 936 이라 세로로 61 % 로
            //     **눌려서** 정사각 타일이 납작한 직사각형이 되고 원이 타원이 된다.
            //   · 평균 명도가 8~26 이다. 화면이 새까매서 어디로 가는지 안 읽힌다.
            //     (규격이 맞는 twin_platform 은 33.6 · 상위10% 50.8)
            //
            // 그래서 규격·격자·밝기가 모두 맞는 한 장을 전 방에 깐다.
            // **새 배경이 오면** `InterimRoomFloor` 를 지우고 아래 두 줄의 주석을 푼다 —
            // 그때 확인할 것은 캔버스 720×936 · 바닥 줄눈 36 px · 안쪽 (72,144)~(648,864) 이다.
            // 배경이 다 온 챕터는 **방마다 제 지형 배경**을 쓴다.
            // 아직 안 온 챕터·보스방은 통과한 CH1 배경 한 장으로 버틴다.
            // 새 배경이 오면 `FloorReadyChapters` 를 올리면 그만이다.
            //
            // ⚠ 챕터는 **방이 들고 있는 값**을 쓴다. 플레이어 진행도(`CurrentChapter`)로 읽으면
            //   방과 어긋날 수 있고(디버그로 방을 건너뛸 때 실제로 CH2 방이 CH1 배경을 받았다),
            //   무엇보다 배경은 **그 방의 성질**이지 플레이어의 상태가 아니다.
            //
            // ⚠ 예전에는 보스방도 공용 아레나 한 장으로 보냈다. 보스 전용 바닥이
            //   720×1260 이라 936 방에 눌려 들어가고 명도도 8~29 라 새까맸기 때문이다.
            //   **58차에 여섯 장이 720×936 으로 다시 왔다** — 명도 33.6~48.2 로
            //   지금 쓰는 무대 배경들과 같은 수준이다. 막을 이유가 없어졌으므로 막지 않는다.
            // 정본이 보스방이라고 적어 두지 않았는데 보스가 서는 방(테스트로 끼워 넣은 방)은
            // `FloorKeyOf` 가 그 방 원래 지형 바닥을 준다. 보스가 있으면 보스 바닥이 이긴다.
            bool forcedBossRoom = _roomBoss != null && (_canonRoom == null || !_canonRoom.IsBoss);
            string floorKey = forcedBossRoom
                              ? $"roomfloor_{_roomBoss.BossKey}"
                              : FloorKeyOf(_canonRoom, chapter);

            // 장애물도 같은 무대 것을 찾도록 이름만 떼어 둔다.
            // `roomfloor_env_junkyard` → `junkyard`, 연구소(`roomfloor_ch1_*`)는 빈 값.
            //
            // ⚠ 보스방은 바닥 이름이 `roomfloor_crusher` 라 여기서 무대를 못 뽑는다.
            //   그렇다고 빈 값으로 두면 쓰레기장 한복판에서 연구소 상자와 싸우게 된다.
            //   보스는 제 무대를 알고 있으므로(원작 스테이지) 그것으로 뽑는다.
            const string EnvPrefix = "roomfloor_env_";
            _floorEnv = forcedBossRoom ? BossEnvOf(_roomBoss.BossKey)
                      : _canonRoom != null && _canonRoom.IsBoss
                        ? BossEnvOf(BossSlug(_canonRoom.BossId))
                        : floorKey.StartsWith(EnvPrefix) ? floorKey.Substring(EnvPrefix.Length) : string.Empty;

            // 못 찾은 바닥이 있을 때 떨어질 곳.
            //   보스방  → 공용 아레나. 보스만 회색 격자 위에서 싸우는 일이 없도록
            //   일반 방 → 통과한 배경 한 장(`InterimRoomFloor`)
            //
            // ⚠ 일반 방에도 대비책이 필요해졌다. 60방 배정이 CH5 에 `roomfloor_env_lab`
            //   (연구소)를 쓰는데 **그 그림이 아직 없다**(8개 방). 대비책이 없으면
            //   그 여덟 방이 지난 방 바닥을 그대로 달고 다닌다.
            //   `InterimRoomFloor` 도 연구소 배경이라 자리는 맞는다 — 그림이 오면 자동으로 이긴다.
            string fallback = _canonRoom != null && _canonRoom.IsBoss ? BossArenaFloor : InterimRoomFloor;
            LoadRoomFloorAsync(floorKey, fallback).Forget();   // fire-and-forget: 바닥은 한 프레임 늦어도 된다

            // 넓은 화면에서 필드 옆에 남는 자리를 같은 무대의 벽으로 채운다
            // (`BattleDirector.RoomSide.cs`). 폰에서는 남는 자리가 없어 저절로 꺼진다.
            ApplyRoomSides(_floorEnv, chapter);

            // 방 아래 남는 자리(조작 버튼이 떠 있는 곳)를 같은 무대의 「방 밖 바닥」으로 채운다
            // (`BattleDirector.RoomApron.cs`). 방이 창보다 길면 남는 자리가 없어 저절로 꺼진다.
            ApplyRoomApron(_floorEnv, chapter);

            // 방 위 바깥(카메라가 문 앞까지 따라 올라가면 드러난다)은 구름으로 가린다
            // (`BattleDirector.RoomCloud.cs`). 구름은 무대 공용 한 장.
            ApplyRoomCloud(_floorEnv, chapter);
        }

        // ── 레일 보스는 지금 못 넣는다 ────────────────────────────
        //
        // 「크러셔는 걷지 않고 위쪽 레일을 좌우로만 움직인다」를 한 번 넣어 봤다가 뺐다.
        // 높이만 못 박는 것으로는 **안 된다.** 재 보면 이렇다:
        //
        //   보스가 서는 자리   y −131 px (방 맨 위)
        //   플레이어          y −696 px
        //   사이              565 px = 7.8 m
        //   크러셔 패턴 반경   2.5 m(압착) · 3.5 m(파괴구)
        //   AttackRange       260 px = 3.6 m
        //
        // 닿을 수가 없다. 게다가 접근 코드가 매 프레임 아래로 걸으려 하고 레일이 매 프레임
        // 되돌리니, **걷는 동작만 제자리에서 돌았다** — 화면에서는 보스가 멈춰 서서
        // 혼자 이상한 짓을 하는 것으로 보인다. 실제로 그렇게 보고가 들어왔다.
        //
        // 레일을 제대로 넣으려면 패턴을 **크레인 아래 바닥**에 조준하도록 다시 짜야 한다
        // (지금은 전부 보스 몸을 중심으로 그린다). 그건 한 줄 제한이 아니라 설계 변경이라
        // 기획에 되돌렸다. 그때까지 크러셔는 다른 보스와 같이 걸어서 다가온다.


        /// <summary>
        /// 챕터 → 배경. 실제 배치와 테스트 배치가 **같은 표**를 쓴다.
        ///
        /// ⚠ 예전에는 48방을 「챕터 3개 × 앞뒤」로 갈라 여섯 구간을 만들고 구간 번호를
        ///   받았다. 방 표가 6챕터 60방으로 바뀌면서 **챕터 하나가 무대 하나**다.
        ///   짝은 주장이 아니라 방 표에서 뽑았다 — 챕터별 일반 방 8개의 `_floor` 집계:
        ///     CH1 junkyard · CH2 missile · CH3 street · CH4 rooftop · CH5 lab · CH6 refinery
        /// </summary>
        private static string ChapterFloorKey(int chapter, RoomEntry room) => chapter switch
        {
            // ⚠ 예전에는 CH1 만 배치 글자별로 여섯 장(`roomfloor_ch1_*`)을 따로 깔았다.
            //   그 여섯 장은 720×936 이라 16 m 방에서 세로로 늘어난다(2026-09-14) — 무대 한 장으로 통일한다.
            1 => "roomfloor_env_junkyard",
            2 => "roomfloor_env_missile",
            3 => "roomfloor_env_street",
            4 => "roomfloor_env_rooftop",
            5 => "roomfloor_env_lab",
            _ => "roomfloor_env_refinery",
        };

        private const string BossArenaFloor = "roomfloor_env_holding";

        /// <summary>
        /// 이 챕터가 몇 방인가 — **방 표를 센다.**
        ///
        /// ⚠ 예전에는 12·16·20 을 상수로 박아 뒀다. 3챕터 48방 시절 값이다.
        ///   방 표가 6챕터 60방(챕터마다 10방)으로 바뀐 뒤에도 상수는 그대로라
        ///   HUD 가 계속 `02 / 12` 를 찍었다 — 실제로는 10방인데 12방이라 우겼고,
        ///   진행 막대도 그만큼 덜 찼다. 세는 편이 낫다. 방을 더하거나 빼도 따라온다.
        /// </summary>
        private int ChapterRoomCount(int chapter)
        {
            if (_rooms == null) return _config.StagesPerChapter;
            int n = 0;
            var all = _rooms.Rooms;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Chapter == chapter) n++;
            return n > 0 ? n : _config.StagesPerChapter;
        }

        /// <summary>`ROOM_CH2_007` → 7. 못 읽으면 0 이라 앞 구간으로 떨어진다.</summary>
        private static int RoomNumberOf(string roomId)
            => roomId != null && roomId.Length >= 3
               && int.TryParse(roomId.Substring(roomId.Length - 3), out int n) ? n : 0;
        private string _floorKey;        // 지금 띄우려는 바닥
        private string _floorHeld;       // 실제로 메모리에 물고 있는 주소

        /// <summary>
        /// 지금 바닥을 읽는 중인 횟수. 0 이면 화면에 걸릴 것이 다 걸렸다는 뜻이다.
        ///
        /// ⚠ 바닥은 fire-and-forget 으로 읽는다. 그대로 두면 **가림막이 바닥보다 먼저 걷혀**
        ///   방이 반만 그려진 화면이 잠깐 보인다. 부팅에서 이 값이 0 이 될 때까지 기다린다.
        /// </summary>
        private int _floorPending;

        private async UniTaskVoid LoadRoomFloorAsync(string key, string fallback = null)
        {
            if (key == _floorKey) return;   // 같은 템플릿이 이어지면 다시 읽지 않는다
            _floorKey = key;

            if (string.IsNullOrEmpty(key)) { SetFloorSprite(null); return; }

            _floorPending++;
            try
            {
                var address = RoomFloorPrefix + key;
                var art = await LoadOptionalAsync<Sprite>(address);   // 아직 안 온 바닥이다

                if (art == null && !string.IsNullOrEmpty(fallback))
                {
                    address = RoomFloorPrefix + fallback;
                    art = await LoadOptionalAsync<Sprite>(address);   // 이것도 없으면 기본 바닥으로 떨어진다
                }

                // 기다리는 사이에 방이 또 바뀌었으면 이 결과는 버린다.
                // 방금 물어 온 것도 놓아 줘야 한다 — 안 그러면 빨리 넘길수록 쌓인다.
                if (_floorKey != key || _floorImage == null)
                {
                    // ⚠ **지금 화면이 쓰고 있는 주소면 놓지 않는다.** 방을 빠르게 넘기면
                    //   같은 주소를 두 번 읽는 일이 생기는데, 늦게 끝난 쪽이 그것을 놓아 버리면
                    //   참조가 0 이 되어 그림이 사라지고 다음 로드부터 계속 실패한다.
                    //   (방 48개를 연속으로 넘겨 보다가 실제로 그렇게 됐다.)
                    if (art != null && address != _floorHeld)
                        CoreModule.Get<IResourceManager>().Release(address);
                    return;
                }

                SetFloorSprite(art);

                // 새 바닥이 걸린 **뒤에** 지난 것을 놓는다. 먼저 놓으면 한 프레임 빈다.
                var old = _floorHeld;
                _floorHeld = art != null ? address : null;
                if (old != null && old != _floorHeld) CoreModule.Get<IResourceManager>().Release(old);
            }
            finally { _floorPending--; }   // 도중에 return 해도 반드시 내려간다
        }

        private void SetFloorSprite(Sprite art)
        {
            if (_floorImage == null) return;
            if (art != null)
            {
                // 전용 바닥은 방 한 칸을 통째로 그린 그림이라 늘려서 채운다.
                _floorImage.sprite = art;
                _floorImage.type = Image.Type.Simple;
                return;
            }
            // ⚠ 못 읽었을 때 기대는 곳은 **챕터 타일**이다.
            //   예전에는 720×900 자리표시자 한 장(`roomfloor.png`)을 물고 있었는데,
            //   그 한 장이 인게임 아틀라스를 2048 에서 못 벗어나게 붙들고 있었다.
            //   타일은 이미 아틀라스에 있으므로 따로 지고 갈 것이 없다.
            int ch = Mathf.Clamp(_runChapter, 1, 3);
            _floorImage.sprite = GetSprite($"floor_tile_ch{ch}") ?? _defaultFloor;
            _floorImage.type = Image.Type.Tiled;
        }

        /// <summary>챕터 바닥 타일을 반복해서 깐다. 방이 아무리 길어도 무늬가 안 늘어난다.</summary>
        private void SetFloorTile(Sprite tile)
        {
            if (_floorImage == null) return;
            _floorImage.sprite = tile;
            _floorImage.type = Image.Type.Tiled;
        }

        /// <summary>
        /// 정본 보스 ID → 바닥 그림 이름.
        ///
        /// ⚠ 챕터로 고르면 안 된다. 챕터마다 보스가 **둘**이라(중간·최종)
        ///   챕터 기준으로 찾으면 두 방이 같은 바닥을 쓰고, 그나마도
        ///   `BossTable` 에 없는 보스는 엉뚱한 이름으로 떨어진다.
        ///   방이 제 보스를 들고 있으므로 그것을 그대로 쓴다.
        /// </summary>
        /// <summary>
        /// 정본 보스 ID → 우리 키. 그림 이름과 바닥 이름이 같은 슬러그를 쓴다 —
        /// 둘이 어긋나면 어느 쪽이 진짜인지 코드를 읽어야만 알 수 있게 된다.
        /// </summary>
        private static string BossSlug(string bossId) => bossId switch
        {
            "B01" => "crusher",
            "B02" => "guardian",
            "B03" => "kingpin",
            "B04" => "python",
            "B05" => "robot_snakes",
            "B06" => "sludge",
            _ => "boss",
        };

        /// <summary>
        /// 그림이 아직 안 온 보스가 빌려 쓸 몸.
        ///
        /// 지금은 여섯 보스가 전부 제 그림을 갖고 있어 비어 있다.
        /// 빌릴 것이 생기면 여기 한 줄을 더한다 — 이 표가 곧
        /// "아직 그림이 없는 보스" 목록이라, 미리 올리는 쪽도 이것을 보고 판단한다.
        /// </summary>
        private static string BossStand(string slug) => slug;

        private void TickDeploy(float dt)
        {
            if (_deployCooldown > 0f) _deployCooldown -= dt;

            var me = Avatar;
            if (me == null || _host == null || _host.Key != DeployHostKey) return;
            if (_deployCooldown > 0f) return;

            if (AliveDeployables(ghostly: false) >= 2) return;   // 화면이 포탑으로 덮이면 무엇이 적인지 안 보인다

            _deployCooldown = DeployCooldown;
            SpawnDeployable(me.Position);
        }

        private int AliveDeployables(bool ghostly)
        {
            int n = 0;
            for (int i = 0; i < _deployables.Count; i++)
            {
                var d = _deployables[i];
                if (d != null && d.IsActive && d.Ghostly == ghostly) n++;
            }
            return n;
        }

        private void SpawnDeployable(Vector2 at, bool ghostly = false,
                                     float seconds = DeploySeconds, float range = DeployRange,
                                     int damage = -1, float fireInterval = -1f)
        {
            Deployable d = null;
            for (int i = 0; i < _deployables.Count; i++)
                if (!_deployables[i].IsActive) { d = _deployables[i]; break; }

            if (d == null)
            {
                if (_deployables.Count >= MaxDeployables) { d = _deployables[0]; d.Despawn(); }
                else
                {
                    var go = new GameObject("Deployable", typeof(RectTransform));
                    go.transform.SetParent(_unitLayer, false);
                    d = go.AddComponent<Deployable>();
                    d.Init(null, new Vector2(56f, 56f));
                    _deployables.Add(d);
                }
            }

            // 전용 그림이 아직 없다. 로봇을 줄여 쓴다 — 무엇이 놓았는지는 읽힌다.
            // 유령 포대는 고스트를 쓴다. 로봇 몸이 아닐 때도 나오므로 로봇 그림을 쓰면
            // "저 로봇은 어디서 났나" 가 된다.
            // ⚠ 포탑은 **포탑처럼 생겨야 한다.** 예전에는 놓은 사람(로봇) 그림을 줄여
            //   썼는데, 로봇이 놓으면 작은 로봇이 서서 "내가 둘로 늘었나" 로 읽혔다.
            //   삼각대 기관포 그림(`obj_turret`)이 이미 소품으로 들어와 있으므로 그것을 쓴다.
            //   유령 포대는 그대로 고스트다 — 로봇 몸이 아닐 때도 나오는 물건이라
            //   포탑 그림을 주면 "저 포탑은 어디서 났나" 가 된다.
            d.SetSprite(ghostly
                ? UnitGet("ghost", "s") ?? UnitGet("ghost")
                : GetSprite(TurretSpriteKey) ?? UnitGet(DeployHostKey, "s") ?? UnitGet(DeployHostKey));

            // 정본 BUF_T06 스마트 배치 — 재조준이 빨라진다(= 발사 간격이 준다)
            float interval = fireInterval > 0f
                ? fireInterval
                : DeployFireInterval * (1f - _buffs.DeployRetargetCut);
            int atk = damage >= 0
                ? damage
                : Mathf.RoundToInt(_host.Atk * _buffs.AttackMul * 0.6f);
            d.Spawn(at, seconds, range, atk, Mathf.Max(0.25f, interval), ghostly);
        }

        private void TickDeployables(float dt)
        {
            for (int i = 0; i < _deployables.Count; i++)
            {
                var d = _deployables[i];
                if (!d.IsActive || !d.Tick(dt)) continue;

                var target = NearestEnemy(d.Position, d.Range);
                if (target == null) continue;

                var shot = RentShot();
                if (shot == null) continue;
                // `shot_pulse` 한 장짜리 이름은 없다 — 원작 그림은 shot_pulse_1..4 다.
                shot.SetSprite(ShotFrames("pulse"), "pulse");
                shot.Fire(d.Position, target.Position, _config.ShotSpeedPlayer * _buffs.ShotSpeedMul, d.Damage,
                          fromPlayer: true, target, _config.ShotSize, ShotPlayerColor,
                          _config.ShotLifeSeconds,
                          // 정본 S08 도탄 터렛 — 포탑 탄도 튕긴다
                          bounces: 0);
                // 포탑도 로봇의 미사일이다 — 몸이 쏘는 것과 똑같이 터진다(기획 2026-09-15).
                shot.SetBlastRadius(Meters(RobotBlastMeters));

                // 정본 S01/S03 — 로봇 포탑이 불을 물려받는다. 쏜 자리에 불장판이 남는다.
                if (_buffs.DeployablesBurn)
                    SpawnField(target.Position, 90f * _buffs.AoeMul, 2f,
                               FieldEffect.Burn, 3, fromPlayer: true);
            }
        }

        /// <summary>
        /// 조준에 잡히는 몸인가.
        ///
        /// ⚠ **숨은 보스는 빠진다.** 벽 뒤·구멍 안에 있는 것을 계속 쏘면
        ///   탄이 허공으로 나가고, 오토어택이라 플레이어가 그것을 못 바꾼다.
        ///   조준을 세는 자리가 셋(`Nearest`·`NearestEnemy` 둘)이라 한 함수로 묶는다 —
        ///   따로 두면 한 곳만 고쳐진다.
        /// </summary>
        private bool Targetable(Unit e)
            => e != null && e.IsAlive && !e.IsDying && !e.IsHidden;

        private Unit NearestEnemy(Vector2 from, float range)
        {
            Unit best = null;
            float bestD = range;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e)) continue;
                // 가장자리까지 잰다 — 거리를 재는 자는 온 코드에서 하나여야 한다.
                // 스킬이 "반경 안 최근접" 을 물을 때 보스만 72px 멀리 있는 것으로 세면,
                // 코앞의 보스를 두고 뒤쪽 잡몹에게 사슬이 날아간다.
                float d = Vector2.Distance(from, e.Position) - BodyExcess(e);
                if (d > bestD) continue;
                bestD = d; best = e;
            }
            return best;
        }

        private void ClearDeployables()
        {
            for (int i = 0; i < _deployables.Count; i++) _deployables[i]?.Despawn();
        }

        // ── 장판 ──────────────────────────────────────────────────
        //
        // 정본에서 이것 하나가 여러 갈래를 막고 있었다 — 버프 4종과 보스 패턴 둘이
        // 전부 "바닥에 깔린 지속 영역" 을 전제한다.

        private const int MaxFields = 24;
        private const int FieldSlowPercent = 45;
        private readonly List<Field> _fields = new();

        private static readonly Color FieldColorDamage = new(1f, 0.45f, 0.20f, 0.32f);
        private static readonly Color FieldColorSlow   = new(0.45f, 0.70f, 1f, 0.28f);
        private static readonly Color FieldColorBurn   = new(1f, 0.55f, 0.18f, 0.34f);
        private static readonly Color FieldColorFreeze = new(0.60f, 0.90f, 1f, 0.32f);
        private static readonly Color FieldColorCurse  = new(0.66f, 0.35f, 1f, 0.32f);

        /// <summary>
        /// 효과별 장판 그림 이름. 전용 그림이 없으면 흰 원판(`field`)에 색만 입혀 쓴다 —
        /// 그림이 오는 대로 저절로 갈아 끼워진다.
        /// </summary>
        private static string FieldSpriteOf(FieldEffect e) => e switch
        {
            FieldEffect.Slow   => "field_slow",
            FieldEffect.Burn   => "field_burn",
            FieldEffect.Freeze => "field_freeze",
            FieldEffect.Curse  => "field_curse",
            _                  => "field_damage",
        };

        private static Color ColorOf(FieldEffect e) => e switch
        {
            FieldEffect.Slow   => FieldColorSlow,
            FieldEffect.Burn   => FieldColorBurn,
            FieldEffect.Freeze => FieldColorFreeze,
            FieldEffect.Curse  => FieldColorCurse,
            _                  => FieldColorDamage,
        };

        /// <summary>장판을 깐다. 자리가 없으면 가장 오래된 것을 밀어낸다.</summary>
        /// <param name="artKey">
        /// 효과가 정한 그림 대신 쓸 이름. 같은 효과라도 **누가 깔았느냐에 따라**
        /// 다른 것이 깔려야 읽히는 경우가 있다 — 파이썬 독 웅덩이가 저주 장판과
        /// 같은 보라 원판이면 무엇에 서 있는지 알 수 없다.
        /// </param>
        private Field SpawnField(Vector2 at, float radius, float seconds,
                                 FieldEffect effect, int damagePerTick, bool fromPlayer,
                                 string artKey = null)
        {
            Field f = null;
            for (int i = 0; i < _fields.Count; i++)
                if (!_fields[i].IsActive) { f = _fields[i]; break; }

            if (f == null)
            {
                if (_fields.Count >= MaxFields) { f = _fields[0]; f.Despawn(); }
                else
                {
                    var go = new GameObject("Field", typeof(RectTransform));
                    go.transform.SetParent(_fieldLayer, false);
                    f = go.AddComponent<Field>();
                    f.Init(null);
                    _fields.Add(f);
                }
            }

            // ⚠ 그림은 **깔 때마다** 정한다. 장판도 풀에서 돌려 쓰므로 태어날 때 정하면
            //    직전 효과의 그림이 그대로 남는다(탄·포탑에서 이미 두 번 겪었다).
            // 이름이 컷 묶음(`fx_{artKey}_1~`)이면 그것부터 쓴다 — 드라군 불바다가 `lava` 로
            // 부르는데 한 장짜리만 찾아서 **정지 그림**이 깔렸다(기획 2026-09-15).
            var keyFrames = artKey != null ? FxFrames(artKey) : null;
            var art = keyFrames != null ? keyFrames[0] : GetSprite(artKey ?? FieldSpriteOf(effect));
            // 전용 그림이 없으면 흰 원판에 색을 입힌다. 색까지 없으면 그리지 않는다 —
            // 흰 네모가 바닥에 깔리는 것보다 아무것도 없는 편이 낫다.
            bool generic = art == null;
            // 여러 장이 있으면 **돌린다.** `fx_lava_1~4` 처럼 컷이 갈린 장판은
            // 한 장만 깔면 타는 것이 아니라 붙여 놓은 그림이 된다.
            var frames = generic ? null : keyFrames ?? FxFrames(artKey ?? FieldSpriteOf(effect));
            if (frames != null && frames.Length > 1) f.SetFrames(frames);
            else f.SetSprite(art ?? GetSprite("field"));

            // 정본 BUF_A02 — 장판이 더 오래 남는다
            f.Spawn(at, radius, seconds + _buffs.FieldExtraSeconds,
                    effect, damagePerTick, fromPlayer,
                    generic ? ColorOf(effect) : Color.white);
            return f;
        }

        private void TickFields(float dt)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                var f = _fields[i];
                if (!f.IsActive || !f.Tick(dt)) continue;

                if (f.FromPlayer)
                {
                    for (int j = 0; j < _enemies.Count; j++)
                    {
                        var e = _enemies[j];
                        if (e == null || !e.IsAlive || !f.Contains(e.Position)) continue;
                        ApplyField(f, e, toEnemy: true);
                    }
                }
                else
                {
                    var me = Avatar;
                    if (me != null && me.IsAlive && f.Contains(me.Position))
                        ApplyField(f, me, toEnemy: false);
                }
            }
        }

        private void ApplyField(Field f, Unit u, bool toEnemy)
        {
            switch (f.Effect)
            {
                // ⚠ 걸어 두는 시간은 장판 한 박자(1.5초 — `Field.TickInterval`)보다 길어야 한다.
                //   짧으면 박자 사이에 풀렸다 걸렸다 깜빡인다(기획 2026-09-15 박자를 0.5 → 1.5초로 늘리며 같이 늘렸다).
                case FieldEffect.Slow:
                    u.ApplySlow(FieldSlowPercent, 1.8f);
                    // 정본 BUF_T03 — 둔화 장판 가장자리가 피해를 준다.
                    // 가장자리로 한정하는 이유는 "안에 있으면 아프다" 가 아니라
                    // "들어오고 나갈 때 아프다" 라야 자리를 잡을 이유가 생기기 때문이다.
                    if (_buffs.SlowFieldEdgeDamage > 0 && toEnemy)
                    {
                        float d = (u.Position - f.Center).magnitude;
                        if (d > f.Radius * 0.7f) HurtByField(u, _buffs.SlowFieldEdgeDamage, toEnemy);
                    }
                    break;
                case FieldEffect.Burn:
                    u.ApplyBurn(1.8f);
                    if (toEnemy) ChainStatus(u, StatusKind.Burn, 1.8f);
                    break;
                case FieldEffect.Freeze:
                    u.ApplyFreeze(1.8f);
                    if (toEnemy) ChainStatus(u, StatusKind.Freeze, 1.8f);
                    break;
                case FieldEffect.Curse:
                    u.ApplyCurse(1.8f);
                    if (toEnemy) ChainStatus(u, StatusKind.Curse, 1.8f);
                    break;
            }
            if (f.DamagePerTick > 0) HurtByField(u, f.DamagePerTick, toEnemy);
        }

        private void HurtByField(Unit u, int damage, bool toEnemy)
        {
            if (damage <= 0) return;
            if (toEnemy)
            {
                ShowDamage(u.Position, damage, toEnemy: true);
                if (u.TakeDamage(damage)) { KillEnemy(u); return; }
                if (u.IsBoss) _bus.Publish(new BossHpChangedEvent { BossHp = u.Hp, BossHpMax = u.HpMax });
            }
            else
            {
                DamagePlayer(damage);   // 감소·유령 전환까지 이쪽이 다 처리한다
            }
        }

        private void ClearFields()
        {
            for (int i = 0; i < _fields.Count; i++) _fields[i]?.Despawn();
        }

        // ── 정본 BUF_T02 화상 전이 ────────────────────────────────
        private const float BurnSpreadRadius = 150f;
        private const float BurnSpreadInterval = 1.0f;
        private float _burnSpreadTimer;

        private void SpreadBurn(Unit source)
        {
            if (_burnSpreadTimer > 0f) return;
            _burnSpreadTimer = BurnSpreadInterval;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var o = _enemies[i];
                if (o == null || o == source || !o.IsAlive) continue;
                if ((o.Position - source.Position).sqrMagnitude > BurnSpreadRadius * BurnSpreadRadius) continue;
                o.ApplyBurn(_config.SlowSeconds);
            }
        }

        // ── 정본 BUF_U04 집중한 영혼 ──────────────────────────────
        //
        // "같은 표적 4회 명중마다 피해 +6%, 3단계까지". 표적을 바꾸면 처음부터다.
        // 누구를 몇 번 때렸는지만 들고 있으면 되므로 유닛 참조 하나와 정수 하나로 족하다 —
        // 적마다 카운터를 달면 죽을 때마다 정리해야 한다.
        private const int FocusHitsPerStack = 4;
        private Unit _focusTarget;
        private int _focusHits;

        /// <summary>
        /// 정본 v2.3 카드가 붙이는 피해 보정. 곱으로 쌓는다.
        ///   C002 정밀 조준 — 살아 있는 적이 **하나뿐일 때만**
        ///   C003 마무리 본능 — 상대 체력이 낮을수록 (30% 이하에서 최대)
        ///   C015 보스 압축 — 보스에게
        /// </summary>
        private float CardDamageMul(Unit victim)
        {
            float m = GritDamageMul;   // 궁지 — 내 체력이 절반 아래면

            if (_buffs.SingleTargetBonus > 0f)
            {
                int alive = 0;
                for (int i = 0; i < _enemies.Count && alive < 2; i++)
                {
                    var e = _enemies[i];
                    if (e != null && e.IsAlive && !e.IsDying) alive++;
                }
                if (alive == 1) m *= 1f + _buffs.SingleTargetBonus;
            }

            if (_buffs.ExecuteBonus > 0f && victim.HpMax > 0)
            {
                // 체력 30% 이하에서 계수가 다 실린다. 그 위로는 비례해서 줄어든다.
                float hp = (float)victim.Hp / victim.HpMax;
                float t = Mathf.Clamp01((0.7f - hp) / 0.4f);
                m *= 1f + _buffs.ExecuteBonus * t;
            }

            if (_buffs.BossBonus > 0f && victim.IsBoss) m *= 1f + _buffs.BossBonus;

            if (_buffs.SustainBonus > 0f)
            {
                // 같은 대상 1~4타 = 25/50/75/100%. 대상이 바뀌거나 1.5초가 비면 초기화된다.
                if (_sustainTarget != victim || Time.time - _sustainAt > SustainGapSeconds)
                { _sustainTarget = victim; _sustainHits = 0; }
                _sustainAt = Time.time;
                _sustainHits = Mathf.Min(_sustainHits + 1, SustainMaxHits);
                m *= 1f + _buffs.SustainBonus * _sustainHits / SustainMaxHits;
            }

            return m;
        }

        // ── C005 연속 압박 ───────────────────────────────────────
        private const int SustainMaxHits = 4;
        private const float SustainGapSeconds = 1.5f;
        private Unit _sustainTarget;
        private int _sustainHits;
        private float _sustainAt;

        // ── C032 영혼 복제 ───────────────────────────────────────
        //
        // 유효 **기본 공격** 8회마다 직전 공격을 한 번 더 낸다.
        // 복제된 공격은 카운터를 올리지 않는다 — 안 막으면 8타마다 무한히 늘어난다
        // (정본 PROC 규칙: NoRecursiveProc).
        private const int EchoEveryHits = 8;
        private int _echoHits;
        private bool _echoing;

        /// <summary>기본 공격 한 번을 셌다. 8회째면 true — 부르는 쪽이 한 번 더 낸다.</summary>
        private bool CountEcho()
        {
            if (_buffs.EchoPercent <= 0f || _echoing) return false;
            if (++_echoHits < EchoEveryHits) return false;
            _echoHits = 0;
            return true;
        }

        // ── C030 유령 포대 ───────────────────────────────────────
        //
        // 유효 기본 공격 8회마다 3초짜리 포대 1기가 선다. 로봇의 설치물과 같은
        // 구조를 쓰지만 **로봇 몸이 아닐 때도** 나와야 하므로 수를 따로 센다.
        // C032 와 주기가 같지만 카운터는 나눠 둔다 — 하나만 든 판에서
        // 다른 카드의 진행도를 빌려 쓰게 되면 8타가 8타가 아니게 된다.
        private const int TurretEveryHits = 8;
        private const float TurretSeconds = 3f;
        private const float TurretInterval = 0.75f;
        private const float TurretRangeMeters = 11f;
        private int _turretHits;

        private void TryGhostTurret(Unit attacker)
        {
            if (_buffs.GhostTurretPercent <= 0f || attacker == null) return;
            if (++_turretHits < TurretEveryHits) return;
            _turretHits = 0;
            if (AliveDeployables(ghostly: true) >= 1) return;   // 정본 max=1

            int atk = Mathf.Max(1, Mathf.RoundToInt(
                attacker.Atk * _buffs.AttackMul * _buffs.GhostTurretPercent));
            SpawnDeployable(attacker.Position, ghostly: true,
                            seconds: TurretSeconds, range: TurretRangeMeters * _pxPerMeter,
                            damage: atk, fireInterval: TurretInterval);
        }

        // ── C013 폭발 메아리 ─────────────────────────────────────
        //
        // 터진 자리에서 잠깐 뒤 한 번 더 터진다. 같은 프레임에 두 번 때리면
        // 그냥 "피해가 는 것"이라 카드가 눈에 안 보인다 — 늦게 와야 메아리로 읽힌다.
        // 2차 충격은 다시 메아리치지 않는다(정본 PROC 규칙: NoRecursiveProc).
        private const float EchoBlastDelay = 0.3f;
        private const float EchoBlastRadiusRatio = 0.72f;

        private struct EchoBlast
        {
            public Vector2 At;
            public float Radius;
            public int Damage;
            public float Delay;
            public string Kind;
        }

        private readonly List<EchoBlast> _echoBlasts = new();

        private void QueueEchoBlast(Vector2 at, float radius, int damage, string kind)
        {
            if (_buffs.ExplosiveEchoPercent <= 0f) return;
            int dmg = Mathf.RoundToInt(damage * _buffs.ExplosiveEchoPercent);
            if (dmg <= 0) return;
            _echoBlasts.Add(new EchoBlast
            {
                At = at,
                Radius = radius * EchoBlastRadiusRatio,
                Damage = dmg,
                Delay = EchoBlastDelay,
                Kind = kind,
            });
        }

        private void TickEchoBlasts(float dt)
        {
            for (int i = _echoBlasts.Count - 1; i >= 0; i--)
            {
                var b = _echoBlasts[i];
                b.Delay -= dt;
                if (b.Delay > 0f) { _echoBlasts[i] = b; continue; }

                _echoBlasts.RemoveAt(i);
                SpawnImpact(b.At, b.Kind, b.Radius * 2f);
                for (int k = 0; k < _enemies.Count; k++)
                {
                    var e = _enemies[k];
                    if (e == null || !e.IsAlive) continue;
                    if (Vector2.Distance(b.At, e.Position) > b.Radius) continue;
                    e.IsAggro = true;
                    HurtByField(e, b.Damage, toEnemy: true);
                }
            }
        }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // ── 상점 방 ──────────────────────────────────────────────
        //
        // 이벤트가 "운을 걸겠는가" 라면 상점은 "모아 둔 것을 지금 쓰겠는가" 다.
        // 값이 정확히 붙어 있고 살 수 있는 수가 정해져 있다(정본: 판당 2회, 카드 1장).
        // 무제한이면 골드를 아낄 이유가 사라지고 방마다 들르는 정산소가 된다.

        private readonly List<ShopOffer> _shopOffers = new();
        private ShopChapter _shopRules;
        private int _shopBought, _shopCardsBought;
        /// <summary>이 상점에서 치료를 이미 샀는가. 한 번만 판다.</summary>
        private bool _shopHealBought;
        private bool _shopOpen;

        public bool IsShopOpen => _shopOpen;

        private void OpenShop()
        {
            _shopOpen = false;
            _shopOffers.Clear();
            if (_shopTable == null || _buffTable == null) { SpawnExit(); return; }

            int ch = Mathf.Clamp(_runChapter, 1, 3);
            _shopRules = _shopTable.ForChapter(ch);
            if (_shopRules == null) { SpawnExit(); return; }

            _shopBought = _shopCardsBought = 0;
            _shopHealBought = false;

            // 이 판에서 만난 몸 중 지금 안 타고 있는 것 하나를 진열한다.
            _hostOffers.Clear();
            if (_player != null)
                foreach (var key in _metHostKeys)
                {
                    if (_host != null && _host.Key == key) continue;
                    var he = _player.GetHost(key);
                    if (he != null && !he.IsGhost) { _hostOffers.Add(he); break; }
                }
            RollShopConsumable();

            // 못 올리는 카드(5레벨을 다 찍은 것)는 진열하지 않는다 —
            // 살 수는 있는데 아무 일도 안 일어나면 값이 거짓이 된다.
            _shopFilter.Clear();
            foreach (var k in _buffs.ExcludedKeys) _shopFilter.Add(k);

            // ⚠ **표에 없는 카드도 뺀다.** 카드 목록을 갈면 상점 표가 옛 카드를
            //   가리킨 채 남는다 — 실제로 32종을 10종으로 바꾼 뒤 상점 칸에
            //   이름 대신 `C015` 가 떴다. 표를 다시 굽는 것이 정답이지만,
            //   여기서도 막아 두면 표가 낡아도 없는 물건을 팔지는 않는다.
            for (int i = 0; i < _shopTable.Offers.Count; i++)
            {
                var key = _shopTable.Offers[i].BuffKey;
                if (_buffTable == null || _buffTable.Get(key) == null) _shopFilter.Add(key);
            }

            _shopTable.Draw(_shopOffers, ch, Mathf.Max(1, _shopRules.OfferCount), _shopFilter, _rng);
            _shopOpen = true;
            PublishShop();
        }

        private readonly HashSet<string> _shopFilter = new();

        /// <summary>
        /// 특성 한 장이 쓸 아이콘 이름.
        ///
        /// 정본 CardID 를 그대로 파일 이름으로 쓴다 — `C001` → `buffcard_c001`.
        /// 처음에는 갈래 7장으로 굴렸는데 상점은 한 번에 네 장이라 같은 그림이
        /// 겹쳐 떴다(2026-09-08). 32종에 32장을 붙인다.
        /// </summary>
        private static string BuffIconOf(BuffEntry card)
        {
            if (card == null) return string.Empty;
            var id = card.CardId;
            return string.IsNullOrEmpty(id) ? string.Empty : "buffcard_" + id.ToLowerInvariant();
        }

        /// <summary>
        /// 진열 순서. 이 순서가 곧 `BuyShopItem` 의 번호다 — 두 곳이 어긋나면
        /// 「A 를 눌렀는데 B 가 팔린다」가 된다. 한 곳에서 세고 한 곳에서 판다.
        ///
        ///   0 ~ n-1  카드
        ///   n        몸 (`_hostOffers` 가 비어 있으면 이 칸이 없다)
        ///   n+1      소모품
        ///   마지막   회복
        /// </summary>
        private int ShopHostIndex => _hostOffers.Count > 0 ? _shopOffers.Count : -1;
        private int ShopConsumableIndex
            => _shopConsumable == Consumable.None ? -1
             : _shopOffers.Count + (_hostOffers.Count > 0 ? 1 : 0);
        private int ShopHealIndex
            => _shopOffers.Count + (_hostOffers.Count > 0 ? 1 : 0)
             + (_shopConsumable == Consumable.None ? 0 : 1);

        private void PublishShop()
        {
            int n = ShopHealIndex + 1;
            var names = new string[n];
            var descs = new string[n];
            var prices = new int[n];
            var can = new bool[n];
            var icons = new string[n];

            for (int i = 0; i < _shopOffers.Count; i++)
            {
                var o = _shopOffers[i];
                var card = _buffTable.Get(o.BuffKey);
                int lv = _buffs.LevelOf(o.BuffKey);
                names[i] = card != null
                    ? (lv > 0 ? $"{card.DisplayName}  Lv.{lv}→{Mathf.Min(lv + 1, card.MaxLevel)}" : card.DisplayName)
                    : o.CardId;
                descs[i] = card != null ? card.DisplayDescription : string.Empty;
                prices[i] = PriceOf(o);
                can[i] = CanBuyCard(o);
                icons[i] = BuffIconOf(card);
            }

            int hostAt = ShopHostIndex;
            if (hostAt >= 0)
            {
                var he = _hostOffers[0];
                int price = HostPriceOf();
                names[hostAt] = Localize.Format("ui.shop.host.name", he.DisplayName);
                descs[hostAt] = Localize.Get("ui.shop.host.desc");
                prices[hostAt] = price;
                can[hostAt] = _shopBought < _shopRules.TotalPurchaseLimit && _runGold >= price;
                // ⚠ UI 아틀라스에는 몸의 초상이 없다(`hostportraitimage_*` 는 로비 쪽 그림이다).
                //   `unit:` 을 붙여 **유닛 아틀라스에서 꺼내라**고 알린다 — HUD 초상과 같은 그림이다.
                icons[hostAt] = "unit:" + he.HostKey;
            }

            int conAt = ShopConsumableIndex;
            if (conAt >= 0)
            {
                int price = ConsumablePrice();
                names[conAt] = ConsumableNameOf(_shopConsumable);
                descs[conAt] = ConsumableDescOf(_shopConsumable);
                prices[conAt] = price;
                // 이미 하나 사 두었으면 또 못 산다. 두 개를 들고 다니면
                // 다음 방에서 하나는 조용히 사라진다.
                can[conAt] = _shopBought < _shopRules.TotalPurchaseLimit
                          && _runGold >= price && _pendingConsumable == Consumable.None;
                icons[conAt] = ConsumableIconOf(_shopConsumable);
            }

            int heal = ShopHealIndex;
            names[heal] = Localize.Get("ui.shop.heal.name");
            descs[heal] = Localize.Format("ui.shop.heal.desc", _shopRules.HostHealPct, _shopRules.GhostHealPct);
            prices[heal] = _shopRules.HealPrice;
            // ⚠ 치료는 **한 번만** 판다. 카드·몸은 산 뒤 진열대에서 빠지고
            //   소모품은 하나만 들 수 있는데, 치료만 한도가 남으면 두 번 살 수 있었다.
            can[heal] = !_shopHealBought
                     && _shopBought < _shopRules.TotalPurchaseLimit
                     && _runGold >= _shopRules.HealPrice;
            icons[heal] = "buffcard_heal";   // 회복은 카드가 아니라 상점 고유 칸이다

            _bus.Publish(new ShopOpenedEvent
            {
                Names = names, Descs = descs, Prices = prices, CanBuy = can, Icons = icons,
                Gold = _runGold,
                LimitLine = Localize.Format("ui.shop.limit", _shopRules.TotalPurchaseLimit - _shopBought,
                                          _shopRules.CardPurchaseLimit - _shopCardsBought),
            });
        }

        /// <summary>암시장 연줄(EV_CH2_05)이 붙어 있으면 그만큼 싸다. 최소 1 골드는 받는다.</summary>
        private int PriceOf(ShopOffer o)
            => _shopDiscount <= 0 ? o.Price
             : Mathf.Max(1, Mathf.RoundToInt(o.Price * (100 - _shopDiscount) / 100f));

        private bool CanBuyCard(ShopOffer o)
            => _shopBought < _shopRules.TotalPurchaseLimit
            && _shopCardsBought < _shopRules.CardPurchaseLimit
            && _runGold >= PriceOf(o);

        /// <summary>진열대에서 하나를 산다. UI 가 호출한다.</summary>
        public void BuyShopItem(int index)
        {
            if (!_shopOpen || _shopRules == null) return;
            if (index < 0 || index > ShopHealIndex) return;

            string line;
            if (index == ShopHostIndex)
            {
                var he = _hostOffers[0];
                int price = HostPriceOf();
                if (_shopBought >= _shopRules.TotalPurchaseLimit) return;
                if (_runGold < price) return;
                AddRunGold(-price);
                _shopBought++;
                _hostOffers.RemoveAt(0);
                // 그 자리에서 몸을 갈아탄다. 상점 방에는 적이 없으므로
                // 「뺏을 몸을 세워 두고 빙의」가 아니라 바로 입는다.
                var at = Avatar != null ? Avatar.Position : Vector2.zero;
                if (_host != null) LeaveHost();
                EnterHost(he, he.HostKey, he.DisplayName, at, 100);
                line = Localize.Format("ui.shop.result.host", he.DisplayName);
            }
            else if (index == ShopConsumableIndex)
            {
                int price = ConsumablePrice();
                if (_shopBought >= _shopRules.TotalPurchaseLimit) return;
                if (_runGold < price) return;
                if (_pendingConsumable != Consumable.None) return;
                AddRunGold(-price);
                _shopBought++;
                _pendingConsumable = _shopConsumable;
                line = Localize.Format("ui.shop.result.consumable", ConsumableNameOf(_shopConsumable));
            }
            else if (index == ShopHealIndex)
            {
                if (_shopHealBought) return;
                if (_shopBought >= _shopRules.TotalPurchaseLimit) return;
                if (_runGold < _shopRules.HealPrice) return;
                AddRunGold(-_shopRules.HealPrice);
                _shopBought++;
                _shopHealBought = true;

                _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + GhostHpMax * _shopRules.GhostHealPct / 100);
                if (_host != null) _host.Heal(Mathf.Max(1, _host.HpMax * _shopRules.HostHealPct / 100));
                PublishHp();
                line = Localize.Format("ui.shop.heal.desc", _shopRules.HostHealPct, _shopRules.GhostHealPct);
            }
            else
            {
                var o = _shopOffers[index];
                if (!CanBuyCard(o)) return;
                var card = _buffTable.Get(o.BuffKey);
                if (card == null) return;

                AddRunGold(-PriceOf(o));
                _shopBought++;
                _shopCardsBought++;
                _buffs.Apply(card);
                _bus.Publish(new BuffChosenEvent { ChosenKey = card.BuffKey, TotalBuffCount = _buffs.Count });

                // 산 물건은 진열대에서 뺀다. 남겨 두면 다 산 뒤에도 값이 붙어 있어
                // 아직 살 수 있는 것처럼 보인다.
                _shopOffers.RemoveAt(index);
                line = Localize.Format("ui.shop.result.card", card.DisplayName);
            }

            // ⚠ **하나 사면 창이 닫힌다** (2026-09-09).
            //   한 상점에서 두 번까지 살 수 있지만, 창을 열어 둔 채로 두면
            //   "더 살 수 있나" 를 매번 눈으로 훑게 된다. 사는 순간 방으로 돌아가고,
            //   더 사고 싶으면 가판에 다시 다가서면 된다 — 무엇을 샀는지도 그때 보인다.
            // ⚠ **`PublishShop()` 을 부르지 않는다.** 저 발행이 진열을 다시 그리면서
            //   방금 접은 창을 되살린다 — 실제로 그래서 안 닫혔다.
            //   어차피 닫는 창이라 다시 그릴 것도 없다.
            _bus.Publish(new ShopPurchasedEvent { Index = index, ResultLine = line });
            CloseShop();
        }

        /// <summary>상점을 닫고 나간다. UI 가 호출한다.</summary>
        public void CloseShop()
        {
            if (!_shopOpen) return;
            _shopOpen = false;
            _shopOffers.Clear();
            SpawnExit();
        }

        // ── C031 과충전 회로 ─────────────────────────────────────
        //
        // 전기의 정체성은 "튄다" 다. 맞은 대상에게 한 번 더 넣는 것만으로는
        // 그냥 공격력 증가라 카드가 눈에 안 보인다 — 옆 사람까지 감전돼야
        // 화면에서 무슨 일이 났는지 읽힌다.
        //
        // "제한된"(정본) 은 재사용 대기로 푼다. 매 타격마다 터지면 연사 호스트가
        // 화면을 전기로 덮는다.
        private const float OverchargeCooldown = 0.55f;
        private const float OverchargeArcRange = 220f;
        private float _overchargeTimer;
        private readonly System.Collections.Generic.List<Unit> _chainHit = new();

        private void Overcharge(Unit victim, int hitDamage)
        {
            if (_buffs.OverchargePercent <= 0f || _overchargeTimer > 0f) return;
            if (victim == null) return;

            int spark = Mathf.Max(1, Mathf.RoundToInt(hitDamage * _buffs.OverchargePercent));
            _overchargeTimer = OverchargeCooldown;

            var me = Avatar;
            if (me != null) DrawBolt(me.Position, victim.Position);

            if (victim.IsAlive)
            {
                ShowDamage(victim.Position, spark, toEnemy: true);
                SpawnImpact(victim.Position, "pulse");
                if (victim.TakeDamage(spark)) { KillEnemy(victim); }
                else if (victim.IsBoss)
                    _bus.Publish(new BossHpChangedEvent { BossHp = victim.Hp, BossHpMax = victim.HpMax });
            }

            // 옆으로 튄다. 기본 1회, 「번개 사슬」이 있으면 그만큼 더 튄다.
            // ⚠ 튄 곳은 다시 안 친다. 안 막으면 둘 사이를 오가며 무한히 튄다.
            _chainHit.Clear();
            _chainHit.Add(victim);
            var from = victim;

            for (int hop = 0; hop <= _buffs.ExtraChains; hop++)
            {
                Unit arc = null;
                float best = OverchargeArcRange;
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsAlive || e.IsDying) continue;
                    if (_chainHit.Contains(e)) continue;
                    float d = Vector2.Distance(from.Position, e.Position);
                    if (d > best) continue;
                    best = d; arc = e;
                }
                if (arc == null) return;

                _chainHit.Add(arc);
                arc.IsAggro = true;
                DrawBolt(from.Position, arc.Position);   // 어디로 튀었는지 줄기로 보여 준다
                SpawnImpact(arc.Position, "pulse");
                HurtByField(arc, spark, toEnemy: true);
                from = arc;
            }
        }

        // ── C014 연쇄 번짐 ───────────────────────────────────────
        //
        // 상태이상을 **건 그 순간** 옆으로 옮긴다. 이미 있던 화상 전이(BUF_T02)는
        // 화상만, 그것도 계속 도는 방식이었다. 이쪽은 세 가지 모두를 한 번만 옮긴다 —
        // 계속 돌면 방 하나가 통째로 얼어붙는다.
        private const float StatusChainRadius = 170f;

        private enum StatusKind { Burn, Freeze, Curse }

        private void ChainStatus(Unit from, StatusKind kind, float seconds)
        {
            if (_buffs.StatusChainPercent <= 0 || from == null) return;
            if (_rng.Next(100) >= _buffs.StatusChainPercent) return;

            Unit near = null;
            float best = StatusChainRadius;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || e == from || !e.IsAlive || e.IsDying) continue;
                float d = Vector2.Distance(from.Position, e.Position);
                if (d > best) continue;
                best = d; near = e;
            }
            if (near == null) return;

            switch (kind)
            {
                case StatusKind.Burn:   near.ApplyBurn(seconds); break;
                case StatusKind.Freeze: near.ApplyFreeze(seconds); break;
                case StatusKind.Curse:  near.ApplyCurse(seconds); break;
            }
        }

        // ── C018 위기 방벽 ───────────────────────────────────────
        //
        // 체력이 위험해지는 **그 순간** 한 번 선다. 방마다 한 번뿐이라
        // 아껴 쓸 수도, 믿고 밀어붙일 수도 있다.
        private const int CrisisHpPercent = 30;
        private int _barrier;
        private bool _barrierUsedThisRoom;

        public int Barrier => _barrier;

        /// <summary>위기에 들어섰는지 본다. 들어섰으면 방벽을 세운다.</summary>
        private void CheckCrisisBarrier()
        {
            if (_buffs.CrisisBarrierPercent <= 0f || _barrierUsedThisRoom) return;

            int hp, hpMax;
            if (_host != null) { hp = _host.Hp; hpMax = _host.HpMax; }
            else { hp = _ghostHp; hpMax = GhostHpMax; }
            if (hpMax <= 0 || hp * 100 > hpMax * CrisisHpPercent) return;

            _barrierUsedThisRoom = true;
            _barrier = Mathf.Max(1, Mathf.RoundToInt(hpMax * _buffs.CrisisBarrierPercent));

            // 방벽이 섰다는 것이 화면에 남아야 한다. 체력바만 보고 있으면
            // 다음 한 방을 왜 안 맞았는지 알 수 없다.
            var t = RentDamageText();
            if (t != null && Avatar != null) t.Show(Avatar.Position, Localize.Format("ui.battle.barrier", _barrier), HealColor);
        }

        /// <summary>방벽이 먼저 받아 낸다. 남은 피해만 돌려준다.</summary>
        private int AbsorbWithBarrier(int amount)
        {
            if (_barrier <= 0 || amount <= 0) return amount;
            int taken = Mathf.Min(_barrier, amount);
            _barrier -= taken;
            return amount - taken;
        }

        // ── C023 회피 잔상 ───────────────────────────────────────
        //
        // 이 게임에 회피 동작이 없다. 그래서 "위험 회피"(정본)의 실체를
        // **스쳐 지나간 탄**으로 정한다 — 몸에 닿을 뻔했는데 맞지 않고 지나가
        // 수명을 다한 탄. 실제로 아슬아슬하게 움직였을 때만 일어난다.
        private const float GrazeRadiusMul = 2.6f;   // 명중 반경의 몇 배까지를 "스쳤다" 로 볼지
        private const float AfterimageCooldown = 1.1f;
        private float _afterimageTimer;

        private void FireAfterimage()
        {
            if (_buffs.AfterimagePercent <= 0f || _afterimageTimer > 0f) return;
            var me = Avatar;
            if (me == null) return;

            var target = NearestEnemy(me.Position, _config.EnemyDetectRange * 1.5f);
            if (target == null) return;

            var shot = RentShot();
            if (shot == null) return;
            _afterimageTimer = AfterimageCooldown;

            int dmg = Mathf.Max(1, Mathf.RoundToInt(me.Atk * _buffs.AttackMul * _buffs.AfterimagePercent));
            shot.SetSprite(ShotFrames("pulse"), "pulse");
            shot.Fire(me.Position, target.Position, _config.ShotSpeedPlayer * _buffs.ShotSpeedMul, dmg,
                      fromPlayer: true, target, _config.ShotSize, ShotPlayerColor,
                      _config.ShotLifeSeconds, bounces: 0);
        }

        /// <summary>C025·C026·C027 각인 — 기본 공격에 상태이상을 얹는다.</summary>
        private void ApplyImprints(Unit victim)
        {
            if (victim == null || !victim.IsAlive) return;
            float sec = _config.SlowSeconds;
            if (_buffs.FlameImprint > 0 && _rng.Next(100) < _buffs.FlameImprint)
            { victim.ApplyBurn(sec); ChainStatus(victim, StatusKind.Burn, sec); }
            if (_buffs.FrostImprint > 0 && _rng.Next(100) < _buffs.FrostImprint)
            { victim.ApplyFreeze(sec); ChainStatus(victim, StatusKind.Freeze, sec); }
            if (_buffs.CurseImprint > 0 && _rng.Next(100) < _buffs.CurseImprint)
            { victim.ApplyCurse(sec); ChainStatus(victim, StatusKind.Curse, sec); }
        }

        // ── C024 전투 스텝 ───────────────────────────────────────
        //
        // 기본 공격을 끝낸 뒤 1.2초 동안 이동이 빨라진다. 지속시간만 갱신되고
        // 배율은 겹치지 않는다 — 겹치면 연사 호스트가 계속 달린다.
        private const float CombatStepSeconds = 1.2f;
        private float _combatStep;

        private float CombatStepMul => _combatStep > 0f ? 1f + _buffs.CombatStepBonus : 1f;

        private float FocusMul(Unit victim)
        {
            if (_buffs.FocusPerStack <= 0f) return 1f;
            if (_focusTarget != victim) { _focusTarget = victim; _focusHits = 0; }
            _focusHits++;
            int stack = Mathf.Min(Unit.StatusMaxStack, _focusHits / FocusHitsPerStack);
            return 1f + stack * _buffs.FocusPerStack;
        }

        /// <summary>
        /// 튕긴 뒤의 탄이 주는 피해. 정본 BUF_A03 은 50% 로 못박았고,
        /// 버프가 없으면 튕긴 탄은 **피해가 없다** — 그냥 다 때리면 벽이 공짜 이득이 된다.
        /// </summary>
        private int BouncedDamage(Projectile shot, int damage)
        {
            if (!shot.HasBounced) return damage;
            if (_buffs.ReturnDamagePercent <= 0) return 0;
            return Mathf.Max(1, damage * _buffs.ReturnDamagePercent / 100);
        }

        private void HitEnemyWith(Unit victim, int damage, HostEntry p)
        {
            victim.IsAggro = true;
            victim.SetState(EnemyState.Hit);
            // 저주는 받는 피해를 늘린다. 표시되는 숫자도 늘어난 값이어야 —
            // 저주를 걸어 놓고 숫자가 그대로면 걸린 줄 모른다.
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * victim.CurseDamageMul));
            // 표식·저주 증폭. 상태이상 저주와 **다른 층**이다 —
            // 저것은 관통 평타가 쌓는 것이고, 이것은 액티브 스킬이 거는 것이다.
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * victim.AmpDamageMul));
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * ScorchMul(victim)));
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * FocusMul(victim)));
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * CardDamageMul(victim)));
            // C004 갑옷 분쇄 — 원거리(`ApplyShotHit`)와 같은 규칙. 근접·액티브 스킬도
            // 플레이어 공격이므로 여기서 빠지면 근접 호스트만 이 카드를 못 쓴다.
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * victim.ArmorBreakMul(_buffs.ArmorBreakPerStack)));
            if (_buffs.ArmorBreakPerStack > 0f) victim.AddArmorBreak();
            // 상대 방어력(새 스탯). 코만도(수류탄)는 이 값을 절반 무시한다.
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * victim.DamageTakenMul(ArmorIgnorePercent)));
            // 설녀 — 얼려 놓은 적에게는 더 아프다. 닌자(사슬)는 묶어 놓은 적에게.
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * FrozenBonusMul(victim)));
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * BindBonusMul(victim)));
            // 처형 — 약해진 잡몹을 단칼에. 피해 계산을 다 마친 뒤에 본다.
            if (TryAssassinate(victim)) { KillEnemy(victim); return; }
            ApplyImprints(victim);
            // 방패 전개 — 이 구간에는 **앞에서 때리면** 잘 안 들어간다.
            // 그래야 "지금은 피할 때가 아니라 돌아갈 때" 라는 구간이 생긴다.
            if (victim.IsBoss && Avatar != null && BlockedByBossShield(victim, Avatar.Position))
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * BossShieldDamageMul));
            // ⚠ 숨어 있는 보스는 안 맞는다. 벽 뒤·구멍 안에 있는 것을 때릴 수는 없다.
            if (victim.IsBoss && !_bossExposed) return;

            // 취약 창 보너스. 보스에게만 붙는다.
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * BreakMul(victim)));
            // 가디언 마디 · 「나와 있을 때 때렸는가」를 여기서 센다.
            NoteBossDamage(victim, damage);
            ShowDamage(victim.Position, damage, toEnemy: true);
            SpawnFx("hit", victim.Position, HitFxSize);
            Shake(victim.IsBoss ? ShakeOnBossHurt : ShakeOnHit);
            bool dead = victim.TakeDamage(damage);
            // 둔화·흡혈은 이제 확률이다. 세기는 호스트마다 다르지 않고 한 값으로 묶는다 —
            // 터졌는지 아닌지가 읽혀야지, 25% 냐 45% 냐는 화면에서 구별되지 않는다.
            if (((p?.SlowPercent ?? 0) + _buffs.SlowPercent) > 0 && Roll(SlowChance))
                ApplySlowProc(victim);
            // 혈갈이 도는 동안은 확률이 아니라 확정이고, 도는 양도 배율이 붙는다.
            if (((p?.LifestealPercent ?? 0) + _buffs.LifestealPercent) > 0 && _host != null
                && (IsDrainForced || Roll(LeechChance)))
                Leech(Mathf.Max(1, Mathf.RoundToInt(damage * LeechPercent / 100f * DrainMul)));

            MeleeJobProc(victim, p);
            PassiveOnHit(victim, damage);

            // C031 과충전 — 맞은 자리에서 전기가 튄다. 이 경로는 전부 플레이어 공격이다
            // (근접 타격과 액티브 스킬). 적 공격은 `DamagePlayer` 로 간다.
            Overcharge(victim, damage);
            ChargeSkillOnHit();

            if (dead) { KillEnemy(victim); return; }
            if (victim.IsBoss)
                _bus.Publish(new BossHpChangedEvent { BossHp = victim.Hp, BossHpMax = victim.HpMax });
        }

        // ── 투사체 ────────────────────────────────────────────────
        private static readonly Color ShotPlayerColor = new(1f, 0.72f, 0.24f, 1f);

        /// <summary>
        /// 탄에 입힐 색. 플레이어 탄은 눈에 띄라고 주황으로 물들인다.
        ///
        /// ⚠ **제 색을 가진 그림은 물들이지 않는다.** 2026-09-14 샐러맨더 독불(초록)을 넣었는데
        ///   주황이 곱해져 화면에서는 그대로 주황 불로 보였다 — 새 색이 들어간 의미가 없다.
        /// </summary>
        private static Color ShotTint(bool fromPlayer, string kind)
        {
            if (!fromPlayer) return ShotEnemyColor;
            return kind == "venom" || kind == "thunder" || kind == "beam" || kind == "lightorb" || kind == "darkorb"
                 ? Color.white : ShotPlayerColor;
        }
        private static readonly Color ShotEnemyColor = new(0.55f, 0.78f, 1f, 1f);
        // 보스 탄은 잡몹과 색을 나눈다 — 화면이 탄으로 덮이면 무엇을 피해야 할지 안 보인다
        private static readonly Color ShotBossColor = new(1f, 0.36f, 0.30f, 1f);

        /// <summary>한 번에 나가는 탄 수 상한. 수치가 어긋났을 때의 마지막 방어선이다.</summary>
        private const int MaxShotsPerVolley = 8;

        /// <summary>여러 발을 벌리는 각도(발당). 8°는 겹치고 24°는 빗나간다.</summary>
        private const float ExtraFanDegrees = 14f;

        /// <summary>부채꼴 탄을 조준선과 직각으로 밀어 두는 폭(탄 상자 대비).</summary>
        private const float FanSideRatio = 0.5f;

        private void FireShot(Unit attacker, Unit target, bool fromPlayer, float angleOffsetDeg,
                              bool lastShot = false, int split = 1, float sideOffset = 0f)
        {
            var shot = RentShot();
            if (shot == null) return;
            var kind = ShotKindOf(attacker);
            shot.SetSprite(ShotSpriteOf(attacker), kind, LoopsFrames(kind));
            var p = attacker.Profile;
            bool snipe = p != null && p.Kind == AttackKind.Snipe;

            float speed = ShotSpeedOf(p, fromPlayer)
                          * (snipe ? 1.6f : 1f)
                          * (fromPlayer ? _buffs.ShotSpeedMul : 1f);

            // 몸 중심이 아니라 총구에서 나간다. 탄이 배에서 튀어나오면
            // 방향 스프라이트를 그린 의미가 없다.
            var muzzle = attacker.MuzzlePosition;
            if (sideOffset != 0f)
            {
                var aim = target.Position - muzzle;
                if (aim.sqrMagnitude > 0.0001f)
                {
                    var f = aim.normalized;
                    // 조준선의 법선. 부채꼴 탄이 총구 한 점에서 겹쳐 태어나지 않게 민다.
                    muzzle += new Vector2(-f.y, f.x) * sideOffset;
                }
            }

            shot.Fire(muzzle, target.Position, speed,
                      fromPlayer
                          ? Mathf.Max(1, Mathf.RoundToInt(
                                attacker.Atk * _buffs.AttackMul * EchoMul * SwingMul(fromPlayer)
                                * (lastShot ? 1f + _buffs.LastShotBonus : 1f) / split))
                          : Mathf.Max(1, Mathf.RoundToInt(attacker.Atk / (float)split)),
                      fromPlayer, target,
                      fromPlayer ? _config.ShotSize * BeamWidthMul : _config.ShotSize,
                      ShotTint(fromPlayer, kind),
                      _config.ShotLifeSeconds,
                      pierce: (p != null && p.Kind == AttackKind.Pierce)
                              || (fromPlayer && (_buffs.Pierce || IsPierceGranted)),
                      slowPercent: (p?.SlowPercent ?? 0) + (fromPlayer ? _buffs.SlowPercent : 0),
                      lifestealPercent: (p?.LifestealPercent ?? 0) + (fromPlayer ? _buffs.LifestealPercent : 0),
                      angleOffsetDeg: angleOffsetDeg,
                      // 슬러거는 탄을 되받아치는 것이 정체성이다(`ReflectsShots`).
                      // 그 몸에 들어가면 쏘는 탄도 튕긴다 — 버프 없이도 한 번은 튕긴다.
                      bounces: fromPlayer
                          ? _buffs.Bounces + ((p != null && p.ReflectsShots) ? 1 : 0)
                          : 0);

            if (fromPlayer && _buffs.HomingStrength > 0f) shot.SetHoming(_buffs.HomingStrength);
            // 정본이 유도라고 적은 적만 휜다(지금은 코만도(미사일) 하나).
            // 나머지는 쏜 방향으로 끝까지 직진한다.
            else if (!fromPlayer && p != null && p.CanonHoming > 0f) shot.SetHoming(p.CanonHoming);

            // 던지는 탄도 물린 자리에서 출발해야 앞뒤 간격이 유지된다
            if (kind == "grenade") ThrowAsGrenade(shot, muzzle, target.Position,
                                                  angleOffsetDeg, speed);
            // ⚠ **미사일도 터진다.** 예전에는 폭발 반경이 없어 닿은 한 명만 때리고,
            //   빗나가면 그대로 날아가 수명으로 조용히 사라졌다 —
            //   화면에서는 「폭탄이 쭉 날아가 없어지는」 것으로 보였다(기획 2026-09-15).
            else if (kind == "missile") shot.SetBlastRadius(Meters(MissileBlastMeters));
            // ⚠ **로봇 미사일은 뚫지 않고 터진다**(기획 2026-09-15). 표의 공격 종류가 관통이라 적을
            //   뚫고 지나갔다 — 원작은 맞은 자리에서 크게 터진다. 반경이 있으면 명중 즉시 터진다.
            else if (fromPlayer && attacker.Key == DeployHostKey) shot.SetBlastRadius(Meters(RobotBlastMeters));
        }

        // ── 던지는 탄(수류탄) ────────────────────────────────────
        //
        // 사람이 아니라 땅을 노린다. 나는 동안은 아무것도 맞히지 않고 —
        // 그래서 기둥을 넘어간다 — 떨어진 자리 반경 안의 모두를 때린다.
        // 빗나가도 발밑이면 아프고, 대신 날아오는 게 보이므로 피할 수 있다.

        private const float LobMinSeconds = 0.35f;
        private const float LobMaxSeconds = 0.95f;
        private const float LobArcRatio = 0.30f;    // 던진 거리에 비례한 높이
        private const float LobMinArc = 60f;
        private const float LobMaxArc = 200f;
        /// <summary>로봇 미사일 폭발 반경(m). 코만도 미사일(1.9)보다 조금 작다 — 연사가 빠르다.</summary>
        private const float RobotBlastMeters = 1.6f;
        private const float BlastRadius = 130f;     // 탄 명중 반경(34)보다 훨씬 넓다

        private void ThrowAsGrenade(Projectile shot, Vector2 from, Vector2 at,
                                    float angleOffsetDeg, float speed)
        {
            var d = at - from;
            if (Mathf.Abs(angleOffsetDeg) > 0.01f)
            {
                // 여러 발을 던지면 **떨어지는 자리**가 벌어져야 한다.
                // 곧게 나는 탄처럼 진행 방향만 틀면 결국 같은 곳에 떨어진다.
                float r = angleOffsetDeg * Mathf.Deg2Rad;
                float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
                d = new Vector2(d.x * cos - d.y * sin, d.x * sin + d.y * cos);
            }
            var landing = from + d;
            float dist = d.magnitude;
            shot.Lob(landing,
                     Mathf.Clamp(dist / Mathf.Max(1f, speed), LobMinSeconds, LobMaxSeconds),
                     Mathf.Clamp(dist * LobArcRatio, LobMinArc, LobMaxArc));
        }

        /// <summary>떨어진 자리에서 터진다. 반경 안은 모두 맞는다.</summary>
        private void Explode(Projectile shot)
        {
            var at = shot.Position;
            float r = (shot.BlastRadiusOverride > 0f ? shot.BlastRadiusOverride : BlastRadius)
                    * (shot.FromPlayer ? _buffs.AoeMul : 1f);
            // 불덩이가 피해 반경과 같은 크기로 뜬다. 그림이 반경보다 작으면
            // "안 맞았는데 맞았다" 로 읽히고, 크면 그 반대가 된다.
            // 로봇 미사일(`pulse` 탄)은 원작처럼 **큰 폭발**로 터진다 — 이름대로 찾으면 작은 섬광 두 장이다.
            SpawnImpact(at, shot.Kind == "pulse" ? "grenade" : shot.Kind, r * 2f);

            if (!shot.FromPlayer)
            {
                var me = Avatar;
                if (me != null && Vector2.Distance(at, me.Position) <= r) DamagePlayer(shot.Damage);
                return;
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                if (Vector2.Distance(at, e.Position) > r) continue;
                ApplyShotHit(e, shot);
            }

            QueueEchoBlast(at, r, shot.Damage, shot.Kind);
        }

        /// <summary>풀에서 하나 꺼낸다. 매 발마다 GameObject 를 만들면 교전 중 GC 가 튄다.</summary>
        /// <summary>
        /// 캐릭터 키 → 탄 그림 이름. 무기가 다른데 탄이 같으면 화면에서 무엇이
        /// 날아오는지 읽히지 않는다 — 레이저도 수류탄도 서리도 노란 총알이었다.
        ///
        /// 캐릭터마다 한 장씩 두지 않고 **무기 계열로 묶는다.** 21종이면 21장을
        /// 그려야 하지만 계열로 묶으면 8장이면 되고, 그래도 읽히는 데는 충분하다.
        /// 근접(amazon·amazon_elite·baseball·vampire)은 탄이 없어 여기 없다.
        /// </summary>
        private static readonly Dictionary<string, string> ShotKind = new()
        {
            // ⚠ 예전에는 이 일곱이 **전부 `bullet` 한 장**이었다. 무엇이 날아오는지
            //   구별이 안 되고 화면이 늘 같아 보였다(2026-09-10 「투사체가 다 똑같다」).
            //   쏘는 물건이 다르면 탄도 달라야 한다.
            { "gangster", "bullet" },       // 권총 — 기본 탄을 그대로 쓴다
            // ⚠ 폭력배는 **기관총**이다(확정본 2026-09-14 — 중거리 3종 중 하나).
            //   예전에 쇳조각을 던지던 시절의 탄(`shot_thug`)이 그대로 남아 있어
            //   기관총이 고철을 뿌리고 있었다. 이미 있는 예광탄을 쓴다 — 새 그림은 없다.
            { "thug", "mg" },               // 기관총 — 길쭉한 예광탄
            { "hopper", "bullet" },
            { "hopper_smg", "smg" },        // 기관단총 — 작고 빠른 탄
            { "commando_mg", "mg" },        // 기관총 — 길쭉한 예광탄
            { "commando_laser", "laser" },
            { "commando_grenade", "grenade" },
            // ⚠ 여기가 비어 있어서 미사일 코만도가 **딱총을 쐈다.** 표에 없는 배우는
            //   `ShotFrames(null)` 로 떨어져 기본 탄 한 장(`shot_1`)이 나간다.
            //   정본에서 유일한 유도탄(CanonHoming 0.25)인데 화면에서는 점이었다.
            { "commando_missile", "missile" },
            // ⚠ 네 쌍이 둘씩 같은 그림을 쓰고 있었다 — 누가 쏜 것인지 구별이 안 됐다.
            //   원작에서도 비슷하면 우리 쪽에서 색과 모양을 갈라 놓는다.
            { "salamander", "venom" }, { "dragoon", "dragoon" },      // 독불(2026-09-14 기획) / 불덩이
            { "dragon_blue", "thunder" }, { "snowwoman", "frost" },   // 청룡은 번개(2026-09-14) · 설녀만 냉기
            { "ninja", "shuriken" }, { "ninja_chain", "chain" },      // 수리검 / 사슬낫
            // 매지션 라이트 · 다크 — 원작 「Magic Beam」 빛 구슬(2026-09-15 — 주황 막대 · 도깨비불이 원작과 달랐다)
            { "white_wizard", "lightorb" }, { "medium", "darkorb" },
            { "guru", "pulse" }, { "robot", "pulse" },                // 둥근 파동 / 로봇은 미사일(`shot_pulse` 가 미사일 그림 — 2026-09-15 되돌림)
            // 정본에서 원거리로 바뀐 둘. 전용 그림이 없으면 흰 점으로 나간다.
            { "vampire", "drain" },     // 원작 시트의 박쥐 2장 (날개 편 것 / 접은 것)
            { "baseball", "ball" },     // 야구공 — 붉은 실밥
            { "amazon_elite", "spear" }, // 던지는 창
            // 첫 원거리 잡몹. 전용 탄이 없으면 흰 점으로 나가서
            // 무엇이 날아오는지 안 보인다.
            { TrashGunnerKey, TrashGunnerKey },
            { TrashWardenKey, TrashWardenKey },
            { TrashCoilKey,   TrashCoilKey },
        };

        /// <summary>캐릭터별 탄 그림. 아직 안 온 것은 기본 탄으로 떨어진다.</summary>
        private readonly Dictionary<string, Sprite[]> _shotSprite = new();

        /// <summary>탄 종류 이름. 없는 배우는 null — 기본 그림을 쓴다.</summary>
        private static string ShotKindOf(Unit u)
            => u != null && u.Key != null && ShotKind.TryGetValue(u.Key, out var k) ? k : null;

        /// <summary>
        /// 여러 장이 **반복 동작**인 탄. 나머지는 태어나는 모습(작은 것이 커진다)이라
        /// 한 번만 넘기고 멈춘다.
        ///
        /// 박쥐는 원작 시트의 `Bats` 2장 — 날개 편 것과 접은 것이다. 멈춰 세우면
        /// 날개를 접은 채 미끄러져 가고, 그게 "박쥐가 안 난다"로 보인다.
        /// 표창(회전 2장)·수류탄(회전 4장)도 같다 — 던진 물건은 돌면서 간다.
        /// </summary>
        /// 미사일 4장도 반복이다 — 몸통은 네 장 모두 같고 **꼬리불만 뛴다.**
        /// 한 번만 넘기면 꼬리가 가장 긴 4번에서 굳은 채로 날아간다.
        private static bool LoopsFrames(string kind)
            => kind == "drain" || kind == "shuriken" || kind == "grenade" || kind == "missile"
               || kind == "lightorb" || kind == "darkorb";   // 빛 구슬 4장 — 테가 일렁이며 날아간다

        /// <summary>
        /// 맞은 자리에서 터뜨린다. 그림이 없으면 아무것도 하지 않는다 —
        /// 8종을 한 번에 받지 못해도 받은 것부터 보이게 한다.
        /// </summary>
        // ── 상태 표시(fx_*) ───────────────────────────────────
        //
        // 착탄(`impact_*`)과 이름을 나눠 둔다 — 이쪽은 탄이 맞은 자리가 아니라
        // **몸 위에서** 벌어지는 일이라, 몸을 따라다니고 상태가 풀릴 때까지 돈다.
        private readonly Dictionary<string, Sprite[]> _fxSprite = new();

        private Sprite[] FxFrames(string name)
        {
            if (_fxSprite.TryGetValue(name, out var cached)) return cached;
            var list = new List<Sprite>(4);
            for (int i = 1; i <= 8; i++)
            {
                var sp = GetSprite($"fx_{name}_{i}");
                if (sp == null) break;
                list.Add(sp);
            }
            var frames = list.Count > 0 ? list.ToArray() : null;
            _fxSprite[name] = frames;   // 없으면 null 을 넣어 둔다 — 매 프레임 다시 찾지 않게
            return frames;
        }

        /// <summary>한 번 보여 주고 끝나는 표시(흡혈).</summary>
        private void SpawnFx(string name, Vector2 at, float size)
            => PlayFx(name, at, size, loop: false);

        /// <summary>돌아가는 표시를 하나 얻는다. 상태가 풀릴 때 <see cref="Impact.Stop"/> 로 돌려준다.</summary>
        private Impact TakeLoopFx(string name, Vector2 at, float size)
            => PlayFx(name, at, size, loop: true);

        /// <summary>
        /// <paramref name="over"/> 를 주면 여러 장을 그 시간에 걸쳐 넘기고
        /// **마지막 장에서 멈춰 선다**(예고 내내 자라는 바닥 균열). 거두는 것은 부른 쪽의 몫이다.
        /// </summary>
        private Impact PlayFx(string name, Vector2 at, float size, bool loop, float over = 0f)
        {
            var frames = FxFrames(name);
            if (frames == null) return null;

            var im = FreeImpact(size);
            if (im == null) return null;

            if (over > 0f) im.PlayOver(at, frames, size, over);
            else im.Play(at, frames, size, loop);
            return im;
        }

        private Impact FreeImpact(float size)
        {
            for (int i = 0; i < _impacts.Count; i++)
                if (!_impacts[i].IsActive) return _impacts[i];

            if (_impacts.Count >= MaxImpacts) return null;
            var go = new GameObject($"Impact_{_impacts.Count}", typeof(RectTransform));
            var im = go.AddComponent<Impact>();
            im.Cache(_shotLayer, size);
            _impacts.Add(im);
            return im;
        }

        /// <summary>스턴 표시는 정수리 위에 뜬다. 그림 아래 절반이 비어 있어 얼굴을 가리지 않는다.</summary>
        private const float StunFxSize = 48f;

        /// <summary>묶임 표시를 발밑으로 내리는 거리. 몸 한가운데 달면 체력바·피해 숫자와 겹친다.</summary>
        private const float RootFxLift = 26f;
        private const float StunFxLift = 52f;

        /// <summary>
        /// **내 몸 위의 별은 더 크고 더 높이 뜬다.**
        ///
        /// ⚠ 잡몹과 같은 48px·52px 로 띄웠더니 **통째로 묻혔다.** 내 몸 정수리에는
        ///   이미 체력바가 걸려 있고 피해 숫자도 거기서 솟는다 — 별이 그 뒤에 깔려
        ///   "굳었는데 아무 표시가 없다" 가 됐다(기획 2026-09-03).
        ///   굳은 것은 조작이 안 먹는다는 뜻이라 **제일 먼저 보여야 하는 표시**다.
        /// </summary>
        private const float MyStunFxSize = 84f;
        private const float MyStunFxLift = 74f;   // 92 는 너무 떠 보였다
        private const float ShieldFxSize = 96f;

        private readonly Dictionary<Unit, Impact> _stunFx = new();
        private readonly List<Unit> _stunFxDone = new();

        /// <summary>
        /// 화상 불. **몸에 붙어서 화상이 풀릴 때까지 돈다.**
        ///
        /// ⚠ 예전에는 피해 틱마다 한 번짜리 불을 **그 자리에** 피웠다. 1단계만 걸려도
        ///   초당 여섯 번이 겹쳐 뜨고, 적이 걸어가면 불이 제자리에 남아 뒤로 줄을 이었다 —
        ///   몸이 타는 게 아니라 **누가 불을 뿜는 것처럼** 보였다(기획 2026-09-11).
        /// </summary>
        private readonly Dictionary<Unit, Impact> _burnFx = new();
        private readonly List<Unit> _burnFxDone = new();

        /// <summary>
        /// 독 · 묶임도 **몸에 붙어 돈다.** 화상과 같은 규칙이다 —
        /// 걸린 순간 한 번 번쩍이고 마는 표시로는 **누가 걸렸는지**를 알 수 없다.
        /// 새 그림 없이 있는 것을 쓴다(독 `fx_venom` · 사슬 `fx_chain`).
        /// </summary>
        private readonly Dictionary<Unit, Impact> _poisonFx = new();
        private readonly List<Unit> _poisonFxDone = new();
        private readonly Dictionary<Unit, Impact> _rootFx = new();
        private readonly List<Unit> _rootFxDone = new();

        private const float PoisonFxSize = 56f;
        private const float RootFxSize = 64f;
        private Impact _shieldFx;

        /// <summary>
        /// 굳은 몸 위에 표시를 띄우고, 풀린 몸의 표시는 거둔다.
        /// 쉴드 링은 내 몸 하나뿐이라 따로 들고 있는다.
        /// </summary>
        private void TickStatusFx(float dt)
        {
            _stunFxDone.Clear();
            foreach (var kv in _stunFx)
            {
                var u = kv.Key;
                if (u == null || !u.IsAlive || !u.IsStunned) { kv.Value?.Stop(); _stunFxDone.Add(u); continue; }
                // ⚠ 띄울 때 쓴 높이와 **같은 높이**로 따라다녀야 한다. 여기서만
                //   잡몹 높이를 쓰면 다음 프레임에 별이 체력바 뒤로 내려앉는다.
                kv.Value?.MoveTo(u.Position
                    + Vector2.up * (u == Avatar ? MyStunFxLift : StunFxLift));
            }
            for (int i = 0; i < _stunFxDone.Count; i++) _stunFx.Remove(_stunFxDone[i]);

            _burnFxDone.Clear();
            foreach (var kv in _burnFx)
            {
                var u = kv.Key;
                if (u == null || !u.IsAlive || u.BurnStack <= 0) { kv.Value?.Stop(); _burnFxDone.Add(u); continue; }
                kv.Value?.MoveTo(u.Position);
            }
            for (int i = 0; i < _burnFxDone.Count; i++) _burnFx.Remove(_burnFxDone[i]);

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.BurnStack <= 0 || _burnFx.ContainsKey(e)) continue;
                var fx = TakeLoopFx("burn", e.Position, BurnFxSize);
                if (fx != null) _burnFx[e] = fx;
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || !e.IsStunned || _stunFx.ContainsKey(e)) continue;
                var fx = TakeLoopFx("stun", e.Position + Vector2.up * StunFxLift, StunFxSize);
                if (fx != null) _stunFx[e] = fx;
            }

            // ── 독 ─────────────────────────────────────────────
            _poisonFxDone.Clear();
            foreach (var kv in _poisonFx)
            {
                var u = kv.Key;
                if (u == null || !u.IsAlive || !u.IsPoisoned) { kv.Value?.Stop(); _poisonFxDone.Add(u); continue; }
                kv.Value?.MoveTo(u.Position);
            }
            for (int i = 0; i < _poisonFxDone.Count; i++) _poisonFx.Remove(_poisonFxDone[i]);

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || !e.IsPoisoned || _poisonFx.ContainsKey(e)) continue;
                var fx = TakeLoopFx("venom", e.Position, PoisonFxSize);
                if (fx != null) _poisonFx[e] = fx;
            }

            // ── 묶임 ───────────────────────────────────────────
            //
            // 방 전체를 5초 묶는 기술이라 **누가 묶였는지**가 보여야 한다.
            // 발을 묶은 것이므로 표시는 발밑에 단다.
            _rootFxDone.Clear();
            foreach (var kv in _rootFx)
            {
                var u = kv.Key;
                if (u == null || !u.IsAlive || !u.IsRooted) { kv.Value?.Stop(); _rootFxDone.Add(u); continue; }
                kv.Value?.MoveTo(u.Position - Vector2.up * RootFxLift);
            }
            for (int i = 0; i < _rootFxDone.Count; i++) _rootFx.Remove(_rootFxDone[i]);

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || !e.IsRooted || _rootFx.ContainsKey(e)) continue;
                var fx = TakeLoopFx("chain", e.Position - Vector2.up * RootFxLift, RootFxSize);
                if (fx != null) _rootFx[e] = fx;
            }

            var me = Avatar;

            // 별은 잡몹만 다는 것이 아니다. 내가 굳었을 때가 제일 알아야 할 때다.
            if (me != null && me.IsAlive && me.IsStunned && !_stunFx.ContainsKey(me))
            {
                var mine = TakeLoopFx("stun", me.Position + Vector2.up * MyStunFxLift, MyStunFxSize);
                if (mine != null) _stunFx[me] = mine;
            }

            bool wantShield = me != null && me.IsAlive && me.Shield > 0;
            if (wantShield)
            {
                if (_shieldFx == null || !_shieldFx.IsActive)
                    _shieldFx = TakeLoopFx("shield", me.Position, ShieldFxSize);
                _shieldFx?.MoveTo(me.Position);
            }
            else if (_shieldFx != null) { _shieldFx.Stop(); _shieldFx = null; }
        }

        private void SpawnImpact(Vector2 at, string kind, float size = 0f)
        {
            var frames = ImpactFrames(kind);
            if (frames == null) return;
            // 터짐 그림은 48 캔버스라 탄(24)보다 여백이 크다. 상자를 탄과 같은 값으로
            // 두면 화면에서 탄보다 조금 큰 정도로 보인다 — 그게 기본이다.
            if (size <= 0f) size = _config.ShotSize;

            for (int i = 0; i < _impacts.Count; i++)
                if (!_impacts[i].IsActive) { _impacts[i].Play(at, frames, size); return; }

            if (_impacts.Count >= MaxImpacts) return;   // 화면이 터짐으로 덮이지 않게 상한을 둔다
            var go = new GameObject($"Impact_{_impacts.Count}", typeof(RectTransform));
            var im = go.AddComponent<Impact>();
            im.Cache(_shotLayer, size);
            _impacts.Add(im);
            im.Play(at, frames, size);
        }

        /// <summary>터짐 그림 여러 장. 종류마다 장 수가 다르다 — 수류탄 폭발은 원작이 5장이다.</summary>
        private Sprite[] ImpactFrames(string kind)
        {
            if (kind != null && _impactSprite.TryGetValue(kind, out var cached)) return cached;

            var list = new List<Sprite>(5);
            for (int i = 1; i <= 8; i++)
            {
                var s = GetSprite(kind != null ? $"impact_{kind}_{i}" : $"impact_{i}");
                if (s == null) break;
                list.Add(s);
            }
            if (list.Count == 0)
            {
                var fallback = GetSprite("impact_1");
                if (fallback != null) list.Add(fallback);
            }
            var frames = list.Count > 0 ? list.ToArray() : null;
            if (kind != null) _impactSprite[kind] = frames;
            return frames;
        }

        private readonly Dictionary<string, Sprite[]> _impactSprite = new();

        /// <summary>
        /// 그 몸의 탄 그림 여러 장. 원작이 날아가는 동안 보여 주는 장면들이다 —
        /// 표창은 2장(회전), 수류탄·미사일은 4장, 눈덩이·화염·구슬은 3장(커짐).
        /// 없으면 기본 탄 한 장으로 떨어진다.
        /// </summary>
        private Sprite[] ShotSpriteOf(Unit u)
        {
            var key = u != null ? u.Key : null;
            if (key == null) return ShotFrames(null);
            if (_shotSprite.TryGetValue(key, out var cached)) return cached;

            var frames = ShotFrames(ShotKind.TryGetValue(key, out var kind) ? kind : null);
            _shotSprite[key] = frames;
            return frames;
        }

        private readonly Dictionary<string, Sprite[]> _kindShotSprite = new();

        private Sprite[] ShotFrames(string kind)
        {
            // `SpriteAtlas.GetSprite` 는 부를 때마다 새 Sprite 를 만든다(876행 주석 참조).
            // 진화 원형탄은 한 번에 12발까지 나가므로 부를 때마다 만들면 그대로 쌓인다.
            var cacheKey = kind ?? string.Empty;
            if (_kindShotSprite.TryGetValue(cacheKey, out var hit)) return hit;

            var list = new List<Sprite>(4);
            for (int i = 1; i <= 8; i++)
            {
                var s = GetSprite(kind != null ? $"shot_{kind}_{i}" : $"shot_{i}");
                if (s == null) break;
                list.Add(s);
            }
            if (list.Count == 0)
            {
                var fallback = GetSprite("shot_1") ?? GetSprite("shot");
                if (fallback != null) list.Add(fallback);
            }
            var frames = list.Count > 0 ? list.ToArray() : null;
            _kindShotSprite[cacheKey] = frames;
            return frames;
        }

        private Projectile RentShot()
        {
            for (int i = 0; i < _shots.Count; i++)
                if (!_shots[i].IsActive) return _shots[i];

            if (_shots.Count >= MaxShots) return null;   // 폭주 방지 상한
            var go = new GameObject("Shot", typeof(RectTransform));
            go.transform.SetParent(_shotLayer, false);
            var p = go.AddComponent<Projectile>();
            p.Init(GetSprite("shot"));
            _shots.Add(p);
            return p;
        }

        /// <summary>
        /// 방 네 벽에서 튕긴다. 튕겼으면 true.
        /// 벽 안쪽으로 한 칸 밀어 넣어 같은 프레임에 두 번 튕기는 것을 막는다.
        /// </summary>
        private bool BounceOffWalls(Projectile p)
        {
            var q = p.Position;
            Vector2 n = Vector2.zero;
            if (q.x <= 0f) n = Vector2.right;
            else if (q.x >= _roomSize.x) n = Vector2.left;
            else if (q.y >= 0f) n = Vector2.down;
            else if (q.y <= -_roomSize.y) n = Vector2.up;
            if (n == Vector2.zero) return false;
            return p.Bounce(n);
        }

        /// <summary>부딪힌 엄폐물에서 되튕길 방향. 얕게 겹친 축으로 튕겨야 자연스럽다.</summary>
        private Vector2 BounceNormalFromCover(Vector2 at)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                var o = _obstacles[i];
                if (!o.BlocksShot || !o.ShotBounds.Contains(at)) continue;
                float dx = at.x - o.ShotBounds.center.x;
                float dy = at.y - o.ShotBounds.center.y;
                float ox = o.ShotBounds.width * 0.5f - Mathf.Abs(dx);
                float oy = o.ShotBounds.height * 0.5f - Mathf.Abs(dy);
                return ox < oy ? new Vector2(Mathf.Sign(dx), 0f) : new Vector2(0f, Mathf.Sign(dy));
            }
            return Vector2.up;
        }

        private void TickShots(float dt)
        {
            var me = Avatar;
            for (int i = 0; i < _shots.Count; i++)
            {
                var p = _shots[i];
                if (!p.IsActive) continue;

                if (!p.Tick(dt))
                {
                    // 수명을 다했다. 오는 길에 몸을 스쳤다면 그것이 곧 "피한 것"이다.
                    if (p.GrazedPlayer) FireAfterimage();
                    // 아무것도 못 맞히고 사라진 내 탄 = 빗나감. 과열 카운터를 되돌린다.
                    if (p.FromPlayer && !p.HasHitAnything) ResetOverheat();
                    // 터지는 탄은 **땅에 떨어진 것**이다. 조용히 사라지면 안 된다.
                    if (p.BlastRadiusOverride > 0f) Explode(p);
                    p.Despawn();
                    continue;
                }

                // 던진 탄은 공중에 있다 — 벽도 기둥도 사람도 스쳐 지나간다.
                // 떨어진 그 순간에만 일이 벌어진다.
                if (p.IsLob)
                {
                    if (!p.HasLanded) continue;
                    Explode(p);
                    p.Despawn();
                    continue;
                }

                // 방 벽에서 튕긴다. 도탄이 없는 탄은 그냥 밖으로 나가 수명으로 사라진다 —
                // 벽에서 없애 버리면 화면 끝에서 탄이 뚝 끊겨 어색하다.
                if (p.BouncesLeft > 0 && !BounceOffWalls(p)) { }

                // 엄폐물에 막힌다. 이게 없으면 기둥이 그림일 뿐이라
                // 뒤에 숨는 것이 아무 의미가 없다.
                // 부술 수 있는 것이 먼저다. 막히기 전에 때려야 뚫린다.
                if (p.FromPlayer && p.Damage > 0 && DamageCrate(p.Position, p.Damage))
                {
                    if (!p.Pierce) { p.Despawn(); continue; }
                }

                if (BlockedByCover(p.Position, p.FromPlayer))
                {
                    // 도탄이 남아 있으면 기둥에서도 튕긴다. 정본 "도탄 벽" 지형지물이
                    // 이 경로를 쓴다 — 기둥이 막기만 하는 것이 아니라 되돌려 준다.
                    if (!p.Bounce(BounceNormalFromCover(p.Position))) { p.Despawn(); continue; }
                }

                if (p.Damage <= 0) continue;   // 근접 타격 섬광 — 수명만 흘려보낸다

                if (p.FromPlayer)
                {
                    var hit = HitEnemy(p.Position, p);
                    if (hit == null) continue;
                    // 터지는 탄은 **닿은 자리에서 터진다.** 반경 안이 다 맞는다.
                    if (p.BlastRadiusOverride > 0f) { Explode(p); p.Despawn(); continue; }
                    if (p.Pierce) p.MarkHit(hit); else p.Despawn();
                    SpawnImpact(ImpactPointOn(hit, p.Position), p.Kind);
                    ApplyShotHit(hit, p);
                }
                else
                {
                    // ⚠ **도발 중인 소환수(골렘·분신)도 적 탄을 맞는다**(기획 2026-09-15). 탄이 플레이어하고만
                    //   부딪혀서, 적이 골렘을 겨누고 쏜 탄이 골렘을 그대로 뚫고 지나갔다.
                    var taunt = TauntUnit;
                    if (taunt != null
                        && Vector2.Distance(p.Position, taunt.Position) <= taunt.BodyRadius + _config.ShotHitRadius)
                    {
                        SpawnImpact(ImpactPointOn(taunt, p.Position), p.Kind);
                        p.Despawn();
                        SoakWithSummon(p.Damage);
                        continue;
                    }
                    if (me == null) { p.Despawn(); continue; }
                    float d = Vector2.Distance(p.Position, me.Position);
                    if (d > _config.ShotHitRadius)
                    {
                        // 맞지는 않았지만 몸을 스쳤다. 표시만 해 두고 지나 보낸다 —
                        // 여기서 바로 잔상을 내면 아직 피한 것이 아니다(뒤에서 맞을 수 있다).
                        if (_buffs.AfterimagePercent > 0f
                            && d <= _config.ShotHitRadius * GrazeRadiusMul) p.GrazedPlayer = true;
                        continue;
                    }
                    // ⚠ **유령은 탄이 통과한다.** `DamagePlayer` 가 피해를 막고는 있었지만
                    //   탄이 여기서 사라지고 명중 이펙트까지 터져서, 화면에는 유령이
                    //   계속 맞고 있는 것으로 보였다 — 마침 Ghost HP 는 시계로 줄고 있어서
                    //   "맞아서 닳는다" 로 읽힌다. 판정 자체를 지나가게 한다.
                    if (_host == null) continue;

                    if (p.BlastRadiusOverride > 0f) { Explode(p); p.Despawn(); continue; }
                    p.Despawn();
                    SpawnImpact(ImpactPointOn(me, p.Position), p.Kind);
                    DamagePlayer(p.Damage);
                }
            }
        }

        /// <summary>
        /// 터지는 자리는 **맞은 몸 위**다. 탄이 있던 자리에 터뜨리면 몸에서 떨어져 터진다 —
        /// 명중 판정을 몸통 반경(53) + 탄 반경(34) 으로 넓히면서 최대 87픽셀까지 벌어졌다.
        /// 몸 중심에서 탄이 온 쪽으로 조금 당겨, 어느 쪽에서 맞았는지도 함께 읽히게 한다.
        /// </summary>
        private static Vector2 ImpactPointOn(Unit victim, Vector2 shotAt)
        {
            if (victim == null) return shotAt;
            return victim.Position
                 + Vector2.ClampMagnitude(shotAt - victim.Position, victim.BodyRadius * 0.5f);
        }

        /// <summary>탄이 닿은 적. 관통탄은 이미 때린 대상을 건너뛴다.</summary>
        private Unit HitEnemy(Vector2 at, Projectile shot)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                if (shot.Pierce && shot.HasHit(e)) continue;
                // 몸통 반경 + 탄 반경. 중심끼리의 고정 거리로 재면 몸이 커진 만큼
                // 어깨를 지나는 탄이 통과한다 — 관통탄이 앞사람만 맞히던 원인이다.
                if (Vector2.Distance(at, e.Position) <= e.BodyRadius + _config.ShotHitRadius) return e;
            }
            return null;
        }

        // ⚠ **중거리의 착탄 범위는 걷어냈다** (2026-09-14 결정).
        //    「직업이 규칙을 하나씩 갖는다」를 그만두고, 차별화는 캐릭터의 스킬이 맡는다.
        //    남은 직업 규칙은 **근거리의 쉴드 · 확률 스턴** 하나뿐이다.
        //    범위가 필요한 몸은 제 스킬로 갖는다 — 수류탄의 포물선 투척은 여기와 무관하게
        //    따로 살아 있다(기둥을 넘어가 1.8칸이 터지는 그것).

        private void ApplyShotHit(Unit victim, Projectile shot)
        {
            // 맞았으면 무조건 반응한다. 사거리가 탐지 거리보다 긴 호스트(히트맨 357)로
            // 저격하면 적이 맞고도 가만히 있는 그림이 된다.
            victim.IsAggro = true;
            victim.SetState(EnemyState.Hit);
            int dmg = BouncedDamage(shot, shot.Damage);
            if (dmg <= 0) return;   // 버프 없이 튕긴 탄은 스쳐 지나간다

            // 패시브 · 탄창 과열(호퍼 기관단총) — 연속 명중 N타마다 다음 1발이 3배.
            // ⚠ **세는 것은 명중한 순간**이고, 터지는 것은 그 다음 발이다.
            //   같은 발에 세고 터뜨리면 8타째가 곧 3배가 되어 "다음 1발" 이 사라진다.
            if (shot.FromPlayer)
            {
                if (_overheatArmed)
                {
                    _overheatArmed = false;
                    dmg = Mathf.RoundToInt(dmg * OverheatMul);
                }
                CountOverheat();
            }
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * victim.CurseDamageMul));
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * victim.AmpDamageMul));
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * ScorchMul(victim)));
            if (victim.IsBoss && !_bossExposed) return;   // 숨어 있으면 안 맞는다
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * BreakMul(victim)));
            NoteBossDamage(victim, dmg);
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * CardDamageMul(victim)));
            // C004 갑옷 분쇄 — 이번 타격은 **이미 벗겨진 만큼** 더 아프다.
            // 겹은 때린 다음에 쌓는다. 먼저 쌓으면 첫 타부터 보너스가 붙어
            // "반복 공격이 약화시킨다" 가 아니라 그냥 공격력 증가가 된다.
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * victim.ArmorBreakMul(_buffs.ArmorBreakPerStack)));
            if (_buffs.ArmorBreakPerStack > 0f) victim.AddArmorBreak();
            if (shot.FromPlayer)
            {
                // 상대 방어력(새 스탯) · 얼어 있는 적 보너스. 근접 경로와 같은 규칙이다.
                dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * victim.DamageTakenMul(ArmorIgnorePercent)));
                dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * FrozenBonusMul(victim)));
                dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * BindBonusMul(victim)));
            }
            ApplyImprints(victim);

            // 치명타는 **맨 마지막에** 곱한다. 다른 보정(저주·갑옷 분쇄·카드)을
            // 다 태운 값에 얹어야 "크게 터진 한 방" 이 실제로 크다.
            bool crit = shot.FromPlayer && RollCrit();
            // 호퍼 패시브는 치명타 **피해**를 키운다. 확률은 스탯이 따로 갖는다.
            if (crit) dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * (CritMultiplier + CritDamageBonus)));

            ShowDamage(victim.Position, dmg, toEnemy: true, crit);
            if (shot.FromPlayer)
            {
                GameSound.Cue("hit.enemy");
                SpawnFx(crit ? "crit" : "hit", victim.Position,
                        crit ? CritFxSize : HitFxSize);
                Shake(crit ? ShakeOnCrit : victim.IsBoss ? ShakeOnBossHurt : ShakeOnHit);
                if (crit) HitStop(HitStopOnCrit);
            }
            bool dead = victim.TakeDamage(dmg);
            // 원거리 몸이 근접 몹을 맞히면 뒤로 민다(기획 2026-09-16). 적 탄이 나를 맞힐 때는 이 길을 안 지난다.
            if (!dead && shot.FromPlayer) RangedKnockback(victim);
            if (shot.SlowPercent > 0 && Roll(SlowChance))
                ApplySlowProc(victim);
            if (shot.LifestealPercent > 0 && _host != null && Roll(LeechChance))
                Leech(Mathf.Max(1, shot.Damage * LeechPercent / 100));

            // C031 과충전 — 맞은 자리에서 전기가 튄다. 죽은 뒤에도 옆으로는 튄다.
            if (shot.FromPlayer) { Overcharge(victim, dmg); ChargeSkillOnHit(); PassiveOnHit(victim, dmg); }

            if (dead) { KillEnemy(victim); return; }
            if (victim.IsBoss)
                _bus.Publish(new BossHpChangedEvent { BossHp = victim.Hp, BossHpMax = victim.HpMax });
        }

        /// <summary>
        /// 몸을 입고 있을 때 **조건부 적의 잠금 표식**을 그려 줄 거리.
        /// 빙의 사거리가 아니다 — 갈아탈 수 없으므로 뺏는 데는 안 쓰인다.
        /// 호스트는 265 밖에서 쏘고 있어 유령 사거리(110)로는 표식이 영영 안 뜬다.
        /// </summary>
        private const float MarkShowRange = 460f;

        // ── 무적이 보이게 ────────────────────────────────────────
        //
        // ⚠ 예전에는 무적이면 `return` 하고 끝이라 **화면에 아무 일도 안 일어났다.**
        //   맞았는데 숫자가 안 뜨는 것과, 애초에 안 맞은 것이 구별되지 않았다 —
        //   그래서 무적이 걸려 있는지조차 알 수 없었다(기획 2026-09-15).
        //
        //   **아무 일도 안 일어나는 것 자체가 무적의 효과다**(기획 2026-09-15).
        //   숫자나 글자를 띄우지 않는다 — 대신 몸을 보면 알 수 있어야 한다.
        //   표현은 `Unit` 쪽이다: 몸이 비치고 **윤곽만 빛난다.**

        private void DamagePlayer(int amount)
        {
            if (IsInvulnerable) return;

            // ⚠ **유령은 적 공격을 받지 않는다.** 몸이 없는 동안 줄어드는 것은
            //   시계(자연 감소)와 몸을 잃고 놓아주는 값뿐이다.
            //   유령 구간에서 맞아 죽으면 "빼앗을 몸을 찾는 시간" 이 위험 회피 시간으로
            //   바뀌어, 이 게임이 묻는 질문(어느 몸을 언제 탈까)이 사라진다.
            //
            //   ⚠ 방벽(`AbsorbWithBarrier`)보다 **먼저** 빠져나간다. 뒤에 두면
            //     맞지도 않는 피해에 방벽이 닳아 없어진다.
            if (_host == null) return;

            // 회피 — 깎는 것이 아니라 **없던 일**이 된다(닌자(사슬) 패시브).
            // 동료·방벽보다 먼저 본다: 흘릴 피해에 동료가 맞거나 방벽이 닳으면 안 된다.
            if (Avatar != null && Avatar.DodgePercent > 0 && Roll(Avatar.DodgePercent))
            {
                SpawnFx("dash", Avatar.Position, HurtFxSize);   // 잔상 — 흘렸다는 표시
                return;
            }

            // 동료가 앞에 서 있으면 **동료가 대신 받는다** (`BattleDirector.Ally.cs`).
            // 무적·방벽보다 먼저 본다 — 뒤에 두면 동료를 사 놓고도 내 방벽이 먼저 닳는다.
            // 도발 중인 분신이 있으면 그쪽이 먼저 맞는다 — 방금 켠 것이 늘 서 있는 동료보다 우선이다.
            if (SoakWithSummon(amount)) return;
            if (SoakWithAlly(amount)) return;

            // 찰나의 불사 — 맞는 그 순간 2초를 산다. 방벽보다 **먼저** 본다:
            // 뒤에 두면 막아 낼 피해에 방벽이 먼저 닳는다.
            if (TryGuardInvuln()) return;

            // 받는 피해 감소(정본 BUF_A04). 0 이 되지 않게 최소 1 은 남긴다 —
            // 무적이 되어 버리면 버프가 아니라 버그로 보인다.
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * _buffs.DamageTakenMul));
            // 방어력(새 스탯 · 명세 2026-09-14). 카드 감소와 **곱해진다** — 더하면 두 겹에 무적이 된다.
            if (Avatar != null)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * Avatar.DamageTakenMul()));
            // 구루 수호 결계 — 카드 감소와 **곱해진다.** 더하면 −50% 두 장에 무적이 된다.
            if (WardReduce > 0f)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - WardReduce)));

            // C018 위기 방벽 — 남아 있으면 이쪽이 먼저 받는다. 다 막았으면 끝이다.
            amount = AbsorbWithBarrier(amount);
            if (amount <= 0) return;

            var hurtAt = Avatar != null ? Avatar.Position : Vector2.zero;
            ShowDamage(hurtAt, amount, toEnemy: false);
            SpawnFx("hurt", hurtAt, HurtFxSize);
            GameSound.HostHurt(_host.Key);
            Shake(ShakeOnPlayerHurt);
            if (_host.TakeDamage(amount)) LoseHost();
            else PublishHp();
        }

        /// <summary>보호 시간 동안 근처 적을 늦춘다. 범위는 빙의 사거리의 두 배로 잡는다.</summary>
        /// <summary>
        /// 몸을 잃거나 놓아준 자리의 보호 슬로우 반경.
        ///
        /// 예전에는 `PossessRange * 2` 였다. 빙의 사거리를 1.9m → 5m 로 넓히면서
        /// 이 값이 따라 커지면 방 전체가 슬로우에 걸린다 — 보호가 아니라 무적이 된다.
        /// 사거리와 무관한 값으로 떼어내고, 예전 반경(165×2)을 그대로 유지한다.
        /// </summary>
        private const float ProtectSlowRadius = 330f;

        private void SlowNearbyEnemies(Vector2 center)
        {
            float r = ProtectSlowRadius;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                if (Vector2.Distance(e.Position, center) > r) continue;
                e.ApplySlow(_config.ProtectSlowPercent, _config.GhostProtectSeconds);
            }
        }

        private void LoseHost()
        {
            var pos = _host.Position;
            var key = _host.Key;
            Retire(_host);       // 몸은 쓰러진다 — 유령이 그 자리에서 빠져나온다
            _host = null;

            // 쓰다 잃은 몸이 가장 많이 준다. 타 본 값이다.
            GrantShards(key, lost: true);

            // 기획서 1-2 A — 몸을 잃는 값(-20%)이 놓아주는 값(-15%)보다 커야
            // "죽기 전에 버리고 나온다" 가 선택지가 된다.
            int cost = Mathf.Max(1, GhostHpMax * _config.GhostDeathCostPercent / 100);
            _ghostHp = Mathf.Max(1, _ghostHp - cost);
            ShowGhostCost(pos, cost);

            _ghost.gameObject.SetActive(true);
            // 빙의 연출이 줄여 놓은 크기·그림을 되돌린다. 안 되돌리면 몸을 잃고
            // 유령으로 나올 때 **콩알만 한 채로** 남는다.
            _ghost.transform.localScale = Vector3.one;
            _ghost.SetSpriteOverride(null);
            _ghost.Position = pos;

            // 기획서 A 1-3 — 호스트를 잃은 자리는 적 한복판이다. 보호가 없으면
            // 다시 빙의할 틈 없이 연쇄로 죽는다. 무적과 함께 주변을 늦춘다.
            _ghostProtect = _config.GhostProtectSeconds;
            _drainCarry = 0f;
            _buffs.SetHost(null);
            SlowNearbyEnemies(pos);

            _bus.Publish(new HostLostEvent { LostHostKey = key });
            PublishHp();
        }

        /// <summary>
        /// 사격 대상 선택 (기획서 A 3-3 Target Type).
        /// 호스트마다 "누구를 먼저 때리는가"가 다르면 같은 화력도 다른 교전이 된다.
        /// 체력 기준으로 고를 때도 사거리 밖은 후보가 아니므로 거리를 함께 본다.
        /// </summary>
        private Unit Nearest(Vector2 from)
        {
            var rule = _host != null && _host.Profile != null
                ? _host.Profile.Targeting : TargetType.Nearest;

            Unit best = null;
            float bestD = float.MaxValue;
            int bestHp = rule == TargetType.LowestHp ? int.MaxValue : int.MinValue;

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e)) continue;
                // 화면 밖은 겨누지 않는다. 방이 화면보다 길어진 뒤로 안 보이는 적을 향해
                // 쏘는 일이 생겼다 — 플레이어에게는 허공에 대고 쏘는 것으로 보인다.
                if (!IsOnScreen(e)) continue;
                // 여기도 가장자리다. 중심으로 재면 **덩치 큰 놈이 항상 멀어 보여**,
                // 코앞의 보스를 두고 뒤쪽 잡몹을 겨눈다.
                float d = Vector2.Distance(from, e.Position) - BodyExcess(e);

                bool better;
                switch (rule)
                {
                    case TargetType.LowestHp:
                        better = e.Hp < bestHp || (e.Hp == bestHp && d < bestD);
                        break;
                    case TargetType.HighestHp:
                        better = e.Hp > bestHp || (e.Hp == bestHp && d < bestD);
                        break;
                    default:
                        better = d < bestD;
                        break;
                }
                if (!better) continue;
                best = e; bestD = d; bestHp = e.Hp;
            }
            return best;
        }

        // ── 파편 ─────────────────────────────────────────────
        //
        // **죽이면 1 · 빙의해 쓰다가 잃으면 3.**
        //
        // 이 차등이 설계의 핵심이다. "죽이면만 나온다" 가 되면 파밍이 빙의를 벌줘서
        // 플레이어가 이 게임의 핵심 재미를 스스로 피하게 된다.
        // 몸을 쓰다 잃는 쪽이 더 많이 줘야 "타 보고 배운다" 가 이득이 된다.
        // 값은 `PlayerDataService.ShardDrop` 이 등급에서 뽑는다 —
        // 일반 1/3 · 정예 2/6 · 희귀 10/30. 여기서 상수로 박지 않는다.

        /// <summary>
        /// 파편은 **그 호스트 것만** 쌓인다. 잡몹·보스는 빼앗을 몸이 아니므로 안 준다.
        /// 지급 창구를 하나로 두어 두 경로가 어긋나지 않게 한다.
        /// </summary>
        private void GrantShards(string hostKey, bool lost)
        {
            if (_player == null || string.IsNullOrEmpty(hostKey)) return;
            // 호스트 표에 없는 키(해골·박쥐 같은 잡몹)는 거른다.
            if (_player.GetHost(hostKey) == null) return;
            _player.AddShards(hostKey, _player.ShardDropFor(hostKey, lost));
        }

        private void KillEnemy(Unit u)
        {
            u.SetState(EnemyState.Dead);
            // ⚠ **목록에서 빼기 전에** 옮긴다. 뺀 뒤에 부르면 옆 사람을 찾는
            //   `EnemiesInRange` 가 이미 죽은 자리를 기준으로 도는 것은 같지만,
            //   전이 대상 후보에서 자기 자신을 빼려고 목록 조작에 기대게 된다.
            TransferMark(u);
            SpreadCurse(u);
            // C017 생명 회수 — 잡을 때마다 최대 체력의 몇 %를 돌려받는다
            if (_buffs.RegenPercentPerKill > 0 && _host != null)
                Leech(Mathf.Max(1, _host.HpMax * _buffs.RegenPercentPerKill / 100));
            if (u.IsBoss)
            {
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = u.HpMax });
                ClearBossVisuals();   // 예고 도형·화살표·파괴구·방패판을 한꺼번에 거둔다
                // 원작처럼 나던 소리를 전부 끊고 대폭발 한 번
                GameSound.StopEffects();
                GameSound.Cue("boss.down");
            }
            _enemies.Remove(u);
            if (u == _possessTarget) _possessTarget = null;
            Retire(u);

            // 잡은 순간을 몸으로 알린다. 보스는 크게 — 한 판의 매듭이다.
            Shake(u.IsBoss ? ShakeMaxPixels : ShakeOnKill);
            if (u.IsBoss) HitStop(HitStopOnBossKill);

            GainExp(u.IsBoss ? _config.ExpPerBoss : _config.ExpPerEnemy);
            // 보스는 빼앗을 몸이 아니다 — 파편도 안 나온다.
            if (!u.IsBoss) GrantShards(u.Key, lost: false);

            PassiveOnKill(u);
            DropGold(u);
        }

        // ── 바닥에 떨어지는 골드 ─────────────────────────────────
        //
        // 방 보상 골드를 **적 머릿수로 나눠** 죽은 자리마다 떨어뜨린다. 방을 다 비우면
        // 흩어져 있던 것이 한꺼번에 플레이어에게 빨려 들어온다.
        //
        // 방을 비운 순간 숫자만 올리면 어느 적이 얼마를 줬는지가 안 보이고,
        // 남은 놈을 마저 잡을 이유도 화면에 안 나온다. 바닥에 쌓여 있어야
        // "저기 아직 있다" 가 보인다.
        //
        // ⚠ **총액은 정본 그대로다**(`ROOM_REWARD.Gold`). 나누기만 하고 더 주지 않는다 —
        //   연출 때문에 밸런스가 움직이면 표를 못 믿게 된다.

        private readonly List<GoldPile> _goldPiles = new();
        private Sprite _goldSprite;
        private bool _goldCollecting;   // 방이 비어 빨려 들어가는 중
        private int _goldCollected;     // 이번에 모은 액수

        // ── 몹에 따라 다르게 떨군다 ─────────────────────────────
        //
        // 머릿수로 똑같이 나누면 **해골 한 마리와 엘리트 한 마리가 같은 값**이 된다.
        // 뚫고 들어가야 하는 것이 더 줘야 그 자리로 갈 이유가 생긴다.
        //
        // ⚠ 총액은 여전히 정본(`ROOM_REWARD.Gold`) 그대로다. 비율만 바꾼다 —
        //   연출 때문에 밸런스가 움직이면 표를 못 믿게 된다.
        //   남은 액수를 남은 가중치로 나눠 주므로 마지막 한 마리에서 딱 떨어진다.

        private int _goldPool;          // 아직 안 나눠 준 액수
        private int _goldWeightLeft;    // 아직 안 죽은 것들의 가중치 합

        /// <summary>이 몹이 가져가는 몫. 잡몹 1 · 빼앗을 몸 2 · 엘리트 4 · 보스 8.</summary>
        private static int GoldWeightOf(Unit u)
            => u == null ? 1
             : u.IsBoss ? 8
             : u.IsElite ? 4
             : u.IsHostBody ? 2
             : 1;

        /// <summary>몫이 클수록 여러 무더기로 흩어진다. 많이 떨군 것이 눈에도 많아야 한다.</summary>
        private static int GoldPilesFor(Unit u)
            => u == null ? 3
             : u.IsBoss ? 10
             : u.IsElite ? 6
             : u.IsHostBody ? 4
             : 3;

        /// <summary>바닥 골드 그림 크기(px).</summary>
        private const float GoldPileSize = 28f;

        /// <summary>
        /// 이 방의 골드를 **가중치 합**으로 나눌 준비를 한다. 방에 들어설 때 한 번 센다.
        /// 적이 없는 방이면 나눌 것도 없다 — 그때는 방을 비울 때 통째로 들어간다.
        /// </summary>
        private void PrepareGoldDrops()
        {
            _goldPool = 0;
            _goldWeightLeft = 0;
            _goldCollected = 0;
            _goldCollecting = false;

            int gold = _canonRoom != null ? _canonRoom.Gold : 0;
            if (gold <= 0 || _enemies.Count == 0) return;

            int weight = 0;
            for (int i = 0; i < _enemies.Count; i++) weight += GoldWeightOf(_enemies[i]);
            if (weight <= 0) return;

            _goldPool = gold;
            _goldWeightLeft = weight;
        }

        /// <summary>흩어지는 무더기 수의 흔들림. 매번 같은 수면 기계처럼 보인다.</summary>
        private const int GoldPileJitter = 1;

        private void DropGold(Unit u)
        {
            if (u == null || _goldPool <= 0 || _goldWeightLeft <= 0) return;

            int w = GoldWeightOf(u);
            // ⚠ **남은 것을 남은 가중치로 나눈다.** 처음에 한 번 나눠 두면 반올림이
            //   쌓여 마지막에 남거나 모자란다. 이렇게 하면 마지막 한 마리에서 딱 떨어진다.
            int amount = w >= _goldWeightLeft
                       ? _goldPool
                       : Mathf.Max(1, Mathf.RoundToInt((float)_goldPool * w / _goldWeightLeft));
            amount = Mathf.Min(amount, _goldPool);

            _goldPool -= amount;
            _goldWeightLeft -= w;
            if (amount <= 0) return;

            // ⚠ **한 마리에서 여러 무더기가 흩어진다.** 하나만 떨구면
            //   "떨어졌다" 가 아니라 "숫자가 하나 붙었다" 로 보인다.
            //   액수가 무더기 수보다 적으면 그 수만큼만 떨군다 — 0 짜리는 안 만든다.
            int want = GoldPilesFor(u) + _rng.Next(-GoldPileJitter, GoldPileJitter + 1);
            int piles = Mathf.Clamp(want, 1, amount);
            var me = Avatar;

            for (int i = 0; i < piles; i++)
            {
                // 나눈 나머지는 앞쪽 무더기에 한 닢씩 얹는다 — **총액은 그대로다.**
                int share = amount / piles + (i < amount % piles ? 1 : 0);
                var pile = RentGoldPile();
                if (pile == null) return;
                pile.Drop(u.Position, share);

                // ⚠ **떨어지는 즉시 나에게 날아온다** (2026-09-10).
                //   예전에는 방을 다 비운 뒤에야 한꺼번에 걷었다. 그러면 잡는 동안
                //   바닥에 동전만 쌓이고 「번 느낌」이 방 끝까지 미뤄진다 —
                //   보스방처럼 오래 싸우는 방에서는 특히 그렇다.
                //   한 마리 잡을 때마다 그 자리에서 동전이 날아와야 잡은 값이 읽힌다.
                if (me != null) pile.FlyTo(me.Position);
            }

            // 날아오는 중인 것이 있으면 `TickGoldPiles` 가 닿는 순간 판 골드에 넣는다.
            _goldCollecting = true;
        }

        private GoldPile RentGoldPile()
        {
            for (int i = 0; i < _goldPiles.Count; i++)
                if (!_goldPiles[i].IsActive) return _goldPiles[i];

            if (_fieldLayer == null || _goldPiles.Count >= MaxGoldPiles) return null;
            if (_goldSprite == null) _goldSprite = GetSprite("goldicon");
            if (_goldSprite == null) return null;

            var g = GoldPile.Create(_fieldLayer, _goldSprite,
                                    new Vector2(GoldPileSize, GoldPileSize));
            _goldPiles.Add(g);
            return g;
        }

        /// <summary>
        /// 한 방에 놓을 수 있는 무더기 수.
        /// 한 마리가 3~4개를 떨구고 방에 최대 9기가 서므로 40 이면 남는다.
        /// </summary>
        private const int MaxGoldPiles = 40;

        /// <summary>방이 비었다. 바닥에 남은 것을 전부 플레이어에게 보낸다.</summary>
        private void CollectGoldPiles()
        {
            var me = Avatar;
            if (me == null) { GiveCollectedGold(true); return; }

            bool any = false;
            for (int i = 0; i < _goldPiles.Count; i++)
            {
                if (!_goldPiles[i].IsActive) continue;
                _goldPiles[i].FlyTo(me.Position);
                any = true;
            }

            if (!any) { GiveCollectedGold(true); return; }
            _goldCollecting = true;
            _goldCollected = 0;
        }

        private void TickGoldPiles(float dt)
        {
            var me = Avatar;
            Vector2 at = me != null ? me.Position : Vector2.zero;

            bool anyLeft = false;
            for (int i = 0; i < _goldPiles.Count; i++)
            {
                var g = _goldPiles[i];
                if (!g.IsActive) continue;
                int amount = g.Amount;
                if (g.Tick(dt, at)) _goldCollected += amount;
                else anyLeft = true;
            }

            // 마지막 한 닢이 닿은 프레임에 한꺼번에 넣는다. 닿을 때마다 넣으면
            // HUD 동전이 여러 번 나뉘어 날아 산만해진다.
            if (_goldCollecting && !anyLeft) GiveCollectedGold(false);
        }

        /// <summary>모은 것을 판 골드에 넣는다. 여기서부터는 HUD 동전이 이어받는다.</summary>
        private void GiveCollectedGold(bool useRoomTotal)
        {
            _goldCollecting = false;
            int amount = useRoomTotal
                ? (_canonRoom != null ? _canonRoom.Gold : 0)   // 적이 없던 방
                : _goldCollected;

            // 마지막 한 마리를 **빼앗아서** 방이 비면 떨굴 몸이 남지 않는다.
            // 그때 통에 남은 것은 갈 곳이 없으므로 여기서 함께 넣는다 —
            // 방을 비웠으면 방 골드는 다 받는다는 규칙을 어느 길로 와도 지킨다.
            if (!useRoomTotal && _goldPool > 0) { amount += _goldPool; _goldPool = 0; }

            _goldCollected = 0;
            if (amount > 0) AddRunGoldAtPlayer(amount);
        }

        private void ClearGoldPiles()
        {
            for (int i = 0; i < _goldPiles.Count; i++) _goldPiles[i].Despawn();
            _goldCollecting = false;
            _goldCollected = 0;
        }

        // ── 런 레벨 ──────────────────────────────────────────────
        // 기획서 A 5-2 — EXP 가 차면 전투를 멈추고 버프 3택1 을 띄운다.
        // 방을 비워야 버프가 나오던 것과 달리, **잡는 만큼** 성장한다.
        // 방 하나가 곧 스테이지인 지금 구조에서는 이게 없으면 성장이 스테이지당 한 번뿐이다.

        private void GainExp(int amount)
        {
            if (amount <= 0) return;
            // 성장 가속 — 얻는 경험치 자체를 늘린다. 필요량을 깎지 않는 이유는,
            // 깎으면 이미 쌓인 경험치까지 소급돼 카드를 고른 순간 레벨이 튀기 때문이다.
            if (_buffs.ExpGainMul > 1f)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * _buffs.ExpGainMul));
            _exp += amount;

            int need = _config.ExpToNext(_level);
            if (_exp < need)
            {
                PublishExp(need);
                return;
            }

            _exp -= need;
            _level++;
            PublishExp(_config.ExpToNext(_level));

            // 바로 띄우지 않는다. 마지막 한 대를 때린 순간 팝업이 뜨면
            // **적이 죽기도 전에 먼저 뜬 것처럼** 보인다.
            // 방이 실제로 비고(쓰러지는 연출까지) 잠깐 뒤에 띄운다.
            _pendingLevelUps++;
        }

        /// <summary>
        /// 방이 비기를 기다렸다가 레벨업 3택1 을 띄운다. 1.0 → 0.5 → 0.1 로 줄였다.
        ///
        /// 이 뜸은 "적이 죽기도 전에 팝업이 먼저 뜬 것처럼" 보이지 않게 하려는 것이다.
        /// 쓰러지는 연출은 `_dying` 이 따로 붙잡고 있으므로(아래 `TickPendingLevelUp`),
        /// 여기서 더 기다릴 이유가 없다 — 기다린 만큼 그냥 멈춰 있는 시간이다.
        /// </summary>
        private const float BuffOfferDelay = 0.1f;

        private int _pendingLevelUps;
        private float _buffOfferTimer;

        private bool HasPendingLevelUp => _pendingLevelUps > 0;

        private void TickPendingLevelUp(float dt)
        {
            if (_pendingLevelUps <= 0 || _awaitingBuff) return;

            // 살아 있는 적도, 쓰러지는 중인 몸도 없어야 "다 죽었다" 이다.
            if (_enemies.Count > 0 || _dying.Count > 0 || IsChanneling)
            {
                _buffOfferTimer = BuffOfferDelay;
                return;
            }

            _buffOfferTimer -= dt;
            if (_buffOfferTimer > 0f) return;

            _pendingLevelUps--;
            _buffOfferTimer = BuffOfferDelay;
            OfferBuff();
        }

        private void PublishExp(int need)
            => _bus.Publish(new RunExpChangedEvent { Level = _level, Exp = _exp, ExpToNext = need });

        /// <summary>버프 3택1 을 연다. 이미 열려 있으면 아무것도 하지 않는다.</summary>
        private void OfferBuff()
        {
            if (_awaitingBuff) return;

            // 뽑을 수 없는 카드는 **5레벨을 다 찍은 것뿐**이다(`RunBuffs.Apply`).
            // 예전에는 그 위에 「서로 다른 카드 8종」 상한이 하나 더 있었는데,
            // 화면에 아무 표시가 없어 8종을 채운 순간 새 카드가 조용히 사라졌다.
            // 카드 종류가 적은 지금은 방해만 된다 — 종류가 크게 늘면 그때 다시 본다.
            _buffTable?.Draw(_offer, 3, _buffs.ExcludedKeys, _rng, _host?.Profile,
                             _runChapter);
            if (_offer.Count == 0) return;

            _awaitingBuff = true;
            var keys = new string[_offer.Count];
            for (int i = 0; i < _offer.Count; i++) keys[i] = _offer[i].BuffKey;
            _bus.Publish(new BuffOfferEvent { OfferedKeys = keys, Level = _level });
        }

        /// <summary>
        /// 몸을 화면에서 물린다. 사망 그림이 있으면 쓰러지는 연출을 돌리고,
        /// 없으면 예전처럼 바로 없앤다 — 캐릭터를 한 종씩 채워 넣는 중이라
        /// 그림이 없는 종이 멈춰 있으면 안 된다.
        /// </summary>
        private void Retire(Unit u)
        {
            if (u == null) return;

            // ⚠ **보스가 죽으면 위쪽 체력 게이지를 내린다.**
            //   게이지는 `BossHpMax == 0` 일 때만 숨는데(`InGameMainUI.OnBossHp`),
            //   죽는 자리에서 그 신호를 아무도 안 보내고 있었다 — 보스가 사라진 뒤에도
            //   0/1200 짜리 빈 게이지가 화면 위에 계속 남았다(기획 2026-09-03).
            //   `_boss` 참조도 여기서 놓는다. 죽은 몸을 계속 들고 있으면
            //   `TickBoss` 가 시체를 붙들고 패턴을 굴린다.
            if (u == _boss)
            {
                _boss = null;
                _bus.Publish(new BossHpChangedEvent { BossHp = 0, BossHpMax = 0 });
                // 벽 보스는 제 죽는 연출이 따로 있다 — 그냥 두면 선 채로 투명해진다.
                BeginPythonDeath(u);
            }

            // 벽 보스는 방향별 die 그림이 없다(있어도 옛 옆모습이라 안 쓴다).
            // 사라지는 시간만 받아 그동안 제 연출을 돈다.
            if (u.BeginDeath(fadeOnly: _pyDying == u)) _dying.Add(u);
            else Destroy(u.gameObject);
        }

        /// <summary>쓰러지는 중인 몸을 진행시키고, 다 사라진 것을 치운다.</summary>
        private void TickDying(float dt)
        {
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                var u = _dying[i];
                if (u == null) { _dying.RemoveAt(i); continue; }
                if (!u.TickDeath(dt)) continue;
                _dying.RemoveAt(i);
                Destroy(u.gameObject);
            }
        }

        // ── 피해 수치 ──────────────────────────────────────────────
        // 색은 "누가 맞았나"로 나눈다. 내가 때린 것과 내가 맞은 것이 같은 색이면
        // 화면이 숫자로 덮였을 때 상황 판단이 안 된다.
        private static readonly Color DamageToEnemy = new(1f, 0.95f, 0.75f, 1f);
        private static readonly Color DamageToPlayer = new(1f, 0.42f, 0.38f, 1f);
        // 유령 HP 색(#5AC8F0)과 같은 계열. 피해 숫자와 섞이면 안 된다 — 성격이 다른 값이다.
        private static readonly Color GhostCostColor = new(0.35f, 0.78f, 0.94f, 1f);

        private const int MaxDamageTexts = 24;

        private DamageText RentDamageText()
        {
            for (int i = 0; i < _damageTexts.Count; i++)
                if (!_damageTexts[i].IsActive) return _damageTexts[i];

            if (_damageTexts.Count >= MaxDamageTexts) return null;   // 폭주 방지 상한
            var go = new GameObject("DamageText", typeof(RectTransform));
            go.transform.SetParent(_textLayer, false);
            var t = go.AddComponent<DamageText>();
            t.Init(TMP_Settings.defaultFontAsset);
            _damageTexts.Add(t);
            return t;
        }

        /// <summary>맞은 자리에 피해 수치를 띄운다. 풀이 다 차면 조용히 넘어간다.</summary>
        private void ShowDamage(Vector2 at, int damage, bool toEnemy)
            => ShowDamage(at, damage, toEnemy, false);

        private void ShowDamage(Vector2 at, int damage, bool toEnemy, bool crit)
        {
            if (damage <= 0) return;
            var t = RentDamageText();
            if (t == null) return;
            t.Show(at, damage, crit ? CritDamageColor : toEnemy ? DamageToEnemy : DamageToPlayer, crit);
        }

        /// <summary>치명타 숫자 색. 평타(흰색)와 한눈에 갈려야 한다.</summary>
        private static readonly Color CritDamageColor = new(1f, 0.83f, 0.29f, 1f);

        // ── 치명타 ──────────────────────────────────────────────
        //
        // ⚠ 확률 효과 5종(흡혈·스턴·약화·반사·쉴드)과 **다른 층**이다.
        //   그쪽은 패시브 스킬이고 이쪽은 스탯이라, 몸을 바꾸면 값이 통째로 바뀐다.

        /// <summary>지금 몸의 치명타 확률(%). 고스트 Lv 이 얹힌 값이다.</summary>
        private float CritPercent =>
            HostStats.CritPercent(_config, _host?.Profile, GhostLevel,
                                  _player != null ? _player.GhostLevelMax : 50);

        private float CritMultiplier => _config != null ? _config.CritMultiplier : 2f;

        private bool RollCrit()
        {
            // 호퍼 액티브가 도는 동안은 스탯을 보지 않는다 — 확률이 **고정**이다(명세).
            float p = _critLockSeconds > 0f ? _critLockPercent : CritPercent;
            return p > 0f && _rng.NextDouble() * 100.0 < p;
        }

        /// <summary>
        /// 회복한 자리에 <c>+N</c> 을 띄운다.
        ///
        /// ⚠ 로그만으로는 **화면에서 회복이 안 보인다.** 보스는 맞는 중이라
        ///   체력바가 계속 줄어서, 회복한 것이 그 감소분에 묻힌다 —
        ///   숫자가 떠야 "물어서 채웠다" 가 읽힌다(기획 2026-09-03).
        /// </summary>
        /// <summary>
        /// 피흡이 뜨는 자리 — 몸 **오른쪽 위로 비켜** 세운다.
        ///
        /// 물린 자리와 문 자리는 붙어 있어서, 같은 점에 띄우면 회복 숫자가
        /// 피해 숫자 위에 그대로 얹힌다(기획 2026-09-07 — "피흡 숫자 작게 빼줘").
        /// </summary>
        private static readonly Vector2 HealTextOffset = new(46f, 34f);

        private void ShowHeal(Vector2 at, int amount)
        {
            if (amount <= 0) return;
            var t = RentDamageText();
            if (t == null) return;
            t.ShowMinor(at + HealTextOffset, $"+{amount}", HealColor);
        }

        /// <summary>전술 빙의로 나간 Ghost HP. 유령 색으로 띄워 피해 숫자와 구분한다.</summary>
        private void ShowGhostCost(Vector2 at, int cost)
        {
            if (cost <= 0) return;
            var t = RentDamageText();
            if (t == null) return;
            t.Show(at, $"-{cost}", GhostCostColor);
        }

        private void TickDamageTexts(float dt)
        {
            for (int i = 0; i < _damageTexts.Count; i++) _damageTexts[i].Tick(dt);
        }

        private void CleanupDead()
        {
            _dead.Clear();
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] == null || !_enemies[i].IsAlive) _dead.Add(_enemies[i]);
            for (int i = 0; i < _dead.Count; i++)
            {
                _enemies.Remove(_dead[i]);
                if (_dead[i] != null) Retire(_dead[i]);
            }
            _dead.Clear();
        }

        /// <summary>
        /// 빙의 가능 대상 갱신. 유령일 때와 몸을 입고 있을 때 **둘 다** 의미가 있다.
        ///
        /// 유령이면 공짜다 — 몸이 없으니 다른 선택지가 없다.
        /// 몸이 있으면 전술 빙의다 — Ghost HP 를 내고 살아 있는 몸을 버린다.
        /// 기준점도 다르다. 유령은 유령 자리에서, 호스트는 호스트 자리에서 잰다.
        /// </summary>
        private void RefreshPossessTarget()
        {
            _possessTarget = null;
            var from = Avatar;
            // 기획서 1-7 — 몸을 타고 있는 동안에는 다른 몸을 노리지 않는다.
            // 그때 이 버튼은 탈출이고, 대상 표식도 뜨면 안 된다.
            if (from != null && !_awaitingBuff && _host == null)
            {
                // 기획서 A 4-3 — 우선순위가 높은 적을 먼저 잡는다. 같으면 가까운 쪽.
                // 사거리는 적마다 다를 수 있다(PossessRange 0 이면 전역 기본값).
                int bestPri = int.MinValue;
                float bestD = float.MaxValue;
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsPossessable) continue;

                    // 몸을 입은 채로는 갈아탈 수 없다(전술 빙의 폐기). 여기 오는 것은
                    // 언제나 유령이므로 사거리도 하나뿐이다.
                    float range = (e.PossessRange > 0f ? e.PossessRange
                                : _config.PossessRange) * PossessReachMul;
                    float d = Vector2.Distance(from.Position, e.Position);
                    if (d > range) continue;

                    if (e.PossessPriority < bestPri) continue;
                    if (e.PossessPriority == bestPri && d >= bestD) continue;

                    bestPri = e.PossessPriority; bestD = d; _possessTarget = e;
                }
            }
            RefreshPossessMarks(from);

            // 몸이 있으면 버튼은 언제나 누를 수 있는 **탈출**이다. 값(-15%)을 함께 적는다.
            bool has = _host != null || _possessTarget != null;
            int cost = _host != null
                ? Mathf.Max(1, GhostHpMax * _config.GhostLeaveCostPercent / 100) : 0;
            // 놓아준 직후의 짧은 잠금 동안에는 대상이 있어도 못 누른다.
            bool blocked = _host == null && _repossessLock > 0f;
            if (has == _hadPossessTarget && cost == _hadPossessCost
                && blocked == _hadPossessBlocked) return;

            _hadPossessTarget = has;
            _hadPossessCost = cost;
            _hadPossessBlocked = blocked;
            _bus.Publish(new PossessTargetChangedEvent
            {
                HasTarget = has, GhostCost = cost, Blocked = blocked,
            });
        }

        // ── 빙의 표식 (기획서 1-5) ────────────────────────────────
        //
        // 표식은 **사거리 안 후보에게만**, 가까운 순으로 최대 5개까지 뜬다.
        // 방 하나에 열 마리가 서 있는데 전부 조준 링을 달면 표식이 적을 덮어
        // "누구를 뺏을까"가 아니라 "누가 누구지"가 된다.

        /// <summary>한 번에 띄우는 표식 수 (기획서 1-5 B).</summary>
        private const int MaxPossessMarks = 5;

        /// <summary>고스트일 때 표식 배율 (기획서 1-5 B · 120%).</summary>
        private const float GhostMarkScale = 1.2f;

        private readonly Unit[] _markSlot = new Unit[MaxPossessMarks];
        private readonly float[] _markDist = new float[MaxPossessMarks];
        private int _markCount;

        private void RefreshPossessMarks(Unit from)
        {
            _markCount = 0;
            if (from != null && !_awaitingBuff) CollectMarks(from);
            RefreshPossessArrows(from);

            // 고스트일 때 크게. 몸이 없을 때가 "어디로 들어갈까"를 고르는 시간이다.
            float scale = _host == null ? GhostMarkScale : 1f;

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;

                if (!IsMarked(e)) { e.SetPossessMark(Unit.PossessMark.None); continue; }

                if (e == _possessTarget)
                    e.SetPossessMark(Unit.PossessMark.Target, _markTarget, 1f, scale);
                // 정본이 "네 다음 몸" 으로 찍어 둔 적(`POSSESSION_TARGET`). 아직 못 타더라도
                // **죽이지 말고 남겨 두라**는 뜻이라, 잠금 표식과 다른 것을 달아야 한다.
                else if (e.IsNextBody && !e.RepossessBanned && _markNextBody1 != null)
                    e.SetPossessMark(Unit.PossessMark.Locked,
                        Mathf.Repeat(Time.time, NextBodyBlink * 2f) < NextBodyBlink
                            ? _markNextBody1 : _markNextBody2,
                        e.IsPossessable ? 1f : e.PossessProgress, scale);
                else if (e.RepossessBanned)
                    e.SetPossessMark(Unit.PossessMark.Banned, _markBanned, 1f, scale);
                else if (!e.IsPossessable)
                    e.SetPossessMark(Unit.PossessMark.Locked, _markLocked, e.PossessProgress, scale);
                else
                    e.SetPossessMark(Unit.PossessMark.Ready, _markReady, 1f, scale);
            }
        }

        private void CollectMarks(Unit from)
        {
            // 조준 중인 몸은 거리와 무관하게 자리를 차지한다 — 우선순위 규칙(A 4-3)으로
            // 뽑힌 대상이 여섯 번째로 가까웠다는 이유로 금색 링이 사라지면 거짓말이 된다.
            if (_possessTarget != null) InsertMark(_possessTarget, -1f);

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || e == _possessTarget) continue;
                if (!e.IsAlive || e.IsDying || e.IsBoss) continue;

                // 뺏을 수 있거나, 뺏었다가 버렸거나, 조건이 안 찼거나 — 셋 다 알려줄 값이 있다.
                // 그 밖(보스·빙의 불가 종류)은 표식을 달아 봐야 화면만 시끄럽다.
                if (!e.IsPossessable && !e.RepossessBanned && !e.HasPossessCondition) continue;

                // 몸을 입고 있는 동안에는 다른 몸을 노리지 않는다(1-7). 그래도 조건부 적의
                // 잠금 표식은 남긴다 — 지금 두들기는 놈이 언제 열리는지가 다음 수다.
                if (_host != null && !e.HasPossessCondition && !e.IsNextBody) continue;

                // 몸을 입은 동안에는 **잠금 표식만** 그린다 — 갈아탈 수는 없다.
                // 이때는 교전 거리에서 보여야 "저놈이 언제 열리는지" 가 다음 수가 된다.
                float range = (_host != null ? MarkShowRange
                            : e.PossessRange > 0f ? e.PossessRange
                            : _config.PossessRange) * PossessReachMul;
                float d = Vector2.Distance(from.Position, e.Position);
                if (d > range) continue;          // 사거리를 벗어나면 아이콘이 사라진다

                InsertMark(e, d);
            }
        }

        /// <summary>가까운 순으로 끼워 넣는다. 뒤로 밀려 5개를 넘으면 버린다.</summary>
        private void InsertMark(Unit e, float d)
        {
            int at = _markCount;
            while (at > 0 && _markDist[at - 1] > d) at--;
            if (at >= MaxPossessMarks) return;

            for (int i = Mathf.Min(_markCount, MaxPossessMarks - 1); i > at; i--)
            {
                _markSlot[i] = _markSlot[i - 1];
                _markDist[i] = _markDist[i - 1];
            }
            _markSlot[at] = e;
            _markDist[at] = d;
            if (_markCount < MaxPossessMarks) _markCount++;
        }

        private bool IsMarked(Unit e)
        {
            for (int i = 0; i < _markCount; i++)
                if (_markSlot[i] == e) return true;
            return false;
        }

        // ── 화면 밖 후보 화살표 (기획서 1-5 B) ────────────────────
        //
        // 방이 화면보다 길어서(14 m 방 · 9.3 m 창) 뺏을 몸이 창 밖에 있을 수 있다.
        // 고스트는 초당 3씩 깎이는 중이라 "어디로 가야 몸이 있는가" 를 모르면
        // 그 시간이 그대로 손해다. 창 가장자리에 방향만 찍어 준다.
        //
        // 세로로만 스크롤하므로 화살표는 위·아래 둘뿐이다. 방향마다 **가장 가까운
        // 한 기**만 가리킨다 — 여럿 띄우면 가장자리가 화살표 띠가 된다.

        private RectTransform _arrowLayer;
        private Image _arrowUp, _arrowDown;

        /// <summary>화살표를 창 가장자리에서 얼마나 안쪽에 둘 것인가.</summary>
        private const float ArrowEdgeInset = 30f;

        /// <summary>창 아래쪽을 조작 버튼이 덮는 높이. 인게임 UI 가 넘겨준다(`SetControlBand`).</summary>
        private float _controlBandHeight;

        /// <summary>
        /// 조작 버튼(D패드 · 빙의 버튼) 띠의 높이를 받는다. 아래 빙의 화살표를 그 위에 띄운다.
        /// </summary>
        public void SetControlBand(float height) => _controlBandHeight = Mathf.Max(0f, height);

        private void RefreshPossessArrows(Unit from)
        {
            Unit up = null, down = null;
            float bestUp = float.MaxValue, bestDown = float.MaxValue;

            // 몸을 입고 있으면 몸을 찾을 이유가 없다. 표식과 같은 규칙(1-7)이다.
            if (from != null && _host == null && !_awaitingBuff && _running)
            {
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsPossessable) continue;
                    if (IsOnScreen(e)) continue;

                    float d = Vector2.Distance(from.Position, e.Position);
                    bool above = RoomToView(e.Position).y > 0f;   // 창 위쪽으로 벗어났다
                    if (above) { if (d < bestUp) { bestUp = d; up = e; } }
                    else if (d < bestDown) { bestDown = d; down = e; }
                }
            }

            SetArrow(ref _arrowUp, up, true);
            SetArrow(ref _arrowDown, down, false);
        }

        private void SetArrow(ref Image view, Unit at, bool up)
        {
            if (at == null)
            {
                if (view != null && view.gameObject.activeSelf) view.gameObject.SetActive(false);
                return;
            }
            if (view == null) view = NewArrow(up);
            if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);

            // 가로는 대상이 있는 쪽, 세로는 창의 위 끝 · **조작 버튼 띠 바로 위**.
            //
            // ⚠ 2026-09-11 에 필드가 화면 끝까지 내려가 창의 아래 끝이 엄지 밑이 됐다.
            //   거기 두면 아래 화살표가 D패드 밑에 깔려 안 보인다(실제로 16:9 에서 그랬다).
            float w = _field.rect.width, h = _field.rect.height;
            // 가로도 창 좌표로 — 줌을 당기면 좌우로도 스크롤된다.
            float x = Mathf.Clamp(RoomToView(at.Position).x, ArrowEdgeInset, w - ArrowEdgeInset);
            ((RectTransform)view.transform).anchoredPosition =
                new Vector2(x, up ? -ArrowEdgeInset : -(h - _controlBandHeight - ArrowEdgeInset));
        }

        private Image NewArrow(bool up)
        {
            if (_arrowLayer == null)
            {
                // 창에 붙는다 — 방(UnitLayer)에 붙이면 스크롤을 따라 같이 흘러가 버린다.
                var layer = new GameObject("PossessArrows", typeof(RectTransform));
                _arrowLayer = (RectTransform)layer.transform;
                _arrowLayer.SetParent(_field, false);
                _arrowLayer.anchorMin = _arrowLayer.anchorMax = new Vector2(0f, 1f);
                _arrowLayer.pivot = new Vector2(0f, 1f);
                _arrowLayer.anchoredPosition = Vector2.zero;
                _arrowLayer.sizeDelta = _field.rect.size;
            }

            var go = new GameObject(up ? "ArrowUp" : "ArrowDown",
                                    typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_arrowLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(32f, 32f);
            // 그림은 위를 향한 한 장뿐이다. 아래쪽은 뒤집어 쓴다.
            if (!up) rt.localRotation = Quaternion.Euler(0f, 0f, 180f);

            var img = go.GetComponent<Image>();
            img.sprite = _markArrow;
            img.raycastTarget = false;
            img.preserveAspect = true;
            if (_markArrow == null) img.color = new Color(0.37f, 0.78f, 1f, 0.95f);
            return img;
        }

        /// <summary>
        /// 지금 전술 빙의를 낼 수 있는가 (정본 TC_POS_D 의 선행 조건).
        /// 대상 유무는 보지 않는다 — 그건 부르는 쪽이 따로 본다.
        /// </summary>
        /// <summary>
        /// 복제된 공격의 피해 비율. 정본 C032 는 35~55% 다 —
        /// 온전한 한 방이 공짜로 더 나가면 8타마다 화력이 두 배가 된다.
        /// </summary>
        private float EchoMul => _echoing ? Mathf.Max(0.05f, _buffs.EchoPercent) : 1f;

        /// <summary>
        /// 갱스터가 때린 적에 표식을 남긴다. 갱스터의 유지 훅이 "표식 릴레이" 인 것과
        /// 같은 뿌리다 — 이 몸이 세상에 남기는 흔적이 곧 다음 몸의 재료가 된다.
        /// </summary>
        /// <summary>
        /// 흡혈. 정본 BUF_T04 피의 부채는 **넘치는 만큼을 고스트 체력으로** 돌린다.
        /// 방마다 횟수를 막는 이유는 정본 그대로다 — 안 막으면 잡몹 많은 방에서
        /// 고스트가 무한정 회복되어 유령 시계의 압박이 사라진다.
        /// </summary>
        /// <summary>회복 숫자. 피해와 헷갈리지 않게 초록으로 띄운다.</summary>
        private static readonly Color HealColor = new(0.45f, 1f, 0.55f, 1f);

        private void Leech(int amount)
        {
            if (_host == null || amount <= 0) return;
            int room = _host.HpMax - _host.Hp;
            _host.Heal(amount);

            // 확률로만 터지므로 **터진 것이 보여야** 한다. 안 보이면 그냥 안 되는 것과 같다.
            SpawnFx("leech", _host.Position, StunFxSize);

            // 체력은 실제로 올라가는데 상단 체력바가 안 움직여서 "흡혈이 안 된다" 로 보였다.
            // Unit.Heal 은 발밑 막대만 고친다. HUD 는 이 이벤트로만 갱신된다.
            PublishHp();

            // 한 방에 도는 양이 두어 점이라 체력바만으로는 눈에 안 띈다. 숫자로 띄운다.
            ShowHeal(_host.Position, Mathf.Min(amount, room));

            int over = amount - room;
            if (over <= 0) return;
            if (_buffs.BloodDebtPerRoom <= 0 || _bloodDebtUsed >= _buffs.BloodDebtPerRoom) return;
            _bloodDebtUsed++;
            _ghostHp = Mathf.Min(_config.GhostHpMax, _ghostHp + over);
            PublishHp();
        }

        private int _bloodDebtUsed;

        // ─────────────────────────────────────────────────────────
        /// <summary>
        /// 몸을 빼앗는다. 두 갈래다.
        ///
        /// **유령일 때** — 공짜다. 몸이 없으니 다른 선택지가 없고, 여기에 값을 매기면
        /// 죽은 뒤에 벌을 두 번 주는 셈이 된다.
        ///
        /// **몸을 입고 있을 때** — 갈아탈 수 없다. 그 버튼은 **탈출**이다.
        /// 전술 교체는 폐기했다 — 몸을 고르는 판단이 "지금 갈아탈까" 로 바뀌면
        /// 뺏은 몸을 끝까지 쓰는 맛이 사라진다.
        /// </summary>
        // ── 빙의 연출 ────────────────────────────────────────────
        //
        // 정본 `PossessionChannel` 0.35 초. 원작도 영혼이 **작아지면서 몸으로 빨려 들어간다**.
        // 즉시 갈아타면 몸을 빼앗았다는 감각이 없다 — 화면이 그냥 바뀔 뿐이다.

        private float _channel;                 // 남은 시간
        private float _channelTotal;
        private Vector2 _channelFrom, _channelTo;
        private Game.Character.HostEntry _channelEntry;
        private string _channelKey, _channelName;

        /// <summary>
        /// 빼앗기는 중인 몸. 채널이 끝날 때까지 **지우지 않고** 그 자리에 세워 둔다 —
        /// 지워 버리면 영혼이 빈 바닥으로 빨려 들어가는 그림이 되고,
        /// 몸이 저항하는 자세를 보여 줄 자리가 없다.
        /// </summary>
        private Unit _channelBody;

        /// <summary>빙의가 들어가는 중인가. 이 동안은 조작을 받지 않는다.</summary>
        public bool IsChanneling => _channel > 0f;

        /// <summary>축소 그림이 없을 때 스케일로 줄이는 끝값.</summary>
        private const float ShrinkEnd = 0.15f;

        /// <summary>몸에 닿는 시점(진행도). 이 뒤는 제자리에서 빨려 들어가는 시간이다.</summary>
        private const float SuckStart = 0.85f;

        /// <summary>
        /// 지금 진행도에 맞는 영혼 축소 그림. 아직 안 들어왔으면 null —
        /// 그때는 부르는 쪽이 스케일로 줄인다.
        /// </summary>
        private Sprite ShrinkFrame(float t)
            => UnitGet("ghost", t < 0.45f ? "shrink1" : t < 0.75f ? "shrink2" : "shrink3");

        /// <summary>
        /// 빼앗기는 몸의 자세. 원작은 **정면 2 장**만 그려 두었다 — 방향이 없다.
        /// 몸을 빼앗기는 순간에는 어느 쪽을 보고 있었는지가 중요하지 않고,
        /// 정면으로 팔을 벌린 그 자세 자체가 신호다.
        /// </summary>
        private Sprite PossessedFrame(string key, float t)
            => UnitGet(key, (int)(t / PossessFlipSeconds) % 2 == 0 ? "possess1" : "possess2");

        /// <summary>두 장을 번갈아 넘기는 간격. 원작처럼 빠르게 떤다.</summary>
        private const float PossessFlipSeconds = 0.09f;

        private void TickPossessChannel(float dt)
        {
            if (_channel <= 0f) return;

            _channel -= dt;
            float t = _channelTotal <= 0f ? 1f : 1f - Mathf.Clamp01(_channel / _channelTotal);

            if (_ghost != null)
            {
                // 몸에는 **일찍** 닿는다(85%). 나머지 15% 는 제자리에서 쏙 빨려 들어가는 시간이다 —
                // 도착과 사라짐이 같은 순간이면 "들어갔다"가 아니라 "없어졌다"로 보인다.
                float travel = Mathf.Clamp01(t / SuckStart);
                _ghost.Position = Vector2.Lerp(_channelFrom, _channelTo, travel * travel);

                // 축소 그림(3장)이 크기를 담고 있으므로 스케일은 건드리지 않는다.
                // 그림이 아직 없을 때만 스케일로 줄인다 — 픽셀이 뭉개지지만 없는 것보다 낫다.
                float scale = 1f;
                var frame = ShrinkFrame(t);
                if (frame != null) _ghost.SetSpriteOverride(frame);
                else scale = Mathf.Lerp(1f, ShrinkEnd, Mathf.SmoothStep(0f, 1f, t));

                // 마지막 한 순간 — 몸 속으로 쏙
                if (t > SuckStart)
                    scale = Mathf.Lerp(scale, 0.02f, (t - SuckStart) / (1f - SuckStart));

                _ghost.transform.localScale = Vector3.one * scale;
            }

            // 빼앗기는 몸이 정면으로 팔을 벌린 채 떤다. 그림이 없으면 그냥 서 있는다.
            if (_channelBody != null)
            {
                var pose = PossessedFrame(_channelBody.Key, _channelTotal - _channel);
                if (pose != null) _channelBody.SetSpriteOverride(pose);
            }

            if (_channel > 0f) return;

            _channel = 0f;
            if (_ghost != null)
            {
                _ghost.transform.localScale = Vector3.one;
                _ghost.SetSpriteOverride(null);
            }

            // 이제야 옛 몸을 치운다. 그 자리에 내 몸이 선다.
            if (_channelBody != null) Destroy(_channelBody.gameObject);
            _channelBody = null;

            EnterHost(_channelEntry, _channelKey, _channelName, _channelTo,
                      _config.HostStartHpPercent);
            _channelEntry = null;
        }

        /// <summary>
        /// 몸을 스스로 놓아준다 (기획서 1-1 A · 자발적 탈출).
        ///
        /// 놓아준 몸은 **쓰러지지 않는다.** 지금 체력·상태 그대로 적으로 돌아가
        /// 곧바로 나를 공격한다. 그래야 "버리고 도망친다"가 대가를 갖는다.
        /// 그리고 그 몸은 이 방에서 다시 탈 수 없다 — 한 몸을 무한히 재활용하면
        /// 탈출 비용이 무의미해진다.
        /// </summary>
        private void LeaveHost()
        {
            var body = _host;
            if (body == null) return;

            var pos = body.Position;
            var key = body.Key;

            int cost = Mathf.Max(1, GhostHpMax * _config.GhostLeaveCostPercent / 100);
            _ghostHp = Mathf.Max(1, _ghostHp - cost);   // 탈출로 소멸하지는 않는다
            ShowGhostCost(pos, cost);

            _host = null;
            body.BecomeEnemy();
            body.BanRepossess();
            body.SetPossessMark(Unit.PossessMark.None);
            _enemies.Add(body);

            _ghost.gameObject.SetActive(true);
            _ghost.transform.localScale = Vector3.one;
            _ghost.SetSpriteOverride(null);
            _ghost.Position = pos;

            // 놓아준 자리도 적 한복판이다. 몸을 잃었을 때와 같은 보호를 준다 —
            // 없으면 탈출이 곧 자살이라 이 선택지가 죽는다.
            _ghostProtect = _config.GhostProtectSeconds;
            _drainCarry = 0f;
            _buffs.SetHost(null);
            SlowNearbyEnemies(pos);

            // 놓아주자마자 옆 몸으로 갈아타면 탈출이 그냥 순간이동이 된다.
            // 짧게 잠근다 — 빙의 버튼의 덮개가 차오르는 동안이 그 시간이다.
            _repossessLock = _config.RepossessLockSeconds;
            _repossessLockShown = -1;
            _bus.Publish(new RepossessLockEvent
            {
                Remain = _repossessLock, Total = _config.RepossessLockSeconds,
            });

            _bus.Publish(new HostLostEvent { LostHostKey = key });
            PublishHp();
        }

        public void TryPossess()
        {
            if (!_running || _awaitingBuff || IsChanneling) return;

            // 기획서 1-7 — 빙의 중에는 다른 몸으로 갈아탈 수 없다.
            // 몸이 있을 때 이 버튼은 **탈출**이다.
            if (_host != null) { LeaveHost(); return; }

            if (_possessTarget == null || _repossessLock > 0f) return;

            var target = _possessTarget;
            var entry = _player.GetHost(target.Key);
            var pos = target.Position;

            // ⚠ 영혼이 출발하는 자리는 **지금 내가 서 있는 자리**다.
            //   `_ghost` 는 몸을 탄 동안 꺼져 있어 좌표가 마지막으로 유령이었던 곳
            //   (방에 들어온 자리) 에 멈춰 있다. 그걸 그대로 쓰면 몸을 갈아탈 때마다
            //   영혼이 방 입구 바닥에서 날아온다.
            var from = Avatar != null ? Avatar.Position : _ghost.Position;

            // 적 목록에서만 빼고 **지우지는 않는다.** 채널이 도는 동안 그 자리에서
            // 빼앗기는 자세로 굳어 있어야 한다. 총알·AI 는 목록을 보므로 더는 안 건드린다.
            _enemies.Remove(target);
            // ⚠ **몫만 덜어 낸다 — 돈은 안 준다.** 이 몸은 죽은 게 아니라 빼앗긴 것이라
            //   `DropGold` 를 안 탄다. 그런데 몫(가중치)은 방에 들어설 때 이미 세어 놨다.
            //   빼 주지 않으면 남은 것들이 자기 몫만 떨구고 끝나, 빼앗은 몸이 들고 있던
            //   만큼이 방 바닥에 나오지도 않고 사라진다.
            //   실제로 24 골드짜리 첫 방에서 15 밖에 못 걷었다 — 몸을 뺏을수록 손해였다.
            //   방 골드는 **방의 몫**이지 한 마리의 몫이 아니다.
            _goldWeightLeft = Mathf.Max(0, _goldWeightLeft - GoldWeightOf(target));
            target.CancelWindup();
            target.HoldPossessed(true);
            _channelBody = target;
            _possessTarget = null;

            // 여기 도달했다는 것은 내가 지금 영혼이라는 뜻이다 —
            // 몸이 있으면 위에서 탈출로 갈라져 나갔다.
            _ghost.gameObject.SetActive(true);
            _ghost.transform.localScale = Vector3.one;
            _ghost.Position = from;      // 켜기 전에 옛 좌표를 버린다
            _ghost.SetFacing(pos - from);

            _channelFrom = from;
            _channelTo = pos;
            _channelEntry = entry;
            _channelKey = target.Key;
            _channelName = target.DisplayName;
            _channelTotal = _channel = _config.PossessChannelSeconds;

            // 채널 시간이 0 이면 예전처럼 즉시 들어간다
            if (_channel <= 0f) TickPossessChannel(0f);
        }

        /// <summary>
        /// 몸을 입는다. 일반 빙의와 긴급 호스트가 같은 길을 쓴다 —
        /// 시작 체력만 다르고 나머지(무적·버프 재계산·표시)는 똑같아야 한다.
        /// </summary>
        /// <summary>
        /// 이 판에서 마지막으로 입었던 몸. 긴급 호스트가 돌아갈 자리다.
        /// 판이 끝날 때까지 남는다 — 몸을 잃어도 지워지지 않는다.
        /// </summary>
        private HostEntry _lastHostEntry;

        // == 악마 계약 - 최대 체력 빚 =================================
        //
        // 계약은 지금 아픈 것이 아니라 **앞으로 빼앗을 모든 몸**을 작게 만든다.
        // 그래서 몸에 붙이지 않고 판에 붙인다 - 몸을 갈아타도 따라온다.
        //
        // 주의: 하한이 없으면 계약을 살수록 몸이 종잇장이 되어 판이 끝난다.
        //   원본의 40% 를 바닥으로 두고, 그 아래로 내려가는 계약은 `CanAfford` 가
        //   아예 못 고르게 막는다 - 목록에서 빼는 것과 같은 효과다.

        /// <summary>최대 체력 빚의 상한(%). 원본의 40% 가 바닥이므로 60 이 상한이다.</summary>
        private const int MaxHpDebtCap = 60;

        /// <summary>지금까지 판 계약의 합(%). 판 한정 - 로비로 나가면 사라진다.</summary>
        private int _maxHpDebt;

        /// <summary>빚을 더한다. 상한을 넘지 않는다.</summary>
        private void AddMaxHpDebt(int percent)
        {
            _maxHpDebt = Mathf.Clamp(_maxHpDebt + Mathf.Max(0, percent), 0, MaxHpDebtCap);
            // 이미 타고 있는 몸에도 그 자리에서 적용한다. 다음 몸까지 기다리게 하면
            // 「계약했는데 아무 일도 안 일어났다」가 되고, 그건 대가로 안 읽힌다.
            if (_host != null)
            {
                int want = Mathf.Max(1, Mathf.RoundToInt(_host.HpMax * (1f - percent / 100f)));
                _host.SetHpMax(want);
                PublishHp();
            }
        }

        /// <summary>빚을 뺀 최대 체력. 새 몸을 세울 때마다 여기를 지난다.</summary>
        private int WithMaxHpDebt(int hpMax)
            => Mathf.Max(1, Mathf.RoundToInt(hpMax * (1f - _maxHpDebt / 100f)));

        private void EnterHost(HostEntry entry, string key, string fallbackName,
                               Vector2 pos, int startHpPercent)
        {
            if (entry != null) _lastHostEntry = entry;
            _dashTime = 0f;   // 몸이 바뀌면 돌진도 끊는다
            _ghost.gameObject.SetActive(false);

            _host = NewUnit($"Host_{key}");
            _host.Setup(UnitSide.Player, key, entry != null ? entry.DisplayName : fallbackName,
                        UnitGet(key),
                        // 고스트가 들고 온 Lv 로 이 몸의 능력치를 정한다.
                        // 악마 계약을 샀으면 여기서 깎인다 - 몸이 아니라 판에 붙은 빚이다.
                        WithMaxHpDebt(Mathf.RoundToInt(LeveledHp(entry) * _buffs.HostHpMul)),
                        LeveledAtk(entry),
                        HostSpeedOf(entry),
                        HostRangeOf(entry),
                        HostIntervalOf(entry),
                        UnitBox(96f, 92f), isBoss: false, profile: entry);
            _host.Position = pos;
            ApplyFacingSprites(_host, key);


            // 기획서 A 3-3 — 빼앗은 몸은 온전하지 않다. 최대 체력의 70%로 시작한다.
            // 이게 없으면 교체가 곧 완전 회복이라, 몸을 갈아타는 데 대가가 없어진다.
            _host.SetHpPercent(startHpPercent);

            // 기획서 A 02 — 빙의 직후 무적(0.35) + 호스트 진입 무적(0.5). 몸을 얻는 순간이
            // 가장 취약한 지점이라, 여기서 맞으면 빙의 자체가 손해가 된다.
            _invuln = _config.PossessInvulnSeconds + _buffs.SwitchShieldSeconds;
            _ghostProtect = 0f;
            _emergencyWait = 0f;
            // 태그형·전용 버프는 쓰는 몸에 따라 켜지고 꺼진다 (기획서 A 5-4)
            _buffs.SetHost(entry);
            // 몸에 붙는 패시브(회피 · 방어력 · 이속 배수) — 명세 2026-09-14
            ApplyHostPassives(_host, key, entry);

            // ⚠ 쿨 게이지를 **가득 채운 채로** 시작한다. 0 에서 시작하면 뺏자마자
            //   8~28초 동안 버튼이 덮개에 가려져 "고장난 버튼" 으로 보인다.
            //   몸을 뺏는 순간이 이 게임에서 가장 쓰고 싶은 순간이기도 하다.
            _skillCooldown = SkillCooldownOf(entry);
            // ⚠ 지속형 스킬은 **몸에 붙은 것**이지 판에 붙은 것이 아니다.
            //   안 끄면 오버히트를 켜고 다른 몸으로 갈아타는 것이 이득이 된다.
            ClearSkillState();

            _bus.Publish(new PossessedEvent
            {
                PossessedHostKey = key,
                DisplayNameEn = entry != null ? ShortNameEn(entry.NameEn) : fallbackName,
                DisplayNameKr = entry != null ? entry.DisplayName : string.Empty,
                Mastery = _player != null && key != null ? _player.GetMastery(key) : 0,
                HostHpMax = _host.HpMax,
            });
            PublishHp();
        }

        // ── 액티브 스킬 ────────────────────────────────────────────────
        //
        // 몸 하나에 스킬 하나. 실제 동작은 `BattleDirector.Skills.cs` 에 있다 —
        // 여기는 **쓸 수 있는가**(쿨·유령·봉인)만 묻고 넘긴다.
        //
        // 한때 21종이 전부 같은 전체 광역이었고, 그다음엔 정본 11종을 23명이 나눠 썼다.
        // 액티브 스킬은 그 몸을 고른 이유가 가장 크게 드러나는 자리인데,
        // 겹쳐 쓰면 몸이 아니라 게이지를 쓰는 것이 된다.

        /// <summary>이 몸의 스킬이 봉인돼 있는가 (숙련도 0).</summary>
        public bool IsSkillSealed(string hostKey)
            => _player != null && !string.IsNullOrEmpty(hostKey) && _player.IsSkillSealed(hostKey);

        public void TryActiveSkill()
        {
            if (!_running || _skillCooldown < SkillCooldownOf(_host?.Profile)) return;
            // 유령은 싸우지 않는다. 자동 사격은 막혀 있었는데 액티브 스킬은 뚫려 있어서,
            // 몸이 없는 상태로 화면 전체를 쓸어버릴 수 있었다.
            // 게이지는 그대로 둔다 — 몸을 얻으면 그때 쓴다.
            var me = _host;
            if (me == null) return;
            // 봉인(숙련도 0)이면 스킬이 안 나간다. 몸은 그대로 쓴다 — 평타만 남는다.
            if (IsSkillSealed(me.Key)) return;

            if (!SandboxKeepsGauge) _skillCooldown = 0f;
            CastHostSkill(me);
        }

        // ── 돌풍 돌진 (아마존) ───────────────────────────────────
        //
        // 짧은 직선 대시. **진입기**다 — 원거리 밭을 가로질러 붙는 것이 목적이지
        // 피해가 목적이 아니다.
        //
        // ⚠ 지형을 통과한다(PD 결정). 그래서 `SlideMove` 를 쓰지 않는다 —
        //   그건 막힌 것을 타고 미끄러지는 함수라, 기둥 앞에서 대시가 멈춘다.
        //   직선 이동 + 방 경계 클램프면 끝이고, 오브젝트 충돌을 안 보므로 더 단순하다.
        //
        // ⚠ 넉백은 넣지 않는다(PD 결정). 밀어내면 붙으려고 쓴 기술이
        //   적을 떼어 놓는 기술이 된다.

        private const float GaleDashMeters = 3.0f;    // 대시 거리
        private const float GaleDashWidthMeters = 0.8f; // 경로 판정 폭
        private const float GaleDashSeconds = 0.15f;  // 이동에 걸리는 시간

        /// <summary>`GANGSTER — GUN` → `GANGSTER`. HUD 이름줄에는 몸 이름만 넣는다.</summary>
        private static string ShortNameEn(string nameEn)
        {
            if (string.IsNullOrEmpty(nameEn)) return string.Empty;
            int cut = nameEn.IndexOf('—');
            if (cut < 0) cut = nameEn.IndexOf('(');
            return (cut > 0 ? nameEn.Substring(0, cut) : nameEn).Trim();
        }


        // ── 돌진 이동 ────────────────────────────────────────────

        private Vector2 _dashFrom, _dashTo;
        private float _dashTime;

        /// <summary>돌진하는 중인가. 이 동안은 조작을 받지 않는다.</summary>
        private bool IsDashing => _dashTime > 0f;

        /// <summary>
        /// 돌진을 한 프레임 옮긴다.
        ///
        /// 감속 곡선이다 — 등속으로 가면 미끄러지는 것처럼 보이고, 가속이면 마지막에
        /// 튀어나간다. 차고 나갔다가 멈춰 서는 것이 이 기술의 그림이다.
        /// </summary>
        private void TickDash(float dt, Unit me)
        {
            _dashTime -= dt;
            if (_dashTime > 0f)
            {
                float k = 1f - Mathf.Clamp01(_dashTime / GaleDashSeconds);
                me.Position = Vector2.Lerp(_dashFrom, _dashTo, k * (2f - k));   // 감속
                return;
            }

            _dashTime = 0f;
            me.Position = _dashTo;
            PlayFx("dash", _dashTo, GaleDashFxSize, loop: false);
        }

        /// <summary>돌진 먼지 크기(px). 그림은 48 캔버스다.</summary>
        private const float GaleDashFxSize = 48f;

        /// <summary>
        /// 대시 피해 배율. 숙련도 Lv1 ×1.5 → Lv4 ×2.2 (`SkillScaling` 의 기본 구간).
        /// 표에 값이 없으면 Lv1 값으로 떨어진다.
        /// </summary>
        private float GaleDashDamageMul
        {
            get
            {
                var sc = ActiveScalingOf(_host?.Profile);
                if (sc == null || sc.Value.IsEmpty) return 1.5f;
                return sc.Value.BaseAt(HostMastery);
            }
        }

        /// <summary>
        /// 도착 후 무적 시간(초). **Lv5 미만이면 0** — 특수 효과는 그때 열린다.
        /// Lv5 1.0초 → Lv10 2.0초.
        /// </summary>
        private float GaleDashInvulnSeconds
        {
            get
            {
                int lv = HostMastery;
                if (lv < SkillScaling.SpecLevel) return 0f;
                var sc = ActiveScalingOf(_host?.Profile);
                if (sc == null || sc.Value.IsEmpty) return 1f;
                return sc.Value.SpecAt(lv, MasteryMaxOrDefault);
            }
        }

        /// <summary>지금 몸의 숙련도. 저장이 없으면 0(봉인)이다.</summary>
        private int HostMastery
            => _player != null && _host != null ? _player.GetMastery(_host.Key) : 0;

        private int MasteryMaxOrDefault => _player != null ? _player.MasteryMax : 10;

        /// <summary>그 몸의 액티브 스킬 성장 수치. 표가 없으면 null.</summary>
        private SkillScaling? ActiveScalingOf(HostEntry e)
        {
            if (e == null || _player == null) return null;
            var skill = _player.GetActiveSkill(e.ActiveSkillKey);
            return skill != null ? skill.Scaling : (SkillScaling?)null;
        }

        /// <summary>점에서 선분까지의 최단 거리. 경로 판정에 쓴다.</summary>
        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>가장 가까운 살아 있는 적. 없으면 null.</summary>
        private Unit NearestEnemy(Vector2 at)
        {
            Unit best = null; float bestD = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive) continue;
                float d = Vector2.Distance(at, e.Position);
                if (d >= bestD) continue;
                bestD = d; best = e;
            }
            return best;
        }

        // ── 잔상 ────────────────────────────────────────────────
        //
        // 대시가 0.15초라 **지나간 자리가 안 보인다.** 잔상 세 장이 경로를 그려 줘야
        // "훅 갔다" 가 읽힌다. 그림은 필요 없다 — 몸 그림을 복제해 청록으로 눕힌다.

        private const int AfterimageCount = 3;
        private const float AfterimageLife = 0.2f;
        private static readonly float[] AfterimageAlpha = { 0.60f, 0.35f, 0.15f };

        private readonly List<Afterimage> _afterimages = new();

        private void SpawnAfterimages(Unit me, Vector2 from, Vector2 to)
        {
            var sprite = me.BodySprite;
            if (sprite == null) return;
            var size = me.GetComponent<RectTransform>().sizeDelta;

            for (int i = 0; i < AfterimageCount; i++)
            {
                var img = RentAfterimage();
                if (img == null) return;
                // 출발점에서 도착점 사이를 고르게 나눈다 — 앞쪽이 진하다.
                float t = (i + 1) / (float)(AfterimageCount + 1);
                img.Play(sprite, Vector2.Lerp(from, to, t), size, me.BodyFlipX,
                         AfterimageAlpha[i], AfterimageLife);
            }
        }

        private Afterimage RentAfterimage()
        {
            for (int i = 0; i < _afterimages.Count; i++)
                if (!_afterimages[i].IsPlaying) return _afterimages[i];
            if (_unitLayer == null) return null;
            var a = Afterimage.Create(_unitLayer);
            _afterimages.Add(a);
            return a;
        }

        private void TickAfterimages(float dt)
        {
            for (int i = 0; i < _afterimages.Count; i++)
            {
                _afterimages[i].Tick(dt);
                _afterimages[i].Refresh();
            }
        }

        /// <summary>반경 안의 살아 있는 적. 순회 중 죽어도 안전하도록 버퍼에 담아 준다.</summary>
        private readonly List<Unit> _rangeBuffer = new();

        private List<Unit> EnemiesInRange(Vector2 at, float radius)
        {
            _rangeBuffer.Clear();
            float r2 = radius * radius;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (!Targetable(e)) continue;
                if ((e.Position - at).sqrMagnitude > r2) continue;
                _rangeBuffer.Add(e);
            }
            return _rangeBuffer;
        }

        private void OnRoomCleared()
        {
            // 매복을 걸고 싸운 방이면 여기서 삯을 치른다.
            // 3택1 이 뜨면 `_awaitingBuff` 때문에 출구가 뒤로 밀린다 — 아래 흐름 그대로다.
            PayFightReward();

            // 정본 ROOM_REWARD — 방 몫의 골드는 이미 **적을 잡을 때마다 바닥에 떨어져 있다.**
            // 방이 비었으니 흩어진 것을 한꺼번에 걷는다. 다 모이면 `GiveCollectedGold` 가
            // 판 골드에 넣고, 거기서부터 HUD 동전이 이어받는다.
            CollectGoldPiles();

            // 정본 ROOM_REWARD.HealPct — 방을 비우면 돌려받는다.
            //
            // ⚠ 이 값이 붙은 방은 원래 **회복 방**이었다. 그 방을 전투방으로 돌려세우면서
            //   들어서자마자 회복해 주던 길이 사라졌는데, 회복을 통째로 없애면
            //   열두 방을 도는 동안 돌려받을 자리가 하나도 없다.
            //   싸워서 비운 대가로 돌려주는 쪽으로 옮긴다 — 값은 정본 그대로다.
            if (_canonRoom != null && _canonRoom.HealPct > 0) HealOnClear(_canonRoom.HealPct);

            // 마지막 스테이지 = 보스방. 보스를 잡으면 **챕터 클리어**로 끝난다.
            if (_roomKind == RoomKind.Elite) _eliteRoomsCleared++;
            bool isLast = IsLastRoom;
            _bus.Publish(new RoomClearedEvent { ClearedRoomIndex = _roomIndex, IsLastRoom = isLast });
            if (isLast) { Finish(true); return; }

            // 버프는 이제 **레벨업**에서 나온다(기획서 A 5-2). 방을 비운 것만으로는
            // 주지 않는다 — 잡는 만큼 성장하는 쪽이 교전을 피하지 않게 만든다.
            SpawnExit();
        }

        /// <summary>
        /// 방을 비운 대가로 돌려받는다. 호스트와 고스트를 같은 비율로 올린다.
        /// </summary>
        private void HealOnClear(int percent)
        {
            _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + GhostHpMax * percent / 100);
            if (_host != null) _host.Heal(Mathf.Max(1, _host.HpMax * percent / 100));
            PublishHp();
        }

        // ── 판 골드 ──────────────────────────────────────────────
        //
        // 로비의 골드와 다른 주머니다. 방을 비울 때마다 들어오고 이벤트·상점에서
        // 나간다. 판이 끝나면 남은 만큼이 보상에 얹힌다 —
        // 안 쓰고 아낀 것이 손해가 되면 아무도 안 쓴다.

        private int _runGold;
        public int RunGold => _runGold;

        private void AddRunGold(int amount) => AddRunGold(amount, Vector2.zero, false);

        /// <summary>골드가 **누구 자리에서** 들어왔는지까지 알린다. 그 자리에서 동전이 튄다.</summary>
        private void AddRunGoldAtPlayer(int amount)
        {
            var a = Avatar;
            if (a == null) { AddRunGold(amount); return; }
            AddRunGold(amount, a.Position, true);
        }

        private void AddRunGold(int amount, Vector2 fieldAt, bool hasAt)
        {
            if (amount == 0) return;
            // 폭력배 패시브 — 이 몸으로 주우면 더 들어온다.
            if (amount > 0) amount = Mathf.RoundToInt(amount * GoldGainMul);
            _runGold = Mathf.Max(0, _runGold + amount);

            // 자리는 **들어올 때만** 붙는다. 상점에서 나가는 골드까지 동전이 튀면
            // 쓴 것과 번 것이 화면에서 같아 보인다.
            bool gain = amount > 0 && hasAt;
            _bus.Publish(new RunGoldChangedEvent
            {
                Gold = _runGold,
                Delta = amount,
                SourceWorld = gain ? FieldToWorld(fieldAt) : default,
                HasSource = gain,
            });

            if (gain) ShowGoldGain(fieldAt, amount);
        }

        /// <summary>획득 수치. 피해 숫자와 같은 풀을 쓰되 금색이라 한눈에 갈린다.</summary>
        private static readonly Color GoldGainColor = new(1f, 0.78f, 0.25f, 1f);

        private void ShowGoldGain(Vector2 at, int amount)
        {
            var t = RentDamageText();
            if (t == null) return;
            t.Show(at, $"+{amount}", GoldGainColor);
        }

        /// <summary>
        /// 필드 좌표 → 월드 좌표.
        ///
        /// `_textLayer` 의 자식은 앵커 (0,1) 규약이라(<see cref="DamageText"/>) 기준점이
        /// 부모 rect 의 **좌상단**이다. HUD 는 다른 가지에 있어서 이 변환 없이는
        /// 동전이 날아갈 목적지를 서로 말할 수 없다.
        /// </summary>
        private Vector3 FieldToWorld(Vector2 at)
        {
            if (_textLayer == null) return Vector3.zero;
            var r = _textLayer.rect;
            return _textLayer.TransformPoint(new Vector2(r.xMin + at.x, r.yMax + at.y));
        }

        // ── 이벤트 방 ────────────────────────────────────────────
        //
        // 전투가 없는 대신 자원에 값을 매기는 자리다. 골드·호스트 체력·고스트 체력을
        // 내주고 카드나 회복을 받는다. 정본은 전부 판당 한 번씩만 나온다.

        private readonly HashSet<string> _eventsUsed = new();
        private EventEntry _event;
        private bool _eventDone;

        public EventEntry PendingEvent => _event;

        /// <summary>
        /// 이벤트 방을 그냥 지나가게 하는 스위치.
        ///
        /// 예전에는 켜 둔 채였다 — 이벤트 방을 둘 자리가 없어서 표만 세워 두고
        /// 팝업을 막았다. 이제 **004 가 이벤트 방**이라 자리가 생겼으므로 내린다
        /// (기획 2026-09-08). `EventTable` 18종이 전부 `Implemented` 다.
        /// </summary>
        private static readonly bool EventOffersDisabled = false;

        private void OfferEvent()
        {
            _event = null;
            _eventDone = false;
            if (EventOffersDisabled) { SpawnExit(); return; }
            if (_eventTable == null) { SpawnExit(); return; }

            // 챕터를 가리지 않고 **18종 한 통**에서 뽑는다(`EventTable.Draw` 주석).
            _event = _eventTable.Draw(_eventsUsed, _rng);
            if (_event == null) { SpawnExit(); return; }   // 이 판에서 다 봤다

            _eventsUsed.Add(_event.EventId);
            _bus.Publish(new EventOfferEvent
            {
                EventId = _event.EventId,
                Title = _event.DisplayTitle,
                Body = _event.DisplayBody,
                AcceptLabel = _event.DisplayAccept,
                DeclineLabel = _event.DisplayDecline,
                CostLabel = CostLabelOf(_event),
                RewardLabel = RewardLabelOf(_event),
                CanAfford = CanAfford(_event) && CanReceive(_event),
                BlockedReason = BlockedReasonOf(_event),
            });
        }

        /// <summary>
        /// 이벤트 골드값의 챕터 배율.
        ///
        /// 주의: 표 값은 **CH1 기준**이다. 이벤트를 챕터로 안 나누고 한 통에서 뽑기로
        ///   하면서(2026-09-08), CH1 첫 방에 「골드 115」짜리가 뜰 수 있게 됐다.
        ///   값을 챕터마다 적어 두는 대신 여기서 곱한다 - 표는 하나면 된다.
        ///   상점 사다리(42-48-78)에 맞춘 임시값이고, 밸런스에서 확정한다.
        /// </summary>
        private static readonly float[] EventGoldMuls = { 1.0f, 1.4f, 1.9f, 2.5f, 3.2f, 4.0f };

        private int EventGold(int baseValue)
            => Mathf.RoundToInt(baseValue * EventGoldMuls[Mathf.Clamp(_runChapter, 1, 6) - 1]);

        /// <summary>
        /// 무엇을 받는가. 「악마의 계약이 무슨 버프인지 하나도 모르겠다」는 보고를 받고
        /// 붙였다 — 값(대가)만 적혀 있고 **받는 것이 화면에 없었다.**
        /// 본문은 분위기를 적는 자리라 「가장 큰 것을 준다」 같은 말뿐이어서,
        /// 무엇을 사는 거래인지 알 수가 없었다.
        /// </summary>
        private string RewardLabelOf(EventEntry e)
        {
            int many = Mathf.Max(1, e.RewardValue);
            string grade = e.RewardRarity switch
            {
                CardRarity.Legendary => Localize.Get("ui.rarity.legendary"),
                CardRarity.Epic => Localize.Get("ui.rarity.epic"),
                CardRarity.Rare => Localize.Get("ui.rarity.rare"),
                _ => Localize.Get("ui.rarity.common"),
            };
            return e.RewardType switch
            {
                EventReward.CardGrant => Localize.Format("ui.event.reward.card_grant", grade, many),
                EventReward.CardOffer => Localize.Format("ui.event.reward.card_offer", grade),
                EventReward.UpgradeCard => Localize.Get("ui.event.reward.upgrade"),
                EventReward.HostHeal => Localize.Format("ui.event.reward.host_heal", e.RewardValue),
                EventReward.GhostHeal => Localize.Format("ui.event.reward.ghost_heal", e.RewardValue),
                EventReward.Gold => Localize.Format("ui.event.gold", EventGold(e.RewardValue)),
                EventReward.PossessReach => Localize.Format("ui.event.reward.reach", e.RewardValue),
                EventReward.ShopDiscount => Localize.Format("ui.event.reward.discount", e.RewardValue),
                EventReward.BossShieldBreak => Localize.Get("ui.event.reward.shield_break"),
                EventReward.SpawnHost => Localize.Get("ui.event.reward.spawn_host"),
                _ => string.Empty,
            };
        }

        private string CostLabelOf(EventEntry e) => e.CostType switch
        {
            EventCost.Gold => Localize.Format("ui.event.gold", EventGold(e.CostValue)),
            EventCost.GhostHp => Localize.Format("ui.event.cost.ghost_hp", e.CostValue),
            EventCost.HostHp => Localize.Format("ui.event.cost.host_hp", e.CostValue),
            EventCost.MaxHp => Localize.Format("ui.event.cost.max_hp", e.CostValue),
            _ => string.Empty,
        };

        private bool CanAfford(EventEntry e) => e.CostType switch
        {
            EventCost.Gold => _runGold >= EventGold(e.CostValue),
            // 체력을 다 내주고 그 자리에서 죽는 선택지는 주지 않는다.
            // 값을 치르는 순간 지는 거래는 거래가 아니다.
            EventCost.GhostHp => _ghostHp > GhostHpMax * e.CostValue / 100,
            EventCost.HostHp => _host != null && _host.Hp > _host.HpMax * e.CostValue / 100,
            // 하한(원본의 40%)을 뚫는 계약은 아예 못 고른다. 목록에서 빼는 것과 같다 -
            // 계약을 셋 다 사면 스스로 멈춘다.
            EventCost.MaxHp => _maxHpDebt + e.CostValue <= MaxHpDebtCap,
            _ => true,
        };

        /// <summary>
        /// 지금 이 보상을 실제로 받을 수 있는가.
        ///
        /// ⚠ `HostHeal` 은 몸이 없으면 아무 일도 안 일어난다. 그런데 값은 먼저 치러진다 —
        ///   유령 상태로 사당(EV_CH1_01)을 받으면 **골드 18 만 나가고 끝**이었다.
        ///   돈을 내고 나서야 "몸이 없어 받을 수 없었다" 를 알려 주는 건 거래가 아니다.
        ///   고를 수 없게 막고, 왜 못 고르는지 팝업에 적어 준다.
        /// </summary>
        private bool CanReceive(EventEntry e) => e.RewardType switch
        {
            EventReward.HostHeal => _host != null,
            _ => true,
        };

        /// <summary>
        /// 못 받는 이유. **짧게 적는다** — 대가 명판(256px)에 값과 나란히 들어간다.
        /// 길게 적으면 명판 밖으로 두 줄이 되어 버튼 위로 흘러내린다.
        /// </summary>
        private string BlockedReasonOf(EventEntry e)
            => !CanReceive(e) ? Localize.Get("ui.event.blocked.no_body")
             : !CanAfford(e) ? Localize.Get("ui.event.blocked.cant_pay")
             : string.Empty;

        /// <summary>이벤트를 받아들이거나 지나친다. UI 가 호출한다.</summary>
        public void ResolveEvent(bool accept)
        {
            if (_event == null || _eventDone) return;
            var e = _event;
            _eventDone = true;

            string line;
            if (!accept || !CanAfford(e) || !CanReceive(e))
            {
                // 등을 돌려도 받는 것이 있는 이벤트가 있다 (정본의 `..._OR_...`)
                line = e.HasDeclineReward
                    ? GiveReward(e.DeclineReward, e.DeclineValue, e.RewardRarity, e.RewardKey)
                    : Localize.Get("ui.event.result.passed");
            }
            else if (e.FightFirst)
            {
                // 받아들였으면 먼저 싸운다. 보상은 방을 비운 뒤에 온다 —
                // 이기기 전에 주면 그건 도전이 아니라 그냥 상자다.
                PayCost(e);
                _fightReward = e;
                SpawnProcedural(e.FightCount, e.FightElite, _roomIndex * 7 + 3);
                line = e.FightElite ? Localize.Get("ui.event.result.elite") : Localize.Get("ui.event.result.ambush");
            }
            else
            {
                PayCost(e);
                // 확률이 걸린 것은 빗나갈 수 있다. 빗나가면 뒷쪽 보상으로 떨어진다 —
                // 값만 치르고 빈손이면 그건 선택이 아니라 벌이다.
                bool hit = e.ChancePercent <= 0 || _rng.Next(100) < e.ChancePercent;
                line = hit
                    ? GiveReward(e.RewardType, e.RewardValue, e.RewardRarity, e.RewardKey)
                    : e.HasDeclineReward
                        ? Localize.Get("ui.event.result.missed") + " " + GiveReward(e.DeclineReward, e.DeclineValue, e.RewardRarity, e.RewardKey)
                        : Localize.Get("ui.event.result.missed");
                if (hit && e.ExtraGold > 0) { AddRunGoldAtPlayer(e.ExtraGold); line += " · " + Localize.Format("ui.event.result.gold", e.ExtraGold); }
            }

            _bus.Publish(new EventResolvedEvent { EventId = e.EventId, Accepted = accept, ResultLine = line });
            _event = null;

            // 싸움이 시작됐으면 출구는 적을 다 잡은 뒤에 열린다.
            // 3택1 이 떴으면 그것을 고른 뒤에 열린다 (`ChooseBuff` 가 연다).
            if (_fightReward == null && !_awaitingBuff) SpawnExit();
        }

        /// <summary>매복·도전을 이겼을 때 줄 것. 싸우는 동안 여기 들고 있는다.</summary>
        private EventEntry _fightReward;

        /// <summary>매복을 이겼다. 걸려 있던 보상을 준다.</summary>
        private void PayFightReward()
        {
            var e = _fightReward;
            if (e == null) return;
            _fightReward = null;

            string line = GiveReward(e.RewardType, e.RewardValue, e.RewardRarity, e.RewardKey);
            if (e.ExtraGold > 0) { AddRunGoldAtPlayer(e.ExtraGold); line += " · " + Localize.Format("ui.event.result.gold", e.ExtraGold); }
            _bus.Publish(new EventResolvedEvent { EventId = e.EventId, Accepted = true, ResultLine = line });
        }

        private void PayCost(EventEntry e)
        {
            switch (e.CostType)
            {
                case EventCost.Gold:
                    AddRunGold(-e.CostValue);
                    break;
                case EventCost.GhostHp:
                    _ghostHp = Mathf.Max(1, _ghostHp - GhostHpMax * e.CostValue / 100);
                    PublishHp();
                    break;
                case EventCost.HostHp:
                    if (_host != null)
                    {
                        // 여기서 죽지는 않는다. `TakeDamage` 를 쓰면 몸을 잃는 흐름을
                        // 타게 되는데, 거래로 몸이 죽는 것은 이 방의 약속이 아니다.
                        _host.SpendHp(_host.HpMax * e.CostValue / 100);
                        PublishHp();
                    }
                    break;
                case EventCost.MaxHp:
                    AddMaxHpDebt(e.CostValue);
                    break;
            }
        }

        private string GiveReward(EventReward kind, int value, CardRarity rarity, string key = null)
        {
            switch (kind)
            {
                case EventReward.HostHeal:
                    if (_host == null) return Localize.Get("ui.event.result.no_body");
                    _host.Heal(Mathf.Max(1, _host.HpMax * value / 100));
                    PublishHp();
                    return Localize.Format("ui.event.reward.host_heal", value);

                case EventReward.GhostHeal:
                    _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + GhostHpMax * value / 100);
                    PublishHp();
                    return Localize.Format("ui.event.reward.ghost_heal", value);

                case EventReward.Gold:
                {
                    // 대가와 같은 배율을 탄다. 한쪽만 곱하면 뒤 챕터의 거래가 한없이 남는다.
                    int gold = EventGold(value);
                    AddRunGoldAtPlayer(gold);
                    return Localize.Format("ui.event.result.gold", gold);
                }

                case EventReward.CardGrant:
                {
                    // 이름이 박힌 카드가 있으면 그것을 준다 (`굶주림 → 흡혈 c019`).
                    // 없으면 등급으로 뽑는다. `value` 는 장수다 — 0·1 은 한 장.
                    int count = Mathf.Max(1, value);
                    string last = null;
                    for (int i = 0; i < count; i++)
                    {
                        // ⚠ 이름이 박힌 카드가 **없어진 카드**일 수 있다.
                        //   실제로 「굶주림」이 지워진 `c019` 를 가리켜, 최대 체력을
                        //   15% 내주고도 아무것도 안 주고 끝났다. 못 찾으면 등급으로 뽑는다.
                        var card = !string.IsNullOrEmpty(key) && i == 0
                                 ? (_buffTable?.Get(key) ?? DrawCardOfRarity(rarity))
                                 : DrawCardOfRarity(rarity);
                        if (card == null) break;
                        _buffs.Apply(card);
                        _bus.Publish(new BuffChosenEvent { ChosenKey = card.BuffKey, TotalBuffCount = _buffs.Count });
                        last = card.DisplayName;
                    }
                    if (last == null) return Localize.Get("ui.event.result.nothing_left");
                    return count > 1 ? Localize.Format("ui.event.result.cards", count) : Localize.Format("ui.event.result.got", last);
                }

                case EventReward.CardOffer:
                    OfferBuff();
                    return _awaitingBuff ? Localize.Get("ui.event.result.pick_card") : Localize.Get("ui.event.result.nothing_to_pick");

                case EventReward.UpgradeCard:
                {
                    var up = PickUpgradable();
                    if (up == null) return Localize.Get("ui.event.result.nothing_to_upgrade");
                    int lv = _buffs.LevelOf(up.BuffKey);
                    _buffs.Apply(up);
                    _bus.Publish(new BuffChosenEvent { ChosenKey = up.BuffKey, TotalBuffCount = _buffs.Count });
                    return $"{up.DisplayName} Lv.{lv} → Lv.{_buffs.LevelOf(up.BuffKey)}";
                }

                case EventReward.PossessReach:
                    _possessReachMul += value / 100f;
                    return Localize.Format("ui.event.result.reach", value);

                case EventReward.ShopDiscount:
                    _shopDiscount = Mathf.Clamp(_shopDiscount + value, 0, 80);
                    return Localize.Format("ui.event.result.discount", _shopDiscount);

                case EventReward.BossShieldBreak:
                    _bossShieldBreak = true;
                    return Localize.Get("ui.event.result.shield_break");

                case EventReward.SpawnHost:
                    return SpawnPossessableHost(key);
            }
            return string.Empty;
        }

        // ── 이벤트가 남기는 판 단위 효과 ─────────────────────────
        //
        // 카드가 아니라 **그 판에만 붙는 상태**다. 카드 슬롯(8칸)을 먹지 않으므로
        // 이벤트를 밟을 이유가 카드 말고도 생긴다.

        private float _possessReachMul;   // 빙의 사거리 증가분 (0.35 = +35%)
        private int _shopDiscount;        // 상점 카드 할인율(%)
        private bool _bossShieldBreak;    // 다음 보스 방어막 1회 무효

        public float PossessReachMul => 1f + _possessReachMul;
        public int ShopDiscount => _shopDiscount;
        public bool HasBossShieldBreak => _bossShieldBreak;

        /// <summary>
        /// 레벨을 올릴 수 있는 카드 중 **가장 낮은 것**을 고른다.
        /// 제일 높은 것을 올리면 이미 센 쪽만 더 세지고, 무작위로 고르면
        /// 값을 치른 결과를 스스로 설명하지 못한다.
        /// </summary>
        private BuffEntry PickUpgradable()
        {
            if (_buffTable == null) return null;
            BuffEntry best = null;
            int bestLv = int.MaxValue;
            var list = _buffTable.Entries;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null) continue;
                int lv = _buffs.LevelOf(e.BuffKey);
                if (lv <= 0 || lv >= e.MaxLevel) continue;
                if (lv >= bestLv) continue;
                bestLv = lv; best = e;
            }
            return best;
        }

        /// <summary>
        /// 빼앗을 수 있는 몸 하나를 세운다. 싸우라고 부른 것이 아니라
        /// **가져가라고 놓아 둔 것**이라 먼저 덤비지 않는다.
        /// </summary>
        private string SpawnPossessableHost(string hostKey)
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || string.IsNullOrEmpty(hostKey)) return Localize.Get("ui.event.result.nobody");

            HostEntry def = null;
            for (int i = 0; i < hosts.Count; i++)
                if (hosts[i].HostKey == hostKey) { def = hosts[i]; break; }
            if (def == null) return Localize.Get("ui.event.result.nobody");

            var u = NewUnit($"Event_{hostKey}");
            u.Setup(UnitSide.Enemy, def.HostKey, def.DisplayName, UnitGet(def.SpriteKey),
                    EnemyHpOf(def), EnemyAtkOf(def), EnemySpeedOf(def),
                    EnemyRangeOf(def), EnemyIntervalOf(def),
                    UnitBox(84f, 78f), isBoss: false, profile: def);
            var me = Avatar;
            u.Position = me != null ? me.Position + new Vector2(0f, 260f) : SpawnSlot(0, 1);
            ClampToField(u);
            u.PossessPriority = def.PossessPriority;
            u.PossessRange = 0f;
            u.IsAggro = false;   // 먼저 덤비지 않는다
            u.SetState(EnemyState.Idle);
            ApplyFacingSprites(u, def.SpriteKey);
            _enemies.Add(u);
            return Localize.Format("ui.event.result.appeared", def.DisplayName);
        }

        /// <summary>
        /// 그 등급에서 아직 안 가진(또는 더 올릴 수 있는) 카드 하나를 뽑는다.
        /// 없으면 한 단계 낮은 등급으로 내려간다 — 등급이 높을수록 수가 적어
        /// 후보가 금방 마르는데, 그때 빈손으로 돌려보내면 값을 치른 보람이 없다.
        /// </summary>
        private BuffEntry DrawCardOfRarity(CardRarity want)
        {
            if (_buffTable == null) return null;
            for (int r = (int)want; r >= 0; r--)
            {
                BuffEntry pick = null;
                int seen = 0;
                var list = _buffTable.Entries;
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null || !e.Implemented || (int)e.Rarity != r) continue;
                    if (_buffs.ExcludedKeys.Contains(e.BuffKey)) continue;
                    if (_rng.Next(++seen) == 0) pick = e;
                }
                if (pick != null) return pick;
            }
            return null;
        }

        /// <summary>제시된 3장 중 하나를 고른다. UI 가 호출한다.</summary>
        public void ChooseBuff(string buffKey)
        {
            if (!_awaitingBuff) return;

            var e = _buffTable?.Get(buffKey);
            if (e == null) return;

            _buffs.Apply(e);

            // ⚠ **실제로 붙은 뒤에** 보여 준다. 위 검사보다 앞에 두면 아무 일도 안 난
            //   호출(빈 키·중복 클릭)에도 번쩍여서 「먹었나?」가 헷갈린다.
            PlayUpgradeFx();

            // 즉발 효과 — 누적 배율이 아니라 그 자리에서 끝나는 것들
            if (e.Kind == BuffKind.GhostHp)
            {
                _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + e.Value);
                PublishHp();
            }
            else if (e.Kind == BuffKind.Heal && _host != null)
            {
                _host.Heal(Mathf.Max(1, _host.HpMax * e.Value / 100));
                PublishHp();
            }

            _awaitingBuff = false;
            _offer.Clear();
            _bus.Publish(new BuffChosenEvent { ChosenKey = buffKey, TotalBuffCount = _buffs.Count });

            // ⚠ 여기서 출구를 직접 열지 않는다.
            //   방이 이미 비었으면 다음 프레임의 `Tick` 이 `OnRoomCleared` 를 부른다 —
            //   골드·정예 집계·매복 삯이 전부 그 안에 있다.
            //   예전에는 여기서 `SpawnExit()` 를 바로 불렀는데, 그러면 출구가 열려 버려
            //   `Tick` 의 조건(지금은 `!_exitOpen`)이 깨지고 **방 클리어가 통째로 건너뛰어졌다.**
            //   레벨업이 뜬 방에서만 골드가 안 들어오던 원인이다.
        }

        // ── 출구 ─────────────────────────────────────────────────
        // 방을 비우면 자동으로 다음 방으로 넘어가는 게 아니라 **출구가 열린다.**
        // 걸어서 통과해야 넘어가므로, 다 잡은 뒤에도 한 번 더 판단할 여지가 생긴다
        // (남은 Ghost HP 를 보고 쉬어 갈지 바로 갈지 — 시계가 계속 도는 상태다).

        /// <summary>
        /// 방에 들어서면서 문을 **닫힌 채로** 세운다.
        /// 닫힌 그림이 아직 없으면 아무것도 세우지 않는다 — 열린 문을 미리 보여 주면
        /// 걸어가 봤자 안 열려서 고장으로 읽힌다.
        /// </summary>
        private void PlaceClosedExit()
        {
            _exitOpen = false;
            _exitOpenTime = -1f;
            if (_exitClosed == null) { DespawnExit(); return; }
            BuildExitGates();
            ApplyExitSprite();
        }

        /// <summary>문을 연다. 방을 비웠거나 방의 볼일이 끝났을 때 부른다.</summary>
        // ── 출구 안내 화살표 ─────────────────────────────────────
        //
        // 방을 비우면 문이 열리는데 **어디로 가야 하는지 화면에 표시가 없었다.**
        // 위쪽 안내 문구는 글자라 전투 직후에는 눈에 안 들어온다.
        // 문 위에 화살표를 띄워 둔다 — 문을 지나면 저절로 사라진다(방이 바뀐다).

        private readonly List<Impact> _exitArrows = new();

        /// <summary>
        /// 화살표가 문에서 **방 안쪽으로** 이만큼(px) 떨어져 뜬다.
        ///
        /// ⚠ 92 px 로는 모자랐다. 방 위쪽은 화면에 다 안 들어와서, 화살표 몸통이
        ///   상단 HUD 와 안내 문구 뒤로 숨고 **바닥 광채만** 보였다.
        ///   문에서 한 칸 더 내려와야 화살표가 통째로 읽힌다.
        /// </summary>
        private const float ExitArrowLift = 180f;
        private const float ExitArrowSize = 96f;

        private void ShowExitArrows()
        {
            ClearExitArrows();
            // ⚠ **문 바깥으로 밀면 안 된다.** 문은 방 가장자리에 붙어 있어서,
            //   바깥으로 92 px 밀면 화살표가 방 밖으로 나가 상단 HUD 뒤에 숨는다 —
            //   실제로 그렇게 짰다가 화면에 아무것도 안 떴다.
            //   방 가운데를 향해(안쪽으로) 밀어야 문 앞에 선다. 문이 어느 벽에 붙어
            //   있든(위·아래·좌·우) 같은 규칙 하나로 맞는다.
            var center = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * 0.5f);
            for (int i = 0; i < _exits.Count; i++)
            {
                var view = _exits[i].View;
                if (view == null) continue;
                var inward = center - view.anchoredPosition;
                var at = view.anchoredPosition
                       + (inward.sqrMagnitude > 0.01f ? inward.normalized * ExitArrowLift : Vector2.zero);
                // ⚠ 돌아가는 표시(`loop: true`)로 띄운다. 한 번 재생하고 끝나면
                //   전투 중에 문이 열린 뒤 화살표가 사라져 다시 길을 잃는다.
                var im = TakeLoopFx("exitarrow", at, ExitArrowSize);
                if (im != null) _exitArrows.Add(im);
            }
        }

        private void ClearExitArrows()
        {
            for (int i = 0; i < _exitArrows.Count; i++) _exitArrows[i]?.Stop();
            _exitArrows.Clear();
        }

        private void SpawnExit()
        {
            if (_exitOpen) return;              // 이미 열린 문을 다시 열지 않는다
            _exitOpen = true;

            // 닫힌 채로 세워 둔 문이 있으면 그것을 연다. 없으면 지금 세운다
            // (닫힌 그림이 아직 없는 동안의 예전 동작 그대로).
            if (_exits.Count == 0) BuildExitGates();
            _exitOpenTime = _exitClosed != null ? 0f : -1f;
            ShowExitArrows();
            ApplyExitSprite();

            if (_exits.Count > 0) _bus.Publish(new ExitOpenedEvent { StageIndex = _roomIndex });
        }

        // 65° 기준으로 다시 그린 문(26차). 예전 `exitportal*` 은 벽에 난 문을
        // 정면에서 본 그림이라 바닥과 따로 놀았다 — 파일을 통째로 갈았다.
        private const string ExitClosedKey = "exitgate_closed";
        private const string ExitOpen1Key = "exitgate_open1";
        private const string ExitOpen2Key = "exitgate_open2";
        private const string ExitOpenKey = "exitgate_open";

        /// <summary>지금 상태에 맞는 그림을 문 전부에 바른다.</summary>
        private void ApplyExitSprite()
        {
            Sprite sprite = !_exitOpen ? _exitClosed
                          : _exitOpenTime < 0f ? _exitOpened
                          : _exitOpenTime < ExitOpenSeconds * 0.34f ? _exitOpen1
                          : _exitOpenTime < ExitOpenSeconds * 0.67f ? _exitOpen2
                          : _exitOpened;
            // 중간 그림이 아직 안 왔으면 건너뛰고 열린 그림을 쓴다 — 없는 장에서 멈추면
            // 문이 사라진 것처럼 보인다.
            if (sprite == null) sprite = _exitOpened;
            for (int i = 0; i < _exits.Count; i++)
            {
                if (_exits[i].Img == null) continue;
                _exits[i].Img.sprite = sprite;
                // 팻말은 열린 뒤에만 읽을 수 있으면 된다. 닫힌 문에 길 안내를 붙여 두면
                // 아직 고를 수 없는 것을 고르라고 보여 주는 셈이다.
                if (_exits[i].Label != null) _exits[i].Label.SetActive(_exitOpen);
            }
        }

        /// <summary>여는 연출을 진행시킨다.</summary>
        private void TickExitOpen(float dt)
        {
            if (!_exitOpen || _exitOpenTime < 0f || _exitOpenTime >= ExitOpenSeconds) return;
            _exitOpenTime += dt;
            ApplyExitSprite();
        }

        private void BuildExitGates()
        {
            DespawnExit();

            // ⚠ **갈림길은 없다. 문은 하나다.**
            //   임포터가 첫 갈래만 굽는다(`WriteExits`). 문이 둘 서면 그 앞이 고르는
            //   화면이 되고, 그런 선택지는 지금 기획에 없다.
            if (_canonRoom != null && _canonRoom.Exits.Count > 0)
            {
                var x = _canonRoom.Exits[0];
                _exits.Add(NewExit(ExitInField(ToPixels(x.At)), x.NextRoomId));
            }
            else if (_canonRoom == null)
            {
                // 정본 경로가 없을 때(절차적 생성)만 이름 없는 문을 세운다.
                _exits.Add(NewExit(new Vector2(_roomSize.x * 0.5f, -_roomSize.y * 0.05f), null));
            }
            else
            {
                // 정본 방인데 문이 없다 = **챕터 끝**이다. 여기서 문을 세우면
                // 아무 데도 가지 않는 문이 되고, 걸어 들어가면 정본을 벗어나
                // 절차적으로 만든 방으로 떨어진다. 끝은 `Finish` 가 처리한다.
            }
        }

        /// <summary>
        /// 문을 **방 안으로** 끌어들인다.
        ///
        /// ⚠ 보스 방은 아레나 높이가 따로 있어 표의 방 높이보다 낮을 수 있다.
        ///   표는 문을 12.4 m 에 적어 두는데 파이썬 아레나는 11.1 m 라, 문이
        ///   방 위쪽 **바깥**(y +92 px)에 서 버렸다. 플레이어는 방 끝까지 걸어가도
        ///   닿을 수가 없어(터치 반경 70) **보스를 잡고도 방에서 못 나갔다**
        ///   (2026-09-08 자동 플레이로 CH3 010 에서 걸렸다).
        ///
        ///   문은 언제나 걸어가 닿을 수 있어야 한다. 방 위 끝에서 한 걸음 안쪽으로 당긴다.
        /// </summary>
        private Vector2 ExitInField(Vector2 at)
        {
            const float Margin = 56f;   // 문 그림 절반보다 조금 안쪽
            return new Vector2(Mathf.Clamp(at.x, Margin, _roomSize.x - Margin),
                               Mathf.Clamp(at.y, -_roomSize.y + Margin, -Margin));
        }

        private ExitGate NewExit(Vector2 at, string nextRoomId, string label = null)
        {
            var go = new GameObject($"Exit_{nextRoomId ?? "next"}",
                                    typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // 새 문 그림은 216×180 px (가로 3 m · 세로 2.5 m). 그림 비율 그대로 둔다 —
            // 늘리면 좌우 기둥이 블록과 다른 두께가 되어 같은 돌로 안 보인다.
            rt.sizeDelta = new Vector2(216f, 180f);
            rt.anchoredPosition = at;

            var img = go.GetComponent<Image>();
            img.sprite = _exitOpened;   // 상태에 맞는 그림은 ApplyExitSprite 가 바른다
            img.raycastTarget = false;
            img.preserveAspect = true;

            GameObject labelGo = null;
            if (!string.IsNullOrEmpty(label)) labelGo = AttachExitLabel(rt, label);
            return new ExitGate { View = rt, Img = img, NextRoomId = nextRoomId, Label = labelGo };
        }

        /// <summary>
        /// 문에 팻말을 붙인다.
        ///
        /// ⚠ 문 **위**가 아니라 아래다. 출구는 방 맨 위(높이-0.6m)에 서므로
        ///   위에 붙이면 방 밖으로 나가 잘린다. 실제로 그렇게 만들었다가
        ///   세 문이 다 무명으로 보였다.
        /// </summary>
        private GameObject AttachExitLabel(RectTransform gate, string text)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(gate, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            // 문 사이 간격(3갈래 기준 202px)보다 좁게 잡는다
            rt.sizeDelta = new Vector2(180f, 54f);
            rt.anchoredPosition = new Vector2(0f, -2f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18f;
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.color = new Color(0.90f, 0.86f, 0.70f);
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            tmp.lineSpacing = -12f;
            tmp.font = TMP_Settings.defaultFontAsset;   // 피해 숫자와 같은 폰트를 쓴다
            return go;
        }

        private void DespawnExit()
        {
            for (int i = 0; i < _exits.Count; i++)
                if (_exits[i].View != null) Destroy(_exits[i].View.gameObject);
            _exits.Clear();
        }

        /// <summary>출구에 닿았으면 다음 스테이지로 넘어간다.</summary>
        private void TickExit()
        {
            if (_exits.Count == 0) return;
            if (!_exitOpen) return;        // 닫힌 문은 서 있기만 한다 — 닿아도 안 넘어간다
            var me = Avatar;
            if (me == null) return;

            for (int i = 0; i < _exits.Count; i++)
            {
                var gate = _exits[i];
                if (gate.View == null) continue;
                if (Vector2.Distance(me.Position, gate.View.anchoredPosition)
                    > _config.ExitTouchRadius) continue;

                // 어느 문으로 나갔는지가 곧 경로 선택이다.
                if (_canonRoom != null) _canonRoomId = gate.NextRoomId;
                DespawnExit();
                // 도달 스테이지를 갱신한다 — 호스트 해금 조건이 이 값을 본다.
                if (_player != null)
                    _runStage = _roomIndex + 2;
                EnterRoom(_roomIndex + 1);
                return;
            }
        }

        private int GhostHpMax => _config.GhostHpMax + _buffs.GhostHpBonus;

        private void Finish(bool cleared)
        {
            if (!_running) return;
            _running = false;

            // ⚠ **판이 끝나는 지금, 딱 한 번 저장에 옮긴다.**
            //   걷는 동안에는 `_runChapter`·`_runStage` 만 움직였다 —
            //   방마다 저장을 건드리면 시험으로 열어 본 방까지 진행도에 박힌다.
            //   호스트 해금(`ClearedChapter`·`ReachedStage`)이 이 값을 보므로
            //   여기서 옮겨 줘야 걸어온 만큼이 기록된다.
            if (_player != null && _player.IsReady)
                _player.SetProgress(_runChapter, _runStage);
            // 보상은 **통과한 스테이지 수** 기준. 챕터를 끝냈으면 전부 통과한 것이다.
            int stages = cleared ? RoomTotal : Mathf.Max(0, _roomIndex);

            // 정본 REWARD_DB — 방마다 골드가 조금씩 붙고, 정예방은 스피릿 코어와
            // 호스트 메모리를 준다. 챕터를 끝내면 큰 몫이 따로 온다.
            //   R_STD  방당 Gold 10
            //   R_ELITE 정예방 Gold 25 · Core 2 · Memory 1
            //   R_CH1  챕터 클리어 Gold 120 · Core 4 · EXP 5
            // 판에서 모은 골드가 정산의 축이다. 방마다 붙는 정본 값이라
            // 통과한 방 수에 비례한다 — 여기에 정예·챕터 클리어 몫이 얹힌다.
            int gold = _runGold + _eliteRoomsCleared * 15;
            int core = _eliteRoomsCleared * 2;
            int memory = _eliteRoomsCleared;
            int gem = 0;
            if (cleared)
            {
                int ch = Mathf.Clamp(_runChapter, 1, 3);
                gold += ch == 1 ? 120 : ch == 2 ? 180 : 260;
                core += ch == 1 ? 4 : ch == 2 ? 6 : 9;
                memory += ch == 1 ? 0 : ch == 2 ? 2 : 4;
                gem += ch == 3 ? 20 : 0;
            }

            _bus.Publish(new StageFinishedEvent
            {
                IsCleared = cleared,
                RewardGold = gold,
                RewardGhostExp = _config.RewardGhostExp(stages),
                RewardSpiritCore = core,
                RewardHostMemory = memory,
                RewardGem = gem,
            });
        }

        private void PublishHp()
        {
            // 고스트 머리 위 바를 실제 남은 값에 맞춘다. 안 맞추면 늘 가득 찬 채로 떠 있다.
            if (_ghost != null) _ghost.SyncHp(_ghostHp);

            _bus.Publish(new CombatHpChangedEvent
            {
                GhostHp = _ghostHp,
                GhostHpMax = GhostHpMax,
                HostHp = _host != null ? _host.Hp : 0,
                HostHpMax = _host != null ? _host.HpMax : 0,
                HasHost = _host != null,
            });
        }
    }
}
