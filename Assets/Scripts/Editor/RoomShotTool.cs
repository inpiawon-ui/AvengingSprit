using System.IO;
using System.Reflection;
using Game.Module.InGame;
using GameFramework.Core.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>
    /// 방 배치 검수 스샷 — 부트 씬에서 플레이를 켜고 로비 → 챕터 선택 → 빙의 시작으로
    /// 인게임에 들어간 뒤, 적어 둔 방을 차례로 **직접 열어** 두 장씩 찍는다
    /// (들어선 직후 · 4초 뒤 — 적이 깨어나 엄폐를 쓰는 모습까지).
    ///
    /// 방은 진행으로 가지 않고 `BattleDirector._canonRoomId` 를 바꿔 `EnterRoom` 을 부른다 —
    /// 6챕터 열세째 방을 보려고 80방을 깰 수는 없다. 잡몹 그림은 판 시작에 전 챕터 것이
    /// 다 올라가므로(2026-09-08 결정) 어느 방으로 뛰어도 그림이 선다.
    ///
    /// `LobbyShotTool` 과 같은 방식(EditorApplication.update + SessionState)이다.
    /// 값은 SessionState 에 있어 도메인 리로드를 넘어간다.
    /// </summary>
    [InitializeOnLoad]   // 플레이 진입의 도메인 리로드 뒤에도 `Tick` 이 다시 걸리게
    public static class RoomShotTool
    {
        private const string Key = "RoomShot.step";
        private const string Wait = "RoomShot.wait";
        private const string Menu = "Tools/Game/방 스샷 (배치 검수)";

        /// <summary>찍을 방. 챕터마다 대표 둘·셋 — 새 기믹이 처음 나오는 방을 고른다.</summary>
        public static readonly string[] Rooms =
        {
            "ROOM_CH1_001", "ROOM_CH1_007", "ROOM_CH1_011",
            "ROOM_CH2_003", "ROOM_CH2_014",
            "ROOM_CH3_001", "ROOM_CH3_005", "ROOM_CH3_009",
            "ROOM_CH4_001", "ROOM_CH4_002", "ROOM_CH4_003",
            "ROOM_CH5_001", "ROOM_CH5_002", "ROOM_CH5_003",
            "ROOM_CH6_006", "ROOM_CH6_013",
        };

        public static string OutDir = Path.Combine(Path.GetTempPath(), "claude", "roomshots");

        private static int _wait;

        static RoomShotTool() { EditorApplication.update += Tick; }

        [MenuItem(Menu)]
        private static void Start()
        {
            Directory.CreateDirectory(OutDir);
            SessionState.SetInt(Key, 1);
            SessionState.SetInt("RoomShot.room", 0);
            SessionState.SetInt("RoomShot.shot", 0);
            _wait = 0;
            if (!EditorApplication.isPlaying) { OpenBootScene(); EditorApplication.EnterPlaymode(); }
        }

        [MenuItem(Menu + " 멈춤")]
        private static void Stop()
        {
            SessionState.SetInt(Key, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step <= 0 || !EditorApplication.isPlaying) return;
            if (_wait > 0) { _wait--; return; }
            try { Step(step); }
            catch (System.Exception e)
            {
                Debug.LogError("[RoomShot] " + e);
                SessionState.SetInt(Key, 0);
            }
        }

        private static void Step(int step)
        {
            // ── 1. 인게임까지 걸어 들어간다 ─────────────────────
            var director = Object.FindAnyObjectByType<BattleDirector>();
            if (director == null)
            {
                bool loading = GameObject.Find("[LoadingView]") != null;
                if (loading) { _wait = 20; return; }
                if (Click("SkipButton") || Click("TouchArea")) { _wait = 40; return; }
                if (GameObject.Find("BottomNav") != null)
                {
                    if (!CoreModule.TryGet<Game.User.IPlayerDataService>(out var p) || !p.IsReady)
                    { _wait = 30; return; }
                    if (Screen.width != 720 || Screen.height != 1280) { SetSize(720, 1280); _wait = 120; return; }
                    if (Click("PossessStartButton")) { _wait = 120; return; }
                    if (Click("ChapterStartButton")) { _wait = 60; return; }
                    if (Click("ModePlayButton")) { _wait = 60; return; }
                }
                _wait = 15;
                return;
            }

            // ── 2. 방을 차례로 열고 찍는다 ─────────────────────
            var t = typeof(BattleDirector);
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
            var canonRoom = t.GetField("_canonRoom", F).GetValue(director);
            if (canonRoom == null) { _wait = 30; return; }   // 아직 첫 방을 안 열었다

            int room = SessionState.GetInt("RoomShot.room", 0);
            int shot = SessionState.GetInt("RoomShot.shot", 0);
            // 목록을 바꿔 찍고 싶으면 `RoomShot.rooms` 에 쉼표로 적는다 — 정적 배열은 플레이 진입의
            // 도메인 리로드에서 기본값으로 돌아가므로 SessionState 가 유일하게 살아남는 자리다.
            string custom = SessionState.GetString("RoomShot.rooms", string.Empty);
            var rooms = string.IsNullOrEmpty(custom) ? Rooms : custom.Split(',');
            if (room >= rooms.Length)
            {
                SessionState.SetInt(Key, 0);
                // 플레이는 켜 둔다 — 찍은 뒤 같은 판에서 기믹 동작을 코드로 찔러 본다.
                Debug.Log("[RoomShot] 끝 — " + OutDir + " (플레이 유지)");
                return;
            }

            string id = rooms[room];
            if (shot == 0)
            {
                // 방을 연다. 맞아 죽으면 검수가 끊기므로 판 동안 무적으로 둔다.
                t.GetField("_canonRoomId", F).SetValue(director, id);
                int index = (int)t.GetField("_roomIndex", F).GetValue(director);
                t.GetMethod("EnterRoom", F).Invoke(director, new object[] { index + 1 });
                var inv = t.GetField("_invuln", F);
                if (inv != null && inv.FieldType == typeof(float)) inv.SetValue(director, 9999f);
                SessionState.SetInt("RoomShot.shot", 1);
                _wait = 90;   // 들어선 직후 — 한 호흡 뒤
                return;
            }

            string name = shot == 1 ? $"{id}_a.png" : $"{id}_b.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, name));
            Debug.Log($"[RoomShot] {name}");
            if (shot == 1)
            {
                // 두 번째 장은 **방 한가운데**에서 찍는다. 입구에 서 있으면 창이 아래 7 m 만
                // 보여 줘서 물건과 적이 대부분 화면 밖이다. 걸어 올라가는 대신 옮겨 놓는다 —
                // 적이 깨어나(화면에 들어오면 곧 싸움) 엄폐를 쓰는 모습까지 같이 찍힌다.
                var avatar = t.GetProperty("Avatar", F)?.GetValue(director) as Unit;
                var toPixels = t.GetMethod("ToPixels", F);
                if (avatar != null && toPixels != null)
                    avatar.Position = (Vector2)toPixels.Invoke(director, new object[] { new Vector2(5f, 8.5f) });
                SessionState.SetInt("RoomShot.shot", 2);
                _wait = 240;   // 4초 — 적이 깨어나 자리를 잡는다
            }
            else
            {
                SessionState.SetInt("RoomShot.room", room + 1);
                SessionState.SetInt("RoomShot.shot", 0);
                _wait = 20;
            }
        }

        private static void OpenBootScene()
        {
            const string boot = "Assets/Scenes/BootScene.unity";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == boot) return;
            if (!UnityEditor.SceneManagement.EditorSceneManager
                    .SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(boot);
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

        /// <summary>게임 뷰를 고정 해상도로 맞춘다 (에디터 내부 타입이라 리플렉션). `LobbyShotTool` 과 같다.</summary>
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
                var size = ctor.Invoke(new object[]
                    { System.Enum.Parse(kindType, "FixedResolution"), w, h, $"fit {w}x{h}" });
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                found = total;
            }
            var viewType = asm.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType, false, null, false);
            viewType.GetMethod("SizeSelectionCallback").Invoke(view, new object[] { found, null });
        }
    }
}
