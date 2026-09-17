using System.Collections.Generic;
using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 스킬 **문구**를 명세 2026-09-14(대화)로 맞춘다 — 액티브 23종 · 패시브 23종.
    ///
    /// 왜 따로 있나 — 액티브 이름·설명은 정본 임포터(`CanonImporterV23`)가 굽고,
    /// 패시브 표는 손으로 만든 에셋이었다. 새 명세는 대화로 왔으므로 정본 파일을 고치지 않고
    /// **이 도구가 문구만 덮어쓴다.** 정본 임포터를 다시 돌렸으면 이것을 한 번 더 돌리면 된다.
    ///
    /// ⚠ **실제 동작은 여기 없다.** 무슨 일이 벌어지는지는
    ///   `BattleDirector.SkillsNew.cs`(액티브) · `BattleDirector.HostPassives.cs`(패시브)가 정본이다.
    ///   이 표는 화면에 띄우는 글일 뿐이라, 둘이 어긋나면 **코드 쪽이 맞다.**
    /// </summary>
    public static class HostSkillTextImporter
    {
        private const string HostPath = "Assets/BundleResource/TableData/HostTable.asset";
        private const string ActivePath = "Assets/BundleResource/TableData/ActiveSkillTable.asset";
        private const string PassivePath = "Assets/BundleResource/TableData/PassiveSkillTable.asset";

        /// <summary>호스트키 → 액티브 이름 · 설명.</summary>
        private static readonly string[][] Actives =
        {
            // 넷째 칸 = 영문 이름. 스킬을 명세 2026-09-14 로 바꾸면서 한글만 바뀌고 영문은 정본(예전 스킬)
            // 이름으로 남아, 컷인에 「雷電暴走 / GLACIAL BREATH」처럼 서로 다른 스킬이 떴다(기획 2026-09-17).
            // 아마존 정예는 스킬 보류라 정본 값 그대로 둔다.
            new[]{ "amazon",           "도약 강타",     "가장 가까운 적에게 뛰어들어 둘레를 300% 피해로 친다.", "Valkyrie Leap" },
            new[]{ "amazon_elite",     "불굴",          "무적 2.5초 + 쉴드 상한 해제. 해제가 무적보다 1.5초 더 간다." },
            new[]{ "baseball",         "전탄 반사",     "4초간 날아오는 탄을 되받아친다. 반사탄은 더 아프다.", "Full Reflect" },
            new[]{ "death",            "사신의 시간",   "3초간 때리는 적이 죽는다. 중간 보스는 절반, 보스는 열에 하나.", "Death Harvest" },
            new[]{ "guru",             "수호 결계",     "3초간 받는 피해 −50% · 쉴드 획득 2배.", "Guardian Ward" },
            new[]{ "ninja_chain",      "사슬 결박",     "방 안 모든 적을 5초간 묶는다. 묶인 적은 더 아프게 맞는다.", "Chain Bind" },
            new[]{ "dragoon",          "화염 지대",     "가장 가까운 적 발밑에 불바다를 편다. 밟고 선 적이 계속 탄다.", "Inferno Field" },
            new[]{ "salamander",       "독 뿜기",       "둘레 5 m 의 적을 모두 중독시킨다. 독은 아프고 느려진다.", "Venom Spray" },
            new[]{ "dragon_blue",      "뇌전 폭주",     "2초간 번개 튕김이 반드시 터진다.", "Thunder Rampage" },
            new[]{ "commando_grenade", "융단 폭격",     "수류탄 5발을 부채꼴로 동시에 던진다.", "Carpet Bombing" },
            new[]{ "snowwoman",        "얼음 감옥",     "2초간 얼음에 들어간다. 맞지도 때리지도 않고 체력 30% 를 채운다.", "Ice Prison" },
            new[]{ "thug",             "난사",          "1초간 3방향으로 쏟아붓는다.", "Street Barrage" },
            new[]{ "hopper_smg",       "도약 강습",     "가장 먼 적에게 뛰어들고 2초간 무적.", "Phantom Leap" },
            new[]{ "ninja",            "그림자 분신",   "방 한가운데 분신을 세운다. 5초간 적이 전부 분신을 노린다.", "Shadow Clone" },
            new[]{ "vampire",          "혈연",          "둘레 5 m 의 적 머릿수만큼 최대 체력의 2% 씩 회복한다.", "Blood Feast" },
            new[]{ "commando_mg",      "방벽 전개",     "5초간 최대 체력만큼 쉴드를 두른다. 깎이면 끝난다.", "Barrier Deploy" },
            new[]{ "gangster",         "일제 표식",     "방 안 모든 적에게 3초간 표식을 새긴다.", "Mass Death Mark" },
            new[]{ "hopper",           "정조준",        "5초간 치명타 확률이 90% 로 고정된다.", "Dead Aim" },
            new[]{ "commando_missile", "다중 유도",     "반원으로 퍼진 8발이 한 대상으로 모여 든다.", "Multi Homing" },
            new[]{ "medium",           "골렘 소환",     "골렘을 세운다. 해골보다 크고 오래 싸운다.", "Summon Golem" },
            new[]{ "white_wizard",     "광휘 확산",     "부채꼴 8방향으로 광탄을 쏜다.", "Radiant Burst" },
            new[]{ "commando_laser",   "연쇄 방전",     "3초간 주변 적에게 전기가 계속 튄다.", "Chain Discharge" },
            new[]{ "robot",            "포탑 전개",     "10초간 자동으로 쏘는 포탑을 세운다.", "Turret Deploy" },
        };

        /// <summary>호스트키 → 패시브 키 · 이름 · 설명 · 확률(0 이면 상시).</summary>
        private static readonly object[][] Passives =
        {
            new object[]{ "amazon",           "psv_amazon",           "쉴드 질주",   "쉴드가 찰수록 빨라진다. 가득이면 이동속도 +40%.", 0 },
            new object[]{ "amazon_elite",     "psv_counter",          "역전의 자세", "쉴드가 남아 있는 동안 공격력이 오른다.", 0 },
            new object[]{ "baseball",         "psv_slugger",          "장외 타구",   "막타에 20% 로 주변 적을 날려 버린다.", 20 },
            new object[]{ "death",            "psv_death",            "망자 소집",   "적을 잡을 때마다 그 자리에서 해골이 일어선다.", 0 },
            new object[]{ "guru",             "psv_guru",             "무형보",      "걸어 다닐 때 지형지물을 통과한다.", 0 },
            new object[]{ "ninja_chain",      "psv_ninja_chain",      "흘리기",      "10% 로 피해를 통째로 흘린다.", 10 },
            new object[]{ "dragoon",          "psv_dragoon",          "불씨",        "명중한 적에게 10% 로 화상. 겹치지 않는다.", 10 },
            new object[]{ "salamander",       "psv_salamander",       "독니",        "명중한 적에게 10% 로 독. 겹치지 않는다.", 10 },
            new object[]{ "dragon_blue",      "psv_dragon_blue",      "낙뢰",        "10% 로 옆 적에게 번개가 튄다. 튈 적이 없으면 터지지 않는다.", 10 },
            new object[]{ "commando_grenade", "psv_commando_grenade", "파편탄",      "상대 방어력을 50% 무시한다.", 0 },
            new object[]{ "snowwoman",        "psv_snowwoman",        "서릿발",      "30% 로 적을 얼린다. 얼어 있는 적에게는 더 아프다. 보스는 안 걸린다.", 30 },
            new object[]{ "hopper_smg",       "psv_hopper_smg",       "충격 반동",   "10% 로 맞은 적을 밀어낸다.", 10 },
            new object[]{ "commando_mg",      "psv_commando_mg",      "중장갑",      "방어력 20% 증가.", 0 },
            new object[]{ "thug",             "psv_thug",             "삥",          "주운 골드가 10% 더 들어온다.", 0 },
            new object[]{ "ninja",            "psv_ninja",            "질주",        "적을 잡으면 1.5초간 이동속도 +30%.", 0 },
            new object[]{ "vampire",          "psv_leech",            "흡혈",        "8% 로 공격력의 5% 만큼 회복한다.", 8 },
            new object[]{ "gangster",         "psv_gangster",         "처형 계약",   "표식이 붙은 적을 20% 로 즉사. 엘리트는 최대 체력 50%, 보스는 30%.", 20 },
            new object[]{ "hopper",           "psv_momentum",         "급소",        "치명타 피해 +30%.", 0 },
            new object[]{ "commando_missile", "psv_commando_missile", "고폭탄",      "투사체가 지형지물을 무시한다.", 0 },
            new object[]{ "medium",           "psv_medium",           "강령",        "적을 잡으면 30% 로 해골이 일어선다.", 30 },
            new object[]{ "white_wizard",     "psv_white_wizard",     "관통 광선",   "투사체가 지형지물을 무시하고 적을 관통한다.", 0 },
            new object[]{ "commando_laser",   "psv_commando_laser",   "집속 광선",   "투사체가 지형지물을 무시하고 적을 관통한다.", 0 },
            new object[]{ "robot",            "psv_spare_circuit",    "예비 회로",   "평타가 2% 로 액티브 쿨을 1초 당긴다.", 2 },
        };

        [MenuItem("Tools/Game/스킬 문구 갱신 (명세 2026-09-14)")]
        public static void Import()
        {
            var hostTable = AssetDatabase.LoadAssetAtPath<HostTable>(HostPath);
            var activeTable = AssetDatabase.LoadAssetAtPath<ActiveSkillTable>(ActivePath);
            var passiveTable = AssetDatabase.LoadAssetAtPath<PassiveSkillTable>(PassivePath);
            if (hostTable == null || activeTable == null || passiveTable == null)
            {
                Debug.LogError("[스킬 문구] 표를 못 찾았다.");
                return;
            }

            // 호스트키 → 액티브 키. 액티브 표는 키로 찾아야 하므로 호스트 표에서 먼저 뽑는다.
            var activeKeyOf = new Dictionary<string, string>();
            var hostSo = new SerializedObject(hostTable);
            var hostArr = hostSo.FindProperty("_entries");
            for (int i = 0; i < hostArr.arraySize; i++)
            {
                var e = hostArr.GetArrayElementAtIndex(i);
                activeKeyOf[e.FindPropertyRelative("_hostKey").stringValue]
                    = e.FindPropertyRelative("_activeSkillKey").stringValue;
            }

            // ── 액티브 문구 ──────────────────────────────────
            var actSo = new SerializedObject(activeTable);
            var actArr = actSo.FindProperty("_entries");
            int actDone = 0;
            foreach (var row in Actives)
            {
                if (!activeKeyOf.TryGetValue(row[0], out var key) || string.IsNullOrEmpty(key))
                {
                    Debug.LogWarning($"[스킬 문구] 액티브 키를 못 찾았다: {row[0]}");
                    continue;
                }
                for (int i = 0; i < actArr.arraySize; i++)
                {
                    var e = actArr.GetArrayElementAtIndex(i);
                    if (e.FindPropertyRelative("_activeSkillKey").stringValue != key) continue;
                    e.FindPropertyRelative("_nameKr").stringValue = row[1];
                    e.FindPropertyRelative("_description").stringValue = row[2];
                    if (row.Length > 3) e.FindPropertyRelative("_nameEn").stringValue = row[3];
                    actDone++;
                    break;
                }
            }
            actSo.ApplyModifiedPropertiesWithoutUndo();

            // ── 패시브 표를 통째로 다시 쓴다 ──────────────────
            //
            // 예전에는 11명만 가졌다. 새 명세는 23명 전원이라 줄을 맞추는 것보다 다시 쓰는 편이 낫다.
            var pasSo = new SerializedObject(passiveTable);
            var pasArr = pasSo.FindProperty("_entries");
            pasArr.ClearArray();
            for (int i = 0; i < Passives.Length; i++)
            {
                var row = Passives[i];
                pasArr.InsertArrayElementAtIndex(i);
                var e = pasArr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("_passiveSkillKey").stringValue = (string)row[1];
                e.FindPropertyRelative("_nameEn").stringValue = ((string)row[1]).Replace("psv_", "").ToUpperInvariant();
                e.FindPropertyRelative("_nameKr").stringValue = (string)row[2];
                e.FindPropertyRelative("_description").stringValue = (string)row[3];
                int chance = (int)row[4];
                e.FindPropertyRelative("_kind").enumValueIndex = chance > 0 ? (int)PassiveKind.Chance : (int)PassiveKind.Always;
                e.FindPropertyRelative("_chancePercent").intValue = chance;
            }
            pasSo.ApplyModifiedPropertiesWithoutUndo();

            // ── 호스트에 패시브를 물린다 ─────────────────────
            int linked = 0;
            for (int i = 0; i < hostArr.arraySize; i++)
            {
                var e = hostArr.GetArrayElementAtIndex(i);
                string hostKey = e.FindPropertyRelative("_hostKey").stringValue;
                string passiveKey = "";
                foreach (var row in Passives)
                    if ((string)row[0] == hostKey) { passiveKey = (string)row[1]; break; }
                e.FindPropertyRelative("_passiveSkillKey").stringValue = passiveKey;
                if (!string.IsNullOrEmpty(passiveKey)) linked++;
            }
            hostSo.ApplyModifiedPropertiesWithoutUndo();

            // ── 주인 없는 액티브를 뺀다 ─────────────────────
            //
            // 어떤 호스트도 쓰지 않는 줄이 남아 있으면 강화 화면이 그것을 띄울 수 있고,
            // 표를 읽는 사람도 "이건 누구 것인가" 를 매번 되묻게 된다.
            var used = new HashSet<string>(activeKeyOf.Values);
            int dropped = 0;
            for (int i = actArr.arraySize - 1; i >= 0; i--)
            {
                var key = actArr.GetArrayElementAtIndex(i).FindPropertyRelative("_activeSkillKey").stringValue;
                if (used.Contains(key)) continue;
                Debug.Log($"[스킬 문구] 주인 없는 액티브 제거: {key}");
                actArr.DeleteArrayElementAtIndex(i);
                dropped++;
            }
            if (dropped > 0) actSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(hostTable);
            EditorUtility.SetDirty(activeTable);
            EditorUtility.SetDirty(passiveTable);
            AssetDatabase.SaveAssets();
            Debug.Log($"[스킬 문구] 액티브 {actDone}종 · 패시브 {Passives.Length}종 · 호스트 연결 {linked}명");
        }
    }
}
