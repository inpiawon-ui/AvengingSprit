using System.Text;
using Game.Character;
using Game.Module.Common;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 레벨업 카드 글자가 3언어 전 문장에서 칸 안에 들어가는지 잰다 (PD 2026-10-12 「텍스트는 삐져나가면 안 돼, 언어별 대응도」).
    ///
    /// ⚠ 프리팹 안 · 꺼진 창의 TMP 는 메시가 안 생겨 `isTextOverflowing` 이 늘 거짓이다.
    ///   그래서 자동 크기를 직접 돈다 — 최대 → 최소 글자 크기로 내려가며 `GetPreferredValues` 가 칸에 들어가는지 본다.
    /// 칸 자체가 카드 그림 속 상자 안쪽인지는 배치 표(`Projects/AVSR/Tools/popup_c_layout.py`)가 책임진다.
    /// </summary>
    public static class CardTextFitCheck
    {
        private const string PrefabPath = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private const string StringTablePath = "Assets/BundleResource/TableData/StringTable.asset";
        private const string BuffTablePath = "Assets/BundleResource/TableData/BuffTable.asset";
        private static readonly string[] ChipSamples = { "COMMON", "RARE", "EPIC", "LEGENDARY", "Lv.4 → Lv.5" };

        [MenuItem("Tools/Game/언어팩/레벨업 카드 글자 점검")]
        public static void Run()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            var report = new StringBuilder();
            int total = 0, over = 0;
            try
            {
                var name = Find(root, "BuffCard0Name");
                var desc = Find(root, "BuffCard0Desc");
                var chip = Find(root, "BuffCard0ChipText");
                var strings = AssetDatabase.LoadAssetAtPath<StringTable>(StringTablePath);
                var buffs = AssetDatabase.LoadAssetAtPath<BuffTable>(BuffTablePath);
                if (name == null || desc == null || chip == null || strings == null || buffs == null)
                {
                    Debug.LogError("[카드 글자 점검] 노드 · 표를 못 찾았다");
                    return;
                }
                foreach (Language lang in System.Enum.GetValues(typeof(Language)))
                {
                    var font = strings.FontFor(lang);
                    if (font != null)
                    {
                        name.font = font.Heavy;
                        desc.font = font.Gothic;
                        chip.font = font.Heavy;
                    }
                    foreach (var e in buffs.Entries)
                    {
                        if (e == null) continue;
                        Check(name, Text(strings, $"card.{e.BuffKey}.name", lang, e.NameKr), lang, ref total, ref over, report);
                        Check(desc, Text(strings, $"card.{e.BuffKey}.desc", lang, e.Description), lang, ref total, ref over, report);
                    }
                    foreach (var s in ChipSamples) Check(chip, s, lang, ref total, ref over, report);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            string head = $"[카드 글자 점검] {total}개 중 넘침 {over}개";
            if (over > 0) Debug.LogError(head + "\n" + report);
            else Debug.Log(head);
        }

        private static TMP_Text Find(GameObject root, string node)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                if (t.name == node) return t;
            return null;
        }

        private static string Text(StringTable table, string key, Language lang, string korean)
        {
            var entry = table.Find(key);
            return (entry != null ? entry.Raw(lang) : null) ?? korean;
        }

        private static void Check(TMP_Text t, string text, Language lang, ref int total, ref int over, StringBuilder report)
        {
            total++;
            var size = ((RectTransform)t.transform).rect.size;
            t.enableAutoSizing = false;
            for (float fs = t.fontSizeMax; fs >= t.fontSizeMin - 0.01f; fs -= 0.5f)
            {
                t.fontSize = fs;
                var p = t.enableWordWrapping ? t.GetPreferredValues(text, size.x, 0f) : t.GetPreferredValues(text, 0f, 0f);
                if (p.x <= size.x + 0.5f && p.y <= size.y + 0.5f) return;
            }
            over++;
            report.AppendLine($"{lang} {t.name} ({size.x}x{size.y}) | {text}");
        }
    }
}
