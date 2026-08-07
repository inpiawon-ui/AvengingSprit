using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Events;
using GameFramework.Core.Module.EventBus;
using UnityEngine;

namespace GameFramework.Core.Module.Localization
{
    /// <summary>
    /// Resources/Localization/{locale}.json 파일에서 현지화 데이터를 로드합니다.
    ///
    /// JSON 형식:
    /// {
    ///   "UI_Start": "게임 시작",
    ///   "UI_Settings": "설정"
    /// }
    /// </summary>
    public sealed class LocalizationManager : ILocalizationManager
    {
        private const string ResourcesPath = "Localization";

        private readonly IEventBus            _bus;
        private Dictionary<string, string>    _table = new();

        public string CurrentLocale { get; private set; } = "ko";

        public LocalizationManager(IEventBus bus) => _bus = bus;

        public async UniTask SetLocaleAsync(string locale)
        {
            var request = Resources.LoadAsync<TextAsset>($"{ResourcesPath}/{locale}");
            await request;

            if (request.asset is not TextAsset textAsset)
            {
                Debug.LogWarning($"[LocalizationManager] 로케일 파일 없음: Resources/{ResourcesPath}/{locale}.json");
                return;
            }

            _table = ParseJson(textAsset.text);
            CurrentLocale = locale;
            _bus?.Publish(new OnLocaleChanged { NewLocale = locale });
        }

        public string Get(string key) =>
            _table.TryGetValue(key, out var value) ? value : key;

        public string GetFormat(string key, params object[] args)
        {
            string text = Get(key);
            try   { return string.Format(text, args); }
            catch { return text; }
        }

        // ── JSON 파서 ─────────────────────────────────────────
        // Unity의 JsonUtility는 Dictionary를 지원하지 않으며, Newtonsoft.Json은 프로젝트 의존성에 없다.
        // 따라서 객체 단일 레벨 { "key": "value" }만 지원하는 경량 파서를 자체 구현하되,
        // 이스케이프 시퀀스(\", \\, \n, \r, \t, \uXXXX)를 안전하게 처리한다.

        private static Dictionary<string, string> ParseJson(string json)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json)) return result;

            int i = 0;
            while (i < json.Length)
            {
                // 다음 따옴표(키 시작) 탐색
                if (!SkipToQuote(json, ref i)) break;
                if (!ReadString(json, ref i, out string key))
                {
                    Debug.LogWarning("[LocalizationManager] JSON 파싱 실패: 키 문자열 종료 누락");
                    break;
                }

                // 콜론
                if (!SkipToChar(json, ref i, ':')) break;
                i++; // ':' 건너뜀

                // 값 따옴표
                if (!SkipToQuote(json, ref i)) break;
                if (!ReadString(json, ref i, out string value))
                {
                    Debug.LogWarning($"[LocalizationManager] JSON 파싱 실패: 값 문자열 종료 누락 (key={key})");
                    break;
                }

                result[key] = value;
            }
            return result;
        }

        private static bool SkipToQuote(string s, ref int i)
        {
            while (i < s.Length && s[i] != '"') i++;
            return i < s.Length;
        }

        private static bool SkipToChar(string s, ref int i, char target)
        {
            while (i < s.Length && s[i] != target) i++;
            return i < s.Length;
        }

        /// <summary>
        /// 현재 위치가 여는 따옴표(")라고 가정하고, 다음 닫는 따옴표까지의 문자열을 읽는다.
        /// 이스케이프 시퀀스를 해제하며 i는 닫는 따옴표 다음 위치로 진행된다.
        /// </summary>
        private static bool ReadString(string s, ref int i, out string value)
        {
            value = null;
            if (i >= s.Length || s[i] != '"') return false;
            i++; // 여는 따옴표 건너뜀

            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '"')
                {
                    i++; // 닫는 따옴표 건너뜀
                    value = sb.ToString();
                    return true;
                }
                if (c == '\\' && i + 1 < s.Length)
                {
                    char esc = s[i + 1];
                    switch (esc)
                    {
                        case '"':  sb.Append('"');  i += 2; continue;
                        case '\\': sb.Append('\\'); i += 2; continue;
                        case '/':  sb.Append('/');  i += 2; continue;
                        case 'n':  sb.Append('\n'); i += 2; continue;
                        case 'r':  sb.Append('\r'); i += 2; continue;
                        case 't':  sb.Append('\t'); i += 2; continue;
                        case 'b':  sb.Append('\b'); i += 2; continue;
                        case 'f':  sb.Append('\f'); i += 2; continue;
                        case 'u':
                            if (i + 5 < s.Length &&
                                int.TryParse(s.Substring(i + 2, 4), System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture, out int code))
                            {
                                sb.Append((char)code);
                                i += 6;
                                continue;
                            }
                            // 잘못된 \u — 그대로 추가
                            sb.Append(c);
                            i++;
                            continue;
                        default:
                            // 알 수 없는 이스케이프 — 원본 보존
                            sb.Append(c);
                            i++;
                            continue;
                    }
                }
                sb.Append(c);
                i++;
            }

            // 닫는 따옴표 없이 종료
            return false;
        }
    }
}
