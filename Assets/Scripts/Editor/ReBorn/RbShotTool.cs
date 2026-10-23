// RE:BORN UI 리뉴얼(2026-10-23) — 새 화면을 플레이에서 해상도 · 언어 · 상태별로 찍는다(실제 데이터).
// 결과: Projects/AVSR/_exchange/ref/ui_reborn_1023/play/{이름}.png · 끝나면 플레이를 끄고 언어를 한국어(0)로 되돌린다.
// ⚠ EditorApplication.update 안에서는 예외를 흘리지 않는다(LobbyShotTool 머리말 — 에디터가 멎은 적이 있다).
using System.IO;
using Game.Module.Common;
using Game.Module.Lobby;
using Game.User;
using GameFramework.Core.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.ReBorn
{
    [InitializeOnLoad]
    internal static class RbShotTool
    {
        private const string Key = "avsr.rbshot";
        private const string OutDir = "Projects/AVSR/_exchange/ref/ui_reborn_1023/play";

        /// <summary>상태 이름 — battle · host · growth:{hostKey}(비우면 가진 몸 중 첫째).</summary>
        private static readonly (int w, int h, Language lang, string state, string name)[] Shots =
        {
            (720, 1280, Language.Korean, "battle", "lobby_16x9"),
            (720, 1280, Language.Korean, "host", "host_16x9"),
            (1080, 2400, Language.Korean, "host", "host_20x9"),
            (768, 1024, Language.Korean, "host", "host_4x3"),
            (720, 1280, Language.Japanese, "host", "host_ja"),
            (720, 1280, Language.English, "host", "host_en"),
            (720, 1280, Language.Korean, "growth:low", "growth_low_16x9"),
            (720, 1280, Language.Korean, "growth:scroll_low", "growth_low_16x9_bottom"),
            (720, 1280, Language.Korean, "host:low", "host_low_16x9"),
            (720, 1280, Language.Korean, "growth:", "growth_16x9"),
            (720, 1280, Language.Korean, "growth:scroll", "growth_16x9_bottom"),
            (1080, 2400, Language.Korean, "growth:", "growth_20x9"),
            (768, 1024, Language.Korean, "growth:", "growth_4x3"),
            (720, 1280, Language.Japanese, "growth:", "growth_ja"),
            (720, 1280, Language.English, "growth:", "growth_en"),
            (720, 1280, Language.ChineseSimplified, "growth:", "growth_zhs"),
        };

        private static int _wait;
        private static int _prepared = -1;

        static RbShotTool() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Game/RE:BORN 화면/플레이 캡처")]
        private static void Begin()
        {
            SessionState.SetInt(Key, 1);
            _prepared = -1;
            RebornUiSwitch.Enabled = true;
            if (!EditorApplication.isPlaying)
            {
                const string boot = "Assets/Scenes/BootScene.unity";
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != boot
                    && UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(boot);
                EditorApplication.EnterPlaymode();
            }
        }

        [MenuItem("Tools/Game/RE:BORN 화면/플레이 캡처 멈춤")]
        private static void Cancel() => SessionState.SetInt(Key, 0);

        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step <= 0 || !EditorApplication.isPlaying) return;
            if (_wait > 0) { _wait--; return; }
            try { Step(step); }
            catch (System.Exception e)
            {
                SessionState.SetInt(Key, 0);
                Debug.LogError($"[RbShot] 중단 — {e.GetType().Name}: {e.Message}");
            }
        }

        private static void Step(int step)
        {
            var lobby = Object.FindAnyObjectByType<RbLobbyUI>();
            if (lobby == null || GameObject.Find("[LoadingView]") != null)
            {
                if (Click("SkipButton") || Click("TouchArea")) _wait = 30; else _wait = 10;
                return;
            }
            if (!CoreModule.TryGet<IPlayerDataService>(out var player) || !player.IsReady) { _wait = 30; return; }

            int shot = step - 1;
            if (shot >= Shots.Length)
            {
                SessionState.SetInt(Key, 0);
                LowMode(false);
                PlayerPrefs.SetInt("game.language", 0);   // PD 에디터 언어는 한국어(메모리 restore-language-after-recording)
                PlayerPrefs.Save();
                Debug.Log($"[RbShot] 끝 → {OutDir}");
                EditorApplication.ExitPlaymode();
                return;
            }
            var s = Shots[shot];
            if (Screen.width != s.w || Screen.height != s.h) { SetSize(s.w, s.h); _wait = 150; return; }
            if (_prepared < shot)
            {
                _prepared = shot;
                if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.SetLanguage(s.lang);
                Show(lobby, player, s.state);
                _wait = 90;
                return;
            }
            Directory.CreateDirectory(OutDir);
            ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, s.name + ".png"));
            Debug.Log($"[RbShot] {s.name} {Screen.width}x{Screen.height}");
            SessionState.SetInt(Key, step + 1);
            _wait = 45;
        }

        // 「모든 호스트 열어 둠」(GameConfig._openAllHosts)이면 성급이 늘 10으로 읽힌다 — 보통 상태를 찍는 동안만 끈다
        private static readonly System.Reflection.FieldInfo OpenAll =
            typeof(PlayerDataService).GetField("s_openAllHosts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        private static object s_savedOpenAll;

        private static void LowMode(bool on)
        {
            if (OpenAll == null) return;
            if (on && s_savedOpenAll == null) { s_savedOpenAll = OpenAll.GetValue(null); OpenAll.SetValue(null, false); }
            else if (!on && s_savedOpenAll != null) { OpenAll.SetValue(null, s_savedOpenAll); s_savedOpenAll = null; }
        }

        private static void Show(RbLobbyUI lobby, IPlayerDataService player, string state)
        {
            LowMode(state.EndsWith("low"));
            if (state == "battle") { lobby.SelectTab("Battle"); return; }
            lobby.SelectTab("Host");
            if (!state.StartsWith("growth:")) return;
            string key = null;
            foreach (var e in player.PlayableHosts)
                if (e != null && !e.IsGhost && player.IsHostOwned(e) && !PlayerDataService.IsHiddenHost(e.HostKey)) { key = e.HostKey; break; }
            if (key == null) return;
            if (state.EndsWith("low")) SeedLow(player, key);
            lobby.OpenGrowth(key);
            if (state.Contains("scroll") && GameObject.Find("GrwBody") is GameObject body && body.TryGetComponent<ScrollRect>(out var sr))
                sr.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        /// 시험 세이브는 모든 몸이 10성이라 보통 상태(성급 2 · 조각 12/20)를 볼 수 없다 — 메모리에서만 바꾼다.
        /// 저장하지 않는다(캡처 전 세이브 백업 · 끝나면 되돌림).
        /// </summary>
        private static void SeedLow(IPlayerDataService player, string key)
        {
            var t = player.GetType();
            var data = t.GetField("_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(player);
            var index = t.GetMethod("IndexOfHost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (data == null || index == null) return;
            int i = (int)index.Invoke(player, new object[] { key });
            if (i < 0) return;
            var dt = data.GetType();
            if (dt.GetField("hostMastery")?.GetValue(data) is int[] mastery && i < mastery.Length) mastery[i] = 2;
            if (dt.GetField("hostShards")?.GetValue(data) is int[] shards && i < shards.Length) shards[i] = 12;
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

        /// <summary>게임 뷰를 고정 해상도로 맞춘다(LobbyShotTool 과 같은 리플렉션).</summary>
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
                if ((int)st.GetProperty("width").GetValue(size) == w && (int)st.GetProperty("height").GetValue(size) == h) { found = i; break; }
            }
            if (found < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var kindType = asm.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) });
                var made = ctor.Invoke(new object[] { System.Enum.Parse(kindType, "FixedResolution"), w, h, $"fit {w}x{h}" });
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { made });
                found = total;
            }
            var gameViewType = asm.GetType("UnityEditor.GameView");
            var win = EditorWindow.GetWindow(gameViewType, false, null, false);
            gameViewType.GetMethod("SizeSelectionCallback").Invoke(win, new object[] { found, null });
        }
    }
}
