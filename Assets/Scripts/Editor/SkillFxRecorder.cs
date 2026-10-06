using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.InGame;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Scene;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 스킬 연출 영상 — 23명의 액티브 스킬을 한 판에서 연달아 쓰고 하나씩 mp4 로 남긴다(2026-10-06).
    ///
    /// 이펙트 퀄업을 스킬마다 눈으로 보고 정하려고 만들었다. 스크린샷으로는 움직임 · 타이밍이 안 보인다.
    ///
    /// 쓰는 법: 테스트 판(`GameConfig._sandboxMode`)을 켜고 인게임 전투 방에 들어간 뒤 메뉴를 누른다.
    ///   몸마다 — 인게임 새로 불러오기 → 몸 바꾸기 → 1초 기다림 → 녹화 시작 → 스킬 → 6초 → 녹화 끝.
    ///   ⚠ 몸마다 판을 새로 연다 — 앞 스킬이 방을 비우면 보상 고르기 창을 기다리며 전투가 멈춰
    ///     다음 스킬들이 허공에 나가거나 안 나갔다(2026-10-06).
    ///   영상은 `Projects/AVSR/_exchange/skillfx_now/{순번}_{몸}.mp4` (게임 소리 포함).
    /// ⚠ 테스트 판은 다 찍은 뒤 반드시 끈다 — 켠 채로 빌드하면 적이 안 죽는 게임이 나간다.
    /// </summary>
    public static class SkillFxRecorder
    {
        private const string OutDir = "Projects/AVSR/_exchange/skillfx_now";
        private const float SettleSeconds = 1.2f;   // 몸을 바꾼 뒤 빙의 연출이 가라앉을 때까지
        private const float LeadSeconds = 0.4f;     // 스킬 전에 잠깐 — 시작 자세가 보이게
        private const float RecordSeconds = 6.5f;   // 컷인 + 스킬 + 여운

        private enum Step { Load, WaitBattle, WaitAtlas, Swap, Settle, Cast, Recording }
        private const float LoadSettleSeconds = 1.5f;   // 전투가 선 뒤 방 입장 연출이 끝날 때까지

        private static readonly List<string> s_keys = new();
        private static readonly List<int> s_numbers = new();   // 파일 순번(전체 목록 기준)
        private static int s_index;
        private static Step s_step;
        private static double s_at;
        private static RecorderController s_rec;
        private static BattleDirector s_battle;
        private static int s_width, s_height;

        [MenuItem("Tools/Game/스킬 연출 영상 찍기 (테스트 판 · 인게임에서)")]
        public static void RunAll() => Run();

        /// <summary>몇 종만 다시 찍는다(몸 키). 비우면 전부. 파일 번호는 전체 목록의 순번을 그대로 쓴다.</summary>
        public static void Run(params string[] only)
        {
            if (!Application.isPlaying) { Debug.LogError("[스킬 영상] 플레이 중 인게임에서 누른다"); return; }
            s_battle = Object.FindAnyObjectByType<BattleDirector>();
            if (s_battle == null || !s_battle.SandboxRunning)
            {
                Debug.LogError("[스킬 영상] 테스트 판 전투가 아니다 — GameConfig._sandboxMode 를 켜고 전투 방에 들어갈 것");
                return;
            }
            if (!CoreModule.TryGet<IPlayerDataService>(out var player)) return;

            s_keys.Clear();
            s_numbers.Clear();
            int n = 0;
            foreach (var e in player.PlayableHosts)
            {
                if (e == null || e.IsGhost) continue;
                n++;
                if (only != null && only.Length > 0 && System.Array.IndexOf(only, e.HostKey) < 0) continue;
                s_keys.Add(e.HostKey);
                s_numbers.Add(n);
            }
            Directory.CreateDirectory(OutDir);
            // 크기는 **처음 한 번만** 정한다 — 녹화기가 게임 화면을 녹화 크기로 바꿔 놓아서, 찍을 때마다
            // 「지금 화면의 반」을 다시 재면 1080 → 540 → 270 … 으로 줄어들었다(2026-10-06)
            s_width = Mathf.Max(2, Screen.width / 2 * 2);
            s_height = Mathf.Max(2, Screen.height / 2 * 2);
            s_index = 0;
            s_step = Step.Load;
            BattleDirector.SandboxSkipTopUp = true;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log($"[스킬 영상] 시작 — {s_keys.Count}종");
        }

        private static void Tick()
        {
            if (!Application.isPlaying) { Stop("플레이가 끝났다"); return; }
            double now = EditorApplication.timeSinceStartup;
            switch (s_step)
            {
                case Step.Load:
                    if (s_index >= s_keys.Count) { Stop(null); return; }
                    // fire-and-forget: 다 올라왔는지는 아래 WaitBattle 이 전투로 확인한다
                    CoreModule.Get<ISceneManager>()
                        .LoadAsync(new SceneLoadRequest { SceneName = SceneNames.InGame }).Forget();
                    s_battle = null;
                    s_at = now + 2.0;   // 옛 씬이 내려갈 틈
                    s_step = Step.WaitBattle;
                    break;
                case Step.WaitBattle:
                    if (now < s_at) return;
                    s_battle = Object.FindAnyObjectByType<BattleDirector>();
                    if (s_battle == null || !s_battle.SandboxRunning) return;
                    // 그 판에 안 나오는 몸은 그림이 안 올라와 있다 — 먼저 올린다(흰 네모 방지)
                    s_battle.SandboxPreload(s_keys[s_index]);
                    s_at = now + LoadSettleSeconds;
                    s_step = Step.WaitAtlas;
                    break;
                case Step.WaitAtlas:
                    if (now < s_at || s_battle == null) return;
                    if (!s_battle.SandboxUnitReady(s_keys[s_index]) && now < s_at + 5.0) return;
                    s_step = Step.Swap;
                    break;
                case Step.Swap:
                    if (now < s_at) return;
                    if (s_battle == null || !s_battle.SandboxSwapHost(s_keys[s_index]))
                    {
                        s_index++;
                        s_step = Step.Load;
                        return;
                    }
                    s_at = now + SettleSeconds;
                    s_step = Step.Settle;
                    break;
                case Step.Settle:
                    if (now < s_at) return;
                    StartRecording($"{OutDir}/{s_numbers[s_index]:00}_{s_keys[s_index]}");
                    s_at = now + LeadSeconds;
                    s_step = Step.Cast;
                    break;
                case Step.Cast:
                    if (now < s_at) return;
                    s_battle.TryActiveSkill();
                    s_at = now + RecordSeconds;
                    s_step = Step.Recording;
                    break;
                case Step.Recording:
                    if (now < s_at) return;
                    s_rec?.StopRecording();
                    s_rec = null;
                    Debug.Log($"[스킬 영상] {s_index + 1}/{s_keys.Count} {s_keys[s_index]}");
                    s_index++;
                    s_step = Step.Load;
                    break;
            }
        }

        private static void StartRecording(string pathNoExt)
        {
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "SkillFx";
            movie.Enabled = true;
            movie.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
            movie.VideoBitRateMode = VideoBitrateMode.High;
            // 게임 화면 크기 그대로(시작할 때 잰 값) — 해상도를 바꿔 가며 보는 일이 있어 고정 값을 쓰면 찌그러진다.
            // 게임 화면을 1280x720 Portrait(시안 기준 720x1280)로 두고 찍는다
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = s_width, OutputHeight = s_height };
            movie.AudioInputSettings.PreserveAudio = true;
            movie.OutputFile = pathNoExt;
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRate = 30f;
            s_rec = new RecorderController(settings);
            s_rec.PrepareRecording();
            s_rec.StartRecording();
        }

        private static void Stop(string why)
        {
            EditorApplication.update -= Tick;
            BattleDirector.SandboxSkipTopUp = false;
            if (s_rec != null && s_rec.IsRecording()) s_rec.StopRecording();
            s_rec = null;
            Debug.Log(why == null ? $"[스킬 영상] 끝 — {OutDir}" : $"[스킬 영상] 멈춤 — {why}");
        }
    }
}
