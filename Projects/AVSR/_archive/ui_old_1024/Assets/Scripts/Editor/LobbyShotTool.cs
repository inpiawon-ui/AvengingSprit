using System.IO;
using Game.Module.Common;
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
    /// 로비를 해상도 · 상태 · 언어별로 찍어 둔다 (로비 v4 검수용).
    ///   - 시안 상태: 상자 세 칸에 은 · 금 · 백금 「3時間 12分」, 골드 1,357 · 젬 1,000,000 — 시안과 픽셀 대조한다
    ///   - 글자 뺀 판: 같은 상태에서 글자만 끈 것 — `lobby_calib.py` 가 게임 글자 잉크를 잰다
    ///   - 상태: 빈 칸 · 완료 · 세는 중(짧은 시간)
    ///   - 한국어 · 영어, 4:3 · 20:9
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

        private enum Seeds { Mockup, States }

        /// <summary>어느 화면 — 로비(PLAY 칸) · 육성 유령 탭 · 육성 호스트 능력치 · 육성 호스트 스킬.</summary>
        private enum Screen2 { Lobby, Ghost, HostStats, HostSkill }

        /// <summary>무엇을 찍나. `bare` 는 글자를 전부 끄고 찍는다(글자 보정용).</summary>
        private static readonly (int w, int h, Seeds seed, Language lang, bool bare, Screen2 screen, string name)[] Shots =
        {
            (720, 1280, Seeds.Mockup, Language.Japanese, false, Screen2.Lobby, "lobby_16x9"),
            (720, 1280, Seeds.Mockup, Language.Japanese, true, Screen2.Lobby, "lobby_16x9_bare"),
            (720, 1280, Seeds.States, Language.Japanese, false, Screen2.Lobby, "lobby_states"),
            (720, 1280, Seeds.Mockup, Language.Korean, false, Screen2.Lobby, "lobby_ko"),
            (720, 1280, Seeds.Mockup, Language.English, false, Screen2.Lobby, "lobby_en"),
            (768, 1024, Seeds.Mockup, Language.Japanese, false, Screen2.Lobby, "lobby_4x3"),
            (1080, 2400, Seeds.Mockup, Language.Japanese, false, Screen2.Lobby, "lobby_20x9"),
            (720, 1280, Seeds.Mockup, Language.Korean, false, Screen2.Ghost, "growth_ghost_ko"),
            (720, 1280, Seeds.Mockup, Language.Korean, false, Screen2.HostStats, "growth_host_ko"),
            (720, 1280, Seeds.Mockup, Language.Korean, false, Screen2.HostSkill, "growth_skill_ko"),
            (720, 1280, Seeds.Mockup, Language.Korean, true, Screen2.Ghost, "growth_ghost_bare"),
            (720, 1280, Seeds.Mockup, Language.Korean, true, Screen2.HostStats, "growth_host_bare"),
            (720, 1280, Seeds.Mockup, Language.Japanese, false, Screen2.Ghost, "growth_ghost_ja"),
            (720, 1280, Seeds.Mockup, Language.Japanese, false, Screen2.HostStats, "growth_host_ja"),
            (720, 1280, Seeds.Mockup, Language.English, false, Screen2.HostSkill, "growth_skill_en"),
            (1080, 2400, Seeds.Mockup, Language.Japanese, false, Screen2.HostStats, "growth_host_20x9"),
            (768, 1024, Seeds.Mockup, Language.Japanese, false, Screen2.Ghost, "growth_ghost_4x3"),
        };

        private static int _wait;

        static LobbyShotTool() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Game/로비 해상도 스샷")]
        private static void Begin()
        {
            SessionState.SetInt(Key, 1);
            SessionState.SetBool(Settled, false);
            SessionState.SetInt(Key + ".prepared", -1);
            // 출시 언어(일본어)로 고정해 찍는다. 저장된 언어가 무엇이든 같은 조건에서
            // 찍혀야 어제 것과 오늘 것을 견줄 수 있다.
            // ⚠ 목업은 한국어라 글자 길이가 다르다 — 자리 대조는 목업 좌표로 하고,
            //   **글자가 칸을 넘치는지**는 이 일본어 스샷으로 본다(가장 긴 언어).
            PlayerPrefs.SetInt("game.language", (int)Game.Module.Common.Language.Japanese);
            PlayerPrefs.Save();
            SetSize(Shots[0].w, Shots[0].h);
            if (!EditorApplication.isPlaying) { OpenBootScene(); EditorApplication.EnterPlaymode(); }
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
            var card = GameObject.Find("BottomNav");
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
                _wait = 120;
                return;
            }

            int shot = step - 1;
            if (shot >= Shots.Length)
            {
                SessionState.SetInt(Key, 0);
                SetBare(false);
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

            // 찍기 직전에 상태 · 언어를 심는다 — 상자 시간은 흐르므로 매 장 다시 심어야 「3時間 12分」 이 선다
            if (SessionState.GetInt(Key + ".prepared", -1) < shot)
            {
                SessionState.SetInt(Key + ".prepared", shot);
                if (CoreModule.TryGet<Game.Module.Common.ILanguageService>(out var lang)) lang.SetLanguage(s.lang);
                Seed(s.seed);
                Show(s.screen);
                SetBare(s.bare);
                _wait = 90;
                return;
            }

            var dir = Path.Combine(Path.GetTempPath(), "claude");
            Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, s.name + ".png"));
            Debug.Log($"[LobbyShot] {s.name} {Screen.width}x{Screen.height}");
            if (shot == 0) LogBottomStrip();
            // ⚠ 다음 해상도로 **여기서 바꾸지 마라.** `CaptureScreenshot` 은 다음 프레임 끝에
            //   찍히므로 먼저 바꾸면 이번 장이 다음 해상도로 찍힌다.
            SessionState.SetInt(Key, step + 1);
            _wait = 60;
        }

        /// <summary>
        /// 상자 세 칸을 심는다. 시안용 더미다(저장하지 않는다 — 다음 저장 때까지만 남는다).
        ///   Mockup  은 · 금 · 백금 「3時間 12分」 — 시안 그대로. 골드 · 젬 글자도 시안 숫자로 덮는다
        ///   States  빈 칸 · 완료(금) · 세는 중(백금 5분)
        /// </summary>
        private static void Seed(Seeds seed)
        {
            if (!CoreModule.TryGet<IPlayerDataService>(out var p) || !p.IsReady) return;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (seed == Seeds.Mockup)
            {
                // 3시간 12분 + 40초 — 찍는 사이 1분 단위가 넘어가지 않게
                for (int i = 0; i < 3; i++)
                    p.SetChestSlot(i, new[] { "silver", "gold", "platinum" }[i], now + 1000L * (11520 + 40), 4 * 3600);
            }
            else
            {
                p.SetChestSlot(0, null, 0, 0);
                p.SetChestSlot(1, "gold", now - 1000L, 2 * 3600);
                p.SetChestSlot(2, "platinum", now + 1000L * 330, 3600);
            }
            if (CoreModule.TryGet<IEventBus>(out var bus))
                bus.Publish(new Game.Module.Events.ChestChangedEvent());
            // 재화 글자는 화면에서만 시안 숫자로 — 세이브는 건드리지 않는다
            SetText("GoldText", "1,357");
            SetText("GemText", "1,000,000");
        }

        private static void SetText(string name, string value)
        {
            var go = GameObject.Find(name);
            var t = go != null ? go.GetComponent<TMPro.TextMeshProUGUI>() : null;
            if (t != null) t.text = value;
        }

        /// <summary>하단 바 · 탭을 눌러 그 화면으로 간다.</summary>
        private static void Show(Screen2 screen)
        {
            if (screen == Screen2.Lobby) { Click("ChapterButton"); return; }
            Click("HostButton");
            if (screen == Screen2.Ghost) Click("TabGhostButton");
            else
            {
                Click("TabHostButton");
                Click(screen == Screen2.HostSkill ? "SkillTabButton" : "StatTabButton");
            }
            SetText("GrowthGoldText", "125,680");   // 시안 숫자 — 화면에서만
        }

        /// <summary>로비 글자를 전부 끄거나 켠다 — 글자 뺀 판을 찍어 게임 글자 잉크만 뽑는다.</summary>
        private static void SetBare(bool bare)
        {
            var lobby = Object.FindAnyObjectByType<Game.Module.Lobby.LobbyMainUI>();
            if (lobby == null) return;
            foreach (var t in lobby.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                t.enabled = !bare;
            foreach (var img in lobby.GetComponentsInChildren<Image>(true))
                if (img.name == "ModePlayButtonArrow") img.enabled = !bare;
        }


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

        private static bool Click(string name)
        {
            var go = GameObject.Find(name);
            if (go == null || !go.activeInHierarchy) return false;
            var b = go.GetComponent<Button>();
            if (b == null || !b.isActiveAndEnabled) return false;
            b.onClick.Invoke();
            return true;
        }

        /// <summary>
        /// 로비에 **다른 화면의 조각이 켜져 있는지** 적는다.
        ///
        /// 호스트 선택 판은 제 `Awake` 에서 꺼지므로 프리팹만 봐서는 안 보이는데,
        /// 켜지는 도중의 끄기가 안 먹어 조각이 로비 위로 삐져나온 적이 있다(2026-09-16).
        /// 스샷에만 나오는 종류의 사고라 찍는 김에 같이 적는다.
        /// </summary>
        private static void LogBottomStrip()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var g in Object.FindObjectsByType<UnityEngine.UI.Graphic>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!g.isActiveAndEnabled) continue;

                string path = g.name;
                bool foreign = false;
                for (var p = g.transform.parent; p != null; p = p.parent)
                {
                    path = p.name + "/" + path;
                    if (p.name == "HostSelectPanel") foreign = true;
                }
                // 게임 모드 줄과 하단 바 **사이**(화면 아래 150~280 px)에는 아무것도 없어야 한다.
                // 다른 화면 조각이 여기로 흘러나온 적이 있다(2026-09-16).
                var box = new Vector3[4];
                g.rectTransform.GetWorldCorners(box);
                var c0 = g.canvas != null ? g.canvas.worldCamera : null;
                float b0 = RectTransformUtility.WorldToScreenPoint(c0, box[0]).y;
                float t0 = RectTransformUtility.WorldToScreenPoint(c0, box[2]).y;
                // 게임 모드 카드 아래 ~ 하단 바 위의 **빈 띠**. 여기엔 아무것도 없어야 한다.
                float lo = Screen.height * 0.06f, hi = Screen.height * 0.25f;
                bool inGap = b0 > lo && t0 < hi && g.name != "LobbyBackground"
                             && !g.name.EndsWith("Text") && g.name != "NotifyBadge";
                var im0 = g as UnityEngine.UI.Image;
                if (im0 != null && im0.sprite != null && im0.sprite.name.StartsWith("chest_"))
                    inGap = true;   // 상자 그림이 어디에 떠 있는지 전부 적는다
                if (!foreign && !inGap) continue;

                var corners = new Vector3[4];
                g.rectTransform.GetWorldCorners(corners);
                var cam = g.canvas != null ? g.canvas.worldCamera : null;
                // ⚠ 줄바꿈으로 나누지 마라 — 콘솔은 첫 줄만 보여 준다(2026-09-16).
                sb.Append($"  |  {path}  y {RectTransformUtility.WorldToScreenPoint(cam, corners[0]).y:0}"
                          + $"~{RectTransformUtility.WorldToScreenPoint(cam, corners[2]).y:0}");
            }
            Debug.Log(sb.Length == 0
                ? "[LobbyShot] 남의 화면 조각 없음"
                : "[LobbyShot] ⚠ 로비 위에 켜져 있는 남의 화면 조각:" + sb);

            // 화면에 실제로 그려지는 것 **전부**를 파일로 남긴다. 콘솔 한 줄로는
            // 어디서 온 그림인지 못 찾는 일이 있었다(2026-09-16).
            var all = new System.Text.StringBuilder();
            foreach (var g in Object.FindObjectsByType<UnityEngine.UI.Graphic>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!g.isActiveAndEnabled) continue;
                string q = g.name;
                for (var pp = g.transform.parent; pp != null; pp = pp.parent) q = pp.name + "/" + q;
                var cc = new Vector3[4];
                g.rectTransform.GetWorldCorners(cc);
                var cam2 = g.canvas != null ? g.canvas.worldCamera : null;
                var lo2 = RectTransformUtility.WorldToScreenPoint(cam2, cc[0]);
                var hi2 = RectTransformUtility.WorldToScreenPoint(cam2, cc[2]);
                var im2 = g as UnityEngine.UI.Image;
                all.AppendLine($"{q}\tx {lo2.x:0}~{hi2.x:0}\ty {lo2.y:0}~{hi2.y:0}\t"
                               + (im2 != null ? (im2.sprite != null ? im2.sprite.name : "-") : g.GetType().Name));
            }
            var dump = Path.Combine(Path.GetTempPath(), "claude", "lobby_graphics.txt");
            File.WriteAllText(dump, all.ToString());
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
