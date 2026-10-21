using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 호스트 스킬 개편(2026-10-14) 녹화 도우미 — 플레이 중에 부른다.
    /// 1챕터에 그 호스트로 들어가 적을 8명 더 세우고, 몸을 방 가운데쪽으로 옮긴 뒤 스킬을 쓰며
    /// `SkillFxRecorder.RecordClip` 으로 찍는다. 시험 중 무적 · 쿨 무시. 끝나면 플레이를 멈춘다.
    /// 쓰기: 플레이 시작 → `SkillReworkRecorder.Run("thug", "rw_thug2", 4.2f)`
    /// </summary>
    public static class SkillReworkRecorder
    {
        // ── 일괄 녹화 (2026-10-16 「캐릭터별 액티브 · 필살기 다 플레이해서 영상으로」) ──────────
        //
        // 녹화 하나마다 플레이를 껐다 켜야 한다(도메인 리로드로 콜백이 날아간다). 줄을 SessionState 에 두고,
        // 플레이에 들어갈 때마다 한 줄씩 꺼내 돌린다 — 끝나면 `Run` 이 플레이를 끄고, 여기서 다시 켠다.
        // 줄 형식: 호스트|클립|녹화초|필살기초|액티브초|갈아입을몸  (줄끼리는 ;)
        private const string QueueKey = "AVSR.RecQueue";
        private const string LastMark = "#LAST";

        public static void Batch(string spec)
        {
            SessionState.SetString(QueueKey, spec ?? string.Empty);
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }

        public static void CancelBatch() => SessionState.EraseString(QueueKey);

        [InitializeOnLoadMethod]
        private static void HookBatch()
        {
            EditorApplication.playModeStateChanged -= OnBatchMode;
            EditorApplication.playModeStateChanged += OnBatchMode;
        }

        private static void OnBatchMode(PlayModeStateChange state)
        {
            var q = SessionState.GetString(QueueKey, string.Empty);
            if (string.IsNullOrEmpty(q)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                int cut = q.IndexOf(';');
                string head = cut < 0 ? q : q.Substring(0, cut);
                string rest = cut < 0 ? string.Empty : q.Substring(cut + 1);
                SessionState.SetString(QueueKey, rest);
                if (string.IsNullOrEmpty(rest)) SessionState.SetString(QueueKey, LastMark);   // 마지막 줄 — 끝나고 다시 켜지 않게 표시
                var f = head.Split('|');
                if (f.Length < 5 || f[0] == LastMark) { SessionState.EraseString(QueueKey); return; }
                float.TryParse(f[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sec);
                float.TryParse(f[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ult);
                float.TryParse(f[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var act);
                string swap = f.Length > 5 && f[5].Length > 0 ? f[5] : null;
                bool solo = f.Length > 6 && f[6] == "solo";
                Debug.Log("[RW] batch " + f[1] + " (남은 줄 " + (string.IsNullOrEmpty(rest) ? 0 : rest.Split(';').Length) + ")");
                Run(f[0], f[1], sec, 3f, ult, act, swap, solo: solo);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (q == LastMark) { SessionState.EraseString(QueueKey); Debug.Log("[RW] batch done"); return; }
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
            }
        }

        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        private static readonly Vector2[] Offsets =
        {
            new(-3f, 2.5f), new(3f, 3f), new(-2f, 4.5f), new(2.5f, 4f), new(0f, 3.5f),
            new(1.2f, 6.5f), new(-3.5f, 5.5f), new(3.5f, 1.2f),
        };

        /// <param name="ultAt">녹화 시작 뒤 이 초에 필살기를 누른다(0 이하면 안 누름). 액티브는 0.5초에 누른다.</param>
        /// <param name="activeAt">액티브를 누를 초(0 이하면 안 누름).</param>
        public static void Run(string hostKey, string clip, float recordSeconds, float liftMeters = 3f,
                               float ultAt = -1f, float activeAt = 0.5f, string swapTo = null,
                               string[] enemyHosts = null, float perEnemy = 2.6f, bool trialCheck = false,
                               bool solo = false)
        {
            // 필살기 1회 체험 확인 — swapTo 몸을 못 가진 몸으로 친다. 필살기를 쓰고, 다시 빙의해도 잠긴 채인지 본다
            Game.Module.InGame.BattleDirector.EditorUnownedHost = trialCheck ? swapTo : null;
            bool reEntered = false;
            int phase = 0, wait = 0;
            double t0 = EditorApplication.timeSinceStartup, recAt = 0;
            float battleAt = 0f;
            bool cast = false, ulted = false;
            int enemyIndex = 0;
            Game.Module.InGame.Unit enemyHost = null;
            var bt = typeof(Game.Module.InGame.BattleDirector);
            EditorApplication.CallbackFunction cb = null;
            cb = () =>
            {
                try
                {
                    double now = EditorApplication.timeSinceStartup;
                    if (now - t0 > 260)
                    {
                        EditorApplication.update -= cb;
                        Debug.Log("[RW] timeout " + phase);
                        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;   // 일괄 녹화가 멈추지 않게 다음으로 넘긴다
                        return;
                    }
                    if (!EditorApplication.isPlaying) return;
                    if (wait > 0) { wait--; return; }
                    if (phase == 0)
                    {
                        if (GameObject.Find("[LoadingView]") != null) { wait = 20; return; }
                        if (Click("SkipButton") || Click("TouchArea")) { wait = 40; return; }
                        var lobby = UnityEngine.Object.FindAnyObjectByType<Game.Module.Lobby.LobbyMainUI>();
                        if (lobby == null) { wait = 15; return; }
                        if (!GameFramework.Core.Base.CoreModule.TryGet<Game.User.IPlayerDataService>(out var p) || !p.IsReady)
                        { wait = 30; return; }
                        var panel = lobby.GetComponentInChildren<Game.Module.Lobby.ChapterHostPanel>(true);
                        if (!panel.IsOpen)
                        {
                            typeof(Game.Module.Lobby.LobbyMainUI).GetMethod("PlayScenario", F).Invoke(lobby, null);
                            wait = 60;
                            return;
                        }
                        var pt = typeof(Game.Module.Lobby.ChapterHostPanel);
                        pt.GetMethod("Step", F).Invoke(panel, new object[] { -9 });
                        pt.GetMethod("PickHost", F).Invoke(panel, new object[] { hostKey });
                        var extra = new System.Collections.Generic.List<string>();
                        if (swapTo != null) extra.Add(p.GetHost(swapTo)?.SpriteKey);
                        if (enemyHosts != null) foreach (var k in enemyHosts) extra.Add(p.GetHost(k)?.SpriteKey);
                        Game.Module.InGame.BattleDirector.EditorExtraUnitKey = string.Join(",", extra);
                        // 판마다 행동력이 빠진다(2026-10-16~) — 녹화를 연달아 돌리면 바닥나 판이 안 열린다. 시험 세이브는 끝나면 되돌린다
                        p.SetStamina(p.StaminaMax);
                        pt.GetMethod("OnStart", F).Invoke(panel, null);
                        phase = 1;
                        wait = 60;
                        return;
                    }
                    var bd = UnityEngine.Object.FindAnyObjectByType<Game.Module.InGame.BattleDirector>();
                    if (bd == null) { wait = 10; return; }
                    bt.GetField("_invuln", F).SetValue(bd, 999f);
                    // 녹화 중 레벨업 창이 화면을 덮는다(녹화 ssr_md1) — 경험치를 바닥에 붙여 둔다
                    bt.GetField("_exp", F).SetValue(bd, -1000000);
                    // 방을 비우면 레벨업 카드가 한 번 뜬다(스테이지 레벨업 2026-10-16) — 녹화 중엔 이미 준 것으로 둔다
                    bt.GetField("_stageLevelGiven", F)?.SetValue(bd, true);
                    if (phase == 1)
                    {
                        if (Click("SkipButton")) { wait = 20; return; }
                        if (battleAt == 0f) battleAt = Time.unscaledTime;
                        if (Time.unscaledTime - battleAt < 6f) return;
                        var enemies = (IList)bt.GetField("_enemies", F).GetValue(bd);
                        var host = (Game.Module.InGame.Unit)bt.GetField("_host", F).GetValue(bd);
                        Game.Module.InGame.Unit melee = null, gun = null;
                        int alive = 0;
                        foreach (var o in enemies)
                        {
                            var u = o as Game.Module.InGame.Unit;
                            if (u == null || !u.IsAlive || u.IsBoss) continue;
                            alive++;
                            if (u.Profile == null) continue;
                            if (u.Profile.Kind == Game.Character.AttackKind.Melee) { if (melee == null || !u.IsHostBody) melee = u; }
                            else if (gun == null || !u.IsHostBody) gun = u;
                        }
                        if ((alive == 0 || host == null) && Time.unscaledTime - battleAt < 20f) return;
                        var src = melee ?? gun;
                        float m = (float)bt.GetMethod("Meters", F).Invoke(bd, new object[] { 1f });
                        // 입구(아래 벽)에 서 있으면 화면 아래가 기계 그림이라 연출이 잘린다 — 방 안쪽으로 올린다
                        host.Position += new Vector2(0f, liftMeters * m);
                        var spawn = bt.GetMethod("SpawnRoomUnit", F);
                        // 적 호스트 예고 녹화면 잡몹을 더 세우지 않는다 — 바닥 예고가 잡몹에 가린다
                        for (int i = 0; i < Offsets.Length && src != null && enemyHosts == null && !solo; i++, alive++)
                        {
                            // 근접만 더 세운다 — 1챕터 원거리는 수류탄병이라 더 세우면 수류탄 폭발이 화면을 덮었다(녹화 rw_sl_1 · rw_sl_2)
                            var pick = src;
                            var u = spawn.Invoke(bd, new object[] { pick.Profile, false, false, host.Position + Offsets[i] * m, "RwTest" + i })
                                as Game.Module.InGame.Unit;
                            // 시험판은 숙련도 10(×1.84)이라 1챕터 잡몹이 한 대에 죽는다 — 연출(끌려오기 · 날아가기 · 3중첩)이 보이게 체력만 늘린다
                            if (u != null) SetHp(u, u.HpMax * EnemyHpMul);
                        }
                        // 로비에서 못 고르는 몸(사신 · 아마존 정예 — 숨김)은 판 안에서 갈아입힌다
                        if (swapTo != null && host.Key != swapTo)
                        {
                            var pd = GameFramework.Core.Base.CoreModule.Get<Game.User.IPlayerDataService>();
                            var entry = pd.GetHost(swapTo);
                            var oldHost = host;
                            bt.GetMethod("EnterHost", F).Invoke(bd, new object[] { entry, swapTo, swapTo, host.Position, 100 });
                            host = (Game.Module.InGame.Unit)bt.GetField("_host", F).GetValue(bd);
                            if (oldHost != host && oldHost != null) oldHost.gameObject.SetActive(false);
                        }
                        bt.GetField("_skillCooldown", F).SetValue(bd, 999f);
                        Debug.Log($"[RW] {hostKey} enemies {alive} melee {melee?.Key} gun {gun?.Key}");
                        // 방에 해골이 수십 마리 서는 판이 있다(녹화 r_gs1) — 가까운 14명만 남겨 연출이 읽히게
                        {
                            var all = new System.Collections.Generic.List<Game.Module.InGame.Unit>();
                            foreach (var o in enemies)
                            {
                                var u = o as Game.Module.InGame.Unit;
                                if (u != null && u.IsAlive && !u.IsBoss) all.Add(u);
                            }
                            var hp = host.Position;
                            all.Sort((x, y) => (x.Position - hp).sqrMagnitude.CompareTo((y.Position - hp).sqrMagnitude));
                            var kill = bt.GetMethod("KillEnemy", F);
                            // 한 명 판(다단 히트 확인, 2026-10-16) — 가장 가까운 하나만 앞에 세우고 맷집을 크게
                            int keep = solo ? 1 : MaxEnemies;
                            for (int i = keep; i < all.Count; i++) kill.Invoke(bd, new object[] { all[i] });
                            if (solo && all.Count > 0)
                            {
                                all[0].Position = hp + new Vector2(0f, 2.6f * m);
                                SetHp(all[0], all[0].HpMax * SoloHpMul);
                            }
                        }
                        SkillFxRecorder.RecordClip(clip, recordSeconds);
                        recAt = now;
                        phase = 2;
                        return;
                    }
                    // 적 호스트를 하나씩 세워 액티브를 쓰게 한다(바닥 예고 확인 — BattleDirector.EnemySkillTell)
                    if (enemyHosts != null && enemyIndex < enemyHosts.Length && now - recAt > 0.3 + enemyIndex * perEnemy)
                    {
                        var host = (Game.Module.InGame.Unit)bt.GetField("_host", F).GetValue(bd);
                        if (enemyHost != null && enemyHost.IsAlive) bt.GetMethod("KillEnemy", F).Invoke(bd, new object[] { enemyHost });
                        var pd = GameFramework.Core.Base.CoreModule.Get<Game.User.IPlayerDataService>();
                        var entry = pd.GetHost(enemyHosts[enemyIndex]);
                        float m = (float)bt.GetMethod("Meters", F).Invoke(bd, new object[] { 1f });
                        var at = host.Position + new Vector2(enemyIndex % 2 == 0 ? -1.2f : 1.2f, 3.4f) * m;
                        enemyHost = bt.GetMethod("SpawnRoomUnit", F).Invoke(bd, new object[] { entry, true, false, at, "RwHost" + enemyIndex })
                            as Game.Module.InGame.Unit;
                        if (enemyHost != null) SetHp(enemyHost, enemyHost.HpMax * 20);
                        bt.GetField("_enemySkillSeen", F).SetValue(bd, false);
                        bt.GetField("_enemySkillClock", F).SetValue(bd, 7.6f);
                        enemyIndex++;
                    }
                    // 체험 확인: 필살기를 쓴 뒤 같은 몸으로 다시 빙의해 본다 — 자물쇠 · 「잠김」이 떠야 한다
                    if (trialCheck && ulted && !reEntered && now - recAt > ultAt + 3.0)
                    {
                        reEntered = true;
                        var pd = GameFramework.Core.Base.CoreModule.Get<Game.User.IPlayerDataService>();
                        var host = (Game.Module.InGame.Unit)bt.GetField("_host", F).GetValue(bd);
                        bt.GetMethod("EnterHost", F).Invoke(bd, new object[] { pd.GetHost(swapTo), swapTo, swapTo, host.Position, 100 });
                        if (host != null && host != (Game.Module.InGame.Unit)bt.GetField("_host", F).GetValue(bd)) host.gameObject.SetActive(false);
                        Debug.Log("[RW] trial re-enter ready=" + bt.GetField("_ultReady", F).GetValue(bd)
                                  + " spent=" + bt.GetProperty("IsUltTrialSpent", F).GetValue(bd));
                    }
                    if (trialCheck && reEntered && !cast && now - recAt > ultAt + 4.0)
                    {
                        cast = true;   // 잠긴 필살기를 눌러 본다 — 알림이 떠야 한다
                        bt.GetMethod("TryUltimate", F).Invoke(bd, null);
                    }
                    if (!ulted && ultAt > 0f && now - recAt > ultAt)
                    {
                        ulted = true;
                        bt.GetMethod("TryUltimate", F).Invoke(bd, null);
                    }
                    if (!cast && activeAt > 0f && now - recAt > activeAt)
                    {
                        cast = true;
                        bt.GetField("_skillCooldown", F).SetValue(bd, 999f);
                        bt.GetMethod("TryActiveSkill", F).Invoke(bd, null);
                    }
                    if (now - recAt > recordSeconds + 2.0)
                    {
                        EditorApplication.update -= cb;
                        Game.Module.InGame.BattleDirector.EditorUnownedHost = null;
                        Debug.Log("[RW] done " + clip);
                        EditorApplication.isPlaying = false;
                    }
                }
                catch (Exception e)
                {
                    EditorApplication.update -= cb;
                    Debug.LogError("[RW] " + e);
                }
            };
            EditorApplication.update += cb;
        }

        private const int EnemyHpMul = 4;
        private const int MaxEnemies = 14;
        private const int SoloHpMul = 60;

        private static void SetHp(Game.Module.InGame.Unit u, int hp)
        {
            var t = typeof(Game.Module.InGame.Unit);
            t.GetProperty("HpMax", F)?.SetValue(u, hp);
            t.GetProperty("Hp", F)?.SetValue(u, hp);
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
