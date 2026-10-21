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
            public readonly int FromChapter;
            public readonly CardBodyRule Body;
            public readonly string[] Requires;
            public readonly bool Live;

            public Card(string id, string nameKr, BuffKind kind, CardRarity rarity,
                        string category, int[] levels, string desc,
                        int ch = 1, CardBodyRule body = CardBodyRule.Any, string[] needs = null, bool live = true)
            { Id = id; NameKr = nameKr; Kind = kind; Rarity = rarity;
              Category = category; Levels = levels; Desc = desc;
              FromChapter = ch; Body = body; Requires = needs ?? System.Array.Empty<string>(); Live = live; }
        }

        // 1단계 레벨업 카드(PD 2026-10-21, `_exchange/plan_content_expand/step1_cards.html`) — 13장 → 36장.
        //   풀리는 챕터: 1챕터 15장 → 2~6챕터 4장씩. 몸 조건 · 앞 카드는 `BattleDirector.CardRules`.
        private static readonly CardBodyRule R = CardBodyRule.Ranged;
        private static string[] Needs(params string[] keys) => keys;

        // 아이콘은 카드 번호를 따라간다 — 카드 아틀라스의 `card_c001`~`card_c010`.
        // 지금 그 자리에는 **예전 32종의 그림**이 그대로 있어 뜻이 안 맞는다.
        // 새 10장을 그려 같은 이름으로 덮으면 코드는 그대로 두고 그림만 바뀐다.
        private static readonly Card[] Cards =
        {
            new("c001", "감전", BuffKind.Overcharge, CardRarity.Epic,
                "ATTACK", new[] { 25, 35, 45, 55, 65 },
                "명중하면 전기가 옆 적에게 튄다."),

            // ⚠ 일반 판은 「방 하나 = 레벨 하나」라 경험치가 아무 일도 안 한다 — 경험치 레벨업(3단계 30방)이
            //   들어올 때까지 3택1 에서 뺀다(고르면 아무 일도 없는 카드가 되지 않게).
            new("c002", "성장 가속", BuffKind.ExpGain, CardRarity.Rare,
                "UTILITY", new[] { 12, 20, 28, 36, 44 },
                "얻는 경험치가 늘어난다.", live: false),

            new("c003", "수호 방패", BuffKind.OrbitShield, CardRarity.Rare,
                "SURVIVAL", new[] { 3, 3, 4, 4, 5 },
                "방패가 내 주위를 돌며 막는다."),

            new("c004", "처형", BuffKind.Assassinate, CardRarity.Legendary,
                "ATTACK", new[] { 3, 5, 7, 9, 12 },
                "빈사인 적을 확률로 즉사시킨다.", ch: 6),

            new("c005", "번개 사슬", BuffKind.ChainLightning, CardRarity.Epic,
                "AREA", new[] { 1, 1, 2, 2, 3 },
                "전기가 더 여러 번 튄다.", ch: 2, needs: Needs("c001")),

            new("c006", "흡혼", BuffKind.Regen, CardRarity.Common,
                "SURVIVAL", new[] { 3, 5, 7, 9, 11 },
                "적을 잡으면 체력을 돌려받는다."),

            new("c007", "찰나의 불사", BuffKind.GuardInvuln, CardRarity.Epic,
                "SURVIVAL", new[] { 30, 26, 22, 18, 14 },
                "맞는 순간 2초 무적이 된다."),

            new("c008", "추가 발사", BuffKind.MultiShot, CardRarity.Rare,
                "PROJECTILE", new[] { 1, 2, 3, 4, 5 },
                "쏠 때마다 탄이 더 나간다.", body: R),

            // ⚠ 8~28% 였다. 「공격에 화상을 붙인다」고 적어 놓고 스무 대에 한두 번만
            //   붙으니 카드를 먹은 줄도 몰랐다(2026-09-10 「화상 효과가 안 나온다」).
            //   레벨 1 에서도 세 대에 한 번은 붙어야 「붙는 카드」로 읽힌다.
            new("c009", "화염 각인", BuffKind.FlameImprint, CardRarity.Rare,
                "UTILITY", new[] { 30, 40, 50, 60, 70 },
                "공격에 화상을 붙인다."),

            new("c010", "궁지", BuffKind.LowHpPower, CardRarity.Rare,
                "ATTACK", new[] { 15, 25, 35, 45, 55 },
                "체력이 절반 아래면 세진다."),

            // 2026-10-12 — 공격력 · 공격 속도를 바로 올리는 카드가 한 장도 없어 「레벨업 안 해도 깬다」(PD).
            // 적 체력을 올리면서, 카드를 고르면 화력이 확실히 붙게 한 장에 약 +10~15% 로 맞췄다.
            new("c011", "공격 강화", BuffKind.Attack, CardRarity.Common,
                "ATTACK", new[] { 12, 18, 24, 30, 36 },
                "공격 피해가 늘어난다."),

            new("c012", "속사", BuffKind.AttackSpeed, CardRarity.Rare,
                "PROJECTILE", new[] { 10, 15, 20, 25, 30 },
                "더 빠르게 쏜다."),

            // 즉발 카드 — 고를 때마다 한 번. 다섯 번 고르면 3택1 에서 빠진다(다른 카드와 같은 규칙)
            new("c013", "필살기 초기화", BuffKind.UltReset, CardRarity.Rare,
                "UTILITY", new[] { 1, 1, 1, 1, 1 },
                "지금 몸의 필살기를 다시 쓸 수 있다."),

            // ── 1단계 추가 23장 (PD 2026-10-21) ────────────────────────────
            // 탄 모양 — 원거리 몸만
            new("c014", "관통탄", BuffKind.Pierce, CardRarity.Rare,
                "PROJECTILE", new[] { 1, 1, 2, 2, 3 },
                "탄이 적을 뚫고 지나간다.", body: R),
            new("c015", "벽 반사", BuffKind.Ricochet, CardRarity.Rare,
                "PROJECTILE", new[] { 50, 60, 65, 75, 80 },
                "탄이 벽과 엄폐물에서 튕겨 다시 날아간다.", ch: 3, body: R),
            new("c016", "유도탄", BuffKind.Homing, CardRarity.Common,
                "PROJECTILE", new[] { 12, 20, 28, 36, 44 },
                "탄이 가까운 적 쪽으로 휘어 날아간다.", body: R),

            // 원소 · 상태이상
            new("c017", "냉기 각인", BuffKind.FrostImprint, CardRarity.Rare,
                "UTILITY", new[] { 25, 35, 45, 55, 65 },
                "공격에 빙결을 붙인다. 걸린 적은 느려진다.", ch: 2),
            new("c018", "저주 각인", BuffKind.CurseImprint, CardRarity.Rare,
                "UTILITY", new[] { 20, 28, 36, 44, 52 },
                "공격에 저주를 붙인다. 걸린 적은 더 아프게 맞는다.", ch: 3),
            new("c019", "화상 번짐", BuffKind.BurnSpread, CardRarity.Epic,
                "AREA", new[] { 1 },
                "화상이 3단계가 된 적 둘레로 화상이 옮는다.", ch: 3, needs: Needs("c009")),
            new("c020", "상태이상 번짐", BuffKind.StatusChain, CardRarity.Epic,
                "AREA", new[] { 18, 25, 32, 39, 46 },
                "상태이상을 걸 때 옆 적에게도 옮긴다.", ch: 5, needs: Needs("c009", "c017", "c018", "c021")),
            new("c021", "둔화탄", BuffKind.Slow, CardRarity.Common,
                "UTILITY", new[] { 20, 28, 36, 44, 52 },
                "맞은 적이 잠시 느려진다."),

            // 표적 · 마무리
            new("c022", "갑옷 분쇄", BuffKind.ArmorBreak, CardRarity.Epic,
                "ATTACK", new[] { 5, 9, 13, 17, 21 },
                "때릴수록 그 적이 받는 피해가 쌓여 늘어난다.", ch: 2),
            new("c023", "연속 압박", BuffKind.SustainStack, CardRarity.Common,
                "ATTACK", new[] { 10, 16, 22, 28, 35 },
                "같은 적을 연달아 칠수록 세진다.", ch: 4),
            new("c024", "마무리 본능", BuffKind.Execute, CardRarity.Rare,
                "ATTACK", new[] { 10, 18, 26, 34, 42 },
                "체력이 낮은 적에게 더 아프다.", ch: 2),
            new("c025", "보스 사냥", BuffKind.BossFocus, CardRarity.Rare,
                "ATTACK", new[] { 18, 24, 30, 36, 42 },
                "보스에게 주는 피해가 늘어난다.", ch: 3),

            // 폭발 · 범위 · 장판
            new("c026", "폭발 메아리", BuffKind.ExplosiveEcho, CardRarity.Epic,
                "AREA", new[] { 20, 28, 36, 44, 52 },
                "내 탄이 터지면 같은 자리에 작은 폭발이 한 번 더.", ch: 5, body: CardBodyRule.Blast),
            new("c027", "범위 확대", BuffKind.AoeRadius, CardRarity.Rare,
                "AREA", new[] { 8, 14, 20, 26, 32 },
                "스킬 · 필살기 · 폭발 범위가 넓어진다.", ch: 4),
            new("c028", "장판 지속", BuffKind.FieldDuration, CardRarity.Common,
                "AREA", new[] { 12, 20, 28, 36, 44 },
                "내가 깐 장판이 더 오래 남는다.", ch: 6),

            // 소환 · 복제
            new("c029", "유령 포대", BuffKind.GhostTurret, CardRarity.Epic,
                "SPECIAL", new[] { 22, 27, 32, 37, 42 },
                "평타 8번마다 유령 포대가 잠시 선다.", ch: 5),
            new("c030", "영혼 복제", BuffKind.SpectralEcho, CardRarity.Legendary,
                "SPECIAL", new[] { 35, 40, 45, 50, 55 },
                "평타 8번째마다 같은 공격이 한 번 더 나간다.", ch: 6),

            // 생존 · 움직임
            new("c031", "유령 갑주", BuffKind.DamageReduction, CardRarity.Common,
                "SURVIVAL", new[] { 5, 8, 11, 14, 17 },
                "받는 피해가 줄어든다."),
            new("c032", "흡혈", BuffKind.Lifesteal, CardRarity.Rare,
                "SURVIVAL", new[] { 10, 15, 20, 25, 30 },
                "공격하면 확률로 준 피해의 절반만큼 회복한다.", ch: 4),
            new("c033", "위기 방벽", BuffKind.CrisisBarrier, CardRarity.Rare,
                "SURVIVAL", new[] { 12, 18, 24, 30, 36 },
                "체력이 위험해지면 방마다 한 번 방벽이 선다.", ch: 5),
            new("c034", "가속", BuffKind.MoveSpeed, CardRarity.Common,
                "MOBILITY", new[] { 5, 9, 13, 17, 21 },
                "이동 속도가 빨라진다."),
            new("c035", "사거리", BuffKind.Range, CardRarity.Common,
                "MOBILITY", new[] { 10, 15, 20, 25, 30 },
                "근접 공격이 닿는 거리가 늘어난다.", ch: 4, body: CardBodyRule.Melee),

            // 스킬(두 버튼)
            new("c036", "스킬 쿨 감소", BuffKind.ActiveSkillCharge, CardRarity.Rare,
                "UTILITY", new[] { 8, 14, 20, 26, 32 },
                "액티브 스킬 쿨타임이 줄어든다.", ch: 6),
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
                e.FindPropertyRelative("_stackable").boolValue = c.Levels.Length > 1;   // 1회 카드(화상 번짐)는 한 번 고르면 끝
                e.FindPropertyRelative("_implemented").boolValue = c.Live;
                e.FindPropertyRelative("_body").enumValueIndex = (int)c.Body;
                var needs = e.FindPropertyRelative("_requiresAny");
                needs.ClearArray();
                for (int k = 0; k < c.Requires.Length; k++)
                {
                    needs.InsertArrayElementAtIndex(k);
                    needs.GetArrayElementAtIndex(k).stringValue = c.Requires[k];
                }
                e.FindPropertyRelative("_colorHex").stringValue =
                    CategoryColor.TryGetValue(c.Category, out var col) ? col : "#F0B428";

                // 챕터마다 3~5장씩 풀린다(PD 2026-10-21) — 뒤 챕터로 갈수록 처음 보는 카드가 나오게
                e.FindPropertyRelative("_fromChapter").intValue = Mathf.Max(1, c.FromChapter);
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
