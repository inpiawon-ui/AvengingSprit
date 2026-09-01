using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 컷신 그림을 받아 Addressable 에 올린다.
    ///
    /// 아틀라스로 묶지 않는 이유는 방 바닥과 같다 — 이건 스프라이트가 아니라
    /// **화면 한 장짜리 그림**이다. 640×640 스무 장을 묶으면 오프닝에서만 쓰는 그림이
    /// 판 내내 메모리에 상주한다. 오프닝은 첫 실행에 한 번 보고 끝나는 화면이다.
    ///
    /// 그래서 **한 장씩 주소로 올리고 넘어갈 때마다 놓아 준다.**
    /// 동시에 살아 있는 컷은 두 장뿐이다(지금 것과 다음 것).
    /// </summary>
    public static class CutsceneImporter
    {
        private const string InDir = "Projects/AVSR/_exchange/in";
        private const string ResDir = "Assets/BundleResource/Cutscene";
        private const string Group = "cutscene";
        private const string Label = "label_cutscene";
        private const string Prefix = "cut_";

        [MenuItem("Tools/Game/컷신 임포트 (cut_*)")]
        public static void Import()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var src = Path.Combine(root, InDir);
            if (!Directory.Exists(src)) { Debug.LogError($"[컷신] 납품 폴더 없음: {src}"); return; }

            var files = Directory.GetFiles(src, Prefix + "*.png");
            if (files.Length == 0) { Debug.LogWarning("[컷신] cut_*.png 가 없다"); return; }

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
            if (settings == null) { Debug.LogError("[컷신] Addressable 설정 없음"); return; }

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
                    // ⚠ 컷 대부분은 불투명이지만 **관 안 유령 네 장은 투명 배경**이다.
                    //   켜 두면 Unity 가 투명한 자리의 색을 이웃에서 번지게 채워
                    //   가장자리에 검은 테가 생기지 않는다. 불투명 그림에는 영향이 없다.
                    ti.alphaIsTransparency = true;
                    ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    {
                        name = "DefaultTexturePlatform",
                        overridden = true,
                        // 38색 픽셀아트다. 블록 압축을 걸면 색이 뭉개진다.
                        textureCompression = TextureImporterCompression.Uncompressed,
                        maxTextureSize = 1024,             // 640 < 1024 — 줄어들지 않는다
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
            Debug.Log($"[컷신] {copied.Count}장 → 주소 {Group}/cut_*");
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
