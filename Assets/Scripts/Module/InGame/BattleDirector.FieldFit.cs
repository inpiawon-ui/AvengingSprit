using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 남는 세로 칸을 플레이 필드가 먹는다 (2026-09-10).
    ///
    /// ── 왜 남나 ─────────────────────────────────────────────
    /// 잘리지 않게 맞추는 규칙(contain) 때문에 폰 20:9(1080×2400)에서는 보이는 칸이
    /// **720 × 1600** 이 된다. 1280 으로 그린 화면보다 320 칸이 남는다.
    ///
    /// `ScreenFit` 이 상단 HUD 를 위에, 조작바를 아래에 붙이고 나면 그 320 칸이
    /// **필드와 조작바 사이에 검은 띠**로 남는다. 그냥 두면 화면 한가운데가 뚫린 것처럼 보인다.
    ///
    /// ── 어떻게 먹나 ─────────────────────────────────────────
    /// 필드를 그만큼 키운다 — 창이 커지면 **방이 더 보인다.** 세로 스크롤이 줄고,
    /// 폰에서는 방(13 m = 936 px) 전체가 한 화면에 들어온다.
    ///
    /// ⚠ **방보다 크게는 못 키운다.** 방 밖에는 그릴 것이 없어서, 창이 방보다 커지면
    ///   바닥 아래로 빈 자리가 드러난다. 그래서 방 높이에서 자른다.
    ///
    /// ⚠ **가로는 절대 안 건드린다.** `_pxPerMeter` 가 필드 **폭**에서 나오므로
    ///   폭을 바꾸면 픽셀/미터가 달라져 사거리·이동 속도·캐릭터 크기가 전부 어긋난다.
    ///   그래서 필드에는 `ScreenFitLock` 이 붙어 있고, 크기는 여기서만 잰다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>레이아웃을 그린 기준 세로. 9:16 의 높이다.</summary>
        private const float FieldBaseScreenHeight = 1280f;

        /// <summary>그려진 필드 높이. 부팅 때 한 번 떠 둔다.</summary>
        private float _fieldBaseHeight;

        /// <summary>필드 윗변이 화면 위에서 얼마나 내려와 있나(= 상단 HUD 몫). 안 바뀐다.</summary>
        private float _fieldTopOffset;

        /// <summary>조작바 안쪽을 바닥에 붙여 놓았나. 한 번만 하면 된다.</summary>
        private bool _controlPinned;

        /// <summary>부팅 때 그려진 높이를 떠 둔다. 이 값이 기준이라 한 번만 읽는다.</summary>
        private void CaptureFieldBaseHeight()
        {
            if (_fieldBaseHeight > 0f || _field == null) return;
            _fieldBaseHeight = _field.rect.height;
            // 필드는 위에 못 박혀 있다(`aY = 1`, `pivot.y = 1`). 그 내려온 거리다.
            _fieldTopOffset = -_field.anchoredPosition.y;
        }

        /// <summary>
        /// 매 프레임 부른다. 값이 그대로면 안에서 바로 빠져나오므로 싸다.
        ///
        /// ⚠ **`Screen` 크기로 판단하면 안 된다.** 해상도가 바뀌는 그 프레임에는
        ///   `Screen` 은 이미 새 값인데 캔버스 배율은 아직 옛 값이라, 부모 높이가
        ///   한 프레임 동안 엉뚱하게 크게 읽힌다. 그때 한 번 재고 끝내면 그 값이 굳는다 —
        ///   실제로 조작바가 화면 위로 튀어나올 만큼 커졌다(3000−230−936−20 = 1814).
        ///   부모가 제 크기를 찾을 때까지 계속 보게 둔다.
        /// </summary>
        private void TickFieldFit() => FitFieldHeight();

        private void FitFieldHeight()
        {
            CaptureFieldBaseHeight();
            if (_field == null || _fieldBaseHeight <= 0f) return;
            if (_field.parent is not RectTransform parent) return;

            // 그린 화면에서 필드 위아래에 있던 것(HUD·조작바)이 차지하던 몫.
            // 화면이 길어져도 저것들의 높이는 그대로이므로, 늘어난 만큼이 곧 필드 몫이다.
            float chrome = FieldBaseScreenHeight - _fieldBaseHeight;
            float available = parent.rect.height - chrome;

            float want = Mathf.Max(_fieldBaseHeight, available);
            if (_roomSize.y > 0f) want = Mathf.Min(want, _roomSize.y);   // 방보다 크게는 안 된다

            if (Mathf.Abs(_field.sizeDelta.y - want) > 0.5f)
            {
                _field.sizeDelta = new Vector2(_field.sizeDelta.x, want);
                ApplyScroll();   // 창이 커졌으면 방을 다시 물려 놓는다
            }

            FitControlBar(parent);
        }

        // ── 그러고도 남으면 조작바가 마저 먹는다 ────────────────
        //
        // 필드를 방 높이까지 키우고도 칸이 남는다(폰 20:9 에서 204 칸). 그냥 두면
        // 필드와 조작바 사이에 검은 띠가 생겨 화면 한가운데가 뚫린 것처럼 보인다.
        // 조작바를 필드 밑까지 끌어올려 그 자리를 격자 바닥으로 덮는다.
        //
        // ⚠ 조작바가 커지면 안쪽 것들을 **바닥에 붙여야** 한다. 그린 자리가 위쪽에 가까워서
        //   그대로 두면 D패드와 버튼이 위로 떠올라 엄지가 안 닿는다.
        //
        // ⚠ 배경 격자(`ControlGrid`)는 세로 스트레치라 저절로 따라 늘어난다. 여기서
        //   다시 붙이면 오히려 늘어나는 것을 막는다 — 그래서 스트레치인 놈은 건너뛴다.

        /// <summary>그린 화면에서 필드 아래변과 조작바 윗변 사이의 틈.</summary>
        private const float ControlGapBase = 20f;

        private RectTransform _controlBar;
        private float _controlBarBaseHeight;
        private readonly List<(RectTransform Rect, float Bottom)> _controlItems = new();

        private void CaptureControlBar(RectTransform parent)
        {
            if (_controlBar != null || parent == null) return;
            _controlBar = parent.Find("ControlGroup") as RectTransform;
            if (_controlBar == null) return;

            _controlBarBaseHeight = _controlBar.rect.height;
            foreach (Transform ch in _controlBar)
            {
                if (ch is not RectTransform rt) continue;
                bool stretched = rt.anchorMin.y < 0.01f && rt.anchorMax.y > 0.99f;
                if (stretched) continue;   // 배경 격자는 늘어나야 한다
                float bottom = rt.anchorMin.y * _controlBarBaseHeight
                             + rt.anchoredPosition.y - rt.rect.height * rt.pivot.y;
                _controlItems.Add((rt, bottom));
            }
        }

        private void FitControlBar(RectTransform parent)
        {
            CaptureControlBar(parent);
            if (_controlBar == null || _controlBarBaseHeight <= 0f) return;

            // ⚠ **월드 좌표로 재지 않는다.** 창 높이를 방금 바꾼 그 프레임에는 월드 모서리가
            //   아직 갱신 전이라, 한 번 크게 어긋난 값이 그대로 굳는다 —
            //   실제로 조작바가 화면 위로 튀어나올 만큼 커졌다.
            //   캔버스 값(부모 높이 · 필드 자리)만으로 셈한다.
            float below = parent.rect.height - _fieldTopOffset - _field.rect.height;
            float want = Mathf.Max(_controlBarBaseHeight, below - ControlGapBase);
            bool changed = Mathf.Abs(_controlBar.rect.height - want) > 0.5f;
            if (changed) _controlBar.sizeDelta = new Vector2(_controlBar.sizeDelta.x, want);
            if (!changed && _controlPinned) return;
            _controlPinned = true;

            for (int i = 0; i < _controlItems.Count; i++)
            {
                var (rt, bottom) = _controlItems[i];
                if (rt == null) continue;
                rt.anchorMin = new Vector2(rt.anchorMin.x, 0f);
                rt.anchorMax = new Vector2(rt.anchorMax.x, 0f);
                rt.pivot = new Vector2(rt.pivot.x, 0f);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, bottom);
            }
        }
    }
}
