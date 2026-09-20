using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 파티클 재질 만들기 (2026-09-20).
    ///
    /// `Assets/BaseResource/ParticleFx/*.png` (원본 그림)을 읽어
    /// `Assets/BundleResource/ParticleFx/pfx_*.mat` (런타임이 쓰는 재질)을 만든다.
    ///
    /// ── 왜 재질이 따로 필요한가 ──────────────────────────────
    /// 파티클 시스템은 **스프라이트를 못 받는다.** 렌더러가 받는 것은 재질뿐이라,
    /// 아틀라스에 넣어 둔 그림을 그대로 쓸 수 없다(아틀라스 안의 한 칸만 그릴 방법이 없다).
    /// 그래서 파티클 그림만은 아틀라스에 넣지 않고 **낱장 텍스처 + 재질**로 둔다.
    ///
    /// ── 어디에 두나 ──────────────────────────────────────────
    /// 원본 그림은 `BaseResource`(번들 아님), 런타임이 로드하는 재질은 `BundleResource`.
    /// 아틀라스와 같은 구조다 — 재질이 텍스처를 참조하므로 번들에 함께 실린다.
    ///
    /// 메뉴: Tools/Game/파티클/파티클 재질 만들기
    /// </summary>
    public static class CreateParticleFx
    {
        private const string SourceDir = "Assets/BaseResource/ParticleFx";
        private const string MaterialDir = "Assets/BundleResource/ParticleFx";
        private const string Group = "particlefx";
        private const string Label = "label_particlefx";

        /// <summary>
        /// 더하기로 섞을 그림 — **빛나는 것**. 겹칠수록 밝아져야 번쩍임이 된다.
        /// 덩어리(연기·파편)는 빛이 아니라 물체라 보통 알파 합성이다 — 겹쳐도 밝아지면 안 된다.
        /// </summary>
        /// <summary>⚠ 색조만 돌린 변형(`spark_ice` 등)도 같은 합성을 써야 한다 — 이름 앞머리로 가린다.</summary>
        private static readonly string[] Additive = { "spark", "glow", "ember", "streak", "ring", "star4" };

        private static bool IsAdditive(string key)
        {
            foreach (var a in Additive)
                if (key == a || key.StartsWith(a + "_")) return true;
            return false;
        }

        [MenuItem("Tools/Game/파티클/파티클 재질 만들기")]
        public static void Build()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("[ParticleFx] Addressable 설정이 없다"); return; }
            if (!Directory.Exists(SourceDir)) { Debug.LogError($"[ParticleFx] 원본 폴더가 없다: {SourceDir}"); return; }
            Directory.CreateDirectory(MaterialDir);

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) { Debug.LogError("[ParticleFx] URP 파티클 셰이더를 못 찾았다"); return; }

            var group = settings.FindGroup(Group) ?? settings.CreateGroup(
                Group, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            if (!settings.GetLabels().Contains(Label)) settings.AddLabel(Label);

            int made = 0;
            foreach (var png in Directory.GetFiles(SourceDir, "*.png").OrderBy(p => p))
            {
                string path = png.Replace('\\', '/');
                string key = Path.GetFileNameWithoutExtension(path);
                ApplyImport(path);

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) { Debug.LogWarning($"[ParticleFx] 텍스처를 못 읽었다: {path}"); continue; }

                string matPath = $"{MaterialDir}/pfx_{key}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
                mat.shader = shader;
                SetupBlend(mat, IsAdditive(key));
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(mat);

                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(matPath), group);
                entry.address = $"ParticleFx/{key}";
                entry.SetLabel(Label, true);
                made++;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ParticleFx] 재질 {made}개 — {MaterialDir} · 주소 ParticleFx/*");
        }

        /// <summary>
        /// 파티클 그림은 **스프라이트가 아니다.** 아틀라스에 끌려 들어가지 않게 Default 로 두고,
        /// 픽셀이 뭉개지지 않게 Point 필터 · 밉맵 없음으로 맞춘다.
        /// </summary>
        private static void ApplyImport(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            bool dirty = false;
            if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; dirty = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; dirty = true; }
            if (ti.mipmapEnabled) { ti.mipmapEnabled = false; dirty = true; }
            if (ti.filterMode != FilterMode.Point) { ti.filterMode = FilterMode.Point; dirty = true; }
            if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            if (dirty) ti.SaveAndReimport();
        }

        /// <summary>
        /// URP 파티클 셰이더는 **키워드와 블렌드 값을 직접 맞춰 줘야** 한다 —
        /// 인스펙터(ShaderGUI)가 해 주던 일을 코드가 대신한다.
        /// </summary>
        private static void SetupBlend(Material mat, bool additive)
        {
            mat.SetFloat("_Surface", 1f);                     // Transparent
            mat.SetFloat("_Blend", additive ? 2f : 0f);       // 2 = Additive · 0 = Alpha
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(additive
                ? UnityEngine.Rendering.BlendMode.One
                : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_ColorMode", 0f);                   // Multiply — 색은 런타임이 입힌다
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
