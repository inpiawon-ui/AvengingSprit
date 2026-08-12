using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace Game.EditorTools
{
    /// <summary>
    /// 원작 시스템 폰트(비트맵) → `TMP_FontAsset`.
    ///
    /// TMP 는 보통 TTF 에서 폰트 에셋을 굽지만, 도트 폰트는 TTF 로 만들면 힌팅에
    /// 뭉개진다. 아틀라스와 글리프 표를 직접 채워 **정적 비트맵 폰트**로 만든다.
    ///
    /// 원작 폰트에는 **영문 대문자·숫자·기호만** 있고 한글이 없다. 그래서 이것을
    /// 주 폰트로 두고 기존 한글 폰트를 대체(fallback)로 연결한다 — HUD 의
    /// GHOST·HOST·STAGE·숫자처럼 도트 느낌이 가장 중요한 곳이 전부 영문·숫자다.
    ///
    /// 아틀라스는 `Projects/AVSR/_orig_font.py` 가 만든다.
    /// </summary>
    public static class BuildOriginalFont
    {
        private const string AtlasPath = "Assets/BaseResource/Fonts/orig_font_atlas.png";
        private const string OutPath = "Assets/BaseResource/Fonts/OriginalPixel SDF.asset";
        private const string KoreanPath = "Assets/BaseResource/Fonts/NotoSansKR SDF.asset";

        private const int Cell = 9;
        private const int Cols = 16;
        private const int Rows = 4;
        private const int First = 0x20;

        /// <summary>TMP 가 읽기 전용으로 노출한 값은 뒷 필드에 직접 넣는다.</summary>
        private static void Set(object target, string field, object value)
        {
            var f = target.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f == null) { Debug.LogError($"[Font] 필드 없음: {field}"); return; }
            f.SetValue(target, value);
        }

        [MenuItem("Tools/Game/Build Original Pixel Font")]
        public static void Run()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (tex == null)
            {
                Debug.LogError($"[Font] 아틀라스 없음: {AtlasPath} — _orig_font.py 를 먼저 돌린다");
                return;
            }

            // 아틀라스는 픽셀 그대로 읽어야 한다. 압축·밉맵이 켜져 있으면 글자가 뭉개진다.
            var imp = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            imp.textureType = TextureImporterType.Default;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            // 144x36 은 2의 거듭제곱이 아니다. 기본값(ToNearest)이면 128x32 로 줄여 버려
            // 9px 격자가 통째로 어긋난다.
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.isReadable = true;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

            var font = ScriptableObject.CreateInstance<TMP_FontAsset>();
            font.name = "OriginalPixel SDF";
            font.atlasTextures = new[] { tex };
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            // 아래 값들은 TMP 가 읽기 전용으로 열어 둔 것이라 뒷 필드에 직접 넣는다.
            // 비트맵이므로 SDF 가 아니다 — RenderMode 를 틀리게 두면 셰이더가
            // 외곽선을 만들려다 글자가 흐려진다.
            Set(font, "m_AtlasWidth", tex.width);
            Set(font, "m_AtlasHeight", tex.height);
            Set(font, "m_AtlasPadding", 0);
            Set(font, "m_AtlasRenderMode", GlyphRenderMode.RASTER);
            // 버전을 비워 두면 TMP 가 "구버전 에셋 업그레이드" 경로를 타면서
            // 아직 채우지 않은 표를 읽어 NRE 를 낸다.
            Set(font, "m_Version", "1.1.0");

            font.faceInfo = new FaceInfo
            {
                familyName = "OriginalPixel",
                styleName = "Regular",
                pointSize = Cell,
                scale = 1f,
                lineHeight = Cell + 2,
                ascentLine = Cell,
                baseline = 0f,
                descentLine = 0f,
                capLine = Cell,
                meanLine = Cell * 0.6f,
                underlineOffset = -1f,
                underlineThickness = 1f,
                strikethroughOffset = Cell * 0.4f,
                strikethroughThickness = 1f,
                tabWidth = Cell * 4,
            };

            var glyphs = new List<Glyph>();
            var chars = new List<TMP_Character>();
            var pixels = tex.GetPixels32();

            uint index = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    int code = First + r * Cols + c;

                    // 아틀라스는 위에서 아래로 만들었고 텍스처 좌표는 아래에서 위다.
                    int rectY = tex.height - (r + 1) * Cell;
                    int rectX = c * Cell;

                    // 글자가 실제로 차지하는 범위.
                    //
                    // 가로를 재는 이유는 좁은 글자(I·.)를 좁게 흘리기 위해서다.
                    // 세로도 함께 재야 한다 — 칸을 통째로(9px) 쓰면 글리프 사각형이
                    // **윗줄 칸과 맞닿아** 큰 글씨에서 윗줄 획이 한 줄 배어 나온다.
                    // 아틀라스에 여백이 없으므로 사각형을 잉크에 딱 맞춰 잘라 낸다.
                    int minX = Cell, maxX = -1, minY = Cell, maxY = -1;
                    for (int y = 0; y < Cell; y++)      // y=0 이 칸의 아래쪽(베이스라인)
                    {
                        for (int x = 0; x < Cell; x++)
                        {
                            if (pixels[(rectY + y) * tex.width + rectX + x].a <= 8) continue;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }

                    // 공백은 잉크가 있어도 공백으로 둔다 — 시트의 0x20 칸에 잡티가 있다.
                    bool blank = code == 0x20 || maxX < 0;
                    float advance = blank ? Cell * 0.6f : maxX - minX + 2;

                    var metrics = new GlyphMetrics(
                        blank ? 0f : maxX - minX + 1, blank ? 0f : maxY - minY + 1,
                        blank ? 0f : minX, blank ? 0f : maxY + 1, advance);
                    var rect = new GlyphRect(rectX + (blank ? 0 : minX), rectY + (blank ? 0 : minY),
                                             blank ? 0 : maxX - minX + 1,
                                             blank ? 0 : maxY - minY + 1);

                    glyphs.Add(new Glyph(index, metrics, rect, 1f, 0));
                    chars.Add(new TMP_Character((uint)code, font, glyphs[glyphs.Count - 1]));
                    index++;
                }
            }

            // 원작 시트에는 **소문자가 없다**. 그대로 두면 `Lv.1` 의 v 만 한글 폰트로
            // 떨어져 한 낱말 안에서 서체가 갈린다. 오락실 폰트의 관례대로 소문자를
            // 대문자 글리프에 그대로 물린다.
            for (int code = 'a'; code <= 'z'; code++)
            {
                int upper = code - 'a' + 'A';
                chars.Add(new TMP_Character((uint)code, font, glyphs[upper - First]));
            }

            Set(font, "m_GlyphTable", glyphs);
            Set(font, "m_CharacterTable", chars);

            var shader = Shader.Find("TextMeshPro/Bitmap");
            if (shader == null)
            {
                Debug.LogError("[Font] TextMeshPro/Bitmap 셰이더를 못 찾았다");
                return;
            }
            var mat = new Material(shader) { name = font.name + " Material" };
            mat.SetTexture(ShaderUtilities.ID_MainTex, tex);

            // 이 폰트에는 ASCII 밖에 없다. 한글은 기존 폰트로 흘려보낸다.
            var kr = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanPath);
            if (kr != null) Set(font, "m_FallbackFontAssetTable", new List<TMP_FontAsset> { kr });
            else Debug.LogWarning($"[Font] 한글 대체 폰트 없음: {KoreanPath}");

            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutPath);
            if (existing == null)
            {
                font.material = mat;
                AssetDatabase.CreateAsset(font, OutPath);
                AssetDatabase.AddObjectToAsset(mat, font);
            }
            else
            {
                // ⚠ 지우고 새로 만들면 GUID 가 바뀌어 **이 폰트를 쓰던 프리팹의 참조가
                //   전부 끊긴다**(실제로 한 번 겪었다). 기존 에셋 위에 값만 덮어쓴다.
                Material old = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(OutPath))
                    if (o is Material m) old = m;

                if (old != null)
                {
                    EditorUtility.CopySerialized(mat, old);
                    UnityEngine.Object.DestroyImmediate(mat);
                    mat = old;
                }
                font.material = mat;
                EditorUtility.CopySerialized(font, existing);
                UnityEngine.Object.DestroyImmediate(font);
                font = existing;
                if (old == null) AssetDatabase.AddObjectToAsset(mat, font);
                EditorUtility.SetDirty(font);
            }

            font.ReadFontAssetDefinition();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Font] 원작 도트 폰트 생성 — 글자 {chars.Count}자 → {OutPath}");
        }
    }
}
