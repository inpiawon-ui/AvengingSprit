using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 큰 연출 — 화면 섬광 · 줌 펀치 (2026-09-20).
    ///
    /// 스킬은 원작에 없던 것이라 「우와」가 나와야 하는데, 지금까지 쓸 수 있던 것은
    /// 흔들림(최대 6px)과 히트스톱뿐이었다. 그림만 화려해도 화면이 가만히 있으면 크게 안 느껴진다.
    ///
    /// ── 왜 이 두 가지인가 ────────────────────────────────────
    /// 이 게임은 전부 uGUI `Image` 다. 카메라 포스트프로세싱(블룸 · 화면 왜곡)이 닿지 않고
    /// 파티클도 안 쓴다. 그 안에서 큰 사건을 말할 수 있는 수단은
    ///   ① 화면 전체를 한 색으로 덮었다 빼기(섬광)
    ///   ② 화면을 확 당겼다 놓기(줌 펀치)
    /// 둘뿐이고, 둘 다 **새 그림이 필요 없다.**
    ///
    /// ⚠ 둘 다 **실제 시간**(`unscaledDeltaTime`)으로 센다. 히트스톱이 도는 동안에도
    ///   섬광이 빠져야 하고, 시전 정지(`_castFreezeLeft`) 중에도 줌이 돌아와야 한다.
    ///
    /// ⚠ 섬광은 방 창(`_field`) 안에만 깐다. 화면 전체를 덮으면 상단 HUD 와 조작 버튼까지
    ///   하얗게 날아가 「무슨 일이 난 건지」가 아니라 「화면이 고장났나」로 읽힌다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 화면 섬광 ────────────────────────────────────────────

        /// <summary>섬광이 가장 밝을 때의 알파 상한. 1 로 채우면 방이 통째로 사라져 자리를 잃는다.</summary>
        private const float FlashMaxAlpha = 0.55f;

        private Image _screenFlash;
        private float _flashLeft;      // 남은 시간
        private float _flashSpan;      // 전체 시간
        private float _flashPeak;      // 이번 섬광의 최대 알파

        /// <summary>
        /// 방 창을 한 색으로 덮었다 뺀다. <paramref name="color"/> 는 그 스킬의 색
        /// (`GameConfig.CastColorOf`)을 그대로 쓴다 — 무엇이 터졌는지가 색으로 읽힌다.
        /// </summary>
        private void ScreenFlash(Color color, float seconds = 0.22f, float peak = FlashMaxAlpha)
        {
            if (_field == null) return;
            if (_screenFlash == null)
            {
                var go = new GameObject("ScreenFlash", typeof(RectTransform)) { layer = _field.gameObject.layer };
                var rt = (RectTransform)go.transform;
                rt.SetParent(_field, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                _screenFlash = go.AddComponent<Image>();
                _screenFlash.raycastTarget = false;
            }
            // 방 위에 그린다. 새로 생긴 이펙트가 뒤에 붙으므로 매번 맨 앞으로 올린다.
            _screenFlash.transform.SetAsLastSibling();
            _flashPeak = Mathf.Clamp01(peak);
            _flashSpan = Mathf.Max(0.01f, seconds);
            _flashLeft = _flashSpan;
            _screenFlash.color = new Color(color.r, color.g, color.b, _flashPeak);
            _screenFlash.enabled = true;
        }

        /// <summary>⚠ 실제 시간으로 센다 — 히트스톱 중에도 빠져야 한다.</summary>
        private void TickScreenFlash()
        {
            if (_screenFlash == null || _flashLeft <= 0f) return;
            _flashLeft -= Time.unscaledDeltaTime;
            if (_flashLeft <= 0f)
            {
                _flashLeft = 0f;
                _screenFlash.enabled = false;
                return;
            }
            // 확 밝아졌다 천천히 빠진다 — 제곱으로 떨어뜨리면 번쩍임이 남는 느낌이 된다
            float t = _flashLeft / _flashSpan;
            var c = _screenFlash.color;
            _screenFlash.color = new Color(c.r, c.g, c.b, _flashPeak * t * t);
        }

        // ── 줌 펀치 ──────────────────────────────────────────────
        //
        // 화면을 확 당겼다 제자리로 놓는다. `Place` 가 레이어 크기를 정할 때 이 값을 곱한다.
        // 판정(`RoomToView`)에는 넣지 않는다 — 보이는 것만 흔들고 규칙은 그대로 둬야
        // 「맞았는데 안 맞았다」가 안 생긴다.

        private const float ZoomPunchDecay = 4.5f;   // 초당 빠지는 비율

        private float _zoomPunch;

        /// <summary>양수면 당기고 음수면 민다. 0.05 가 「화면이 한 번 숨을 쉬는」 정도다.</summary>
        private void ZoomPunch(float amount)
            => _zoomPunch = Mathf.Clamp(Mathf.Max(_zoomPunch, amount), -0.2f, 0.2f);

        /// <summary>⚠ 실제 시간으로 센다 — 시전 정지 중에도 돌아와야 한다.</summary>
        private void TickZoomPunch()
        {
            if (Mathf.Approximately(_zoomPunch, 0f)) return;
            _zoomPunch *= Mathf.Exp(-ZoomPunchDecay * Time.unscaledDeltaTime);
            if (Mathf.Abs(_zoomPunch) < 0.001f) _zoomPunch = 0f;
            ApplyScroll();
        }

        /// <summary>방을 나가거나 판이 끝날 때 — 섬광과 줌을 원래대로.</summary>
        private void ClearBigJuice()
        {
            _pfx?.ClearAll();   // 남은 알갱이가 다음 방에 떠 있으면 안 된다
            _flashLeft = 0f;
            if (_screenFlash != null) _screenFlash.enabled = false;
            if (_zoomPunch != 0f) { _zoomPunch = 0f; ApplyScroll(); }
        }
    }
}

