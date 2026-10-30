using System.Collections.Generic;
using System.IO;
using Game.Module.InGame;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 방 표를 챕터별로 나눈다(PD 2026-10-30 최적화 — `RoomTable` 머리 주석).
    ///
    ///   원본  Assets/BundleResource/TableData/RoomTable.asset          — 임포터 · 맵 툴이 쓰는 통째 표. **주소를 뗀다**(빌드에 안 들어간다)
    ///   결과  Assets/BundleResource/TableData/Rooms/RoomTable_ch{n}.asset → 주소 TableData/RoomTable_ch{n}
    ///         Assets/BundleResource/TableData/Rooms/RoomTable_bosses.asset → 주소 TableData/RoomTable_bosses (모드 판용 보스 방 모음)
    ///
    /// 원본이 다시 가져와지면(임포터 · 맵 툴 저장) 저절로 다시 나눈다. 빌드(`BuildApk`)도 굽기 전에 한 번 부른다.
    /// 있는 파일은 덮어써서 GUID 를 지킨다.
    /// </summary>
    public static class RoomTableSplitter
    {
        public const string SourcePath = "Assets/BundleResource/TableData/RoomTable.asset";
        private const string OutDir = "Assets/BundleResource/TableData/Rooms";
        private const string Group = "tabledata";
        private const string Label = "label_tabledata";

        [MenuItem("Tools/Game/방 표 챕터별 나누기")]
        public static void Run()
        {
            var source = AssetDatabase.LoadAssetAtPath<RoomTable>(SourcePath);
            if (source == null) { Debug.LogError("[방 표 나누기] 원본이 없다: " + SourcePath); return; }
            if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/BundleResource/TableData", "Rooms");

            var byChapter = new SortedDictionary<int, List<RoomEntry>>();
            var bosses = new List<RoomEntry>();
            foreach (var r in source.Rooms)
            {
                if (r == null) continue;
                if (!byChapter.TryGetValue(r.Chapter, out var list)) byChapter[r.Chapter] = list = new List<RoomEntry>();
                list.Add(r);
                if (r.IsBoss) bosses.Add(r);
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings != null ? settings.FindGroup(Group) : null;
            if (group == null) { Debug.LogError("[방 표 나누기] 어드레서블 그룹이 없다: " + Group); return; }

            int written = 0;
            foreach (var kv in byChapter)
            {
                Write($"{OutDir}/RoomTable_ch{kv.Key}.asset", kv.Value, source.ContractVersion, RoomTable.ChapterAddress(kv.Key), settings, group);
                written++;
            }
            Write($"{OutDir}/RoomTable_bosses.asset", bosses, source.ContractVersion, RoomTable.BossesAddress, settings, group);

            // 원본은 빌드에서 뺀다 — 판은 이제 챕터 표만 읽는다
            string srcGuid = AssetDatabase.AssetPathToGUID(SourcePath);
            if (settings.FindAssetEntry(srcGuid) != null) settings.RemoveAssetEntry(srcGuid);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[방 표 나누기] 챕터 {written}장 · 보스 방 {bosses.Count}개 → {OutDir} (원본 주소 뗌)");
        }

        private static void Write(string path, List<RoomEntry> rooms, string version, string address,
                                  UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings,
                                  UnityEditor.AddressableAssets.Settings.AddressableAssetGroup group)
        {
            var fresh = ScriptableObject.CreateInstance<RoomTable>();
            fresh.AddRooms(rooms, version);
            fresh.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<RoomTable>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(fresh, existing);
                existing.name = fresh.name;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(fresh);
            }
            else AssetDatabase.CreateAsset(fresh, path);

            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
            entry.address = address;
            entry.SetLabel(Label, true, true);
        }
    }

    /// <summary>원본 방 표가 다시 가져와지면 나누기를 다시 돈다(가져오기가 끝난 뒤 한 번).</summary>
    internal sealed class RoomTableSplitWatcher : AssetPostprocessor
    {
        private static bool s_queued;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (s_queued) return;
            for (int i = 0; i < imported.Length; i++)
            {
                if (imported[i] != RoomTableSplitter.SourcePath) continue;
                s_queued = true;
                EditorApplication.delayCall += () => { s_queued = false; RoomTableSplitter.Run(); };
                return;
            }
        }
    }
}
