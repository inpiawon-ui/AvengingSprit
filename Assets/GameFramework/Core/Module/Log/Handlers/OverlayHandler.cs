#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Core.Module.Log.Handlers
{
    /// <summary>
    /// 화면 우측 상단에 최근 N줄을 링 버퍼로 표시하는 핸들러.
    /// DEVELOPMENT_BUILD / UNITY_EDITOR 전용.
    /// </summary>
    public sealed class OverlayHandler : ILogHandler
    {
        private readonly int            _maxLines;
        private readonly Queue<string>  _lines;

        private GUIStyle _style;
        private bool     _guiSubscribed;

        public OverlayHandler(int maxLines = 20)
        {
            _maxLines = maxLines;
            _lines    = new Queue<string>(maxLines);
        }

        public void Handle(LogEntry entry)
        {
            if (_lines.Count >= _maxLines)
                _lines.Dequeue();
            _lines.Enqueue(entry.ToString());
        }

        /// <summary>
        /// MonoBehaviour.OnGUI() 에서 호출하거나, OverlayRenderer 컴포넌트를 씬에 추가해 사용.
        /// </summary>
        public void DrawGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 11,
                    alignment = TextAnchor.UpperRight,
                    wordWrap  = false,
                };
                _style.normal.textColor = Color.white;
            }

            float y = 4f;
            foreach (var line in _lines)
            {
                GUI.Label(new Rect(0, y, Screen.width - 4, 16), line, _style);
                y += 16f;
            }
        }
    }
}
#endif
