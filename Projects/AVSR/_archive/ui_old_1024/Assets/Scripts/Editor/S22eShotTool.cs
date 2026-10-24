using System.IO;
using Game.Module.Common;
using Game.User;
using GameFramework.Core.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 로비 S22e(출전 방식 · 카드 키트 · 아래 바 4칸 · 위 행동력)를 언어 · 출전 방식별로 찍는다(2026-10-15).
    /// 찍은 것: `Projects/AVSR/_exchange/ref/store_mock/s22e_game_*.png`. 끝나면 언어를 **찍기 전 것으로 되돌린다**
    /// (2026-10-12 녹화 하네스가 일본어로 박아 PD 화면이 일본어로 남았다).
    ///
    /// ⚠ `EditorApplication.update` 안에서 예외를 흘리지 않는다 — 한 번 알리고 스스로 멈춘다(LobbyShotTool 참고).
    /// </summary>
    [InitializeOnLoad]
    internal static class S22eShotTool
    {
        private const string Key = "avsr.s22eshot";
        private const string LangKey = Key + ".lang";
        private const string Out = "Projects/AVSR/_exchange/ref/store_mock";

        private enum View { Lobby, Host, Random, Ghost }

        private static readonly (Language lang, View view, string name)[] Shots =
        {
            (Language.Korean, View.Lobby, "s22e_game_lobby_ko"),
            (Language.Korean, View.Host, "s22e_game_host_ko"),
            (Language.Korean, View.Random, "s22e_game_random_ko"),
            (Language.Korean, View.Ghost, "s22e_game_ghost_ko"),
            (Language.Japanese, View.Host, "s22e_game_host_ja"),
            (Language.English, View.Host, "s22e_game_host_en"),
        };

        private static int _wait;

        static S22eShotTool() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Game/로비 S22e 스샷")]
        private static void Begin()
        {
            SessionState.SetInt(Key, 1);
            SessionState.SetInt(Key + ".prepared", -1);
            SessionState.SetInt(LangKey, PlayerPrefs.GetInt("game.language", (int)Language.Japanese));
            SetSize(720, 1280);
            if (!EditorApplication.isPlaying)
            {
                const string boot = "Assets/Scenes/BootScene.unity";
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != boot)
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(boot);
                EditorApplication.EnterPlaymode();
            }
        }

        [MenuItem("Tools/Game/로비 S22e 스샷 멈춤")]
        private static void Cancel() => Finish();

        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step <= 0 || !EditorApplication.isPlaying) return;
            if (_wait > 0) { _wait--; return; }
            try { Step(step); }
            catch (System.Exception e)
            {
                Finish();
                Debug.LogError($"[S22eShot] 중단 — {e.GetType().Name}: {e.Message}");
            }
        }

        private static void Step(int step)
        {
            var nav = GameObject.Find("BottomNav");
            if (nav == null || !nav.activeInHierarchy || GameObject.Find("[LoadingView]") != null)
            {
                _wait = Click("SkipButton") || Click("TouchArea") ? 30 : 10;
                return;
            }
            if (Localize.Get("ui.lobby.game_mode") == "ui.lobby.game_mode") { _wait = 30; return; }
            if (!CoreModule.TryGet<IPlayerDataService>(out var p) || !p.IsReady) { _wait = 30; return; }

            int shot = step - 1;
            if (shot >= Shots.Length)
            {
                Finish();
                Debug.Log("[S22eShot] 끝");
                EditorApplication.ExitPlaymode();
                return;
            }

            var s = Shots[shot];
            if (SessionState.GetInt(Key + ".prepared", -1) < shot)
            {
                SessionState.SetInt(Key + ".prepared", shot);
                if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.SetLanguage(s.lang);
                Show(s.view);
                _wait = 90;
                return;
            }

            Directory.CreateDirectory(Out);
            ScreenCapture.CaptureScreenshot(Path.Combine(Out, s.name + ".png"));
            Debug.Log($"[S22eShot] {s.name} {Screen.width}x{Screen.height}");
            SessionState.SetInt(Key, step + 1);
            _wait = 60;
        }

        private static void Show(View v)
        {
            Click("ChapterButton");                 // PLAY 칸 — 열린 창이 있으면 닫힌다
            if (v == View.Lobby) return;
            Click("ModePlayButton");                // 챕터 · 호스트 창
            Click(v == View.Random ? "CHModeRandom" : v == View.Ghost ? "CHModeGhost" : "CHModeHost");
            if (v == View.Host) PickTodayCard();
        }

        /// <summary>오늘의 몸 카드를 하나 고른다 — 카드 띠 · 버튼 위 「무료」 띠까지 한 장에 보이게.</summary>
        private static void PickTodayCard()
        {
            var content = GameObject.Find("CHHostContent");
            if (content == null) return;
            foreach (Transform card in content.transform)
            {
                if (!card.gameObject.activeInHierarchy) continue;
                var band = card.Find("CardToday");
                if (band == null || !band.gameObject.activeSelf) continue;
                var b = card.GetComponent<Button>();
                if (b != null) b.onClick.Invoke();
                return;
            }
        }

        private static void Finish()
        {
            SessionState.SetInt(Key, 0);
            // 찍기 전 언어로 되돌린다 — 저장(PlayerPrefs)까지
            int before = SessionState.GetInt(LangKey, -1);
            if (before < 0) return;
            if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.SetLanguage((Language)before);
            PlayerPrefs.SetInt("game.language", before);
            PlayerPrefs.Save();
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
            => typeof(LobbyShotTool).GetMethod("SetSize", System.Reflection.BindingFlags.NonPublic
                                                          | System.Reflection.BindingFlags.Static)
                                    ?.Invoke(null, new object[] { w, h });
    }
}
