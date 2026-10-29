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
            // PD 2026-10-29 스킬 다시 잡기(`_exchange/plan_party_skills.md`) — 넷째 칸 = 영문 이름(컷인)
            new[]{ "amazon", "도약 강타", "가장 가까운 적에게 뛰어들어 둘레 적을 강타한다.", "Leap Smash" },
            new[]{ "amazon_elite", "전장의 함성", "둘레 적을 밀쳐내고 1.5초간 기절시킨다. 메인이면 최대 체력 20%만큼 쉴드를 얻는다.", "War Cry" },
            new[]{ "baseball", "라인 드라이브", "적을 꿰뚫는 강타구를 친다. 맞은 적은 멀리 날아가고, 날아간 적에 부딪힌 적도 피해를 받는다.", "Line Drive" },
            new[]{ "death", "사신의 시간", "3초간 공격한 적을 즉사시킨다. 중간 보스는 50% 확률, 보스는 10% 확률로 최대 체력 25% 피해.", "Death Harvest" },
            new[]{ "guru", "진압 결계", "둘레 3m 안의 적을 2초간 묶어 움직이지 못하게 한다.", "Binding Ward" },
            new[]{ "ninja_chain", "사슬 결박", "주변 적 5명을 사슬로 내 앞에 끌어와 묶고 한 번에 후려친다.", "Chain Bind" },
            new[]{ "dragoon", "화염 지대", "가장 가까운 적의 발밑에 불바다를 만든다. 불바다 위의 적은 계속 화상 피해를 받는다.", "Inferno Field" },
            new[]{ "salamander", "독 뿜기", "주변 5m 안의 적을 모두 중독시킨다. 중독된 적은 지속 피해를 받고 느려진다.", "Venom Spray" },
            new[]{ "dragon_blue", "뇌전", "둘레 적 5명에게 낙뢰를 한 번씩 떨어뜨린다.", "Thunderclap" },
            new[]{ "commando_grenade", "융단 폭격", "주변에 수류탄을 원형으로 뿌린다. 가까운 적이 있으면 그 적을 노린다.", "Carpet Bomb" },
            new[]{ "snowwoman", "눈보라", "둘레 적을 1.5초간 빙결시킨다. 보스는 얼지 않고 느려진다.", "Blizzard" },
            new[]{ "thug", "난사", "다섯 갈래로 세 번 빠르게 쏜다.", "Bullet Storm" },
            new[]{ "hopper_smg", "백스텝 사격", "가까운 적에게서 뒤로 크게 물러나며 앞쪽으로 부채꼴 연사한다. 맞은 적은 밀려난다.", "Backstep Fire" },
            new[]{ "ninja", "그림자 베기", "가까운 적 3명 사이를 순간이동하며 벤다.", "Shadow Cut" },
            new[]{ "vampire", "박쥐 떼", "가까운 적 넷에게 박쥐가 날아가 물고, 빨아 온 피로 메인을 회복한다. 적 1명당 최대 체력 2%.", "Bat Swarm" },
            new[]{ "commando_mg", "제압 사격", "앞쪽 부채꼴로 2초간 연사한다. 맞은 적은 30% 느려진다.", "Suppressing Fire" },
            new[]{ "gangster", "일제 표식", "체력이 가장 높은 적 3명에게 4초간 표식을 새긴다. 표식이 붙은 적은 받는 피해 +30%.", "Mass Mark" },
            new[]{ "hopper", "조준 사격", "가장 먼 적에게 확정 치명타 3발을 쏜다.", "Aimed Shot" },
            new[]{ "commando_missile", "다탄두", "미사일 7발이 주변 적들에게 한 발씩 날아간다.", "Multi-Warhead" },
            new[]{ "medium", "미니 골렘", "미니 골렘을 소환해 10초간 함께 싸운다.", "Mini Golem" },
            new[]{ "white_wizard", "빛의 인력", "적이 가장 많이 모인 곳에 마법진을 펼쳐 1.2초간 가운데로 끌어당긴 뒤 빛으로 폭발시킨다.", "Light Pull" },
            new[]{ "commando_laser", "충전 레이저", "1초간 힘을 모은 뒤 일직선으로 모든 적을 꿰뚫는 레이저를 쏜다.", "Charge Laser" },
            new[]{ "robot", "포탑 전개", "10초간 자동으로 공격하는 포탑을 설치한다.", "Turret Deploy" },
        };

        /// <summary>호스트키 → 패시브 키 · 이름 · 설명 · 확률(0 이면 상시).</summary>
        private static readonly object[][] Passives =
        {
            // PD 2026-10-29 스킬 다시 잡기 — 다섯째 칸 = 확률(0 이면 상시)
            new object[]{ "amazon", "psv_amazon", "질주", "도약 강타로 적을 처치하면 한 번 더 도약한다.", 0 },
            new object[]{ "amazon_elite", "psv_counter", "불굴", "체력이 30% 아래로 떨어지면 한 판에 한 번 3초간 무적이 되고 최대 체력 30%를 회복한다.", 0 },
            new object[]{ "baseball", "psv_slugger", "반사 신경", "날아온 적 탄을 30% 확률로 자동으로 되받아친다.", 30 },
            new object[]{ "death", "psv_death", "망자 소집", "적을 처치할 때마다 그 자리에 아군 해골을 소환한다.", 0 },
            new object[]{ "guru", "psv_guru", "무형보", "이동 중 지형지물을 통과한다.", 0 },
            new object[]{ "ninja_chain", "psv_ninja_chain", "사슬 감기", "사슬로 끌려온 적은 3초간 받는 피해 +25%.", 0 },
            new object[]{ "dragoon", "psv_dragoon", "용의 피", "불바다 위에 서 있으면 공격력 +30%, 화상을 입지 않는다.", 0 },
            new object[]{ "salamander", "psv_salamander", "독니", "중독된 적에게 주는 피해 +20%.", 0 },
            new object[]{ "dragon_blue", "psv_dragon_blue", "낙뢰", "적중 시 10% 확률로 근처 다른 적에게 번개가 튄다. 근처에 적이 없으면 발동하지 않는다.", 10 },
            new object[]{ "commando_grenade", "psv_commando_grenade", "파편탄", "상대 방어력을 50% 무시한다.", 0 },
            new object[]{ "snowwoman", "psv_snowwoman", "서릿발", "빙결된 적에게 주는 피해 +30%.", 0 },
            new object[]{ "hopper_smg", "psv_hopper_smg", "치고 빠지기", "이동 중에도 기본 공격을 쏜다(위력 60%).", 0 },
            new object[]{ "commando_mg", "psv_commando_mg", "중장갑", "받는 피해 15% 감소.", 0 },
            new object[]{ "thug", "psv_thug", "난전", "근처에 적이 3명 이상이면 기본 공격 속도 +20%.", 0 },
            new object[]{ "ninja", "psv_ninja", "몸놀림", "20% 확률로 받는 피해를 회피한다.", 0 },
            new object[]{ "vampire", "psv_leech", "피의 갈증", "주는 피해의 5%를 늘 흡혈한다. 체력이 가득이면 넘친 만큼 쉴드가 된다.", 0 },
            new object[]{ "gangster", "psv_gangster", "청부 계약", "표식이 붙은 적을 처치하면 액티브 쿨타임이 1초 줄어든다.", 0 },
            new object[]{ "hopper", "psv_momentum", "급소", "치명타 피해 +30%.", 0 },
            new object[]{ "commando_missile", "psv_commando_missile", "표적 고정", "같은 적을 연속으로 맞히면 피해가 10%씩 쌓인다(최대 50%).", 0 },
            new object[]{ "medium", "psv_medium", "강령술", "소환물 최대 수 +2. 적 처치 시 30% 확률로 아군 해골을 소환한다.", 30 },
            new object[]{ "white_wizard", "psv_white_wizard", "빛의 가호", "액티브로 적 3명 이상을 맞히면 최대 체력 15%만큼 쉴드를 얻는다.", 0 },
            new object[]{ "commando_laser", "psv_commando_laser", "집속 광선", "투사체가 지형지물을 무시하고 적을 관통한다.", 0 },
            new object[]{ "robot", "psv_spare_circuit", "예비 회로", "포탑이 서 있는 동안 받는 피해 −20%.", 0 },
        };

        [MenuItem("Tools/Game/스킬 문구 갱신 (명세 2026-09-14)")]   // 문구는 PD 2026-10-29 판
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
