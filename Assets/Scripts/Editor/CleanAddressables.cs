using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 대상 에셋이 사라진 Addressable 엔트리와 빈 그룹을 제거한다.
    ///
    /// 템플릿에서 물려받은 항목(PetRoomUI·MinigameUI·CareFeedPopup 등)이
    /// 실제 에셋 없이 주소만 남아 있어, 주소 오타를 냈을 때 조용히 넘어가거나
    /// 빌드 시 경고를 낸다.
    /// </summary>
    public static class CleanAddressables
    {
        // 게임이 실제로 쓰는 그룹 — 비어 있어도 지우지 않는다
        private static readonly HashSet<string> Keep = new()
        {
            "ui", "atlas", "tabledata", "Default Local Group", "unifiedraytracing",
        };

        [MenuItem("Tools/Game/Clean Dangling Addressables")]
        public static void Run()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("[CleanAddressables] 설정 없음"); return; }

            int removed = 0;
            foreach (var g in settings.groups.Where(g => g != null).ToList())
            {
                foreach (var e in g.entries.ToList())
                {
                    var p = AssetDatabase.GUIDToAssetPath(e.guid);
                    bool exists = !string.IsNullOrEmpty(p) &&
                                  AssetDatabase.LoadAssetAtPath<Object>(p) != null;
                    if (exists) continue;
                    Debug.Log($"[CleanAddressables] 끊긴 주소 제거 — {g.Name} / {e.address}");
                    settings.RemoveAssetEntry(e.guid, false);
                    removed++;
                }
            }

            int groupsRemoved = 0;
            foreach (var g in settings.groups.Where(g => g != null).ToList())
            {
                if (g.entries.Count > 0 || Keep.Contains(g.Name) || g.IsDefaultGroup()) continue;
                Debug.Log($"[CleanAddressables] 빈 그룹 제거 — {g.Name}");
                settings.RemoveGroup(g);
                groupsRemoved++;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CleanAddressables] 완료 — 주소 {removed}개 · 그룹 {groupsRemoved}개 제거");
        }
    }
}
