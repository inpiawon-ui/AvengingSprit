using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 호스트 능력치 눈금(`Projects/AVSR/Balance/host_stats.tsv`)을 `HostTable` 에 굽는다.
    ///
    /// 눈금(0~100) → 등급(1~10) → 실제 수치의 길에서 **눈금만** 여기서 정한다.
    /// 등급 곡선은 `GameConfig`, 그 밖의 칸(스킬 · 공격 방식 · 이름)은 정본 임포터가 정한다.
    ///
    /// ⚠ 정본 임포터를 다시 돌리면 눈금이 정본 값으로 돌아간다 — 그 뒤에 이것을 한 번 더 돌린다.
    /// </summary>
    public static class HostStatsImporter
    {
        private const string TsvPath = "Projects/AVSR/Balance/host_stats.tsv";
        private const string TablePath = "Assets/BundleResource/TableData/HostTable.asset";

        [MenuItem("Tools/Game/호스트 능력치 임포트")]
        public static void Import()
        {
            var table = AssetDatabase.LoadAssetAtPath<HostTable>(TablePath);
            if (table == null) { Debug.LogError("[호스트 능력치] HostTable 없음: " + TablePath); return; }
            if (!File.Exists(TsvPath)) { Debug.LogError("[호스트 능력치] 파일이 없다: " + TsvPath); return; }

            var rows = new Dictionary<string, int[]>();
            string[] head = null;
            var inv = CultureInfo.InvariantCulture;
            foreach (var raw in File.ReadAllLines(TsvPath))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw[0] == '#') continue;
                var t = raw.Split('\t');
                if (head == null) { head = t; continue; }
                int At(string name) => System.Array.IndexOf(head, name);
                rows[t[At("key")].Trim()] = new[]
                {
                    int.Parse(t[At("hp")], inv), int.Parse(t[At("atk")], inv),
                    int.Parse(t[At("spd")], inv), int.Parse(t[At("aspd")], inv),
                };
            }

            var so = new SerializedObject(table);
            var entries = so.FindProperty("_entries");
            int changed = 0, missing = 0;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                string key = e.FindPropertyRelative("_hostKey").stringValue;
                if (!rows.TryGetValue(key, out var v))
                {
                    if (key != HostEntry.GhostKey) { Debug.LogWarning($"[호스트 능력치] 표에 없는 몸: {key}"); missing++; }
                    continue;
                }
                string[] names = { "_hp", "_atk", "_spd", "_atkSpeed" };
                for (int k = 0; k < names.Length; k++)
                {
                    var p = e.FindPropertyRelative(names[k]);
                    if (p.intValue == v[k]) continue;
                    p.intValue = v[k];
                    changed++;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[호스트 능력치] {rows.Count}명 · 바뀐 칸 {changed} · 표에 없는 몸 {missing}");
        }
    }
}
