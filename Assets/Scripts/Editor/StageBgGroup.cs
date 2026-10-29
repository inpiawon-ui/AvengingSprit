using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 챕터 배경(`rb_art_bg_stage_ch*`)을 어드레서블 `stagebg` 묶음에 **한 장씩 따로** 등록한다 — 주소 `StageBg/{파일 이름}`.
    /// 로비 · 성장 · 챕터 목록이 보일 때 한 장만 읽는다(Game.Module.Lobby.StageBgSlot, 2026-10-30 메모리).
    /// 새 챕터 배경이 들어와도 빠지지 않게 APK 빌드 직전에 늘 돈다(<see cref="BuildApk"/>). 여러 번 돌려도 같다.
    /// </summary>
    public static class StageBgGroup
    {
        private const string Dir = "Assets/BaseResource/ReBornKit/Art";
        private const string Group = "stagebg";
        private const string Label = "label_stagebg";

        [MenuItem("Tools/Game/최적화/챕터 배경 묶음 등록")]
        public static void RunMenu() => Debug.Log($"[챕터 배경] 등록 {Ensure()}장");

        /// <returns>등록된 배경 수</returns>
        public static int Ensure()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(Group);
            if (group == null)
                group = settings.CreateGroup(Group, false, false, true, settings.FindGroup("tabledata").Schemas);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            EditorUtility.SetDirty(schema);

            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("rb_art_bg_stage_ch t:Texture2D", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry == null) continue;
                entry.address = "StageBg/" + System.IO.Path.GetFileNameWithoutExtension(path);
                entry.SetLabel(Label, true, true, false);
                n++;
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            return n;
        }
    }
}
