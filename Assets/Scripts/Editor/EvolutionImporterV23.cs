using System;
using System.Collections.Generic;
using System.IO;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 정본 v2.2 EVOLUTION_RUNTIME(11) + RECIPE_RUNTIME 을 `EvolutionTable` 로 굽는다.
    ///
    /// 두 파일은 `data` 배열이 아니라 각자 다른 키를 쓴다(`evolutions` / `recipes`).
    /// 값도 중첩이 없는 평평한 객체라 `FlatJson` 으로는 못 읽는다 —
    /// 최상위 키가 다르기 때문이다. 그래서 여기서만 쓰는 작은 읽기를 따로 둔다.
    /// </summary>
    public static class EvolutionImporterV23
    {
        private const string Dir = "Projects/AVSR/Canon/v23";
        private const string TablePath = "Assets/BundleResource/TableData/EvolutionTable.asset";

        /// <summary>정본 `AttackType` → 우리 거동. 연출 이름이 아니라 **맞는 방식**으로 묶는다.</summary>
        private static EvolutionKind KindOf(string attackType) => attackType switch
        {
            "CROSSING_SCYTHE" => EvolutionKind.PiercingLane,
            "PIERCING_FROST_LANE" => EvolutionKind.PiercingLane,
            "PIERCING_SWEEP" => EvolutionKind.PiercingLane,

            "EDGE_CONE_BURST" => EvolutionKind.ConeBurst,
            "SEQUENTIAL_BOMBARDMENT" => EvolutionKind.ConeBurst,

            "ORBIT_RELEASE" => EvolutionKind.Orbit,
            "SCREEN_SALVO_BURST" => EvolutionKind.Orbit,
            "RICOCHET_BALL" => EvolutionKind.Orbit,

            "MOVING_CONTROL_STORM" => EvolutionKind.Field,
            "SIGIL_PILLAR" => EvolutionKind.Field,

            "TEMP_SUSTAIN_SUPPORT" => EvolutionKind.Turret,
            _ => EvolutionKind.PiercingLane,
        };

        [MenuItem("Tools/Game/Canon v2.2 임포트 (진화 11종)")]
        public static void Import()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var evoPath = Path.Combine(root, Dir, "EVOLUTION_RUNTIME.json");
            var rcpPath = Path.Combine(root, Dir, "RECIPE_RUNTIME.json");
            if (!File.Exists(evoPath) || !File.Exists(rcpPath))
            { Debug.LogError("[EvoV23] 정본 파일 없음"); return; }

            var evos = Objects(File.ReadAllText(evoPath), "evolutions");
            var rcps = Objects(File.ReadAllText(rcpPath), "recipes");
            if (evos.Count == 0) { Debug.LogError("[EvoV23] 진화 파싱 실패"); return; }

            int maxSlots = FirstInt(File.ReadAllText(rcpPath), "maxEvolutionSlots", 3);

            // 레시피를 진화 ID 로 찾아 쓰기 좋게 뒤집는다
            var byEvo = new Dictionary<string, Dictionary<string, string>>();
            for (int i = 0; i < rcps.Count; i++)
            {
                var id = S(rcps[i], "evolutionId");
                if (!string.IsNullOrEmpty(id)) byEvo[id] = rcps[i];
            }

            var table = AssetDatabase.LoadAssetAtPath<EvolutionTable>(TablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<EvolutionTable>();
                AssetDatabase.CreateAsset(table, TablePath);
            }

            var so = new SerializedObject(table);
            so.FindProperty("_maxSlots").intValue = maxSlots;
            var arr = so.FindProperty("_entries");
            arr.ClearArray();

            int starters = 0;
            for (int i = 0; i < evos.Count; i++)
            {
                var e = evos[i];
                var id = S(e, "EvolutionID");
                byEvo.TryGetValue(id, out var r);

                arr.InsertArrayElementAtIndex(i);
                var x = arr.GetArrayElementAtIndex(i);

                x.FindPropertyRelative("_evolutionId").stringValue = id;
                x.FindPropertyRelative("_nameKr").stringValue = S(e, "NameKR");
                x.FindPropertyRelative("_nameEn").stringValue = S(e, "NameEN");
                x.FindPropertyRelative("_motifHost").stringValue = S(e, "MotifHostID");

                // 카드 키는 소문자다 (`CardImporterV23` 와 같은 규칙)
                x.FindPropertyRelative("_cardA").stringValue = (r != null ? S(r, "cardA") : "").ToLowerInvariant();
                x.FindPropertyRelative("_cardB").stringValue = (r != null ? S(r, "cardB") : "").ToLowerInvariant();

                // 해금은 아웃게임(마스터리 Lv7)이 가른다. 지금 열려 있는 것은 스타터 둘뿐이다.
                bool starter = r != null && S(r, "unlock") == "STARTER_RECIPE";
                if (starter) starters++;
                x.FindPropertyRelative("_starter").boolValue = starter;

                var attack = S(e, "AttackType");
                x.FindPropertyRelative("_canonAttackType").stringValue = attack;
                x.FindPropertyRelative("_kind").enumValueIndex = (int)KindOf(attack);

                x.FindPropertyRelative("_cooldown").floatValue = F(e, "CooldownSec", 12f);
                x.FindPropertyRelative("_damageCoef").floatValue = F(e, "DamageCoefficient", 1f);
                x.FindPropertyRelative("_hitCount").intValue = I(e, "HitCount", 1);
                x.FindPropertyRelative("_projectileCount").intValue = I(e, "ProjectileCount", 1);
                x.FindPropertyRelative("_projectileSpeed").floatValue = F(e, "ProjectileSpeed", 14f);
                x.FindPropertyRelative("_rangeMeters").floatValue = F(e, "Range", 9f);
                x.FindPropertyRelative("_radiusMeters").floatValue = F(e, "Radius", 0f);
                x.FindPropertyRelative("_durationSeconds").floatValue = F(e, "DurationSec", 0f);
                x.FindPropertyRelative("_pierce").intValue = I(e, "Pierce", 0);
                x.FindPropertyRelative("_bounce").intValue = I(e, "Bounce", 0);
                x.FindPropertyRelative("_homing").floatValue = F(e, "HomingStrength", 0f);
                x.FindPropertyRelative("_statusType").stringValue = S(e, "StatusType");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EvoV23] 진화 {evos.Count}종 · 지금 만들 수 있는 것(스타터) {starters}종 · 슬롯 {maxSlots}칸");
        }

        // ── 작은 읽기 ────────────────────────────────────────────
        // `FlatJson` 은 `{"data":[…]}` 만 읽는다. 여기 둘은 최상위 키가 달라
        // 그것만 바꿔 끼우면 되므로 배열 이름을 받아 같은 방식으로 훑는다.

        private static List<Dictionary<string, string>> Objects(string json, string arrayKey)
        {
            var list = new List<Dictionary<string, string>>();
            int at = json.IndexOf($"\"{arrayKey}\"", StringComparison.Ordinal);
            if (at < 0) return list;
            at = json.IndexOf('[', at);
            if (at < 0) return list;

            int depth = 0;
            int start = -1;
            for (int i = at; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '{') { if (depth++ == 0) start = i; }
                else if (c == '}') { if (--depth == 0 && start >= 0) list.Add(Fields(json, start, i)); }
                else if (c == ']' && depth == 0) break;
            }
            return list;
        }

        private static Dictionary<string, string> Fields(string json, int from, int to)
        {
            var map = new Dictionary<string, string>();
            int i = from;
            while (i < to)
            {
                int k0 = json.IndexOf('"', i);
                if (k0 < 0 || k0 >= to) break;
                int k1 = json.IndexOf('"', k0 + 1);
                if (k1 < 0 || k1 >= to) break;
                var key = json.Substring(k0 + 1, k1 - k0 - 1);

                int colon = json.IndexOf(':', k1);
                if (colon < 0 || colon >= to) break;

                int v = colon + 1;
                while (v < to && char.IsWhiteSpace(json[v])) v++;
                if (v >= to) break;

                string val;
                if (json[v] == '"')
                {
                    int v1 = json.IndexOf('"', v + 1);
                    if (v1 < 0 || v1 > to) break;
                    val = json.Substring(v + 1, v1 - v - 1);
                    i = v1 + 1;
                }
                else
                {
                    int v1 = v;
                    while (v1 < to && json[v1] != ',' && json[v1] != '}') v1++;
                    val = json.Substring(v, v1 - v).Trim();
                    i = v1;
                }
                map[key] = val;
            }
            return map;
        }

        private static int FirstInt(string json, string key, int fallback)
        {
            int at = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
            if (at < 0) return fallback;
            int colon = json.IndexOf(':', at);
            if (colon < 0) return fallback;
            int v = colon + 1;
            while (v < json.Length && char.IsWhiteSpace(json[v])) v++;
            int e = v;
            while (e < json.Length && (char.IsDigit(json[e]) || json[e] == '-')) e++;
            return int.TryParse(json.Substring(v, e - v), out var n) ? n : fallback;
        }

        private static string S(Dictionary<string, string> m, string k)
            => m != null && m.TryGetValue(k, out var v) ? v : string.Empty;

        private static int I(Dictionary<string, string> m, string k, int d)
            => m != null && m.TryGetValue(k, out var v) && float.TryParse(v,
                   System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out var f) ? Mathf.RoundToInt(f) : d;

        private static float F(Dictionary<string, string> m, string k, float d)
            => m != null && m.TryGetValue(k, out var v) && float.TryParse(v,
                   System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : d;
    }
}
