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
            (720, 1280, Language.Japanese, "battle", "lobby_ja"),
            (720, 1280, Language.English, "battle", "lobby_en"),
            (720, 1280, Language.Korean, "rail:Ghost", "ghost_16x9"),
            (1080, 2400, Language.Korean, "rail:Ghost", "ghost_20x9"),
            (720, 1280, Language.Korean, "rail:Missions", "missions_16x9"),
            (720, 1280, Language.Korean, "rail:Pass", "pass_16x9"),
            (720, 1280, Language.Korean, "rail:Mail", "mail_16x9"),
            (720, 1280, Language.Korean, "rail:Daily", "daily_16x9"),
            (720, 1280, Language.Korean, "rail:Settings", "settings_16x9"),
            (720, 1280, Language.Korean, "rail:Language", "language_16x9"),
            (720, 1280, Language.Korean, "rail:Profile", "profile_16x9"),
            (1080, 2400, Language.Korean, "rail:Profile", "profile_20x9"),
            (768, 1024, Language.Korean, "rail:Profile", "profile_4x3"),
            (720, 1280, Language.English, "rail:Profile", "profile_en"),
            (720, 1280, Language.Japanese, "rail:ProfileTitle", "profile_title_ja"),
            (720, 1280, Language.English, "rail:Pass", "pass_en"),
            (720, 1280, Language.Japanese, "rail:Missions", "missions_ja"),
            (720, 1280, Language.Korean, "challenge", "challenge_16x9"),
            (720, 1280, Language.Korean, "challenge:tower", "tower_16x9"),
            (720, 1280, Language.Korean, "challenge:night", "night_16x9"),
            (1080, 2400, Language.Korean, "challenge", "challenge_20x9"),
            (720, 1280, Language.English, "challenge:tower", "tower_en"),
            (720, 1280, Language.Japanese, "challenge:night", "night_ja"),
            (720, 1280, Language.Korean, "shop:best", "shop_best_16x9"),
            (720, 1280, Language.Korean, "shop:today", "shop_today_16x9"),
            (720, 1280, Language.Korean, "shop:chest", "shop_chest_16x9"),
            (720, 1280, Language.Korean, "shop:currency", "shop_currency_16x9"),
            (1080, 2400, Language.Korean, "shop:best", "shop_best_20x9"),
            (720, 1280, Language.Japanese, "shop:currency", "shop_currency_ja"),
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
            (720, 1280, Language.Korean, "growth:ask_low", "growthpop_ask_16x9"),
            (720, 1280, Language.Korean, "growth:done_low", "growthpop_done_16x9"),
            (720, 1280, Language.Korean, "growth:skill", "growthpop_skill_16x9"),
            (1080, 2400, Language.Korean, "growth:ask_low", "growthpop_ask_20x9"),
            (768, 1024, Language.Korean, "growth:skill", "growthpop_skill_4x3"),
            (720, 1280, Language.English, "growth:ask_low", "growthpop_ask_en"),
            (720, 1280, Language.Japanese, "growth:done_low", "growthpop_done_ja"),
            (720, 1280, Language.English, "growth:skill", "growthpop_skill_en"),
            (720, 1280, Language.Korean, "random:ask", "random_ask_16x9"),
            (720, 1280, Language.Korean, "random:reveal", "random_reveal_16x9"),
            (720, 1280, Language.Korean, "random:hidden", "random_hidden_16x9"),
            (1080, 2400, Language.Korean, "random:reveal", "random_reveal_20x9"),
            (768, 1024, Language.Korean, "random:ask", "random_ask_4x3"),
            (720, 1280, Language.English, "random:reveal", "random_reveal_en"),
            (720, 1280, Language.Japanese, "random:hidden", "random_hidden_ja"),
            (720, 1280, Language.Korean, "chapters", "chapters_16x9"),
            (1080, 2400, Language.Korean, "chapters", "chapters_20x9"),
            (768, 1024, Language.Korean, "chapters", "chapters_4x3"),
            (720, 1280, Language.English, "chapters", "chapters_en"),
            (720, 1280, Language.Japanese, "chapters", "chapters_ja"),
            (720, 1280, Language.Korean, "rail:PassOptions", "notice_passopt_16x9"),
            (720, 1280, Language.English, "rail:PassOptions", "notice_passopt_en"),
            (720, 1280, Language.Korean, "notice:reward", "notice_reward_16x9"),
            (720, 1280, Language.Korean, "notice:reward4", "notice_reward4_16x9"),
            (720, 1280, Language.Korean, "notice:stamina", "notice_stamina_16x9"),
            (720, 1280, Language.Korean, "notice:gem", "notice_gem_16x9"),
            (720, 1280, Language.Korean, "notice:gold", "notice_gold_16x9"),
            (720, 1280, Language.Korean, "notice:confirm", "notice_confirm_16x9"),
            (768, 1024, Language.Korean, "notice:gold", "notice_gold_4x3"),
            (1080, 2400, Language.Korean, "notice:reward", "notice_reward_20x9"),
            (720, 1280, Language.English, "notice:gold", "notice_gold_en"),
            (720, 1280, Language.Japanese, "notice:stamina", "notice_stamina_ja"),
            (720, 1280, Language.English, "notice:reward4", "notice_reward4_en"),
            (720, 1280, Language.Korean, "host:lock_low", "hostlock_locked_16x9"),
            (720, 1280, Language.Korean, "host:lockready_low", "hostlock_ready_16x9"),
            (1080, 2400, Language.Korean, "host:lock_low", "hostlock_locked_20x9"),
            (768, 1024, Language.Korean, "host:lock_low", "hostlock_locked_4x3"),
            (720, 1280, Language.English, "host:lock_low", "hostlock_locked_en"),
            (720, 1280, Language.Korean, "host:reveal", "hostlock_new_16x9"),
            (720, 1280, Language.Korean, "host:awaken6", "hostlock_awaken6_16x9"),
            (720, 1280, Language.Korean, "host:awaken10", "hostlock_awaken10_16x9"),
            (1080, 2400, Language.Korean, "host:reveal", "hostlock_new_20x9"),
            (768, 1024, Language.Korean, "host:reveal", "hostlock_new_4x3"),
            (720, 1280, Language.Japanese, "host:reveal", "hostlock_new_ja"),
            (720, 1280, Language.Korean, "growth:sources_low", "growthsrc_sources_16x9"),
            (720, 1280, Language.Korean, "growth:sources_essence_low", "growthsrc_essence_16x9"),
            (768, 1024, Language.Korean, "growth:sources_low", "growthsrc_sources_4x3"),
            (720, 1280, Language.English, "growth:sources_low", "growthsrc_sources_en"),
            (720, 1280, Language.Japanese, "growth:sources_essence_low", "growthsrc_essence_ja"),
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
            (720, 1280, Language.Korean, "ingame:tower_clear", "moderesult_tower_clear_16x9"),
            (720, 1280, Language.Korean, "ingame:tower_fail", "moderesult_tower_fail_16x9"),
            (720, 1280, Language.Korean, "ingame:night", "moderesult_night_16x9"),
            (720, 1280, Language.English, "ingame:night", "moderesult_night_en"),
            (720, 1280, Language.Japanese, "ingame:tower_clear", "moderesult_tower_clear_ja"),
            (1080, 2400, Language.Korean, "ingame:tower_clear", "moderesult_tower_clear_20x9"),
            (720, 1280, Language.Korean, "ingame:pause", "pause_16x9"),
            (720, 1280, Language.Korean, "ingame:abandon", "abandon_16x9"),
            (720, 1280, Language.Japanese, "ingame:pause", "pause_ja"),
            // 모드 판 표시는 맨 끝 — 위 가운데 칸 · 띠를 바꿔 두므로 뒤 장면에 남는다
            (720, 1280, Language.Korean, "ingame:towerhud", "modehud_tower_16x9"),
            (720, 1280, Language.Korean, "ingame:nighthud", "modehud_night_16x9"),
            (1080, 2400, Language.Korean, "ingame:towerhud", "modehud_tower_20x9"),
            (768, 1024, Language.Korean, "ingame:nighthud", "modehud_night_4x3"),
        };

        // 이름 앞머리 거르개(SessionState Key+".only") — 새 화면만 찍을 때 목록 전체를 돌지 않게
        private static (int w, int h, Language lang, string state, string name)[] Current
        {
            get
            {
                var all = SessionState.GetInt(Key + ".set", 0) == 1 ? InGameShots : Shots;
                string only = SessionState.GetString(Key + ".only", string.Empty);
                if (string.IsNullOrEmpty(only)) return all;
                var heads = only.Split(',');   // 쉼표로 여러 앞머리
                return System.Array.FindAll(all, s => System.Array.Exists(heads, h => s.name.StartsWith(h.Trim())));
            }
        }

        private static int _wait;
        private static int _loadingSeen;
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
            SessionState.SetBool(Key + ".tShot", false);
            SessionState.SetBool(Key + ".tWait", false);
            SessionState.SetBool(Key + ".lShot", false);
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
            bool ingame = SessionState.GetInt(Key + ".set", 0) == 1;   // 거르개를 쓰면 목록이 새 배열이라 참조로 비교하면 안 된다
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
                // 랜덤 출전은 공개 쪽이 먼저 뜬다 — 「공개하기」 · 「전투 시작」 을 차례로 누른다
                if (Click("SkipButton") || Click("TouchArea") || Click("StartButton") || Click("RndGo")) _wait = 30; else _wait = 20;
                return;
            }
            if (ingame && (GameObject.Find("BootCover") != null || battleUi.GetComponent<Game.Module.InGame.BattleDirector>() == null))
            { _wait = 30; return; }
            if (!ingame && (lobby == null || GameObject.Find("[LoadingView]") != null))
            {
                if (PreShot()) return;
                if (Click("SkipButton") || Click("TouchArea") || Click("StartButton")) _wait = 30; else _wait = 10;
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

        /// <summary>
        /// 로비 앞 화면(타이틀 · 로딩)은 지나가며 한 번씩 찍는다 — 거르개가 「title」일 때만.
        /// 크기는 SessionState `.preW` · `.preH`(기본 720 × 1280), 이름 꼬리는 `.preTag`(기본 16x9).
        /// </summary>
        private static bool PreShot()
        {
            if (SessionState.GetString(Key + ".only", string.Empty) != "title") return false;
            int w = SessionState.GetInt(Key + ".preW", 720), h = SessionState.GetInt(Key + ".preH", 1280);
            string tag = SessionState.GetString(Key + ".preTag", "16x9");
            if (Screen.width != w || Screen.height != h) { SetSize(w, h); _wait = 150; return true; }
            var title = Object.FindAnyObjectByType<Game.Module.Title.TitleMainUI>();
            bool covered = GameObject.Find("[LoadingView]") != null;   // 부트 → 타이틀 전환도 가림막을 쓴다 — 걷힌 뒤에 찍는다
            if (title != null && !SessionState.GetBool(Key + ".tShot", false))
            {
                if (covered) { _wait = 10; return true; }
                if (!SessionState.GetBool(Key + ".tWait", false)) { SessionState.SetBool(Key + ".tWait", true); _wait = 120; return true; }
                Capture($"title_{tag}");
                SessionState.SetBool(Key + ".tShot", true);
                _wait = 45;
                return true;
            }
            var loading = GameObject.Find("RbLoadingView(Clone)");
            if (loading != null && SessionState.GetBool(Key + ".tShot", false) && !SessionState.GetBool(Key + ".lShot", false))
            {
                if (_loadingSeen++ < 40) { _wait = 0; return true; }   // 막대가 조금 찬 뒤에 찍는다
                Capture($"loading_{tag}");
                SessionState.SetBool(Key + ".lShot", true);
                return true;
            }
            return false;
        }

        private static void Capture(string name)
        {
            Directory.CreateDirectory(OutDir);
            ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, name + ".png"));
            Debug.Log($"[RbShot] {name} {Screen.width}x{Screen.height}");
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
            if (state.StartsWith("chapters")) { LowMode(state.EndsWith("low")); lobby.SelectTab("Chapters"); return; }
            if (state.StartsWith("random:"))
            {
                // 랜덤 공개 — 박자 1(?) · 박자 2(가진 몸 첫째 · 히든). 「전투 시작」 은 누르지 않는다
                lobby.SelectTab("Battle");
                var rnd = Object.FindAnyObjectByType<RbRandomPage>(FindObjectsInactive.Include);
                if (rnd == null) return;
                string pick = PlayerDataService.HiddenTrialHostKey;
                if (!state.EndsWith("hidden"))
                    foreach (var e in player.PlayableHosts)
                        if (e != null && !e.IsGhost && player.IsHostOwned(e) && !PlayerDataService.IsHiddenHost(e.HostKey)) { pick = e.HostKey; break; }
                rnd.Show(player, pick, null);
                if (!state.EndsWith("ask")) typeof(RbRandomPage).GetMethod("Reveal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(rnd, null);
                return;
            }
            if (state.StartsWith("notice:"))
            {
                // 알림 창 — 값은 시안 예시(보상 432 골드 · 조각 3 · 유령 경험치 30 / 골드 필요 200 보유 40 / 입문 패키지 ₩5,900)
                lobby.SelectTab("Battle");
                var n = lobby.Notice;
                string what = state.Substring(7);
                if (what == "reward")
                    n.ShowReward(new System.Collections.Generic.List<(string, string, int)> { ("gold", null, 432), ("shard", null, 3), ("passxp", null, 30) });
                else if (what == "reward4")
                    n.ShowReward(new System.Collections.Generic.List<(string, string, int)> { ("gem", null, 300), ("ticket", null, 10), ("gold", null, 5000), ("chest", "gold", 1) });
                else if (what == "stamina" && CoreModule.TryGet<Game.Module.Common.Store.IStaminaService>(out var st)) n.ShowStamina(st, player, true, null);
                else if (what == "gem") n.ShowGemShort();
                else if (what == "gold") n.ShowGoldShort(200, 40, Localize.FromTable("ui.rb.notice.gold_keep", "선택한 호스트는 그대로 유지됩니다."),
                                                       Localize.FromTable("ui.rb.lobby.gold_short_random", "랜덤으로 출전 · 골드 없음"), null);
                else if (what == "confirm") n.ShowConfirm("rb_icon_currency_gem_96", "입문 패키지 I 구매", "₩5,900", Localize.FromTable("ui.rb.notice.store_pay", "결제는 스토어 계정으로 진행됩니다"), null);
                return;
            }
            if (state.StartsWith("rail:"))
            {
                lobby.SelectTab("Battle");
                string what = state.Substring(5);
                if (what == "Language")
                {
                    lobby.OpenRail("Settings");
                    var misc = Object.FindAnyObjectByType<RbMiscPages>();
                    foreach (var t in misc.GetComponentsInChildren<Transform>(true)) if (t.name == "RbLanguage") t.gameObject.SetActive(true);
                    typeof(RbMiscPages).GetMethod("FillLanguage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(misc, null);
                }
                else if (what == "ProfileTitle")
                {
                    lobby.OpenRail("Profile");
                    foreach (var b in Object.FindAnyObjectByType<RbMiscPages>().GetComponentsInChildren<UnityEngine.UI.Button>(true))
                        if (b.name == "ProfTabTitle") b.onClick.Invoke();
                }
                else if (what == "PassOptions")
                {
                    lobby.OpenRail("Pass");
                    typeof(RbPassPage).GetMethod("BuyPremium", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(Object.FindAnyObjectByType<RbPassPage>(), null);
                }
                else lobby.OpenRail(what);
                return;
            }
            if (state.StartsWith("challenge"))
            {
                lobby.SelectTab("Challenge");
                var chl = Object.FindAnyObjectByType<RbChallengePage>();
                if (chl == null || state == "challenge") return;
                const System.Reflection.BindingFlags F3 = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(RbChallengePage).GetMethod("Show", F3)?.Invoke(chl, new object[] { state.EndsWith("tower") ? "ChlTower" : "ChlNight" });
                return;
            }
            if (state.StartsWith("shop:"))
            {
                lobby.SelectTab("Shop");
                var shop = Object.FindAnyObjectByType<RbShopPage>();
                if (shop == null) return;
                const System.Reflection.BindingFlags F2 = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(RbShopPage).GetField("_tab", F2)?.SetValue(shop, state.Substring(5));
                typeof(RbShopPage).GetMethod("Refresh", F2)?.Invoke(shop, null);
                return;
            }
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
            string key = null;
            foreach (var e in player.PlayableHosts)
                if (e != null && !e.IsGhost && player.IsHostOwned(e) && !PlayerDataService.IsHiddenHost(e.HostKey)) { key = e.HostKey; break; }
            if (key == null) return;
            // 미보유(조각 12 · 다 모음) · 새 몸 · 6 · 10성 각성 보여주기 — 미보유는 메모리에서만 성급 0 으로 만든다
            if (state.StartsWith("host:lock"))
            {
                SeedHost(player, key, 0, 12);
                if (state.StartsWith("host:lockready")) SeedHost(player, key, 0, player.MasteryCost(key));
                lobby.OpenGrowth(key);
                return;
            }
            if (state.StartsWith("host:reveal")) { lobby.ShowReveal(key, 0); return; }
            if (state.StartsWith("host:awaken")) { lobby.ShowReveal(key, state.Contains("10") ? 10 : 6); return; }
            if (!state.StartsWith("growth:")) return;
            if (state.EndsWith("low")) SeedLow(player, key);
            lobby.OpenGrowth(key);
            if (state.Contains("scroll") && GameObject.Find("GrwBody") is GameObject body && body.TryGetComponent<ScrollRect>(out var sr))
                sr.verticalNormalizedPosition = 0f;
            // 창 — 성급 강화 확인(2 → 3 · 조각 20 · 골드 1,200 예시값) · 성급 올랐어요 · 스킬 정보(필살기)
            var growthPage = Object.FindAnyObjectByType<RbGrowthPage>();
            const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            if (state.Contains("ask")) typeof(RbGrowthPage).GetMethod("ShowStarAsk", Private)?.Invoke(growthPage, new object[] { 2, 20, 1200 });
            if (state.Contains("done")) typeof(RbGrowthPage).GetMethod("ShowStarDone", Private)?.Invoke(growthPage, null);
            if (state.Contains("skill")) typeof(RbGrowthPage).GetMethod("ShowSkill", Private)?.Invoke(growthPage, new object[] { 0 });
            if (state.Contains("sources")) typeof(RbGrowthPage).GetMethod("ShowSources", Private)?.Invoke(growthPage, null);
            if (state.Contains("essence")) typeof(RbGrowthPage).GetMethod("ShowEssence", Private)?.Invoke(growthPage, null);
        }

        /// <summary>
        /// 시험 세이브는 모든 몸이 10성이라 보통 상태(성급 2 · 조각 12/20)를 볼 수 없다 — 메모리에서만 바꾼다.
        /// 저장하지 않는다(캡처 전 세이브 백업 · 끝나면 되돌림).
        /// </summary>
        private static void SeedLow(IPlayerDataService player, string key) => SeedHost(player, key, 2, 12);

        private static void SeedHost(IPlayerDataService player, string key, int rank, int shardCount)
        {
            var t = player.GetType();
            var data = t.GetField("_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(player);
            var index = t.GetMethod("IndexOfHost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (data == null || index == null) return;
            int i = (int)index.Invoke(player, new object[] { key });
            if (i < 0) return;
            var dt = data.GetType();
            if (dt.GetField("hostMastery")?.GetValue(data) is int[] mastery && i < mastery.Length) mastery[i] = rank;
            if (dt.GetField("hostShards")?.GetValue(data) is int[] shards && i < shards.Length) shards[i] = shardCount;
        }

        /// <summary>판을 거의 세운다(유령이 몇 초 만에 죽어 화면이 바뀐다 — 메모리 freeze-run-before-ingame-check). levelup 은 3택1 을 다시 연다(언어가 바뀌면 카드 글도 다시).</summary>
        private static void ShowInGame(Game.Module.InGame.InGameMainUI ui, string state)
        {
            Time.timeScale = 0.05f;
            Game.Module.Common.Modes.RunModeContext.Clear();   // 모드 판 캡처가 켜 둔 것을 되돌린다
            // 앞 장의 창은 닫는다 — 같은 화면에 겹쳐 찍히지 않게
            foreach (var t in ui.GetComponentsInChildren<Transform>(true))
                if (t.name is "BuffChoicePanel" or "ShopPanel" or "EventPanel" or "ShrinePanel" or "RbResultPanel" or "RbPausePanel" or "RbModeBar" or "RbModeBanner") t.gameObject.SetActive(false);
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
                case "ingame:towerhud":
                case "ingame:nighthud":
                {
                    // 모드 판 모양만 — 판은 스토리 그대로 두고 모드 표시만 켠다(시안 예시값). 띠는 걷히지 않게 막는다
                    var t = typeof(Game.Module.InGame.InGameMainUI);
                    bool tower = state == "ingame:towerhud";
                    if (tower) Game.Module.Common.Modes.RunModeContext.BeginTower(2, 0);
                    else Game.Module.Common.Modes.RunModeContext.BeginNight(3, 0);
                    t.GetMethod("ApplyModeHud", F)?.Invoke(ui, null);
                    if (tower)
                    {
                        t.GetMethod("OnTowerFloorEntered", F)?.Invoke(ui, new object[] { new Game.Module.Events.TowerFloorEnteredEvent { EnteredFloor = 2, IsLeaveLocked = true, FloorNameKey = "tower.floor.2.name" } });
                        t.GetMethod("OnTowerEnemiesLeft", F)?.Invoke(ui, new object[] { new Game.Module.Events.TowerEnemiesLeftEvent { RemainingEnemies = 12 } });
                    }
                    else
                    {
                        t.GetMethod("OnNightState", F)?.Invoke(ui, new object[] { new Game.Module.Events.NightStateEvent { ElapsedSeconds = 522, KillCount = 128, WaveAffinityKind = 3, NextEventKind = 2, NextEventSeconds = 80 } });
                        t.GetMethod("ShowModeBanner", F)?.Invoke(ui, new object[] { null, Localize.Format("ui.night.banner.boss", "망령왕"), Localize.Get("ui.night.banner.boss_sub") });
                    }
                    var idField = t.GetField("_modeBannerId", F);
                    if (idField != null) idField.SetValue(ui, (int)idField.GetValue(ui) + 1);
                    foreach (var tr in ui.GetComponentsInChildren<Transform>(true))
                        if (tr.name == "RbModeBanner" && tr.TryGetComponent<CanvasGroup>(out var g)) g.alpha = 1f;
                    break;
                }
                case "ingame:tower_clear":
                case "ingame:tower_fail":
                case "ingame:night":
                    // 탑 · 밤 결과 판만 띄운다(판을 끝내지 않는다). 숫자는 시안 예시(2F · 남은 입장 2 · 128 처치)
                    var modeFaces = typeof(Game.Module.InGame.InGameMainUI).GetField("_faceAtlas", F)?.GetValue(ui) as UnityEngine.U2D.SpriteAtlas;
                    var rb = ui.GetComponentInChildren<Game.Module.InGame.RbRunResult>(true);
                    if (rb == null) break;
                    if (state == "ingame:night")
                        rb.ShowNight(new Game.Module.Events.NightFinishedEvent
                        {
                            IsCleared = true, NightDayIndex = 3, FinalKills = 128, PreviousBest = 96, IsNewBest = true,
                            SurvivedSeconds = 522, ReachedLevel = 12, ShardHostKeys = new[] { "amazon" }, ShardCounts = new[] { 3 }, EntriesLeft = 2,
                        }, modeFaces);
                    else
                    {
                        bool towerClear = state.EndsWith("clear");
                        rb.ShowTower(new Game.Module.Events.TowerFinishedEvent
                        {
                            IsFloorCleared = towerClear, PlayedFloor = 2, NextFloor = 3, RewardGold = 500, EntriesLeft = 2,
                            ShardHostKeys = new[] { "amazon" }, ShardCounts = new[] { 3 }, SuggestHostKeys = new[] { "gangster" },
                            PossessCount = 3, LostCount = 1, ElapsedSeconds = 162,
                        }, modeFaces);
                    }
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
