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
            // PD 2026-10-29 패시브 다시 잡기 — 질주(아마존) · 청부 계약(갱스터)은 오르는 수치가 없다
            { "psv_counter",          (30f, 45f) },   // 불굴 — 회복량(최대 체력 %)
            { "psv_slugger",          (30f, 45f) },   // 반사 신경 — 반사 확률
            { "psv_ninja_chain",      (25f, 40f) },   // 사슬 감기 — 추가 피해
            { "psv_dragoon",          (30f, 45f) },   // 용의 피 — 공격력
            { "psv_salamander",       (20f, 35f) },   // 독니 — 추가 피해
            { "psv_dragon_blue",      (10f, 18f) },   // 낙뢰 — 번개 확률
            { "psv_commando_grenade", (50f, 75f) },   // 파편탄 — 방어 무시
            { "psv_snowwoman",        (30f, 45f) },   // 서릿발 — 추가 피해
            { "psv_hopper_smg",       (60f, 80f) },   // 치고 빠지기 — 이동 사격 위력
            { "psv_commando_mg",      (15f, 25f) },   // 중장갑 — 받는 피해 감소(기획서 −15% · Lv5 −25% 시험값)
            { "psv_thug",             (20f, 35f) },   // 난전 — 공격 속도
            { "psv_ninja",            (20f, 30f) },   // 몸놀림 — 회피
            { "psv_leech",            (5f, 8f) },   // 피의 갈증 — 흡혈량
            { "psv_momentum",         (30f, 50f) },   // 급소 — 치명타 피해
            { "psv_commando_missile", (10f, 15f) },   // 표적 고정 — 겹마다 피해
            { "psv_medium",           (30f, 45f) },   // 강령술 — 해골 확률
            { "psv_white_wizard",     (15f, 25f) },   // 빛의 가호 — 쉴드량
            { "psv_spare_circuit",    (20f, 30f) },   // 예비 회로 — 받는 피해 감소
        };

        // 액티브 키 → (단위, 배수). 배수는 표 값이 화면 숫자가 되는 곱이다(전투 코드가 곱하는 상수 그대로).
        private static readonly Dictionary<string, (SkillGrowthUnit unit, float mul)> Active = new()
        {
            { "psg_h01", (SkillGrowthUnit.Multiplier, 6f) },    // 도약 강타 — 피해(공격력 ×). AmazonStrikeMul 6 을 곱한다
            { "psg_h03", (SkillGrowthUnit.Multiplier, 1f) },    // 융단 폭격 — 수류탄 한 발 피해
            { "psg_h04", (SkillGrowthUnit.Multiplier, 3.5f / 3f) },   // 충전 레이저 — 피해 ×3.5 × 축 비율(축 3 → 4.5)
            { "psg_h05", (SkillGrowthUnit.Multiplier, 0.3f) },   // 제압 사격 — 한 발 ×0.3 × 축(1 → 1.5)
            { "psg_h06", (SkillGrowthUnit.Multiplier, 1f) },    // 다중 유도 — 미사일 한 발 피해
            { "psg_h07", (SkillGrowthUnit.Multiplier, 0.75f) },   // 뇌전 — 낙뢰 ×1.5 × 축 비율(축 2 → 3)
            { "psg_h08", (SkillGrowthUnit.Multiplier, 1f) },    // 독 뿜기 — 독 피해
            { "psg_h09", (SkillGrowthUnit.Multiplier, 1f) },    // 화염 지대 — 초당 피해
            { "psg_h10", (SkillGrowthUnit.Seconds, 4f / 3f) },   // 일제 표식 — 4초 × 축 비율(축 3 → 5)
            { "psg_h11", (SkillGrowthUnit.Seconds, 1f) },       // 난사 — 난사 시간
            { "psg_h12", (SkillGrowthUnit.Seconds, 4f) },   // 진압 결계 — 2초 × 축 비율(축 0.5 → 0.7)
            { "psg_h13", (SkillGrowthUnit.Multiplier, 0.24f) },   // 조준 사격 — 한 발 ×1.2 × 축 비율(축 5 → 7.5) · 치명 배율은 따로
            { "psg_h14", (SkillGrowthUnit.Seconds, 1f) },       // 도약 강습 — 무적 시간
            { "psg_h17", (SkillGrowthUnit.Seconds, 1f) },       // 사슬 결박 — 결박 시간
            { "psg_h19", (SkillGrowthUnit.Multiplier, 1f) },    // 포탑 전개 — 포탑 한 발 피해
            { "psg_h20", (SkillGrowthUnit.Multiplier, 0.5f) },   // 라인 드라이브 — 타구 ×2.5 × 축 비율(축 5 → 7)
            { "psg_h21", (SkillGrowthUnit.Seconds, 1.2f) },   // 눈보라 — 빙결 1.5초 × 축 비율(축 1.25 → 1.9)
            { "psg_h22", (SkillGrowthUnit.Percent, 2f) },       // 혈연 — 적 하나당 회복(최대 체력 %). 0.02 × 값
            { "psg_h18", (SkillGrowthUnit.Multiplier, 4f) },   // 그림자 베기 — 한 번 ×2 × 축 비율(축 0.5 → 0.8)
            { "psg_h02", (SkillGrowthUnit.Percent, 8f) },   // 전장의 함성 — 쉴드(최대 체력 %). 20% × 축 비율(축 2.5 → 4)
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
