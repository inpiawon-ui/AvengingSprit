using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// <see cref="HostSkillTable"/> 의 값을 `HostTable` · `ActiveSkillTable` 에 굽는다.
    ///
    /// 두 표에 나눠 들어간다 — **쿨은 몸에, 성장 두 축은 스킬에** 붙는다.
    ///   `HostEntry._activeSkillCooldown`      호스트별 8 · 14 · 20 · 28
    ///   `ActiveSkillEntry._scaling`           Lv1~4 축 · Lv6~10 축
    ///
    /// 몸과 스킬은 `HostEntry._activeSkillKey` 로 이어진다. 스킬을 두 몸이 나눠 쓰면
    /// 나중에 굽는 쪽이 이긴다 — 지금 23명이 1:1 이라 그럴 일이 없지만,
    /// 겹치면 경고를 남긴다.
    /// </summary>
    public static class HostSkillImporter
    {
        private const string HostPath = "Assets/BundleResource/TableData/HostTable.asset";
        private const string SkillPath = "Assets/BundleResource/TableData/ActiveSkillTable.asset";

        [MenuItem("Tools/Game/호스트 스킬 임포트 (쿨 · 성장 두 축)")]
        public static void Import()
        {
            var hostTable = AssetDatabase.LoadAssetAtPath<HostTable>(HostPath);
            var skillTable = AssetDatabase.LoadAssetAtPath<ActiveSkillTable>(SkillPath);
            if (hostTable == null || skillTable == null)
            {
                Debug.LogError("[스킬] HostTable 또는 ActiveSkillTable 을 못 찾았다.");
                return;
            }

            var hostSo = new SerializedObject(hostTable);
            var skillSo = new SerializedObject(skillTable);
            var hosts = hostSo.FindProperty("_entries");
            var skills = skillSo.FindProperty("_entries");

            int cool = 0, scaled = 0, missing = 0;
            var seenSkill = new System.Collections.Generic.Dictionary<string, string>();

            for (int i = 0; i < hosts.arraySize; i++)
            {
                var h = hosts.GetArrayElementAtIndex(i);
                string key = h.FindPropertyRelative("_hostKey").stringValue;
                var row = HostSkillTable.Get(key);
                if (row == null) continue;   // 유령 등 표에 없는 것

                h.FindPropertyRelative("_activeSkillCooldown").floatValue = row.Cooldown;
                cool++;

                string skillKey = h.FindPropertyRelative("_activeSkillKey").stringValue;
                if (string.IsNullOrEmpty(skillKey))
                {
                    Debug.LogWarning($"[스킬] {key} 에 액티브 스킬 키가 없다 — 성장 축을 못 붙인다.");
                    missing++;
                    continue;
                }

                if (seenSkill.TryGetValue(skillKey, out var owner))
                    Debug.LogWarning($"[스킬] {skillKey} 를 {owner} 와 {key} 가 나눠 쓴다 — "
                                     + "뒤에 굽는 쪽이 이긴다.");
                seenSkill[skillKey] = key;

                var target = FindSkill(skills, skillKey);
                if (target == null)
                {
                    Debug.LogWarning($"[스킬] {key} 의 스킬 {skillKey} 가 표에 없다.");
                    missing++;
                    continue;
                }

                var sc = target.FindPropertyRelative("_scaling");
                sc.FindPropertyRelative("_baseValue").floatValue = row.Base;
                sc.FindPropertyRelative("_baseValueMax").floatValue = row.BaseMax;
                sc.FindPropertyRelative("_specValue").floatValue = row.Spec;
                sc.FindPropertyRelative("_specValueMax").floatValue = row.SpecMax;
                scaled++;
            }

            hostSo.ApplyModifiedPropertiesWithoutUndo();
            skillSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hostTable);
            EditorUtility.SetDirty(skillTable);
            AssetDatabase.SaveAssets();

            Debug.Log($"[스킬] 쿨 {cool}명 · 성장 축 {scaled}개"
                      + (missing > 0 ? $" · 못 붙인 것 {missing}" : string.Empty));
        }

        private static SerializedProperty FindSkill(SerializedProperty skills, string key)
        {
            for (int i = 0; i < skills.arraySize; i++)
            {
                var e = skills.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("_activeSkillKey").stringValue == key) return e;
            }
            return null;
        }
    }
}
