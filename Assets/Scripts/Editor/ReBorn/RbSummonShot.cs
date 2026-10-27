// 소환 연출 녹화 — 플레이 중 로비 소환 쪽에서 SummonReveal.PlayAsync 를 직접 튼다(재화 · 뽑기 기록은 건드리지 않는다).
// 0.1초마다 화면을 찍어 Projects/AVSR/_exchange/ref/summon_v2/{장면}/ 에 남긴다 — GIF 는 바깥 스크립트가 묶는다.
// 장면: one_r · one_sr · one_ssr(1회, 몸 등급별) · ten(10회 — R 여섯 · SR 셋 · SSR 하나). 끝나면 다음 장면, 다 찍으면 플레이를 끈다.
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Lobby;
using Game.User;
using GameFramework.Core.Base;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.ReBorn
{
    [InitializeOnLoad]
    public static class RbSummonShot
    {
        private const string Key = "avsr.summonshot";
        private const string OutRoot = "Projects/AVSR/_exchange/ref/summon_v2";
        private static readonly (string name, float seconds)[] Scenes = { ("one_r", 5.5f), ("one_sr", 7f), ("one_ssr", 7.5f), ("ten", 12f) };
        private const float Step = 0.1f;

        private static int _scene = -1;
        private static float _startAt, _nextShot;
        private static int _frame;
        private static bool _playing;

        static RbSummonShot() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Game/RE:BORN 화면/소환 연출 녹화")]
        private static void Begin()
        {
            SessionState.SetInt(Key, 1);
            _scene = -1;
            if (!EditorApplication.isPlaying)
            {
                const string boot = "Assets/Scenes/BootScene.unity";
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != boot
                    && UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(boot);
                EditorApplication.EnterPlaymode();
            }
        }

        private static void Tick()
        {
            if (SessionState.GetInt(Key, 0) != 1 || !EditorApplication.isPlaying) return;
            var lobby = Object.FindAnyObjectByType<RbLobbyUI>();
            if (lobby == null || !lobby.IsReady)
            {
                // 타이틀 · 로딩 — 화면을 눌러 넘긴다
                foreach (var n in new[] { "SkipButton", "TouchArea", "StartButton" })
                    if (GameObject.Find(n) is GameObject go && go.TryGetComponent<UnityEngine.UI.Button>(out var b) && b.interactable) { b.onClick.Invoke(); break; }
                return;
            }
            if (_scene < 0) { _scene = 0; StartScene(lobby); return; }
            float now = Time.realtimeSinceStartup;
            if (_playing && now >= _nextShot)
            {
                _nextShot += Step;
                var dir = Path.Combine(OutRoot, Scenes[_scene].name);
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"f{_frame++:000}.png"));
            }
            if (_playing && now - _startAt >= Scenes[_scene].seconds)
            {
                _playing = false;
                _scene++;
                if (_scene >= Scenes.Length)
                {
                    SessionState.SetInt(Key, 0);
                    Debug.Log($"[소환 녹화] 끝 → {OutRoot}");
                    EditorApplication.ExitPlaymode();
                    return;
                }
                StartScene(lobby);
            }
        }

        private static void StartScene(RbLobbyUI lobby)
        {
            lobby.SelectTab("Summon");
            var reveal = Object.FindAnyObjectByType<SummonReveal>(FindObjectsInactive.Include);
            if (reveal == null || !CoreModule.TryGet<IPlayerDataService>(out var player)) { SessionState.SetInt(Key, 0); Debug.LogError("[소환 녹화] 소환 연출이 없다"); return; }
            var dir = Path.Combine(OutRoot, Scenes[_scene].name);
            if (Directory.Exists(dir)) foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
            Directory.CreateDirectory(dir);
            var entries = Entries(player, Scenes[_scene].name);
            reveal.PlayAsync(entries, "10회 더  젬 3,000", default).Forget();   // fire-and-forget: 녹화 — 시간이 다 되면 다음 장면이 덮는다
            _frame = 0;
            _startAt = Time.realtimeSinceStartup;
            _nextShot = _startAt;
            _playing = true;
        }

        /// <summary>장면에 맞는 몸 — 등급별로 표에서 고른다(숨은 몸 · 유령 빼고).</summary>
        private static List<SummonReveal.Entry> Entries(IPlayerDataService player, string scene)
        {
            var byGrade = new List<HostEntry>[3] { new(), new(), new() };
            foreach (var h in player.PlayableHosts)
                if (h != null && !h.IsGhost && !PlayerDataService.IsHiddenHost(h.HostKey)) byGrade[(int)h.Grade].Add(h);
            SummonReveal.Entry Make(HostEntry h, int shards, bool isNew)
                => new(h.HostKey, h.DisplayName, HostGradeLabel.Of(h.Grade), isNew ? "ui.summon.reveal.new" : "ui.summon.reveal.get",
                       h.Grade == HostGrade.B ? SummonReveal.Rarity.Normal : SummonReveal.Rarity.Rare, (int)h.Grade, shards, 0, isNew);
            var list = new List<SummonReveal.Entry>();
            switch (scene)
            {
                case "one_r": list.Add(Make(byGrade[0][0], 2, false)); break;
                case "one_sr": list.Add(Make(byGrade[1][0], 5, false)); break;
                case "one_ssr": list.Add(Make(byGrade[2][0], 5, true)); break;
                default:
                    int[] grades = { 0, 0, 1, 0, 0, 0, 2, 1, 0, 1 };
                    for (int i = 0; i < grades.Length; i++)
                    {
                        var pool = byGrade[grades[i]];
                        list.Add(Make(pool[i % pool.Count], grades[i] == 0 ? 2 : 5, i == 6));
                    }
                    break;
            }
            return list;
        }
    }
}
