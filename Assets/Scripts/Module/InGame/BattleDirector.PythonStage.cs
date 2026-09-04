using System;
using Cysharp.Threading.Tasks;
using Game.Character;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 파이썬(보스 3) 무대 — 방 위쪽 2 m 를 가로지르는 **벽**과 그 뒤로 흐르는 **몸통**.
    ///
    /// ── 왜 무대가 따로 필요한가 ──────────────────────────────────
    /// 파이썬은 원작에서 벽에 뚫린 구멍으로 머리만 내미는 보스다.
    /// 벽이 없으면 머리가 허공에서 나타났다 사라지는 순간이동이 된다 —
    /// 실제로 그렇게 보였고, 그래서 이 무대를 먼저 깐다.
    ///
    /// ── 층 순서 ────────────────────────────────────────────────
    ///   RoomFloor → **몸통 → 벽** → FieldLayer(장판) → UnitLayer(유닛)
    ///   몸통은 벽 **뒤**다. 아치 구멍(알파 0) 너머로만 보인다 —
    ///   벽이 뚫려 있으니 그 안에 뭔가 살아 움직인다는 것이 구멍으로만 읽힌다.
    ///
    /// ── 몸통이 흐르는 원리 ──────────────────────────────────────
    /// `obj_python_body_1` 은 **가로 주기가 80 px** 이고
    /// `obj_python_body_2` 는 그것을 정확히 **+40 px** 민 그림이다(반 주기).
    /// 그래서 둘을 번갈아 놓기만 하면 40 px 씩 오른쪽으로 끝없이 흐른다 —
    /// 되돌아오지 않는다. 판을 움직일 필요가 없어 좌표 계산도 필요 없다.
    /// (실측: 자기 자신을 +80 밀면 평균차 0.030, body_2 는 +40 에서 0.009)
    /// </summary>
    public sealed partial class BattleDirector
    {
        // 벽 3장은 720×144 라 아틀라스 밖이다. 방 바닥과 같은 그룹·같은 방식으로 낱장 로드한다.
        private const string PythonWallKey = "obj_python_wall";

        /// <summary>벽 띠의 높이. 그림이 720×144 이고 방 폭이 10 m 이므로 정확히 2 m 다.</summary>
        private const float WallMeterHeight = 2f;

        /// <summary>몸통 한 칸의 폭(m). 그림이 240 px = 3.333 m 다.</summary>
        private const float BodyTileMeters = 240f / 72f;

        /// <summary>벽 뒤 몸통이 한 칸(40 px) 흐르는 데 걸리는 시간. 평소에는 느릿하다.</summary>
        private const float BodyIdleStepSeconds = 0.22f;

        private RectTransform _pyStage;
        private Image[] _pyBody;
        private Image _pyWall;
        private Sprite _pyBody1, _pyBody2;
        private string _pyWallHeld;
        private float _pyBodyTimer;
        private bool _pyBodyFlip;

        /// <summary>벽 뒤 몸통이 흐르는 배속. 1 이 평소, 0 이면 멈춘다.</summary>
        private float _pyBodySpeed = 1f;

        /// <summary>이 방이 파이썬 방인가.</summary>
        private bool IsPythonRoom => _pyStage != null && _pyStage.gameObject.activeSelf;

        /// <summary>
        /// 이 보스의 무대를 건다. 벽 보스가 아니면 무대를 내린다.
        /// </summary>
        private void ApplyPythonStage(BossEntry def)
        {
            bool want = def != null && def.State == BossState.Walls;
            if (!want)
            {
                if (_pyStage != null) _pyStage.gameObject.SetActive(false);
                ReleasePythonWall();
                return;
            }

            EnsurePythonStage();
            _pyStage.gameObject.SetActive(true);
            LayoutPythonStage();
            // 방에 들어오면 **벽 뒤에서 시작한다.** 처음부터 나와 있으면
            // 어디서 나온 놈인지 못 보고 그냥 서 있는 보스가 된다.
            _wallPhase = WallPhase.Slide;
            _wallTimer = 0f;
            _wallArch = -1;
            _pyOut = null;   // 방마다 아틀라스가 다시 올라온다
            LoadPythonWallAsync().Forget();   // fire-and-forget: 벽 그림은 늦게 와도 무대는 먼저 선다
        }

        private void EnsurePythonStage()
        {
            if (_pyStage != null) return;

            var go = new GameObject("PythonStage", typeof(RectTransform));
            go.transform.SetParent(_unitLayer.parent, false);
            _pyStage = (RectTransform)go.transform;
            _pyStage.anchorMin = _pyStage.anchorMax = new Vector2(0f, 1f);
            _pyStage.pivot = new Vector2(0f, 1f);
            // 장판보다 **아래**. 여기 끼워 넣으면 장판·유닛이 한 칸씩 위로 밀린다.
            _pyStage.SetSiblingIndex(_fieldLayer != null
                ? _fieldLayer.GetSiblingIndex() : 1);

            // 몸통 — 방 폭을 덮는 칸 수 + 1. 벽 뒤라 아치 구멍으로만 보인다.
            int tiles = Mathf.CeilToInt(RoomMeterWidth / BodyTileMeters) + 1;
            _pyBody = new Image[tiles];
            _pyBody1 = GetSprite("obj_python_body_1");
            _pyBody2 = GetSprite("obj_python_body_2");
            for (int i = 0; i < tiles; i++)
            {
                var bg = new GameObject($"Body{i + 1}", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(_pyStage, false);
                var rt = (RectTransform)bg.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                var img = bg.GetComponent<Image>();
                img.sprite = _pyBody1;
                img.raycastTarget = false;
                _pyBody[i] = img;
            }

            // 벽 — 몸통 위, 유닛 아래.
            var wg = new GameObject("Wall", typeof(RectTransform), typeof(Image));
            wg.transform.SetParent(_pyStage, false);
            var wrt = (RectTransform)wg.transform;
            wrt.anchorMin = wrt.anchorMax = new Vector2(0f, 1f);
            wrt.pivot = new Vector2(0f, 1f);
            _pyWall = wg.GetComponent<Image>();
            _pyWall.raycastTarget = false;
        }

        /// <summary>방 크기가 정해진 뒤에 자리를 다시 잡는다.</summary>
        private void LayoutPythonStage()
        {
            if (_pyStage == null) return;
            _pyStage.sizeDelta = _roomSize;
            _pyStage.anchoredPosition = _unitLayer.anchoredPosition;

            float band = WallMeterHeight * _pxPerMeter;
            float tile = BodyTileMeters * _pxPerMeter;
            for (int i = 0; i < _pyBody.Length; i++)
            {
                var rt = (RectTransform)_pyBody[i].transform;
                rt.sizeDelta = new Vector2(tile, band);
                rt.anchoredPosition = new Vector2(i * tile, 0f);
            }
            var w = (RectTransform)_pyWall.transform;
            w.sizeDelta = new Vector2(_roomSize.x, band);
            w.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 벽 뒤 몸통을 흘린다. 두 장을 번갈아 놓으면 40 px 씩 한 방향으로 나아간다
        /// (클래스 주석의 주기 계산 참조).
        /// </summary>
        private void TickPythonStage(float dt)
        {
            if (!IsPythonRoom || _pyBody == null || _pyBody1 == null || _pyBody2 == null) return;
            if (_pyBodySpeed <= 0f) return;

            _pyBodyTimer += dt * _pyBodySpeed;
            if (_pyBodyTimer < BodyIdleStepSeconds) return;
            _pyBodyTimer -= BodyIdleStepSeconds;
            _pyBodyFlip = !_pyBodyFlip;
            var art = _pyBodyFlip ? _pyBody2 : _pyBody1;
            for (int i = 0; i < _pyBody.Length; i++) _pyBody[i].sprite = art;
        }

        private async UniTaskVoid LoadPythonWallAsync()
        {
            if (_pyWall == null || _pyWall.sprite != null) return;
            var address = RoomFloorPrefix + PythonWallKey;
            Sprite art = null;
            try { art = await CoreModule.Get<IResourceManager>().LoadAsync<Sprite>(address); }
            catch (Exception) { /* 아직 주소가 없으면 벽 없이 그냥 진행한다 */ }
            if (art == null) return;
            // 기다리는 사이에 방이 바뀌었으면 물지 않고 놓아 준다
            if (_pyWall == null || !IsPythonRoom)
            {
                CoreModule.Get<IResourceManager>().Release(address);
                return;
            }
            _pyWall.sprite = art;
            _pyWallHeld = address;
        }

        // ─────────────────────────────────────────────────────────
        // 나왔다 들어가는 주기
        //
        // 예전에는 **1.5초 사라졌다가 아무 벽 앞에 다시 나타났다.** 연출이 하나도 없어
        // 순간이동으로 보였다("보스가 지금 자꾸 순간이동을 하는데 왜 하는거야?").
        // 땅굴에 사는 놈이면 들어가는 것과 나오는 것이 보여야 한다.
        //
        //   나옴 0.4s → 물기·예고 1.0s → 들어감 0.4s → 벽 뒤 미끄러짐 0.8s → 반복
        //
        // 나와 있는 1.4초가 때릴 수 있는 시간이다(2.6초 중 54%).
        // 그 앞머리 1.2초가 취약 창 조건이기도 하다 — `BreakCause` 참조.

        private enum WallPhase { Emerge, Strike, Retreat, Slide }

        private const float EmergeSeconds  = 0.4f;
        private const float StrikeSeconds  = 1.0f;
        private const float RetreatSeconds = 0.4f;
        private const float SlideSeconds   = 0.8f;

        /// <summary>숨어서 자리를 옮기는 동안 벽 뒤 몸이 빨라진다 — 그것이 이동으로 읽힌다.</summary>
        private const float SlideBodySpeed = 3.5f;

        // 벽 그림(720×144) 에 픽셀로 박힌 자리다. 여기서 딴 값을 쓰면 머리가 벽을 뚫고 나온다.
        //   아치 구멍  x 30~150 · 210~330 · 390~510 · 570~690   y 24~144
        private const float WallArtWidth = 720f;
        private const float ArchTopPx = 24f;
        private static readonly float[] ArchCenterPx = { 90f, 270f, 450f, 630f };

        /// <summary>납품 규격 — 머리 그림의 꼭대기가 256 캔버스의 위에서 24 px 에 있다.</summary>
        private const float HeadTopPx = 24f;

        private WallPhase _wallPhase;
        private float _wallTimer;
        private int _wallArch = -1;
        private Sprite[] _pyOut, _pyIn;

        /// <summary>지금 머리가 나와 있는 아치. 스킬이 어디서 나가는지도 이 자리다.</summary>
        private int WallArch => _wallArch < 0 ? 0 : _wallArch;

        private float ArchX(int i) => _roomSize.x * (ArchCenterPx[i] / WallArtWidth);

        /// <summary>
        /// 머리 꼭대기(캔버스 24 px)가 아치 입구에 닿도록 유닛 중심을 내린다.
        /// 유닛은 상자 한가운데가 좌표라서 그 절반만큼 더 내려가야 한다.
        /// </summary>
        private float HeadY()
        {
            float box = 256f * BossScale * _config.UnitScale;
            float archTop = _roomSize.x * (ArchTopPx / WallArtWidth);
            return -archTop - (box * 0.5f - HeadTopPx * (box / 256f));
        }

        private void EnsurePythonHeadFrames()
        {
            if (_pyOut != null) return;
            _pyOut = new Sprite[4];
            _pyIn = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                _pyOut[i] = UnitGet("python", $"s_out{i + 1}");
                _pyIn[i] = UnitGet("python", $"s_in{i + 1}");
            }
        }

        /// <summary>
        /// 파이썬이 어디에 있는가. `TickBossPresence` 의 벽 보스 갈래가 이것만 부른다.
        /// </summary>
        private void TickPythonPresence(float dt, Unit boss)
        {
            EnsurePythonHeadFrames();
            _wallTimer += dt;

            switch (_wallPhase)
            {
                // ── 벽 뒤로 미끄러져 자리를 옮긴다 ────────────────
                case WallPhase.Slide:
                    _pyBodySpeed = SlideBodySpeed;
                    if (_wallTimer < SlideSeconds) break;
                    BeginEmerge(boss);
                    break;

                // ── 구멍에서 머리가 자라 나온다 ──────────────────
                case WallPhase.Emerge:
                    SetHeadFrame(boss, _pyOut, _wallTimer / EmergeSeconds);
                    if (_wallTimer < EmergeSeconds) break;
                    SetHeadFrame(boss, _pyOut, 1f);
                    _wallPhase = WallPhase.Strike;
                    _wallTimer = 0f;
                    break;

                // ── 다 나와 있다. 때릴 수 있고, 이때 패턴이 나간다 ──
                case WallPhase.Strike:
                    if (_wallTimer < StrikeSeconds) break;
                    _wallPhase = WallPhase.Retreat;
                    _wallTimer = 0f;
                    break;

                // ── 구멍으로 되돌아 들어간다 ────────────────────
                case WallPhase.Retreat:
                    SetHeadFrame(boss, _pyIn, _wallTimer / RetreatSeconds);
                    if (_wallTimer < RetreatSeconds) break;
                    Hide(boss, shadow: false);
                    boss.SetSpriteOverride(null);
                    _wallPhase = WallPhase.Slide;
                    _wallTimer = 0f;
                    break;
            }
        }

        /// <summary>
        /// 다음 구멍을 고르고 머리를 그 자리에 놓는다.
        /// **직전 구멍은 다시 고르지 않는다** — 같은 데서 두 번 나오면 옮긴 것이 안 읽힌다.
        /// </summary>
        private void BeginEmerge(Unit boss)
        {
            int next = _rng.Next(ArchCenterPx.Length);
            if (next == _wallArch) next = (next + 1) % ArchCenterPx.Length;
            _wallArch = next;

            boss.Position = new Vector2(ArchX(_wallArch), HeadY());
            _pyBodySpeed = 1f;
            Show(boss);
            SetHeadFrame(boss, _pyOut, 0f);
            _wallPhase = WallPhase.Emerge;
            _wallTimer = 0f;
        }

        private void SetHeadFrame(Unit boss, Sprite[] frames, float t)
        {
            if (frames == null) return;
            int i = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
            if (frames[i] != null) boss.SetSpriteOverride(frames[i]);
        }

        private void ReleasePythonWall()
        {
            if (_pyWallHeld == null) return;
            if (_pyWall != null) _pyWall.sprite = null;
            CoreModule.Get<IResourceManager>().Release(_pyWallHeld);
            _pyWallHeld = null;
        }
    }
}
