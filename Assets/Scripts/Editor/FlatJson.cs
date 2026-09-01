using System.Collections.Generic;
using System.Text;

namespace Game.EditorTools
{
    /// <summary>
    /// 정본 런타임 JSON 전용 최소 파서.
    ///
    /// `JsonUtility` 는 사전(키가 미리 안 정해진 것)을 못 읽는다. 정본 표는
    /// 시트마다 열이 달라서 C# 클래스를 스무 개 만들어야 하는데, 그 클래스들은
    /// 정본이 열 하나만 늘어도 전부 손봐야 한다.
    ///
    /// 정본 파일의 모양은 항상 같다 — `{"version": "...", "data": [ {평평한 객체}, ... ]}`.
    /// 값은 문자열·숫자·불린뿐이고 **중첩이 없다.** 그 모양만 읽는다.
    /// </summary>
    public static class FlatJson
    {
        /// <summary>`data` 배열을 문자열 사전 목록으로. 못 읽으면 빈 목록.</summary>
        public static List<Dictionary<string, string>> Rows(string json)
        {
            var rows = new List<Dictionary<string, string>>();
            if (string.IsNullOrEmpty(json)) return rows;

            int i = json.IndexOf("\"data\"", System.StringComparison.Ordinal);
            i = i < 0 ? 0 : json.IndexOf('[', i);
            if (i < 0) return rows;

            var row = new Dictionary<string, string>();
            string key = null;
            var buf = new StringBuilder();
            bool inString = false, escaped = false, hasToken = false;

            for (i++; i < json.Length; i++)
            {
                char c = json[i];

                if (inString)
                {
                    if (escaped) { buf.Append(Unescape(c)); escaped = false; }
                    else if (c == '\\') escaped = true;
                    else if (c == '"') { inString = false; hasToken = true; }
                    else buf.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '"': inString = true; hasToken = true; break;
                    case '{': row = new Dictionary<string, string>(); key = null; break;
                    case ':': key = Take(buf, ref hasToken); break;
                    case ',':
                    case '}':
                        if (key != null)
                        {
                            row[key] = Take(buf, ref hasToken);
                            key = null;
                        }
                        else buf.Clear();
                        if (c == '}') { rows.Add(row); row = new Dictionary<string, string>(); }
                        break;
                    case ']':
                        return rows;     // `data` 배열이 끝났다
                    default:
                        if (!char.IsWhiteSpace(c)) { buf.Append(c); hasToken = true; }
                        break;
                }
            }
            return rows;
        }

        private static string Take(StringBuilder buf, ref bool hasToken)
        {
            string s = hasToken ? buf.ToString() : string.Empty;
            buf.Clear();
            hasToken = false;
            return s;
        }

        private static char Unescape(char c) => c switch
        {
            'n' => '\n', 't' => '\t', 'r' => '\r', _ => c,
        };
    }
}
