using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.Core.Module.Input
{
    public interface IInputManager
    {
        // ── 키보드 직접 쿼리 ───────────────────────────────────
        bool GetKey    (Key key);
        bool GetKeyDown(Key key);
        bool GetKeyUp  (Key key);

        // ── 마우스 ────────────────────────────────────────────
        Vector2 GetMousePosition();
        Vector2 GetMouseDelta();
        bool    GetMouseButton    (int button);   // 0=좌, 1=우, 2=가운데
        bool    GetMouseButtonDown(int button);

        // ── 액션 바인딩 ────────────────────────────────────────
        void RegisterAction  (string actionName, Key primaryKey, Key secondaryKey = Key.None);
        void UnregisterAction(string actionName);

        bool IsActionHeld    (string actionName);
        bool IsActionPressed (string actionName);
        bool IsActionReleased(string actionName);

        IDisposable OnActionPressed (string actionName, Action callback);
        IDisposable OnActionReleased(string actionName, Action callback);
    }
}
