using System;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Scene;
using UnityEngine;

namespace Game.Module.Title
{
    /// <summary>
    /// 타이틀 화면. 기능은 전면 탭 1종뿐이다.
    /// 원작 아케이드의 "화면 하나 · 프롬프트 하나" 를 따른다 — 버튼 위젯을 두지 않는다.
    /// </summary>
    public sealed class TitleMainUI : MonoBehaviour
    {
        private const float BlinkPeriod = 1.1f;

        private UIBinder _ui;
        private IDisposable _startToken;
        private Transform _tapText;
        private bool _transitioning;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _tapText = _ui.Find("TapToStartText");

            _ui.SetText("TapToStartText", "TAP TO START");
            _ui.SetText("CopyrightText", "©1991 JALECO / CITY CONNECTION");
            _ui.SetText("VersionText", $"v{Application.version}");

            _ui.OnClick("TouchArea", OnTapped);
        }

        private void OnEnable()
        {
            _startToken = CoreModule.Get<IEventBus>()
                .Subscribe<TitleStartRequestedEvent>(OnStartRequested);
        }

        private void OnDisable()
        {
            _startToken?.Dispose();
            _startToken = null;
        }

        private void Update()
        {
            if (_tapText == null) return;
            // 아케이드식 점멸 프롬프트. 알파만 흔들어 도트를 건드리지 않는다.
            float a = Mathf.PingPong(Time.unscaledTime / BlinkPeriod, 1f);
            var tmp = _tapText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                var c = tmp.color;
                c.a = Mathf.Lerp(0.25f, 1f, a);
                tmp.color = c;
            }
        }

        private void OnTapped()
        {
            if (_transitioning) return;
            CoreModule.Get<IEventBus>().Publish(new TitleStartRequestedEvent());
        }

        private void OnStartRequested(TitleStartRequestedEvent _)
        {
            if (_transitioning) return;
            _transitioning = true;
            GoLobbyAsync().Forget(); // fire-and-forget: 씬 전환 완료를 기다릴 필요가 없다
        }

        private async UniTaskVoid GoLobbyAsync()
        {
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.Lobby,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }
    }
}
