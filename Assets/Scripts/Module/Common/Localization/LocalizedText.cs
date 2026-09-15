using System;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using TMPro;
using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 프리팹에 **고정으로 박힌 글자**를 언어팩으로 바꾼다. 키만 적어 두면 켜질 때와
    /// 언어가 바뀔 때 알아서 다시 쓴다.
    ///
    /// 코드가 `SetText` 로 채우는 칸에는 붙이지 않는다 — 두 곳이 같은 칸을 쓰면
    /// 실행 순서에 따라 결과가 뒤집힌다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        private TMP_Text _text;
        private IDisposable _token;

        public string Key => _key;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            if (CoreModule.TryGet<IEventBus>(out var bus))
                _token = bus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            Refresh();
        }

        private void OnDisable()
        {
            _token?.Dispose();
            _token = null;
        }

        /// <summary>에디터 도구가 키를 심는다.</summary>
        public void SetKey(string key)
        {
            _key = key;
            if (isActiveAndEnabled) Refresh();
        }

        private void OnLanguageChanged(LanguageChangedEvent _) => Refresh();

        private void Refresh()
        {
            if (_text == null || string.IsNullOrEmpty(_key)) return;
            _text.text = Localize.Get(_key);
        }
    }
}
