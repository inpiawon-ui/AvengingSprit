using System.Linq;
using Game.Module.Common.Chest;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 보물상자 표를 만든다 (기획 2026-09-16).
    ///
    /// 해제 시간 · 젬값 · 보상 범위의 **단일 출처**다. 프리팹이나 코드에 숫자를 적지 말고
    /// 여기를 고쳐 다시 돌린다.
    ///
    /// ⚠ 값은 전부 **밸런스 미확정(TBD-BAL)** 이다. 목업이 보여 준 것은 화면뿐이라
    ///   시간·젬값·보상은 근거가 없다. 첫 판 감을 잡을 자리표시로 둔 값이다.
    /// </summary>
    public static class ChestTableBuilder
    {
        private const string TablePath = "Assets/BundleResource/TableData/ChestTable.asset";
        private const string TableGroup = "tabledata";
        private const string TableLabel = "label_tabledata";
        private const string Address = "TableData/ChestTable";

        private const int Minute = 60;
        private const int Hour = 60 * Minute;

        [MenuItem("Tools/Game/상자/상자 표 만들기")]
        public static void Run()
        {
            var entries = new[]
            {
                //             키          스프라이트       해제       분당젬  골드            코어        기억         젬         파편
                new ChestEntry("wood",   "chest_wood",   30 * Minute, 2f, (400, 900),   (0, 1),   (2, 5),   (0, 0),  (1, 3)),
                new ChestEntry("silver", "chest_silver",  2 * Hour,   3f, (1200, 2400), (1, 3),   (6, 12),  (0, 5),  (3, 8)),
                new ChestEntry("gold",   "chest_gold",    4 * Hour,   4f, (3000, 6000), (3, 8),   (15, 30), (10, 25),(8, 18)),
                new ChestEntry("magic",  "chest_magic",   8 * Hour,   5f, (7000, 13000),(8, 16),  (35, 60), (30, 60),(18, 35)),
            };

            var table = AssetDatabase.LoadAssetAtPath<ChestTable>(TablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<ChestTable>();
                AssetDatabase.CreateAsset(table, TablePath);
            }
            table.Fill(entries);
            EditorUtility.SetDirty(table);

            Register(AddressableAssetSettingsDefaultObject.Settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Chest] 상자 표 {entries.Length}종 — {TablePath}");
        }

        private static void Register(AddressableAssetSettings settings)
        {
            if (settings == null) { Debug.LogError("[Chest] Addressable 설정이 없다"); return; }
            var group = settings.FindGroup(TableGroup) ?? settings.CreateGroup(
                TableGroup, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(TableLabel)) settings.AddLabel(TableLabel);
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(TablePath), group);
            entry.address = Address;
            entry.SetLabel(TableLabel, true);
            EditorUtility.SetDirty(settings);
        }
    }
}
