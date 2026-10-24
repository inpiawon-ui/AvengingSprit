using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 몬스터 검증 녹화(2단계 몬스터 조합, PD 2026-10-22) — 몬스터 한 종 = 영상 한 개.
    ///
    /// 1챕터 판 **하나**에 들어가 몬스터를 차례로 바꿔 세운다(판을 종마다 새로 열면 한 종에 2분이 넘게 걸렸다).
    /// 한 종마다 방의 적을 모두 치우고 그 몬스터만 내 앞에 세워 10초를 찍는다. 몬스터는 맷집을 크게 잡아 안 죽는다.
    /// 내 몸은 좌우로 걸어 예고 · 빈틈을 끌어낸다. 끝나면 `[MON]` 줄에 성질 · 원소 · 내 몸이 받은 피해 · 원소 상태를 남긴다.
    ///
    /// 쓰기: `MonsterCheckRecorder.Batch("skeleton_2|die;bat_bounce|")` — 줄 = 몬스터 키|조건(쉼표), 줄끼리 `;`
    /// 조건: still(걷지 않고 서 있음) · near(2 m 앞에 세움) · die(6초에 잡아 죽을 때 하는 짓을 본다)
    ///       ghost(몸을 버려 유령으로 맞선다)
    /// 내 몸은 무적을 끄고 체력을 크게 잡는다 — 원소 · 피해가 실제로 들어가는지 본다.
    /// 영상: `Projects/AVSR/_exchange/skillfx_now/clip_mon_{키}.mp4`
    /// </summary>
    public static class MonsterCheckRecorder
    {
        private const string QueueKey = "AVSR.MonRecQueue";
        private const string HostKey = "gangster";
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        private const float RecordSeconds = 10f;
        private const float GapSeconds = 1.5f;
        private const int MonsterHpMul = 40;
        private const float DieAt = 6f;

        public static void Batch(string spec)
        {
            SessionState.SetString(QueueKey, spec ?? string.Empty);
            if (EditorApplication.isPlaying) Run(spec);
            else EditorApplication.isPlaying = true;
        }

        public static void CancelBatch() => SessionState.EraseString(QueueKey);

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnMode;
            EditorApplication.playModeStateChanged += OnMode;
        }

        private static void OnMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            var q = SessionState.GetString(QueueKey, string.Empty);
            if (!string.IsNullOrEmpty(q)) Run(q);
        }

        private static void Run(string spec)
        {
            SessionState.EraseString(QueueKey);
            var rows = new List<string[]>();
            foreach (var r in spec.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) rows.Add(r.Split('|'));
            Debug.Log($"[MON] 시작 — {rows.Count}종");

            int phase = 0, wait = 0, row = -1;
            double t0 = EditorApplication.timeSinceStartup, recAt = 0;
            float battleAt = 0f, moveClock = 0f;
            int hpBefore = 0, warnsSeen = 0, zonesSeen = 0;
            bool burnSeen = false, poisonSeen = false, chillSeen = false, shieldSeen = false;
            string key = null;
            HashSet<string> flags = null;
            Game.Module.InGame.Unit mon = null;
            Game.Character.TrashDef def = null;
            var bt = typeof(Game.Module.InGame.BattleDirector);
            EditorApplication.CallbackFunction cb = null;
            cb = () =>
            {
                try
                {
                    double now = EditorApplication.timeSinceStartup;
                    if (now - t0 > 120 + rows.Count * (RecordSeconds + GapSeconds + 4))
                    { Stop(cb, "시간 초과 — 줄 " + row); return; }
                    if (!EditorApplication.isPlaying) { EditorApplication.update -= cb; return; }
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
                        lobby.StartRunDirect(1, HostKey);
                        phase = 1;
                        wait = 60;
                        return;
                    }

                    var bd = UnityEngine.Object.FindAnyObjectByType<Game.Module.InGame.BattleDirector>();
                    if (bd == null) { wait = 10; return; }
                    var host = (Game.Module.InGame.Unit)bt.GetField("_host", F).GetValue(bd);
                    var ghost = (Game.Module.InGame.Unit)bt.GetField("_ghost", F).GetValue(bd);
                    bt.GetField("_exp", F).SetValue(bd, -1000000);
                    bt.GetField("_stageLevelGiven", F)?.SetValue(bd, true);
                    if (bt.GetField("_trashTable", F).GetValue(bd) == null) { wait = 10; return; }
                    var enemies = (IList)bt.GetField("_enemies", F).GetValue(bd);

                    if (phase == 1)   // 판이 막 열렸다 — 6초 기다린다(입장 연출 · 표 읽기)
                    {
                        if (Click("SkipButton")) { wait = 20; return; }
                        if (battleAt == 0f) battleAt = Time.unscaledTime;
                        if (Time.unscaledTime - battleAt < 6f || host == null) return;
                        phase = 2;
                        return;
                    }

                    if (phase == 2)   // 다음 몬스터를 세운다
                    {
                        row++;
                        if (row >= rows.Count) { Stop(cb, null); return; }
                        key = rows[row][0];
                        flags = new HashSet<string>((rows[row].Length > 1 ? rows[row][1] : string.Empty)
                                                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                        bt.GetField("_invuln", F).SetValue(bd, 999f);
                        ClearRoom(bd, enemies);
                        // 앞 줄의 원소 상태 · 자국을 거둔다 — 남으면 다음 몬스터 줄에 「독」이 찍힌다
                        bt.GetMethod("ClearElements", F).Invoke(bd, null);
                        // 내 몸을 방 가운데 아래쪽으로 — 입구(맨 아래 난간 뒤)에 서면 싸움이 난간에 가려 찍힌다
                        var room = (Vector2)bt.GetField("_roomSize", F).GetValue(bd);
                        var self = host ?? ghost;
                        // 가운데는 1챕터 첫 방의 모래주머니 옆이라 근접 몹이 거기 걸린다 — 왼쪽 빈 바닥에 선다
                        if (self != null) self.Position = new Vector2(room.x * 0.25f, -room.y * 0.62f);
                        var entry = (Game.Character.HostEntry)bt.GetMethod("TrashEntryOf", F, null, new[] { typeof(string) }, null)
                                                              .Invoke(bd, new object[] { key });
                        if (entry == null) { Debug.LogError("[MON] 몬스터 없음: " + key); return; }
                        var me = host ?? ghost;
                        float m = Meters(bd);
                        var at = me.Position + new Vector2(0f, (flags.Contains("near") ? 2f : 4f) * m);
                        mon = bt.GetMethod("SpawnRoomUnit", F).Invoke(bd, new object[] { entry, false, false, at, "MonTest_" + key })
                              as Game.Module.InGame.Unit;
                        if (mon == null) { Debug.LogError("[MON] 세우기 실패: " + key); return; }
                        def = bt.GetMethod("TrashDefOf", F, null, new[] { typeof(Game.Module.InGame.Unit) }, null)
                                .Invoke(bd, new object[] { mon }) as Game.Character.TrashDef;
                        SetHp(mon, mon.HpMax * MonsterHpMul);
                        mon.IsAggro = true;
                        // 수호 등불은 지킬 몸이 있어야 한다 — 해골 하나를 곁에 세운다
                        if (key == "lantern")
                            bt.GetMethod("SpawnRoomUnit", F).Invoke(bd, new object[]
                            {
                                bt.GetMethod("TrashEntryOf", F, null, new[] { typeof(string) }, null).Invoke(bd, new object[] { "skeleton" }),
                                false, false, at + new Vector2(1.5f * m, -1f * m), "MonTest_ally",
                            });
                        if (flags.Contains("ghost") && host != null) bt.GetMethod("LoseHost", F).Invoke(bd, null);
                        bt.GetField("_invuln", F).SetValue(bd, 0f);
                        host = (Game.Module.InGame.Unit)bt.GetField("_host", F).GetValue(bd);
                        if (host != null) SetHp(host, 5000);
                        hpBefore = host != null ? host.Hp : 0;
                        warnsSeen = zonesSeen = 0;
                        burnSeen = poisonSeen = chillSeen = shieldSeen = false;
                        moveClock = 0f;
                        SkillFxRecorder.RecordClip("mon_" + key, RecordSeconds);
                        recAt = now;
                        phase = 3;
                        return;
                    }

                    double t = now - recAt;
                    if (phase == 3)
                    {
                        if (flags.Contains("die") && t > DieAt && mon != null && mon.IsAlive && !mon.IsDying)
                            bt.GetMethod("KillEnemy", F).Invoke(bd, new object[] { mon });
                        if (!flags.Contains("still"))
                        {
                            moveClock += Time.deltaTime;
                            bd.MoveInput = (int)(moveClock / 1.4f) % 2 == 0 ? Vector2.right : Vector2.left;
                        }
                        // 내 몸이 쓰러지지 않게 — 맞는 것은 보되 판이 끝나면 안 된다
                        if (host != null && host.Hp < 1500) SetHp(host, 5000);
                        warnsSeen = Mathf.Max(warnsSeen, CountActiveWarns(bd));
                        zonesSeen = Mathf.Max(zonesSeen, ((IList)bt.GetField("_zones", F).GetValue(bd)).Count);
                        burnSeen |= (float)bt.GetField("_playerBurnLeft", F).GetValue(bd) > 0f;
                        poisonSeen |= (float)bt.GetField("_playerPoisonLeft", F).GetValue(bd) > 0f;
                        chillSeen |= (float)bt.GetField("_playerChillLeft", F).GetValue(bd) > 0f;
                        shieldSeen |= ((IDictionary)bt.GetField("_lanternShield", F).GetValue(bd)).Count > 0;
                        if (t < RecordSeconds + 0.5) return;
                        bd.MoveInput = Vector2.zero;
                        Debug.Log($"[MON] done {key} 몸={def?.Body} 성질={def?.Traits} 원소={def?.Element}"
                                  + $" 살아있음={mon != null && mon.IsAlive} 내피해={(host != null ? hpBefore - host.Hp : -1)}"
                                  + $" 예고최대={warnsSeen} 자국최대={zonesSeen} 화상={burnSeen} 독={poisonSeen} 냉기={chillSeen}"
                                  + $" 등불방패={shieldSeen}");
                        // 유령으로 맞선 판은 몸이 없다 — 다음 줄은 몸으로 서야 하니 여기서 끝낸다
                        if (flags.Contains("ghost")) { Stop(cb, null); return; }
                        phase = 2;
                        wait = Mathf.RoundToInt(GapSeconds * 60);
                    }
                }
                catch (Exception e)
                {
                    Stop(cb, e.ToString());
                }
            };
            EditorApplication.update += cb;
        }

        private static void Stop(EditorApplication.CallbackFunction cb, string why)
        {
            EditorApplication.update -= cb;
            Debug.Log(why == null ? "[MON] batch done" : "[MON] 멈춤 — " + why);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        private static void ClearRoom(Game.Module.InGame.BattleDirector bd, IList enemies)
        {
            var kill = typeof(Game.Module.InGame.BattleDirector).GetMethod("KillEnemy", F);
            var all = new List<Game.Module.InGame.Unit>();
            foreach (var o in enemies) if (o is Game.Module.InGame.Unit u && u.IsAlive) all.Add(u);
            foreach (var u in all) kill.Invoke(bd, new object[] { u });
        }

        private static int CountActiveWarns(Game.Module.InGame.BattleDirector bd)
        {
            var list = (IList)typeof(Game.Module.InGame.BattleDirector).GetField("_warns", F).GetValue(bd);
            int n = 0;
            foreach (var w in list)
                if (w != null && (bool)w.GetType().GetField("Active", F).GetValue(w)) n++;
            return n;
        }

        private static float Meters(Game.Module.InGame.BattleDirector bd)
            => (float)typeof(Game.Module.InGame.BattleDirector).GetMethod("Meters", F).Invoke(bd, new object[] { 1f });

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
