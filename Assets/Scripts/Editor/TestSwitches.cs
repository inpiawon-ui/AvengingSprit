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
        private const string OpeningMenu  = "Tools/Game/테스트 — 오프닝 다시 보기";
        private const string MinionMenu   = "Tools/Game/테스트 — 보스방 잔몹 끄기";
        private const string BossHpMenu   = "Tools/Game/테스트 — 보스 체력 10배";
        private const string IdleMenu     = "Tools/Game/테스트 — 보스 가만히 (버튼으로만)";

        [MenuItem(OneEnemyMenu)]
        private static void ToggleOneEnemy()
            => BattleDirector.OneEnemyPerRoom = !BattleDirector.OneEnemyPerRoom;

        [MenuItem(OneEnemyMenu, true)]
        private static bool ToggleOneEnemyValidate()
        {
            Menu.SetChecked(OneEnemyMenu, BattleDirector.OneEnemyPerRoom);
            return true;
        }


        // ── 보스 체력 10배 ─────────────────────────────────────
        //
        // 페이즈 2·3 패턴은 체력 60%·30% 아래에서만 나온다. 정본 체력으로는
        // 그 전에 보스가 죽어 네 패턴 중 둘만 보고 끝나는 일이 생긴다.
        // 표 값은 그대로 두고 여기서만 곱한다.

        // ── 보스 가만히 (버튼으로만) ───────────────────────────
        //
        // 스킬을 하나씩 눌러 보는 동안 쿨다운이 돌면 확인하려는 패턴 위에 다른
        // 패턴이 겹친다. 켜 두면 보스가 스스로는 아무것도 안 한다.

        [MenuItem(IdleMenu)]
        private static void ToggleIdle()
            => BattleDirector.BossIdleOnly = !BattleDirector.BossIdleOnly;

        [MenuItem(IdleMenu, true)]
        private static bool ToggleIdleValidate()
        {
            Menu.SetChecked(IdleMenu, BattleDirector.BossIdleOnly);
            return true;
        }

        [MenuItem(BossHpMenu)]
        private static void ToggleBossHp()
            => BattleDirector.BossDoubleHp = !BattleDirector.BossDoubleHp;

        [MenuItem(BossHpMenu, true)]
        private static bool ToggleBossHpValidate()
        {
            Menu.SetChecked(BossHpMenu, BattleDirector.BossDoubleHp);
            return true;
        }

        // ── 보스방 잔몹 끄기 ────────────────────────────────────
        //
        // 패턴 하나를 들여다볼 때 화면에 몸이 서 있으면 도형이 가려지고,
        // 누구한테 맞았는지도 헷갈린다. 잠깐 비우는 스위치다.
        //
        // ⚠ **켜 둔 채로 잊으면 보스방이 못 깨는 방이 된다.** 보스 여섯은 전부
        //   빙의 불가라, 몸이 없으면 내 몸이 죽는 순간 되찾을 것이 없다.

        [MenuItem(MinionMenu)]
        private static void ToggleMinions()
            => BattleDirector.NoBossMinions = !BattleDirector.NoBossMinions;

        [MenuItem(MinionMenu, true)]
        private static bool ToggleMinionsValidate()
        {
            Menu.SetChecked(MinionMenu, BattleDirector.NoBossMinions);
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
        // 켜면 방 001~006 이 통째로 보스방이 된다. **표에 적힌 순서 그대로** 선다 —
        // 그 순서가 곧 무대 순서(원작 6스테이지)라 세기도 같이 올라간다.
        //
        //   001 크러셔 1650 · 002 가디언 2400 · 003 파이썬 3150
        //   004 킹핀 4300  · 005 로봇스네이크 5200 · 006 슬러지 6900
        //
        // ⚠ 킹핀과 파이썬의 자리를 맞바꾼 뒤로 순서가 바뀌었다. 여기 적힌 것이
        //   `BossTable` 순서와 어긋나면 이 주석이 거짓말이 된다 — 표가 정본이다.
        //
        // 정상 진행으로는 여섯째 보스를 보려면 챕터 여섯을 다 깨야 한다.
        // 예고 도형·취약 창처럼 **보스마다 다른 것**을 확인하려면 나란히 놓고 봐야 한다.
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

        // ── 오프닝 다시 보기 ────────────────────────────────────
        //
        // 오프닝은 **첫 실행에만** 뜨고, 한 번 보거나 건너뛰면 `PlayerPrefs` 에
        // 표시가 남아 다음부터 타이틀에서 바로 로비로 간다.
        //
        // ⚠ 그 표시는 **플레이를 멈춰도 남는다.** 확인하다 한 번 건너뛰면
        //   그다음부터 오프닝이 안 떠서 "안 나온다" 로 보인다 — 실제로 그렇게 헤맸다.
        //   이 메뉴로 지운다.
        //
        // `EditorPrefs` 가 아니라 `PlayerPrefs` 다. 게임이 읽는 값이라 여기 있어야 한다.

        [MenuItem(OpeningMenu)]
        private static void ReplayOpening()
        {
            PlayerPrefs.DeleteKey(Game.Module.Opening.OpeningMainUI.SeenKey);
            PlayerPrefs.Save();
            Debug.Log("[테스트] 오프닝 본 기록을 지웠다 — 다음 시작에 다시 뜬다");
        }

        [MenuItem(OpeningMenu, true)]
        private static bool ReplayOpeningValidate()
        {
            // 체크 표시가 곧 "지금 오프닝이 뜨는 상태인가" 다.
            Menu.SetChecked(OpeningMenu,
                PlayerPrefs.GetInt(Game.Module.Opening.OpeningMainUI.SeenKey, 0) == 0);
            return true;
        }

        // ── 보스 하나만 계속 ─────────────────────────────────────
        //
        // 방 1~6 에 여섯을 순서대로 세우면 **여섯째를 보려고 다섯 방을 깨야 한다.**
        // 하나를 오래 들여다보려면 그 하나가 어느 방에서든 나와야 한다.
        // 「순서대로」로 되돌리면 원래대로 여섯이 차례로 선다.

        private const string PickMenu = "Tools/Game/테스트 — 보스 고정/";

        [MenuItem(PickMenu + "순서대로 (1~6방)")]      private static void PickAll()     => Pick(0);
        [MenuItem(PickMenu + "1 크러셔")]              private static void Pick1()       => Pick(1);
        [MenuItem(PickMenu + "2 가디언")]              private static void Pick2()       => Pick(2);
        [MenuItem(PickMenu + "3 파이썬")]              private static void Pick3()       => Pick(3);
        [MenuItem(PickMenu + "4 킹핀")]                private static void Pick4()       => Pick(4);
        [MenuItem(PickMenu + "5 로봇 스네이크")]        private static void Pick5()       => Pick(5);
        [MenuItem(PickMenu + "6 슬러지")]              private static void Pick6()       => Pick(6);

        [MenuItem(PickMenu + "순서대로 (1~6방)", true)] private static bool VAll()  => Mark(0);
        [MenuItem(PickMenu + "1 크러셔", true)]         private static bool V1()    => Mark(1);
        [MenuItem(PickMenu + "2 가디언", true)]         private static bool V2()    => Mark(2);
        [MenuItem(PickMenu + "3 파이썬", true)]         private static bool V3()    => Mark(3);
        [MenuItem(PickMenu + "4 킹핀", true)]           private static bool V4()    => Mark(4);
        [MenuItem(PickMenu + "5 로봇 스네이크", true)]   private static bool V5()    => Mark(5);
        [MenuItem(PickMenu + "6 슬러지", true)]         private static bool V6()    => Mark(6);

        private static readonly string[] PickNames =
        { "순서대로 (1~6방)", "1 크러셔", "2 가디언", "3 파이썬", "4 킹핀", "5 로봇 스네이크", "6 슬러지" };

        private static void Pick(int n)
        {
            BattleDirector.BossPickIndex = n;
            // 켜 두지 않으면 아무 일도 안 일어난다 — 고른 순간 같이 켠다.
            BattleDirector.BossPerRoomTest = true;
            Debug.Log(n == 0
                ? "[테스트] 보스 순서대로 — 방 1~6 에 여섯이 차례로 선다"
                : $"[테스트] 보스 고정: {PickNames[n]} — 어느 방이든 이 보스만 나온다");
        }

        private static bool Mark(int n)
        {
            Menu.SetChecked(PickMenu + PickNames[n], BattleDirector.BossPickIndex == n);
            return true;
        }

        // ── 보스 쿨 절반 ─────────────────────────────────────────
        //
        // 정본 쿨은 8~20초다. 한 판에 네 패턴을 다 보기가 어렵고,
        // 첫 보스는 그 전에 죽어서 두 개만 보고 끝난다 — 확인 자체가 안 된다.
        //
        // ⚠ **정본 값을 고치는 것이 아니다.** `BossDefTable` 숫자는 그대로 있고
        //   `BossBrain.CooldownOf` 가 곱할 때만 반으로 준다. 끄면 즉시 원래대로다.
        private const string BossCoolMenu = "Tools/Game/테스트 — 보스 쿨 절반";

        [MenuItem(BossCoolMenu)]
        private static void ToggleBossCooldown()
            => BattleDirector.BossHalfCooldown = !BattleDirector.BossHalfCooldown;

        [MenuItem(BossCoolMenu, true)]
        private static bool ToggleBossCooldownValidate()
        {
            Menu.SetChecked(BossCoolMenu, BattleDirector.BossHalfCooldown);
            return true;
        }

        // ── 보스 스킬 2초마다 무작위 ──────────────────────────────
        //
        // 「스킬 버튼」(보스 아이들)은 **내가 고른 것**만 나오므로 네 연출이
        // 실제 흐름 속에서 다 제대로 나오는지는 못 본다. 이 스위치는 보스가
        // 스스로 돌되 쿨·거리·묶음을 무시하고 2초마다 하나를 뽑는다.
        //
        // ⚠ 표의 쿨 값을 2초로 낮추는 방식은 안 된다 — 넷이 동시에 0 이 되어
        //   한 프레임에 연달아 나간다. 시계를 하나로 합쳐야 한다.
        private const string BossRandomMenu = "Tools/Game/테스트 — 보스 스킬 2초마다 무작위";

        [MenuItem(BossRandomMenu)]
        private static void ToggleBossRandom()
            => BattleDirector.BossRandomEvery2s = !BattleDirector.BossRandomEvery2s;

        [MenuItem(BossRandomMenu, true)]
        private static bool ToggleBossRandomValidate()
        {
            Menu.SetChecked(BossRandomMenu, BattleDirector.BossRandomEvery2s);
            return true;
        }

        // ── 보스 패턴 이름표 다시 보기 ────────────────────────────
        //
        // 이름표는 **처음 보는 패턴에만** 뜬다(정본 예고 4겹 ④). 한 번 보면
        // 기록이 남아 다음부터 회피 한마디만 뜬다.
        //
        // ⚠ 그 기록은 플레이를 멈춰도 남는다. 확인하다 24패턴을 한 바퀴 돌면
        //   그다음부터 이름이 안 떠서 "이름표가 안 나온다" 로 보인다.
        private const string PatternMenu = "Tools/Game/테스트 — 보스 패턴 이름표 다시 보기";

        [MenuItem(PatternMenu)]
        private static void ForgetPatterns()
        {
            BattleDirector.ForgetSeenPatterns();
            Debug.Log("[테스트] 본 패턴 기록을 지웠다 — 24패턴 이름표가 다시 한 번씩 뜬다");
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
