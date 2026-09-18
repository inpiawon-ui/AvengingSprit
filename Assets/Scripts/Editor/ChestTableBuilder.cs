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

        [MenuItem("Tools/Game/상자/상자 표 만들기")]
        public static void Run()
        {
            // 등급 3종 — 챕터 1·2 은, 3·4 금, 5·6 백금 (기획 2026-09-18).
            // 해제 시간은 **테스트용** 1·2·3분이다(기획). 출시 전 다시 정한다.
            // ⚠ 백금 상자 그림은 아직 없다 — 발주본이 오기 전까지 chest_magic 을 쓴다.
            var entries = new[]
            {
                //             키            스프라이트        해제         분당젬  골드          조각 총수   호스트 수  등급 무게 B·A·S
                new ChestEntry("silver",   "chest_silver",  1 * Minute, 3f, (100, 200),  (6, 10),  (1, 2), (85, 15, 0)),
                new ChestEntry("gold",     "chest_gold",    2 * Minute, 4f, (300, 500),  (14, 22), (2, 3), (55, 38, 7)),
                new ChestEntry("platinum", "chest_magic",   3 * Minute, 5f, (700, 1000), (28, 40), (3, 4), (30, 45, 25)),
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
