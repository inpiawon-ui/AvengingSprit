using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Character;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;
using T = Game.Character.TrashTrait;

namespace Game.Editor
{
    /// <summary>
    /// 2단계(몬스터 조합) 기능 테스트 — 표 · 챕터 배정 · 성질 대응을 값으로 잰다. 플레이 없이 돈다.
    /// 화면에서 하는 짓(예고 · 빈틈 · 원소)은 `MonsterCheckRecorder` 녹화가 맡는다.
    ///
    /// 결과: `Projects/AVSR/_exchange/plan_content_expand/step2_check/feature_test.md` + 로그 `[FT2]`.
    /// 쓰기: `Game.Editor.Step2FeatureTest.Run()`
    /// </summary>
    public static class Step2FeatureTest
    {
        private const string Report = "Projects/AVSR/_exchange/plan_content_expand/step2_check/feature_test.md";
        private const string TablePath = "Assets/BundleResource/TableData/TrashTable.asset";
        private const string ConfigPath = "Assets/BundleResource/TableData/GameConfig.asset";

        private static readonly List<(string area, string item, string verdict, string evidence)> s_rows = new();

        // 예전 챕터 단계 행동이 그대로 이름 붙은 종이 됐는지 — 키 → 꼭 가져야 할 성질
        private static readonly (string key, T traits)[] StageMap =
        {
            ("bat", T.Dive), ("bat_2", T.Dive | T.Weave),
            ("skeleton_2", T.DeathBurst4), ("skeleton_3", T.Ambush | T.DeathBurst4),
            ("skeleton_4", T.Ambush | T.DeathBurst8), ("skeleton_mage", T.Ring),
            ("actor_enforcer_2", T.Slam), ("actor_enforcer_3", T.Slam | T.GorillaMix),
            ("actor_enforcer_4", T.SplitBats), ("gorilla_charger", T.GorillaMix | T.ChargeFirst),
            ("scrapgunner_2", T.GunnerBurst), ("scrapgunner_3", T.GunnerBurst | T.Burst4),
            ("scrapgunner_sniper", T.Snipe), ("roadwarden_2", T.WardenBurst), ("roadwarden_3", T.Spiral),
            ("coilwalker_2", T.BounceShot), ("coilwalker_3", T.SplitShot), ("coil_mortar", T.Mortar),
            ("obj_turret_2", T.CrossAlt), ("obj_turret_3", T.CrossStream), ("boar", T.Dive), ("boar_2", T.BoarChain),
        };

        private static readonly (string key, T trait)[] NewLv2 =
        {
            ("mole_2", T.MoleDouble), ("mantis_2", T.MantisDouble), ("armadillo_2", T.ArmadilloRoll),
            ("mushroom_2", T.MushroomPool), ("mummy_2", T.MummyDouble), ("spider_2", T.SpiderPool),
            ("bat_bounce", T.BatBounce),
        };

        private static readonly string[] NewBodies = { "watcher_eye", "shadow_cat", "saw_monkey", "mimic", "lantern" };

