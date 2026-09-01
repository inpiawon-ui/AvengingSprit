using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// 폐기된 컷. 납품 폴더에는 남아 있지만 게임이 안 쓴다 —
        /// 관 안 유령을 본편 스프라이트로 바꾸면서 가리킬 자리가 없어졌다.
        /// </summary>
        private static readonly HashSet<string> Retired = new()
        {
            "cut_start_6", "cut_start_7", "cut_start_8", "cut_start_9",
        };

        /// <summary>본편 유령이 사는 곳. 관 안 유령을 여기서 가져온다.</summary>
        private const string GhostDir = "Assets/BaseResource/Unit/ghost";

        /// <summary>
        /// 관 안 유령. 정면 대기 한 장이면 된다 —
        /// 걷기 프레임은 꼬리가 좌우로 크게 흔들려 관 안에서 펄럭이는 것처럼 보인다.
        /// 떠 있는 느낌은 코드가 위아래로 살짝 흔들어 만든다.
        /// </summary>
        public static readonly string[] GhostFrames = { "unit_ghost_s" };

        [MenuItem("Tools/Game/컷신 임포트 (cut_*)")]
        public static void Import()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var src = Path.Combine(root, InDir);
            if (!Directory.Exists(src)) { Debug.LogError($"[컷신] 납품 폴더 없음: {src}"); return; }

            // ⚠ `cut_start_6~9` 는 안 쓴다. 관 안 유령을 본편 스프라이트로 바꾸면서
            //   가리킬 자리가 없어졌다. 납품 폴더에는 원본이 남아 있으므로
            //   여기서 거르지 않으면 툴을 돌릴 때마다 되살아난다.
            var files = Directory.GetFiles(src, Prefix + "*.png")
                                 .Where(f => !Retired.Contains(Path.GetFileNameWithoutExtension(f)))
                                 .ToArray();
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

            // ⚠ 관 안 유령은 **본편 유령 스프라이트를 그대로 쓴다.**
            //
            //   납품본(`cut_start_6~9`)은 윤곽선이 없어 뿌옇고, 관보다 커서 유리 밖으로
            //   삐져나왔다. 본편 `unit_ghost_s` 는 검은 윤곽선에 주황 입까지 또렷하고,
            //   무엇보다 **플레이어가 곧 조종할 그 유령**이다 — 오프닝에서 본 것이
            //   그대로 게임에 나오는 것이 맞다. 그림을 새로 받을 이유가 없다.
            //
            //   아틀라스에도 들어 있지만 오프닝에서 유닛 아틀라스를 통째로 물 수는 없어
            //   여기에 따로 주소를 준다. 96×96 세 장이라 크기는 무시할 수준이다.
            foreach (var n in GhostFrames)
            {
                var ghostPath = $"{GhostDir}/{n}.png";
                if (File.Exists(Path.Combine(root, ghostPath))) copied.Add(ghostPath);
                else Debug.LogWarning($"[컷신] 유령 프레임 없음: {ghostPath}");
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("[컷신] Addressable 설정 없음"); return; }

            var group = settings.FindGroup(Group) ?? settings.CreateGroup(
                Group, false, false, true, null,
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(Label)) settings.AddLabel(Label);

            foreach (var path in copied)
            {
                // 본편 유령은 이미 유닛 규격으로 잡혀 있다. 여기서 다시 손대면
                // 아틀라스에 들어가는 원본 설정까지 바뀐다 — 주소만 준다.
                bool isGhost = path.StartsWith(GhostDir);
                if (!isGhost && AssetImporter.GetAtPath(path) is TextureImporter ti)
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
