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

            // 새 납품이 없어도 그냥 지나가지 않는다 — 아래에서 폴더 전체에 주소를 다시 건다.
            var files = Directory.GetFiles(src, Prefix + "*.png");

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

            // ⚠ 주소는 **폴더에 있는 것 전부**에 건다. 복사한 것만 걸면
            //   다른 툴(`ExchangeImporter`)이 이 폴더에 넣어 둔 파일 —
            //   파이썬 벽 3장이 그랬다 — 이 주소 없이 남아 런타임에 안 잡힌다.
            var all = new List<string>(copied);
            foreach (var f in Directory.GetFiles(Path.Combine(root, ResDir), "*.png"))
            {
                var rel = $"{ResDir}/{Path.GetFileName(f)}";
                if (!all.Contains(rel)) all.Add(rel);
            }

            foreach (var path in all)
            {
                // 그림 설정은 **`roomfloor_` 배경에만** 건다.
                // 여기 설정은 `alphaIsTransparency = false`(불투명 배경) 라서
                // 벽처럼 구멍이 뚫린 그림에 걸면 아치가 막힌다.
                if (Path.GetFileName(path).StartsWith(Prefix)
                    && AssetImporter.GetAtPath(path) is TextureImporter ti)
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
            Debug.Log($"[RoomFloor] 새로 들여온 바닥 {copied.Count}장 · 주소 걸린 것 {all.Count}장");
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
