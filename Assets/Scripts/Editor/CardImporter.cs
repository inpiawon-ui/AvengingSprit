using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 특성 카드를 `BuffTable` 에 굽는다 (2026-09-09 확정 목록).
    ///
    /// 예전 정본 32종과 진화 11종을 걷어내고 새로 짠 목록이다.
    /// **카드 종류는 앞으로 더 늘어난다** — 늘릴 때는 아래 `Cards` 에 줄 하나만
    /// 더하고 이 메뉴를 다시 누르면 된다.
    ///
    /// ⚠ 카드 번호(`c001`…)는 **한 번 준 것을 바꾸지 않는다.** 아이콘 이름
    ///   (`buffcard_c001`)이 이 번호를 따르고, 표에 구운 값도 이 키로 찾는다.
    ///   새 카드는 늘 뒤 번호를 받는다.
    ///
    /// ⚠ 3택1 에서 빠지는 카드는 **5레벨을 다 찍은 것뿐이다**(`RunBuffs.Apply`).
    ///   예전에 있던 「서로 다른 카드 8종」 상한은 걷어냈다 — 화면에 표시가 없어
    ///   새 카드가 조용히 사라지는 것으로만 보였다.
    /// </summary>
    public static class CardImporter
    {
        private const string TablePath = "Assets/BundleResource/TableData/BuffTable.asset";

        private readonly struct Card
        {
            public readonly string Id, NameKr, Desc, Category;
            public readonly BuffKind Kind;
            public readonly CardRarity Rarity;
            public readonly int[] Levels;

            public Card(string id, string nameKr, BuffKind kind, CardRarity rarity,
                        string category, int[] levels, string desc)
            { Id = id; NameKr = nameKr; Kind = kind; Rarity = rarity;
              Category = category; Levels = levels; Desc = desc; }
        }

        // 아이콘은 카드 번호를 따라간다 — 카드 아틀라스의 `card_c001`~`card_c010`.
        // 지금 그 자리에는 **예전 32종의 그림**이 그대로 있어 뜻이 안 맞는다.
        // 새 10장을 그려 같은 이름으로 덮으면 코드는 그대로 두고 그림만 바뀐다.
        private static readonly Card[] Cards =
        {
            new("c001", "감전", BuffKind.Overcharge, CardRarity.Epic,
                "ATTACK", new[] { 25, 35, 45, 55, 65 },
                "명중하면 전기가 옆 적에게 튄다."),

            new("c002", "성장 가속", BuffKind.ExpGain, CardRarity.Rare,
                "UTILITY", new[] { 12, 20, 28, 36, 44 },
                "얻는 경험치가 늘어난다."),

            new("c003", "수호 방패", BuffKind.OrbitShield, CardRarity.Rare,
                "SURVIVAL", new[] { 3, 3, 4, 4, 5 },
                "방패가 내 주위를 돌며 막는다."),

            new("c004", "처형", BuffKind.Assassinate, CardRarity.Legendary,
                "ATTACK", new[] { 3, 5, 7, 9, 12 },
                "빈사인 적을 확률로 즉사시킨다."),

            new("c005", "번개 사슬", BuffKind.ChainLightning, CardRarity.Epic,
                "AREA", new[] { 1, 1, 2, 2, 3 },
                "전기가 더 여러 번 튄다."),

            new("c006", "흡혼", BuffKind.Regen, CardRarity.Common,
                "SURVIVAL", new[] { 3, 5, 7, 9, 11 },
                "적을 잡으면 체력을 돌려받는다."),

            new("c007", "찰나의 불사", BuffKind.GuardInvuln, CardRarity.Epic,
                "SURVIVAL", new[] { 30, 26, 22, 18, 14 },
                "맞는 순간 2초 무적이 된다."),

            new("c008", "추가 발사", BuffKind.MultiShot, CardRarity.Rare,
                "PROJECTILE", new[] { 1, 2, 3, 4, 5 },
                "쏠 때마다 탄이 더 나간다."),

            new("c009", "화염 각인", BuffKind.FlameImprint, CardRarity.Rare,
                "UTILITY", new[] { 8, 13, 18, 23, 28 },
                "공격에 화상을 붙인다."),

            new("c010", "궁지", BuffKind.LowHpPower, CardRarity.Rare,
                "ATTACK", new[] { 15, 25, 35, 45, 55 },
                "체력이 절반 아래면 세진다."),
        };

        private static readonly System.Collections.Generic.Dictionary<string, string> CategoryColor = new()
        {
            ["ATTACK"] = "#F0645A", ["PROJECTILE"] = "#F0B428", ["AREA"] = "#B478F0",
            ["SURVIVAL"] = "#5CC850", ["MOBILITY"] = "#50C8F0", ["UTILITY"] = "#F0F0F0",
        };

        [MenuItem("Tools/Game/특성 카드 임포트")]
        public static void Import()
        {
            var table = AssetDatabase.LoadAssetAtPath<BuffTable>(TablePath);
            if (table == null) { Debug.LogError("[카드] BuffTable 없음"); return; }

            var so = new SerializedObject(table);
            var entries = so.FindProperty("_entries");
            entries.ClearArray();   // 예전 목록을 통째로 갈아 끼운다

            for (int i = 0; i < Cards.Length; i++)
            {
                var c = Cards[i];
                entries.InsertArrayElementAtIndex(i);
                var e = entries.GetArrayElementAtIndex(i);

                e.FindPropertyRelative("_buffKey").stringValue = c.Id;
                e.FindPropertyRelative("_cardId").stringValue = c.Id.ToUpperInvariant();
                e.FindPropertyRelative("_nameKr").stringValue = c.NameKr;
                e.FindPropertyRelative("_description").stringValue = c.Desc;
                e.FindPropertyRelative("_kind").enumValueIndex = (int)c.Kind;
                e.FindPropertyRelative("_category").stringValue = c.Category;
                e.FindPropertyRelative("_rarity").enumValueIndex = (int)c.Rarity;
                e.FindPropertyRelative("_stackable").boolValue = true;
                e.FindPropertyRelative("_implemented").boolValue = true;
                e.FindPropertyRelative("_colorHex").stringValue =
                    CategoryColor.TryGetValue(c.Category, out var col) ? col : "#F0B428";

                // 챕터로 가르지 않는다. 해금은 아웃게임 몫인데 아직 없다.
                e.FindPropertyRelative("_fromChapter").intValue = 1;
                e.FindPropertyRelative("_scope").enumValueIndex = (int)BuffScope.Common;
                e.FindPropertyRelative("_canonId").stringValue = string.Empty;
                e.FindPropertyRelative("_canonEffect").stringValue = string.Empty;
                e.FindPropertyRelative("_pool").stringValue = c.Category;
                // 뽑기 무게는 등급에만 둔다 — 카드마다 또 주면 "왜 이건 안 나오지" 를
                // 아무도 설명 못 한다.
                e.FindPropertyRelative("_weight").floatValue = 1f;

                var lv = e.FindPropertyRelative("_levelValues");
                lv.ClearArray();
                for (int k = 0; k < c.Levels.Length; k++)
                {
                    lv.InsertArrayElementAtIndex(k);
                    lv.GetArrayElementAtIndex(k).intValue = c.Levels[k];
                }
                e.FindPropertyRelative("_value").intValue = c.Levels.Length > 0 ? c.Levels[0] : 0;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[카드] {Cards.Length}종을 구웠다.");
        }
    }
}
