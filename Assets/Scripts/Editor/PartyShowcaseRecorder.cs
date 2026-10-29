using System.Collections;
using System.Reflection;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 빙의 파티 플레이 영상 찍기(PD 2026-10-29 「빙의랑 보조 빙의 · 인연 효과 다 플레이 영상으로」).
    /// 시험판 판을 열고 아래 장면을 차례로 일으키며 게임 화면을 그대로 녹화한다(스킬 영상 도구의 녹화를 쓴다).
    ///   ① 보조 빙의 — 메인의 인연 짝을 데려와 분혼 → 인연 켜짐(금빛 실 · 스킬 버튼)
    ///   ② 인연형 액티브 한 번
    ///   ③ 둘째 보조 빙의
    ///   ④ 보조 교체(혼 조각이 옮겨 감)
    ///   ⑤ 메인 교체(빠져나오기 → 들어가기)
    /// 결과: `Projects/AVSR/_exchange/skillfx_now/clip_party_showcase.mp4`
    /// ⚠ 시험용 — 적 호스트를 이 도구가 직접 세운다. 세이브는 부르는 쪽이 백업 · 되돌린다.
    /// </summary>
    public static class PartyShowcaseRecorder
    {
        private const float Gap = 4f;
        private const float Total = 26f;
        private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        private static double s_start = -1;
        private static int s_step;
        private static bool s_waitingRun;

        [MenuItem("Tools/Game/시험판 — 빙의 파티 영상 찍기")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.ExecuteMenuItem("Tools/Game/시험판 — 상성 한 판 돌리기");
                s_waitingRun = true;
            }
            s_start = -1;
            s_step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying && !s_waitingRun) { EditorApplication.update -= Tick; return; }
            var bd = Object.FindObjectOfType<BattleDirector>();
            if (bd == null) return;
            var t = typeof(BattleDirector);
            var host = t.GetField("_host", F).GetValue(bd) as Unit;
            if (s_start < 0)
            {
                if (host == null || !(bool)t.GetField("_running", F).GetValue(bd)) return;
                s_waitingRun = false;
                s_start = EditorApplication.timeSinceStartup;
                SkillFxRecorder.RecordClip("party_showcase", Total);
                Debug.Log("[빙의 파티 영상] 녹화 시작");
                return;
            }

            double el = EditorApplication.timeSinceStartup - s_start;
            if (s_step >= 6 || el > Total + 1f) { EditorApplication.update -= Tick; Debug.Log("[빙의 파티 영상] 장면 끝"); return; }
            if (el < 1.5f + s_step * Gap) return;
            if (host == null) { s_step++; return; }   // 메인이 없으면(쓰러짐 등) 이 장면은 넘긴다

            switch (s_step)
            {
                case 0:   // 보조 빙의 — 인연 짝
                {
                    var partners = BattleDirector.InyeonPartners(host.Key);
                    var u = Spawn(bd, partners.Count > 0 ? partners[0] : "baseball", host.Position + new Vector2(150f, 110f));
                    if (u != null) t.GetMethod("PartySetSub", F).Invoke(bd, new object[] { u, 0, 10 });
                    break;
                }
                case 1:   // 인연형 액티브
                {
                    var cd = t.GetProperty("PlayerSkillCooldown", F);
                    if (cd != null) t.GetField("_skillCooldown", F).SetValue(bd, (float)cd.GetValue(bd));
                    bd.TryActiveSkill();
                    break;
                }
                case 2:   // 둘째 보조
                {
                    var u = Spawn(bd, "medium", host.Position + new Vector2(-160f, 90f));
                    if (u != null) t.GetMethod("PartySetSub", F).Invoke(bd, new object[] { u, 1, 10 });
                    break;
                }
                case 3:   // 보조 교체 — 보조 2 를 새 호스트로
                {
                    var u = Spawn(bd, "ninja_chain", host.Position + new Vector2(-120f, -150f));
                    if (u != null) t.GetMethod("PartySetSub", F).Invoke(bd, new object[] { u, 1, 15 });
                    break;
                }
                case 4:   // 메인 교체
                {
                    var u = Spawn(bd, "dragoon", host.Position + new Vector2(140f, -140f));
                    if (u != null) t.GetMethod("PartySwapMain", F).Invoke(bd, new object[] { u });
                    break;
                }
            }
            s_step++;
        }

        /// <summary>그 호스트를 적으로 한 명 세운다(이미 파티에 있으면 null).</summary>
        private static Unit Spawn(BattleDirector bd, string key, Vector2 at)
        {
            if (Unit.PartyKeys.Contains(key)) return null;
            var t = typeof(BattleDirector);
            var player = t.GetField("_player", F).GetValue(bd);
            var entry = player.GetType().GetMethod("GetHost").Invoke(player, new object[] { key });
            if (entry == null) return null;
            var u = (Unit)t.GetMethod("SpawnRoomUnit", F).Invoke(bd, new object[] { entry, false, false, at, "Showcase_" + key });
            var enemies = (IList)t.GetField("_enemies", F).GetValue(bd);
            if (!enemies.Contains(u)) enemies.Add(u);
            return u;
        }
    }
}
