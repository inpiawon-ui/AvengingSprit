using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 필드가 상단 HUD 아래 **화면 끝까지** 내려간다 (2026-09-11).
    ///
    /// ── 조작은 필드 위에 뜬다 ──────────────────────────────
    /// 예전에는 하단 조작판(격자 `ControlGrid`)이 화면 아래 414 칸을 따로 먹고
    /// 필드는 그 위에 남는 칸만 썼다. 16:9 에서 필드가 800 이라 방(936)이 늘 스크롤됐다.
    /// 기획(2026-09-11) — 「하단 UI 단을 전체 배경으로 쓰고 HUD 는 배경 위에 띄운다」.
    /// 판을 걷고 필드를 아래까지 내렸다. D패드·빙의 버튼은 방 위에 떠 있다.
    ///
    /// ⚠ **방보다 크게는 못 키운다.** 방 밖에는 그릴 것이 없다. 방 아래 남는 칸은
    ///   `RoomApron` 이 「방 밖 바닥」 그림으로 채운다(16:9 에서 114, 20:9 에서 434 칸).
    ///
    /// ⚠ **가로는 절대 안 건드린다.** `_pxPerMeter` 가 필드 **폭**에서 나오므로
    ///   폭을 바꾸면 픽셀/미터가 달라져 사거리·이동 속도·캐릭터 크기가 전부 어긋난다.
    ///   그래서 필드에는 `ScreenFitLock` 이 붙어 있고, 크기는 여기서만 잰다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>필드 윗변이 화면 위에서 얼마나 내려와 있나(= 상단 HUD 몫). 안 바뀐다.</summary>
        private float _fieldTopOffset = -1f;

        /// <summary>마지막으로 맞춘 부모 크기. 화면이 바뀌면 방 밖 자리도 다시 놓는다.</summary>
        private Vector2 _fitParentSize = new(-1f, -1f);

        /// <summary>부팅 때 한 번 읽는다. 필드는 위에 못 박혀 있다(`aY = 1`, `pivot.y = 1`).</summary>
        private void CaptureFieldTop()
        {
            if (_fieldTopOffset >= 0f || _field == null) return;
            _fieldTopOffset = -_field.anchoredPosition.y;
        }

        /// <summary>
        /// 매 프레임 부른다. 값이 그대로면 안에서 바로 빠져나오므로 싸다.
        ///
        /// ⚠ **`Screen` 크기로 판단하면 안 된다.** 해상도가 바뀌는 그 프레임에는
        ///   `Screen` 은 이미 새 값인데 캔버스 배율은 아직 옛 값이라, 부모 높이가
        ///   한 프레임 동안 엉뚱하게 읽힌다. 캔버스 값(부모 높이)만 보고 매 프레임 다시 잰다.
        /// </summary>
        private void TickFieldFit() => FitFieldHeight();

        private void FitFieldHeight()
        {
            CaptureFieldTop();
            if (_field == null || _fieldTopOffset < 0f) return;
            if (_field.parent is not RectTransform parent) return;

            float want = parent.rect.height - _fieldTopOffset;           // HUD 아래 화면 끝까지
            if (_roomSize.y > 0f) want = Mathf.Min(want, _roomSize.y);  // 방보다 크게는 안 된다
            if (want <= 0f) return;

            bool changed = Mathf.Abs(_field.sizeDelta.y - want) > 0.5f;
            if (changed)
            {
                _field.sizeDelta = new Vector2(_field.sizeDelta.x, want);
                ApplyScroll();   // 창이 커졌으면 방을 다시 물려 놓는다
            }
            // 폭도 본다 — 태블릿으로 넓어지면 높이는 그대로여도 벽을 켜야 한다.
            var parentSize = parent.rect.size;
            if (!changed && (_fitParentSize - parentSize).sqrMagnitude < 0.25f) return;
            _fitParentSize = parentSize;

            // 방 밖 자리(좌우 벽 · 방 아래 바닥)는 필드를 따라간다.
            // 벽은 켜고 끄는 것까지 다시 본다 — 화면이 넓어졌을 수 있다.
            RefreshRoomSides();
            LayoutRoomApron();
        }
    }
}
