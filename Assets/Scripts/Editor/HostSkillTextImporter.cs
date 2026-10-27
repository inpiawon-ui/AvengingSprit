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
            new[]{ "amazon", "도약 강타", "가장 가까운 적에게 도약해 주변 적에게 300% 피해를 준다.", "Leap Smash" },
            new[]{ "amazon_elite", "불굴", "2.5초간 무적이 된다.", "Unyielding" },
            new[]{ "baseball", "전탄 반사", "5초간 날아오는 탄을 반사한다. 반사된 탄은 더 큰 피해를 준다.", "Full Reflect" },
            new[]{ "death",            "사신의 시간",   "3초간 공격한 적을 즉사시킨다. 중간 보스는 50% 확률, 보스는 10% 확률로 최대 체력 25% 피해.", "Death Harvest" },
            new[]{ "guru",             "수호 결계",     "3초간 받는 피해 −50% · 쉴드 획득 2배.", "Guardian Ward" },
            new[]{ "ninja_chain",      "사슬 끌어오기", "주변 적 5명을 사슬로 내 앞으로 끌어와 속박하고 한 번에 후려친다. 속박된 적은 받는 피해가 늘어난다.", "Chain Pull" },   // PD 2026-10-21
            new[]{ "dragoon",          "화염 지대",     "가장 가까운 적의 발밑에 불바다를 만든다. 불바다 위의 적은 계속 화상 피해를 받는다.", "Inferno Field" },
            new[]{ "salamander",       "독 뿜기",       "주변 5m 안의 적을 모두 중독시킨다. 중독된 적은 지속 피해를 받고 느려진다.", "Venom Spray" },
            new[]{ "dragon_blue",      "뇌전 폭주",     "2초간 패시브 낙뢰가 100% 발동한다.", "Thunder Rampage" },
            new[]{ "commando_grenade", "융단 폭격", "주변에 수류탄을 원형으로 뿌린다. 가까운 적이 있으면 그 적을 노린다.", "Carpet Bomb" },   // PD 2026-10-21
            new[]{ "snowwoman",        "얼음 감옥",     "2초간 얼음에 들어간다. 맞지도 때리지도 않고 체력 30% 를 채운다.", "Ice Prison" },
            new[]{ "thug", "난사", "다섯 갈래로 세 번 빠르게 쏜다.", "Bullet Storm" },   // PD 2026-10-21
            new[]{ "hopper_smg", "백스텝 사격", "가까운 적에게서 뒤로 크게 물러나며 앞쪽으로 부채꼴 연사한다.", "Backstep Fire" },
            new[]{ "ninja",            "그림자 걸음",   "4초간 회피 50% · 이동 속도 +35%, 잔상이 남는다.", "Shadow Step" },   // PD 2026-10-21
            new[]{ "vampire",          "혈연",          "화면 안의 적마다 박쥐를 날려 피를 빨아 온다. 적 1명당 최대 체력 2% 회복.", "Blood Feast" },
            new[]{ "commando_mg",      "방벽 전개",     "5초간 최대 체력만큼 쉴드를 얻는다. 쉴드가 다 깎이면 끝난다.", "Barrier Deploy" },
            new[]{ "gangster",         "일제 표식",     "방 안 모든 적에게 3초간 표식을 새긴다.", "Mass Death Mark" },
            new[]{ "hopper",           "정조준",        "5초간 치명타 확률이 90% 로 고정된다.", "Dead Aim" },
            new[]{ "commando_missile", "다중 유도",     "미사일 7발을 주변 적들에게 한 발씩 쏜다.", "Multi Homing" },   // PD 2026-10-21
            new[]{ "medium", "미니 골렘", "미니 골렘을 소환해 10초간 함께 싸운다.", "Mini Golem" },   // PD 2026-10-21
            new[]{ "white_wizard",     "광휘 확산",     "부채꼴 8방향으로 광탄을 쏜다.", "Radiant Burst" },
            new[]{ "commando_laser",   "연쇄 방전",     "3초간 주변 적에게 전기가 계속 튄다.", "Chain Discharge" },
            new[]{ "robot",            "포탑 전개",     "10초간 자동으로 공격하는 포탑을 설치한다.", "Turret Deploy" },
        };

        /// <summary>호스트키 → 패시브 키 · 이름 · 설명 · 확률(0 이면 상시).</summary>
        private static readonly object[][] Passives =
        {
            new object[]{ "amazon",           "psv_amazon",           "쉴드 질주",   "쉴드가 찰수록 이동 속도가 빨라진다(최대 +40%).", 0 },
            new object[]{ "amazon_elite",     "psv_counter",          "역전의 자세", "쉴드가 남아 있는 동안 공격력이 오른다.", 0 },
            new object[]{ "baseball",         "psv_slugger",          "장외 타구",   "적 처치 시 20% 확률로 주변 적을 날려 보낸다.", 20 },
            new object[]{ "death",            "psv_death",            "망자 소집",   "적을 처치할 때마다 그 자리에 아군 해골을 소환한다.", 0 },
            new object[]{ "guru",             "psv_guru",             "무형보",      "이동 중 지형지물을 통과한다.", 0 },
            new object[]{ "ninja_chain",      "psv_ninja_chain",      "흘리기",      "10% 확률로 받는 피해를 회피한다.", 10 },
            new object[]{ "dragoon",          "psv_dragoon",          "불씨",        "적중 시 10% 확률로 화상을 건다. 중첩되지 않는다.", 10 },
            new object[]{ "salamander",       "psv_salamander",       "독니",        "적중 시 10% 확률로 중독시킨다. 중독된 적은 받는 피해 +25%.", 10 },
            new object[]{ "dragon_blue",      "psv_dragon_blue",      "낙뢰",        "적중 시 10% 확률로 근처 다른 적에게 번개가 튄다. 근처에 적이 없으면 발동하지 않는다.", 10 },
            new object[]{ "commando_grenade", "psv_commando_grenade", "파편탄",      "상대 방어력을 50% 무시한다.", 0 },
            new object[]{ "snowwoman",        "psv_snowwoman",        "서릿발",      "적중 시 30% 확률로 적을 빙결시킨다. 빙결된 적에게 주는 피해 +30%. 보스는 면역.", 30 },
            new object[]{ "hopper_smg",       "psv_hopper_smg",       "충격 반동",   "적중 시 10% 확률로 적을 넉백시킨다.", 10 },
            new object[]{ "commando_mg",      "psv_commando_mg",      "중장갑",      "방어력 20% 증가.", 0 },
            new object[]{ "thug",             "psv_thug",             "삥",          "주운 골드가 10% 더 들어온다.", 0 },
            new object[]{ "ninja",            "psv_ninja",            "질주",        "적 처치 시 1.5초간 이동 속도 +30%.", 0 },
            new object[]{ "vampire",          "psv_leech",            "흡혈",        "적중 시 8% 확률로 공격력의 5%만큼 체력을 회복한다.", 8 },
            new object[]{ "gangster",         "psv_gangster",         "처형 계약",   "표식이 붙은 적을 20% 확률로 즉사시킨다. 엘리트는 최대 체력 50%, 보스는 30% 피해.", 20 },
            new object[]{ "hopper",           "psv_momentum",         "급소",        "치명타 피해 +30%.", 0 },
            new object[]{ "commando_missile", "psv_commando_missile", "고폭탄",      "투사체가 지형지물을 무시한다.", 0 },
            new object[]{ "medium",           "psv_medium",           "강령",        "적 처치 시 30% 확률로 아군 해골을 소환한다. 해골은 적의 공격을 받지 않고 3초 뒤 사라진다.", 30 },
            new object[]{ "white_wizard",     "psv_white_wizard",     "관통 광선",   "투사체가 지형지물을 무시하고 적을 관통한다.", 0 },
            new object[]{ "commando_laser",   "psv_commando_laser",   "집속 광선",   "투사체가 지형지물을 무시하고 적을 관통한다.", 0 },
            new object[]{ "robot",            "psv_spare_circuit",    "예비 회로",   "기본 공격 시 2% 확률로 액티브 쿨타임이 1초 줄어든다.", 2 },
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
