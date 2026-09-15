using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using TMPro;
using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 언어팩 (2026-09-15, 일본 출시).
    ///
    /// ── 왜 프레임워크의 LocalizationModule 을 안 쓰나 ─────────────
    /// 그 모듈은 `Resources/Localization/{locale}.json` 평면 JSON 만 읽는다.
    /// 이 프로젝트는 런타임 에셋을 **Addressables 표**로 두는 것이 규칙이고(02_addressables),
    /// 프레임워크는 고칠 수 없다(3-tier). 그래서 게임 쪽에 둔다. 표 편집도 인스펙터에서 된다.
    /// </summary>
    [Module(Layer = ModuleLayer.Game, DependsOn = new[] { typeof(IEventBus), typeof(IResourceManager) })]
    public sealed class LanguageModule : IModule
    {
        private LanguageService _service;

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _service = new LanguageService();
            CoreModule.Register<ILanguageService>(_service);
            IsInitialized = true;
        }

        public void Initialize()
        {
            _service.LoadAsync().Forget();   // fire-and-forget: 표가 오기 전에는 원문으로 답한다
        }

        public void Dispose()
        {
            _service?.RestoreFallbacks();
            _service = null;
            IsInitialized = false;
        }
    }

    internal sealed class LanguageService : ILanguageService
    {
        private const string Address = "TableData/StringTable";
        private const string PrefsKey = "game.language";

        private StringTable _table;
        private Language _current = Language.Japanese;

#if UNITY_EDITOR
        // 빠진 키를 한 번씩만 알린다. 매 프레임 같은 경고가 쌓이면 진짜 경고가 묻힌다.
        private readonly HashSet<string> _warned = new();
#endif

        // 픽셀 폰트의 대체 목록을 바꾼 기록. 에디터 플레이 중 에셋을 건드리므로 끝날 때 되돌린다.
        private TMP_FontAsset _pixel;
        private List<TMP_FontAsset> _pixelOriginal;

        public Language Current => _current;
        public bool IsReady => _table != null;
        public TMP_FontAsset GothicFont => _table?.FontFor(_current)?.Gothic;

        public async UniTask LoadAsync()
        {
            try
            {
                _table = await CoreModule.Get<IResourceManager>().LoadAsync<StringTable>(Address);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Language] 문자열 표를 못 읽었다 — 원문(한국어)으로 간다: {e.Message}");
            }

            _current = PickStartLanguage();
            ApplyAll();
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            var entry = _table?.Find(key);
            if (entry == null)
            {
                Warn(key, "키 없음");
                return key;
            }
            var value = entry.Raw(_current);
            if (value != null) return value;
            Warn(key, LanguageCodes.Code(_current) + " 비어 있음");
            return entry.Korean ?? key;
        }

        public string Format(string key, params object[] args)
        {
            var pattern = Get(key);
            try { return string.Format(pattern, args); }
            catch (FormatException) { return pattern; }   // 번역에서 자리표시가 틀려도 게임은 안 멈춘다
        }

        public string FromTable(string key, string korean)
        {
            if (_current == Language.Korean || _table == null) return korean;
            var value = _table.Find(key)?.Raw(_current);
            return value ?? korean;
        }

        public void SetLanguage(Language language)
        {
            PlayerPrefs.SetInt(PrefsKey, (int)language);
            PlayerPrefs.Save();
            if (language == _current && _table != null) return;
            _current = language;
            ApplyAll();
        }

        public void ApplyFonts(Transform root)
        {
            if (root == null || _table == null) return;
            var target = _table.FontFor(_current);
            if (target?.Gothic == null) return;

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++) SwapFont(texts[i], target);
        }

        public void RestoreFallbacks()
        {
            if (_pixel == null || _pixelOriginal == null) return;
            _pixel.fallbackFontAssetTable = _pixelOriginal;
            _pixel = null;
            _pixelOriginal = null;
        }

        // ─────────────────────────────────────────────────────────

        private Language PickStartLanguage()
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                int saved = PlayerPrefs.GetInt(PrefsKey);
                if (Enum.IsDefined(typeof(Language), saved)) return (Language)saved;
            }
            if (_table == null) return Language.Korean;
            return _table.FollowSystemLanguage
                 ? LanguageCodes.FromSystem(Application.systemLanguage)
                 : _table.DefaultLanguage;
        }

        private void ApplyAll()
        {
            ApplyPixelFallback();
            // 이미 떠 있는 모든 화면. 꺼진 오브젝트까지 — 켜지는 순간 옛 폰트가 보이면 안 된다.
            if (_table != null)
            {
                var target = _table.FontFor(_current);
                if (target?.Gothic != null)
                {
                    var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for (int i = 0; i < texts.Length; i++) SwapFont(texts[i], target);
                }
            }

            if (CoreModule.TryGet<IEventBus>(out var bus))
                bus.Publish(new LanguageChangedEvent { NewLanguage = _current });
        }

        /// <summary>
        /// 픽셀 폰트(영문·숫자)는 한글·일본어를 대체 폰트로 받는다. 그 대체를 지금 언어의
        /// 본문 폰트로 **맨 앞에** 둔다 — 한글 폰트가 앞에 있으면 일본어 한자가 한국식으로 나온다.
        /// </summary>
        private void ApplyPixelFallback()
        {
            if (_table == null) return;
            var pixel = TMP_Settings.defaultFontAsset;
            var gothic = _table.FontFor(_current)?.Gothic;
            if (pixel == null || gothic == null) return;

            if (_pixel == null)
            {
                _pixel = pixel;
                _pixelOriginal = new List<TMP_FontAsset>(pixel.fallbackFontAssetTable ?? new List<TMP_FontAsset>());
            }

            var list = new List<TMP_FontAsset> { gothic };
            for (int i = 0; i < _table.Fonts.Count; i++)
            {
                var other = _table.Fonts[i]?.Gothic;
                if (other != null && !list.Contains(other)) list.Add(other);
            }
            pixel.fallbackFontAssetTable = list;
        }

        private void SwapFont(TMP_Text text, LanguageFont target)
        {
            if (text == null || text.font == null || text.font == target.Gothic) return;
            if (!IsGothic(text.font)) return;   // 픽셀 폰트 칸은 건드리지 않는다

            // 재질 프리셋 이름 꼬리를 먼저 읽는다 — 폰트를 바꾸면 재질이 기본으로 돌아간다.
            string suffix = PresetSuffix(text.fontSharedMaterial);
            text.font = target.Gothic;
            var preset = FindPreset(target, suffix);
            if (preset != null) text.fontSharedMaterial = preset;
        }

        private bool IsGothic(TMP_FontAsset font)
        {
            for (int i = 0; i < _table.Fonts.Count; i++)
                if (_table.Fonts[i]?.Gothic == font) return true;
            return false;
        }

        /// <summary>`NotoSansKR SDF - Outline` → ` - Outline`. 기본 재질이면 null.</summary>
        private static string PresetSuffix(Material material)
        {
            if (material == null) return null;
            int at = material.name.IndexOf(" - ", StringComparison.Ordinal);
            return at < 0 ? null : material.name.Substring(at);
        }

        private static Material FindPreset(LanguageFont target, string suffix)
        {
            if (string.IsNullOrEmpty(suffix)) return null;
            for (int i = 0; i < target.Presets.Count; i++)
            {
                var m = target.Presets[i];
                if (m != null && m.name.EndsWith(suffix, StringComparison.Ordinal)) return m;
            }
            return null;
        }

        private void Warn(string key, string why)
        {
#if UNITY_EDITOR
            if (_table == null || !_warned.Add(key)) return;
            Debug.LogWarning($"[Language] {why}: {key}");
#endif
        }
    }
}
