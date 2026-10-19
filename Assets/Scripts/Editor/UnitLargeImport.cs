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

        // 코드가 Show 를 안 부르는 고정 진열 칸 — 빌드 때 박힌 초상 대신 큰 몸 그림(유령은 둥실, 패스 · 밤의 호스트는 갱스터)
        // fill = 칸 높이 대비 몸 높이 — 유령 육성 진열 칸(300×258)은 0.95 면 유령이 칸을 꽉 메운다(2026-10-19 캡처)
        private static readonly (string node, string key, bool floating, float fill)[] Fixed =
        {
            ("GhostV5Portrait", "ghost", true, 0.72f), ("PassV5Ghost", "ghost", true, 0.85f), ("NightV5Ghost", "ghost", true, 0.85f),
            ("ShopV5Ghost", "ghost", true, 0.85f), ("H6V5Ghost", "ghost", true, 0.85f),
            ("PassV5Host", "gangster", false, 0.95f), ("NightV5Host", "gangster", false, 0.95f),
        };

        /// <summary>
        /// 로비 프리팹의 고정 진열 칸에 `UnitShowcase` 를 꽂는다. UI v5 빌더가 칸을 새로 만들면 빠지므로
        /// 「UI v5 — 4:3 가로 채우기」(늘 마지막에 돈다) 끝에서 다시 부른다.
        /// </summary>
        internal static void AttachFixed(GameObject lobby)
        {
            foreach (var (node, key, floating, fill) in Fixed)
            {
                var t = FindDeep(lobby.transform, node);
                if (t == null || t.GetComponent<UnityEngine.UI.Image>() == null) continue;
                var show = t.GetComponent<Game.Module.Common.UI.UnitShowcase>();
                if (show == null) show = t.gameObject.AddComponent<Game.Module.Common.UI.UnitShowcase>();
                var so = new SerializedObject(show);
                so.FindProperty("_autoKey").stringValue = key;
                so.FindProperty("_float").boolValue = floating;
                so.FindProperty("_fill").floatValue = fill;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
                if (FindDeep(c, name) is Transform f) return f;
            return null;
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
