using System.Collections.Generic;
using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 성급과 함께 오르는 스킬 값을 표에 굽는다(기획 2026-10-06 — 「성급이 오르면 스킬도 · 패시브도 오른다」).
    ///
    /// **패시브** — 대표 수치 하나가 Lv1 → 만렙으로 오른다(% 단위). Lv1 은 전투 코드의 상수와 같고, 만렙은 임시로 1.5~2배(TBD-BAL).
    ///   오르는 수치가 없는 패시브(무형보 · 고폭탄 · 관통 광선 · 집속 광선 · 망자 소집)는 표에 없다 — 성급 창에 줄이 안 생긴다.
    /// **액티브** — 값은 이미 표(`_scaling` 기본 축)에 있다. 여기서는 그 값이 **화면에서 읽히는 모양**(단위 · 배수)만 붙인다.
    ///   어느 값이 무엇인지는 전투 코드에서 읽은 것이다(`BattleDirector.Skills*.cs` — 2026-10-06 조사).
    ///   기본 축을 전투가 안 읽는 스킬(h02 · h15 · h16 · h18 · h23)은 단위 None — 창에 줄이 안 생긴다.
    /// 값의 이름(「즉사 확률」 · 「표식 시간」 등)은 언어팩 `pskill.{키}.growth` · `askill.{키}.growth`.
    /// </summary>
    public static class SkillGrowthImporter
    {
        private const string PassivePath = "Assets/BundleResource/TableData/PassiveSkillTable.asset";
        private const string ActivePath = "Assets/BundleResource/TableData/ActiveSkillTable.asset";

        // 패시브 키 → (Lv1, 만렙) %. 전투 코드의 어느 상수를 대신하는지는 `BattleDirector.HostPassives.cs`.
        private static readonly Dictionary<string, (float min, float max)> Passive = new()
        {
            { "psv_amazon",           (40f, 60f) },   // 쉴드 질주 — 이동속도
            { "psv_counter",          (15f, 25f) },   // 역전의 자세 — 공격력
            { "psv_slugger",          (20f, 35f) },   // 장외 타구 — 날려 보낼 확률
            { "psv_ninja_chain",      (10f, 18f) },   // 흘리기 — 회피
            { "psv_dragoon",          (10f, 18f) },   // 불씨 — 화상 확률
            { "psv_salamander",       (10f, 18f) },   // 독니 — 중독 확률
            { "psv_dragon_blue",      (10f, 18f) },   // 낙뢰 — 번개 확률
            { "psv_commando_grenade", (50f, 75f) },   // 파편탄 — 방어 무시
            { "psv_snowwoman",        (30f, 45f) },   // 서릿발 — 빙결 확률
            { "psv_hopper_smg",       (10f, 18f) },   // 충격 반동 — 밀쳐내기 확률
            { "psv_commando_mg",      (20f, 35f) },   // 중장갑 — 방어력 증가
            { "psv_thug",             (10f, 20f) },   // 삥 — 골드 증가
            { "psv_ninja",            (30f, 45f) },   // 질주 — 이동속도
            { "psv_leech",            (8f, 14f) },    // 흡혈 — 발동 확률
            { "psv_gangster",         (20f, 30f) },   // 처형 계약 — 즉사 확률
            { "psv_momentum",         (30f, 50f) },   // 급소 — 치명타 피해
            { "psv_medium",           (30f, 45f) },   // 강령 — 해골 확률
            { "psv_spare_circuit",    (2f, 4f) },     // 예비 회로 — 발동 확률
        };

        // 액티브 키 → (단위, 배수). 배수는 표 값이 화면 숫자가 되는 곱이다(전투 코드가 곱하는 상수 그대로).
        private static readonly Dictionary<string, (SkillGrowthUnit unit, float mul)> Active = new()
        {
            { "psg_h01", (SkillGrowthUnit.Multiplier, 6f) },    // 도약 강타 — 피해(공격력 ×). AmazonStrikeMul 6 을 곱한다
            { "psg_h03", (SkillGrowthUnit.Multiplier, 1f) },    // 융단 폭격 — 수류탄 한 발 피해
            { "psg_h04", (SkillGrowthUnit.Seconds, 1f) },       // 연쇄 방전 — 지속
            { "psg_h05", (SkillGrowthUnit.Percent, 100f) },     // 방벽 전개 — 쉴드(최대 체력 %)
            { "psg_h06", (SkillGrowthUnit.Multiplier, 1f) },    // 다중 유도 — 미사일 한 발 피해
            { "psg_h07", (SkillGrowthUnit.Seconds, 1f) },       // 뇌전 폭주 — 폭주 시간
            { "psg_h08", (SkillGrowthUnit.Multiplier, 1f) },    // 독 뿜기 — 독 피해
            { "psg_h09", (SkillGrowthUnit.Multiplier, 1f) },    // 화염 지대 — 초당 피해
            { "psg_h10", (SkillGrowthUnit.Seconds, 1f) },       // 일제 표식 — 표식 시간
            { "psg_h11", (SkillGrowthUnit.Seconds, 1f) },       // 난사 — 난사 시간
            { "psg_h12", (SkillGrowthUnit.Percent, 100f) },     // 수호 결계 — 받는 피해 감소
            { "psg_h13", (SkillGrowthUnit.Seconds, 1f) },       // 정조준 — 치명타 고정 시간
            { "psg_h14", (SkillGrowthUnit.Seconds, 1f) },       // 도약 강습 — 무적 시간
            { "psg_h17", (SkillGrowthUnit.Seconds, 1f) },       // 사슬 결박 — 결박 시간
            { "psg_h19", (SkillGrowthUnit.Multiplier, 1f) },    // 포탑 전개 — 포탑 한 발 피해
            { "psg_h20", (SkillGrowthUnit.Seconds, 1f) },       // 전탄 반사 — 반사 시간
            { "psg_h21", (SkillGrowthUnit.Seconds, 1f) },       // 얼음 감옥 — 얼음 시간
            { "psg_h22", (SkillGrowthUnit.Percent, 2f) },       // 혈연 — 적 하나당 회복(최대 체력 %). 0.02 × 값
        };

        [MenuItem("Tools/Game/스킬 성장 값 굽기 (성급과 함께 오르는 수치)")]
        public static void Run()
        {
            var passive = AssetDatabase.LoadAssetAtPath<PassiveSkillTable>(PassivePath);
            var active = AssetDatabase.LoadAssetAtPath<ActiveSkillTable>(ActivePath);
            if (passive == null || active == null) { Debug.LogError("[스킬 성장] 표가 없다"); return; }

            int p = 0, a = 0;
            foreach (var e in passive.Entries)
            {
                bool has = Passive.TryGetValue(e.PassiveSkillKey, out var g);
                e.SetGrowth(has ? g.min : 0f, has ? g.max : 0f);
                if (has) p++;
            }
            foreach (var e in active.Entries)
            {
                bool has = Active.TryGetValue(e.ActiveSkillKey, out var g);
                e.SetGrowth(has ? g.unit : SkillGrowthUnit.None, has ? g.mul : 1f);
                if (has) a++;
            }
            foreach (var key in Passive.Keys) if (passive.Get(key) == null) Debug.LogWarning("[스킬 성장] 패시브 표에 없는 키: " + key);
            foreach (var key in Active.Keys) if (active.Get(key) == null) Debug.LogWarning("[스킬 성장] 액티브 표에 없는 키: " + key);

            EditorUtility.SetDirty(passive);
            EditorUtility.SetDirty(active);
            AssetDatabase.SaveAssets();
            Debug.Log($"[스킬 성장] 패시브 {p}/{passive.Entries.Count} · 액티브 {a}/{active.Entries.Count} 오름");
        }
    }
}
