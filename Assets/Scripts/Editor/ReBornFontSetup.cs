// RE:BORN UI 리뉴얼(2026-10-23) — 언어별 본문 글꼴을 Pretendard · Pretendard JP · Noto Sans SC · TC 로.
// PD 「언어별로 통일이 되고 지금 시안 느낌 나는 폰트여야 돼」 → 4번 「그대로 진행해」.
using System.Collections.Generic;
using Game.Module.Common;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.Editor
{
    /// <summary>
    /// 글꼴 파일(`Assets/BaseResource/Fonts/ReBorn/`)에서 TMP 동적 SDF 글꼴을 만들고, 효과 프리셋 8개(이름 꼬리)를 같이 만들어
    /// 문자열 표의 언어별 글꼴(`StringTable._fonts`)에 건다. 여러 번 돌려도 같다(있는 것은 다시 만들지 않는다).
    /// 설정은 지금 쓰던 NotoSansKR SDF 와 같게 — 90pt · 여백 9 · SDFAA · 1024 · 여러 장 아틀라스 · 빌드 때 동적 글자 비움(규칙 09).
    /// </summary>
    public static class ReBornFontSetup
    {
        private const string Dir = "Assets/BaseResource/Fonts/ReBorn";
        private const string TablePath = "Assets/BundleResource/TableData/StringTable.asset";
        // 프리셋의 본보기 — 이름 꼬리(` - Outline` 등)가 같아야 LanguageModule 이 짝을 찾는다
        private const string PresetSource = "Assets/BaseResource/Fonts/NotoSansKR SDF - {0}.mat";
        private static readonly string[] Tails = { "GlowBlue", "GlowGold", "GlowPurple", "Label", "Outline", "Shadow", "Soft", "Title" };

        // 언어 → (본문 파일, 굵은 파일)
        private static readonly (Language lang, string body, string heavy)[] Map =
        {
            (Language.Korean, "Pretendard-SemiBold.otf", "Pretendard-Bold.otf"),
            (Language.English, "Pretendard-SemiBold.otf", "Pretendard-Bold.otf"),
            (Language.Japanese, "PretendardJP-SemiBold.otf", "PretendardJP-Bold.otf"),
            (Language.ChineseTraditional, "NotoSansTC-SemiBold.ttf", "NotoSansTC-Bold.ttf"),
            (Language.ChineseSimplified, "NotoSansSC-SemiBold.ttf", "NotoSansSC-Bold.ttf"),
        };

        [MenuItem("Tools/Game/RE:BORN 키트/언어별 글꼴 만들기 · 걸기")]
        public static void Apply()
        {
            var made = new Dictionary<string, TMP_FontAsset>();
            var presets = new Dictionary<string, Material[]>();
            foreach (var (_, body, heavy) in Map)
            {
                if (!made.ContainsKey(body)) { made[body] = Ensure(body); presets[body] = EnsurePresets(made[body]); }
                if (!made.ContainsKey(heavy)) made[heavy] = Ensure(heavy);
            }

            // 번역이 빈 줄은 한국어 원문으로 떨어진다 — 한글이 없는 글꼴(중국어)은 한국어 글꼴을 대체로 건다
            var korean = made["Pretendard-SemiBold.otf"];
            foreach (var kv in made)
            {
                if (kv.Value == korean || kv.Key.StartsWith("Pretendard")) continue;
                // 새로 만든 글꼴은 대체 목록이 비어(null) 있다
                if (kv.Value.fallbackFontAssetTable == null) kv.Value.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!kv.Value.fallbackFontAssetTable.Contains(korean)) kv.Value.fallbackFontAssetTable.Add(korean);
                EditorUtility.SetDirty(kv.Value);
            }

            var table = AssetDatabase.LoadAssetAtPath<StringTable>(TablePath);
            var so = new SerializedObject(table);
            var fonts = so.FindProperty("_fonts");
            fonts.ClearArray();
            for (int i = 0; i < Map.Length; i++)
            {
                var (lang, body, heavy) = Map[i];
                fonts.InsertArrayElementAtIndex(i);
                var e = fonts.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("_language").enumValueIndex = (int)lang;
                e.FindPropertyRelative("_gothic").objectReferenceValue = made[body];
                e.FindPropertyRelative("_heavy").objectReferenceValue = made[heavy];
                var p = e.FindPropertyRelative("_presets");
                p.ClearArray();
                for (int k = 0; k < presets[body].Length; k++)
                {
                    p.InsertArrayElementAtIndex(k);
                    p.GetArrayElementAtIndex(k).objectReferenceValue = presets[body][k];
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RE:BORN 글꼴] 글꼴 {made.Count}개 · 언어 {Map.Length}개 걸음");
        }

        private static TMP_FontAsset Ensure(string file)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(file) + " SDF";
            string path = $"{Dir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{Dir}/{file}");
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                                                    AtlasPopulationMode.Dynamic, true);
            fa.name = name;
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            var so = new SerializedObject(fa);
            var clear = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clear != null) clear.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return fa;
        }

        private static Material[] EnsurePresets(TMP_FontAsset fa)
        {
            var list = new Material[Tails.Length];
            for (int i = 0; i < Tails.Length; i++)
            {
                string path = $"{Dir}/{fa.name} - {Tails[i]}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    var src = AssetDatabase.LoadAssetAtPath<Material>(string.Format(PresetSource, Tails[i]));
                    m = src != null ? new Material(src) : new Material(fa.material);
                    m.SetTexture(ShaderUtilities.ID_MainTex, fa.atlasTexture);
                    AssetDatabase.CreateAsset(m, path);
                }
                list[i] = m;
            }
            return list;
        }
    }
}
