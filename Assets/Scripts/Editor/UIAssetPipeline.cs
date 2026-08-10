using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.Editor
{
    /// <summary>
    /// 납품 UI 스프라이트를 픽셀아트 규율에 맞게 임포트하고, 화면별 아틀라스로 묶어
    /// Addressable 에 등록한다.
    ///
    /// 입력 : Assets/Scripts/Editor/UISpec/_import.json  (파일명·규격·9slice·pivot·아틀라스)
    /// 텍스처: Assets/BaseResource/{프리팹명}/*.png        — 아틀라스 PackingSource
    /// 아틀라스: Assets/BundleResource/Atlas/{소문자}.spriteatlasv2 — Addressable 등록 대상
    ///
    /// 개별 스프라이트는 Addressable 에 등록하지 않는다 (02_addressables.md).
    /// </summary>
    public static class UIAssetPipeline
    {
        private const string SpecPath = "Assets/Scripts/Editor/UISpec/_import.json";
        private const string BaseRes = "Assets/BaseResource";
        private const string UnitRes = "Assets/BaseResource/Unit";
        private const string AtlasDir = "Assets/BundleResource/Atlas";

        // 페이지 상한.
        // 4096 자체는 요즘 모바일 GPU 가 문제없이 지원한다(ES 3.0 이 4096 최소 보장).
        // 걸리는 건 하드웨어가 아니라 **무압축**이다 — 픽셀아트는 블록 압축에서
        // 하드 엣지와 알파가 뭉개져 압축을 못 쓴다. 무압축 RGBA32 로
        // 4096×4096 은 한 장이 64MB 다.
        // 2048 로 묶으면 내용이 넘칠 때 64MB 한 장 대신 16MB 여러 장으로 쪼개진다.
        private const int ScreenAtlasMaxSize = 2048;
        private const int UnitAtlasMaxSize = 1024;   // 한 종은 방향 5장 + 프레임 몇 장이 전부
        private const string AtlasGroup = "atlas";
        private const string AtlasLabel = "label_atlas";

        [Serializable]
        private sealed class Asset
        {
            public string file;
            public string prefab;
            public string atlas;
            public int w, h;
            public string alpha;
            public string pivot;
            public int[] slice9;
        }

        [Serializable]
        private sealed class Spec { public Asset[] assets; }

        [MenuItem("Tools/Game/Import UI Assets And Pack Atlas")]
        public static void Run()
        {
            if (!File.Exists(SpecPath))
            {
                Debug.LogError($"[UIAssetPipeline] 스펙 없음: {SpecPath}");
                return;
            }
            var spec = JsonUtility.FromJson<Spec>(File.ReadAllText(SpecPath));
            if (spec?.assets == null || spec.assets.Length == 0)
            {
                Debug.LogError("[UIAssetPipeline] 스펙 파싱 실패");
                return;
            }

            int done = 0, miss = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var a in spec.assets)
                {
                    var path = $"{BaseRes}/{a.prefab}/{a.file}";
                    if (ApplyImporter(path, a)) done++;
                    else miss++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            Debug.Log($"[UIAssetPipeline] 임포터 설정 {done}개 (누락 {miss})");

            BuildAtlases(spec.assets);
            PackAtlases();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 매니페스트(_import.json)를 거치지 않고 들어온 낱장 PNG 를 임포트한다.
        /// 방향 스프라이트·공격 프레임처럼 화면 설계서에 없는 파일이 대상이다.
        ///
        /// 아틀라스는 폴더 오브젝트를 PackingSource 로 잡으므로(BuildAtlases 참조),
        /// **Sprite 타입으로만 만들면 아틀라스에 자동 수록된다.** 아틀라스 재구성은 필요 없다.
        /// 그냥 두면 Unity 기본값(Default 타입·Bilinear·압축)으로 들어와 도트가 뭉개지고
        /// `SpriteAtlas.GetSprite()` 가 null 을 준다.
        /// </summary>
        [MenuItem("Tools/Game/Import Loose Sprites And Repack")]
        public static void ImportLooseSprites()
        {
            var targets = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { BaseRes }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter ti
                    && ti.textureType != TextureImporterType.Sprite)
                    targets.Add(path);
            }

            if (targets.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var p in targets) ApplyImporter(p, new Asset { pivot = "center" });
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh();
                }
                Debug.Log($"[UIAssetPipeline] 낱장 임포트 {targets.Count}개 — "
                          + string.Join(", ", targets.Select(Path.GetFileName)));
            }
            else
            {
                Debug.Log("[UIAssetPipeline] 새 낱장 없음 — 리팩만 한다.");
            }

            // 캐릭터 폴더가 늘거나(신규 캐릭터) 프레임이 추가되면 여기서 따라간다.
            // 새 폴더를 만들고 이 메뉴만 돌리면 아틀라스·주소까지 붙는다.
            BuildUnitAtlases();

            foreach (var p in Directory.GetFiles(AtlasDir, "*.spriteatlasv2"))
                DedupePackables(p.Replace('\\', '/'));

            // 대상이 없어도 리팩은 항상 한다. 기존 PNG 를 덮어썼을 때
            // (재납품·재정렬) 아틀라스 안의 그림이 옛 것으로 남는 것을 막는다.
            PackAtlases();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 아틀라스의 Objects for Packing 에서 중복을 걷어낸다.
        ///
        /// `SpriteAtlasAsset.Add()` 는 이미 들어 있는지 보지 않고 그냥 덧붙인다.
        /// 그래서 파이프라인을 돌릴 때마다 같은 폴더가 한 줄씩 쌓인다 —
        /// 실제로 `hostselectpanel` 에 30줄까지 늘어나 있었다.
        /// 팩 결과물은 같지만 목록을 읽을 수 없게 되고 팩 시간도 늘어난다.
        /// </summary>
        private static bool DedupePackables(string atlasPath)
        {
            var loaded = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (loaded == null) return false;

            var packables = SpriteAtlasExtensions.GetPackables(loaded);
            if (packables == null || packables.Length == 0) return false;

            var distinct = packables.Where(o => o != null).Distinct().ToArray();
            if (distinct.Length == packables.Length) return false;

            var asset = SpriteAtlasAsset.Load(atlasPath);
            if (asset == null) return false;

            asset.Remove(packables);      // 중복분까지 통째로 걷어낸 뒤
            asset.Add(distinct);          // 하나씩만 다시 넣는다
            SpriteAtlasAsset.Save(asset, atlasPath);
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);

            Debug.Log($"[UIAssetPipeline] {Path.GetFileNameWithoutExtension(atlasPath)} "
                      + $"PackingSource {packables.Length} → {distinct.Length} (중복 제거)");
            return true;
        }

        /// <summary>
        /// 아틀라스를 실제로 팩한다.
        /// `spritePackerMode` 가 Disabled 면 런타임에 `SpriteAtlas.GetSprite()` 가 항상 null 을 준다
        /// (에셋은 로드되지만 spriteCount = 0). 에디터 플레이에서도 동작하도록 V2 모드로 켠다.
        /// </summary>
        private static void PackAtlases()
        {
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
            {
                EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
                Debug.Log("[UIAssetPipeline] spritePackerMode → SpriteAtlasV2 (Disabled 였음)");
            }
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);

            foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(p);
                if (a != null)
                    Debug.Log($"[UIAssetPipeline] 팩 완료 {Path.GetFileNameWithoutExtension(p)} — 스프라이트 {a.spriteCount}개");
            }
        }

        private static bool ApplyImporter(string path, Asset a)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
            {
                Debug.LogWarning($"[UIAssetPipeline] 없음: {path}");
                return false;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.isReadable = false;

            // 픽셀아트 규율 — Point 필터, 무압축. 보간·압축은 도트를 뭉갠다.
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(Mathf.Max(a.w, a.h)));

            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;   // UI·9-slice 필수
            st.spriteAlignment = (int)(a.pivot == "left"
                ? SpriteAlignment.LeftCenter
                : SpriteAlignment.Center);
            // 9-slice 경계 (L, B, R, T) 순서 — Unity 규약
            st.spriteBorder = a.slice9 != null && a.slice9.Length == 4
                ? new Vector4(a.slice9[0], a.slice9[3], a.slice9[1], a.slice9[2])
                : Vector4.zero;
            ti.SetTextureSettings(st);

            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            return true;
        }

        private static void BuildAtlases(Asset[] assets)
        {
            EnsureFolder(AtlasDir);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[UIAssetPipeline] Addressable 설정이 없다. Window > Asset Management > Addressables 로 초기화할 것.");
                return;
            }
            var group = settings.FindGroup(AtlasGroup) ?? settings.CreateGroup(
                AtlasGroup, false, false, true, null, typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(AtlasLabel)) settings.AddLabel(AtlasLabel);

            foreach (var g in assets.GroupBy(x => x.atlas))
                BuildOneAtlas(g.Key, $"{BaseRes}/{g.First().prefab}", settings, group, ScreenAtlasMaxSize);

            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// 캐릭터(유닛) 아틀라스를 캐릭터 폴더 하나당 하나씩 만든다.
        ///
        /// 화면 아틀라스에 섞어 두면 안 되는 이유:
        /// 한 방에서 실제로 쓰는 캐릭터는 3~4종인데, 화면 아틀라스는 통짜라
        /// 12종을 전부 메모리에 올린다. 방향 5장에 공격 프레임까지 붙으면
        /// 한 종이 20장이 되어 (14종 × 20 = 280장) 한 페이지에 들어가지도 않는다.
        /// 캐릭터별로 쪼개면 한 종이 512×512(무압축 1MB)로 끝나고,
        /// 방이 필요한 것만 올리면 된다.
        ///
        /// 폴더를 PackingSource 로 잡으므로, 새 프레임을 폴더에 떨어뜨리고
        /// 이 메뉴만 돌리면 자동으로 수록된다.
        /// </summary>
        [MenuItem("Tools/Game/Build Unit Atlases")]
        public static void BuildUnitAtlases()
        {
            if (!AssetDatabase.IsValidFolder(UnitRes))
            {
                Debug.LogError($"[UIAssetPipeline] 캐릭터 폴더 없음: {UnitRes}");
                return;
            }
            EnsureFolder(AtlasDir);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[UIAssetPipeline] Addressable 설정이 없다.");
                return;
            }
            var group = settings.FindGroup(AtlasGroup) ?? settings.CreateGroup(
                AtlasGroup, false, false, true, null,
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(AtlasLabel)) settings.AddLabel(AtlasLabel);

            int n = 0;
            foreach (var folder in AssetDatabase.GetSubFolders(UnitRes))
            {
                var key = Path.GetFileName(folder);
                BuildOneAtlas($"unit_{key}", folder, settings, group, UnitAtlasMaxSize);
                n++;
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIAssetPipeline] 캐릭터 아틀라스 {n}개 갱신");
        }

        /// <summary>아틀라스 하나를 만들고 폴더를 PackingSource 로 물린 뒤 Addressable 에 등록한다.</summary>
        private static void BuildOneAtlas(string atlasName, string folderPath,
                                          AddressableAssetSettings settings,
                                          AddressableAssetGroup group, int maxTextureSize)
        {
            var atlasPath = $"{AtlasDir}/{atlasName}.spriteatlasv2";

            // v2 아틀라스는 AssetDatabase.LoadAssetAtPath 로 못 불러온다. 전용 API 사용.
            var atlas = File.Exists(atlasPath) ? SpriteAtlasAsset.Load(atlasPath) : null;
            if (atlas == null)
            {
                atlas = new SpriteAtlasAsset();
                SpriteAtlasAsset.Save(atlas, atlasPath);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
                atlas = SpriteAtlasAsset.Load(atlasPath);
            }
            if (atlas == null)
            {
                Debug.LogError($"[UIAssetPipeline] 아틀라스 로드 실패: {atlasPath}");
                return;
            }

            // 폴더 오브젝트 전체를 PackingSource 로 등록 (05_prefabs.md)
            var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
            if (folder != null) atlas.Add(new[] { folder });

            SpriteAtlasAsset.Save(atlas, atlasPath);
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
            DedupePackables(atlasPath);

            // 팩킹·텍스처 설정은 Importer 경유 (SpriteAtlasAsset 쪽은 obsolete)
            if (AssetImporter.GetAtPath(atlasPath) is SpriteAtlasImporter imp)
            {
                imp.packingSettings = new SpriteAtlasPackingSettings
                {
                    enableRotation = false,       // UI 스프라이트 회전 금지
                    enableTightPacking = false,
                    padding = 4,
                };
                imp.textureSettings = new SpriteAtlasTextureSettings
                {
                    filterMode = FilterMode.Point,   // 픽셀아트
                    generateMipMaps = false,
                    sRGB = true,
                };
                // 무압축 — 픽셀아트는 블록 압축에서 색이 뭉개진다
                imp.SetPlatformSettings(new TextureImporterPlatformSettings
                {
                    name = "DefaultTexturePlatform",
                    overridden = true,
                    textureCompression = TextureImporterCompression.Uncompressed,
                    maxTextureSize = maxTextureSize,
                });
                imp.SaveAndReimport();
            }

            // Addressable 등록 — 주소 `atlas/{소문자}`, 라벨 `label_atlas`
            var guid = AssetDatabase.AssetPathToGUID(atlasPath);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = $"{AtlasGroup}/{atlasName}";
            entry.SetLabel(AtlasLabel, true);

            Debug.Log($"[UIAssetPipeline] 아틀라스 {atlasName} ← {folderPath} → {entry.address}");
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
