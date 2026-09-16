using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 앱을 켠 **첫 몇 초**를 일정 간격으로 찍는다. 한 프레임만 번쩍이는 것은
    /// 눈으로 잡을 수 없어서, 연달아 찍어 놓고 뒤에서 넘겨 본다.
    ///
    /// ⚠ **`EditorApplication.update` 안에서 예외를 흘리지 마라.** 플레이 중에는 스크립트를
    ///   다시 컴파일하지 않으므로 파일을 지워도 안 멈춘다 — 사람이 정지를 눌러야 풀린다
    ///   (2026-09-16 에 로그가 720 MB 로 불어나며 에디터가 멎었다).
    /// </summary>
    [InitializeOnLoad]
    internal static class BootShotTool
    {
        private const string Key = "avsr.bootshot";
        private const string Dir = "boot";
        private const int Shots = 90;          // 0.12초 간격 × 90 = 약 11초
        private const float Interval = 0.12f;
        private const int TapEvery = 8;        // 8장(약 1초)마다 한 번 넘긴다

        private static float _next;

        static BootShotTool() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Game/부팅 연속 스샷")]
        private static void Begin()
        {
            var dir = Path.Combine(Path.GetTempPath(), "claude", Dir);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            SessionState.SetInt(Key, 1);
            _next = 0f;
            if (!EditorApplication.isPlaying) { OpenBootScene(); EditorApplication.EnterPlaymode(); }
        }

        [MenuItem("Tools/Game/부팅 연속 스샷 멈춤")]
        private static void Cancel() => SessionState.SetInt(Key, 0);


        /// <summary>
        /// 플레이 전에 **부트 씬을 연다.**
        ///
        /// 에디터에 로비 씬이 열린 채로 플레이하면 `GameLauncher` 가 없어 모듈이 하나도
        /// 등록되지 않는다 — 언어팩·유저 데이터가 영영 안 와서 도구가 멈춘 채 기다린다
        /// (2026-09-16). 열린 씬이 무엇이든 여기서 맞춘다.
        /// </summary>
        private static void OpenBootScene()
        {
            const string boot = "Assets/Scenes/BootScene.unity";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == boot) return;
            if (!UnityEditor.SceneManagement.EditorSceneManager
                    .SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(boot);
        }

        private static void Click(string name)
        {
            var go = GameObject.Find(name);
            if (go == null || !go.activeInHierarchy) return;
            var b = go.GetComponent<UnityEngine.UI.Button>();
            if (b != null && b.isActiveAndEnabled) b.onClick.Invoke();
        }

        private static void Tick()
        {
            int shot = SessionState.GetInt(Key, 0);
            if (shot <= 0 || !EditorApplication.isPlaying) return;

            try
            {
                if (Time.realtimeSinceStartup < _next) return;
                _next = Time.realtimeSinceStartup + Interval;

                // ⚠ **건너뛰기를 누르지 않는다.** 매 프레임 누르면 오프닝이 통째로
                //   스킵되어 정작 보고 싶은 첫 컷을 못 본다. 한 박자씩 넘기며 본다.
                if (shot % TapEvery == 0) Click("TouchArea");

                var dir = Path.Combine(Path.GetTempPath(), "claude", Dir);
                Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"{shot:000}.png"));

                if (shot >= Shots)
                {
                    SessionState.SetInt(Key, 0);
                    Debug.Log($"[BootShot] 끝 — {dir}");
                    EditorApplication.ExitPlaymode();
                    return;
                }
                SessionState.SetInt(Key, shot + 1);
            }
            catch (System.Exception e)
            {
                // 한 번 알리고 **스스로 멈춘다.**
                SessionState.SetInt(Key, 0);
                Debug.LogError($"[BootShot] 중단 — {e.GetType().Name}: {e.Message}");
            }
        }
    }
}
