using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.Editor
{
    /// <summary>
    /// 로비 · 아웃게임용 큰 몸 그림(`Assets/BaseResource/UnitLarge/{키}/`, 384 칸) — 임포트 설정 + 몸마다 아틀라스 `atlas/unitl_{키}`.
    /// 그림은 `Projects/AVSR/Tools/charv2_cut.py` 가 원작 기준 다시 그리기 납품 판에서 바로 줄여 만든다(확대 아님).
    /// 쓰는 쪽: `Game.Module.Common.UI.UnitShowcase` (홈 진열 · 호스트 육성).
    /// </summary>
    public static class UnitLargeImport
    {
        private const string Root = "Assets/BaseResource/UnitLarge";
        private const string AtlasDir = "Assets/BundleResource/Atlas";

        [MenuItem("Tools/Game/캐릭터 — 큰 몸 그림 임포트 (UnitLarge · 로비 진열)")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Root)) { Debug.LogWarning($"[UnitLarge] {Root} 없음"); return; }
            int sprites = 0, atlases = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePivot = new Vector2(0.5f, 0.5f);
                // 고해상 원본을 줄인 그림이라 점 필터보다 부드럽게 — 진열 칸에서 원본 크기 근처로 그린다(규칙 09 일러스트 칸)
                ti.filterMode = FilterMode.Bilinear;
                ti.mipmapEnabled = false;
                ti.isReadable = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;   // 압축은 아틀라스에서(규칙 09)
                ti.SaveAndReimport();
                sprites++;
            }
            foreach (var folder in AssetDatabase.GetSubFolders(Root))
            {
                string key = folder.Substring(Root.Length + 1);
                EnsureAtlas($"unitl_{key}", folder);
                atlases++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[UnitLarge] 그림 {sprites}장 · 아틀라스 {atlases}개");
        }

        private static void EnsureAtlas(string name, string folder)
        {
            string atlasPath = $"{AtlasDir}/{name}.spriteatlasv2";
            if (!System.IO.File.Exists(atlasPath))
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new[] { AssetDatabase.LoadAssetAtPath<Object>(folder) });
                SpriteAtlasAsset.Save(atlas, atlasPath);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
            }
            if (AssetImporter.GetAtPath(atlasPath) is not SpriteAtlasImporter imp) return;
            imp.packingSettings = new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4 };
            imp.textureSettings = new SpriteAtlasTextureSettings { filterMode = FilterMode.Bilinear, generateMipMaps = false, sRGB = true, readable = false };
            imp.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ASTC_4x4,
            });
            imp.SaveAndReimport();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings != null ? settings.FindGroup("atlas") : null;
            if (group == null) { Debug.LogError("[UnitLarge] Addressable 그룹 atlas 없음"); return; }
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(atlasPath), group);
            entry.address = $"atlas/{name}";
            entry.SetLabel("label_atlas", true);
        }
    }
}
