// 적용할 때 Assets/Scripts/Editor/ModeUiImport.cs 로 복사한다 (2026-10-15 준비 — 유니티가 다른 세션에 쓰이는 중이라 아직 넣지 않음).
// 먼저 python Projects/AVSR/Tools/apply_mode_ui_parts.py 로 그림을 옮긴 뒤 메뉴를 돌린다.
// 목록 · 값의 정본: Projects/AVSR/wireframes/AVSR_AssetManifest_Modes.md (apply_mode_ui_parts.py 의 MAP 과 같다).
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.Editor
{
    /// <summary>
    /// 추가 모드(망령의 탑 · 망령의 밤) UI 부품 임포트 설정 · 아틀라스 (규칙 09_import_settings).
    /// 도트 그림 — Sprite · Point · 밉맵 끔 · Read/Write 끔. 압축은 아틀라스에서(안드로이드 ASTC 4x4).
    /// 아틀라스 밖 큰 바탕(ModeBackdrop)은 그림 자체에 안드로이드 ASTC 4x4 · 최대 2048.
    /// 여러 번 돌려도 같다.
    /// </summary>
    public static class ModeUiImport
    {
        private const string Base = "Assets/BaseResource";
        private const string AtlasDir = "Assets/BundleResource/Atlas";

        // 폴더/이름 → 9분할 (L, B, R, T). 없으면 Vector4.zero
        private static readonly Dictionary<string, Vector4> Parts = new()
        {
            { "InGameMainUI/tower_plate", Vector4.zero },
            { "InGameMainUI/night_plate", Vector4.zero },
            { "InGameMainUI/mode_rule_chip", new Vector4(23, 0, 23, 0) },
            { "InGameMainUI/mode_intro_band", Vector4.zero },
            { "InGameMainUI/bighost_plate", Vector4.zero },
            { "ModeCommon/mode_frame", new Vector4(16, 16, 16, 16) },
            { "ModeCommon/mode_entry_pip_on", Vector4.zero },
            { "ModeCommon/mode_entry_pip_off", Vector4.zero },
            { "TowerEntryPanel/tower_ladder_cell_past", new Vector4(24, 24, 24, 24) },
            { "TowerEntryPanel/tower_ladder_cell_next", new Vector4(24, 24, 24, 24) },
            { "TowerEntryPanel/tower_ladder_cell_now", new Vector4(26, 26, 26, 26) },
            { "TowerEntryPanel/tower_ladder_cell_boss", new Vector4(24, 24, 24, 24) },
            { "TowerEntryPanel/tower_badge_go", new Vector4(12, 0, 12, 0) },
            { "TowerEntryPanel/tower_floor_card", new Vector4(24, 24, 24, 24) },
            { "NightEntryPanel/night_day_cell", Vector4.zero },
            { "NightEntryPanel/night_day_cell_today", Vector4.zero },
            { "NightEntryPanel/night_today_card", new Vector4(40, 40, 40, 40) },
            { "NightEntryPanel/night_today_art", Vector4.zero },
            { "NightResultPopup/night_result_frame", Vector4.zero },
            { "LobbyMainUI/modeart_tower", Vector4.zero },
        };

        private static readonly string[] Backdrops = { "ModeBackdrop/tower_panel_bg", "ModeBackdrop/night_panel_bg" };

        // 새로 만드는 아틀라스 — InGameMainUI · LobbyMainUI 는 있는 아틀라스가 폴더째 묶는다
        private static readonly string[] NewAtlases = { "ModeCommon", "TowerEntryPanel", "NightEntryPanel", "NightResultPopup" };

        [MenuItem("Tools/Game/모드 UI/임포트 설정")]
        public static void Apply()
        {
            int done = 0;
            foreach (var kv in Parts)
            {
                string path = $"{Base}/{kv.Key}.png";
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti)
                {
                    Debug.LogWarning($"[모드 UI] 없음: {path} — apply_mode_ui_parts.py 를 먼저");
                    continue;
                }
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.isReadable = false;
                ti.alphaIsTransparency = true;
                // 원본은 압축 없이 — 압축은 아틀라스(ASTC 4x4)에서 한다(규칙 09). 원본이 압축이면 묶을 때 경고가 나고 두 번 깎인다
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.spriteBorder = kv.Value;
                ti.SaveAndReimport();
                done++;
            }

            foreach (var key in Backdrops)
            {
                string path = $"{Base}/{key}.png";
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.isReadable = false;
                ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android", overridden = true, maxTextureSize = 2048,
                    format = TextureImporterFormat.ASTC_4x4,
                });
                ti.SaveAndReimport();
                done++;
            }

            foreach (var name in NewAtlases) EnsureAtlas(name);
            AssetDatabase.SaveAssets();
            Debug.Log($"[모드 UI] 임포트 설정 {done}장 · 아틀라스 {NewAtlases.Length}개 확인");
        }

        /// <summary>폴더 하나를 묶는 아틀라스 — Atlas 폴더에 두면 AtlasAddressablePostprocessor 가 주소를 건다.</summary>
        private static void EnsureAtlas(string name)
        {
            string folder = $"{Base}/{name}";
            string atlasPath = $"{AtlasDir}/{name}.spriteatlasv2";
            if (!AssetDatabase.IsValidFolder(folder)) return;
            if (AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>(atlasPath) == null
                && !System.IO.File.Exists(atlasPath))
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new[] { AssetDatabase.LoadAssetAtPath<Object>(folder) });
                SpriteAtlasAsset.Save(atlas, atlasPath);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
            }
            if (AssetImporter.GetAtPath(atlasPath) is not SpriteAtlasImporter imp) return;
            imp.packingSettings = new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4 };
            imp.textureSettings = new SpriteAtlasTextureSettings { filterMode = FilterMode.Point, generateMipMaps = false, sRGB = true, readable = false };
            imp.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ASTC_4x4,
            });
            imp.SaveAndReimport();
            RegisterAtlas(atlasPath, name);
        }

        /// <summary>
        /// 아틀라스를 Addressable `atlas` 그룹에 건다 — 주소 `atlas/{소문자}` · 라벨 `label_atlas`.
        /// Atlas 폴더 자동 등록기는 지금 없다(규칙 02 의 설명과 다르다) — 다른 빌더처럼 직접 건다.
        /// </summary>
        private static void RegisterAtlas(string atlasPath, string name)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings != null ? settings.FindGroup("atlas") : null;
            if (group == null) { Debug.LogError("[모드 UI] Addressable 그룹 atlas 없음"); return; }
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(atlasPath), group);
            entry.address = $"atlas/{name.ToLowerInvariant()}";
            entry.SetLabel("label_atlas", true);
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();   // 안 하면 주소 등록이 파일에 안 남는다(InGamePopupCLayout 2026-10-08)
        }
    }
}
