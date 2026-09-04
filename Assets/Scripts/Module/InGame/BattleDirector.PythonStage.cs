using System;
using System.Collections.Generic;
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

        /// <summary>벽 띠의 높이. 그림이 720×144 이고 방 폭이 10 m 이므로 정확히 2 m 다.</summary>
        private const float WallMeterHeight = 2f;

        /// <summary>몸통 한 칸의 폭(m). 그림이 240 px = 3.333 m 다.</summary>
        private const float BodyTileMeters = 240f / 72f;

        /// <summary>벽 뒤 몸통이 한 칸(40 px) 흐르는 데 걸리는 시간. 평소에는 느릿하다.</summary>
        private const float BodyIdleStepSeconds = 0.22f;

        private RectTransform _pyStage;
        private Image[] _pyBody;
        private Image[] _pyDeep;   // 벽 아래로 비어져 나온 몸통 — P3 에서만 보인다
        private RectTransform _pyDeepClip;

        // 몸통 그림(240×144)에서 **실제 몸은 y 36~108** 이다 — 위아래 36 px 은 비어 있다.
        // 이 값을 모르면 벽 아래로 낸 몸이 바닥 한 줄만큼 떠서 벽과 안 붙어 보인다.
        private const float BodyArtHeightPx = 144f;
        private const float BodyArtTopPx = 36f;

        /// <summary>벽 아래로 몸이 비어져 나오는 깊이. 그림의 몸 두께(72 px = 1 m)와 같다.</summary>
        private const float DeepBodyMeters = 1f;
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
                ClearRubble();
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
            _wallPhaseIndex = -1;             // 아래에서 반드시 한 번 걸리게 한다
            ApplyWallPhase(1);
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
            _pyBody1 = GetSprite("obj_python_body_1");
            _pyBody2 = GetSprite("obj_python_body_2");
            int tiles = Mathf.CeilToInt(RoomMeterWidth / BodyTileMeters) + 1;
            _pyBody = NewBodyRow(tiles, "Body");
            // 아래로 비어져 나온 몸. **잘라서** 낸다 — 그림은 한 줄 통째(2 m)라
            // 그대로 두면 방 위쪽 4 m 가 벽과 몸으로 덮인다.
            var clip = new GameObject("DeepClip", typeof(RectTransform), typeof(RectMask2D));
            clip.transform.SetParent(_pyStage, false);
            _pyDeepClip = (RectTransform)clip.transform;
            _pyDeepClip.anchorMin = _pyDeepClip.anchorMax = new Vector2(0f, 1f);
            _pyDeepClip.pivot = new Vector2(0f, 1f);
            _pyDeep = NewBodyRow(tiles, "Deep", _pyDeepClip);
            ShowDeepBody(false);

            // 목 — 벽 아래 끝과 머리를 잇는다. 벽보다 **아래**에 만들어야
            // 아치 안쪽에서 뻗어 나오는 것으로 보인다.
            var nc = new GameObject("NeckClip", typeof(RectTransform), typeof(RectMask2D));
            nc.transform.SetParent(_pyStage, false);
            _neckClip = (RectTransform)nc.transform;
            _neckClip.anchorMin = _neckClip.anchorMax = new Vector2(0f, 1f);
            _neckClip.pivot = new Vector2(0.5f, 1f);
            _neck = new Image[3];
            for (int i = 0; i < _neck.Length; i++)
            {
                var ng = new GameObject($"Neck{i + 1}", typeof(RectTransform), typeof(Image));
                ng.transform.SetParent(_neckClip, false);
                var rt = (RectTransform)ng.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                // 가로로 심리스한 몸통을 눕혀 세로 목으로 쓴다.
                rt.localEulerAngles = new Vector3(0f, 0f, 90f);
                var nimg = ng.GetComponent<Image>();
                nimg.sprite = _pyBody1;
                nimg.raycastTarget = false;
                _neck[i] = nimg;
            }
            _neckClip.gameObject.SetActive(false);

            // 벽 — 몸통 위, 유닛 아래. **마지막에 만들어야** 몸통 두 줄보다 위에 온다.
            var wg = new GameObject("Wall", typeof(RectTransform), typeof(Image));
            wg.transform.SetParent(_pyStage, false);
            var wrt = (RectTransform)wg.transform;
            wrt.anchorMin = wrt.anchorMax = new Vector2(0f, 1f);
            wrt.pivot = new Vector2(0f, 1f);
            _pyWall = wg.GetComponent<Image>();
            _pyWall.raycastTarget = false;
        }

        private Image[] NewBodyRow(int tiles, string name, RectTransform parent = null)
        {
            var row = new Image[tiles];
            for (int i = 0; i < tiles; i++)
            {
                var go = new GameObject($"{name}{i + 1}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent != null ? parent : _pyStage, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                var img = go.GetComponent<Image>();
                img.sprite = _pyBody1;
                img.raycastTarget = false;
                row[i] = img;
            }
            return row;
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

                // 비어져 나온 쪽은 잘라 내는 틀 **안에서** 위로 올려 둔다 —
                // 그림 위쪽 36 px 이 비어 있어 그대로 두면 벽과 몸 사이가 뜬다.
                var drt = (RectTransform)_pyDeep[i].transform;
                drt.sizeDelta = new Vector2(tile, band);
                drt.anchoredPosition = new Vector2(i * tile, BodyArtTopPx * (band / BodyArtHeightPx));
            }
            _pyDeepClip.sizeDelta = new Vector2(_roomSize.x, DeepBodyMeters * _pxPerMeter);
            _pyDeepClip.anchoredPosition = new Vector2(0f, -band);

            // 목 조각. 눕혀 놨으므로 가로가 길이, 세로가 굵기다.
            for (int i = 0; i < _neck.Length; i++)
            {
                var rt = (RectTransform)_neck[i].transform;
                rt.sizeDelta = new Vector2(tile, band);
                rt.anchoredPosition = new Vector2(0f, -(i * tile + tile * 0.5f));
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
            if (!IsPythonRoom) return;
            TickRubble(dt);
            TickHeadLunge(dt, _boss);
            TickBodyShove(dt);
            if (_pyBody == null || _pyBody1 == null || _pyBody2 == null) return;
            if (_pyBodySpeed <= 0f) return;

            _pyBodyTimer += dt * _pyBodySpeed;
            if (_pyBodyTimer < BodyIdleStepSeconds) return;
            _pyBodyTimer -= BodyIdleStepSeconds;
            _pyBodyFlip = !_pyBodyFlip;
            var art = _pyBodyFlip ? _pyBody2 : _pyBody1;
            for (int i = 0; i < _pyBody.Length; i++)
            {
                _pyBody[i].sprite = art;
                _pyDeep[i].sprite = art;
            }
        }

        /// <summary>
        /// 페이즈가 바뀌면 벽이 한 칸씩 무너진다. 그림도 구멍 목록도 함께 바뀐다.
        /// P3 에서는 몸통이 벽 아래로 한 줄 더 나와 방 안까지 들어온다.
        /// </summary>
        private void ApplyWallPhase(int phase)
        {
            if (_pyStage == null || !IsPythonRoom) return;
            int idx = Mathf.Clamp(phase - 1, 0, WallArtByPhase.Length - 1);
            if (idx == _wallPhaseIndex && _pyWall != null && _pyWall.sprite != null) return;
            _wallPhaseIndex = idx;

            // 무너진 칸에 머리가 나와 있으면 다음에 나올 때 살아 있는 칸으로 옮긴다.
            bool alive = false;
            var live = LiveArches;
            for (int i = 0; i < live.Length; i++) if (live[i] == _wallArch) alive = true;
            if (!alive) _wallArch = -1;

            LoadPythonWallAsync(WallArtByPhase[idx]).Forget();   // fire-and-forget: 늦게 와도 벽은 서 있다
            ShowDeepBody(idx >= 2);
        }

        /// <summary>
        /// 벽 아래로 몸통을 한 줄 더 깐다. P3 전용 —
        /// 벽이 반쯤 무너져 **몸이 방 안까지 밀려 들어온 것**이 보여야 한다.
        /// </summary>
        private void ShowDeepBody(bool on)
        {
            if (_pyDeepClip == null) return;
            _pyDeepClip.gameObject.SetActive(on);
        }

        private async UniTaskVoid LoadPythonWallAsync(string key)
        {
            if (_pyWall == null) return;
            var address = RoomFloorPrefix + key;
            if (address == _pyWallHeld) return;
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
            var old = _pyWallHeld;
            _pyWallHeld = address;
            if (old != null) CoreModule.Get<IResourceManager>().Release(old);
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

        // ⚠ 아치의 **가로 자리는 `DangerShape.ArchAtRoom` 하나만 본다.** 표가 갈라지면
        //   예고 도형과 머리가 다른 구멍을 가리킨다.
        //   세로(입구 높이)는 그림 규격이라 여기 둔다 — 벽 720×144 의 위에서 24 px.
        private const float WallArtWidth = 720f;
        private const float ArchTopPx = 24f;

        // ── 페이즈마다 벽이 무너진다 ─────────────────────────────
        //
        // 어느 칸이 무너지는지는 **벽 그림에 박혀 있다.** 실측(알파 0 구간):
        //   obj_python_wall         83~97 · 263~277 · 443~457 · 623~637   성한 네 칸
        //   obj_python_wall_break1  셋째 칸(450) 둘레가 401~501 까지 통째로 뚫린다
        //   obj_python_wall_break2  둘째·셋째(270·450)가 211~510 으로 함께 무너진다
        //
        // 무너진 칸에서는 머리가 안 나온다 — 거기는 이미 벽이 아니다.
        private static readonly string[] WallArtByPhase =
            { "obj_python_wall", "obj_python_wall_break1", "obj_python_wall_break2" };

        private static readonly int[][] ArchesByPhase =
        {
            new[] { 0, 1, 2, 3 },
            new[] { 0, 1, 3 },
            new[] { 0, 3 },
        };

        private int _wallPhaseIndex;   // 0=성함 1=한 칸 무너짐 2=두 칸 무너짐

        /// <summary>납품 규격 — 머리 그림의 꼭대기가 256 캔버스의 위에서 24 px 에 있다.</summary>
        private const float HeadTopPx = 24f;

        /// <summary>납품 규격 — 다 나온 머리(out4)의 아래 끝이 캔버스 220 px 에 있다.</summary>
        private const float HeadArtBottomPx = 220f;

        private WallPhase _wallPhase;
        private float _wallTimer;
        private int _wallArch = -1;
        private Sprite[] _pyOut, _pyIn;

        /// <summary>지금 머리가 나와 있는 아치. 스킬이 어디서 나가는지도 이 자리다.</summary>
        private int WallArch => _wallArch < 0 ? 0 : _wallArch;

        /// <summary>지금 페이즈에서 쓸 수 있는 아치 목록.</summary>
        private int[] LiveArches => ArchesByPhase[Mathf.Clamp(_wallPhaseIndex, 0, ArchesByPhase.Length - 1)];

        private float ArchX(int i) => DangerShape.ArchAtRoom(i, _roomSize).x;

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
                    // ⚠ 시험 모드에서는 **머리를 내놓은 채로 세워 둔다.**
                    //   버튼을 누른 순간 벽 뒤에 있으면 아무도 없는 구멍에 도형이 그려진다.
                    //   (한 번은 나왔다 서므로 나오는 연출도 그대로 볼 수 있다)
                    if (BossIdleOnly) break;
                    if (_wallTimer < StrikeSeconds) break;
                    // ⚠ 예고가 떠 있는 동안에는 안 들어간다. 그리다 만 도형을 두고
                    //   머리가 사라지면 무엇이 오는지 읽을 근거가 없어진다.
                    if (_brain != null && (_brain.IsTelegraphing || _dangerMove != null)) break;
                    if (IsLunging || IsShoving) break;   // 뻗은 목·민 몸이 돌아오기 전에는 못 들어간다
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
            var live = LiveArches;
            int k = _rng.Next(live.Length);
            if (live.Length > 1 && live[k] == _wallArch) k = (k + 1) % live.Length;
            _wallArch = live[k];

            boss.Position = new Vector2(ArchX(_wallArch), HeadY());
            _pyBodySpeed = 1f;
            Show(boss);
            SetHeadFrame(boss, _pyOut, 0f);
            _wallPhase = WallPhase.Emerge;
            _wallTimer = 0f;
        }

        /// <summary>
        /// 지금 새 패턴을 시작해도 되는가.
        /// 벽 보스가 아니면 늘 참이다 — 이 규칙은 파이썬 하나에만 건다.
        /// </summary>
        private bool CanWallBossAct
        {
            get
            {
                var def = _brain != null ? _brain.Entry : null;
                if (def == null || def.State != BossState.Walls) return true;
                return _wallPhase == WallPhase.Strike;
            }
        }

        // ── 머리 뻗기 ────────────────────────────────────────────
        //
        // 예고만 뜨고 아무것도 안 움직이면 「경고만 뜨고 끝」이 된다.
        // **머리가 그어 둔 띠 끝까지 실제로 내려갔다 돌아온다.**
        //
        // 목은 새 그림을 받지 않고 `obj_python_body` 를 **90° 눕혀** 잇는다 —
        // 가로로 심리스한 몸통이라 세로로 세우면 그대로 이어지는 목이 된다.
        // 굵기는 몸통 두께 그대로 1 m 이고, 머리(76~108 px)보다 살짝 좁아 자연스럽다.

        private const float LungeOutSeconds = 0.12f;
        private const float LungeHoldSeconds = 0.12f;
        private const float LungeBackSeconds = 0.30f;

        private float _lungeTimer;      // 0 이면 안 뻗고 있다
        private float _lungeDepth;      // 이번에 내려갈 거리(px)
        private RectTransform _neckClip;
        private Image[] _neck;

        /// <summary>지금 목을 뻗는 중인가. 이 동안에는 머리가 안 들어간다.</summary>
        private bool IsLunging => _lungeTimer > 0f;

        private float LungeTotal => LungeOutSeconds + LungeHoldSeconds + LungeBackSeconds;

        /// <summary>
        /// 머리 끝이 **띠의 끝**에 닿도록 내려갈 거리를 잰다.
        /// 그린 것과 닿는 것이 같아야 한다(R1) — 눈대중으로 정하면 둘이 갈라진다.
        /// </summary>
        private float LungeDepthFor(float bandLengthPx)
        {
            float box = 256f * BossScale * _config.UnitScale;
            float restTip = HeadY() + box * 0.5f - HeadArtBottomPx * (box / 256f);
            float bandEnd = -(DangerShape.WallBandDepth(_roomSize) + bandLengthPx);
            return Mathf.Max(0f, restTip - bandEnd);
        }

        private void BeginHeadLunge(float bandLengthPx)
        {
            _lungeDepth = LungeDepthFor(bandLengthPx);
            _lungeTimer = LungeTotal;
        }

        /// <summary>지금 얼마나 내려가 있는가(px). 나갈 때 빠르고 돌아올 때 느리다.</summary>
        private float LungeOffset()
        {
            float t = LungeTotal - _lungeTimer;                 // 시작부터 흐른 시간
            if (t <= LungeOutSeconds)
                return _lungeDepth * (t / LungeOutSeconds);
            if (t <= LungeOutSeconds + LungeHoldSeconds)
                return _lungeDepth;
            float back = (t - LungeOutSeconds - LungeHoldSeconds) / LungeBackSeconds;
            return _lungeDepth * (1f - Mathf.Clamp01(back));
        }

        private void TickHeadLunge(float dt, Unit boss)
        {
            if (!IsLunging) { ShowNeck(0f); return; }
            _lungeTimer -= dt;
            float off = _lungeTimer <= 0f ? 0f : LungeOffset();
            if (boss != null && !boss.IsHidden)
                boss.Position = new Vector2(boss.Position.x, HeadY() - off);
            ShowNeck(off);
            if (_lungeTimer <= 0f) { _lungeTimer = 0f; ShowNeck(0f); }
        }

        /// <summary>
        /// 벽 아래 끝과 머리 사이를 목으로 잇는다.
        /// 머리 그림 자체가 아치 안쪽을 이미 채우고 있으므로,
        /// 그만큼 넘게 내려갔을 때만 목이 필요하다.
        /// </summary>
        private void ShowNeck(float offset)
        {
            if (_neckClip == null) return;
            float box = 256f * BossScale * _config.UnitScale;
            float scale = box / 256f;
            float band = DangerShape.WallBandDepth(_roomSize);
            float inArch = band - HeadTopPx * scale;
            float len = offset - inArch;
            if (len <= 1f) { _neckClip.gameObject.SetActive(false); return; }

            _neckClip.gameObject.SetActive(true);
            _neckClip.sizeDelta = new Vector2(band, len);
            _neckClip.anchoredPosition = new Vector2(ArchX(WallArch), -band);
        }

        // ── 몸통 밀기 ────────────────────────────────────────────
        //
        // 벽 뒤에 있던 몸이 **방 안으로 밀고 들어왔다 물러난다.**
        // P3 에서 늘 나와 있는 그 몸통 줄(`_pyDeepClip`)을 그대로 쓴다 —
        // 깊이만 0 에서 도형 두께까지 밀었다 되돌린다.

        private const float ShoveOutSeconds = 0.18f;
        private const float ShoveHoldSeconds = 0.35f;
        private const float ShoveBackSeconds = 0.45f;

        private float _shoveTimer, _shoveDepth;

        private bool IsShoving => _shoveTimer > 0f;
        private float ShoveTotal => ShoveOutSeconds + ShoveHoldSeconds + ShoveBackSeconds;

        private void BeginBodyShove(float depthPx)
        {
            _shoveDepth = Mathf.Max(_pxPerMeter, depthPx);
            _shoveTimer = ShoveTotal;
        }

        private float ShoveOffset()
        {
            float t = ShoveTotal - _shoveTimer;
            if (t <= ShoveOutSeconds) return _shoveDepth * (t / ShoveOutSeconds);
            if (t <= ShoveOutSeconds + ShoveHoldSeconds) return _shoveDepth;
            float back = (t - ShoveOutSeconds - ShoveHoldSeconds) / ShoveBackSeconds;
            return _shoveDepth * (1f - Mathf.Clamp01(back));
        }

        private void TickBodyShove(float dt)
        {
            if (_pyDeepClip == null) return;
            if (IsShoving) _shoveTimer = Mathf.Max(0f, _shoveTimer - dt);

            // P3 는 평소에도 1 m 나와 있다. 미는 동안에는 둘 중 깊은 쪽을 쓴다.
            float baseDepth = _wallPhaseIndex >= 2 ? DeepBodyMeters * _pxPerMeter : 0f;
            float depth = Mathf.Max(baseDepth, IsShoving ? ShoveOffset() : 0f);
            if (depth <= 1f) { _pyDeepClip.gameObject.SetActive(false); return; }

            _pyDeepClip.gameObject.SetActive(true);
            _pyDeepClip.sizeDelta = new Vector2(_roomSize.x, depth);
        }

        // ── 무너진 벽돌 ──────────────────────────────────────────
        //
        // 「벽돌 낙하」가 지나간 자리에 잔해가 남는다. **밟아도 아프지 않다** —
        // 어디가 이미 무너졌는지를 보여 주는 표시다.
        // (막는 엄폐물로 할지는 아직 안 정했다. 지금은 표시만 한다.)

        private const float RubbleSeconds = 6f;
        private const float RubbleFadeSeconds = 1f;
        private const int MaxRubble = 12;

        private readonly List<Image> _rubble = new();
        private readonly List<float> _rubbleLeft = new();

        private void DropRubble(Vector2 at)
        {
            var art = GetSprite($"obj_python_rubble_{_rng.Next(3) + 1}");
            if (art == null || _fieldLayer == null) return;

            int slot = -1;
            for (int i = 0; i < _rubble.Count; i++)
                if (!_rubble[i].gameObject.activeSelf) { slot = i; break; }

            if (slot < 0 && _rubble.Count >= MaxRubble) slot = 0;   // 가장 오래된 것을 밀어낸다
            if (slot < 0)
            {
                var go = new GameObject("Rubble", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_fieldLayer, false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                _rubble.Add(img);
                _rubbleLeft.Add(0f);
                slot = _rubble.Count - 1;
            }

            var r = _rubble[slot];
            var rt = (RectTransform)r.transform;
            // 그림이 72×48 이다. 1 m 폭으로 방 좌표에 맞춰 놓는다.
            rt.sizeDelta = new Vector2(_pxPerMeter, _pxPerMeter * (48f / 72f));
            rt.anchoredPosition = at;
            r.sprite = art;
            r.color = Color.white;
            r.gameObject.SetActive(true);
            _rubbleLeft[slot] = RubbleSeconds;
        }

        private void TickRubble(float dt)
        {
            for (int i = 0; i < _rubble.Count; i++)
            {
                if (!_rubble[i].gameObject.activeSelf) continue;
                _rubbleLeft[i] -= dt;
                if (_rubbleLeft[i] <= 0f) { _rubble[i].gameObject.SetActive(false); continue; }
                if (_rubbleLeft[i] >= RubbleFadeSeconds) continue;
                var c = _rubble[i].color;
                _rubble[i].color = new Color(c.r, c.g, c.b, _rubbleLeft[i] / RubbleFadeSeconds);
            }
        }

        private void SetHeadFrame(Unit boss, Sprite[] frames, float t)
        {
            if (frames == null) return;
            int i = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
            if (frames[i] != null) boss.SetSpriteOverride(frames[i]);
        }

        private void ClearRubble()
        {
            for (int i = 0; i < _rubble.Count; i++) _rubble[i].gameObject.SetActive(false);
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
