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

        private void ReleasePythonWall()
        {
            if (_pyWallHeld == null) return;
            if (_pyWall != null) _pyWall.sprite = null;
            CoreModule.Get<IResourceManager>().Release(_pyWallHeld);
            _pyWallHeld = null;
        }
    }
}
