using TMPro;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 카드 틀 안의 몸 이름 — 한 줄에 들어가면 한 줄, 아니면 괄호 앞에서 접어 두 줄(「코만도 / (수류탄)」).
    /// 괄호가 없으면 자동 축소로 한 줄에 넣고, 최소 크기로도 넘칠 때만 줄을 바꾼다.
    /// PD 2026-10-26 「프레임에 이름이 이상하게 나와」 — 줄바꿈이 괄호 안 아무 데서나 끊겼다.
    /// </summary>
    internal static class RbCardName
    {
        private static readonly char[] Parens = { '(', '（' };

        public static void Fit(TextMeshProUGUI t, string name)
        {
            if (t == null) return;
            if (name == null) name = string.Empty;
            float w = t.rectTransform.rect.width;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.fontSize = t.fontSizeMax;   // 자동 축소가 이미 줄인 크기로 재면 한 줄에 들어가는 이름도 접힌다
            if (w <= 0f || t.GetPreferredValues(name, 9999f, 0f).x <= w) { t.text = name; return; }
            int cut = name.IndexOfAny(Parens);
            if (cut > 0)
            {
                t.text = name.Substring(0, cut).TrimEnd() + "\n" + name.Substring(cut);
                return;
            }
            t.fontSize = t.fontSizeMin;
            t.textWrappingMode = t.GetPreferredValues(name, 9999f, 0f).x > w ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.text = name;
        }
    }
}
