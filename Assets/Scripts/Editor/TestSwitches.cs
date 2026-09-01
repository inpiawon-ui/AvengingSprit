using Cysharp.Threading.Tasks;
using Game.Module.InGame;
using Game.User;
using GameFramework.Core.Base;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 플레이 확인용 스위치. 메뉴에서 켜고 끈다 — 코드를 고치고 다시 컴파일할 일이 없다.
    ///
    /// 값은 `EditorPrefs` 에 있으므로 프로젝트 파일에 남지 않는다.
    /// 즉 **켜 둔 채 커밋되는 일이 없고**, 빌드에도 따라가지 않는다.
    /// </summary>
    public static class TestSwitches
    {
        private const string OneEnemyMenu = "Tools/Game/테스트 — 방당 몹 1기";
        private const string ThemeMenu    = "Tools/Game/테스트 — 1챕터에서 테마 6종 다 보기";
        private const string BossMenu     = "Tools/Game/테스트 — 방 1~6 에 보스 하나씩";

        [MenuItem(OneEnemyMenu)]
        private static void ToggleOneEnemy()
            => BattleDirector.OneEnemyPerRoom = !BattleDirector.OneEnemyPerRoom;

        [MenuItem(OneEnemyMenu, true)]
        private static bool ToggleOneEnemyValidate()
        {
            Menu.SetChecked(OneEnemyMenu, BattleDirector.OneEnemyPerRoom);
            return true;
        }

        [MenuItem(ThemeMenu)]
        private static void ToggleThemes()
            => BattleDirector.CycleThemesInChapter1 = !BattleDirector.CycleThemesInChapter1;

        [MenuItem(ThemeMenu, true)]
        private static bool ToggleThemesValidate()
        {
            Menu.SetChecked(ThemeMenu, BattleDirector.CycleThemesInChapter1);
            return true;
        }

        // ── 보스 여섯을 한 판에서 ────────────────────────────────
        //
        // 켜면 방 001~006 이 통째로 보스방이 된다.
        //   001 크러셔 · 002 가디언 · 003 킹핀 · 004 파이썬 · 005 로봇스네이크 · 006 슬러지
        //
        // 정상 진행으로는 여섯째 보스를 보려면 48방을 깨야 한다. 예고 도형·취약 창처럼
        // **보스마다 다른 것**을 확인하려면 여섯을 나란히 놓고 봐야 한다.
        //
        // ⚠ 방 데이터를 안 고친다. `BossTable` 이 체력·공격력·패턴을 다 들고 있어
        //   그것만 읽어 세운다 — 끄면 그대로 원래 진행으로 돌아간다.

        [MenuItem(BossMenu)]
        private static void ToggleBossRooms()
            => BattleDirector.BossPerRoomTest = !BattleDirector.BossPerRoomTest;

        [MenuItem(BossMenu, true)]
        private static bool ToggleBossRoomsValidate()
        {
            Menu.SetChecked(BossMenu, BattleDirector.BossPerRoomTest);
            return true;
        }

        // ── 파편 지급 ────────────────────────────────────────────
        //
        // 봉인 하나를 풀려면 그 호스트를 여러 판에 걸쳐 만나야 한다(해제 10개).
        // 확인만 하려는데 그 길을 다 걷게 하면 UI 를 보는 데 한 시간이 든다.
        //
        // ⚠ **플레이 중에만 동작한다.** 저장 데이터를 직접 만지는 것이 아니라
        //   돌고 있는 서비스에 넣으므로, 판을 끄면 사라진다.
        // ── 저장 초기화 ──────────────────────────────────────────
        //
        // "게임을 처음 켠 사람" 을 재현한다. 전부 잠기고, 파편도 골드도 0 이다.
        // 지금 보이는 화면이 신규 유저가 볼 화면인지 확인하려면 이 상태여야 한다.
        private const string ResetMenu = "Tools/Game/저장 초기화 — 처음 시작 상태로";

        [MenuItem(ResetMenu)]
        private static void ResetSave()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[초기화] 플레이를 멈추고 실행할 것 — 돌고 있는 데이터가 다시 덮어쓴다.");
                return;
            }
            // 저장 위치가 백엔드마다 다르므로 파일을 통째로 지운다.
            int n = 0;
            var dir = Application.persistentDataPath;
            if (System.IO.Directory.Exists(dir))
                foreach (var f in System.IO.Directory.GetFiles(dir, "*", System.IO.SearchOption.AllDirectories))
                {
                    var low = f.ToLowerInvariant();
                    if (!low.Contains("avsr") && !low.Contains("userdata") && !low.Contains("local_api")) continue;
                    try { System.IO.File.Delete(f); n++; } catch { }
                }
            PlayerPrefs.DeleteKey("avsr_userdata");
            PlayerPrefs.Save();
            Debug.Log($"[초기화] 저장 {n}개 삭제 · PlayerPrefs 정리 — 다시 실행하면 처음 시작 상태다. {dir}");
        }

        private const string ShardMenu = "Tools/Game/테스트 — 전 호스트에 파편 200개";

        [MenuItem(ShardMenu)]
        private static void GrantShards()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[테스트] 플레이 중에만 넣을 수 있다 — 먼저 실행할 것.");
                return;
            }
            if (!CoreModule.TryGet<IPlayerDataService>(out var player) || !player.IsReady)
            {
                Debug.LogWarning("[테스트] 유저 데이터가 아직 준비되지 않았다.");
                return;
            }
            int n = 0;
            var hosts = player.AllHosts;
            for (int i = 0; i < hosts.Count; i++)
            {
                player.AddShards(hosts[i].HostKey, 200);
                n++;
            }
            Debug.Log($"[테스트] 파편 200개 × {n}명 지급 — 로비에서 HOST 강화를 눌러 볼 것.");
        }

        private const string GoldMenu = "Tools/Game/테스트 — 골드 100,000 지급";

        /// <summary>
        /// 호스트로 시작하려면 골드가 든다(아마존 B급 300). 배치·전투를 보려는데
        /// 매번 골드가 모자라 유령으로만 들어가게 되어 확인이 막힌다.
        ///
        /// 저장에 바로 들어가므로 플레이를 멈춰도 남는다.
        /// </summary>
        [MenuItem(GoldMenu)]
        private static void GrantGold()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[테스트] 플레이 중에만 넣을 수 있다 — 먼저 실행할 것.");
                return;
            }
            if (!CoreModule.TryGet<IPlayerDataService>(out var player) || !player.IsReady)
            {
                Debug.LogWarning("[테스트] 유저 데이터가 아직 준비되지 않았다.");
                return;
            }

            player.AddCurrency(100000, 0);
            player.SaveAsync().Forget();   // fire-and-forget: 저장은 한 프레임 늦어도 된다
            Debug.Log($"[테스트] 골드 100,000 지급 — 지금 {player.Gold:N0}");
        }
    }
}
