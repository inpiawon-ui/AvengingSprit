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

        // 획 두께 보정 재질 — (폰트, 두께)마다 하나를 여러 글자 칸이 같이 쓴다
        private readonly Dictionary<(int font, int dilate), Material> _weights = new();

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
            foreach (var m in _weights.Values)
                if (m != null) UnityEngine.Object.Destroy(m);
            _weights.Clear();
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
            if (text == null || text.font == null) return;

            // 굵은 폰트 칸은 굵은 폰트끼리 바꾼다. 그 언어에 굵은 폰트가 없으면
            // 본문 폰트에 굵게 모양을 얹어 대신한다(일본어 굵은 폰트가 아직 없다).
            // ⚠ 표시는 글자 칸 위의 `HeavyText` 가 쥔다 — 폰트로만 알아보면, 굵은 폰트가 없는
            //   언어에서 본문 폰트로 바뀐 순간 다음 언어로 갈 때 굵은 칸이었는지를 잃는다.
            var heavy = text.GetComponent<HeavyText>();
            if (heavy != null || IsHeavy(text.font))
            {
                if (text.font != target.Heavy) text.font = target.Heavy;
                if (target.HasHeavy) text.fontStyle &= ~FontStyles.Bold;
                else text.fontStyle |= FontStyles.Bold;
                if (heavy != null) ApplyWeight(text, heavy.Dilate);
                return;
            }

            if (text.font == target.Gothic) return;
            if (!IsGothic(text.font)) return;   // 픽셀 폰트 칸은 건드리지 않는다

            // 재질 프리셋 이름 꼬리를 먼저 읽는다 — 폰트를 바꾸면 재질이 기본으로 돌아간다.
            var before = text.fontSharedMaterial;
            string suffix = PresetSuffix(before);
            text.font = target.Gothic;
            var preset = FindPreset(target, suffix);
            if (preset != null) { text.fontSharedMaterial = preset; return; }

            // ⚠ 프리셋이 아닌 **제 재질**(피해 숫자처럼 외곽선을 따로 올린 인스턴스)은 폰트만 바뀌고
            //   재질이 옛 폰트의 아틀라스를 그대로 물고 있었다 — 새 글자 좌표로 옛 아틀라스를 읽어
            //   숫자 뒤에 **검은 네모**가 떴다(2026-09-15). 설정은 살리고 아틀라스만 갈아 끼운다.
            var now = text.fontSharedMaterial;
            if (now != null && now.mainTexture != target.Gothic.atlasTexture)
            {
                var copy = new Material(now) { name = now.name };
                copy.SetTexture(ShaderUtilities.ID_MainTex, target.Gothic.atlasTexture);
                text.fontSharedMaterial = copy;
            }
        }

        /// <summary>
        /// 획 두께 보정. 0 이면 폰트 기본 재질. 폰트를 바꾸면 재질이 기본으로 돌아가므로 바꿀 때마다 다시 입힌다.
        /// </summary>
        private void ApplyWeight(TMP_Text text, float dilate)
        {
            int step = Mathf.RoundToInt(dilate * 100f);
            if (step == 0)
            {
                if (text.fontSharedMaterial != text.font.material) text.fontSharedMaterial = text.font.material;
                return;
            }
            var key = (text.font.GetInstanceID(), step);
            if (!_weights.TryGetValue(key, out var m) || m == null)
            {
                m = new Material(text.font.material) { name = $"{text.font.name} W{step}" };
                m.SetFloat(ShaderUtilities.ID_FaceDilate, step / 100f);
                _weights[key] = m;
            }
            text.fontSharedMaterial = m;
        }

        /// <summary>어느 언어든 굵은 폰트로 등록된 것인가. 본문 폰트와 같은 칸은 빼고 본다.</summary>
        private bool IsHeavy(TMP_FontAsset font)
        {
            for (int i = 0; i < _table.Fonts.Count; i++)
            {
                var f = _table.Fonts[i];
                if (f != null && f.HasHeavy && f.Heavy == font) return true;
            }
            return false;
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
