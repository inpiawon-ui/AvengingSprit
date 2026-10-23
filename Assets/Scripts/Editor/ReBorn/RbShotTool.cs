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
            (720, 1280, Language.Korean, "summon", "summon_16x9"),
            (1080, 2400, Language.Korean, "summon", "summon_20x9"),
            (720, 1280, Language.English, "summon", "summon_en"),
            (720, 1280, Language.Korean, "summon:result10", "summon_result10_16x9"),
            (720, 1280, Language.Korean, "summon:result1", "summon_result1_16x9"),
            (720, 1280, Language.Korean, "summon:rates", "summon_rates_16x9"),
            (720, 1280, Language.Korean, "summon:records", "summon_records_16x9"),
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

        /// <summary>인게임 — 로비에서 출전을 눌러 판에 들어간 뒤 판을 거의 세우고(timeScale) 찍는다. levelup 은 3택1 을 바로 연다.</summary>
        private static readonly (int w, int h, Language lang, string state, string name)[] InGameShots =
        {
            (720, 1280, Language.Korean, "ingame", "fight_16x9"),
            (1080, 2400, Language.Korean, "ingame", "fight_20x9"),
            (768, 1024, Language.Korean, "ingame", "fight_4x3"),
            (720, 1280, Language.Korean, "ingame:levelup", "levelup_16x9"),
            (720, 1280, Language.Japanese, "ingame:levelup", "levelup_ja"),
            (720, 1280, Language.English, "ingame:levelup", "levelup_en"),
            (1080, 2400, Language.Korean, "ingame:levelup", "levelup_20x9"),
            (720, 1280, Language.Korean, "ingame:shop", "shop_16x9"),
            (720, 1280, Language.Japanese, "ingame:shop", "shop_ja"),
            (720, 1280, Language.Korean, "ingame:event", "event_16x9"),
            (720, 1280, Language.English, "ingame:event", "event_en"),
            (720, 1280, Language.Korean, "ingame:shrine", "shrine_16x9"),
            (1080, 2400, Language.Korean, "ingame:shrine", "shrine_20x9"),
            (720, 1280, Language.Korean, "ingame:result_clear", "result_clear_16x9"),
            (720, 1280, Language.Korean, "ingame:result_fail", "result_fail_16x9"),
            (1080, 2400, Language.Korean, "ingame:result_clear", "result_clear_20x9"),
            (720, 1280, Language.Japanese, "ingame:result_clear", "result_clear_ja"),
            (768, 1024, Language.English, "ingame:result_fail", "result_fail_4x3"),
            (720, 1280, Language.Korean, "ingame:pause", "pause_16x9"),
            (720, 1280, Language.Korean, "ingame:abandon", "abandon_16x9"),
            (720, 1280, Language.Japanese, "ingame:pause", "pause_ja"),
        };

        private static (int w, int h, Language lang, string state, string name)[] Current
            => SessionState.GetInt(Key + ".set", 0) == 1 ? InGameShots : Shots;

        private static int _wait;
        private static int _prepared = -1;
        private static bool _sortie;

        static RbShotTool() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Game/RE:BORN 화면/플레이 캡처 — 인게임")]
        private static void BeginInGame()
        {
            SessionState.SetInt(Key + ".set", 1);
            _sortie = false;
            Start();
        }

        [MenuItem("Tools/Game/RE:BORN 화면/플레이 캡처")]
        private static void Begin()
        {
            SessionState.SetInt(Key + ".set", 0);
            Start();
        }

        private static void Start()
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
            var shots = Current;
            bool ingame = shots == InGameShots;
            var lobby = Object.FindAnyObjectByType<RbLobbyUI>();
            var battleUi = ingame ? Object.FindAnyObjectByType<Game.Module.InGame.InGameMainUI>() : null;
            if (ingame && battleUi == null)
            {
                // 아직 로비 — 출전을 한 번 누르고 판이 설 때까지 기다린다
                if (lobby != null && !_sortie && CoreModule.TryGet<IPlayerDataService>(out var p0) && p0.IsReady)
                {
                    lobby.SelectTab("Battle");
                    _sortie = Click("SortieButton");
                    _wait = 120;
                    return;
                }
                if (Click("SkipButton") || Click("TouchArea")) _wait = 30; else _wait = 20;
                return;
            }
            if (ingame && (GameObject.Find("BootCover") != null || battleUi.GetComponent<Game.Module.InGame.BattleDirector>() == null))
            { _wait = 30; return; }
            if (!ingame && (lobby == null || GameObject.Find("[LoadingView]") != null))
            {
                if (Click("SkipButton") || Click("TouchArea")) _wait = 30; else _wait = 10;
                return;
            }
            if (!CoreModule.TryGet<IPlayerDataService>(out var player) || !player.IsReady) { _wait = 30; return; }

            int shot = step - 1;
            if (shot >= shots.Length)
            {
                SessionState.SetInt(Key, 0);
                Time.timeScale = 1f;
                LowMode(false);
                PlayerPrefs.SetInt("game.language", 0);   // PD 에디터 언어는 한국어(메모리 restore-language-after-recording)
                PlayerPrefs.Save();
                Debug.Log($"[RbShot] 끝 → {OutDir}");
                EditorApplication.ExitPlaymode();
                return;
            }
            var s = shots[shot];
            if (Screen.width != s.w || Screen.height != s.h) { SetSize(s.w, s.h); _wait = 150; return; }
            if (_prepared < shot)
            {
                _prepared = shot;
                if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.SetLanguage(s.lang);
                if (ingame) ShowInGame(battleUi, s.state);
                else Show(lobby, player, s.state);
                _wait = s.state.StartsWith("ingame:") ? 300 : 90;   // 창 여는 연출이 끝날 때까지
                return;
            }
            if (ingame && battleUi != null)
            {
                // 그리기 순서 기록 — 창보다 HUD 가 위에 그려진 적이 있다(제단 16:9)
                var sb = new System.Text.StringBuilder($"[RbShot] 순서 {s.name}:");
                for (int i = 0; i < battleUi.transform.childCount; i++)
                {
                    var c = battleUi.transform.GetChild(i);
                    if (!c.gameObject.activeSelf) continue;
                    var cv = c.GetComponent<Canvas>();
                    sb.Append($" {i}:{c.name}({(cv != null ? cv.sortingOrder.ToString() : "-")})");
                }
                Debug.Log(sb.ToString());
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
            if (state.StartsWith("summon"))
            {
                lobby.SelectTab("Summon");
                var page = Object.FindAnyObjectByType<RbSummonPage>();
                if (page == null) return;
                const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                if (state == "summon:rates" || state == "summon:records")
                    typeof(RbSummonPage).GetMethod("ShowSub", F)?.Invoke(page, new object[] { state == "summon:rates" ? "SumRates" : "SumRecords" });
                else if (state.StartsWith("summon:result"))
                {
                    // 값을 내지 않고 결과 쪽만 — 가진 몸 열 개로 시안 같은 결과를 꾸민다(저장하지 않는다)
                    var keys = new System.Collections.Generic.List<string>();
                    foreach (var e in player.PlayableHosts)
                        if (e != null && !e.IsGhost && !PlayerDataService.IsHiddenHost(e.HostKey)) keys.Add(e.HostKey);
                    int n = state.EndsWith("1") ? 1 : 10;
                    var pulls = new System.Collections.Generic.List<Game.Module.Common.Store.GachaPull>();
                    for (int i = 0; i < n && keys.Count > 0; i++)
                        pulls.Add(new Game.Module.Common.Store.GachaPull(keys[i % keys.Count], 1 + i % 3, i == 0 && state.EndsWith("jackpot"), false, i == 0, 0));
                    typeof(RbSummonPage).GetMethod("ShowResult", F)?.Invoke(page, new object[] { pulls, n });
                }
                return;
            }
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

        /// <summary>판을 거의 세운다(유령이 몇 초 만에 죽어 화면이 바뀐다 — 메모리 freeze-run-before-ingame-check). levelup 은 3택1 을 다시 연다(언어가 바뀌면 카드 글도 다시).</summary>
        private static void ShowInGame(Game.Module.InGame.InGameMainUI ui, string state)
        {
            Time.timeScale = 0.05f;
            // 앞 장의 창은 닫는다 — 같은 화면에 겹쳐 찍히지 않게
            foreach (var t in ui.GetComponentsInChildren<Transform>(true))
                if (t.name is "BuffChoicePanel" or "ShopPanel" or "EventPanel" or "ShrinePanel" or "RbResultPanel" or "RbPausePanel") t.gameObject.SetActive(false);
            if (state == "ingame") return;
            // 창이 열리는 연출은 게임 시간으로 돈다 — 0.05 배면 덜 열린 채 찍힌다(제단 첫 캡처). 창이 떠 있으면 판은 멈춰 있다
            Time.timeScale = 1f;
            var battle = ui.GetComponent<Game.Module.InGame.BattleDirector>();
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var bt = typeof(Game.Module.InGame.BattleDirector);
            switch (state)
            {
                case "ingame:levelup":
                    bt.GetField("_awaitingBuff", F)?.SetValue(battle, false);
                    bt.GetMethod("OfferBuff", F)?.Invoke(battle, new object[] { Game.Character.CardRarity.Common });
                    break;
                case "ingame:shop": bt.GetMethod("OpenShop", F)?.Invoke(battle, null); break;
                case "ingame:event":
                    // 이 판에서 본 이벤트는 다시 안 나온다 — 장마다 비운다
                    var used = bt.GetField("_eventsUsed", F)?.GetValue(battle);
                    used?.GetType().GetMethod("Clear")?.Invoke(used, null);
                    bt.GetMethod("OfferEvent", F)?.Invoke(battle, null);
                    break;
                case "ingame:shrine": bt.GetMethod("OpenShrine", F)?.Invoke(battle, null); break;
                case "ingame:pause":
                case "ingame:abandon":
                    var pause = ui.GetComponentInChildren<Game.Module.InGame.RbPausePanel>(true);
                    pause?.Open(ui);
                    if (state.EndsWith("abandon") && pause != null)
                        foreach (var t in pause.GetComponentsInChildren<Transform>(true)) if (t.name == "RbAbandon") t.gameObject.SetActive(true);
                    break;
                case "ingame:result_clear":
                case "ingame:result_fail":
                    // 결과 화면만 띄운다(판을 끝내지 않는다 — 저장 · 보상 지급이 일어나지 않게). 숫자는 시안 예시
                    var faces = typeof(Game.Module.InGame.InGameMainUI).GetField("_faceAtlas", F)?.GetValue(ui) as UnityEngine.U2D.SpriteAtlas;
                    bool clear = state.EndsWith("clear");
                    ui.GetComponentInChildren<Game.Module.InGame.RbRunResult>(true)?.Show(new Game.Module.Events.StageFinishedEvent
                    {
                        IsCleared = clear, FinishedChapter = 1, RewardGold = clear ? 432 : 96, RewardGhostExp = 30,
                        ShardHostKeys = new[] { "amazon" }, ShardCounts = new[] { 3 },
                        ReachedRoom = clear ? 16 : 7, ChapterRooms = 16, Kills = clear ? 128 : 54, PlaySeconds = clear ? 522 : 231,
                    }, faces);
                    break;
            }
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
