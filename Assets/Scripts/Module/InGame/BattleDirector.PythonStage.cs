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
            _pyDying = null;
            _pyBodySpeed = 1f;
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
            // 굵기가 다른 세 장을 머리 쪽부터 벽 쪽으로 가늘게 깐다.
            _neckArt = new[]
            {
                GetSprite("obj_python_neck_1"),   // 36 px — 머리 쪽
                GetSprite("obj_python_neck_2"),   // 30 px — 가운데
                GetSprite("obj_python_neck_3"),   // 24 px — 벽 쪽
            };
            _neck = new Image[12];
            for (int i = 0; i < _neck.Length; i++)
            {
                var ng = new GameObject($"Neck{i + 1}", typeof(RectTransform), typeof(Image));
                ng.transform.SetParent(_neckClip, false);
                var rt = (RectTransform)ng.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                // ⚠ pivot 이 아래변(0)이면 조각이 제 칸보다 **한 칸 위**에 그려진다 —
                //   0번은 통째로 위로 잘려 나가고 맨 아래 한 칸이 비어, 목이 머리에
                //   안 닿고 끊겨 보였다. 위변(1)이어야 제 칸을 채운다.
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(NeckTilePx, NeckTilePx);
                rt.anchoredPosition = new Vector2(0f, -i * NeckTilePx);
                var nimg = ng.GetComponent<Image>();
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
            TickPythonDeath(dt);
            if (_pyDying == null)
            {
                if (!IsShoving) TickHeadLunge(dt, _boss);
                TickBodyShove(dt);
                // 뻗는 중이거나 가로지르는 중에는 그쪽이 머리 그림을 쥔다
                if (!IsShoving && !IsLunging) TickHeadKick(dt, _boss);
            }
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

            // ⚠ 그림만 갈아 끼우면 성한 벽이 **한 프레임에 툭** 무너진 벽이 된다.
            //   무너진 칸에서 파편이 터지고 잔해가 떨어져야 "지금 무너졌다" 가 읽힌다.
            //   방에 처음 들어올 때(P1)는 무너진 것이 없으므로 건너뛴다.
            if (idx > 0) PlayCollapse(idx);
        }

        /// <summary>
        /// 이번 페이즈에 **새로 무너진 칸**에서 파편을 터뜨리고 잔해를 떨어뜨린다.
        /// 지난 페이즈에 살아 있었는데 이번에 빠진 구멍이 곧 무너진 자리다.
        /// </summary>
        private void PlayCollapse(int idx)
        {
            var before = ArchesByPhase[idx - 1];
            var after = ArchesByPhase[idx];
            for (int i = 0; i < before.Length; i++)
            {
                bool stillThere = false;
                for (int j = 0; j < after.Length; j++) if (after[j] == before[i]) stillThere = true;
                if (stillThere) continue;

                var at = DangerShape.ArchAtRoom(before[i], _roomSize);
                PlayFx("shatter", at, 168f, loop: false);
                DropRubble(at + new Vector2(-_pxPerMeter * 0.5f, -_pxPerMeter * 0.3f));
                DropRubble(at + new Vector2(_pxPerMeter * 0.5f, -_pxPerMeter * 0.6f));
            }
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
        private Sprite[] _pyOut, _pyIn, _pyDie;

        /// <summary>가로지를 때 쓰는 옆보기 머리 3장(입 다뭄 → 조금 → 활짝).</summary>
        private Sprite[] _pyCross;

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
            _pyCross = new Sprite[3];
            for (int i = 0; i < 3; i++) _pyCross[i] = UnitGet("python", $"e_cross{i + 1}");
            _pyOut = new Sprite[4];
            _pyIn = new Sprite[4];
            _pyDie = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                _pyOut[i] = UnitGet("python", $"s_out{i + 1}");
                _pyIn[i] = UnitGet("python", $"s_in{i + 1}");
                _pyDie[i] = UnitGet("python", $"s_die{i + 1}");
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

            // 머리 위 체력바는 벽 보스에서 방해만 된다 — 목을 뻗으면 목을 가로지른다.
            boss.SetHpBarVisible(false);
            boss.Position = new Vector2(ArchX(_wallArch), HeadY());
            _pyBodySpeed = 1f;
            Show(boss);
            SetHeadFrame(boss, _pyOut, 0f);
            _wallPhase = WallPhase.Emerge;
            _wallTimer = 0f;
        }

        /// <summary>이 방의 보스가 벽에 붙어 사는 보스인가.</summary>
        private bool IsWallBoss
        {
            get
            {
                var def = _brain != null ? _brain.Entry : null;
                return def != null && def.State == BossState.Walls;
            }
        }

        /// <summary>
        /// 지금 새 패턴을 시작해도 되는가.
        /// 벽 보스가 아니면 늘 참이다 — 이 규칙은 파이썬 하나에만 건다.
        /// </summary>
        private bool CanWallBossAct => !IsWallBoss || _wallPhase == WallPhase.Strike;

        // ── 머리 뻗기 ────────────────────────────────────────────
        //
        // 예고만 뜨고 아무것도 안 움직이면 「경고만 뜨고 끝」이 된다.
        // **머리가 그어 둔 띠 끝까지 실제로 내려갔다 돌아온다.**
        //
        // 목은 전용 그림 `obj_python_neck_1~3`(각 64×64)을 **세로로 이어 붙인다.**
        // 위아래 줄이 맞물리는 심리스라 몇 장을 쌓아도 이음매가 안 보인다.
        //
        // 굵기가 셋이다 — 머리 쪽 36 px, 가운데 30 px, 벽 쪽 24 px.
        // **한 장을 반복하면 굵기가 일정해 고무호스로 보인다.** 뻗을수록 밑동이
        // 가늘어져야 「잡아 늘였다」가 읽힌다. 길이에 상관없이 세 토막이
        // 같은 비율을 차지하도록 매 프레임 나눠 준다.
        //
        // ⚠ 예전에는 가로 몸통 그림을 90° 눕혀 썼다. 굵기가 72 px 로 두 배였고
        //   색도 초록이라, 머리(주황 목)와 안 이어지고 덩어리가 뚝뚝 끊겨 보였다.

        private const float LungeOutSeconds = 0.12f;
        private const float LungeHoldSeconds = 0.12f;
        private const float LungeBackSeconds = 0.30f;

        private float _lungeTimer;      // 0 이면 안 뻗고 있다
        private float _lungeDepth;      // 이번에 내려갈 거리(px)
        /// <summary>목 그림 한 장의 크기. 이만큼씩 세로로 쌓는다.</summary>
        private const float NeckTilePx = 64f;

        private RectTransform _neckClip;
        private Image[] _neck;
        /// <summary>[0]=머리 쪽 굵은 것 … [2]=벽 쪽 가는 것.</summary>
        private Sprite[] _neckArt;

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
            _neckClip.sizeDelta = new Vector2(NeckTilePx, len);
            _neckClip.anchoredPosition = new Vector2(ArchX(WallArch), -band);

            // 위(i=0)가 벽, 아래가 머리다. 보이는 토막을 셋으로 갈라
            // 벽 쪽부터 가는 것 → 굵은 것 순으로 깐다.
            if (_neckArt == null) return;
            int shown = Mathf.Clamp(Mathf.CeilToInt(len / NeckTilePx), 1, _neck.Length);
            for (int i = 0; i < _neck.Length; i++)
            {
                if (i >= shown) { _neck[i].enabled = false; continue; }
                // 벽에서 얼마나 내려왔는가 0~1 → 0 이면 벽 쪽(가는 것), 1 이면 머리 쪽(굵은 것)
                float t = shown <= 1 ? 1f : i / (float)(shown - 1);
                int art = t < 0.34f ? 2 : t < 0.67f ? 1 : 0;
                _neck[i].sprite = _neckArt[art];
                _neck[i].enabled = _neckArt[art] != null;
            }
        }

        // ── 몸통 밀기 ────────────────────────────────────────────
        //
        // 벽 뒤에 있던 몸이 **방 안으로 밀고 들어왔다 물러난다.**
        // P3 에서 늘 나와 있는 그 몸통 줄(`_pyDeepClip`)을 그대로 쓴다 —
        // 깊이만 0 에서 도형 두께까지 밀었다 되돌린다.

        private const float ShoveOutSeconds = 0.18f;
        private const float ShoveHoldSeconds = 0.35f;
        private const float ShoveBackSeconds = 0.45f;

        private float _shoveTimer;
        private float _shoveY, _shoveLane;
        private Unit _shoveHead;
        private Vector2 _shoveHome;

        private bool IsShoving => _shoveTimer > 0f;
        private float ShoveTotal => ShoveOutSeconds + ShoveHoldSeconds + ShoveBackSeconds;

        /// <param name="y">지나가는 줄의 한가운데(방 좌표).</param>
        /// <param name="lanePx">그 줄의 두께. 그린 도형과 같은 값이어야 한다.</param>
        private void BeginBodyShove(Unit boss, float y, float lanePx)
        {
            _shoveY = y;
            _shoveLane = Mathf.Max(_pxPerMeter, lanePx);
            _shoveTimer = ShoveTotal;
            _shoveHead = boss;
            _shoveHome = boss != null ? boss.Position : Vector2.zero;
            ShowNeck(0f);                 // 뻗은 목이 있으면 거둔다
        }

        /// <summary>
        /// 몸이 왼쪽 벽 밖에서 들어와 오른쪽 벽 밖으로 나가기까지의 진행도(0~1).
        /// </summary>
        private float ShoveProgress()
            => Mathf.Clamp01((ShoveTotal - _shoveTimer) / ShoveTotal);

        private void TickBodyShove(float dt)
        {
            if (_pyDeepClip == null) return;
            if (IsShoving) _shoveTimer = Mathf.Max(0f, _shoveTimer - dt);

            // ⚠ 미는 동안에는 **벽 뒤 몸을 끈다.** 둘 다 보이면 몸이 두 개인 것으로
            //   보여서 "저 몸이 나온 것" 이라는 느낌이 안 산다.
            bool hideBehind = IsShoving;
            if (_pyBody != null)
                for (int i = 0; i < _pyBody.Length; i++)
                    if (_pyBody[i].enabled == hideBehind) _pyBody[i].enabled = !hideBehind;

            float band = WallMeterHeight * _pxPerMeter;
            float tile = BodyTileMeters * _pxPerMeter;
            float lift = BodyArtTopPx * (band / BodyArtHeightPx);

            // ── 가로지르는 중 ────────────────────────────────
            //
            // **머리가 앞장서고 몸이 뒤를 따른다.** 몸만 지나가면 어디서 온 것인지
            // 알 수 없다 — 얼굴이 먼저 보여야 「저놈이 지나간다」가 된다.
            if (IsShoving)
            {
                float p = ShoveProgress();
                float headX = -tile * 0.5f + (_roomSize.x + tile) * p;

                _pyDeepClip.gameObject.SetActive(true);
                _pyDeepClip.sizeDelta = new Vector2(_roomSize.x, _shoveLane);
                _pyDeepClip.anchoredPosition = new Vector2(0f, _shoveY + _shoveLane * 0.5f);

                // 몸은 머리 **뒤쪽**으로만 이어진다 — 머리보다 앞서 나가지 않는다.
                //
                // ⚠ 여기서 `lift` 를 쓰면 안 된다. 그것은 몸을 **벽 아래에 붙일 때**
                //   그림 위쪽 여백(36px)을 걷어내는 보정값이다. 가로지를 때는 붙일
                //   벽이 없는데 그대로 써서 몸통 줄이 머리보다 36px 위로 떠 있었다 —
                //   화면에서는 머리와 몸이 어긋난 것으로 보였다.
                //   지나갈 때는 줄 한가운데(클립 위쪽 = 0)에 그대로 놓는다.
                for (int i = 0; i < _pyDeep.Length; i++)
                {
                    var rt = (RectTransform)_pyDeep[i].transform;
                    rt.sizeDelta = new Vector2(tile, _shoveLane);
                    rt.anchoredPosition = new Vector2(headX - tile * (i + 1), 0f);
                }

                // 머리를 그 줄에 태워 앞장세운다.
                //
                // ⚠ 한때 정면(s) 그림을 90° 돌려 썼다. **그러면 안 된다.**
                //   돌리면 얼굴의 명암이 몸통과 직각으로 어긋나고(몸은 등이 위·배가 아래,
                //   머리는 배가 오른쪽), 머리 두께도 몸통의 절반이 된다 —
                //   화면에서 머리와 몸이 따로 논다(기획 2026-09-07).
                //   옆을 보는 머리 그림 3장을 따로 받았다. 지나가는 동안 입을 여닫는다.
                if (_shoveHead != null && _shoveHead.IsAlive)
                {
                    _shoveHead.SetHidden(false);
                    SetCrossFrame(_shoveHead, p);
                    _shoveHead.Position = new Vector2(headX, _shoveY);
                }
                return;
            }

            // 지나간 뒤 — 머리를 제 구멍으로, 얼굴도 원래대로 돌려놓는다
            if (_shoveHead != null)
            {
                if (_shoveHead.IsAlive)
                {
                    SetHeadFrame(_shoveHead, _pyOut, 1f);
                    _shoveHead.Position = _shoveHome;
                }
                _shoveHead = null;
            }

            // ── P3 상시 노출 — 벽 아래에 붙어 있는다 ─────────
            if (_wallPhaseIndex < 2) { _pyDeepClip.gameObject.SetActive(false); return; }
            _pyDeepClip.gameObject.SetActive(true);
            _pyDeepClip.sizeDelta = new Vector2(_roomSize.x, DeepBodyMeters * _pxPerMeter);
            _pyDeepClip.anchoredPosition = new Vector2(0f, -band);
            for (int i = 0; i < _pyDeep.Length; i++)
            {
                var rt = (RectTransform)_pyDeep[i].transform;
                rt.sizeDelta = new Vector2(tile, band);
                rt.anchoredPosition = new Vector2(i * tile, lift);
            }
        }

        // ── 죽음 ─────────────────────────────────────────────────
        //
        // 그냥 두면 머리가 `out4` 자세로 **선 채 투명해지기만 한다.**
        // 방향별 die 그림은 있지만 옛 옆모습 시트라 벽 보스에 안 맞고,
        // 어차피 연출 그림(`SetSpriteOverride`)이 쥐고 있어 나오지도 않는다.
        //
        // **죽는 그림 `s_die1~4`** 를 쓴다 — 눈이 감기고 입이 벌어진 채 머리가 늘어진다.
        // 동시에 벽 뒤 몸이 멈추고 아치에서 잔해가 떨어진다.
        //
        // ⚠ 한때 들어가는 프레임(in1~4)을 돌려썼다. 웃는 얼굴로 되들어가서
        //   **죽은 것으로 안 보였다** — 이긴 순간인데 이겼다는 것이 안 읽혔다.
        //
        // 유닛 쪽 페이드가 0.16 + 0.50 = 0.66초라 그 안에 끝나야 한다.

        private const float DeathSlipSeconds = 0.55f;

        private Unit _pyDying;
        private float _pyDeathTimer;

        /// <summary>파이썬이 죽었다. 머리를 구멍으로 흘려 넣는다.</summary>
        private void BeginPythonDeath(Unit boss)
        {
            if (boss == null || !IsPythonRoom) return;
            _pyDying = boss;
            _pyDeathTimer = 0f;
            _lungeTimer = 0f;
            _shoveTimer = 0f;
            ShowNeck(0f);

            // 벽이 버티지 못하고 아치마다 잔해가 떨어진다.
            var live = LiveArches;
            for (int i = 0; i < live.Length; i++)
            {
                var at = DangerShape.ArchAtRoom(live[i], _roomSize);
                DropRubble(at + new Vector2(0f, -_pxPerMeter * 0.4f));
                PlayFx("shatter", at, 120f, loop: false);
            }
        }

        private void TickPythonDeath(float dt)
        {
            if (_pyDying == null) return;
            _pyDeathTimer += dt;

            // 몸이 서서히 멈춘다. 죽은 몸이 계속 흐르면 아직 살아 있는 것으로 보인다.
            _pyBodySpeed = Mathf.Max(0f, 1f - _pyDeathTimer / DeathSlipSeconds);

            var frames = _pyDie != null && _pyDie[0] != null ? _pyDie : _pyIn;
            if (frames != null)
                SetHeadFrame(_pyDying, frames, _pyDeathTimer / DeathSlipSeconds);

            if (_pyDeathTimer >= DeathSlipSeconds) _pyDying = null;
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

        /// <summary>
        /// 머리 뻗기는 **내가 선 x 축**에서 나와야 피할 수 있다.
        /// 예고 전에 그 줄에 가장 가까운 구멍으로 옮긴다 — 구멍은 벽에 박혀 있어
        /// 아무 데서나 나올 수는 없고, 그중 가장 가까운 곳을 고른다.
        ///
        /// ⚠ 도형을 만들기 **전에** 불러야 한다. 뒤에 옮기면 그린 줄과
        ///   나온 자리가 갈라진다.
        /// </summary>
        private void AimPythonAtPlayer(Unit boss, Unit me, BossMove m)
        {
            if (!IsWallBoss || boss == null || me == null || m == null) return;
            if (m.Draw != BossDraw.HeadLunge) return;
            if (_wallPhase != WallPhase.Strike || boss.IsHidden) return;

            var live = LiveArches;
            int best = _wallArch < 0 ? live[0] : _wallArch;
            float bestGap = float.MaxValue;
            for (int i = 0; i < live.Length; i++)
            {
                float gap = Mathf.Abs(ArchX(live[i]) - me.Position.x);
                if (gap >= bestGap) continue;
                bestGap = gap; best = live[i];
            }
            if (best == _wallArch) return;

            _wallArch = best;
            boss.Position = new Vector2(ArchX(best), HeadY());
        }

        // ── 스킬을 쓸 때의 머리 동작 ─────────────────────────────
        //
        // 파이썬은 **스킬 자세 그림이 없다.** 그래서 무슨 스킬을 써도 머리가
        // `out4` 한 장으로 가만히 있었다 — 「경고만 뜨고 아무 일도 안 일어난다」의
        // 정체가 이것이다.
        //
        // 다만 나오는 4단계(`out1~4`)가 곧 **머리가 구멍 안팎으로 오가는 그림**이다.
        // 뒤로 뺐다(→ out1 쪽) 앞으로 내치면(→ out4) 「젖혔다 친다」가 된다.
        // 새 그림 없이 있는 그림으로 진짜 동작을 만든다.

        private const float KickBackSeconds = 0.12f;   // 뒤로 젖히는 시간
        private const float KickHitSeconds = 0.10f;    // 앞으로 내치는 시간
        /// <summary>젖힐 때 얼마나 들어가는가. 0 이면 구멍 속, 1 이면 다 나온 자리.</summary>
        private const float KickDepth = 0.35f;

        private float _kickTimer;

        private float KickTotal => KickBackSeconds + KickHitSeconds;

        /// <summary>스킬이 나가는 순간 머리를 한 번 휘두른다.</summary>
        private void BeginHeadKick()
        {
            if (!IsWallBoss) return;
            _kickTimer = KickTotal;
        }

        private void TickHeadKick(float dt, Unit boss)
        {
            if (_kickTimer <= 0f || boss == null || boss.IsHidden) return;
            _kickTimer -= dt;

            float t = KickTotal - _kickTimer;                       // 시작부터 흐른 시간
            float f = t <= KickBackSeconds
                ? Mathf.Lerp(1f, KickDepth, t / KickBackSeconds)    // 젖힌다 — 천천히
                : Mathf.Lerp(KickDepth, 1f,
                             (t - KickBackSeconds) / KickHitSeconds); // 친다 — 빠르게
            SetHeadFrame(boss, _pyOut, Mathf.Clamp01(f) * 0.999f);

            if (_kickTimer <= 0f) { _kickTimer = 0f; SetHeadFrame(boss, _pyOut, 1f); }
        }

        /// <summary>
        /// 지나가는 동안 입을 여닫는다. 한 번 지나가는 사이 두 번 문다 —
        /// 그림이 세 장뿐이라 여닫이를 반복해야 살아 있는 것으로 보인다.
        /// </summary>
        private void SetCrossFrame(Unit boss, float progress)
        {
            if (_pyCross == null || _pyCross[0] == null) return;
            float cycle = Mathf.Repeat(progress * 2f, 1f);          // 두 번 반복
            int i = cycle < 0.34f ? 0 : cycle < 0.67f ? 1 : 2;      // 다뭄 → 조금 → 활짝
            if (_pyCross[i] != null) boss.SetSpriteOverride(_pyCross[i]);
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
