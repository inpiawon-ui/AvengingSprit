using System.Collections.Generic;
using System.IO;
using Game.Character;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 정본 v2.3 EVENT_MASTER 18종을 `EventTable` 로 굽는다.
    ///
    /// 정본은 `EventType`·`RewardType` 을 문자열로만 준다 — 화면에 띄울 글은 없다.
    /// 그래서 문안은 여기서 쓴다. 문안이 없으면 "SPIRIT_SHRINE" 이라는 팻말 앞에서
    /// 예/아니오를 고르게 되는데, 그건 선택이 아니라 퀴즈다.
    ///
    /// 지금 못 주는 보상(빙의 대상 표시·보스 방어막·상점 할인 등)이 걸린 6종은
    /// `_implemented = false` 로 굽는다. 카드에 쓴 것과 같은 규칙이다 —
    /// 골라도 아무 일 없는 선택지가 섞이면 그 방 전체가 거짓이 된다.
    /// </summary>
    public static class EventImporterV23
    {
        private const string Dir = "Projects/AVSR/Canon/v23";
        private const string TablePath = "Assets/BundleResource/TableData/EventTable.asset";

        /// <summary>정본 한 줄 → 우리가 실제로 줄 수 있는 것 + 화면에 띄울 글.</summary>
        private readonly struct Def
        {
            public readonly string Title, Body, Accept, Decline;
            public readonly EventReward Reward;
            public readonly int Value;
            public readonly CardRarity Rarity;
            public readonly int Chance;
            public readonly bool HasDecline;
            public readonly EventReward DeclineReward;
            public readonly int DeclineValue;
            public readonly string Key;
            public readonly bool Fight;
            public readonly int FightCount;
            public readonly bool FightElite;
            public readonly int ExtraGold;
            public readonly bool Ready;
            public readonly string NotReadyWhy;

            public Def(string title, string body, string accept, string decline,
                       EventReward reward, int value, CardRarity rarity = CardRarity.Common,
                       int chance = 0, bool hasDecline = false,
                       EventReward declineReward = EventReward.Gold, int declineValue = 0,
                       bool fight = false, int fightCount = 3, bool fightElite = false,
                       int extraGold = 0, string key = null)
            {
                Key = key;
                Title = title; Body = body; Accept = accept; Decline = decline;
                Reward = reward; Value = value; Rarity = rarity; Chance = chance;
                HasDecline = hasDecline; DeclineReward = declineReward; DeclineValue = declineValue;
                Fight = fight; FightCount = fightCount; FightElite = fightElite; ExtraGold = extraGold;
                Ready = true; NotReadyWhy = null;
            }

            private Def(string why)
            {
                Title = Body = Accept = Decline = string.Empty;
                Reward = EventReward.Gold; Value = 0; Rarity = CardRarity.Common; Chance = 0;
                HasDecline = false; DeclineReward = EventReward.Gold; DeclineValue = 0;
                Key = null;
                Fight = false; FightCount = 0; FightElite = false; ExtraGold = 0;
                Ready = false; NotReadyWhy = why;
            }

            public static Def NotReady(string why) => new(why);
        }

        private static readonly Dictionary<string, Def> Book = new()
        {
            // ── CH1 유령 항구 ────────────────────────────────────
            ["EV_CH1_01"] = new(
                "영혼의 사당",
                "부서진 방파제 끝에 낡은 사당이 서 있다.\n동전을 놓는 자리가 손때에 파여 있다.",
                "동전을 놓는다", "지나간다",
                EventReward.HostHeal, 18),

            ["EV_CH1_02"] = new(
                "버려진 보급품",
                "뒤집힌 상자 하나. 안에 든 것을 다 가져갈 수는 없다.\n장비를 챙기든지, 팔아 넘기든지.",
                "장비를 챙긴다", "팔아 넘긴다  (골드 25)",
                EventReward.CardGrant, 0, CardRarity.Common,
                hasDecline: true, declineReward: EventReward.Gold, declineValue: 25),

            ["EV_CH1_03"] = new(
                "잠긴 금고",
                "다이얼이 세 칸 남았다. 억지로 열면 안의 것이 상할지도 모른다.",
                "억지로 연다", "지나간다",
                EventReward.CardGrant, 0, CardRarity.Rare, chance: 55,
                hasDecline: true, declineReward: EventReward.Gold, declineValue: 10),

            ["EV_CH1_04"] = new(
                "길 잃은 영혼",
                "저도 몸을 찾다 지친 것이 하나 떠 있다.\n제 몫을 조금 떼어 주면 몸으로 가는 길을 넓혀 주겠다고 한다.",
                "영혼을 나눠 준다", "지나간다",
                EventReward.PossessReach, 35),

            ["EV_CH1_05"] = new(
                "야전 의무병",
                "부두 창고에 아직 숨이 붙은 의무병이 있다.\n말은 안 통하지만 손짓은 통한다.",
                "값을 치른다", "내버려 둔다",
                EventReward.HostHeal, 25),

            ["EV_CH1_06"] = new(
                "좁은 길목",
                "짐짝 사이로 길이 하나뿐이다.\n반대편에서 기척이 난다. 돌아갈 수도 있다.",
                "밀고 들어간다", "돌아간다",
                EventReward.Gold, 35, fight: true, fightCount: 3),

            // ── CH2 네온 뒷골목 ──────────────────────────────────
            ["EV_CH2_01"] = new(
                "저주받은 은닉처",
                "네온이 닿지 않는 구석. 손을 넣으면 살이 타는 냄새가 난다.\n안쪽에서 무언가가 빛난다.",
                "손을 넣는다", "물러선다",
                EventReward.CardOffer, 0, CardRarity.Epic),

            ["EV_CH2_02"] = new(
                "이름난 몸",
                "골목 끝에 하나가 서 있다. 이 구역에서 제일 좋은 몸이다.\n비켜 줄 생각은 없어 보인다.",
                "덤빈다", "돌아간다",
                EventReward.CardGrant, 0, CardRarity.Rare, fight: true, fightCount: 1, fightElite: true),

            ["EV_CH2_03"] = new(
                "봉인된 궤짝",
                "값은 비싸지만 안에 든 것은 확실하다.\n이런 물건은 두 번 나오지 않는다.",
                "값을 치른다", "지나간다",
                EventReward.CardGrant, 0, CardRarity.Rare),

            ["EV_CH2_04"] = new(
                "교차사격",
                "양쪽 옥상에 총구가 걸려 있다. 영혼을 조금 태워 몸을 낮추면\n첫 사격은 넘길 수 있다.",
                "영혼을 태워 뚫는다", "돌아간다",
                EventReward.Gold, 70, fight: true, fightCount: 4),
            ["EV_CH2_05"] = new(
                "암시장 연줄",
                "네온 아래에서 누가 손짓한다. 몸을 좀 내주면\n이 구역 상인들에게 말을 넣어 주겠다고 한다.",
                "살을 떼어 준다", "물러선다",
                EventReward.ShopDiscount, 40),
            ["EV_CH2_06"] = new(
                "부서진 제단",
                "금 간 제단이 아직 숨을 쉰다.\n새 것을 주지는 못하지만, 지닌 것 하나는 벼려 준다고 한다.",
                "제단에 올린다", "지나간다",
                EventReward.UpgradeCard, 0),

            // ── CH3 생체기계 요새 ────────────────────────────────
            ["EV_CH3_01"] = new(
                "영혼을 건 내기",
                "배관 사이에 자기와 똑같이 생긴 것이 앉아 있다.\n제 몫을 떼어 주면 길을 보여 주겠다고 한다.",
                "영혼을 떼어 준다", "지나간다",
                EventReward.CardOffer, 0, CardRarity.Epic),

            ["EV_CH3_02"] = new(
                "치명적인 도전",
                "요새의 문지기 둘이 길을 막고 서 있다.\n이겨서 얻을 것이 크고, 져서 잃을 것도 크다.",
                "받아들인다", "돌아간다",
                EventReward.CardGrant, 0, CardRarity.Epic,
                fight: true, fightCount: 2, fightElite: true, extraGold: 100),
            ["EV_CH3_03"] = new(
                "보스 대비",
                "무기고에 파쇄탄이 하나 남아 있다.\n값은 비싸지만, 그 방어막을 한 번은 확실히 벗긴다.",
                "값을 치른다", "지나간다",
                EventReward.BossShieldBreak, 0),
            ["EV_CH3_04"] = new(
                "타락한 호스트",
                "배관 안쪽에서 낫 끄는 소리가 난다.\n살을 조금 내주면 이쪽으로 나온다. 나오면, 빼앗을 수 있다.",
                "살을 내준다", "지나간다",
                EventReward.SpawnHost, 0, key: "death"),

            ["EV_CH3_05"] = new(
                "심연의 거래",
                "바닥이 보이지 않는 통로. 아래에서 목소리가 올라온다.\n값은 영혼으로 받는다고 한다.",
                "영혼을 내준다", "대신 쉬어 간다  (고스트 30% 회복)",
                EventReward.CardGrant, 0, CardRarity.Epic,
                hasDecline: true, declineReward: EventReward.GhostHeal, declineValue: 30),

            ["EV_CH3_06"] = new(
                "마지막 보급 투하",
                "요새 위로 낙하산 하나가 내려온다.\n먼저 집는 쪽이 임자지만, 안에 무엇이 들었는지는 모른다.",
                "전부 털어 넣는다", "지나간다",
                EventReward.CardGrant, 0, CardRarity.Legendary, chance: 45,
                hasDecline: true, declineReward: EventReward.HostHeal, declineValue: 20),
        };

        [MenuItem("Tools/Game/Canon v2.3 임포트 (이벤트 18종)")]
        public static void Import()
        {
            var rows = ReadRows();
            if (rows == null) return;

            var table = AssetDatabase.LoadAssetAtPath<EventTable>(TablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<EventTable>();
                AssetDatabase.CreateAsset(table, TablePath);
            }

            var so = new SerializedObject(table);
            var entries = so.FindProperty("_entries");
            entries.ClearArray();

            int ready = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (!Book.TryGetValue(r.EventId, out var d))
                    d = Def.NotReady("문안 미작성");
                if (d.Ready) ready++;

                entries.InsertArrayElementAtIndex(i);
                var e = entries.GetArrayElementAtIndex(i);

                e.FindPropertyRelative("_eventId").stringValue = r.EventId;
                e.FindPropertyRelative("_chapter").intValue = ChapterNo(r.Pool);
                e.FindPropertyRelative("_canonType").stringValue = r.Type;
                e.FindPropertyRelative("_implemented").boolValue = d.Ready;

                e.FindPropertyRelative("_titleKr").stringValue = d.Ready ? d.Title : r.Type;
                e.FindPropertyRelative("_bodyKr").stringValue = d.Ready ? d.Body : d.NotReadyWhy;
                e.FindPropertyRelative("_acceptKr").stringValue = d.Ready ? d.Accept : "확인";
                e.FindPropertyRelative("_declineKr").stringValue = d.Ready ? d.Decline : string.Empty;

                // 대가는 정본을 그대로 쓴다 — 우리가 정할 값이 아니다
                e.FindPropertyRelative("_costType").enumValueIndex = (int)CostOf(r.CostType);
                e.FindPropertyRelative("_costValue").intValue = r.CostValue;

                e.FindPropertyRelative("_rewardType").enumValueIndex = (int)d.Reward;
                e.FindPropertyRelative("_rewardValue").intValue = d.Value;
                e.FindPropertyRelative("_rewardRarity").enumValueIndex = (int)d.Rarity;
                e.FindPropertyRelative("_rewardKey").stringValue = d.Key ?? string.Empty;
                e.FindPropertyRelative("_chancePercent").intValue = d.Chance;
                e.FindPropertyRelative("_fightFirst").boolValue = d.Fight;
                e.FindPropertyRelative("_fightCount").intValue = d.FightCount;
                e.FindPropertyRelative("_fightElite").boolValue = d.FightElite;
                e.FindPropertyRelative("_extraGold").intValue = d.ExtraGold;
                e.FindPropertyRelative("_hasDeclineReward").boolValue = d.HasDecline;
                e.FindPropertyRelative("_declineReward").enumValueIndex = (int)d.DeclineReward;
                e.FindPropertyRelative("_declineValue").intValue = d.DeclineValue;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            Debug.Log($"[EventV23] 이벤트 {rows.Count}종 — 지금 동작 {ready}종 · 시스템 대기 {rows.Count - ready}종");
        }

        private static EventCost CostOf(string s) => s switch
        {
            "GOLD" => EventCost.Gold,
            "GHOST_HP" => EventCost.GhostHp,
            "HOST_HP" => EventCost.HostHp,
            _ => EventCost.None,
        };

        private static int ChapterNo(string pool)
            => pool != null && pool.Contains("CH2") ? 2
             : pool != null && pool.Contains("CH3") ? 3 : 1;

        private readonly struct Row
        {
            public readonly string EventId, Pool, Type, CostType;
            public readonly int CostValue;
            public Row(string id, string pool, string type, string costType, int costValue)
            { EventId = id; Pool = pool; Type = type; CostType = costType; CostValue = costValue; }
        }

        private static List<Row> ReadRows()
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                                    Dir, "EVENT_MASTER.json");
            if (!File.Exists(path)) { Debug.LogError("[EventV23] EVENT_MASTER.json 없음"); return null; }

            var rows = FlatJson.Rows(File.ReadAllText(path));
            if (rows == null) { Debug.LogError("[EventV23] 파싱 실패"); return null; }

            var list = new List<Row>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                var m = rows[i];
                list.Add(new Row(
                    Str(m, "EventID"), Str(m, "ChapterPool"), Str(m, "EventType"),
                    Str(m, "CostType"), Int(m, "CostValue")));
            }
            return list;
        }

        private static string Str(Dictionary<string, string> m, string k)
            => m.TryGetValue(k, out var v) ? v : string.Empty;

        private static int Int(Dictionary<string, string> m, string k)
            => m.TryGetValue(k, out var v) && int.TryParse(v, out var n) ? n : 0;
    }
}
