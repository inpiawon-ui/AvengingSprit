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
    /// 언어별 폰트 꾸러미(`Assets/BundleResource/Fonts/FontSet_{코드}.asset`)에 건다. 표에는 이름만 적고,
    /// 폰트는 `font` 묶음에 하나씩 등록한다(2026-10-30 메모리). 여러 번 돌려도 같다(있는 것은 다시 만들지 않는다).
    /// 설정은 지금 쓰던 NotoSansKR SDF 와 같게 — 90pt · 여백 9 · SDFAA · 1024 · 여러 장 아틀라스 · 빌드 때 동적 글자 비움(규칙 09).
    /// </summary>
    public static class ReBornFontSetup
    {
        private const string Dir = "Assets/BaseResource/Fonts/ReBorn";
        private const string TablePath = "Assets/BundleResource/TableData/StringTable.asset";
        // 프리셋의 본보기 — 이름 꼬리(` - Outline` 등)가 같아야 LanguageModule 이 짝을 찾는다
        private const string PresetSource = "Assets/BaseResource/Fonts/NotoSansKR SDF - {0}.mat";
        private static readonly string[] Tails = { "GlowBlue", "GlowGold", "GlowPurple", "Label", "Outline", "Shadow", "Soft", "Title" };
        private const string NumberFont = "Galmuri11-Bold.ttf";
        private const string FontGroup = "font";
        private const string FontLabel = "label_font";

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
            // 숫자 · 고정 영문(재화 · 능력치 값 · 등급) — 언어와 상관없이 같은 도트 글꼴(묶음 지시). 언어팩이 바꾸지 않는다
            EnsurePresets(Ensure(NumberFont));

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

            // 언어마다 꾸러미 한 장. 표는 폰트를 물지 않고 이름만 쥔다(2026-10-30 메모리 — LanguageFontSet 참고)
            if (!AssetDatabase.IsValidFolder(LanguageFontSet.Folder))
                AssetDatabase.CreateFolder("Assets/BundleResource", "Fonts");
            var sets = new List<LanguageFontSet>();
            foreach (var (lang, body, heavy) in Map)
            {
                string path = $"{LanguageFontSet.Folder}/FontSet_{LanguageCodes.Code(lang)}.asset";
                var set = AssetDatabase.LoadAssetAtPath<LanguageFontSet>(path);
                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<LanguageFontSet>();
                    AssetDatabase.CreateAsset(set, path);
                }
                var so = new SerializedObject(set);
                var e = so.FindProperty("_font");
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
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(set);
                sets.Add(set);
            }

            var table = AssetDatabase.LoadAssetAtPath<StringTable>(TablePath);
            var tso = new SerializedObject(table);
            WriteNames(tso.FindProperty("_gothicFontNames"), Map, m => made[m.body].name);
            WriteNames(tso.FindProperty("_heavyFontNames"), Map, m => made[m.heavy].name);
            tso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            int entries = RegisterFontGroup(sets);
            Debug.Log($"[RE:BORN 글꼴] 글꼴 {made.Count}개 · 언어 꾸러미 {sets.Count}장 · font 묶음 항목 {entries}개");
        }

        private static void WriteNames(SerializedProperty list,
                                       (Language lang, string body, string heavy)[] map,
                                       System.Func<(Language lang, string body, string heavy), string> pick)
        {
            var names = new List<string>();
            foreach (var m in map)
            {
                string n = pick(m);
                if (!names.Contains(n)) names.Add(n);
            }
            list.ClearArray();
            for (int i = 0; i < names.Count; i++)
            {
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).stringValue = names[i];
            }
        }

        /// <summary>
        /// 폰트를 `font` 묶음에 **하나씩 따로**(Pack Separately) 등록한다.
        ///
        /// ⚠ 등록하지 않으면 그 폰트를 무는 묶음마다 폰트가 복사돼 들어간다 — 표 묶음과 화면 묶음에
        ///   같은 폰트가 두 벌씩 있었다(0.1.33 빌드 보고서: 압축 전 64.6MB + 58.1MB). 따로 묶어야
        ///   꾸러미 하나를 읽을 때 그 언어 폰트 묶음만 따라 올라온다.
        /// 본문 · 굵은 · 숫자 폰트와 효과 프리셋, 옛 화면이 아직 무는 NotoSans KR/JP 까지 넣는다.
        /// 픽셀 폰트(`OriginalPixel SDF`)는 TMP 설정(Resources)이 물어 앱 본체에 이미 들어가므로 넣지 않는다.
        /// </summary>
        private static int RegisterFontGroup(List<LanguageFontSet> sets)
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(FontGroup);
            if (group == null)
            {
                var template = settings.FindGroup("tabledata");
                group = settings.CreateGroup(FontGroup, false, false, true, template.Schemas);
            }
            var schema = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
            schema.BundleMode = UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            EditorUtility.SetDirty(schema);

            int n = 0;
            foreach (var set in sets)
                n += Register(settings, group, AssetDatabase.GetAssetPath(set), $"Fonts/{set.name}");
            foreach (var dir in new[] { "Assets/BaseResource/Fonts", Dir })
            {
                foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset t:Material", new[] { dir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/') != dir) continue;   // 하위 폴더는 따로 돈다
                    if (path.Contains("OriginalPixel")) continue;
                    n += Register(settings, group, path, "Fonts/" + System.IO.Path.GetFileNameWithoutExtension(path));
                }
            }
            settings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            return n;
        }

        private static int Register(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings,
                                    UnityEditor.AddressableAssets.Settings.AddressableAssetGroup group,
                                    string path, string address)
        {
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group, false, false);
            if (entry == null) return 0;
            entry.address = address;
            entry.SetLabel(FontLabel, true, true, false);
            return 1;
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
