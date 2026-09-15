using GameFramework.Core.Base;
using GameFramework.Core.Module.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// ESC · 안드로이드 백 키를 받아 06_ui.md 의 뒤로가기 우선순위대로 흘려보낸다.
    ///
    ///   1. SystemPopup 열림      → SystemPopup 닫기
    ///   2. Popup / Panel 열림    → 같은 오브젝트의 `IBackTarget` 에 위임
    ///   3. MainUI 만 남은 상태   → 게임 종료 확인 SystemPopup
    ///
    /// `~UI` 가 Awake 에서 자기 오브젝트에 붙인다. 씬마다 하나만 존재한다.
    /// </summary>
    public sealed class BackButtonRouter : MonoBehaviour
    {
        private IBackTarget _target;
        private IInputManager _input;

        private void Awake()
        {
            _target = GetComponent<IBackTarget>();
            CoreModule.TryGet(out _input);
        }

        private void Update()
        {
            if (!WasBackPressed()) return;

            if (SystemPopup.IsShowing) { SystemPopup.Close(); return; }
            if (_target != null && _target.OnBackPressed()) return;

            SystemPopup.Show(Localize.Get("ui.common.quit.message"), Application.Quit,
                             Localize.Get("ui.common.quit"), Localize.Get("ui.common.cancel"));
        }

        /// <summary>
        /// InputModule 이 등록돼 있으면 그쪽을 쓰고, 없으면 Keyboard 를 직접 본다.
        /// 뒤로가기는 모듈 등록 여부와 무관하게 항상 동작해야 한다.
        /// </summary>
        private bool WasBackPressed()
        {
            if (_input != null) return _input.GetKeyDown(Key.Escape);
            var kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
        }
    }
}
