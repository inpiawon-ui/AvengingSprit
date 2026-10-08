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
    ///   AffinityDemo.recordPerChapter  켜면 영상 파일 이름에 `_ch{챕터}` 를 붙인다 — 줄 선 판마다 따로 남긴다(2026-10-08)
    ///   AffinityDemo.gold    판을 열 때 이만큼 골드를 채운다(몸을 골라 시작하는 값)
    ///   AffinityDemo.unlock  켜면 고른 몸을 숙련도 1 로 열어 둔다(안 가진 몸은 유령으로 돈다)
    ///   AffinityDemo.direct  켜면 자동 조종이 피하지 않고 곧장 가서 때린다(검수 영상을 짧게)
    ///   AffinityDemo.atkMul  내 몸 공격력 배수(1 = 그대로) — 전 챕터 훑어보기 영상에서 너무 오래 안 걸리게(2026-10-08)
    ///
    /// ── 밸런스 검증 (2026-10-02) ─────────────────────────────────
    ///   AffinityDemo.chapter   들어갈 챕터(기본 1)
    ///   AffinityDemo.profile   그 판의 플레이어 힘 — 「유령Lv;유령 체,공,속;몸 체,공,속」(강화 단계)
    ///   AffinityDemo.queue     여러 판을 줄 세운다 — 「챕터|몸|힘」을 줄바꿈으로. 한 판이 끝나면 다음 판을 연다
    ///   결과는 `Library/BalanceRuns.tsv` 에 한 줄씩 쌓인다(챕터 · 도달 방 · 깼는가 · 걸린 초 · 판 골드)
    ///
    /// ⚠ 힘을 넣으면 **저장이 바뀐다.** 돌리기 전에 저장 파일을 따로 떠 두고 끝나면 되돌린다.
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
            SessionState.SetInt("AffinityDemo.watchRoom", -1);
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

        public const string ResultPath = "Library/BalanceRuns.tsv";
        private const float StuckSeconds = 240f;

        /// <summary>줄 선 판이 있으면 하나 꺼내 연다. 플레이가 완전히 꺼진 뒤에만.</summary>
        private static void NextInQueue()
        {
            string queue = SessionState.GetString("AffinityDemo.queue", string.Empty);
            if (string.IsNullOrEmpty(queue)) return;
            int nl = queue.IndexOf('\n');
            string head = nl < 0 ? queue : queue.Substring(0, nl);
            SessionState.SetString("AffinityDemo.queue", nl < 0 ? string.Empty : queue.Substring(nl + 1));
            var t = head.Split('|');
            if (t.Length < 3) return;
            SessionState.SetInt("AffinityDemo.chapter", int.Parse(t[0]));
            SessionState.SetString("AffinityDemo.host", t[1]);
            SessionState.SetString("AffinityDemo.profile", t[2]);
            Start();
        }

        /// <summary>그 판의 플레이어 힘을 저장(메모리)에 넣는다 — 모델이 말한 「이 챕터를 깨는 판의 힘」.</summary>
        private static void ApplyProfile(Game.User.IPlayerDataService player, string hostKey, int chapter)
        {
            var dataField = player.GetType().GetField("_data", F);
            if (!(dataField?.GetValue(player) is Game.User.UserData data)) return;
            data.clearedChapter = Mathf.Max(data.clearedChapter, chapter - 1);
            // 시험용 골드 — 몸을 골라 시작하는 값(60~)이 없으면 「골드가 모자란다」 창에 막힌다(2026-10-08)
            int gold = SessionState.GetInt("AffinityDemo.gold", 0);
            if (gold > 0 && data.gold < gold) data.gold = gold;
            // 시험용 해금 — 안 가진 몸은 빙의가 안 되고 유령으로 돈다(2026-10-08 전 챕터 영상)
            if (SessionState.GetBool("AffinityDemo.unlock", false))
            {
                int unlockAt = (int)player.GetType().GetMethod("EnsureHost", F).Invoke(player, new object[] { hostKey });
                if (unlockAt >= 0 && data.hostMastery[unlockAt] < 1) data.hostMastery[unlockAt] = 1;
            }

            string profile = SessionState.GetString("AffinityDemo.profile", string.Empty);
            if (string.IsNullOrEmpty(profile)) return;
            var parts = profile.Split(';');
            int count = System.Enum.GetValues(typeof(Game.Character.HostStat)).Length;
            data.NormalizeStats(count);
            int[] at = { (int)Game.Character.HostStat.Hp, (int)Game.Character.HostStat.Atk,
                         (int)Game.Character.HostStat.AtkSpeed };

            data.ghostLevel = int.Parse(parts[0]);
            for (int i = 0; i < data.ghostStatLevels.Length; i++) data.ghostStatLevels[i] = 0;
            var g = parts[1].Split(',');
            for (int i = 0; i < at.Length; i++) data.ghostStatLevels[at[i]] = int.Parse(g[i]);

            int hostAt = (int)player.GetType().GetMethod("IndexOfHost", F).Invoke(player, new object[] { hostKey });
            for (int i = 0; i < data.hostStatLevels.Length; i++) data.hostStatLevels[i] = 0;
            if (hostAt < 0) return;
            var h = parts[2].Split(',');
            for (int i = 0; i < at.Length; i++) data.hostStatLevels[hostAt * count + at[i]] = int.Parse(h[i]);
        }

        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step <= 0 && !EditorApplication.isPlayingOrWillChangePlaymode) { NextInQueue(); return; }
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
                        // 로비 화면도 찍는다 — 판에 들어가기 전에 상성이 어떻게 보이는지.
                        // `AffinityDemo.lobbyShots` 에 「챕터:경로」 를 `|` 로 이어 적으면 하나씩 찍고 넘어간다.
                        string shots = SessionState.GetString("AffinityDemo.lobbyShots", string.Empty);
                        if (!string.IsNullOrEmpty(shots))
                        {
                            int bar = shots.IndexOf('|');
                            string head = bar < 0 ? shots : shots.Substring(0, bar);
                            SessionState.SetString("AffinityDemo.lobbyShots", bar < 0 ? string.Empty : shots.Substring(bar + 1));
                            if (SessionState.GetBool("AffinityDemo.lobbyArmed", false))
                            {
                                // 앞 차례에 챕터를 바꿔 놨다 — 이제 찍는다.
                                ScreenCapture.CaptureScreenshot(head.Substring(head.IndexOf(':') + 1));
                                SessionState.SetBool("AffinityDemo.lobbyArmed", false);
                                _wait = 20;
                                return;
                            }
                            var pt = panel.GetType();
                            pt.GetField("_chapter", F).SetValue(panel, int.Parse(head.Substring(0, head.IndexOf(':'))));
                            pt.GetMethod("RefreshAll", F).Invoke(panel, null);
                            SessionState.SetString("AffinityDemo.lobbyShots", shots);   // 같은 항목을 다음 차례에 찍는다
                            SessionState.SetBool("AffinityDemo.lobbyArmed", true);
                            _wait = 20;
                            return;
                        }

                        // 시작 직전에 녹화를 건다 — 고르는 장면부터 담긴다.
                        StartRecording();
                        string host = SessionState.GetString("AffinityDemo.host", string.Empty);
                        int chapter = SessionState.GetInt("AffinityDemo.chapter", 1);
                        ApplyProfile(p, host, chapter);
                        var t = panel.GetType();
                        t.GetField("_chapter", F).SetValue(panel, chapter);
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
                pilot.Direct = SessionState.GetBool("AffinityDemo.direct", false);   // 곧장 가서 때리기만(전 챕터 검수 영상)
                PromoPilot.Log.Clear();

                // 타격 반응을 견줄 때 — 1챕터 잡몹은 두세 방에 죽어 반응을 볼 틈이 없다.
                // 첫 방 잡몹의 체력만 곱해 오래 맞게 한다(빼앗을 수 있는 몸은 그대로).
                float hpMul = SessionState.GetFloat("AffinityDemo.hpMul", 1f);
                if (hpMul > 1f && bt.GetField("_enemies", F).GetValue(director) is System.Collections.IList list)
                {
                    var hp = typeof(Unit).GetProperty("Hp");
                    foreach (var o in list)
                        if (o is Unit u && u != null && u.IsAlive && !u.IsHostBody)
                        {
                            int v = Mathf.RoundToInt(u.HpMax * hpMul);
                            u.SetHpMax(v);
                            hp.SetValue(u, v);
                        }
                }
                SessionState.SetInt(Key, 3);
                SessionState.SetInt("AffinityDemo.startFrame", Time.frameCount);
                _wait = 10;
                return;
            }

            // 내 몸 공격력 배수 — 몸이 바뀌거나 값이 되돌아가면 다시 건다
            BoostHostAtk(director, bt);

            // ── 3. 정한 방까지 깨면 멈춘다 ───────────────────────
            int until = SessionState.GetInt("AffinityDemo.rooms", 0);
            int room = (int)bt.GetField("_roomIndex", F).GetValue(director);
            // ⚠ 판이 아직 안 섰을 때도 `_running` 은 false 다 — 한 번 돈 것을 본 뒤에만 «끝»으로 읽는다.
            bool running = (bool)bt.GetField("_running", F).GetValue(director);
            if (running) SessionState.SetBool("AffinityDemo.ran", true);
            bool over = !running && SessionState.GetBool("AffinityDemo.ran", false);
            // 한 방에서 너무 오래 있으면 막힌 것이다 — 「막힘」으로 적고 판을 끝낸다(다음 판이 기다린다).
            // 보스방은 길어도 정상이라 넉넉히 준다.
            if (room != SessionState.GetInt("AffinityDemo.watchRoom", -1))
            {
                SessionState.SetInt("AffinityDemo.watchRoom", room);
                SessionState.SetFloat("AffinityDemo.watchSince", Time.time);
            }
            bool stuck = running && Time.time - SessionState.GetFloat("AffinityDemo.watchSince", Time.time) > StuckSeconds;
            if (stuck) Debug.LogWarning($"[AffinityDemo] 방 {room + 1} 에서 {StuckSeconds}초 넘게 못 나갔다 — 막힘으로 적는다");

            if (over || stuck || (until > 0 && room >= until))
            {
                SessionState.SetString("AffinityDemo.marks", PromoPilot.Log.ToString());
                SessionState.SetInt(Key, 0);
                StopRecording();
                Debug.Log($"[AffinityDemo] 끝 — 방 {room + 1} · 장면 기록 {PromoPilot.Log}");

                // 결과 한 줄 — 유령 에너지가 남아 있으면 깬 것이다(죽음은 0 에서만 난다).
                int ghostHp = (int)bt.GetField("_ghostHp", F).GetValue(director);
                int frames = Time.frameCount - SessionState.GetInt("AffinityDemo.startFrame", Time.frameCount);
                File.AppendAllText(ResultPath, string.Join("\t",
                    SessionState.GetInt("AffinityDemo.chapter", 1),
                    SessionState.GetString("AffinityDemo.host", string.Empty),
                    SessionState.GetString("AffinityDemo.profile", string.Empty),
                    room + 1, stuck ? "stuck" : over && ghostHp > 0 ? "clear" : "dead",
                    Time.timeSinceLevelLoad.ToString("0"), director.RunGold, frames,
                    PromoPilot.Log.ToString()) + "\n");

                // 줄 선 판이 남았으면 플레이를 끄고 다음 판으로 넘어간다
                if (!string.IsNullOrEmpty(SessionState.GetString("AffinityDemo.queue", string.Empty)))
                    EditorApplication.ExitPlaymode();
                return;
            }
            _wait = 15;
        }

        // ── 녹화 ────────────────────────────────────────────────

        private static Unit _boostHost;
        private static int _boostTo;

        private static void BoostHostAtk(BattleDirector director, System.Type bt)
        {
            float mul = SessionState.GetFloat("AffinityDemo.atkMul", 1f);
            if (mul <= 1f) return;
            var host = bt.GetField("_host", F).GetValue(director) as Unit;
            if (host == null) return;
            if (host == _boostHost && host.Atk == _boostTo) return;
            _boostHost = host;
            _boostTo = Mathf.RoundToInt(host.Atk * mul);
            typeof(Unit).GetProperty("Atk").SetValue(host, _boostTo);
        }

        private static void StartRecording()
        {
            string path = SessionState.GetString("AffinityDemo.record", string.Empty);
            if (string.IsNullOrEmpty(path) || _recorder != null) return;
            if (SessionState.GetBool("AffinityDemo.recordPerChapter", false))
                path += "_ch" + SessionState.GetInt("AffinityDemo.chapter", 1).ToString("00");

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
