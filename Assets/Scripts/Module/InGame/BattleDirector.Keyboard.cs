using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Module.InGame
{
    /// <summary>
    /// ⚠⚠ **임시 — 테스트 전용이다. 지울 때는 이 파일만 지우면 된다.** (2026-09-10)
    ///
    /// WASD(와 방향키)로 걸을 수 있게 한다. 에디터에서 손가락 대신 키보드로
    /// 움직여 보려고 붙였다.
    ///
    /// ── 지우는 법 ────────────────────────────────────────────
    ///   1. 이 파일과 `.meta` 를 지운다
    ///   2. `BattleDirector.Update` 의 `TickKeyboardMove();` 한 줄을 지운다
    /// 그게 전부다. 다른 곳은 건드리지 않았다.
    ///
    /// ── 왜 조이스틱을 덮어쓰는가 ─────────────────────────────
    /// 조이스틱은 손을 뗄 때 `MoveInput = Vector2.zero` 를 넣는다. 키보드가
    /// 그보다 **뒤에** 값을 넣어야 눌린 키가 이긴다. 키를 하나도 안 누르면
    /// 아무것도 안 건드리므로 조이스틱은 평소대로 동작한다.
    ///
    /// ⚠ 이 프로젝트는 새 Input System 이다. `UnityEngine.Input` 은 예외를 던진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private void TickKeyboardMove()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            var dir = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1f;

            // 하나도 안 눌렸으면 손대지 않는다 — 조이스틱이 넣어 둔 값을 지우면 안 된다.
            if (dir.sqrMagnitude < 0.0001f) return;

            // ⚠ 화면 좌표는 **아래가 음수**다(`_scroll` 주석 참조).
            //   W 를 눌렀을 때 위로 가려면 +y 가 맞고, 이동 쪽에서 그대로 쓴다.
            MoveInput = dir.normalized;
        }
    }
}
