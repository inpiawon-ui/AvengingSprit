using System.IO;
using System.Reflection;
using Game.Module.InGame;
using GameFramework.Core.Base;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>
    /// 상성 시험판을 **한 판 돌려 보는** 도구 (2026-10-01).
    ///
    /// 부트 씬에서 플레이를 켜고 로비 → 챕터·호스트 선택을 지나 1챕터에 들어간 뒤
    /// 자동 조종(`PromoPilot`)을 붙인다. 켜 두면 영상도 같이 찍는다.
    ///
    /// `RoomShotTool` 과 같은 방식(EditorApplication.update + SessionState)이다 —
    /// 값은 SessionState 에 있어 플레이 진입의 도메인 리로드를 넘어간다.
    ///
    ///   AffinityDemo.host    시작할 몸(호스트 키). 비우면 유령으로 시작한다
    ///   AffinityDemo.seek    유리한 몸으로 갈아탈 것인가(끄면 처음 몸으로 끝까지 — 견주기용)
    ///   AffinityDemo.record  영상 파일 경로(확장자 없이). 비우면 안 찍는다
    ///   AffinityDemo.rooms   이 방까지 깨면 멈춘다(영상 길이). 0 이면 계속
    /// </summary>
    [InitializeOnLoad]
    public static class AffinityDemoTool
    {
        private const string Key = "AffinityDemo.step";
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        private static int _wait;
        private static RecorderController _recorder;

        static AffinityDemoTool() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Game/시험판 — 상성 한 판 돌리기")]
        private static void Start()
        {
            SessionState.SetInt(Key, 1);
            SessionState.SetBool("AffinityDemo.ran", false);
            _wait = 0;
            if (!EditorApplication.isPlaying) { OpenBootScene(); EditorApplication.EnterPlaymode(); }
        }

        [MenuItem("Tools/Game/시험판 — 상성 한 판 돌리기 멈춤")]
        private static void Stop()
        {
            SessionState.SetInt(Key, 0);
            StopRecording();
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
                Debug.LogError("[AffinityDemo] " + e);
                SessionState.SetInt(Key, 0);
                StopRecording();
            }
        }

        private static void Step(int step)
        {
            var director = Object.FindAnyObjectByType<BattleDirector>();

            // ── 1. 인게임까지 걸어 들어간다 ─────────────────────
            if (step == 1)
            {
                if (director != null) { SessionState.SetInt(Key, 2); _wait = 10; return; }

                if (GameObject.Find("[LoadingView]") != null) { _wait = 20; return; }
                if (Click("SkipButton") || Click("TouchArea")) { _wait = 40; return; }
                if (GameObject.Find("BottomNav") != null)
                {
                    if (!CoreModule.TryGet<Game.User.IPlayerDataService>(out var p) || !p.IsReady)
                    { _wait = 30; return; }
                    if (Screen.width != 720 || Screen.height != 1280) { SetSize(720, 1280); _wait = 120; return; }

                    var panel = Object.FindAnyObjectByType<Game.Module.Lobby.ChapterHostPanel>();
                    if (panel != null && panel.gameObject.activeInHierarchy)
                    {
                        // 시작 직전에 녹화를 건다 — 고르는 장면부터 담긴다.
                        StartRecording();
                        string host = SessionState.GetString("AffinityDemo.host", string.Empty);
                        var t = panel.GetType();
                        t.GetField("_chapter", F).SetValue(panel, 1);
                        t.GetField("_pickedRandom", F).SetValue(panel, false);
                        t.GetField("_pickedHost", F).SetValue(panel,
                            string.IsNullOrEmpty(host) ? Game.Character.HostEntry.GhostKey : host);
                        t.GetMethod("OnStart", F).Invoke(panel, null);
                        _wait = 120;
                        return;
                    }
                    if (Click("ModePlayButton")) { _wait = 60; return; }
                }
                _wait = 15;
                return;
            }

            // ── 2. 자동 조종을 붙인다 ───────────────────────────
            if (director == null) { _wait = 15; return; }
            var bt = typeof(BattleDirector);
            if (bt.GetField("_canonRoom", F).GetValue(director) == null) { _wait = 20; return; }

            if (step == 2)
            {
                var pilot = director.gameObject.GetComponent<PromoPilot>();
                if (pilot == null) pilot = director.gameObject.AddComponent<PromoPilot>();
                pilot.SeekAdvantageOn = SessionState.GetBool("AffinityDemo.seek", true);
                // 끄면 진짜로 맞고 죽는다 — 유령 에너지가 얼마나 버티는지 볼 때 쓴다.
                pilot.KeepAlive = SessionState.GetBool("AffinityDemo.keepAlive", true);
                PromoPilot.Log.Clear();
                SessionState.SetInt(Key, 3);
                SessionState.SetInt("AffinityDemo.startFrame", Time.frameCount);
                _wait = 10;
                return;
            }

            // ── 3. 정한 방까지 깨면 멈춘다 ───────────────────────
            int until = SessionState.GetInt("AffinityDemo.rooms", 0);
            int room = (int)bt.GetField("_roomIndex", F).GetValue(director);
            // ⚠ 판이 아직 안 섰을 때도 `_running` 은 false 다 — 한 번 돈 것을 본 뒤에만 «끝»으로 읽는다.
            bool running = (bool)bt.GetField("_running", F).GetValue(director);
            if (running) SessionState.SetBool("AffinityDemo.ran", true);
            bool over = !running && SessionState.GetBool("AffinityDemo.ran", false);
            if (over || (until > 0 && room >= until))
            {
                SessionState.SetString("AffinityDemo.marks", PromoPilot.Log.ToString());
                SessionState.SetInt(Key, 0);
                StopRecording();
                Debug.Log($"[AffinityDemo] 끝 — 방 {room + 1} · 장면 기록 {PromoPilot.Log}");
                return;
            }
            _wait = 15;
        }

        // ── 녹화 ────────────────────────────────────────────────

        private static void StartRecording()
        {
            string path = SessionState.GetString("AffinityDemo.record", string.Empty);
            if (string.IsNullOrEmpty(path) || _recorder != null) return;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "AffinityDemo";
            movie.Enabled = true;
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 720, OutputHeight = 1280 };
            movie.OutputFile = path;
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRate = 30f;
            RecorderOptions.VerboseMode = false;
            _recorder = new RecorderController(settings);
            _recorder.PrepareRecording();
            _recorder.StartRecording();
            Debug.Log("[AffinityDemo] 녹화 시작 — " + path);
        }

        private static void StopRecording()
        {
            if (_recorder == null) return;
            if (_recorder.IsRecording()) _recorder.StopRecording();
            _recorder = null;
            Debug.Log("[AffinityDemo] 녹화 끝");
        }

        // ── 도우미 (`RoomShotTool` 과 같다) ─────────────────────

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
            if (found < 0) return;   // 720×1280 은 `RoomShotTool` 이 이미 만들어 뒀다
            var viewType = asm.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType, false, null, false);
            viewType.GetMethod("SizeSelectionCallback").Invoke(view, new object[] { found, null });
        }
    }
}
