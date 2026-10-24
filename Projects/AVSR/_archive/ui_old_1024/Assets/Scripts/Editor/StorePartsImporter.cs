using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.U2D;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 상점 · 소환 · 패스 · 출전 부품(1차 카드 · 2차 나머지)을 프로젝트에 넣고 포맷 · 아틀라스까지 한 번에 맞춘다.
    ///
    /// 부품은 유니티가 다른 세션에 잡혀 있을 때 미리 잘라 `_exchange/out/parts1 · parts2/` 에 모아 뒀다.
    /// 손으로 복사하면 다시 납품됐을 때 어긋나므로 해시로 대조해 바뀐 것만 넣는다.
    /// 포맷은 09_import_settings.md — 도트 Sprite · Point · 밉맵 끔 · R/W 끔, 아틀라스는 안드로이드 ASTC 4x4 · 2048 · 여백 4.
    /// </summary>
    public static class StorePartsImporter
    {
        private const string OutDir = "Projects/AVSR/_exchange/out";
        private const string AtlasDir = "Assets/BundleResource/Atlas";
        private const string AtlasGroup = "atlas";
        private const string AtlasLabel = "label_atlas";

        // 납품 폴더 → 프로젝트 폴더 · 묶일 아틀라스(null = 이미 있는 아틀라스가 폴더째 물고 있음)
        private static readonly (string src, string dst, string atlas)[] Routes =
        {
            ("parts1", "Assets/BaseResource/ChapterHost", null),
            ("lobby_s22e", "Assets/BaseResource/ChapterHost", null),
            ("parts2/startmode", "Assets/BaseResource/ChapterHost", null),
            ("parts2/bottombar", "Assets/BaseResource/LobbyMainUI/Nav", null),
            ("parts2/topbar", "Assets/BaseResource/LobbyMainUI/Nav", null),
            ("lobby_nav", "Assets/BaseResource/LobbyMainUI/Nav", null),
            ("parts2/shop_a", "Assets/BaseResource/Store", "store"),
            ("parts2/shop_b", "Assets/BaseResource/Store", "store"),
            ("parts2/popups", "Assets/BaseResource/Store", "store"),
            ("parts2/products_gem", "Assets/BaseResource/Store/Products", "store"),
            ("parts2/products_misc", "Assets/BaseResource/Store/Products", "store"),
            ("parts2/summon_bg", "Assets/BaseResource/Summon", "summon"),
            ("parts2/summon_ui", "Assets/BaseResource/Summon", "summon"),
            ("parts2/fx_pillar_cyan", "Assets/BaseResource/SummonFx", "summonfx"),
            ("parts2/fx_pillar_purple", "Assets/BaseResource/SummonFx", "summonfx"),
            ("parts2/fx_pillar_gold", "Assets/BaseResource/SummonFx", "summonfx"),
            ("parts2/fx_split", "Assets/BaseResource/SummonFx", "summonfx"),
            ("parts2/fx_flip", "Assets/BaseResource/SummonFx", "summonfx"),
            ("parts2/fx_jackpot", "Assets/BaseResource/SummonFx", "summonfx"),
            ("parts2/fx_essence", "Assets/BaseResource/SummonFx", "summonfx"),
            ("parts2/pass", "Assets/BaseResource/Pass", "pass"),
            ("parts2/mission_icons", "Assets/BaseResource/Pass", "pass"),
            ("parts2/profile", "Assets/BaseResource/Profile", "profile"),
        };

        [MenuItem("Tools/Game/상점 · 소환 · 패스 부품 넣기")]
        public static void Apply()
        {
            var copied = new List<string>();
            int same = 0;
            foreach (var (src, dst, _) in Routes)
            {
                var from = $"{OutDir}/{src}";
                if (!Directory.Exists(from)) continue;
                Directory.CreateDirectory(dst);
                foreach (var f in Directory.GetFiles(from, "*.png"))
                {
                    var name = Path.GetFileName(f);
                    if (name.StartsWith("_")) continue;   // 개수가 안 맞아 이름 못 붙인 조각
                    var to = $"{dst}/{name}";
                    if (File.Exists(to) && SameFile(f, to)) { same++; continue; }
                    File.Copy(f, to, true);
                    copied.Add(to);
                }
            }
            AssetDatabase.Refresh();
            foreach (var p in copied) SetSprite(p);
            // ChapterHost 는 어느 아틀라스에도 안 묶인다 — 그림마다 안드로이드 ASTC 4x4(09_import_settings 「아틀라스 밖 그림」)
            foreach (var p in copied)
                if (p.StartsWith(OutsideAtlas)) SetAstc(p);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(AtlasGroup) ?? settings.CreateGroup(AtlasGroup, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            foreach (var g in Routes.Where(r => r.atlas != null).GroupBy(r => r.atlas))
            {
                // 하위 폴더(Store/Products)는 상위 폴더가 함께 물므로 가장 짧은 경로 하나만 PackingSource 로 건다
                var root = g.Select(r => r.dst).OrderBy(d => d.Length).First();
                if (AssetDatabase.IsValidFolder(root)) BuildAtlas(g.Key, root, settings, group);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[상점 부품] 넣음 {copied.Count}개 · 동일 {same}개\n  "
                      + string.Join("\n  ", copied.Select(Path.GetFileName)));
        }

        private const string OutsideAtlas = "Assets/BaseResource/ChapterHost/";

        private static void SetAstc(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ASTC_4x4,
            });
            ti.SaveAndReimport();
        }

        private static void SetSprite(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            // 원본은 압축 없음 — 압축은 아틀라스(ASTC)에서 한 번만. 원본까지 압축하면 묶을 때 두 번 뭉개진다
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.isReadable = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.spritePixelsPerUnit = 100f;
            ti.SaveAndReimport();
        }

        private static void BuildAtlas(string atlasName, string folder, AddressableAssetSettings settings,
                                       AddressableAssetGroup group)
        {
            var atlasPath = $"{AtlasDir}/{atlasName}.spriteatlasv2";
            var atlas = File.Exists(atlasPath) ? SpriteAtlasAsset.Load(atlasPath) : null;
            if (atlas == null)
            {
                atlas = new SpriteAtlasAsset();
                SpriteAtlasAsset.Save(atlas, atlasPath);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
                atlas = SpriteAtlasAsset.Load(atlasPath);
                atlas.Add(new[] { AssetDatabase.LoadAssetAtPath<Object>(folder) });
                SpriteAtlasAsset.Save(atlas, atlasPath);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
            }
            if (AssetImporter.GetAtPath(atlasPath) is SpriteAtlasImporter imp)
            {
                imp.packingSettings = new SpriteAtlasPackingSettings
                {
                    enableRotation = false, enableTightPacking = false, padding = 4,
                };
                imp.textureSettings = new SpriteAtlasTextureSettings
                {
                    filterMode = FilterMode.Point, generateMipMaps = false, sRGB = true, readable = false,
                };
                imp.SetPlatformSettings(new TextureImporterPlatformSettings
                {
                    name = "DefaultTexturePlatform", overridden = true, maxTextureSize = 2048,
                    textureCompression = TextureImporterCompression.Compressed,
                });
                imp.SetPlatformSettings(new TextureImporterPlatformSettings
                {
                    name = "Android", overridden = true, maxTextureSize = 2048,
                    format = TextureImporterFormat.ASTC_4x4,
                });
                imp.SaveAndReimport();
            }
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(atlasPath), group);
            entry.address = $"{AtlasGroup}/{atlasName}";
            entry.SetLabel(AtlasLabel, true);
            // 더럽힘 표시가 없으면 SaveAssets 가 주소 등록을 파일에 안 남긴다(InGamePopupCLayout 2026-10-08)
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(settings);
        }

        private static bool SameFile(string a, string b)
        {
            using (var md5 = MD5.Create())
            using (var fa = File.OpenRead(a))
            using (var fb = File.OpenRead(b))
                return md5.ComputeHash(fa).SequenceEqual(md5.ComputeHash(fb));
        }
    }
}
