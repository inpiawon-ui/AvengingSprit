using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.Core.Module.Input
{
    /// <summary>
    /// Unity 새 Input System 기반 구현체.
    /// Keyboard.current / Mouse.current를 사용합니다.
    /// Tick(deltaTime)은 InputModule(ITickable)이 매 프레임 위임 호출한다.
    /// </summary>
    public sealed class InputManager : IInputManager
    {
        private struct ActionBinding
        {
            public Key Primary;
            public Key Secondary;
        }

        private readonly Dictionary<string, ActionBinding> _bindings       = new();
        private readonly Dictionary<string, List<Action>>  _onPressed      = new();
        private readonly Dictionary<string, List<Action>>  _onReleased     = new();
        // Tick 중 콜백 목록 변경 대비 스냅샷 버퍼 — 재사용으로 ToArray() GC 제거
        private readonly List<Action>                       _callbackBuffer = new();

        // 유효한 Key 값 캐시 — Inspector 역직렬화 오류 방지
        private static readonly HashSet<int> _validKeys;
        static InputManager()
        {
            _validKeys = new HashSet<int>();
            foreach (Key k in Enum.GetValues(typeof(Key)))
                _validKeys.Add((int)k);
        }

        private static bool IsValidKey(Key key) =>
            key != Key.None && _validKeys.Contains((int)key);

        // ── IInputManager — 키보드 ────────────────────────────

        public bool GetKey    (Key key) => IsValidKey(key) && (Keyboard.current?[key].isPressed          ?? false);
        public bool GetKeyDown(Key key) => IsValidKey(key) && (Keyboard.current?[key].wasPressedThisFrame ?? false);
        public bool GetKeyUp  (Key key) => IsValidKey(key) && (Keyboard.current?[key].wasReleasedThisFrame ?? false);

        // ── IInputManager — 마우스 ────────────────────────────

        public Vector2 GetMousePosition() => Mouse.current?.position.ReadValue() ?? Vector2.zero;
        public Vector2 GetMouseDelta()    => Mouse.current?.delta.ReadValue()    ?? Vector2.zero;

        public bool GetMouseButton(int button) => button switch
        {
            0 => Mouse.current?.leftButton.isPressed   ?? false,
            1 => Mouse.current?.rightButton.isPressed  ?? false,
            2 => Mouse.current?.middleButton.isPressed ?? false,
            _ => false,
        };

        public bool GetMouseButtonDown(int button) => button switch
        {
            0 => Mouse.current?.leftButton.wasPressedThisFrame   ?? false,
            1 => Mouse.current?.rightButton.wasPressedThisFrame  ?? false,
            2 => Mouse.current?.middleButton.wasPressedThisFrame ?? false,
            _ => false,
        };

        // ── IInputManager — 액션 ──────────────────────────────

        public void RegisterAction(string actionName, Key primaryKey, Key secondaryKey = Key.None)
        {
            _bindings[actionName] = new ActionBinding { Primary = primaryKey, Secondary = secondaryKey };
        }

        public void UnregisterAction(string actionName)
        {
            _bindings.Remove(actionName);
            _onPressed.Remove(actionName);
            _onReleased.Remove(actionName);
        }

        public bool IsActionHeld(string actionName)
        {
            if (!_bindings.TryGetValue(actionName, out var b)) return false;
            return GetKey(b.Primary) || (b.Secondary != Key.None && GetKey(b.Secondary));
        }

        public bool IsActionPressed(string actionName)
        {
            if (!_bindings.TryGetValue(actionName, out var b)) return false;
            return GetKeyDown(b.Primary) || (b.Secondary != Key.None && GetKeyDown(b.Secondary));
        }

        public bool IsActionReleased(string actionName)
        {
            if (!_bindings.TryGetValue(actionName, out var b)) return false;
            return GetKeyUp(b.Primary) || (b.Secondary != Key.None && GetKeyUp(b.Secondary));
        }

        public IDisposable OnActionPressed (string actionName, Action callback) =>
            AddCallback(_onPressed, actionName, callback);

        public IDisposable OnActionReleased(string actionName, Action callback) =>
            AddCallback(_onReleased, actionName, callback);

        // ── Tick (CoreBootstrap.Update()에서 매 프레임 호출) ───
        // 새 Input System은 wasPressedThisFrame을 자체 관리하므로
        // 콜백 발행만 담당합니다.

        public void Tick(float deltaTime)
        {
            foreach (var (actionName, b) in _bindings)
            {
                if (GetKeyDown(b.Primary) || (b.Secondary != Key.None && GetKeyDown(b.Secondary)))
                    InvokeCallbacks(_onPressed, actionName);

                if (GetKeyUp(b.Primary) || (b.Secondary != Key.None && GetKeyUp(b.Secondary)))
                    InvokeCallbacks(_onReleased, actionName);
            }
        }

        // ── 내부 ──────────────────────────────────────────────

        private IDisposable AddCallback(Dictionary<string, List<Action>> map, string actionName, Action callback)
        {
            if (!map.TryGetValue(actionName, out var list))
            {
                list = new List<Action>();
                map[actionName] = list;
            }
            list.Add(callback);
            return new Unsubscriber(() => list.Remove(callback));
        }

        private void InvokeCallbacks(Dictionary<string, List<Action>> map, string actionName)
        {
            if (!map.TryGetValue(actionName, out var list)) return;
            _callbackBuffer.Clear();
            _callbackBuffer.AddRange(list);
            for (int i = 0; i < _callbackBuffer.Count; i++)
                _callbackBuffer[i]?.Invoke();
            _callbackBuffer.Clear();
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly Action _dispose;
            public Unsubscriber(Action dispose) => _dispose = dispose;
            public void Dispose() => _dispose?.Invoke();
        }
    }
}
