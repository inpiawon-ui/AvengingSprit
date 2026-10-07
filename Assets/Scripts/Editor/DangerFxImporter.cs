using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 보스 · 장애물 위험 예고 그림(`DangerView`) 6장을 들인다 (2026-10-07 「PPT 도형 같다」).
    ///
    ///   원본  Assets/BundleResource/ParticleFx/Danger/fx_{danger|safe}_{fill|core|edge}.png
    ///   주소  Fx/danger_fill … Fx/safe_edge   · 그룹 particlefx (파티클 그림과 같이 묶는다)
    ///
    /// ⚠ 스프라이트로 들이지 않는다 — 아틀라스에 묶이면 타일이 안 돈다(`DangerView.CanTile` 주석).
    ///   도형 위에 무늬를 반복하므로 Wrap 은 **Repeat** 이어야 한다. 테두리 띠는 세로로 반복하면 안 되어 U 만 Repeat.
    /// </summary>
    public static class DangerFxImporter
    {
        private const string Dir = "Assets/BundleResource/ParticleFx/Danger";
        private const string Group = "particlefx";
        private const string Label = "label_particlefx";

        private static readonly string[] Keys =
            { "danger_fill", "danger_core", "danger_edge", "safe_fill", "safe_core", "safe_edge" };

        [MenuItem("Tools/Game/위험 예고 그림 들이기")]
        public static void Import()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("[DangerFx] Addressable 설정이 없다"); return; }
            var group = settings.FindGroup(Group) ?? settings.CreateGroup(
                Group, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(Label)) settings.AddLabel(Label);

            int made = 0;
            foreach (var key in Keys)
            {
                string path = $"{Dir}/fx_{key}.png";
                if (!File.Exists(path)) { Debug.LogWarning($"[DangerFx] 그림 없음: {path}"); continue; }
                AssetDatabase.ImportAsset(path);
                if (AssetImporter.GetAtPath(path) is TextureImporter ti)
                {
                    ti.textureType = TextureImporterType.Default;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = false;
                    ti.filterMode = FilterMode.Bilinear;   // 도형 위에서 늘었다 줄었다 하므로 점 필터는 반짝인다
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.wrapModeU = TextureWrapMode.Repeat;
                    ti.wrapModeV = key.EndsWith("_edge") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                    ti.SaveAndReimport();
                }
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                entry.address = $"Fx/{key}";
                entry.SetLabel(Label, true);
                made++;
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DangerFx] {made}/{Keys.Length}장 — 주소 Fx/*");
        }
    }
}
