using System.IO;
using Game.Module.Common.Chest;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 로비를 해상도별로 찍어 둔다. 상자 세 칸에 「세는 중 · 세는 중 · 완료」를 심어
    /// 세 가지 모습이 한 장에 다 나오게 한다.
    ///
    /// ⚠ **`EditorApplication.update` 안에서는 절대 예외를 흘리지 마라.**
    ///   2026-09-16 에 `CoreModule.Get` 이 매 프레임 던지는 바람에 로그가 720 MB 로
    ///   불어나며 에디터가 통째로 멎었다. 플레이 중에는 스크립트를 다시 컴파일하지
    ///   않으므로 **파일을 지워도 안 멈춘다** — 사람이 정지를 눌러야 풀린다.
    ///   그래서 여기는 전부 `TryGet` 이고 바깥을 `try/catch` 로 감싼다.
    /// </summary>
    [InitializeOnLoad]
    internal static class LobbyShotTool
    {
        private const string Key = "avsr.lobbyshot";
        private const string Settled = Key + ".settled";

        /// <summary>
        /// 무엇을 찍나. `turn` 은 찍기 **전에** 게임 모드 칸을 오른쪽으로 넘길 횟수다.
        ///
        /// ⚠ 회전은 반드시 찍어 본다. 목업은 한 장뿐이라 「가운데가 시나리오일 때」만
        ///   맞춰 놓고 끝내기 쉬운데, 넘기면 칸마다 그림·자물쇠·MAIN 딱지가 갈아 끼워진다.
        /// </summary>
        private static readonly (int w, int h, int turn, string name)[] Shots =
        {
            (768, 1024, 0, "lobby_4x3"),
            (720, 1280, 0, "lobby_16x9"),
            (1080, 2400, 0, "lobby_20x9"),
            (720, 1280, 1, "lobby_mode_turn1"),
            (720, 1280, 1, "lobby_mode_turn2"),
        };

        private static int _wait;

        static LobbyShotTool() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Game/로비 해상도 스샷")]
        private static void Begin()
        {
            SessionState.SetInt(Key, 1);
            SessionState.SetBool(Settled, false);
            SessionState.SetInt(Key + ".turned", -1);
            SetSize(Shots[0].w, Shots[0].h);
            if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
        }

        [MenuItem("Tools/Game/로비 해상도 스샷 멈춤")]
        private static void Cancel() => SessionState.SetInt(Key, 0);

        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step <= 0 || !EditorApplication.isPlaying) return;
            if (_wait > 0) { _wait--; return; }

            try { Step(step); }
            catch (System.Exception e)
            {
                // 한 번 알리고 **스스로 멈춘다.** 매 프레임 던지면 에디터가 멎는다.
                SessionState.SetInt(Key, 0);
                Debug.LogError($"[LobbyShot] 중단 — {e.GetType().Name}: {e.Message}");
            }
        }

        private static void Step(int step)
        {
            var card = GameObject.Find("ModeCardCenter");
            bool loading = GameObject.Find("[LoadingView]") != null;
            if (card == null || !card.activeInHierarchy || loading)
            {
                if (Click("SkipButton") || Click("TouchArea")) _wait = 30; else _wait = 10;
                return;
            }

            // ⚠ 「칸이 떴다」로는 부족하다. 언어팩 표와 유저 데이터가 오기 전에 찍으면
            //   글자가 **키 그대로**(`ui.lobby.chest.empty`) 나오고 상자가 전부 빈 칸으로
            //   보인다(2026-09-16 실제로 그랬다). 둘 다 온 뒤에 찍는다.
            if (Game.Module.Common.Localize.Get("ui.lobby.game_mode") == "ui.lobby.game_mode")
            { _wait = 30; return; }
            if (!CoreModule.TryGet<IPlayerDataService>(out var ready) || !ready.IsReady)
            { _wait = 30; return; }

            if (!SessionState.GetBool(Settled, false))
            {
                SessionState.SetBool(Settled, true);
                Seed();
                _wait = 180;
                return;
            }

            int shot = step - 1;
            if (shot >= Shots.Length)
            {
                SessionState.SetInt(Key, 0);
                Debug.Log("[LobbyShot] 끝");
                EditorApplication.ExitPlaymode();
                return;
            }

            var s = Shots[shot];
            if (Screen.width != s.w || Screen.height != s.h)
            {
                SetSize(s.w, s.h);
                _wait = 180;   // 캔버스가 제 크기를 찾고 ScreenFit 이 다시 맞출 때까지
                return;
            }

            if (s.turn > 0 && SessionState.GetInt(Key + ".turned", 0) < shot)
            {
                SessionState.SetInt(Key + ".turned", shot);
                for (int i = 0; i < s.turn; i++) Click("ModeArrowRight");
                _wait = 60;
                return;
            }

            var dir = Path.Combine(Path.GetTempPath(), "claude");
            Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, s.name + ".png"));
            Debug.Log($"[LobbyShot] {s.name} {Screen.width}x{Screen.height}");
            // ⚠ 다음 해상도로 **여기서 바꾸지 마라.** `CaptureScreenshot` 은 다음 프레임 끝에
            //   찍히므로 먼저 바꾸면 이번 장이 다음 해상도로 찍힌다.
            SessionState.SetInt(Key, step + 1);
            _wait = 60;
        }

        /// <summary>세 칸에 「세는 중 · 세는 중 · 완료」를 심는다. 시안용 더미다.</summary>
        private static void Seed()
        {
            if (!CoreModule.TryGet<IPlayerDataService>(out var p) || !p.IsReady) return;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            p.SetChestSlot(0, "gold", now + 1000L * 11520, 4 * 3600);     // 3시간 12분
            p.SetChestSlot(1, "magic", now + 1000L * 6480, 8 * 3600);     // 1시간 48분
            p.SetChestSlot(2, "silver", now - 1000L, 2 * 3600);           // 완료
            if (CoreModule.TryGet<IEventBus>(out var bus))
                bus.Publish(new Game.Module.Events.ChestChangedEvent());
        }

        private static bool Click(string name)
        {
            var go = GameObject.Find(name);
            if (go == null || !go.activeInHierarchy) return false;
            var b = go.GetComponent<Button>();
            if (b == null || !b.isActiveAndEnabled) return false;
            b.onClick.Invoke();
            return true;
        }

        /// <summary>게임 뷰를 고정 해상도로 맞춘다 (에디터 내부 타입이라 리플렉션).</summary>
        private static void SetSize(int w, int h)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = single.GetProperty("instance").GetValue(null);
            var group = sizesType.GetProperty("currentGroup").GetValue(instance);
            var gt = group.GetType();
            int total = (int)gt.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = gt.GetMethod("GetGameViewSize");
            int found = -1;
            for (int i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                var st = size.GetType();
                if ((int)st.GetProperty("width").GetValue(size) == w
                    && (int)st.GetProperty("height").GetValue(size) == h)
                { found = i; break; }
            }
            if (found < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var kindType = asm.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) });
                var made = ctor.Invoke(new object[]
                {
                    System.Enum.Parse(kindType, "FixedResolution"), w, h, $"fit {w}x{h}",
                });
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { made });
                found = total;
            }
            var gameViewType = asm.GetType("UnityEditor.GameView");
            var win = EditorWindow.GetWindow(gameViewType, false, null, false);
            gameViewType.GetMethod("SizeSelectionCallback").Invoke(win, new object[] { found, null });
        }
    }
}
