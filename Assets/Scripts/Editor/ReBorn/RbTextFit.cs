// RE:BORN UI 리뉴얼(2026-10-23) — 글자 넘침 점검(규칙 06 「3언어 전부 · 칸에 안 들어가면 칸 · 문구를 고친다」 — 지금은 5언어).
// 비활성 TMP 는 isTextOverflowing 이 늘 거짓이라 GetPreferredValues 로 최소 크기에서 직접 잰다.
// 고정 글자(LocalizedText 키) 전부 + 코드가 채우는 칸 중 문구 목록이 정해진 것(능력치 이름 · 탭 등).
using System.Collections.Generic;
using System.Text;
using Game.Module.Common;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.ReBorn
{
    internal static class RbTextFit
    {
        private static readonly Language[] Langs =
            { Language.Korean, Language.Japanese, Language.English, Language.ChineseTraditional, Language.ChineseSimplified };

        // 코드가 채우는 칸 — 칸 이름 → 들어갈 수 있는 키들
        private static readonly (string node, string[] keys)[] Dynamic =
        {
            ("GrwStatTemplate/GrwStatName", new[] { "ui.stat.hp", "ui.stat.atk", "ui.stat.crit", "ui.stat.atkspeed", "ui.stat.range", "ui.stat.movespeed", "ui.stat.critdmg", "ui.stat.def" }),
            ("ColFilter4Text", new[] { "ui.rb.host.sort_power", "ui.rb.host.sort_grade" }),
            ("GrwStarUpLabel", new[] { "ui.rb.growth.starup", "ui.rb.growth.unlock", "ui.growth.max" }),
            ("GrwStarInfo", new[] { "ui.rb.growth.max_info" }),
            ("ColCardPower", new[] { "ui.rb.host.locked" }),
        };

        [MenuItem("Tools/Game/RE:BORN 화면/글자 넘침 점검")]
        public static void Run()
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/BundleResource/TableData/StringTable.asset");
            var sb = new StringBuilder();
            int checkedCount = 0, bad = 0;
            foreach (var path in new[] { RbLobbyBuilder.PrefabPath, RbInGameBuilder.PrefabPath })
                bad += RunOne(table, path, sb, ref checkedCount);
            string head = $"[RE:BORN 글자] 점검 {checkedCount}개(칸 × 언어) · 넘침 {bad}";
            System.IO.File.WriteAllText("Projects/AVSR/_exchange/ref/ui_reborn_1023/textfit.txt", head + "\n" + sb);
            Debug.Log(head + (sb.Length > 0 ? "\n" + sb : ""));
        }

        private static int RunOne(StringTable table, string path, StringBuilder sb, ref int checkedCount)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            int bad = 0;
            try
            {
                var rt = (RectTransform)root.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(720f, 1280f);
                foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true)) t.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    var loc = t.GetComponent<LocalizedText>();
                    if (loc == null || string.IsNullOrEmpty(loc.Key)) continue;
                    bad += Check(table, t, new[] { loc.Key }, sb, ref checkedCount);
                }
                foreach (var (node, keys) in Dynamic)
                {
                    var tr = root.transform.Find(node) ?? FindDeep(root.transform, node);
                    var t = tr != null ? tr.GetComponent<TextMeshProUGUI>() : null;
                    if (t == null) continue;   // 그 프리팹에 없는 칸
                    bad += Check(table, t, keys, sb, ref checkedCount);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return bad;
        }

        private static Transform FindDeep(Transform root, string path)
        {
            string leaf = path.Contains("/") ? path.Substring(path.LastIndexOf('/') + 1) : path;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == leaf) return t;
            return null;
        }

        private static int Check(StringTable table, TextMeshProUGUI t, string[] keys, StringBuilder sb, ref int count)
        {
            int bad = 0;
            var size = t.rectTransform.rect.size;
            if (size.x <= 1f) return 0;
            bool heavy = t.GetComponent<HeavyText>() != null;
            var keep = t.font;
            float keepSize = t.fontSize;
            foreach (var key in keys)
            {
                var e = table.Find(key);
                if (e == null) { sb.AppendLine($"  ? 키 없음: {key}"); continue; }
                foreach (var lang in Langs)
                {
                    string s = e.Raw(lang);
                    if (string.IsNullOrEmpty(s)) s = e.Raw(Language.Korean);
                    s = s.Replace("\\n", "\n").Replace("{0}", "88").Replace("{1}", "88").Replace("{2}", "88").Replace("{3:N0}", "8,888").Replace("{0:N0}", "8,888");
                    var lf = table.FontFor(lang);
                    bool number = t.font != null && t.font.name.StartsWith("Galmuri");
                    if (lf != null && !number) t.font = heavy ? lf.Heavy : lf.Gothic;
                    t.enableAutoSizing = false;
                    t.fontSize = t.fontSizeMin;
                    var pref = t.GetPreferredValues(s, size.x, 0f);
                    count++;
                    // 높이는 줄 높이(글꼴 위아래 여백 포함)라 Noto 한자는 늘 칸보다 몇 px 높다 — 한 줄이 넘는 정도만 넘침으로 본다
                    if (pref.x > size.x + 1f || pref.y > size.y * 1.4f)
                    {
                        bad++;
                        sb.AppendLine($"  ✗ {Path(t.transform)} [{lang}] 「{s.Replace("\n", " / ")}」 {pref.x:0}×{pref.y:0} > 칸 {size.x:0}×{size.y:0}");
                    }
                }
            }
            t.font = keep;
            t.fontSize = keepSize;
            t.enableAutoSizing = true;
            return bad;
        }

        private static string Path(Transform t)
        {
            var parts = new List<string>();
            for (var p = t; p != null && p.parent != null; p = p.parent) parts.Insert(0, p.name);
            return string.Join("/", parts);
        }
    }
}
