using System.IO;
using System.Text;
using Game.Module.Common;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 문자열 표 ↔ 탭 구분 파일(TSV) (2026-09-15, 언어팩).
    ///
    /// 번역은 스프레드시트에서 하는 게 빠르다. 인스펙터로 760줄을 고치면 틀린 칸을 못 찾는다.
    /// 파일: `Projects/AVSR/Localization/AVSR_Strings.tsv` — 열은 `key · ko · ja · en`.
    ///
    /// **반영 규칙** — 빈 칸은 건드리지 않는다. 한국어만 적힌 줄을 반영해도
    /// 이미 들어간 일본어가 지워지지 않는다. 지우려면 표에서 직접 지운다.
    ///
    /// 줄바꿈은 파일 안에서 `\n` 두 글자로 적는다 — TSV 한 칸 안에 실제 줄바꿈을 넣으면
    /// 스프레드시트마다 읽는 방식이 달라 줄이 밀린다.
    /// </summary>
    public static class StringTableSeed
    {
        private const string TablePath = "Assets/BundleResource/TableData/StringTable.asset";
        private const string SeedPath = "Projects/AVSR/Localization/AVSR_Strings.tsv";

        [MenuItem("Tools/Game/언어팩/TSV → 문자열 표 반영")]
        public static void Import()
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(TablePath);
            if (table == null) { Debug.LogError("[언어팩] 문자열 표가 없다: " + TablePath); return; }
            if (!File.Exists(SeedPath)) { Debug.LogError("[언어팩] 파일이 없다: " + SeedPath); return; }

            int rows = 0, written = 0;
            foreach (var raw in File.ReadAllLines(SeedPath, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#") || raw.StartsWith("key\t")) continue;
                var cols = raw.Split('\t');
                if (cols.Length < 2 || string.IsNullOrEmpty(cols[0])) continue;

                var key = cols[0].Trim();
                var ko = Cell(cols, 1);
                var entry = table.Upsert(key, ko);          // 한국어가 비어 있으면 기존 원문을 둔다
                rows++;
                if (!string.IsNullOrEmpty(ko)) written++;
                var ja = Cell(cols, 2);
                if (!string.IsNullOrEmpty(ja)) { entry.Write(Language.Japanese, ja); written++; }
                var en = Cell(cols, 3);
                if (!string.IsNullOrEmpty(en)) { entry.Write(Language.English, en); written++; }
            }

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[언어팩] TSV {rows}줄 반영 · 칸 {written}개 씀 (표 총 {table.Entries.Count}줄)");
        }

        [MenuItem("Tools/Game/언어팩/문자열 표 → TSV 내보내기")]
        public static void Export()
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(TablePath);
            if (table == null) { Debug.LogError("[언어팩] 문자열 표가 없다: " + TablePath); return; }

            var sb = new StringBuilder("key\tko\tja\ten\n");
            int missingJa = 0;
            foreach (var e in table.Entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Key)) continue;
                var ja = e.Raw(Language.Japanese);
                if (ja == null) missingJa++;
                sb.Append(e.Key).Append('\t')
                  .Append(Escape(e.Korean)).Append('\t')
                  .Append(Escape(ja)).Append('\t')
                  .Append(Escape(e.Raw(Language.English))).Append('\n');
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SeedPath));
            File.WriteAllText(SeedPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[언어팩] {table.Entries.Count}줄 내보냄 · 일본어 빈칸 {missingJa} → {SeedPath}");
        }

        private static string Cell(string[] cols, int i)
            => i < cols.Length ? cols[i].Replace("\\n", "\n").Trim('\r') : null;

        private static string Escape(string s)
            => string.IsNullOrEmpty(s) ? string.Empty : s.Replace("\r", string.Empty).Replace("\n", "\\n").Replace("\t", " ");
    }
}
