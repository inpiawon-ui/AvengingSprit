using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Game.Character;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 1단계(레벨업 카드 · 상점 · 천사 · 악마 · 액티브 시간 쿨) 기능 테스트 — 영상 없이 값으로 잰다(PD 2026-10-22 「테스트까지만」).
    ///
    /// 실제 판(1챕터 · 코만도 기관총)에 들어가 규칙마다 통과 / 실패 / 미검을 적는다. 결과:
    /// `Projects/AVSR/_exchange/plan_content_expand/step1_check/feature_test.md` + 로그 `[FT]`.
    /// 쓰기: `Game.Editor.Step1FeatureTest.Run()` — 플레이가 꺼져 있으면 켠다. 끝나면 플레이를 끈다.
    /// ⚠ 세이브를 쓴다(행동력 · 판 시작) — 돌리기 전에 세이브를 복사해 두고 끝나면 되돌린다.
    /// </summary>
    public static class Step1FeatureTest
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
        private const string Report = "Projects/AVSR/_exchange/plan_content_expand/step1_check/feature_test.md";
        private const string Pending = "AVSR.Step1FeatureTest";

        private static readonly List<(string area, string item, string verdict, string evidence)> s_rows = new();
        private static BattleDirector s_bd;
        private static RunBuffs s_buffs;
        private static BuffTable s_table;
        private static Type T => typeof(BattleDirector);

        public static void Run()
        {
            SessionState.SetBool(Pending, true);
            if (EditorApplication.isPlaying) Begin();
            else EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnMode;
            EditorApplication.playModeStateChanged += OnMode;
        }

        private static void OnMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) Begin();
        }

        private static void Begin()
        {
            SessionState.EraseBool(Pending);
            int phase = 0, wait = 0;
            float battleAt = 0f;
            double t0 = EditorApplication.timeSinceStartup;
            EditorApplication.CallbackFunction cb = null;
            cb = () =>
            {
                try
                {
                    if (EditorApplication.timeSinceStartup - t0 > 180) { Finish(cb, "시간 초과 — 판에 못 들어갔다"); return; }
                    if (!EditorApplication.isPlaying) return;
                    if (wait > 0) { wait--; return; }
                    if (phase == 0)
                    {
                        if (GameObject.Find("[LoadingView]") != null) { wait = 20; return; }
                        if (Click("SkipButton") || Click("TouchArea") || Click("StartButton")) { wait = 40; return; }
                        // 새 로비(RbLobbyUI)에서 바로 판을 연다 — 옛 로비 · 시나리오 창은 2026-10-24 Assets 밖으로 옮겼다
                        var lobby = UnityEngine.Object.FindAnyObjectByType<Game.Module.Lobby.RbLobbyUI>();
                        if (lobby == null || !lobby.IsReady) { wait = 15; return; }
                        if (!GameFramework.Core.Base.CoreModule.TryGet<Game.User.IPlayerDataService>(out var p) || !p.IsReady) { wait = 30; return; }
                        p.SetStamina(p.StaminaMax);   // 판마다 행동력이 빠진다 — 연달아 돌리면 바닥난다(시험 세이브는 끝나면 되돌린다)
                        lobby.StartRunDirect(1, "commando_mg");
                        phase = 1; wait = 60;
                        return;
                    }
                    var bd = UnityEngine.Object.FindAnyObjectByType<BattleDirector>();
                    if (bd == null) { wait = 10; return; }
                    if (T.GetField("_buffTable", F).GetValue(bd) == null || T.GetField("_host", F).GetValue(bd) == null) { wait = 10; return; }
                    Click("SkipButton");
                    T.GetField("_invuln", F).SetValue(bd, 999f);
                    T.GetField("_exp", F).SetValue(bd, -1000000);
                    if (battleAt == 0f) { battleAt = Time.unscaledTime; return; }
                    if (Time.unscaledTime - battleAt < 4f) return;
                    s_bd = bd;
                    s_buffs = (RunBuffs)T.GetField("_buffs", F).GetValue(bd);
                    s_table = (BuffTable)T.GetField("_buffTable", F).GetValue(bd);
                    s_rows.Clear();
                    Suite();
                    Finish(cb, null);
                }
                catch (Exception e) { Finish(cb, e.ToString()); }
            };
            EditorApplication.update += cb;
        }

        // ── 테스트 ───────────────────────────────────────────────

        private static void Suite()
        {
            Do("카드 표", CardTable);
            Do("최대 레벨 적용", ApplyAll);
            Do("몸 조건", BodyRules);
            Do("테크트리", TechTree);
            Do("뽑기 필터", DrawFilters);
            Do("관통 · 벽 반사 레벨", PierceRicochetLevels);
            Do("탄 규칙", ShotRules);
            Do("둔화 · 흡혈 확률", Chances);
            Do("액티브 시간 쿨", ActiveCooldown);
            Do("위기 방벽", CrisisBarrier);
            Do("보스 사냥", BossHunter);
            Do("필살기 초기화", UltReset);
            Do("상점", Shop);
            Do("천사 제단", Shrine);
            Do("악마 거래", Devil);
        }

        private static readonly Dictionary<string, (CardRarity r, int ch, CardBodyRule body, string req)> Spec = new()
        {
            ["c001"] = (CardRarity.Epic, 1, CardBodyRule.Any, ""), ["c004"] = (CardRarity.Legendary, 6, CardBodyRule.Any, ""),
            ["c005"] = (CardRarity.Epic, 2, CardBodyRule.Any, "c001"), ["c008"] = (CardRarity.Rare, 1, CardBodyRule.Ranged, ""),
            ["c014"] = (CardRarity.Rare, 1, CardBodyRule.Ranged, ""), ["c015"] = (CardRarity.Rare, 3, CardBodyRule.Ranged, ""),
            ["c016"] = (CardRarity.Common, 1, CardBodyRule.Ranged, ""), ["c017"] = (CardRarity.Rare, 2, CardBodyRule.Any, ""),
            ["c018"] = (CardRarity.Rare, 3, CardBodyRule.Any, ""), ["c019"] = (CardRarity.Epic, 3, CardBodyRule.Any, "c009"),
            ["c020"] = (CardRarity.Epic, 5, CardBodyRule.Any, "c009/c017/c018/c021"), ["c026"] = (CardRarity.Epic, 5, CardBodyRule.Blast, ""),
            ["c030"] = (CardRarity.Legendary, 6, CardBodyRule.Any, ""), ["c035"] = (CardRarity.Common, 4, CardBodyRule.Melee, ""),
            ["c036"] = (CardRarity.Rare, 6, CardBodyRule.Any, ""),
        };

        private static void CardTable()
        {
            int n = s_table.Entries.Count, live = s_table.Entries.Count(e => e.Implemented);
            Row("카드 표", "36장 · 켜진 것 35장", n == 36 && live == 35, $"{n}장 · 켜짐 {live}");
            var c002 = s_table.Get("c002");
            Row("카드 표", "성장 가속(c002)은 꺼짐", c002 != null && !c002.Implemented, c002 == null ? "없음" : $"Implemented={c002.Implemented}");
            var bad = new List<string>();
            foreach (var kv in Spec)
            {
                var e = s_table.Get(kv.Key);
                if (e == null) { bad.Add(kv.Key + " 없음"); continue; }
                string req = string.Join("/", e.RequiresAny);
                if (e.Rarity != kv.Value.r || e.FromChapter != kv.Value.ch || e.Body != kv.Value.body || req != kv.Value.req)
                    bad.Add($"{kv.Key}({e.Rarity}·ch{e.FromChapter}·{e.Body}·{req})");
            }
            Row("카드 표", "등급 · 해금 챕터 · 몸 조건 · 앞 카드(15장 표본)", bad.Count == 0, bad.Count == 0 ? "모두 일치" : string.Join(", ", bad));
        }

        private static void ApplyAll()
        {
            var noChange = new List<string>();
            int ok = 0;
            foreach (var e in s_table.Entries.Where(x => x.Implemented))
            {
                Reset(BodyHostFor(e.Body));
                foreach (var pre in e.RequiresAny.Take(1)) Max(pre);
                string before = Snapshot();
                Max(e.BuffKey);
                bool atMax = s_buffs.LevelOf(e.BuffKey) == e.MaxLevel && s_buffs.ExcludedKeys.Contains(e.BuffKey);
                bool changed = Snapshot() != before || e.Kind == BuffKind.UltReset;   // 필살기 초기화는 고르는 순간 효과(따로 잰다)
                if (atMax && changed) ok++; else noChange.Add($"{e.BuffKey}{(atMax ? "" : "(최대 레벨 아님)")}");
            }
            Row("최대 레벨 적용", "35장 모두 최대 레벨 · 뽑기에서 빠짐 · 수치가 바뀜", noChange.Count == 0, $"{ok}/35" + (noChange.Count > 0 ? " · 안 바뀜: " + string.Join(", ", noChange) : ""));
        }

        private static void BodyRules()
        {
            var gun = Host("commando_mg"); var melee = Host("amazon"); var blast = Host("commando_grenade");
            Reset(melee); Max("c014");
            bool offMelee = !s_buffs.Pierce;
            s_buffs.SetHost(gun); bool onGun = s_buffs.Pierce && s_buffs.PierceCount == 3;
            s_buffs.SetHost(melee); bool offAgain = !s_buffs.Pierce;
            Row("몸 조건", "원거리만(관통탄) — 근접 몸이면 꺼지고 원거리로 가면 켜지고 돌아오면 다시 꺼짐", offMelee && onGun && offAgain, $"근접 {B(!offMelee)} → 원거리 {B(onGun)} → 근접 {B(!offAgain)}");
            Reset(gun); Max("c035");
            float rGun = s_buffs.RangeMul; s_buffs.SetHost(melee); float rMelee = s_buffs.RangeMul;
            Row("몸 조건", "근접만(사거리) — 원거리 몸이면 ×1, 근접이면 ×1.3", Mathf.Approximately(rGun, 1f) && rMelee > 1.29f, $"원거리 ×{rGun:0.##} · 근접 ×{rMelee:0.##}");
            Reset(gun); Max("c026");
            float eGun = s_buffs.ExplosiveEchoPercent; s_buffs.SetHost(blast); float eBlast = s_buffs.ExplosiveEchoPercent;
            s_buffs.SetHost(gun); float eBack = s_buffs.ExplosiveEchoPercent;
            Row("몸 조건", "폭발 몸만(폭발 메아리) — 갈아타면 꺼지고 돌아오면 켜짐", eGun == 0f && eBlast > 0f && eBack == 0f, $"기관총 {eGun} · 수류탄 {eBlast} · 기관총 {eBack}");
        }

        private static void TechTree()
        {
            var rule = (ICardRule)s_bd;
            Reset(Host("commando_mg"));
            bool c005Before = s_table.Get("c005").PrerequisiteMet(rule);
            Max("c001"); bool c005After = s_table.Get("c005").PrerequisiteMet(rule);
            Row("테크트리", "감전 → 번개 사슬", !c005Before && c005After, $"감전 전 {B(c005Before)} · 뒤 {B(c005After)}");
            Reset(Host("commando_mg"));
            bool c019Before = s_table.Get("c019").PrerequisiteMet(rule);
            Max("c009"); bool c019After = s_table.Get("c019").PrerequisiteMet(rule);
            Row("테크트리", "화염 각인 → 화상 번짐", !c019Before && c019After, $"전 {B(c019Before)} · 뒤 {B(c019After)}");
            Reset(Host("commando_mg"));
            bool c020Before = s_table.Get("c020").PrerequisiteMet(rule);
            Max("c021"); bool c020After = s_table.Get("c020").PrerequisiteMet(rule);
            Row("테크트리", "각인 · 둔화탄 중 하나 → 상태이상 번짐(둔화탄으로 확인)", !c020Before && c020After, $"전 {B(c020Before)} · 뒤 {B(c020After)}");
        }

        private static void DrawFilters()
        {
            var rng = new System.Random(7);
            var list = new List<BuffEntry>();
            var rule = (ICardRule)s_bd;
            var gun = Host("commando_mg");
            Reset(gun);
            var seen = new HashSet<string>();
            var bad = new List<string>();
            for (int i = 0; i < 400; i++)
            {
                s_table.Draw(list, 3, s_buffs.ExcludedKeys, rng, gun, 1, rule);
                foreach (var e in list)
                {
                    seen.Add(e.BuffKey);
                    if (!e.Implemented) bad.Add(e.BuffKey + " 꺼짐");
                    if (e.FromChapter > 1) bad.Add($"{e.BuffKey} ch{e.FromChapter}");
                    if (!rule.BodyMatches(e.Body, gun)) bad.Add(e.BuffKey + " 몸");
                    if (!e.PrerequisiteMet(rule)) bad.Add(e.BuffKey + " 앞 카드");
                }
            }
            Row("뽑기 필터", "1챕터 · 기관총 400번 — 꺼진 카드 · 해금 전 · 몸 안 맞음 · 앞 카드 없음이 안 나옴", bad.Count == 0, $"나온 카드 {seen.Count}종" + (bad.Count > 0 ? " · 위반 " + string.Join(", ", bad.Distinct().Take(8)) : ""));
            var low = new List<string>();
            for (int i = 0; i < 200; i++)
            {
                s_table.Draw(list, 3, s_buffs.ExcludedKeys, rng, gun, 6, rule, CardRarity.Epic);
                foreach (var e in list) if (e.Rarity < CardRarity.Epic) low.Add(e.BuffKey);
            }
            Row("뽑기 필터", "최소 등급 에픽(6챕터 200번) — 에픽 미만이 안 나옴", low.Count == 0, low.Count == 0 ? "0장" : string.Join(", ", low.Distinct()));
            bool ch6Legend = false;
            for (int i = 0; i < 300 && !ch6Legend; i++)
            {
                s_table.Draw(list, 3, s_buffs.ExcludedKeys, rng, gun, 6, rule);
                ch6Legend = list.Any(e => e.BuffKey == "c004" || e.BuffKey == "c030");
            }
            Row("뽑기 필터", "6챕터에서는 6챕터 카드(처형 · 영혼 복제)가 나옴", ch6Legend, ch6Legend ? "나옴" : "300번 안에 안 나옴");
        }

        private static void PierceRicochetLevels()
        {
            var p = new List<int>(); var b = new List<int>(); var d = new List<int>();
            Reset(Host("commando_mg"));
            for (int i = 0; i < 5; i++) { s_buffs.Apply(s_table.Get("c014")); p.Add(s_buffs.PierceCount); }
            Reset(Host("commando_mg"));
            for (int i = 0; i < 5; i++) { s_buffs.Apply(s_table.Get("c015")); b.Add(s_buffs.Bounces); d.Add(s_buffs.ReturnDamagePercent); }
            Row("관통 · 벽 반사 레벨", "관통 수 1/1/2/2/3(정본)", p.SequenceEqual(new[] { 1, 1, 2, 2, 3 }), string.Join("/", p));
            Row("관통 · 벽 반사 레벨", "튕김 1/1/2/2/3 · 튕긴 탄 피해 50/60/65/75/80%", b.SequenceEqual(new[] { 1, 1, 2, 2, 3 }) && d.SequenceEqual(new[] { 50, 60, 65, 75, 80 }), $"{string.Join("/", b)} · {string.Join("/", d)}%");
        }

        private static void ShotRules()
        {
            var go = new GameObject("FT_Shot");
            try
            {
                var shot = go.AddComponent<Projectile>();
                var apply = T.GetMethod("ApplyCardShotRules", F);
                var bounced = T.GetMethod("BouncedDamage", F);
                typeof(Projectile).GetProperty("HasBounced", F).SetValue(shot, true);
                Reset(Host("commando_mg"));
                apply.Invoke(s_bd, new object[] { shot, true, false });
                int innate = (int)bounced.Invoke(null, new object[] { shot, 100 });
                Row("탄 규칙", "몸이 원래 튕기는 탄(슬러거)은 카드 없이도 피해 그대로(0 이 안 됨)", innate == 100, $"튕긴 피해 {innate}/100");
                Max("c015");
                apply.Invoke(s_bd, new object[] { shot, false, false });
                int card = (int)bounced.Invoke(null, new object[] { shot, 100 });
                Row("탄 규칙", "카드로 튕긴 탄은 카드 값(Lv5 80%)", card == 80, $"{card}/100");
                Reset(Host("commando_mg")); Max("c014");
                apply.Invoke(s_bd, new object[] { shot, false, false });
                int uses = 0; while (uses < 10 && shot.UsePierce()) uses++;
                Row("탄 규칙", "카드 관통(Lv5)은 3명까지 뚫고 멈춤", uses == 3, $"{uses}명");
                typeof(Projectile).GetField("_pierceLeft", F).SetValue(shot, -1);
                apply.Invoke(s_bd, new object[] { shot, false, true });
                int innateUses = 0; while (innateUses < 10 && shot.UsePierce()) innateUses++;
                Row("탄 규칙", "몸이 원래 뚫는 탄(레이저)은 카드가 있어도 끝없이 뚫음", innateUses == 10, $"{innateUses}명(10 = 제한 없음)");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static void Chances()
        {
            var slow = T.GetMethod("SlowChanceOf", F); var leech = T.GetMethod("LeechChanceOf", F);
            int s1 = (int)slow.Invoke(null, new object[] { 0, 52 }); int s2 = (int)slow.Invoke(null, new object[] { 35, 90 });
            int l1 = (int)leech.Invoke(null, new object[] { 0, 30 }); int l0 = (int)leech.Invoke(null, new object[] { 0, 0 });
            Row("둔화 · 흡혈 확률", "카드 레벨 값이 확률로 들어감(둔화 Lv5 52% · 흡혈 Lv5 30%) · 몸 기본 + 카드는 95% 상한 · 없으면 0", s1 == 52 && s2 == 95 && l1 == 30 && l0 == 0, $"둔화 {s1} · 몸+카드 {s2} · 흡혈 {l1} · 없음 {l0}");
        }

        private static void ActiveCooldown()
        {
            Reset(Host("commando_mg"));
            var cd = T.GetProperty("PlayerSkillCooldown", F);
            var tick = T.GetMethod("TickSkillCharge", F);
            float full = (float)cd.GetValue(s_bd);
            T.GetField("_skillCooldown", F).SetValue(s_bd, 0f);
            for (int i = 0; i < 4; i++) tick.Invoke(s_bd, new object[] { 1f });
            float ratio = s_bd.SkillCooldownRatio;
            Row("액티브 시간 쿨", "맞히지 않아도 시간으로 찬다(4초 / 쿨)", Mathf.Abs(ratio - 4f / full) < 0.02f, $"쿨 {full:0.##}초 · 4초 뒤 {ratio:P0}");
            for (int i = 0; i < 40; i++) tick.Invoke(s_bd, new object[] { 1f });
            Row("액티브 시간 쿨", "가득 차면 더 안 넘침", s_bd.SkillCooldownRatio <= 1.0001f, $"{s_bd.SkillCooldownRatio:P0}");
            Max("c036");
            float cut = (float)cd.GetValue(s_bd);
            Row("액티브 시간 쿨", "스킬 쿨 감소 Lv5 → 쿨 ×0.68 · 하한 ×0.4", Mathf.Abs(cut - full * 0.68f) < 0.05f && s_buffs.ActiveCooldownMul >= 0.4f, $"{full:0.##} → {cut:0.##}초 (×{s_buffs.ActiveCooldownMul:0.##})");
        }

        private static void CrisisBarrier()
        {
            var host = (Unit)T.GetField("_host", F).GetValue(s_bd);
            var check = T.GetMethod("CheckCrisisBarrier", F);
            var used = T.GetField("_barrierUsedThisRoom", F); var barrier = T.GetField("_barrier", F);
            int hp = host.Hp;
            Reset(Host("commando_mg")); Max("c033");
            used.SetValue(s_bd, false); barrier.SetValue(s_bd, 0);
            SetHp(host, Mathf.RoundToInt(host.HpMax * 0.27f));
            check.Invoke(s_bd, null);
            int b1 = (int)barrier.GetValue(s_bd);
            used.SetValue(s_bd, false); barrier.SetValue(s_bd, 0);
            T.GetField("_host", F).SetValue(s_bd, null);
            try { check.Invoke(s_bd, null); } finally { T.GetField("_host", F).SetValue(s_bd, host); }
            int b2 = (int)barrier.GetValue(s_bd);
            SetHp(host, hp); used.SetValue(s_bd, false); barrier.SetValue(s_bd, 0);
            Row("위기 방벽", "몸 체력 27%(문턱 30% 아래)에서 방벽이 섬 · 유령일 때는 안 섬", b1 > 0 && b2 == 0, $"몸 {b1} · 유령 {b2}");
        }

        private static void BossHunter()
        {
            Reset(Host("commando_mg")); Max("c025");
            var mul = T.GetMethod("CardDamageMul", F);
            var enemies = (IList)T.GetField("_enemies", F).GetValue(s_bd);
            Unit target = null;
            foreach (var o in enemies) if (o is Unit u && u.IsAlive && !u.IsBoss) { target = u; break; }
            if (target == null) { Row("보스 사냥", "중간보스에게 피해 +42%", null, "방에 적이 없다"); return; }
            var mid = T.GetField("_midBoss", F);
            var oldMid = mid.GetValue(s_bd);
            float plain = (float)mul.Invoke(s_bd, new object[] { target });
            mid.SetValue(s_bd, target);
            float asMid = (float)mul.Invoke(s_bd, new object[] { target });
            mid.SetValue(s_bd, oldMid);
            Row("보스 사냥", "잡몹 ×1 · 중간보스 ×1.42(Lv5)", Mathf.Approximately(plain, 1f) && Mathf.Abs(asMid - 1.42f) < 0.01f, $"잡몹 ×{plain:0.##} · 중간보스 ×{asMid:0.##}");
        }

        private static void UltReset()
        {
            Reset(Host("commando_mg"));
            var ready = T.GetField("_ultReady", F);
            ready.SetValue(s_bd, false);
            T.GetField("_awaitingBuff", F).SetValue(s_bd, true);
            s_bd.ChooseBuff("c013");
            bool r = (bool)ready.GetValue(s_bd);
            Row("필살기 초기화", "다 쓴 필살기가 카드를 고르는 순간 다시 준비됨", r, $"준비={r}");
        }

        private static void Shop()
        {
            Reset(Host("commando_mg"));
            s_buffs.Apply(s_table.Get("c016"));                 // Lv1 — 강화 칸 후보
            for (int i = 0; i < 3; i++) s_buffs.Apply(s_table.Get("c011"));   // Lv3 — 더 높아 강화 대상이 아님
            T.GetMethod("AddRunGold", F, null, new[] { typeof(int) }, null).Invoke(s_bd, new object[] { 1000 });
            T.GetMethod("OpenShop", F).Invoke(s_bd, null);
            var up = T.GetField("_shopUpgrade", F).GetValue(s_bd) as BuffEntry;
            var offers = (List<ShopOffer>)T.GetField("_shopOffers", F).GetValue(s_bd);
            var rules = T.GetField("_shopRules", F).GetValue(s_bd);
            int offerCount = (int)rules.GetType().GetProperty("OfferCount").GetValue(rules);
            Row("상점", "가진 카드 중 가장 낮은 것이 강화 칸 · 새 카드 칸은 하나 줄어듦", up != null && up.BuffKey == "c016" && offers.Count == offerCount - 1 && offers.All(o => o.BuffKey != "c016"),
                $"강화={up?.BuffKey ?? "없음"} · 새 카드 {offers.Count}/{offerCount}");
            int idx = (int)T.GetProperty("ShopUpgradeIndex", F).GetValue(s_bd);
            s_bd.BuyShopItem(idx);
            int lv = s_buffs.LevelOf("c016");
            Row("상점", "강화를 사면 Lv +1 · 창이 닫힘", lv == 2 && !s_bd.IsShopOpen, $"유도탄 Lv{lv} · 열림={s_bd.IsShopOpen}");
            // 진열 필터 — 근접 몸에서 30번 열어 원거리 카드가 안 나오는지
            Reset(Host("amazon"));
            var bad = new List<string>();
            for (int i = 0; i < 30; i++)
            {
                T.GetMethod("OpenShop", F).Invoke(s_bd, null);
                foreach (var o in offers)
                {
                    var c = s_table.Get(o.BuffKey);
                    if (c == null || !c.Implemented || c.FromChapter > 1 || c.Body == CardBodyRule.Ranged || !c.PrerequisiteMet((ICardRule)s_bd)) bad.Add(o.BuffKey);
                }
                s_bd.CloseShop();
            }
            // ⚠ 근접 몸 판정은 실제 몸(_host)을 본다 — 이 시험은 _buffs 의 몸만 바꿨으므로 진열 필터는 지금 몸(기관총)으로 걸린다
            Row("상점", "진열 필터(30번) — 꺼진 · 해금 전 · 앞 카드 없음 카드가 안 나옴", bad.All(k => s_table.Get(k)?.Body == CardBodyRule.Ranged),
                bad.Count == 0 ? "위반 0" : "원거리 카드만 걸림(지금 몸이 기관총이라 정상): " + string.Join(",", bad.Distinct()));
            Reset(Host("commando_mg"));
        }

        private static void Shrine()
        {
            Reset(Host("commando_mg"));
            var gift = T.GetNestedType("ShrineGift", F);
            var apply = T.GetMethod("ApplyShrine", F);
            var offer = (List<BuffEntry>)T.GetField("_offer", F).GetValue(s_bd);
            T.GetField("_awaitingBuff", F).SetValue(s_bd, false);
            apply.Invoke(s_bd, new[] { Enum.Parse(gift, "CardPick") });
            bool awaiting = (bool)T.GetField("_awaitingBuff", F).GetValue(s_bd);
            bool rarePlus = offer.Count > 0 && offer.All(e => e.Rarity >= CardRarity.Rare);
            Row("천사 제단", "카드를 고른다 — 3택1 이 열리고 레어 이상만", awaiting && rarePlus, $"열림={awaiting} · " + string.Join(",", offer.Select(e => $"{e.BuffKey}/{e.Rarity}")));
            T.GetField("_awaitingBuff", F).SetValue(s_bd, false); offer.Clear();
            s_buffs.Apply(s_table.Get("c021")); s_buffs.Apply(s_table.Get("c011")); s_buffs.Apply(s_table.Get("c011"));
            apply.Invoke(s_bd, new[] { Enum.Parse(gift, "CardUpgrade") });
            Row("천사 제단", "카드를 벼린다 — 가진 카드 중 가장 낮은 것 Lv +1", s_buffs.LevelOf("c021") == 2 && s_buffs.LevelOf("c011") == 2, $"둔화탄 Lv{s_buffs.LevelOf("c021")} · 공격 강화 Lv{s_buffs.LevelOf("c011")}");
        }

        private static void Devil()
        {
            var table = (EventTable)T.GetField("_eventTable", F).GetValue(s_bd);
            var deal = table.Entries.FirstOrDefault(e => e.EventId == "EV_DEAL_07");
            Row("악마 거래", "「악마의 진열」 — 최대 체력 20% → 에픽 이상 3택1", deal != null && deal.RewardType == EventReward.CardOffer && deal.RewardRarity == CardRarity.Epic && deal.CostType == EventCost.MaxHp && deal.CostValue == 20,
                deal == null ? "없음" : $"{deal.RewardType}/{deal.RewardRarity} · {deal.CostType} {deal.CostValue}");
            Reset(Host("commando_mg"));
            var offer = (List<BuffEntry>)T.GetField("_offer", F).GetValue(s_bd);
            var low = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                T.GetField("_awaitingBuff", F).SetValue(s_bd, false); offer.Clear();
                T.GetMethod("OfferBuff", F).Invoke(s_bd, new object[] { CardRarity.Epic });
                foreach (var e in offer) if (e.Rarity < CardRarity.Epic) low.Add(e.BuffKey);
            }
            T.GetField("_awaitingBuff", F).SetValue(s_bd, false); offer.Clear();
            Row("악마 거래", "에픽 이상 3택1(20번) — 에픽 미만 안 나옴", low.Count == 0, low.Count == 0 ? "0장" : string.Join(",", low.Distinct()));
        }

        // ── 도우미 ──────────────────────────────────────────────

        private static void Reset(HostEntry host)
        {
            s_buffs.Clear();
            s_buffs.SetHost(host);
        }

        private static void Max(string key)
        {
            var e = s_table.Get(key);
            for (int i = 0; i < e.MaxLevel && s_buffs.LevelOf(key) < e.MaxLevel; i++) s_buffs.Apply(e);
        }

        private static HostEntry Host(string key)
            => GameFramework.Core.Base.CoreModule.Get<Game.User.IPlayerDataService>().GetHost(key);

        private static HostEntry BodyHostFor(CardBodyRule body) => body switch
        {
            CardBodyRule.Melee => Host("amazon"),
            CardBodyRule.Blast => Host("commando_grenade"),
            _ => Host("commando_mg"),
        };

        private static string Snapshot()
        {
            var sb = new StringBuilder();
            foreach (var p in typeof(RunBuffs).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length > 0 || p.Name is "Count" or "ActiveCount" or "CardKinds") continue;
                var ty = p.PropertyType;
                if (ty != typeof(int) && ty != typeof(float) && ty != typeof(bool)) continue;
                sb.Append(p.Name).Append('=').Append(p.GetValue(s_buffs)).Append(' ');
            }
            return sb.ToString();
        }

        private static void SetHp(Unit u, int hp) => typeof(Unit).GetProperty("Hp", F)?.SetValue(u, hp);

        private static string B(bool on) => on ? "켜짐" : "꺼짐";

        private static void Do(string area, Action test)
        {
            try { test(); }
            catch (Exception e) { Row(area, "(테스트 오류)", false, (e.InnerException ?? e).GetType().Name + ": " + (e.InnerException ?? e).Message); }
        }

        private static void Row(string area, string item, bool? pass, string evidence)
            => s_rows.Add((area, item, pass == null ? "미검" : pass.Value ? "통과" : "실패", evidence));

        private static void Finish(EditorApplication.CallbackFunction cb, string error)
        {
            EditorApplication.update -= cb;
            var sb = new StringBuilder();
            int pass = s_rows.Count(r => r.verdict == "통과"), fail = s_rows.Count(r => r.verdict == "실패"), none = s_rows.Count(r => r.verdict == "미검");
            sb.AppendLine("# 1단계 기능 테스트 — 레벨업 카드 · 상점 · 천사 · 악마 · 액티브 시간 쿨");
            sb.AppendLine();
            sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm} · 실제 판(1챕터 · 코만도 기관총)에서 값으로 잼(영상 없음) · `Game.Editor.Step1FeatureTest.Run()`");
            sb.AppendLine();
            sb.AppendLine(error == null ? $"**통과 {pass} · 실패 {fail} · 미검 {none}** (총 {s_rows.Count})" : $"**중단** — {error}");
            sb.AppendLine();
            sb.AppendLine("| 영역 | 항목 | 판정 | 근거 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var r in s_rows) sb.AppendLine($"| {r.area} | {r.item} | {r.verdict} | {r.evidence.Replace("|", "/")} |");
            System.IO.File.WriteAllText(Report, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[FT] 끝 — 통과 {pass} · 실패 {fail} · 미검 {none} → {Report}" + (error != null ? "\n" + error : ""));
            foreach (var r in s_rows.Where(r => r.verdict != "통과")) Debug.Log($"[FT] {r.verdict} {r.area} · {r.item} · {r.evidence}");
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        private static bool Click(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) return false;
            var b = go.GetComponent<UnityEngine.UI.Button>();
            if (b == null || !b.isActiveAndEnabled) return false;
            b.onClick.Invoke();
            return true;
        }
    }
}
