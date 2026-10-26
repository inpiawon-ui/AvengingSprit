// RE:BORN 아웃게임 연출(2026-10-24) — 부품(Assets/BaseResource/ReBornFx, rbfx_import.py 가 넣음) 포맷 · 아틀라스 · 로비 연결.
// 포맷은 규칙 09: 도트 그림 = Sprite · Point · 밉맵 끔 · Read/Write 끔, 압축은 아틀라스(안드로이드 ASTC 4x4 · 최대 2048 · 여백 4).
using System.Collections.Generic;
using Game.Module.Lobby;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.Editor.ReBorn
{
    internal static class RbFxBuilder
    {
        private const string Folder = "Assets/BaseResource/ReBornFx";
        private const string AtlasPath = "Assets/BundleResource/Atlas/ReBornFx.spriteatlasv2";
        private const string AdditiveMat = "Assets/BaseResource/Shaders/UIAdditiveSprite.mat";

        // 인게임 연출(위험 경고 테두리 등) — 판 안에서 로비 연출 아틀라스를 통째로 올리지 않게 따로 묶는다
        private const string GameFolder = "Assets/BaseResource/ReBornFxGame";
        private const string GameAtlasPath = "Assets/BundleResource/Atlas/ReBornFxGame.spriteatlasv2";

        [MenuItem("Tools/Game/RE:BORN 화면/연출 부품 포맷 · 아틀라스")]
        public static void Setup()
        {
            SetupFolder(Folder, AtlasPath);
            if (AssetDatabase.IsValidFolder(GameFolder)) SetupFolder(GameFolder, GameAtlasPath);
            Debug.Log("[RE:BORN 연출] 부품 포맷 · 아틀라스 → " + AtlasPath + " · " + GameAtlasPath);
        }

        private static void SetupFolder(string folder, string atlasPath)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single
                             || ti.filterMode != FilterMode.Point || ti.mipmapEnabled || ti.isReadable || !ti.alphaIsTransparency
                             || ti.textureCompression != TextureImporterCompression.Uncompressed;
                if (!dirty) continue;
                // 압축은 원본이 아니라 아틀라스에서 정한다(규칙 09) — 원본을 압축하면 아틀라스가 두 번 뭉갠다
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.isReadable = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath) == null)
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder) });
                SpriteAtlasAsset.Save(atlas, atlasPath);
                AssetDatabase.ImportAsset(atlasPath);
            }
            if (AssetImporter.GetAtPath(atlasPath) is SpriteAtlasImporter ai)
            {
                ai.textureSettings = new SpriteAtlasTextureSettings
                {
                    filterMode = FilterMode.Point, generateMipMaps = false, readable = false, sRGB = true, anisoLevel = 0,
                };
                ai.packingSettings = new SpriteAtlasPackingSettings { padding = 4, enableRotation = false, enableTightPacking = false };
                ai.SetPlatformSettings(new TextureImporterPlatformSettings
                {
                    name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ASTC_4x4,
                    textureCompression = TextureImporterCompression.Compressed,
                });
                ai.SaveAndReimport();
            }
        }

        /// <summary>인게임 연출 그림 하나(ReBornFxGame) — 인게임 빌더가 꽂는다.</summary>
        public static Sprite GameSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{GameFolder}/{name}.png");

        public static Material Additive => AssetDatabase.LoadAssetAtPath<Material>(AdditiveMat);

        /// <summary>로비 뿌리에 연출 컴포넌트를 붙이고 부품을 꽂는다(로비 세우기에서 부른다).</summary>
        public static void Attach(GameObject root)
        {
            if (!root.TryGetComponent<RbOutFx>(out var fx)) fx = root.AddComponent<RbOutFx>();
            var so = new SerializedObject(fx);
            so.FindProperty("_additive").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(AdditiveMat);
            Frames(so, "_ring6", "rbfx_ring6_", 8);
            Frames(so, "_ring10", "rbfx_ring10_", 8);
            Frames(so, "_star", "rbfx_star_", 6);
            Frames(so, "_flash", "rbfx_flash_", 6);
            Frames(so, "_chestLight", "rbfx_chestlight_", 3);
            Frames(so, "_coin", "rbfx_coin_", 8);
            Frames(so, "_chestSilver", "rbfx_chest_silver_", 4);
            Frames(so, "_chestGold", "rbfx_chest_gold_", 4);
            Frames(so, "_chestPlatinum", "rbfx_chest_platinum_", 4);
            so.FindProperty("_shine").objectReferenceValue = Load("rbfx_shine");
            so.FindProperty("_rays").objectReferenceValue = Load("rbfx_rays");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Frames(SerializedObject so, string field, string prefix, int count)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < count; i++)
                if (Load(prefix + i) is Sprite s) list.Add(s);
            var p = so.FindProperty(field);
            p.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }

        private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");
    }
}
