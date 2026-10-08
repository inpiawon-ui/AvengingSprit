using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Module.InGame;
using Game.Module.Lobby;
using GameFramework.Core.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>
    /// 몬스터 · 오브젝트 배치 확인용 **방 전체** 스샷 (PD 요청 2026-10-07).
    ///
    /// `RoomShotTool` 은 들어선 자리 그대로 찍어 방(16 m)의 아래 7 m 만 나왔다.
    /// 여기서는 방에 들어선 직후 **시간을 세우고 카메라를 방 전체가 들어오게 당겨** 한 장으로 찍는다.
    /// 적은 막 선 자리(배치 그대로)에 있다 — 깨어나 움직이기 전이다.
    ///
    /// 찍는 방은 `Projects/AVSR/Rooms/rooms90.tsv` 의 전투방 전부(ROOM 줄).
    /// 몇 개만 찍으려면 SessionState `RoomLayout.rooms` 에 쉼표로 적는다.
    /// 결과 : `Projects/AVSR/_exchange/ref/room_layout/{방 ID}.png` (720 x 1280).
    ///
    /// 유령은 몸이 없으면 체력이 줄어 사라지므로 찍는 동안 체력 · 무적을 붙잡아 둔다.
    /// 다 찍으면 시간 · 조작 칸을 되돌리고 플레이는 켜 둔다.
    /// </summary>
    [InitializeOnLoad]
    public static class RoomLayoutShotTool
    {
        private const string Key = "RoomLayout.step";
        private const string Menu = "Tools/Game/방 전체 스샷 (배치 확인)";
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        /// <summary>창(필드)은 720 x 1050, 방은 16 m = 1152 px. 위아래 여유를 조금 두고 방이 다 들어오는 배율.</summary>
        private const float OverviewZoom = 0.875f;
        /// <summary>방에 들어선 뒤 찍을 때까지(프레임). 적이 자리에 서되 아직 걷기 전이다.</summary>
        private const int SettleFrames = 20;

        /// <summary>찍는 동안 가리는 조작 칸 — 방 아래를 덮는다.</summary>
        private static readonly string[] HideNodes = { "ControlGroup", "DPadBase", "StageText" };

        public static string OutDir =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Projects", "AVSR", "_exchange", "ref", "room_layout"));

        private static int _wait;

        static RoomLayoutShotTool() { EditorApplication.update += Tick; }

        [MenuItem(Menu)]
        private static void Start()
        {
            Directory.CreateDirectory(OutDir);
            if (string.IsNullOrEmpty(SessionState.GetString("RoomLayout.rooms", string.Empty)))
                SessionState.SetString("RoomLayout.rooms", string.Join(",", ReadRooms()));
            SessionState.SetInt(Key, 1);
            SessionState.SetInt("RoomLayout.room", 0);
            SessionState.SetInt("RoomLayout.phase", 0);
            SessionState.SetBool("RoomLayout.preloaded", false);
            _wait = 0;
            if (!EditorApplication.isPlaying) { OpenBootScene(); EditorApplication.EnterPlaymode(); }
        }

        [MenuItem(Menu + " 멈춤")]
        private static void Stop()
        {
            SessionState.SetInt(Key, 0);
            Time.timeScale = 1f;
            SetHud(true);
        }

        /// <summary>전투방 ID — 표의 ROOM 줄 순서 그대로(챕터 → 방 번호).</summary>
        private static List<string> ReadRooms()
        {
            var list = new List<string>();
            string tsv = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Projects", "AVSR", "Rooms", "rooms90.tsv"));
            foreach (var line in File.ReadAllLines(tsv))
            {
                var cols = line.Split('\t');
                if (cols.Length > 1 && cols[0] == "ROOM") list.Add(cols[1]);
            }
            return list;
        }

        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            // 다 찍은 뒤에도 판을 붙잡아 둔다(`RoomLayout.hold`) — 그 방에서 바로 기믹 · 패턴을 시험하려고.
            //   안 붙잡으면 몇 초 안에 유령이 사라져 판이 끝나고, 그 뒤엔 빙의도 적의 움직임도 없다.
            if (step <= 0)
            {
                if (EditorApplication.isPlaying && SessionState.GetBool("RoomLayout.hold", false))
                {
                    var held = Object.FindAnyObjectByType<BattleDirector>();
                    if (held != null) KeepAlive(held);
                }
                return;
            }
            if (!EditorApplication.isPlaying) return;
            // ⚠ 매 프레임 붙잡는다 — 세운 시간 동안에도 유령이 사라지면 판이 끝나 로비로 간다
            var director = Object.FindAnyObjectByType<BattleDirector>();
            if (director != null) KeepAlive(director);
            if (_wait > 0) { _wait--; return; }
            try { Step(director); }
            catch (System.Exception e)
            {
                // 한 번 알리고 멈춘다. 매 프레임 던지면 에디터가 멎는다(LobbyShotTool 주석)
                Debug.LogError("[RoomLayout] " + e);
                Stop();
            }
        }

        private static void Step(BattleDirector director)
        {
            // ── 1. 인게임까지 — 로비에서 유령(값 없음)으로 시작한다 ─────────
            if (director == null)
            {
                Time.timeScale = 1f;
                if (GameObject.Find("[LoadingView]") != null) { _wait = 20; return; }
                if (Click("SkipButton") || Click("TouchArea")) { _wait = 40; return; }
                var lobby = Object.FindAnyObjectByType<LobbyMainUI>();
                if (lobby != null)
                {
                    if (!CoreModule.TryGet<Game.User.IPlayerDataService>(out var p) || !p.IsReady) { _wait = 30; return; }
                    if (Screen.width != 720 || Screen.height != 1280) { SetSize(720, 1280); _wait = 120; return; }
                    var panel = lobby.GetComponentInChildren<ChapterHostPanel>(true);
                    if (!panel.IsOpen) typeof(LobbyMainUI).GetMethod("PlayScenario", F).Invoke(lobby, null);
                    var keys = (List<string>)typeof(ChapterHostPanel).GetField("_cardKeys", F).GetValue(panel);
                    typeof(ChapterHostPanel).GetMethod("PickHost", F).Invoke(panel, new object[] { keys[0] });
                    typeof(ChapterHostPanel).GetMethod("OnStart", F).Invoke(panel, null);
                    _wait = 120;
                    return;
                }
                _wait = 15;
                return;
            }

            var t = typeof(BattleDirector);
            if (t.GetField("_canonRoom", F).GetValue(director) == null) { _wait = 30; return; }   // 첫 방이 아직
            // ⚠ 진입 가림막(BootCover)은 흐르는 시간으로 걷힌다 — 시간을 세우면 영영 덮인 채라 검은 장만 나왔다
            if (GameObject.Find("BootCover") != null) { Time.timeScale = 1f; _wait = 10; return; }
            if (!SessionState.GetBool("RoomLayout.preloaded", false))
            {
                Preload(director);
                SessionState.SetBool("RoomLayout.preloaded", true);
                _wait = 30;
                return;
            }
            if (!AtlasesReady(director)) { _wait = 10; return; }

            var rooms = SessionState.GetString("RoomLayout.rooms", string.Empty).Split(',');
            int room = SessionState.GetInt("RoomLayout.room", 0);
            int phase = SessionState.GetInt("RoomLayout.phase", 0);
            if (room >= rooms.Length)
            {
                SessionState.SetInt(Key, 0);
                Time.timeScale = 1f;
                SetHud(true);
                Debug.Log($"[RoomLayout] 끝 — {rooms.Length}방 · {OutDir}");
                return;
            }

            string id = rooms[room];
            switch (phase)
            {
                case 0:
                    // 방을 연다. 순번은 늘 1 — 순번이 쌓이면 방 배율(적 체력)이 오르고 보스 판정이 끼어든다
                    Time.timeScale = 1f;
                    // 판의 챕터를 방의 챕터로 — 잡몹은 판 챕터 목록으로 갈아 끼워지므로(`TrashForSlot`),
                    // 세이브가 1챕터면 3챕터 방에도 1챕터 잡몹이 섰다(2026-10-07)
                    var m = System.Text.RegularExpressions.Regex.Match(id, @"_CH(\d+)_");
                    if (m.Success) t.GetField("_runChapter", F).SetValue(director, int.Parse(m.Groups[1].Value));
                    t.GetField("_canonRoomId", F).SetValue(director, id);
                    t.GetMethod("EnterRoom", F).Invoke(director, new object[] { 1 });
                    SessionState.SetInt("RoomLayout.phase", 1);
                    _wait = SettleFrames;
                    return;
                case 1:
                    // 바닥은 방마다 따로 불러온다 — 다 걸릴 때까지 기다린다(안 그러면 지난 방 바닥이 찍힌다)
                    if ((int)t.GetField("_floorPending", F).GetValue(director) > 0) { _wait = 5; return; }
                    Time.timeScale = 0f;
                    // ⚠ 시간을 세워도 카메라는 매 프레임 캐릭터를 따라 다시 맞춘다(제 줌 · 스크롤을 덮어쓴다).
                    //   3택1 대기 플래그를 세우면 전투 틱이 카메라 앞에서 멈춘다 — 찍고 나서 되돌린다
                    t.GetField("_awaitingBuff", F).SetValue(director, true);
                    Overview(director);
                    SetHud(false);
                    SessionState.SetInt("RoomLayout.phase", 2);
                    _wait = 2;
                    return;
                case 2:
                    Overview(director);   // 세운 시간에도 카메라 틱이 돈다 — 찍기 직전에 한 번 더
                    ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, id + ".png"));
                    SessionState.SetInt("RoomLayout.phase", 3);
                    _wait = 3;
                    return;
                default:
                    t.GetField("_awaitingBuff", F).SetValue(director, false);
                    Debug.Log($"[RoomLayout] {room + 1}/{rooms.Length} {id}");
                    SessionState.SetInt("RoomLayout.room", room + 1);
                    SessionState.SetInt("RoomLayout.phase", 0);
                    _wait = 2;
                    return;
            }
        }

        /// <summary>
        /// 방 전체가 창에 들어오게 — 줌을 낮추고 방 가운데를 창 가운데에 둔다.
        /// 방 좌표 → 창 좌표는 f + 줌 × (방 + 스크롤 − f) (`BattleDirector.RoomToView`). f 는 캐릭터 자리(창 45 %).
        /// </summary>
        private static void Overview(BattleDirector director)
        {
            var t = typeof(BattleDirector);
            var field = (RectTransform)t.GetField("_field", F).GetValue(director);
            var roomSize = (Vector2)t.GetField("_roomSize", F).GetValue(director);
            float viewH = field.rect.height;
            float fy = -viewH * 0.45f;
            float s = (-viewH * 0.5f - fy) / OverviewZoom + fy + roomSize.y * 0.5f;
            t.GetField("_zoom", F).SetValue(director, OverviewZoom);
            t.GetField("_scroll", F).SetValue(director, new Vector2(0f, s));
            t.GetMethod("ApplyScroll", F).Invoke(director, null);
        }

        /// <summary>
        /// 모든 방에 서는 몸의 그림을 미리 올린다.
        ///
        /// 판은 시작한 챕터의 길에 나오는 몸만 올린다. 여기서는 한 판 안에서 열 챕터 방으로 건너뛰므로
        /// 그대로면 다른 챕터의 몸이 **흰 사각형**으로 선다 — 배치 확인 그림으로 쓸 수가 없다.
        /// (잠긴 챕터는 판을 따로 시작할 수 없다 — 세이브의 해금 챕터로 묶여 있다. 세이브는 건드리지 않는다)
        /// </summary>
        private static void Preload(BattleDirector director)
        {
            var t = typeof(BattleDirector);
            var rooms = t.GetField("_rooms", F).GetValue(director);
            var player = (Game.User.IPlayerDataService)t.GetField("_player", F).GetValue(director);
            if (rooms == null || player == null) return;
            var keys = new List<string>();
            var profile = t.GetMethod("ActorProfile", F);
            foreach (var room in (System.Collections.IEnumerable)rooms.GetType().GetProperty("Rooms").GetValue(rooms))
            {
                if (room == null) continue;
                foreach (var spawn in (System.Collections.IEnumerable)room.GetType().GetProperty("Spawns").GetValue(room))
                {
                    string actor = (string)spawn.GetType().GetProperty("ActorId").GetValue(spawn);
                    var e = profile.Invoke(director, new object[] { actor, player.AllHosts }) as Game.Character.HostEntry;
                    if (e != null && !keys.Contains(e.SpriteKey)) keys.Add(e.SpriteKey);
                }
            }
            SessionState.SetString("RoomLayout.keys", string.Join(",", keys));
            var res = CoreModule.Get<GameFramework.Core.Module.Resource.IResourceManager>();
            t.GetMethod("LoadUnitAtlasesAsync", F).Invoke(director, new object[] { res, keys });
            Debug.Log($"[RoomLayout] 몸 그림 {keys.Count}종 미리 올리는 중");
        }

        private static bool AtlasesReady(BattleDirector director)
        {
            var atlas = (System.Collections.IDictionary)typeof(BattleDirector).GetField("_unitAtlas", F).GetValue(director);
            foreach (var k in SessionState.GetString("RoomLayout.keys", string.Empty).Split(','))
                if (k.Length > 0 && !atlas.Contains(k)) return false;
            return true;
        }

        private static void KeepAlive(BattleDirector director)
        {
            var t = typeof(BattleDirector);
            // 판이 아직 설정을 못 받았으면(`_config` 없음) 건너뛴다 — `GhostHpMax` 가 매 프레임 NullReference 를 쌓았다(10-08)
            if (t.GetField("_config", F)?.GetValue(director) == null) return;
            var hp = t.GetField("_ghostHp", F);
            var max = t.GetProperty("GhostHpMax", F);
            if (hp != null && max != null) hp.SetValue(director, System.Convert.ChangeType(max.GetValue(director), hp.FieldType));
            t.GetField("_invuln", F)?.SetValue(director, 9999f);
        }

        private static void SetHud(bool on)
        {
            foreach (var n in HideNodes)
            {
                var go = FindAny(n);
                if (go != null) go.SetActive(on);
            }
        }

        /// <summary>꺼진 것까지 찾는다 — 되켤 때 `GameObject.Find` 는 꺼진 것을 못 찾는다.</summary>
        private static GameObject FindAny(string name)
        {
            foreach (var rt in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include))
                if (rt.name == name) return rt.gameObject;
            return null;
        }

        private static void OpenBootScene()
        {
            const string boot = "Assets/Scenes/BootScene.unity";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == boot) return;
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
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
            => typeof(RoomShotTool).GetMethod("SetSize", BindingFlags.NonPublic | BindingFlags.Static)
                                   .Invoke(null, new object[] { w, h });
    }
}
