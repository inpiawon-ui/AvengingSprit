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
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        private static readonly Vector2[] Offsets =
        {
            new(-3f, 2.5f), new(3f, 3f), new(-2f, 4.5f), new(2.5f, 4f), new(0f, 3.5f),
            new(1.2f, 6.5f), new(-3.5f, 5.5f), new(3.5f, 1.2f),
        };

        /// <param name="ultAt">녹화 시작 뒤 이 초에 필살기를 누른다(0 이하면 안 누름). 액티브는 0.5초에 누른다.</param>
        /// <param name="activeAt">액티브를 누를 초(0 이하면 안 누름).</param>
        public static void Run(string hostKey, string clip, float recordSeconds, float liftMeters = 3f,
                               float ultAt = -1f, float activeAt = 0.5f)
        {
            int phase = 0, wait = 0;
            double t0 = EditorApplication.timeSinceStartup, recAt = 0;
            float battleAt = 0f;
            bool cast = false, ulted = false;
            var bt = typeof(Game.Module.InGame.BattleDirector);
            EditorApplication.CallbackFunction cb = null;
            cb = () =>
            {
                try
                {
                    double now = EditorApplication.timeSinceStartup;
                    if (now - t0 > 260) { EditorApplication.update -= cb; Debug.Log("[RW] timeout " + phase); return; }
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
                        pt.GetMethod("OnStart", F).Invoke(panel, null);
                        phase = 1;
                        wait = 60;
                        return;
                    }
                    var bd = UnityEngine.Object.FindAnyObjectByType<Game.Module.InGame.BattleDirector>();
                    if (bd == null) { wait = 10; return; }
                    bt.GetField("_invuln", F).SetValue(bd, 999f);
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
                        for (int i = 0; i < Offsets.Length && src != null; i++, alive++)
                        {
                            // 근접만 더 세운다 — 1챕터 원거리는 수류탄병이라 더 세우면 수류탄 폭발이 화면을 덮었다(녹화 rw_sl_1 · rw_sl_2)
                            var pick = src;
                            var u = spawn.Invoke(bd, new object[] { pick.Profile, false, false, host.Position + Offsets[i] * m, "RwTest" + i })
                                as Game.Module.InGame.Unit;
                            // 시험판은 숙련도 10(×1.84)이라 1챕터 잡몹이 한 대에 죽는다 — 연출(끌려오기 · 날아가기 · 3중첩)이 보이게 체력만 늘린다
                            if (u != null) SetHp(u, u.HpMax * EnemyHpMul);
                        }
                        bt.GetField("_skillCooldown", F).SetValue(bd, 999f);
                        Debug.Log($"[RW] {hostKey} enemies {alive} melee {melee?.Key} gun {gun?.Key}");
                        SkillFxRecorder.RecordClip(clip, recordSeconds);
                        recAt = now;
                        phase = 2;
                        return;
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
