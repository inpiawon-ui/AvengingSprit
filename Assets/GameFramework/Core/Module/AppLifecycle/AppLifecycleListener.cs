using System;
using UnityEngine;

namespace GameFramework.Core.Module.AppLifecycle
{
    /// <summary>
    /// Unity MonoBehaviour 메시지를 수신하여 AppLifecycleManager로 전달한다.
    /// AppLifecycleModule이 DontDestroyOnLoad GameObject에 부착하여 사용한다.
    /// </summary>
    public sealed class AppLifecycleListener : MonoBehaviour
    {
        private Action<bool> _onPause;
        private Action<bool> _onFocus;

        /// <summary>
        /// Manager의 콜백을 주입한다. MonoBehaviour 생성 후 반드시 호출해야 한다.
        /// </summary>
        public void Initialize(Action<bool> onPause, Action<bool> onFocus)
        {
            _onPause = onPause;
            _onFocus = onFocus;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            _onPause?.Invoke(pauseStatus);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _onFocus?.Invoke(hasFocus);
        }
    }
}
