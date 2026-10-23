// RE:BORN UI 리뉴얼(2026-10-23) — 키트 리소스 임포트 설정 · 아틀라스 (규칙 09_import_settings).
// 먼저 python Projects/AVSR/Tools/ui_reborn_import.py 로 그림을 옮긴 뒤 메뉴를 돌린다.
// 묶음에 들어 있던 임포터(RGBA32 · 최대 8192)는 쓰지 않는다 — PD 「우리 규칙 기준으로」.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.Editor
{
    /// <summary>
    /// `Assets/BaseResource/ReBornKit/` 임포트 설정.
    /// 아이콘 · 워드아트 = 도트 그림 → Point · 압축은 아틀라스(안드로이드 ASTC 4x4).
    /// 일러스트(Art) = 아틀라스 밖 큰 그림 → Bilinear · 그림 자체에 ASTC 4x4 · 최대 크기 ≥ 원본 긴 변.
    /// 숫자(Numbers) = 비트맵 글꼴 원본 → Point · ASTC 4x4. 여러 번 돌려도 같다.
    /// </summary>
    public static class ReBornKitImport
    {
        private const string Kit = "Assets/BaseResource/ReBornKit";
        private const string AtlasDir = "Assets/BundleResource/Atlas";

        // 하위 폴더 → (아틀라스 이름, 필터). 판 · 버튼 · HUD 는 매끈한 그라데이션이라 Bilinear(도트 아님 — 묶음 지시 pixel:false)
        private static readonly Dictionary<string, (string atlas, FilterMode filter)> Atlases = new()
        {
            { "Icons", ("ReBornIcons", FilterMode.Point) },
            { "WordArt", ("ReBornWordArt", FilterMode.Point) },
            { "Components", ("ReBornComponents", FilterMode.Bilinear) },
            { "Hud", ("ReBornHud", FilterMode.Bilinear) },
        };

        [MenuItem("Tools/Game/RE:BORN 키트/임포트 설정")]
        public static void Apply()
        {
            int done = 0;
            var borders = LoadBorders();
            foreach (var kv in Atlases) done += SetFolder($"{Kit}/{kv.Key}", kv.Value.filter, false, borders);
            done += SetFolder($"{Kit}/Numbers", FilterMode.Point, true, borders);
            done += SetFolder($"{Kit}/Art", FilterMode.Bilinear, true, borders);
            foreach (var kv in Atlases) EnsureAtlas($"{Kit}/{kv.Key}", kv.Value.atlas, kv.Value.filter);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RE:BORN 키트] 임포트 설정 {done}장 · 아틀라스 {Atlases.Count}개 · 9-slice {borders.Count}개");
        }

        /// <summary>
        /// 9-slice 테두리(L · B · R · T) — 자르기 도구(cut_ui_reborn.py)가 그림에서 잰 값. 키는 `폴더/파일이름`.
        /// JsonUtility 는 사전을 못 읽어 줄 단위로 읽는다(도구가 한 값씩 줄을 바꿔 쓴다).
        /// </summary>
        private static Dictionary<string, Vector4> LoadBorders()
        {
            var map = new Dictionary<string, Vector4>();
            string path = $"{Kit}/borders.json";
            if (!System.IO.File.Exists(path)) return map;
            string key = null;
            var nums = new List<float>(4);
            foreach (var raw in System.IO.File.ReadAllLines(path))
            {
                string line = raw.Trim().TrimEnd(',');
                if (line.StartsWith("\"") && line.Contains(":"))
                {
                    key = line.Substring(1, line.IndexOf('"', 1) - 1);
                    nums.Clear();
                }
                else if (key != null && float.TryParse(line, out float v))
                {
                    nums.Add(v);
                    if (nums.Count == 4) { map[key] = new Vector4(nums[0], nums[1], nums[2], nums[3]); key = null; }
                }
            }
            return map;
        }

        /// <summary>
        /// 폴더 안 PNG 전부. standalone 이면 그림 자체에 안드로이드 ASTC 4x4(아틀라스 밖),
        /// 아니면 압축 없이 두고 아틀라스에서 압축한다(두 번 깎이지 않게).
        /// </summary>
        private static int SetFolder(string folder, FilterMode filter, bool standalone, Dictionary<string, Vector4> borders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return 0;
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = filter;
                ti.mipmapEnabled = false;
                ti.isReadable = false;
                ti.alphaIsTransparency = true;
                ti.npotScale = TextureImporterNPOTScale.None;
                // 키 = `폴더/파일이름`(키트 기준) — 표에 없으면 9-slice 없음
                string key = path.Substring(Kit.Length + 1);
                key = key.Substring(0, key.Length - System.IO.Path.GetExtension(key).Length);
                ti.spriteBorder = borders.TryGetValue(key, out var b) ? b : Vector4.zero;
                if (standalone)
                {
                    ti.GetSourceTextureWidthAndHeight(out int w, out int h);
                    ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    {
                        name = "Android", overridden = true, maxTextureSize = MaxSizeFor(Mathf.Max(w, h)),
                        format = TextureImporterFormat.ASTC_4x4,
                    });
                }
                else
                {
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                }
                ti.SaveAndReimport();
                n++;
            }
            return n;
        }

        // 최대 크기가 원본보다 작으면 줄어 흐려진다(규칙 09) — 원본 긴 변 이상인 2의 거듭제곱
        private static int MaxSizeFor(int longSide)
        {
            int size = 32;
            while (size < longSide && size < 8192) size *= 2;
            return size;
        }

        private static void EnsureAtlas(string folder, string name, FilterMode filter)
        {
            string atlasPath = $"{AtlasDir}/{name}.spriteatlasv2";
            if (!AssetDatabase.IsValidFolder(folder)) return;
            if (!System.IO.File.Exists(atlasPath))
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new[] { AssetDatabase.LoadAssetAtPath<Object>(folder) });
                SpriteAtlasAsset.Save(atlas, atlasPath);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
            }
            if (AssetImporter.GetAtPath(atlasPath) is not SpriteAtlasImporter imp) return;
            imp.packingSettings = new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4 };
            imp.textureSettings = new SpriteAtlasTextureSettings { filterMode = filter, generateMipMaps = false, sRGB = true, readable = false };
            imp.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ASTC_4x4,
            });
            imp.SaveAndReimport();
            RegisterAtlas(atlasPath, name);
        }

        /// <summary>Addressable `atlas` 그룹 — 주소 `atlas/{소문자}` · 라벨 `label_atlas`(ModeUiImport 와 같다).</summary>
        private static void RegisterAtlas(string atlasPath, string name)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings != null ? settings.FindGroup("atlas") : null;
            if (group == null) { Debug.LogError("[RE:BORN 키트] Addressable 그룹 atlas 없음"); return; }
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(atlasPath), group);
            entry.address = $"atlas/{name.ToLowerInvariant()}";
            entry.SetLabel("label_atlas", true);
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
