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

        [MenuItem("Tools/Game/RE:BORN 화면/연출 부품 포맷 · 아틀라스")]
        public static void Setup()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single
                             || ti.filterMode != FilterMode.Point || ti.mipmapEnabled || ti.isReadable || !ti.alphaIsTransparency;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.isReadable = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath) == null)
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(Folder) });
                SpriteAtlasAsset.Save(atlas, AtlasPath);
                AssetDatabase.ImportAsset(AtlasPath);
            }
            if (AssetImporter.GetAtPath(AtlasPath) is SpriteAtlasImporter ai)
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
            Debug.Log("[RE:BORN 연출] 부품 포맷 · 아틀라스 → " + AtlasPath);
        }

        /// <summary>로비 뿌리에 연출 컴포넌트를 붙이고 부품을 꽂는다(로비 세우기에서 부른다).</summary>
        public static void Attach(GameObject root)
        {
            var fx = root.GetComponent<RbOutFx>() ?? root.AddComponent<RbOutFx>();
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
            so.FindProperty("_band").objectReferenceValue = Load("rbfx_band");
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
