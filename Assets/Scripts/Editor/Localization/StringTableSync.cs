using Game.Character;
using Game.Module.Common;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 표에 든 **한국어 원문**을 문자열 표로 모은다 (2026-09-15, 언어팩).
    ///
    /// 원문은 여전히 각 표의 `*Kr` 칸이 쥔다 — 정본 임포터가 그 칸을 덮어쓰기 때문이다.
    /// 문자열 표에는 **키와 한국어 사본**이 들어가고, 번역(ja·en)은 거기에만 적는다.
    /// 그래서 임포터를 다시 돌려도 번역은 안 날아가고, 원문이 바뀌면 이 도구를 한 번 더 돌리면 된다.
    ///
    /// ⚠ 키 규칙은 표 클래스의 `Display*` 속성과 **글자 하나까지 같아야 한다.**
    ///   한쪽만 바꾸면 번역이 있는데도 원문이 뜬다.
    /// </summary>
    public static class StringTableSync
    {
        private const string Dir = "Assets/BundleResource/TableData/";

        [MenuItem("Tools/Game/언어팩/표 글자 → 문자열 표 동기화")]
        public static void Sync()
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(Dir + "StringTable.asset");
            if (table == null)
            {
                Debug.LogError("[언어팩] StringTable.asset 이 없다: " + Dir);
                return;
            }

            int n = 0;

            var hosts = Load<HostTable>("HostTable");
            if (hosts != null)
                foreach (var e in hosts.Entries)
                    if (e != null) n += Put(table, $"host.{e.HostKey}.name", e.NameKr);

            var actives = Load<ActiveSkillTable>("ActiveSkillTable");
            if (actives != null)
                foreach (var e in actives.Entries)
                {
                    if (e == null) continue;
                    n += Put(table, $"askill.{e.ActiveSkillKey}.name", e.NameKr);
                    n += Put(table, $"askill.{e.ActiveSkillKey}.desc", e.Description);
                }

            var passives = Load<PassiveSkillTable>("PassiveSkillTable");
            if (passives != null)
                foreach (var e in passives.Entries)
                {
                    if (e == null) continue;
                    n += Put(table, $"pskill.{e.PassiveSkillKey}.name", e.NameKr);
                    n += Put(table, $"pskill.{e.PassiveSkillKey}.desc", e.Description);
                }

            var cards = Load<BuffTable>("BuffTable");
            if (cards != null)
                foreach (var e in cards.Entries)
                {
                    if (e == null) continue;
                    n += Put(table, $"card.{e.BuffKey}.name", e.NameKr);
                    n += Put(table, $"card.{e.BuffKey}.desc", e.Description);
                }

            var bosses = Load<BossTable>("BossTable");
            if (bosses != null)
                foreach (var e in bosses.Entries)
                {
                    if (e == null) continue;
                    n += Put(table, $"boss.{e.BossKey}.name", e.NameKr);
                    if (e.Moves == null) continue;
                    foreach (var m in e.Moves)
                        if (m != null) n += Put(table, $"bossmove.{m.LabelKey}.name", m.NameKr);
                }

            var events = Load<EventTable>("EventTable");
            if (events != null)
                foreach (var e in events.Entries)
                {
                    if (e == null) continue;
                    n += Put(table, $"event.{e.EventId}.title", e.TitleKr);
                    n += Put(table, $"event.{e.EventId}.body", e.BodyKr);
                    n += Put(table, $"event.{e.EventId}.accept", e.AcceptKr);
                    n += Put(table, $"event.{e.EventId}.decline", e.DeclineKr);
                }

            // 무대 이름은 GameConfig 안의 배열이라 공개 목록이 없다 — 직렬화 칸을 직접 읽는다
            // ⚠ `GameConfig` 는 `Game.Character` 와 `Game.Module.Common` 두 곳에 있다 — 표 쪽을 짚는다
            var config = Load<Game.Character.GameConfig>("GameConfig");
            if (config != null)
            {
                var stages = new SerializedObject(config).FindProperty("_stageNames");
                for (int i = 0; stages != null && i < stages.arraySize; i++)
                {
                    var el = stages.GetArrayElementAtIndex(i);
                    n += Put(table,
                             $"stage.{el.FindPropertyRelative("Chapter").intValue}.{el.FindPropertyRelative("FromRoom").intValue}.name",
                             el.FindPropertyRelative("NameKr").stringValue);
                }
            }

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[언어팩] 표 글자 {n}줄을 문자열 표에 모았다 (총 {table.Entries.Count}줄)");
        }

        private static T Load<T>(string name) where T : Object
            => AssetDatabase.LoadAssetAtPath<T>(Dir + name + ".asset");

        /// <summary>원문이 비었거나 키 조각이 빠진 줄(`host..name`)은 넣지 않는다.</summary>
        private static int Put(StringTable table, string key, string korean)
        {
            if (string.IsNullOrEmpty(korean) || key.Contains("..") || key.EndsWith(".")) return 0;
            table.Upsert(key, korean);
            return 1;
        }
    }
}
