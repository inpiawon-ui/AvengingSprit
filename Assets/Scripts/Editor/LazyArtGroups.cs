using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 보일 때 한 장씩 읽는 큰 그림을 어드레서블에 **한 장씩 따로** 등록한다(Game.Module.Common.UI.LazySpriteSlot, 2026-10-30 메모리).
    ///   · 챕터 배경 `rb_art_bg_stage_ch*` → 묶음 `stagebg` · 주소 `StageBg/{파일 이름}`
    ///   · 결과 배경 `rb_art_result_*_bg*` · 이벤트 그림 `rb_art_ev_*` → 묶음 `kitart` · 주소 `KitArt/{파일 이름}`
    /// 새 그림이 들어와도 빠지지 않게 APK 빌드 직전에 늘 돈다(<see cref="BuildApk"/>). 여러 번 돌려도 같다.
    /// </summary>
    public static class LazyArtGroups
    {
        private const string Dir = "Assets/BaseResource/ReBornKit/Art";

        [MenuItem("Tools/Game/최적화/큰 그림 묶음 등록(챕터 배경 · 결과 · 이벤트)")]
        public static void RunMenu() => Debug.Log($"[큰 그림] 등록 {Ensure()}장");

        /// <returns>등록된 그림 수</returns>
        public static int Ensure()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            int n = Register(settings, "stagebg", "label_stagebg", "StageBg/", "rb_art_bg_stage_ch");
            n += Register(settings, "kitart", "label_kitart", "KitArt/", "rb_art_ev_", "rb_art_result_");
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            return n;
        }

        private static int Register(AddressableAssetSettings settings, string groupName, string label, string prefix,
                                    params string[] filePrefixes)
        {
            var group = settings.FindGroup(groupName);
            if (group == null)
                group = settings.CreateGroup(groupName, false, false, true, settings.FindGroup("tabledata").Schemas);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            EditorUtility.SetDirty(schema);

            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                bool match = false;
                foreach (var p in filePrefixes) if (file.StartsWith(p)) { match = true; break; }
                // 결과 쪽은 배경만(마스코트 · 글자 그림은 작아서 프리팹에 그대로 둔다)
                if (!match || (file.StartsWith("rb_art_result_") && !file.Contains("_bg"))) continue;
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry == null) continue;
                entry.address = prefix + file;
                entry.SetLabel(label, true, true, false);
                n++;
            }
            return n;
        }
    }
}