        public static void Run()
        {
            s_rows.Clear();
            var table = AssetDatabase.LoadAssetAtPath<TrashTable>(TablePath);
            if (table == null) { Add("표", "몬스터 표", "실패", TablePath + " 없음"); Write(); return; }

            // ── 표 ──
            var keys = new HashSet<string>();
            var dup = new List<string>();
            foreach (var d in table.Entries) if (!keys.Add(d.Key)) dup.Add(d.Key);
            Add("표", "몬스터 수 · 키 겹침", table.Entries.Length >= 52 && dup.Count == 0 ? "통과" : "실패",
                $"{table.Entries.Length}종 · 겹침 {dup.Count}");
            Add("표", "Addressable 주소", AddressOk(TablePath, "TableData/TrashTable") ? "통과" : "실패", "TableData/TrashTable");

            var noKind = new List<string>();
            var bodies = new HashSet<string>();
            foreach (var d in table.Entries)
            {
                bodies.Add(d.Body);
                if (AffinityRule.KindOf(d.Body) == Affinity.None) noKind.Add(d.Key);
            }
            Add("표", "몸마다 쪽(파워 · 무기 · 마법)", noKind.Count == 0 ? "통과" : "실패",
                noKind.Count == 0 ? $"몸 {bodies.Count}개 모두" : string.Join(", ", noKind));

            // ── 그림 ──
            var noArt = new List<string>();
            var waiting = new List<string>();
            foreach (var b in bodies)
            {
                bool has = AssetDatabase.IsValidFolder($"Assets/BaseResource/Unit/{b}") || b == "obj_turret";
                if (has) continue;
                if (System.Array.IndexOf(NewBodies, b) >= 0) waiting.Add(b); else noArt.Add(b);
            }
            Add("그림", "있던 몸의 그림", noArt.Count == 0 ? "통과" : "실패", noArt.Count == 0 ? "모두 있음" : string.Join(", ", noArt));
            Add("그림", "새 모양 5종 그림", waiting.Count == 0 ? "통과" : "미검",
                waiting.Count == 0 ? "5종 모두" : "발주 중 — " + string.Join(", ", waiting));
            var elemArt = new List<string>();
            foreach (var d in table.Entries)
                if (d.Element != TrashElement.None && d.Sprite == d.Body) elemArt.Add(d.Key);
            Add("그림", "원소판 제 그림", elemArt.Count == 0 ? "통과" : "미검",
                elemArt.Count == 0 ? "6종 모두" : "발주 중(몸 그림으로 섬) — " + string.Join(", ", elemArt));

            // ── 성질 ──
            var bad = new List<string>();
            foreach (var (key, traits) in StageMap)
            {
                var d = table.Get(key);
                if (d == null || (d.Traits & traits) != traits) bad.Add(key);
            }
            Add("성질", "예전 단계 행동 → 변종 22종", bad.Count == 0 ? "통과" : "실패", bad.Count == 0 ? "모두 맞음" : string.Join(", ", bad));
            bad.Clear();
            foreach (var (key, trait) in NewLv2)
            {
                var d = table.Get(key);
                if (d == null || !d.Has(trait) || d.Level < 2) bad.Add(key);
            }
            Add("성질", "새 Lv2 7종", bad.Count == 0 ? "통과" : "실패", bad.Count == 0 ? "모두 맞음" : string.Join(", ", bad));
            bad.Clear();
            foreach (var d in table.Entries)
                if (d.Level > 1)
                {
                    var lv1 = table.Get(d.Body);
                    if (lv1 != null && d.Hp <= lv1.Hp) bad.Add(d.Key);
                }
            Add("성질", "Lv 가 오르면 체력도 오름", bad.Count == 0 ? "통과" : "실패", bad.Count == 0 ? "Lv2 이상 모두" : string.Join(", ", bad));

            // ── 챕터 배정 ──
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null) Add("챕터", "챕터 표", "실패", ConfigPath + " 없음");
            else
            {
                var missing = new List<string>();
                int used = 0;
                var seen = new HashSet<string>();
                for (int ch = 1; ch <= cfg.ChapterCount; ch++)
                    foreach (var k0 in cfg.ChapterOf(ch).Trash)
                    {
                        var k = k0.Trim() == "turret_cross" ? "obj_turret" : k0.Trim();
                        if (table.Get(k) == null) missing.Add($"CH{ch}:{k}");
                        else if (seen.Add(k)) used++;
                    }
                Add("챕터", "챕터 표의 몬스터 키가 표에 있음", missing.Count == 0 ? "통과" : "실패",
                    missing.Count == 0 ? $"10챕터 · 서로 다른 몬스터 {used}종" : string.Join(", ", missing));
                // 7~10 챕터가 예전처럼 같은 단계로 싸우지 않는지 — 챕터마다 목록이 달라야 한다
                var lists = new HashSet<string>();
                for (int ch = 7; ch <= Mathf.Min(10, cfg.ChapterCount); ch++) lists.Add(string.Join(",", cfg.ChapterOf(ch).Trash));
                Add("챕터", "7~10챕터 구성이 서로 다름", lists.Count == 4 ? "통과" : "실패", $"서로 다른 목록 {lists.Count}/4");
            }

            Write();
        }

        private static bool AddressOk(string path, string address)
        {
            var s = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var e = s != null ? s.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)) : null;
            return e != null && e.address == address;
        }

        private static void Add(string area, string item, string verdict, string evidence)
        {
            s_rows.Add((area, item, verdict, evidence));
            Debug.Log($"[FT2] {verdict} · {area} · {item} — {evidence}");
        }

        private static void Write()
        {
            int ok = 0, fail = 0, unk = 0;
            var sb = new StringBuilder();
            foreach (var r in s_rows)
            {
                if (r.verdict == "통과") ok++; else if (r.verdict == "실패") fail++; else unk++;
                sb.Append($"| {r.area} | {r.item} | {r.verdict} | {r.evidence} |\n");
            }
            var head = "# 2단계 몬스터 조합 — 기능 테스트\n\n"
                     + $"통과 {ok} · 실패 {fail} · 미검 {unk} — `Game.Editor.Step2FeatureTest.Run()`\n\n"
                     + "| 묶음 | 항목 | 결과 | 근거 |\n|---|---|---|---|\n";
            var root = Path.GetDirectoryName(Application.dataPath);
            var path = Path.Combine(root, Report);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, head + sb, new UTF8Encoding(false));
            Debug.Log($"[FT2] 끝 — 통과 {ok} · 실패 {fail} · 미검 {unk} → {Report}");
        }
    }
}
