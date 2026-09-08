using System;
using System.Collections.Generic;
using System.IO;
using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 정본 v2.3 카드 32종을 `BuffTable` 에 굽는다. 기존 버프 35종을 **대체**한다.
    ///
    /// 카드는 버프와 두 가지가 다르다.
    ///   · 같은 카드를 다시 고르면 **레벨이 오른다**(최대 5). 중첩 합산이 아니다.
    ///   · **등급**이 있고 등급마다 뽑힐 확률이 다르다 (60 / 28 / 9.5 / 2.5).
    /// 그래서 `BuffEntry` 에 등급·레벨표를 더해 두고 그 위에 얹는다 —
    /// 효과를 쌓는 부분(`RunBuffs`)은 이미 돌아가므로 그대로 쓴다.
    ///
    /// 아직 만들 시스템이 없는 카드는 `_implemented = false` 로 굽는다.
    /// 그러면 3택1 풀에서 빠진다 — 골라도 아무 일 없는 카드가 섞이면
    /// 선택 자체가 거짓이 된다.
    /// </summary>
    public static class CardImporterV23
    {
        private const string Dir = "Projects/AVSR/Canon/v23";
        private const string TablePath = "Assets/BundleResource/TableData/BuffTable.asset";

        /// <summary>정본 카드 → 우리 효과. `kind` 가 null 이면 아직 시스템이 없다.</summary>
        private readonly struct Map
        {
            public readonly BuffKind Kind;
            public readonly bool Ready;
            public readonly bool Stackable;
            public Map(BuffKind kind, bool ready = true, bool stackable = true)
            { Kind = kind; Ready = ready; Stackable = stackable; }
        }

        private static readonly Dictionary<string, Map> Effect = new()
        {
            // 이미 돌아가는 것에 바로 붙는 카드
            ["C001"] = new(BuffKind.Attack),
            ["C006"] = new(BuffKind.ShotSpeed),
            ["C007"] = new(BuffKind.MultiShot),
            ["C008"] = new(BuffKind.Pierce),
            // ⚠ C010 은 `Ricochet` 이 아니다. 그 종류는 **튕기는 횟수**를 받는데
            //   이 카드의 레벨값은 35/45/55/65/75 — 퍼센트다. 그대로 넣으면
            //   탄이 벽에서 35번 튕겨 방을 영원히 돌아다닌다.
            //   정본 문구는 "한 번 반사하는 탄체" 이고 숫자는 **튕긴 뒤의 피해 %** 다.
            ["C010"] = new(BuffKind.ReturnDamage),
            ["C011"] = new(BuffKind.AoeRadius),
            ["C012"] = new(BuffKind.FieldDuration),
            ["C016"] = new(BuffKind.DamageReduction),
            ["C019"] = new(BuffKind.Lifesteal),
            ["C020"] = new(BuffKind.HostMaxHp),
            ["C021"] = new(BuffKind.MoveSpeed),
            ["C022"] = new(BuffKind.StopDelay),
            ["C028"] = new(BuffKind.Slow),
            ["C029"] = new(BuffKind.ActiveSkillCharge),

            // 이번에 새로 만든 것
            ["C002"] = new(BuffKind.SingleTarget),
            ["C003"] = new(BuffKind.Execute),
            ["C015"] = new(BuffKind.BossFocus),
            ["C017"] = new(BuffKind.Regen),
            ["C024"] = new(BuffKind.CombatStep),
            ["C025"] = new(BuffKind.FrostImprint),
            ["C026"] = new(BuffKind.FlameImprint),
            ["C027"] = new(BuffKind.CurseImprint),

            ["C005"] = new(BuffKind.SustainStack),
            ["C009"] = new(BuffKind.Homing),
            ["C013"] = new(BuffKind.ExplosiveEcho),
            ["C030"] = new(BuffKind.GhostTurret),
            ["C032"] = new(BuffKind.SpectralEcho),
            ["C004"] = new(BuffKind.ArmorBreak),
            ["C014"] = new(BuffKind.StatusChain),
            ["C018"] = new(BuffKind.CrisisBarrier),
            ["C023"] = new(BuffKind.Afterimage),
            ["C031"] = new(BuffKind.Overcharge),
        };

        /// <summary>분류별 강조색. 아이콘 색조와 맞춘다.</summary>
        private static readonly Dictionary<string, string> CategoryColor = new()
        {
            ["ATTACK"] = "#F0603C", ["PROJECTILE"] = "#4CC8F0", ["AREA"] = "#B45CE0",
            ["SURVIVAL"] = "#5CC850", ["MOBILITY"] = "#3CD8C0", ["UTILITY"] = "#7C6CF0",
            ["SPECIAL"] = "#F0C038",
        };

        [MenuItem("Tools/Game/Canon v2.3 임포트 (카드 32종)")]
        public static void Import()
        {
            var cards = ReadCards();
            if (cards == null) return;

            var table = AssetDatabase.LoadAssetAtPath<BuffTable>(TablePath);
            if (table == null) { Debug.LogError("[CardV23] BuffTable 없음"); return; }

            var so = new SerializedObject(table);
            var entries = so.FindProperty("_entries");
            entries.ClearArray();   // 버프 35종을 카드 32종으로 통째로 바꾼다

            int ready = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                var map = Effect.TryGetValue(c.cardId, out var m) ? m : new Map(BuffKind.Attack, false);
                if (map.Ready) ready++;

                entries.InsertArrayElementAtIndex(i);
                var e = entries.GetArrayElementAtIndex(i);

                e.FindPropertyRelative("_buffKey").stringValue = c.cardId.ToLowerInvariant();
                e.FindPropertyRelative("_cardId").stringValue = c.cardId;
                e.FindPropertyRelative("_nameKr").stringValue = c.nameKr;
                e.FindPropertyRelative("_description").stringValue = c.effect;
                e.FindPropertyRelative("_kind").enumValueIndex = (int)map.Kind;
                e.FindPropertyRelative("_category").stringValue = c.category;
                e.FindPropertyRelative("_rarity").enumValueIndex = (int)RarityOf(c.rarity);
                e.FindPropertyRelative("_stackable").boolValue = map.Stackable;
                e.FindPropertyRelative("_implemented").boolValue = map.Ready;
                e.FindPropertyRelative("_colorHex").stringValue =
                    CategoryColor.TryGetValue(c.category, out var col) ? col : "#F0B428";

                // 정본 v2.3 은 전 챕터에서 뽑힌다. 해금은 마스터리·연구가 가르는데
                // 지금은 아웃게임이 없으므로 테스트 동안 전부 열어 둔다.
                e.FindPropertyRelative("_fromChapter").intValue = 1;
                e.FindPropertyRelative("_scope").enumValueIndex = (int)BuffScope.Common;
                e.FindPropertyRelative("_canonId").stringValue = c.cardId;
                e.FindPropertyRelative("_canonEffect").stringValue = c.runtimeFormula;
                e.FindPropertyRelative("_pool").stringValue = c.category;

                // 등급이 확률을 정하므로 개별 가중치는 1 로 둔다
                e.FindPropertyRelative("_weight").floatValue = 1f;

                // 레벨 1~5
                var lv = e.FindPropertyRelative("_levelValues");
                lv.ClearArray();
                for (int k = 0; k < c.levels.Length; k++)
                {
                    lv.InsertArrayElementAtIndex(k);
                    lv.GetArrayElementAtIndex(k).intValue = Mathf.RoundToInt(c.levels[k].value);
                }
                // 예전 단일 값 자리에는 Lv1 을 넣는다 — 레벨표가 없을 때의 대비책이다
                e.FindPropertyRelative("_value").intValue =
                    c.levels.Length > 0 ? Mathf.RoundToInt(c.levels[0].value) : 0;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CardV23] 카드 {cards.Count}종 — 지금 동작 {ready}종 · 시스템 대기 {cards.Count - ready}종");
        }

        private static CardRarity RarityOf(string s) => s switch
        {
            "RARE" => CardRarity.Rare,
            "EPIC" => CardRarity.Epic,
            "LEGENDARY" => CardRarity.Legendary,
            _ => CardRarity.Common,
        };

        private static List<CardRow> ReadCards()
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                                    Dir, "CARD_MASTER_RUNTIME.json");
            if (!File.Exists(path)) { Debug.LogError("[CardV23] CARD_MASTER_RUNTIME.json 없음"); return null; }
            var file = JsonUtility.FromJson<CardFile>(File.ReadAllText(path));
            return file != null && file.cards != null ? new List<CardRow>(file.cards) : null;
        }

        [Serializable] private sealed class CardFile { public CardRow[] cards; }

        [Serializable]
        private sealed class CardRow
        {
            public string cardId, nameKr, nameEn, rarity, category, trait, effect, unlock, runtimeFormula;
            public LevelRow[] levels = Array.Empty<LevelRow>();
        }

        [Serializable]
        private sealed class LevelRow
        {
            public int level;
            public float value;
            public string unit;
        }
    }
}
