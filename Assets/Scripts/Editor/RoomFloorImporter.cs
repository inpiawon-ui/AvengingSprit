using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 방 바닥 18장을 받아 Addressable 에 올린다 (챕터 3 × 지오메트리 템플릿 6).
    ///
    /// 아틀라스로 묶지 않는 이유: 이건 스프라이트가 아니라 **화면 한 장짜리 배경**이다.
    /// 720×1530 짜리 6장이면 4096 페이지 하나가 통째로 차고, 픽셀아트라 무압축이므로
    /// 챕터당 26MB 가 상주하게 된다. 아틀라스는 작은 그림을 한 드로우콜로 묶으려고
    /// 쓰는 것인데 배경은 어차피 한 장씩만 그려진다 — 묶어서 얻을 것이 없다.
    ///
    /// 그래서 **한 장씩 주소로 올리고 방마다 필요한 것만 불러 쓴다.**
    /// 동시에 살아 있는 바닥은 언제나 한 장이다.
    /// </summary>
    public static class RoomFloorImporter
    {
        private const string InDir = "Projects/AVSR/_exchange/in";
        private const string ResDir = "Assets/BundleResource/RoomFloor";
        private const string Group = "roomfloor";
        private const string Label = "label_roomfloor";
        private const string Prefix = "roomfloor_";

        [MenuItem("Tools/Game/방 바닥 임포트")]
        public static void Import()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var src = Path.Combine(root, InDir);
            if (!Directory.Exists(src)) { Debug.LogError($"[RoomFloor] 납품 폴더 없음: {src}"); return; }

            var files = Directory.GetFiles(src, Prefix + "*.png");
            if (files.Length == 0) { Debug.LogWarning("[RoomFloor] roomfloor_*.png 가 없다"); return; }

            EnsureFolder(ResDir);

            var copied = new List<string>();
            foreach (var f in files)
            {
                var dst = $"{ResDir}/{Path.GetFileName(f)}";
                File.Copy(f, Path.Combine(root, dst), overwrite: true);
                copied.Add(dst);
            }
            AssetDatabase.Refresh();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("[RoomFloor] Addressable 설정 없음"); return; }

            var group = settings.FindGroup(Group) ?? settings.CreateGroup(
                Group, false, false, true, null,
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(Label)) settings.AddLabel(Label);

            foreach (var path in copied)
            {
                if (AssetImporter.GetAtPath(path) is TextureImporter ti)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.filterMode = FilterMode.Point;      // 픽셀아트
                    ti.mipmapEnabled = false;
                    ti.alphaIsTransparency = false;        // 불투명 배경이다
                    ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    {
                        name = "DefaultTexturePlatform",
                        overridden = true,
                        // 블록 압축은 7~24색 픽셀아트에서 색을 뭉갠다.
                        // 한 번에 한 장만 상주하므로 무압축을 감당할 수 있다.
                        textureCompression = TextureImporterCompression.Uncompressed,
                        maxTextureSize = 2048,             // 1530 < 2048 — 줄어들지 않는다
                    });
                    ti.SaveAndReimport();
                }

                var guid = AssetDatabase.AssetPathToGUID(path);
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = $"{Group}/{Path.GetFileNameWithoutExtension(path)}";
                entry.SetLabel(Label, true);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RoomFloor] 바닥 {copied.Count}장 → 주소 {Group}/roomfloor_ch#_템플릿");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
